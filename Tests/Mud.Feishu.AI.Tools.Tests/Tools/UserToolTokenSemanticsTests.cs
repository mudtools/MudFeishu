// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// T1.3 用户令牌语义端到端用例：AuthRequired ⇒ 零调用下游 + 载荷可区分于 forbidden。
/// </summary>
/// <remarks>
/// <para>
/// 验收（方案 §T1 验收行）：启用 user 身份工具且用户无令牌 ⇒ 返回 <c>user_auth_required</c> + 下游 HTTP 调用次数 = 0。
/// </para>
/// <para>
/// 与 <c>forbidden</c>（权限不足 ⇒ 放弃/换只读方案）的差异：user_auth_required 是「用户本人未完成飞书授权」，
/// 模型应转告用户去授权——不是换工具或放弃。
/// </para>
/// </remarks>
public class UserToolTokenSemanticsTests
{
    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();
    private readonly List<string> _callLog = [];

    public UserToolTokenSemanticsTests()
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Callback((string appKey) => _callLog.Add($"scope:{appKey}"))
            .Returns(() =>
            {
                var disposed = false;
                return new DisposableAction(() =>
                {
                    if (!disposed)
                    {
                        disposed = true;
                        _callLog.Add("scope-release");
                    }
                });
            });
    }

    private Mock<IFeishuCurrentUserContext> CreateUserContext()
    {
        var context = new Mock<IFeishuCurrentUserContext>();
        context.SetupAllProperties();
        context
            .Setup(c => c.SetUser(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Callback((string _, string? _, string? _, string? _) => _callLog.Add("set-user"));
        context
            .Setup(c => c.Clear())
            .Callback(() => _callLog.Add("clear-user"));
        return context;
    }

    private static Mock<IFeishuTokenManagerResolver> CreateResolver(Mock<IFeishuUserTokenManager> userTokenManager)
    {
        var resolver = new Mock<IFeishuTokenManagerResolver>();
        resolver
            .Setup(r => r.TryGetUserTokenManager(It.IsAny<string?>()))
            .Returns(userTokenManager.Object);
        return resolver;
    }

    private FeishuToolBinding CreateBinding(
        Mock<IFeishuCurrentUserContext> userContext,
        Mock<IFeishuTokenManagerResolver> resolver,
        IFeishuUserAuthorizationLinkProvider? authLinkProvider = null,
        string precheckMode = UserTokenPrecheckModes.Full)
    {
        var options = new FeishuAgentOptions
        {
            Instructions = "test",
            AllowedIdentities = ["tenant", "user"],
            UserTokenPrecheck = precheckMode,
        };
        var precheck = new UserTokenPrecheck(resolver.Object, Options.Create(options));
        return new FeishuToolBinding(
            _scopeFactory.Object,
            Options.Create(options),
            currentUserContext: userContext.Object,
            userTokenPrecheck: precheck,
            authLinkProvider: authLinkProvider);
    }

    private static FeishuToolDefinition UserToolDefinition()
        => new(
            "task.list_my_tasks",
            "列出我的任务",
            ["task:task:readonly"],
            false,
            FeishuToolRisk.Read,
            "user",
            (_, _, _) => Task.FromResult(FeishuToolResult.FromText("downstream-result")));

    private static IReadOnlyDictionary<string, object?> Args()
        => new Dictionary<string, object?> { ["app_token"] = "bascnXxx" };

    [Fact]
    public async Task AuthRequired_ShouldReturnUserAuthRequired_AndZeroDownstreamCalls()
    {
        // Arrange: user has no valid token and cannot refresh
        var userTokenManager = new Mock<IFeishuUserTokenManager>();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userTokenManager
            .Setup(m => m.CanRefreshTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var userContext = CreateUserContext();
        var resolver = CreateResolver(userTokenManager);
        var binding = CreateBinding(userContext, resolver);

        // Act
        var result = await binding.ExecuteAsync(
            UserToolDefinition(),
            Args(),
            new FeishuToolContext("appA", UserId: "ou_1"),
            _ =>
            {
                _callLog.Add("downstream");
                return Task.FromResult(FeishuToolResult.FromText("should-not-reach"));
            });

        // Assert: result contains user_auth_required
        var text = result.ToString();
        text.Should().Contain("user_auth_required",
            "探测 AuthRequired 应产出 user_auth_required subtype——与 forbidden 语义区分");

        // Assert: downstream was never called
        _callLog.Should().NotContain("downstream",
            "AuthRequired ⇒ 零调用下游——探测器短路返回结构化错误");

        // Assert: user context was set and cleared (set-user → clear-user)
        _callLog.Should().Contain("set-user", "SetUser 必须在探测之前调用（令牌缓存查找键依赖它）");
        _callLog.Should().Contain("clear-user", "用户上下文必须在 finally 中清理（防 AsyncLocal 跨用户泄漏）");
    }

    [Fact]
    public async Task AuthRequired_Payload_ShouldBeDistinctFrom_Forbidden()
    {
        // Arrange: user has no valid token and cannot refresh
        var userTokenManager = new Mock<IFeishuUserTokenManager>();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userTokenManager
            .Setup(m => m.CanRefreshTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var userContext = CreateUserContext();
        var resolver = CreateResolver(userTokenManager);
        var binding = CreateBinding(userContext, resolver);

        // Act
        var result = await binding.ExecuteAsync(
            UserToolDefinition(),
            Args(),
            new FeishuToolContext("appA", UserId: "ou_1"),
            _ => Task.FromResult(FeishuToolResult.FromText("should-not-reach")));

        // Assert: payload must NOT contain "forbidden" or "authorization_denied"
        var text = result.ToString();
        text.Should().NotContain("forbidden",
            "user_auth_required 与 forbidden 是不同语义——forbidden 是权限不足（放弃/换只读方案），"
            + "user_auth_required 是用户未授权（转告用户去授权）");
        text.Should().Contain("user_auth_required",
            "载荷必须包含 user_auth_required subtype——模型据此区分动作");
    }

    [Fact]
    public async Task AuthRequired_WithAuthLinkProvider_ShouldIncludeLink()
    {
        // Arrange
        var userTokenManager = new Mock<IFeishuUserTokenManager>();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userTokenManager
            .Setup(m => m.CanRefreshTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var authLinkProvider = new Mock<IFeishuUserAuthorizationLinkProvider>();
        authLinkProvider
            .Setup(p => p.GetAuthorizationLinkAsync("appA", "ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://open.feishu.cn/oauth/auth?app_id=cli_xxx&state=ou_1");

        var userContext = CreateUserContext();
        var resolver = CreateResolver(userTokenManager);
        var binding = CreateBinding(userContext, resolver, authLinkProvider.Object);

        // Act
        var result = await binding.ExecuteAsync(
            UserToolDefinition(),
            Args(),
            new FeishuToolContext("appA", UserId: "ou_1"),
            _ => Task.FromResult(FeishuToolResult.FromText("should-not-reach")));

        // Assert: error message should include the authorization link
        var text = result.ToString();
        text.Should().Contain("https://open.feishu.cn/oauth/auth",
            "授权链接提供者注册后，错误文案应包含授权入口链接");
        text.Should().Contain("user_auth_required");
    }

    [Fact]
    public async Task AuthRequired_WithoutAuthLinkProvider_ShouldHaveTextOnlyGuidance()
    {
        // Arrange: no auth link provider
        var userTokenManager = new Mock<IFeishuUserTokenManager>();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userTokenManager
            .Setup(m => m.CanRefreshTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var userContext = CreateUserContext();
        var resolver = CreateResolver(userTokenManager);
        var binding = CreateBinding(userContext, resolver, authLinkProvider: null);

        // Act
        var result = await binding.ExecuteAsync(
            UserToolDefinition(),
            Args(),
            new FeishuToolContext("appA", UserId: "ou_1"),
            _ => Task.FromResult(FeishuToolResult.FromText("should-not-reach")));

        // Assert: error message should have text guidance without link
        var text = result.ToString();
        text.Should().Contain("完成飞书授权", "软缺席时错误文案应只有文字指引");
        text.Should().NotContain("https://", "未注册授权链接提供者时不应包含 URL");
    }

    [Fact]
    public async Task Ready_ShouldProceedToDownstream()
    {
        // Arrange: user has valid token
        var userTokenManager = new Mock<IFeishuUserTokenManager>();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var userContext = CreateUserContext();
        var resolver = CreateResolver(userTokenManager);
        var binding = CreateBinding(userContext, resolver);

        // Act
        var result = await binding.ExecuteAsync(
            UserToolDefinition(),
            Args(),
            new FeishuToolContext("appA", UserId: "ou_1"),
            _ =>
            {
                _callLog.Add("downstream");
                return Task.FromResult(FeishuToolResult.FromText("downstream-ok"));
            });

        // Assert: downstream was called
        _callLog.Should().Contain("downstream", "令牌就绪应放行——下游被调用");
        result.ToString().Should().Be("downstream-ok");
    }

    [Fact]
    public async Task DryRun_ShouldSkipPrecheck_AndProceedToDownstream()
    {
        // Arrange: even with AuthRequired, dry_run should skip precheck
        var userTokenManager = new Mock<IFeishuUserTokenManager>();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userTokenManager
            .Setup(m => m.CanRefreshTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var userContext = CreateUserContext();
        var resolver = CreateResolver(userTokenManager);
        var binding = CreateBinding(userContext, resolver);
        var argsWithDryRun = new Dictionary<string, object?>
        {
            ["app_token"] = "bascnXxx",
            ["dry_run"] = true,
        };

        // Act
        var result = await binding.ExecuteAsync(
            UserToolDefinition(),
            argsWithDryRun,
            new FeishuToolContext("appA", UserId: "ou_1"),
            _ =>
            {
                _callLog.Add("downstream");
                return Task.FromResult(FeishuToolResult.FromText("dry-run-ok"));
            });

        // Assert: downstream was called despite AuthRequired state
        _callLog.Should().Contain("downstream", "dry_run=true 跳过探测——下游被调用");
        result.ToString().Should().Be("dry-run-ok");
    }

    [Fact]
    public async Task TenantTool_ShouldNotTriggerPrecheck()
    {
        // Arrange: tenant identity tool should not trigger precheck at all
        var userTokenManager = new Mock<IFeishuUserTokenManager>();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var userContext = CreateUserContext();
        var resolver = CreateResolver(userTokenManager);
        var binding = CreateBinding(userContext, resolver);

        var tenantTool = new FeishuToolDefinition(
            "bitable.query_records",
            "查询记录",
            ["bitable:app:readonly"],
            false,
            FeishuToolRisk.Read,
            "tenant",
            (_, _, _) => Task.FromResult(FeishuToolResult.FromText("tenant-ok")));

        // Act
        var result = await binding.ExecuteAsync(
            tenantTool,
            Args(),
            new FeishuToolContext("appA"),
            _ =>
            {
                _callLog.Add("downstream");
                return Task.FromResult(FeishuToolResult.FromText("tenant-ok"));
            });

        // Assert: downstream was called, no precheck for tenant tools
        _callLog.Should().Contain("downstream", "tenant 身份工具不触发用户令牌探测");
        result.ToString().Should().Be("tenant-ok");

        // Verify user token manager was never queried
        userTokenManager.Verify(
            m => m.HasValidTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "tenant 身份工具不应触发用户令牌探测——零触碰");
    }

    private sealed class DisposableAction(Action action) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                action();
        }
    }
}
