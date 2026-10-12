// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.AI.Tools.Tests.Tools.Execution;

/// <summary>
/// T1.2 用户令牌就绪探测器行为用例。
/// </summary>
/// <remarks>
/// <para>
/// 覆盖四态映射、模式开关、异常 fail-open 路径。
/// 使用 Mock 替身注入 <see cref="IFeishuTokenManagerResolver"/> 与 <see cref="IFeishuUserTokenManager"/>。
/// </para>
/// </remarks>
public class UserTokenPrecheckTests
{
    private static FeishuAgentOptions BuildOptions(string mode = UserTokenPrecheckModes.CacheOnly)
        => new() { Instructions = "test", AllowedIdentities = ["tenant", "user"], UserTokenPrecheck = mode };

    private static Mock<IFeishuUserTokenManager> CreateUserTokenManager()
        => new();

    private static Mock<IFeishuTokenManagerResolver> CreateResolver(IFeishuUserTokenManager? userTokenManager)
    {
        var resolver = new Mock<IFeishuTokenManagerResolver>();
        resolver
            .Setup(r => r.TryGetUserTokenManager(It.IsAny<string?>()))
            .Returns(userTokenManager);
        return resolver;
    }

    private static IOptions<FeishuAgentOptions> OptionsWrap(string mode = UserTokenPrecheckModes.CacheOnly)
        => Options.Create(BuildOptions(mode));

    [Fact]
    public async Task Probe_OffMode_ShouldReturnSkipped()
    {
        var precheck = new UserTokenPrecheck(
            CreateResolver(CreateUserTokenManager().Object).Object,
            OptionsWrap(UserTokenPrecheckModes.Off));

        var result = await precheck.ProbeAsync("appA", "ou_1");

        result.Should().Be(UserTokenPrecheckResult.Skipped);
        result.ShouldProceed.Should().BeTrue("Off 模式跳过探测——直接放行（改造前行为）");
    }

    [Fact]
    public async Task Probe_CacheOnly_HasValidToken_ShouldReturnReady()
    {
        var userTokenManager = CreateUserTokenManager();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var precheck = new UserTokenPrecheck(
            CreateResolver(userTokenManager.Object).Object,
            OptionsWrap(UserTokenPrecheckModes.CacheOnly));

        var result = await precheck.ProbeAsync("appA", "ou_1");

        result.Should().Be(UserTokenPrecheckResult.Ready);
        result.IsAuthRequired.Should().BeFalse("令牌就绪——不应短路");
    }

    [Fact]
    public async Task Probe_CacheOnly_NoValidToken_ShouldReturnRefreshable_NotAuthRequired()
    {
        var userTokenManager = CreateUserTokenManager();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var precheck = new UserTokenPrecheck(
            CreateResolver(userTokenManager.Object).Object,
            OptionsWrap(UserTokenPrecheckModes.CacheOnly));

        var result = await precheck.ProbeAsync("appA", "ou_1");

        // CacheOnly 未命中不调 CanRefreshTokenAsync，直接放行（避免 store 读穿透）
        result.Should().Be(UserTokenPrecheckResult.Refreshable);
        result.IsAuthRequired.Should().BeFalse("CacheOnly 未命中不判 AuthRequired——放行由下游 401 兜底");
    }

