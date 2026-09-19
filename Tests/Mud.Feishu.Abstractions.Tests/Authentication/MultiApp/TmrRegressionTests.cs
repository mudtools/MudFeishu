// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.HttpUtils;

namespace Mud.Feishu.Abstractions.Tests.Authentication.MultiApp;

/// <summary>
/// TMR 系列修复回归测试：
/// <list type="bullet">
/// <item>TMR-P1-4（F4）：StartAsync 预热循环异常隔离（T8）。</item>
/// <item>TMR-P1-5（F5）：AddApp IsDefault 唯一性告警与默认键确定性（T9）。</item>
/// <item>TMR-P2-12（F12）：用户令牌恢复阈值与 D9 对齐（T17）。</item>
/// <item>TMR-P2-13（F13）：Dispose 原子化——并发 Dispose 恰好一次（T18）。</item>
/// </list>
/// </summary>
public class TmrRegressionTests
{
    // ============================================================
    // TMR-P1-4（F4）：StartAsync WarmUp——默认应用解析失败不阻断宿主启动（T8）
    // ============================================================

    [Fact]
    public async Task StartAsync_WarmUp_WhenDefaultConfigThrows_ShouldNotThrow()
    {
        // Arrange
        var appManagerMock = new Mock<IFeishuAppManager>();
        appManagerMock.Setup(x => x.GetDefaultApp())
            .Throws(new InvalidOperationException("默认应用初始化失败"));
        appManagerMock.Setup(x => x.DefaultConfig)
            .Throws(new InvalidOperationException("默认应用初始化失败"));
        appManagerMock.Setup(x => x.ConfiguredAppKeys)
            .Returns(new[] { "default", "hr-app" });
        appManagerMock.Setup(x => x.GetApp("hr-app"))
            .Returns(CreateAppContextMock("hr-app").Object);

        var refreshServiceMock = new Mock<ITokenRefreshBackgroundService>();
        var service = new FeishuTokenRegistrationService(
            appManagerMock.Object,
            refreshServiceMock.Object,
            NullLogger<FeishuTokenRegistrationService>.Instance,
            Options.Create(new FeishuAppOptions { WarmUpAllAppsOnStartup = true }));

        // Act + Assert：修复前 DefaultConfig 异常传播出 StartAsync 会阻断宿主启动。
        var act = async () => await service.StartAsync();
        await act.Should().NotThrowAsync();

        // Assert：非默认应用仍被预热（退化为全量预热，默认应用不再跳过）。
        refreshServiceMock.Verify(
            x => x.RegisterTokenManager(It.IsAny<ITokenManager>(), It.IsAny<string?>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_WarmUp_ShouldSkipDefaultApp_ByResolvedKey()
    {
        // Arrange：默认应用正常解析，非默认应用预热成功。
        var defaultAppContext = CreateAppContextMock("default");
        var appManagerMock = new Mock<IFeishuAppManager>();
        appManagerMock.Setup(x => x.GetDefaultApp()).Returns(defaultAppContext.Object);
        appManagerMock.Setup(x => x.DefaultConfig).Returns(defaultAppContext.Object.Config);
        appManagerMock.Setup(x => x.ConfiguredAppKeys)
            .Returns(new[] { "default", "hr-app" });
        appManagerMock.Setup(x => x.GetApp("hr-app"))
            .Returns(CreateAppContextMock("hr-app").Object);

        var refreshServiceMock = new Mock<ITokenRefreshBackgroundService>();
        var service = new FeishuTokenRegistrationService(
            appManagerMock.Object,
            refreshServiceMock.Object,
            NullLogger<FeishuTokenRegistrationService>.Instance,
            Options.Create(new FeishuAppOptions { WarmUpAllAppsOnStartup = true }));

        // Act
        await service.StartAsync();

        // Assert：默认应用键解析一次后复用（循环确定性），默认应用跳过、hr-app 预热。
        appManagerMock.Verify(x => x.DefaultConfig, Times.Once);
        refreshServiceMock.Verify(
            x => x.RegisterTokenManager(It.IsAny<ITokenManager>(), "tenant:hr-app"),
            Times.Once);
    }

    // ============================================================
    // TMR-P1-5（F5）：AddApp IsDefault 唯一性守卫（T9）
    // ============================================================

    [Fact]
    public void AddApp_WhenAnotherDefaultExists_ShouldOverrideDefaultKey()
    {
        // Arrange
        var services = new ServiceCollection();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());
        services.AddSingleton(httpClientFactoryMock.Object);
        services.AddSingleton<IFeishuAuthentication>(new Mock<IFeishuAuthentication>().Object);
        var authFactoryMock = new Mock<IFeishuAuthenticationFactory>();
        authFactoryMock
            .Setup(x => x.Create(It.IsAny<string>()))
            .Returns(new Mock<IFeishuAuthentication>().Object);
        services.AddSingleton(authFactoryMock.Object);
        services.AddSingleton(_ => HttpClientExtensions.GetDefaultJsonSerializerOptions());
        services.AddLogging();
        services.AddMemoryCache();
        services.AddTransient<IHttpRequestExecutor>(sp => new Mock<IHttpRequestExecutor>().Object);
        services.TryAddSingleton<IFeishuTokenStoreFactory, PerAppFeishuTokenStoreFactory>();
        services.TryAddSingleton<IFeishuTokenManagerFactory, DefaultFeishuTokenManagerFactory>();

        services.AddFeishuApp(new List<FeishuAppConfig>
        {
            new() { AppKey = "default", AppId = "cli_a1b2c3d4e5f6g7h8", AppSecret = "secret_default_0123456789", IsDefault = true },
            new() { AppKey = "hr-app", AppId = "cli_h1i2j3k4l5m6n7o8", AppSecret = "secret_hr_0123456789012345", IsDefault = false }
        });
        using var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<FeishuAppManager>();

        // Act：运行时 AddApp 标记 IsDefault=true（后到者胜出，不硬失败）。
        var act = () => appManager.AddApp(new FeishuAppConfig
        {
            AppKey = "runtime-app",
            AppId = "cli_r1u2n3t4i5m6e7a8",
            AppSecret = "secret_runtime_01234567",
            IsDefault = true
        });

        // Assert：不抛异常；默认应用确定性切到 runtime-app（修复前静默漂移不可观测）。
        act.Should().NotThrow();
        appManager.DefaultAppKey.Should().Be("runtime-app");
        appManager.GetDefaultApp().Config.AppKey.Should().Be("runtime-app");
    }

    // ============================================================
    // TMR-P2-13（F13）：FeishuAppContext 并发 Dispose 恰好一次（T18）
    // ============================================================

    [Fact]
    public void Dispose_Concurrent_ShouldDisposeManagersExactlyOnce()
    {
        // Arrange
        var tenantMock = new Mock<ITenantTokenManager>();
        var appMock = new Mock<IAppTokenManager>();
        var userMock = new Mock<IFeishuUserTokenManager>();
        var authMock = new Mock<IFeishuAuthentication>();
        var httpClientMock = new Mock<IEnhancedHttpClient>();

        var context = new FeishuAppContext(
            new FeishuAppConfig { AppKey = "default", AppId = "cli_default", AppSecret = "secret_default" },
            tenantMock.Object,
            appMock.Object,
            userMock.Object,
            authMock.Object,
            httpClientMock.Object);

        // Act：并发 Dispose（模拟退休队列 Sweep 与容器关闭 Flush 重叠）。
        Parallel.For(0, 16, _ => context.Dispose());

        // Assert：各管理器恰好释放一次（Interlocked.Exchange 收敛）。
        tenantMock.Verify(x => x.Dispose(), Times.Once);
        appMock.Verify(x => x.Dispose(), Times.Once);
        userMock.Verify(x => x.Dispose(), Times.Once);
    }

    private static Mock<IFeishuAppContext> CreateAppContextMock(string appKey)
    {
        var contextMock = new Mock<IFeishuAppContext>();
        contextMock.SetupGet(x => x.Config).Returns(new FeishuAppConfig
        {
            AppKey = appKey,
            AppId = $"cli_{appKey}",
            AppSecret = $"secret_{appKey}"
        });

        var tenantMock = new Mock<ITenantTokenManager>();
        tenantMock.SetupGet(x => x.SupportsBackgroundRefresh).Returns(true);
        contextMock.SetupGet(x => x.TenantTokenManager).Returns(tenantMock.Object);

        var appTokenMock = new Mock<IAppTokenManager>();
        appTokenMock.SetupGet(x => x.SupportsBackgroundRefresh).Returns(true);
        contextMock.SetupGet(x => x.AppTokenManager).Returns(appTokenMock.Object);

        contextMock.SetupGet(x => x.UserTokenManager).Returns(new Mock<IFeishuUserTokenManager>().Object);
        return contextMock;
    }
}

/// <summary>
/// TMR-P2-12（F12）用户令牌恢复阈值回归测试 + TMR-P2-11（F11）日志脱敏回归测试。
/// </summary>
public class UserTokenManagerTmrTests
{
    private const string TokenTypeKey = "UserAccessToken:test-app";

    private static (UserTokenManager Manager, Mock<IUserTokenStore> StoreMock, CapturingLogger Logger)
        CreateManager(FeishuAppConfig? config = null)
    {
        var storeMock = new Mock<IUserTokenStore>();
        var logger = new CapturingLogger();
        var effectiveConfig = config ?? new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test",
            AppSecret = "secret_test",
            TokenRefreshThreshold = 300
        };

        var manager = new UserTokenManager(
            currentUserContext: null,
            authenticationApi: new Mock<IFeishuAuthentication>().Object,
            options: Options.Create(effectiveConfig),
            logger: logger,
            userTokenStore: storeMock.Object);

        return (manager, storeMock, logger);
    }

    // ============================================================
    // TMR-P2-12（F12）：恢复阈值与 D9 对齐（T17）
    // ============================================================

    [Fact]
    public async Task GetTokenInfoAsync_ShouldSkipRestoredToken_NearRefreshThreshold()
    {
        // Arrange：access token 距过期仅 100s < TokenRefreshThreshold(300s)——恢复结果应被弃用，
        // 避免"恢复命中 → 组件判临近过期 → 立即再刷新"的自循环（D9 同源不变式）。
        var (manager, storeMock, _) = CreateManager();
        var expireAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 100_000;
        storeMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), TokenTypeKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TokenStoreHelper.EncodeStoredToken("near-expiry-token", expireAt));
        storeMock
            .Setup(x => x.GetRefreshTokenAsync(It.IsAny<string>(), TokenTypeKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await manager.GetTokenInfoAsync("ou_user");

        // Assert
        result.Should().BeNull("临近 TokenRefreshThreshold 的恢复结果应直接弃用");
    }

    [Fact]
    public async Task GetTokenInfoAsync_ShouldRestoreToken_FarFromRefreshThreshold()
    {
        // Arrange：access token 距过期 3600s ≫ 阈值 300s——正常恢复。
        var (manager, storeMock, _) = CreateManager();
        var expireAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3_600_000;
        storeMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), TokenTypeKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TokenStoreHelper.EncodeStoredToken("valid-token", expireAt));
        storeMock
            .Setup(x => x.GetRefreshTokenAsync(It.IsAny<string>(), TokenTypeKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await manager.GetTokenInfoAsync("ou_user");

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("valid-token");
    }

    // ============================================================
    // TMR-P2-11（F11）：用户标识日志脱敏（T16）
    // ============================================================

    [Fact]
    public async Task GetTokenInfoAsync_ShouldNotLogPlaintextUserId()
    {
        // Arrange：恢复命中路径会以 Debug 记录 "Restored user token ... userId: {UserId}"。
        var (manager, storeMock, logger) = CreateManager();
        const string plaintextUserId = "ou_averylongplaintextuserid123456";
        var expireAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3_600_000;
        storeMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), TokenTypeKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TokenStoreHelper.EncodeStoredToken("valid-token", expireAt));
        storeMock
            .Setup(x => x.GetRefreshTokenAsync(It.IsAny<string>(), TokenTypeKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        await manager.GetTokenInfoAsync(plaintextUserId);

        // Assert：日志不得含明文 userId（经 MaskSensitiveData 脱敏）。
        var formatted = logger.GetFormattedMessages();
        formatted.Should().NotBeEmpty("恢复路径应产生日志");
        formatted.Should().NotContain(m => m.Contains(plaintextUserId, StringComparison.Ordinal),
            "用户标识必须脱敏后才能入日志");
    }

    /// <summary>捕获格式化日志消息的测试用 ILogger。</summary>
    private sealed class CapturingLogger : ILogger<UserTokenManager>
    {
        private readonly List<string> _messages = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (_messages)
            {
                _messages.Add(formatter(state, exception));
            }
        }

        public List<string> GetFormattedMessages()
        {
            lock (_messages)
            {
                return _messages.ToList();
            }
        }
    }
}
