// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// WP5 §5.3（R4 方案中<b>最危险的一处</b>）：user 身份工具执行期的
/// <see cref="IFeishuCurrentUserContext"/>（AsyncLocal）<b>设置 / 清理成对</b>断言。
/// </summary>
/// <remarks>
/// <para>
/// 为什么必须有用例：<c>IFeishuCurrentUserContext</c> 是 Singleton + AsyncLocal，
/// <c>UserId</c> 被源生成器用作<b>用户令牌缓存查找键</b>。泄漏会让同一异步流内的后续请求
/// 误用上一个人的令牌（跨用户数据越权）。
/// </para>
/// <para>
/// 三条负例（R4 §5.3 验收 + R-5）：① tenant 路径不得触碰该上下文；② 上下文缺 UserId → 拒绝且不设置；
/// ③ 宿主未注册该上下文 → 拒绝（不得静默降级为 tenant 令牌）。
/// </para>
/// </remarks>
public class UserIdentityContextBoundaryTests
{
    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();
    private readonly List<string> _callLog = [];

    /// <summary>
    /// 构造执行链：<b>显式放行 user 身份</b>——D-1 拍板 ⓑ（默认 <c>["tenant"]</c> 保留 + 装配期 fail-fast），
    /// 故宿主启用 user 工具时必须在 <c>AllowedIdentities</c> 内放行 <c>user</c>；
    /// 默认值下的拒绝语义由 <see cref="DefaultAllowedIdentities_ShouldDenyUserIdentityTool"/> 单独锁定。
    /// </summary>
    private FeishuToolBinding CreateBinding(IFeishuCurrentUserContext? currentUserContext)
        => new(
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test", AllowedIdentities = ["tenant", "user"] }),
            currentUserContext: currentUserContext);

    private Mock<IFeishuCurrentUserContext> CreateUserContext()
    {
        var context = new Mock<IFeishuCurrentUserContext>();
        context
            .Setup(c => c.SetUser(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Callback((string _, string? __, string? ___, string? ____) => _callLog.Add("set-user"));
        context
            .Setup(c => c.Clear())
            .Callback(() => _callLog.Add("clear-user"));
        return context;
    }

    private static FeishuToolDefinition Definition(string identity, string name = "test.tool")
        => new(
            name,
            "测试工具",
            ["test:read"],
            false,
            FeishuToolRisk.Read,
            identity,
            (_, _, _) => Task.FromResult(FeishuToolResult.FromText("downstream-result")));

    private static IReadOnlyDictionary<string, object?> Args()
        => new Dictionary<string, object?> { ["app_token"] = "bascnXxx" };

    [Fact]
    public async Task UserIdentityTool_ShouldSetAndClearCurrentUserContext_WithUserIdAsTokenLookupKey()
    {
        var userContext = CreateUserContext();
        var binding = CreateBinding(userContext.Object);

        var result = await binding.ExecuteAsync(
            Definition("user", "task.list_my_tasks"),
            Args(),
            new FeishuToolContext("appA", UserId: "ou_1"),
            _ =>
            {
                _callLog.Add("downstream");
                return Task.FromResult(FeishuToolResult.FromText("ok"));
            });

        result.ToString().Should().Be("ok");
        _callLog.Should().Equal(
            ["set-user", "downstream", "clear-user"],
            "用户上下文必须在调用下游之前设置、在 finally 中清理（AsyncLocal 泄漏 = 跨用户令牌误用）");

        userContext.Verify(
            c => c.SetUser("ou_1", null, "ou_1", null),
            Times.Once,
            "openId 与 userId 都取宿主提供的 open_id——UserId 是用户令牌的缓存查找键，二者必须一致");
        userContext.Verify(c => c.Clear(), Times.Once, "执行后成对清理");
    }

    [Fact]
    public async Task TenantIdentityTool_ShouldNeverTouchCurrentUserContext()
    {
        var userContext = CreateUserContext();
        var binding = CreateBinding(userContext.Object);

        await binding.ExecuteAsync(
            Definition("tenant", "bitable.list_tables"),
            Args(),
            new FeishuToolContext("appA", UserId: "ou_1"),
            _ => Task.FromResult(FeishuToolResult.FromText("ok")));

        userContext.Verify(
            c => c.SetUser(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never,
            "tenant 身份路径不得写入用户上下文（否则同一异步流内会残留跨身份状态）");
        userContext.Verify(c => c.Clear(), Times.Never);
        _callLog.Should().NotContain("set-user");
    }

    [Fact]
    public async Task UserIdentityTool_WithoutUserId_ShouldDeny_WithoutSettingContext()
    {
        var userContext = CreateUserContext();
        var binding = CreateBinding(userContext.Object);
        var downstreamCalled = false;

        var result = await binding.ExecuteAsync(
            Definition("user", "task.list_my_tasks"),
            Args(),
            new FeishuToolContext("appA"),
            _ =>
            {
                downstreamCalled = true;
                return Task.FromResult(FeishuToolResult.FromText("ok"));
            });

        downstreamCalled.Should().BeFalse();
        result.ToString().Should().Contain("invalid_args", "缺当前用户是参数问题（宿主未提供身份），不是授权拒绝");
        result.ToString().Should().Contain("FeishuToolContext.UserId", "错误文案须指出上下文来源（否则宿主无法定位）");
        userContext.Verify(
            c => c.SetUser(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never);
        _callLog.Should().NotContain("clear-user", "未设置就不得清理（避免误清同一异步流内的既有上下文）");
    }

    /// <summary>
    /// D-1 ⓑ 的默认值语义锁定：<c>AllowedIdentities</c> 缺省 <c>["tenant"]</c> 时 user 身份工具被
    /// <b>策略轴</b>拒绝（<c>policy_denied: identity_mismatch</c>）——而装配层会先一步 fail-fast，
    /// 两条防线共同保证"宿主不会在不知情的情况下拿到静默拒绝"。
    /// </summary>
    [Fact]
    public async Task DefaultAllowedIdentities_ShouldDenyUserIdentityTool()
    {
        var binding = new FeishuToolBinding(
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }));
        var downstreamCalled = false;

        var result = await binding.ExecuteAsync(
            Definition("user", "task.list_my_tasks"),
            Args(),
            new FeishuToolContext("appA", UserId: "ou_1"),
            _ =>
            {
                downstreamCalled = true;
                return Task.FromResult(FeishuToolResult.FromText("ok"));
            });

        downstreamCalled.Should().BeFalse();
        result.ToString().Should().Contain("policy_denied: identity_mismatch");
        result.ToString().Should().Contain("AllowedIdentities", "拒绝文案必须指向可修配置（否则宿主无法定位）");
    }

    [Fact]
    public async Task UserIdentityTool_WithoutRegisteredContext_ShouldDeny()
    {
        var binding = CreateBinding(currentUserContext: null);
        var downstreamCalled = false;

        var result = await binding.ExecuteAsync(
            Definition("user", "task.list_my_tasks"),
            Args(),
            new FeishuToolContext("appA", UserId: "ou_1"),
            _ =>
            {
                downstreamCalled = true;
                return Task.FromResult(FeishuToolResult.FromText("ok"));
            });

        downstreamCalled.Should().BeFalse();
        result.ToString().Should().Contain("authorization_denied");
        result.ToString().Should().Contain("IFeishuCurrentUserContext", "宿主装配缺失必须可读可修");
    }
}