    [Fact]
    public async Task Probe_Full_HasValidToken_ShouldReturnReady()
    {
        var userTokenManager = CreateUserTokenManager();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var precheck = new UserTokenPrecheck(
            CreateResolver(userTokenManager.Object).Object,
            OptionsWrap(UserTokenPrecheckModes.Full));

        var result = await precheck.ProbeAsync("appA", "ou_1");

        result.Should().Be(UserTokenPrecheckResult.Ready);
        // Full 模式下 HasValidTokenAsync 命中即返回，不调 CanRefreshTokenAsync
        userTokenManager.Verify(
            m => m.CanRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Probe_Full_NoValidToken_CanRefresh_ShouldReturnRefreshable()
    {
        var userTokenManager = CreateUserTokenManager();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userTokenManager
            .Setup(m => m.CanRefreshTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var precheck = new UserTokenPrecheck(
            CreateResolver(userTokenManager.Object).Object,
            OptionsWrap(UserTokenPrecheckModes.Full));

        var result = await precheck.ProbeAsync("appA", "ou_1");

        result.Should().Be(UserTokenPrecheckResult.Refreshable);
        result.IsAuthRequired.Should().BeFalse("refresh token 有效——可续期，放行");
    }

    [Fact]
    public async Task Probe_Full_NoValidToken_CannotRefresh_ShouldReturnAuthRequired()
    {
        var userTokenManager = CreateUserTokenManager();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userTokenManager
            .Setup(m => m.CanRefreshTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var precheck = new UserTokenPrecheck(
            CreateResolver(userTokenManager.Object).Object,
            OptionsWrap(UserTokenPrecheckModes.Full));

        var result = await precheck.ProbeAsync("appA", "ou_1");

        result.Should().Be(UserTokenPrecheckResult.AuthRequired);
        result.IsAuthRequired.Should().BeTrue("无有效令牌且无 refresh token——应短路返回 user_auth_required");
        result.ShouldProceed.Should().BeFalse("AuthRequired 不应放行——零调用下游");
    }

    [Fact]
    public async Task Probe_ResolverNull_ShouldFailOpen_ReturnFromError()
    {
        var precheck = new UserTokenPrecheck(
            resolver: null,
            OptionsWrap(UserTokenPrecheckModes.Full));

        var result = await precheck.ProbeAsync("appA", "ou_1");

        result.Should().Be(UserTokenPrecheckResult.FromError);
        result.ShouldProceed.Should().BeTrue("Resolver 缺席 fail-open 放行——Redis 抖动不得放大为全员拒绝");
        result.IsAuthRequired.Should().BeFalse("fail-open 不产出 AuthRequired");
    }

    [Fact]
    public async Task Probe_UserTokenManagerNull_ShouldFailOpen_ReturnFromError()
    {
        var resolver = new Mock<IFeishuTokenManagerResolver>();
        resolver
            .Setup(r => r.TryGetUserTokenManager(It.IsAny<string?>()))
            .Returns((IFeishuUserTokenManager?)null);

        var precheck = new UserTokenPrecheck(
            resolver.Object,
            OptionsWrap(UserTokenPrecheckModes.Full));

        var result = await precheck.ProbeAsync("appA", "ou_1");

        result.Should().Be(UserTokenPrecheckResult.FromError);
        result.ShouldProceed.Should().BeTrue("应用未初始化/已退休 fail-open 放行");
    }

    [Fact]
    public async Task Probe_HasValidTokenAsyncThrows_ShouldFailOpen_ReturnFromError()
    {
        var userTokenManager = CreateUserTokenManager();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis connection lost"));

        var precheck = new UserTokenPrecheck(
            CreateResolver(userTokenManager.Object).Object,
            OptionsWrap(UserTokenPrecheckModes.Full));

        var result = await precheck.ProbeAsync("appA", "ou_1");

        result.Should().Be(UserTokenPrecheckResult.FromError);
        result.ShouldProceed.Should().BeTrue("探测异常 fail-open 放行——Redis 抖动不得放大为全员 AuthRequired");
    }

    [Fact]
    public async Task Probe_CanRefreshTokenAsyncThrows_ShouldFailOpen_ReturnFromError()
    {
        var userTokenManager = CreateUserTokenManager();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userTokenManager
            .Setup(m => m.CanRefreshTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Store read failure"));

        var precheck = new UserTokenPrecheck(
            CreateResolver(userTokenManager.Object).Object,
            OptionsWrap(UserTokenPrecheckModes.Full));

        var result = await precheck.ProbeAsync("appA", "ou_1");

        result.Should().Be(UserTokenPrecheckResult.FromError);
        result.ShouldProceed.Should().BeTrue("CanRefreshTokenAsync 异常 fail-open 放行");
    }

    [Fact]
    public async Task Probe_CacheOnly_ShouldNotCallCanRefreshTokenAsync()
    {
        var userTokenManager = CreateUserTokenManager();
        userTokenManager
            .Setup(m => m.HasValidTokenAsync("ou_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var precheck = new UserTokenPrecheck(
            CreateResolver(userTokenManager.Object).Object,
            OptionsWrap(UserTokenPrecheckModes.CacheOnly));

        await precheck.ProbeAsync("appA", "ou_1");

        userTokenManager.Verify(
            m => m.CanRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "CacheOnly 模式不调 CanRefreshTokenAsync——避免 store 读穿透");
    }
}
