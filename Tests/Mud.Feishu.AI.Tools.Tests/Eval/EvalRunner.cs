// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.AI.Tools.Tests.Eval;

/// <summary>
/// 评测结局桶：报告里四类失败（选错工具 / 参数错 / 幻觉工具名 / 授权拒）的机器可分辨形态。
/// </summary>
internal enum EvalBucket
{
    /// <summary>执行成功并返回结果。</summary>
    Ok,

    /// <summary>写工具预演（<c>[dry_run]</c> 计划回执，零副作用）。</summary>
    Plan,

    /// <summary>入参被结构化拒绝。</summary>
    Validation,

    /// <summary>需人工确认（HITL 挂起）。</summary>
    Confirmation,

    /// <summary>授权拒（宿主门禁 fail-closed）。</summary>
    Denied,

    /// <summary>写类工具不在启用面上（模型根本看不见它）。</summary>
    NotEnabled,

    /// <summary>超出宿主风险上限。</summary>
    Risk,

    /// <summary>未归类——评测要求恒为 0，否则判据本身失效。</summary>
    Unknown,
}

/// <summary>选择层单条用例的判定明细。</summary>
internal sealed record SelectionFinding(
    string Id,
    string Domain,
    string Intent,
    IReadOnlyList<string> Expect,
    string Top1,
    int Rank,
    int GuidedRank,
    double Margin,
    bool Hallucinated,
    bool ExpectsWriteTool);

/// <summary>参数层单条用例的判定明细。</summary>
internal sealed record ParameterFinding(
    string Id,
    string Tool,
    string Layer,
    string Expect,
    bool Passed,
    string Detail);

/// <summary>场景层单步的判定明细。</summary>
internal sealed record ScenarioFinding(
    string Scenario,
    string Profile,
    int Step,
    string Tool,
    string Expect,
    EvalBucket Actual,
    bool Passed,
    string Detail);

/// <summary>
/// 三层评测的执行器与报告渲染器。
/// </summary>
/// <remarks>
/// <b>可复现性</b>：输入全部是仓库内资产 + 编译期 Schema；无网络、无随机、排序带确定序键，
/// 同一份代码与资产在任何机器上得到同一份报告。设 <c>FeishuEvalDumpDir</c> 环境变量可把报告
/// 落成 Markdown 供人工评审；未设置时报告只在断言失败时进入失败消息。
/// </remarks>
internal sealed class EvalRunner : IDisposable
{
    private const string PlanMarker = "[dry_run]";

    /// <summary>
    /// 无分类的结构化错误出口（<c>ToolResult.FromError(StructuredError(name, reason))</c>）：
    /// 文本以它开头即执行链已回拒，但 <c>Error</c> 为空——bitable 的筛选/排序文法走这条路。
    /// </summary>
    private const string ErrorTextMarker = "[tool_error]";

    private readonly EvalToolSurface _surface = EvalToolSurface.Instance;
    private readonly Dictionary<string, ServiceProvider> _providers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ScriptedAuthorizer> _authorizers = new(StringComparer.Ordinal);

    /// <summary>桶名的线面拼写（与 <c>scenario_cases.json</c> 的 expect 词汇一致）。</summary>
    internal static string Literal(EvalBucket bucket) => bucket switch
    {
        EvalBucket.Ok => "ok",
        EvalBucket.Plan => "plan",
        EvalBucket.Validation => "validation",
        EvalBucket.Confirmation => "confirmation",
        EvalBucket.Denied => "denied",
        EvalBucket.NotEnabled => "not_enabled",
        EvalBucket.Risk => "risk",
        _ => "unknown",
    };

    internal IReadOnlyList<SelectionFinding> RunSelection(int candidateDepth = 20)
    {
        var findings = new List<SelectionFinding>();
        foreach (var testCase in EvalAssets.Selection)
        {
            var bare = _surface.Rank(testCase.Intent, guided: false, candidateDepth);
            var guided = _surface.Rank(testCase.Intent, guided: true, candidateDepth);
            var rank = BestRank(bare, testCase.Expect);
            var guidedRank = BestRank(guided, testCase.Expect);
            var matched = testCase.Expect.FirstOrDefault(name => Array.IndexOf(bare, name) == 0) ?? testCase.Expect[0];

            findings.Add(new SelectionFinding(
                testCase.Id,
                testCase.Domain,
                testCase.Intent,
                testCase.Expect,
                bare.Length > 0 ? bare[0] : string.Empty,
                rank,
                guidedRank,
                _surface.TopMargin(testCase.Intent, matched, guided: false),
                testCase.Expect.Any(name => !_surface.Contains(name)),
                testCase.Expect.Any(name => _surface.Find(name)?.IsWrite ?? false)));
        }

        return findings;
    }

