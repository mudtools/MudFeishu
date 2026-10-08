// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// <see cref="ConsoleToolAuthorizer"/> 的策略判定（闸 2）：appKey 校验、策略轴、权限面、以及
/// <b>任何路径都不返回 <c>Confirm</c></b>（ADR-03 契约守卫）。
/// </summary>
public class ConsoleToolAuthorizerTests
{
    private static readonly string[] NoScopes = [];

    /// <summary>appKey 与宿主期望不一致时必须拒（多租户隔离禁止默认应用兜底，TMA2-20）。</summary>
    [Fact]
    public async Task AuthorizeAsync_Should_Deny_AppKeyMismatch()
    {
        var (authorizer, _) = TestDoubles.CreateAuthorizer();

        var result = await authorizer.AuthorizeAsync(
            "docx.get_raw_content",
            ["docx:document:readonly"],
            isWrite: false,
            arguments: new Dictionary<string, object?>(),
            context: new FeishuToolContext("other-app"),
            cancellationToken: CancellationToken.None);

        result.Decision.Should().Be(AuthorizationDecision.Denied);
        result.Reason.Should().Contain("appKey");
    }

    /// <summary><c>strict</c> 策略下高风险写工具必须被授权器**独立**拒绝（「批准 ≠ 放行」）。</summary>
    [Fact]
    public async Task AuthorizeAsync_Should_Deny_HighRiskWrite_UnderStrict()
    {
        var (authorizer, state) = TestDoubles.CreateAuthorizer(DocAgentSettings.PolicyStrict);

        state.Current.Should().Be(DocAgentSettings.PolicyStrict);

        var result = await authorizer.AuthorizeAsync(
            "docx.delete_blocks",
            ["docx:document"],
            isWrite: true,
            arguments: new Dictionary<string, object?>(),
            context: TestDoubles.CreateContext(),
            cancellationToken: CancellationToken.None);

        result.Decision.Should().Be(AuthorizationDecision.Denied);
        result.Reason.Should().Contain("strict");
    }

    /// <summary>切到 <c>ask</c> 后同一调用必须放行（现场演示"再拒一次"的可解除性）。</summary>
    [Fact]
    public async Task AuthorizeAsync_Should_Allow_HighRiskWrite_AfterSwitchToAsk()
    {
        var (authorizer, state) = TestDoubles.CreateAuthorizer(DocAgentSettings.PolicyStrict);
        state.Current = DocAgentSettings.PolicyAsk;

        var result = await authorizer.AuthorizeAsync(
            "docx.delete_blocks",
            ["docx:document"],
            isWrite: true,
            arguments: new Dictionary<string, object?>(),
            context: TestDoubles.CreateContext(),
            cancellationToken: CancellationToken.None);

        result.Decision.Should().Be(AuthorizationDecision.Allowed);
    }

    /// <summary><c>readonly</c> 策略下任何写工具都被拒（含普通 write 级）。</summary>
    [Fact]
    public async Task AuthorizeAsync_Should_Deny_AnyWrite_UnderReadonly()
    {
        var (authorizer, _) = TestDoubles.CreateAuthorizer(DocAgentSettings.PolicyReadonly);

        var result = await authorizer.AuthorizeAsync(
            "docx.append_blocks",
            ["docx:document"],
            isWrite: true,
            arguments: new Dictionary<string, object?>(),
            context: TestDoubles.CreateContext(),
            cancellationToken: CancellationToken.None);

        result.Decision.Should().Be(AuthorizationDecision.Denied);
        result.Reason.Should().Contain("readonly");
    }

    /// <summary>未预授权的权限点必须拒——判据是 <c>RequiredScopes</c> 而非工具名硬编码。</summary>
    [Fact]
    public async Task AuthorizeAsync_Should_Deny_UnapprovedScope()
    {
        var (authorizer, _) = TestDoubles.CreateAuthorizer();

        var result = await authorizer.AuthorizeAsync(
            "unscoped.unknown_scope",
            ["nope:not-authorized"],
            isWrite: false,
            arguments: new Dictionary<string, object?>(),
            context: TestDoubles.CreateContext(),
            cancellationToken: CancellationToken.None);

        result.Decision.Should().Be(AuthorizationDecision.Denied);
        result.Reason.Should().Contain("nope:not-authorized");
    }

    /// <summary>只读工具在 <c>strict</c> 下放行（只读面不含高风险判定）。</summary>
    [Fact]
    public async Task AuthorizeAsync_Should_Allow_ReadTool_UnderStrict()
    {
        var (authorizer, _) = TestDoubles.CreateAuthorizer();

        var result = await authorizer.AuthorizeAsync(
            "docx.get_raw_content",
            ["docx:document:readonly"],
            isWrite: false,
            arguments: new Dictionary<string, object?>(),
            context: TestDoubles.CreateContext(),
            cancellationToken: CancellationToken.None);

        result.Decision.Should().Be(AuthorizationDecision.Allowed);
        result.Reason.Should().BeNull();
    }

