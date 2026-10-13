// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Metrics;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>S-4：工具策略（glob / 域级）契约守卫</b>——把"策略配置写错会静默失效 / 会把生产全拒"
/// 这两类形态钉死为可断言的失败。
/// </summary>
/// <remarks>
/// <para>
/// 守卫面（对应方案 §6 S-1~S-3）：
/// <list type="number">
/// <item><b>Deny 优先反例</b>：同一工具同时被 Allow 与 Deny 命中 ⇒ 必须拒（策略面最危险的歧义）；</item>
/// <item><b>域轴先于工具名轴</b>：拒绝原因必须是 <c>domain_not_allowed</c>（排障路径更短）；</item>
/// <item><b>空白名单语义</b>：空 Allow = 该轴不启用（<b>不是"全拒"</b>——否则
/// <c>Deny:["payroll.*"]</c> 这种最常见的用法会变成全工具不可用）；</item>
/// <item><b>非法 glob fail-closed</b>：装配期 <c>Validate()</c> 即拒（含域轴误写多段模式）；</item>
/// <item><b>策略可观测（S-3）</b>：审计 reason 必须回显命中的规则原文；</item>
/// <item><b>reason_code 闭集有产出</b>：<c>domain_not_allowed</c> / <c>write_not_allowed</c> 必须真的产出。</item>
/// </list>
/// </para>
/// </remarks>
public class ToolPolicyContractGuards
{
    // ────────── ① 策略轴行为（真跑一遍执行链） ──────────

    /// <summary>Deny 优先于 Allow：同一工具同时命中两轴 ⇒ 必须拒（且原因回显 Deny 规则）。</summary>
    [Fact]
    public async Task DenyWinsOverAllow_WhenBothMatchTheSameTool()
    {
        var records = new List<ToolExecutionAuditRecord>();
        var binding = CreateBinding(
            Policy(allow: ["bitable.*"], deny: ["bitable.list_tables"]),
            new CapturingAuditSink(records));
        var downstreamCalled = false;

        var result = await binding.ExecuteAsync(
            Definition("bitable.list_tables"),
            Args(),
            new FeishuToolContext("appA"),
            _ => { downstreamCalled = true; return Task.FromResult(ToolResult.FromText("ok")); });

        downstreamCalled.Should().BeFalse("Deny 命中必须零调用下游");
        result.ToString().Should().Contain("policy_denied");
        result.ToString().Should().Contain(
            "命中 ToolPolicy.Deny 规则 'bitable.list_tables'", "拒绝原因必须回显命中的规则（S-3）");
        // S-2 修复的副产品：策略拒绝的正文标签必须是 policy_denied（此前落到 api_error，
        // 会给出"核实后重试"的错误下一步）。
        result.ToString().Should().Contain("(policy_denied)");
        records.Should().ContainSingle();
        records[0].Decision.Should().Be(FeishuMetrics.ToolOutcomes.Denied);
    }

    /// <summary>Allow 生效时未列明的工具被拒（白名单模式），而列明者放行。</summary>
    [Fact]
    public async Task AllowListMode_ShouldDenyUnlisted_AndAllowListed()
    {
        var binding = CreateBinding(Policy(allow: ["bitable.*"]));

        var denied = await binding.ExecuteAsync(
            Definition("im.send_message", isWrite: true),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("ok")));
        denied.ToString().Should().Contain("policy_denied");
        denied.ToString().Should().Contain(ToolErrorSubtype.WriteNotAllowed,
            "写工具被 Allow 白名单拒 → reason_code = write_not_allowed（与只读工具的排障路径区分）");

