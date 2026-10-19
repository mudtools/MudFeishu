// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;

namespace Mud.Feishu.AI.CapabilityDemo.Acts;

/// <summary>
/// A4：执行链 fail-closed（脚本化模型，零 token / 零网络）。
/// </summary>
/// <remarks>
/// 只演示宿主可观测的四步：入站净化、策略轴、授权门禁、上下文校验；内容安全与出站净化
/// 由库内固定实现，本幕只打印边界，不伪造命中（方案 ADR-03）。
/// </remarks>
internal static class A4ChainAct
{
    private const string ReadTool = "bitable.query_records";
    private const string WriteTool = "bitable.delete_record";

    public static async Task<int> RunAsync(string? caseId, DemoSettings settings, CancellationToken cancellationToken)
    {
        var selected = string.IsNullOrWhiteSpace(caseId) || caseId == "all"
            ? new[] { "a", "b", "c", "d" }
            : new[] { caseId.Trim().ToLowerInvariant() };

        DemoConsole.Banner("A4 · 七步链与 fail-closed（脚本化模型）", new Dictionary<string, string?>
        {
            ["用例"] = string.Join(",", selected),
            ["成本"] = "零 token；不需要模型 Key；四个用例都在下游调用前被拒",
            ["边界"] = "内容安全/出站净化无中间态，本幕只打印配置与边界",
        });

        var allPassed = true;
        foreach (var id in selected)
        {
            allPassed &= id switch
            {
                "a" => await RunInvalidArgsAsync(settings, cancellationToken),
                "b" => await RunRiskExceededAsync(settings, cancellationToken),
                "c" => await RunAuthorizationDeniedAsync(settings, cancellationToken),
                "d" => await RunMissingContextAsync(settings, cancellationToken),
                "e" => await RunEmptyAppKeyAsync(settings, cancellationToken),
                _ => Unknown(id),
            };
        }

        DemoConsole.Line();
        DemoConsole.Notice(allPassed ? NoticeLevel.Info : NoticeLevel.Error,
            allPassed ? "自检通过：执行链按期望 fail-closed。" : "自检失败：至少一个用例未按期望拒绝。");
        return allPassed ? 0 : 1;

        static bool Unknown(string id)
        {
            DemoConsole.Notice(NoticeLevel.Error, $"未知用例 '{id}'（合法：a/b/c/d/e/all）");
            return false;
        }
    }

    // ── a：入站净化（控制字符） ───────────────────────────────────────────────
    private static Task<bool> RunInvalidArgsAsync(DemoSettings settings, CancellationToken ct)
        => RunReadCaseAsync(
            "a · 入站净化",
            settings,
            ScriptedChatClient.ToolCall(ReadTool, new Dictionary<string, object?>
            {
                ["app_token"] = "bascn_placeholder",
                ["table_id"] = "tbl_placeholder",
                ["filter"] = "状态 = \"待办\"\u0007",
            }),
            expect: "invalid_args",
            configure: null,
            beginContext: true,
            ct);

    // ── b：策略轴（风险上限） ────────────────────────────────────────────────
    private static async Task<bool> RunRiskExceededAsync(DemoSettings settings, CancellationToken ct)
    {
        var profile = ActProfiles.A1(settings) with
        {
            Id = "a4-b",
            AgentName = "cap-a4-b",
            ReadTools = [ReadTool],
            WriteTools = [WriteTool],
            ScriptedClient = new ScriptedChatClient(
                ScriptedChatClient.ToolCall(WriteTool, new Dictionary<string, object?>
                {
                    ["app_token"] = "bascn_placeholder",
                    ["table_id"] = "tbl_placeholder",
                    ["record_id"] = "rec_placeholder",
                }),
                "好的，已记录。"),
        };

        // 四个用例都在下游调用前被拒：占位应用足够，且绝不发 HTTP。
        var configuration = DemoConfig.CreateVerifyConfiguration();
        await using var provider = CompositionRoot.Build(
            configuration,
            settings,
            profile,
            includeModel: false,
            consoleAllowed: true,
            configureServices: null,
            configureOptions: o => o.MaxToolRisk = "read");

        var agent = provider.GetRequiredService<FeishuAgent>();
        var accessor = provider.GetRequiredService<IFeishuToolContextAccessor>();
        var sink = provider.GetRequiredService<InMemoryAuditSink>();
        var board = provider.GetRequiredService<PendingApprovalBoard>();

        var key = ConversationKeys.Build(settings.AppKey, "a4b", settings.UserId);
        var session = await agent.GetOrCreateSessionAsync(key, ct);

        using (accessor.Begin(new FeishuToolContext(settings.AppKey, key, UserId: settings.UserId)))
        {
            await agent.RunAsync("删掉这一条。", session, cancellationToken: ct);
        }

        // 写工具先被 MAF 改写为审批请求；批准续跑后才会进入策略轴。
        var pending = board.Snapshot().FirstOrDefault();
        if (pending is null)
        {
            DemoConsole.Section("b · 策略轴");
            DemoConsole.Notice(NoticeLevel.Error, "前置失败：写工具未被改写成审批请求（未登记挂起项）。");
            DemoConsole.AuditTable(sink.Snapshot());
            return false;
        }

        var gate = provider.GetRequiredService<IConversationGate>();
        var channel = provider.GetRequiredService<IFeishuToolApprovalChannel>();
        var store = provider.GetRequiredService<IPendingApprovalStore>();

        await agent.RunApprovalContinuationAsync(
            accessor,
            pending.Framework,
            approved: true,
            reason: "A4 自动批准（只为跨过 MAF，验证策略轴）",
            conversationGate: gate,
            approvalChannel: channel,
            logger: NullLogger.Instance,
            pendingApprovalStore: store,
            cancellationToken: ct);

        return Report("b · 策略轴",  "risk_exceeded", sink);
    }