    /// <summary>
    /// <b>ADR-03 契约守卫</b>：任何可枚举路径都不得返回 <c>NeedsUserConfirmation</c>。
    /// P4-1 之后该职责由 MAF 的 <c>ApprovalRequiredAIFunction</c> 包装承载，授权器返回
    /// <c>Confirm</c> 只会演示一条**实际不可达**的路径。
    /// </summary>
    /// <param name="policy">策略。</param>
    /// <param name="toolName">工具名。</param>
    /// <param name="isWrite">是否写操作。</param>
    /// <param name="appKey">上下文 appKey。</param>
    /// <param name="scopes">权限点。</param>
    [Theory]
    [InlineData(DocAgentSettings.PolicyStrict, "docx.delete_blocks", true, DocAgentSettings.DefaultAppKey, "docx:document")]
    [InlineData(DocAgentSettings.PolicyStrict, "docx.append_blocks", true, DocAgentSettings.DefaultAppKey, "docx:document")]
    [InlineData(DocAgentSettings.PolicyStrict, "docx.get_raw_content", false, DocAgentSettings.DefaultAppKey, "docx:document:readonly")]
    [InlineData(DocAgentSettings.PolicyAsk, "docx.delete_blocks", true, DocAgentSettings.DefaultAppKey, "docx:document")]
    [InlineData(DocAgentSettings.PolicyReadonly, "docx.append_blocks", true, DocAgentSettings.DefaultAppKey, "docx:document")]
    [InlineData(DocAgentSettings.PolicyStrict, "docx.get_raw_content", false, "other-app", "docx:document:readonly")]
    [InlineData(DocAgentSettings.PolicyStrict, "unscoped.unknown_scope", false, DocAgentSettings.DefaultAppKey, "nope:not-authorized")]
    [InlineData(DocAgentSettings.PolicyAsk, "unscoped.unknown_scope", false, DocAgentSettings.DefaultAppKey, "nope:not-authorized")]
    [InlineData(DocAgentSettings.PolicyReadonly, "docx.get_raw_content", false, DocAgentSettings.DefaultAppKey, "docx:document:readonly")]
    public async Task AuthorizeAsync_Should_NeverReturnConfirm(
        string policy,
        string toolName,
        bool isWrite,
        string appKey,
        string scope)
    {
        var (authorizer, _) = TestDoubles.CreateAuthorizer(policy);

        var result = await authorizer.AuthorizeAsync(
            toolName,
            [scope],
            isWrite,
            arguments: new Dictionary<string, object?>(),
            context: new FeishuToolContext(appKey),
            cancellationToken: CancellationToken.None);

        result.Decision.Should().NotBe(
            AuthorizationDecision.NeedsUserConfirmation,
            "授权器只做 Allowed/Denied 策略判定；NeedsUserConfirmation 由 MAF 审批包装承载（ADR-03）");

        result.Decision.Should().BeOneOf(
            AuthorizationDecision.Allowed,
            AuthorizationDecision.Denied);
    }

    /// <summary>
    /// 授权器的风险判据必须来自<b>编译期契约表</b>（而非容器里的工具注册表）。
    /// </summary>
    /// <remarks>
    /// <b>这是一条防回归守卫，不只是事实断言</b>：早期实现把 <c>FeishuToolRegistry</c> 注入授权器，
    /// 形成「注册表 → 注册器 → FeishuToolBinding → 授权器 → 注册表」的运行期循环依赖，
    /// .NET DI 的静态环检测看不见 lambda 工厂里的服务定位调用，表现为<b>启动期无输出卡死</b>。
    /// 本用例锁定"风险事实可脱离 DI 容器取得"这一前提。
    /// </remarks>
    [Fact]
    public void RiskFacts_ShouldComeFromCompileTimeContracts()
    {
        DocAgentToolFacts.RiskOf("docx.delete_blocks").Should().Be(
            FeishuToolRisk.HighRiskWrite,
            "白名单里唯一的高风险写工具（strict 策略据此独立拒绝）");

        DocAgentToolFacts.RiskLiteralOf("docx.delete_blocks", isWrite: true).Should().Be(FeishuToolRiskNames.HighRiskWrite);
        DocAgentToolFacts.RiskLiteralOf("docx.append_blocks", isWrite: true).Should().Be(FeishuToolRiskNames.Write);
        DocAgentToolFacts.RiskLiteralOf("docx.get_raw_content", isWrite: false).Should().Be(FeishuToolRiskNames.Read);

        // 查不到契约时按读写布尔兜底（不得抛错——审计 sink 在拒绝路径上也要工作）。
        DocAgentToolFacts.RiskLiteralOf("not.a.tool", isWrite: true).Should().Be(FeishuToolRiskNames.Write);
        DocAgentToolFacts.RiskLiteralOf("not.a.tool", isWrite: false).Should().Be(FeishuToolRiskNames.Read);
    }

    /// <summary>空权限点集合的工具（元工具）应放行——授权器不得凭"空集"误判。</summary>
    [Fact]
    public async Task AuthorizeAsync_Should_Allow_WhenNoScopesDeclared()
    {
        var (authorizer, _) = TestDoubles.CreateAuthorizer();

        var result = await authorizer.AuthorizeAsync(
            "feishu.capability_lookup",
            NoScopes,
            isWrite: false,
            arguments: new Dictionary<string, object?>(),
            context: TestDoubles.CreateContext(),
            cancellationToken: CancellationToken.None);

        result.Decision.Should().Be(AuthorizationDecision.Allowed);
    }
}