        var allowed = await binding.ExecuteAsync(
            Definition("bitable.list_fields"),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("ok")));
        allowed.ToString().Should().Be("ok", "命中的白名单工具必须放行");
    }

    /// <summary>域轴先于工具名轴：域被禁时拒绝原因是 <c>domain_not_allowed</c>（即使工具名轴放行）。</summary>
    [Fact]
    public async Task DomainAxis_ShouldBeEvaluatedBeforeToolNameAxis()
    {
        var binding = CreateBinding(Policy(
            allow: ["payroll.*"], domainsDeny: ["payroll"]));

        var result = await binding.ExecuteAsync(
            Definition("payroll.list_paygroups"),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("ok")));

        result.ToString().Should().Contain(ToolErrorSubtype.DomainNotAllowed);
        result.ToString().Should().Contain(
            "'payroll'", "拒绝原因必须点明命中的规则原文（域 Deny 'payroll'，S-3 可观测）");
    }

    /// <summary>
    /// <b>空白名单语义（本守卫最重要的一条）</b>：只配 <c>Deny</c> 时，未命中 Deny 的工具必须放行——
    /// 把空 Allow 读成"全拒"会直接产出"生产全不可用"。
    /// </summary>
    [Fact]
    public async Task DenyOnlyPolicy_ShouldNotDenyEverything()
    {
        var binding = CreateBinding(Policy(deny: ["payroll.*"]));

        var allowed = await binding.ExecuteAsync(
            Definition("im.send_message", isWrite: true),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("ok")));
        allowed.ToString().Should().Be(
            "ok", "只拉黑名单（Allow 为空）时，未命中 Deny 的工具必须放行——空 Allow ≠ 全拒");

        var denied = await binding.ExecuteAsync(
            Definition("payroll.list_paygroups"),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("ok")));
        denied.ToString().Should().Contain("policy_denied");
    }

    /// <summary>未配置策略（四集合全空）⇒ 不生效（逐字节等价改造前行为）。</summary>
    [Fact]
    public async Task UnconfiguredPolicy_ShouldBeNoOp()
    {
        var binding = CreateBinding(new FeishuAgentOptions { Instructions = "test" });

        var result = await binding.ExecuteAsync(
            Definition("payroll.list_paygroups"),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("ok")));

        result.ToString().Should().Be("ok", "未配置 ToolPolicy 时不得产生任何新的拒绝");
    }

    /// <summary>跨域危险动词模式（<c>*.delete_*</c>）必须真的能拦下删除类工具，且不误伤读工具。</summary>
    [Fact]
    public async Task CrossDomainDangerVerbPattern_ShouldMatchDeleteToolsOnly()
    {
        var binding = CreateBinding(Policy(deny: ["*.delete_*"]));

        var denied = await binding.ExecuteAsync(
            Definition("bitable.delete_record", isWrite: true, risk: FeishuToolRisk.HighRiskWrite),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("ok")));
        denied.ToString().Should().Contain(ToolErrorSubtype.WriteNotAllowed);

        var allowed = await binding.ExecuteAsync(
            Definition("bitable.query_records"),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("ok")));
        allowed.ToString().Should().Be("ok", "通配域 + 危险动词模式不得误伤只读工具");
    }

    /// <summary>审计 reason 必须承载 reason_code 与命中的规则原文（S-3 可观测）。</summary>
    [Fact]
    public async Task DeniedPolicy_ShouldAuditReasonCodeAndMatchedRule()
    {
        var records = new List<ToolExecutionAuditRecord>();
        var binding = CreateBinding(
            Policy(domainsDeny: ["payroll"]), new CapturingAuditSink(records));

        await binding.ExecuteAsync(
            Definition("payroll.query_payment_details"),
            Args(),
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("ok")));

        records.Should().ContainSingle();
        records[0].Reason.Should().Contain(ToolErrorSubtype.DomainNotAllowed);
        records[0].Reason.Should().Contain("payroll", "审计必须回显命中的规则原文，便于企业排障");
    }

    // ────────── ② 装配期 fail-closed ──────────

    /// <summary>方案 §6.2 的示例配置必须全部合法（防"文档给得出来、代码收不下"）。</summary>
    [Fact]
    public void PlanExamplePatterns_ShouldAllBeLegal()
    {
        var options = new FeishuAgentOptions
        {
            Instructions = "test",
            ToolPolicy = new FeishuToolPolicyOptions
            {
                Allow = ["bitable.*", "im.get_*", "calendar.*"],
                Deny = ["*.delete_*", "drive.transfer_owner", "payroll.*"],
                Domains = new FeishuToolPolicyDomainOptions
                {
                    Allow = ["bitable", "im", "calendar"],
                    Deny = ["payroll", "performance"],
                },
            },
        };

        var act = options.Validate;

        act.Should().NotThrow("方案 §6.2 给出的示例配置必须开箱合法");
    }

    /// <summary>非法 glob 必须在装配期被拒（拼错一个字符 ⇒ 策略静默失效 的形态）。</summary>
    [Theory]
    [InlineData("Bitable.*")]      // 大写
    [InlineData("bitable..list")]  // 空段
    [InlineData(".bitable")]       // 前导空段
    [InlineData("bitable.")]       // 尾随空段
    [InlineData("bitable.**")]     // 连续星号（与单星同义，保留会误导）
    [InlineData("bitable.*-x")]    // 段内非法字符
    [InlineData("")]               // 空串
    public void IllegalGlob_ShouldFailClosed_AtValidate(string pattern)
    {
        var options = new FeishuAgentOptions
        {
            Instructions = "test",
            ToolPolicy = new FeishuToolPolicyOptions { Deny = [pattern] },
        };

        var act = options.Validate;

        act.Should().Throw<InvalidOperationException>(
            $"非法模式 '{pattern}' 必须在装配期被拒——静默不命中会让宿主以为策略已生效");
    }

    /// <summary>域轴写成多段模式（<c>payroll.*</c>）必须被拒：它在域轴上恒不命中（静默失效）。</summary>
    [Fact]
    public void DomainPatternWithDot_ShouldFailClosed_AtValidate()
    {
        var options = new FeishuAgentOptions
        {
            Instructions = "test",
            ToolPolicy = new FeishuToolPolicyOptions
            {
                Domains = new FeishuToolPolicyDomainOptions { Deny = ["payroll.*"] },
            },
        };

        var act = options.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*单段*");
    }

    /// <summary>策略未配置时 <c>IsConfigured</c> 为假（默认值即"不生效"）。</summary>
    [Fact]
    public void DefaultPolicy_ShouldNotBeConfigured()
    {
        new FeishuToolPolicyOptions().IsConfigured.Should().BeFalse();
        new FeishuAgentOptions().ToolPolicy.IsConfigured.Should().BeFalse();
    }

    // ────────── ③ glob 匹配器语义（纯函数穷举） ──────────

    /// <summary>匹配语义用例表（每条都是"宿主会真的这么写"的形态）。</summary>
    [Theory]
    [InlineData("bitable.*", "bitable.list_tables", true)]
    [InlineData("bitable.*", "bitable", false)]
    [InlineData("bitable.*", "bitable.list_tables.extra", false)]
    [InlineData("im.get_*", "im.get_history_messages", true)]
    [InlineData("im.get_*", "im.send_message", false)]
    [InlineData("calendar.*", "calendar.list_events", true)]
    [InlineData("*.delete_*", "bitable.delete_record", true)]
    [InlineData("*.delete_*", "im.delete_chat", true)]
    [InlineData("*.delete_*", "docx.delete_blocks", true)]
    [InlineData("*.delete_*", "docx.get_document_blocks", false)]
    [InlineData("drive.transfer_owner", "drive.transfer_owner", true)]
    [InlineData("drive.transfer_owner", "drive.transfer_owner_x", false)]
    [InlineData("*", "anything.at_all", true)]
    [InlineData("*", "payroll.query_payment_details", true)]
    public void GlobMatching_ShouldFollowTheClosedVocabulary(
        string pattern, string toolName, bool expected)
    {
        ToolNamePattern.IsLegal(pattern).Should().BeTrue($"'{pattern}' 是合法模式（用例表前提）");
        ToolNamePattern.IsMatch(pattern, toolName).Should().Be(
            expected, $"模式 '{pattern}' 对工具 '{toolName}' 的命中结果必须确定");
    }

    /// <summary>非法模式在匹配时返回不命中（不抛）——运行期不得因配置笔误变成异常。</summary>
    [Fact]
    public void IllegalPattern_ShouldNeverMatch_AndNeverThrow()
    {
        ToolNamePattern.IsLegal("Bitable.*").Should().BeFalse();

        var act = () => ToolNamePattern.IsMatch("Bitable.*", "bitable.list_tables");

        act.Should().NotThrow("匹配器是热路径纯函数——不得因配置笔误抛异常");
        ToolNamePattern.IsMatch("Bitable.*", "bitable.list_tables").Should().BeFalse();
    }

    // ────────── ④ reason_code 产出点自证 ──────────

    /// <summary>
    /// <c>domain_not_allowed</c> 与 <c>write_not_allowed</c> 必须各有产出点
    /// （幽灵 reason_code 与幽灵 subtype 同罪：承诺了一条走不到的分支）。
    /// </summary>
    [Fact]
    public void NewReasonCodes_ShouldHaveProducers()
    {
        var sources = Directory
            .EnumerateFiles(
                Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .ToArray();

        sources.Should().NotBeEmpty("扫描面不得为空（否则本守卫恒绿）");
        var combined = string.Join('\n', sources);

        combined.Should().Contain("ToolErrorSubtype.DomainNotAllowed", "domain_not_allowed 必须有产出点");
        combined.Should().Contain("ToolErrorSubtype.WriteNotAllowed", "write_not_allowed 必须有产出点");
    }

    // ────────── 夹具 ──────────

    private static FeishuAgentOptions Policy(
        string[]? allow = null,
        string[]? deny = null,
        string[]? domainsAllow = null,
        string[]? domainsDeny = null)
        => new()
        {
            Instructions = "test",
            EnforceToolAuthorization = false,
            ToolPolicy = new FeishuToolPolicyOptions
            {
                Allow = allow ?? [],
                Deny = deny ?? [],
                Domains = new FeishuToolPolicyDomainOptions
                {
                    Allow = domainsAllow ?? [],
                    Deny = domainsDeny ?? [],
                },
            },
        };

    private static FeishuToolBinding CreateBinding(
        FeishuAgentOptions options, IToolExecutionAuditSink? auditSink = null)
        => new(new NoopScopeFactory(), Options.Create(options), auditSink: auditSink);

    private static FeishuToolDefinition Definition(
        string name,
        bool isWrite = false,
        FeishuToolRisk risk = FeishuToolRisk.Read)
        => new(
            name,
            "测试工具",
            ["test:read"],
            isWrite,
            risk,
            "tenant",
            (_, _, _) => Task.FromResult(ToolResult.FromText("downstream")));

    private static IReadOnlyDictionary<string, object?> Args()
        => new Dictionary<string, object?> { ["app_token"] = "bascnXxx" };

    private sealed class NoopScopeFactory : IFeishuAppContextScopeFactory
    {
        public IDisposable BeginScope(string appKey) => new NoopScope();

        private sealed class NoopScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class CapturingAuditSink(List<ToolExecutionAuditRecord> records) : IToolExecutionAuditSink
    {
        public Task WriteAsync(ToolExecutionAuditRecord record, CancellationToken cancellationToken = default)
        {
            records.Add(record);
            return Task.CompletedTask;
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