    // ── c：授权门禁（显式封禁只读工具） ──────────────────────────────────────
    private static Task<bool> RunAuthorizationDeniedAsync(DemoSettings settings, CancellationToken ct)
        => RunReadCaseAsync(
            "c · 授权门禁",
            settings,
            ScriptedChatClient.ToolCall(ReadTool, new Dictionary<string, object?>
            {
                ["app_token"] = "bascn_placeholder",
                ["table_id"] = "tbl_placeholder",
            }),
            expect: "authorization_denied",
            configure: state => state.DeniedTools.Add(ReadTool),
            beginContext: true,
            ct);

    // ── d：缺上下文 ──────────────────────────────────────────────────────────
    private static Task<bool> RunMissingContextAsync(DemoSettings settings, CancellationToken ct)
        => RunReadCaseAsync(
            "d · 缺租户上下文",
            settings,
            ScriptedChatClient.ToolCall(ReadTool, new Dictionary<string, object?>
            {
                ["app_token"] = "bascn_placeholder",
                ["table_id"] = "tbl_placeholder",
            }),
            expect: "上下文缺失",
            configure: null,
            beginContext: false,
            ct);

    // ── e：空 appKey（TMA2-20） ─────────────────────────────────────────────
    private static Task<bool> RunEmptyAppKeyAsync(DemoSettings settings, CancellationToken ct)
        => RunReadCaseAsync(
            "e · 空 appKey",
            settings,
            ScriptedChatClient.ToolCall(ReadTool, new Dictionary<string, object?>
            {
                ["app_token"] = "bascn_placeholder",
                ["table_id"] = "tbl_placeholder",
            }),
            expect: "TMA2-20",
            configure: null,
            beginContext: true,
            ct,
            appKeyOverride: string.Empty);

    private static async Task<bool> RunReadCaseAsync(
        string name,
        DemoSettings settings,
        Func<ChatMessage> firstResponse,
        string expect,
        Action<ConsoleAuthorizerState>? configure,
        bool beginContext,
        CancellationToken ct,
        string? appKeyOverride = null)
    {
        var profile = ActProfiles.A1(settings) with
        {
            Id = "a4",
            AgentName = "cap-a4",
            ReadTools = [ReadTool],
            WriteTools = [],
            ScriptedClient = new ScriptedChatClient(firstResponse, "（本轮结束）"),
        };

        var configuration = DemoConfig.CreateVerifyConfiguration();
        await using var provider = CompositionRoot.Build(
            configuration, settings, profile, includeModel: false);

        if (configure is not null)
        {
            configure(provider.GetRequiredService<ConsoleAuthorizerState>());
        }

        var agent = provider.GetRequiredService<FeishuAgent>();
        var accessor = provider.GetRequiredService<IFeishuToolContextAccessor>();
        var sink = provider.GetRequiredService<InMemoryAuditSink>();

        var key = ConversationKeys.Build(settings.AppKey, "a4", settings.UserId);
        var session = await agent.GetOrCreateSessionAsync(key, ct);

        if (beginContext)
        {
            using (accessor.Begin(new FeishuToolContext(appKeyOverride ?? settings.AppKey, key, UserId: settings.UserId)))
            {
                await agent.RunAsync("执行一次查询。", session, cancellationToken: ct);
            }
        }
        else
        {
            await agent.RunAsync("执行一次查询。", session, cancellationToken: ct);
        }

        return Report(name, expect, sink);
    }

    private static bool Report(string name, string expect, InMemoryAuditSink sink)
    {
        var records = sink.Snapshot();
        var actual = records.LastOrDefault()?.Reason ?? "（无审计记录）";
        var pass = actual.Contains(expect, StringComparison.Ordinal);

        DemoConsole.Section($"{name} · 期望包含「{expect}」");
        DemoConsole.Notice(pass ? NoticeLevel.Info : NoticeLevel.Error, $"实际：{actual}");
        DemoConsole.AuditTable(records);
        return pass;
    }
}