    internal async Task<IReadOnlyList<ParameterFinding>> RunParametersAsync()
    {
        var findings = new List<ParameterFinding>();
        foreach (var testCase in EvalAssets.Parameters)
        {
            _surface.Contains(testCase.Tool).Should().BeTrue(
                $"参数用例 {testCase.Id} 引用了不存在的工具 '{testCase.Tool}'——评测集与工具面漂移，报告失去意义");

            var expectValid = string.Equals(testCase.Expect, "valid", StringComparison.OrdinalIgnoreCase);
            string detail;
            bool accepted;

            if (testCase.Layer == "schema")
            {
                var errors = EvalArgumentValidator.Validate(testCase.Tool, testCase.Arguments);
                accepted = errors.Count == 0;
                detail = string.Join(",", errors);
            }
            else
            {
                var result = await InvokeAsync("write_open", testCase.Tool, testCase.Arguments).ConfigureAwait(false);
                accepted = !IsRejected(result);
                detail = accepted
                    ? Truncate(result.Text)
                    : DescribeRejection(result);
            }

            findings.Add(new ParameterFinding(testCase.Id, testCase.Tool, testCase.Layer, testCase.Expect, accepted == expectValid, detail));
        }

        return findings;
    }

    internal async Task<IReadOnlyList<ScenarioFinding>> RunScenariosAsync()
    {
        var findings = new List<ScenarioFinding>();
        foreach (var scenario in EvalAssets.Scenarios)
        {
            var step = 0;
            foreach (var item in scenario.Steps)
            {
                step++;
                if (item.ApproveAfterHost)
                {
                    _authorizers.TryGetValue(scenario.Profile, out var scripted).Should().BeTrue(
                        $"场景「{scenario.Name}」用了 approve 步骤，但剖面 {scenario.Profile} 没装配脚本化授权器");
                    scripted!.Approve();
                }

                var (bucket, detail) = await ClassifyStepAsync(
                    scenario.Profile, item.Tool, item.Arguments ?? new Dictionary<string, object?>()).ConfigureAwait(false);

                var expected = item.Expect.Split('|').Select(static s => s.Trim()).ToArray();
                var actual = Literal(bucket);
                findings.Add(new ScenarioFinding(
                    scenario.Name, scenario.Profile, step, item.Tool, item.Expect, bucket,
                    expected.Contains(actual, StringComparer.Ordinal), detail));
            }
        }

        return findings;
    }

    /// <summary>跑完三层并渲染完整报告（落盘只在设置 <c>FeishuEvalDumpDir</c> 时发生）。</summary>
    internal async Task<string> RenderAllAsync()
    {
        var selection = RunSelection();
        var parameters = await RunParametersAsync().ConfigureAwait(false);
        var scenarios = await RunScenariosAsync().ConfigureAwait(false);
        return Render(selection, parameters, scenarios);
    }

    internal string Render(
        IReadOnlyList<SelectionFinding> selection,
        IReadOnlyList<ParameterFinding> parameters,
        IReadOnlyList<ScenarioFinding> scenarios)
    {
        var total = selection.Count;
        var entries = _surface.Entries;
        var builder = new StringBuilder();
        builder.Append("# 工具面评测报告（确定性三层）\n\n");
        builder.Append($"- 工具面：{entries.Count} 枚（只读 {entries.Count(static e => !e.IsWrite)} / 写类 {entries.Count(static e => e.IsWrite)}）；"
            + $"affordance 覆盖 {entries.Count(static e => e.HasAffordance)} 枚\n");
        builder.Append($"- 选择用例：{total} 条 / {selection.Select(static s => s.Domain).Distinct().Count()} 个域\n");
        builder.Append($"- pass@1（裸可见面 tools/list）：{Percent(selection.Count(static f => f.Rank == 1), total)}\n");
        builder.Append($"- pass@3：{Percent(selection.Count(static f => f.Rank is > 0 and <= 3), total)}；"
            + $"pass@5：{Percent(selection.Count(static f => f.Rank is > 0 and <= 5), total)}\n");
        builder.Append($"- pass@1（加 affordance 视图）：{Percent(selection.Count(static f => f.GuidedRank == 1), total)}\n");
        builder.Append($"- 幻觉工具名：{selection.Count(static f => f.Hallucinated)}\n");
        builder.Append($"- 首位但零判别余量：{Percent(selection.Count(static f => f.Rank == 1 && f.Margin <= 0), total)}\n");
        builder.Append($"- 期望写类工具（默认只读面对模型不可见）：{selection.Count(static f => f.ExpectsWriteTool)}\n\n");

        builder.Append("## 一、选择层未中首位的用例\n\n");
        AppendMisses(builder, selection.Where(static f => f.Rank != 1).Take(25));

        builder.Append("\n## 二、参数层\n\n");
        foreach (var layer in new[] { "schema", "handler" })
        {
            var slice = parameters.Where(p => p.Layer == layer).ToArray();
            builder.Append($"- {layer} 层：{slice.Count(static p => p.Passed)}/{slice.Length} 与期望一致\n");
        }

        foreach (var item in parameters.Where(static p => !p.Passed))
        {
            builder.Append($"  - 不一致 {item.Id} {item.Tool}（期望 {item.Expect}）：{item.Detail}\n");
        }

        builder.Append("\n## 三、场景层结局桶\n\n");
        foreach (var group in scenarios.GroupBy(static s => s.Actual).OrderBy(static g => Literal(g.Key), StringComparer.Ordinal))
        {
            builder.Append($"- {Literal(group.Key)}：{group.Count()} 步\n");
        }

        builder.Append($"\n- 步数合计 {scenarios.Count}；场景 {scenarios.Select(static s => s.Scenario).Distinct().Count()} 个；"
            + $"装配剖面 {scenarios.Select(static s => s.Profile).Distinct().Count()} 种\n");

        foreach (var item in scenarios.Where(static s => !s.Passed))
        {
            builder.Append($"  - 不一致 {item.Scenario} 第 {item.Step} 步 {item.Tool}（期望 {item.Expect}）"
                + $"实落 {Literal(item.Actual)}：{item.Detail}\n");
        }

        var validationSteps = scenarios.Count(static s => s.Actual == EvalBucket.Validation);
        var gatedSteps = scenarios.Count(static s => s.Actual is EvalBucket.Denied or EvalBucket.NotEnabled or EvalBucket.Risk);
        var declaredInvalid = parameters.Count(static p => p.Expect == "invalid");
        var rejectedAsDeclared = parameters.Count(static p => p.Expect == "invalid" && p.Passed);
        builder.Append("\n## 四、失败分类（选错工具 / 参数错 / 幻觉工具名 / 授权拒）\n\n");
        builder.Append($"- 选错工具（未中首位）：{selection.Count(static f => f.Rank != 1)}/{total}\n");
        builder.Append($"- 参数错：声明应拒 {declaredInvalid} 条、按期望回拒 {rejectedAsDeclared} 条；"
            + $"场景层另有 {validationSteps} 步落在参数校验桶\n");
        builder.Append($"- 幻觉工具名：{selection.Count(static f => f.Hallucinated)}\n");
        builder.Append($"- 授权拒（含不可见与超风险）：{gatedSteps} 步\n");

        var report = builder.ToString();
        DumpIfRequested(report);
        return report;
    }

    public void Dispose()
    {
        foreach (var provider in _providers.Values)
        {
            provider.Dispose();
        }

        _providers.Clear();
    }

    private static int BestRank(string[] ranked, IReadOnlyList<string> expect)
    {
        var best = int.MaxValue;
        foreach (var name in expect)
        {
            var index = Array.IndexOf(ranked, name);
            if (index >= 0 && index + 1 < best)
            {
                best = index + 1;
            }
        }

        return best == int.MaxValue ? -1 : best;
    }

    private static string Percent(int hits, int total)
        => total == 0 ? "n/a" : $"{(double)hits / total:P1}（{hits}/{total}）";

    private static void AppendMisses(StringBuilder builder, IEnumerable<SelectionFinding> misses)
    {
        var written = false;
        foreach (var miss in misses)
        {
            written = true;
            builder.Append($"  - {miss.Id} 「{miss.Intent}」→ 期望 {string.Join('/', miss.Expect)}，"
                + $"首位 {miss.Top1}（名次 {(miss.Rank < 0 ? "未入候选" : miss.Rank.ToString())}）\n");
        }

        if (!written)
        {
            builder.Append("  - 无\n");
        }
    }

    /// <summary>
    /// 执行链是否回拒：带错误分类的 <c>Error</c>，或无分类但已成文的结构化错误出口（两条都算拒绝）。
    /// </summary>
    private static bool IsRejected(ToolResult result)
        => result.Error is not null || (result.Text?.StartsWith(ErrorTextMarker, StringComparison.Ordinal) ?? false);

    private static string DescribeRejection(ToolResult result)
        => result.Error is { } error
            ? $"{error.Category}/{error.Subtype}：{Truncate(result.Text)}"
            : Truncate(result.Text);

    private static string Truncate(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        const int limit = 160;
        return text.Length <= limit ? text : text[..limit] + "…";
    }

    private static void DumpIfRequested(string report)
    {
        var directory = Environment.GetEnvironmentVariable("FeishuEvalDumpDir");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        // 相对值按仓库根解析：测试宿主的工作目录是输出目录，直接写会把报告丢进 bin\。
        var target = Path.IsPathRooted(directory)
            ? directory
            : Path.Combine(EvalAssets.RepoRoot(), directory.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "eval-report.md"), report, new UTF8Encoding(false));
    }

    private async Task<(EvalBucket Bucket, string Detail)> ClassifyStepAsync(
        string profile,
        string tool,
        IReadOnlyDictionary<string, object?> arguments)
    {
        var registry = Provider(profile).GetRequiredService<FeishuToolRegistry>();
        if (!registry.IsEnabled(tool))
        {
            return (EvalBucket.NotEnabled, "工具未在该装配的启用面上（对模型不可见）");
        }

        var result = await InvokeAsync(profile, tool, arguments).ConfigureAwait(false);
        var text = result.Text ?? string.Empty;
        if (result.Error is null)
        {
            if (text.StartsWith(ErrorTextMarker, StringComparison.Ordinal))
            {
                return (EvalBucket.Validation, Truncate(text));
            }

            return (text.StartsWith(PlanMarker, StringComparison.Ordinal) ? EvalBucket.Plan : EvalBucket.Ok, Truncate(text));
        }

        var category = result.Error.Category?.ToString() ?? string.Empty;
        var subtype = result.Error.Subtype?.ToString() ?? string.Empty;
        var signal = category + "/" + subtype + "/" + text;
        if (signal.Contains("risk", StringComparison.OrdinalIgnoreCase))
        {
            return (EvalBucket.Risk, Truncate(text));
        }

        return category switch
        {
            "validation" => (EvalBucket.Validation, Truncate(text)),
            "confirmation" => (EvalBucket.Confirmation, Truncate(text)),
            "authorization" => (EvalBucket.Denied, Truncate(text)),
            _ => (EvalBucket.Unknown, $"{category}/{subtype}：{Truncate(text)}"),
        };
    }

    private async Task<ToolResult> InvokeAsync(string profile, string tool, IReadOnlyDictionary<string, object?> arguments)
    {
        var registry = Provider(profile).GetRequiredService<FeishuToolRegistry>();
        registry.TryGet(tool, out var definition).Should().BeTrue($"评测引用了未注册的工具 '{tool}'");
        return await definition!.Handler(arguments, new FeishuToolContext("eval_harness"), CancellationToken.None).ConfigureAwait(false);
    }

    private ServiceProvider Provider(string profile)
    {
        if (_providers.TryGetValue(profile, out var cached))
        {
            return cached;
        }

        Action<IServiceCollection>? configureServices = null;
        if (profile == "hitl_confirm")
        {
            var authorizer = new ScriptedAuthorizer();
            _authorizers[profile] = authorizer;
            configureServices = collection =>
            {
                collection.AddSingleton<IToolExecutionAuthorizer>(authorizer);
                collection.AddSingleton<IFeishuToolApprovalChannel>(Mock.Of<IFeishuToolApprovalChannel>());
            };
        }

        var provider = GuardProviderFactory.CreateProvider(
            options =>
            {
                options.Tools = [.. FeishuToolNames.ReadonlyAll];
                options.WriteAllowList = [.. FeishuToolNames.WriteAll];
                options.AllowedIdentities = ["tenant", "user"];
                switch (profile)
                {
                    case "readonly_default":
                        options.WriteAllowList = [];
                        break;
                    case "write_forced":
                        break;
                    case "write_open":
                        options.EnforceToolAuthorization = false;
                        break;
                    case "risk_ceiling_read":
                        options.EnforceToolAuthorization = false;
                        options.MaxToolRisk = "read";
                        break;
                    case "hitl_confirm":
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"未知评测装配剖面 '{profile}'——新增剖面必须同时在 scenario_cases.json 登记判据");
                }
            },
            configureServices: configureServices);

        _providers[profile] = provider;
        return provider;
    }

    /// <summary>脚本化授权器：写工具先判"需人工确认"，宿主模拟批准后转放行。</summary>
    private sealed class ScriptedAuthorizer : IToolExecutionAuthorizer
    {
        private bool _approved;

        internal void Approve() => _approved = true;

        public Task<AuthorizationResult> AuthorizeAsync(
            string toolName,
            IReadOnlyList<string> requiredScopes,
            bool isWrite,
            IReadOnlyDictionary<string, object?> arguments,
            FeishuToolContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(!isWrite || _approved
                ? AuthorizationResult.Allowed
                : AuthorizationResult.Confirm("评测模拟：写操作等待用户批准"));
    }
}
