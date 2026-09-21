// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.HttpUtils;

namespace Mud.Feishu.Abstractions.Tests.Authentication.MultiApp;

/// <summary>
/// TMR-P0-1（F1）桥接转发代理回归测试 + TMR-P2-7（F7）Resolver 三态区分测试。
/// </summary>
/// <remarks>
/// F1 核心回归：桥接从「实例桥接」改为「解析桥接」后，<c>SetDefaultApp</c> / 热更新
/// 对已解析的 DI 单例消费者立即生效（凭据串号回归保护）。
/// </remarks>
public class TokenManagerBridgeProxyTests
{
    private const string DefaultKey = "default";
    private const string HrKey = "hr-app";

    private readonly Dictionary<string, Mock<IFeishuAuthentication>> _authMocks = new();

    private ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());
        services.AddSingleton(httpClientFactoryMock.Object);

        services.AddSingleton<IFeishuAuthentication>(new Mock<IFeishuAuthentication>().Object);

        // TMR-P0-1：per-app 认证 mock——按 appKey 跟踪调用量，用于断言桥接"现取"的语义。
        var authFactoryMock = new Mock<IFeishuAuthenticationFactory>();
        authFactoryMock
            .Setup(x => x.Create(It.IsAny<string>()))
            .Returns((string appKey) => GetOrCreateAuthMock(appKey).Object);
        services.AddSingleton(authFactoryMock.Object);

        services.AddSingleton(_ => HttpClientExtensions.GetDefaultJsonSerializerOptions());
        services.AddLogging();
        services.AddMemoryCache();
        services.AddTransient<IHttpRequestExecutor>(sp => new Mock<IHttpRequestExecutor>().Object);
        services.TryAddSingleton<IFeishuTokenStoreFactory, PerAppFeishuTokenStoreFactory>();
        services.TryAddSingleton<IFeishuTokenManagerFactory, DefaultFeishuTokenManagerFactory>();

        return services;
    }

    private Mock<IFeishuAuthentication> GetOrCreateAuthMock(string appKey)
    {
        if (_authMocks.TryGetValue(appKey, out var existing))
            return existing;

        var authMock = new Mock<IFeishuAuthentication>();
        authMock
            .Setup(x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantAppCredentialResult
            {
                Code = 0,
                Msg = "success",
                Expire = 7200,
                TenantAccessToken = $"token-{appKey}"
            });
        _authMocks[appKey] = authMock;
        return authMock;
    }

    private static ServiceProvider BuildProvider(ServiceCollection services, List<FeishuAppConfig> configs)
    {
        services.AddFeishuApp(configs);
        return services.BuildServiceProvider();
    }

    private static List<FeishuAppConfig> CreateConfigs() => new()
    {
        new FeishuAppConfig { AppKey = DefaultKey, AppId = "cli_a1b2c3d4e5f6g7h8", AppSecret = "secret_default_0123456789", IsDefault = true },
        new FeishuAppConfig { AppKey = HrKey, AppId = "cli_h1i2j3k4l5m6n7o8", AppSecret = "secret_hr_0123456789012345", IsDefault = false }
    };

    // ============================================================
    // TMR-P0-1（F1）：SetDefaultApp 后桥接现取新默认应用（T1/T3）
    // ============================================================

    [Fact]
    public async Task BridgedTenantTokenManager_ShouldResolveCurrentDefaultApp_AfterSetDefaultApp()
    {
        // Arrange
        var provider = BuildProvider(CreateServiceCollection(), CreateConfigs());
        using (provider)
        {
            var bridge = provider.GetRequiredService<ITenantTokenManager>();
            var appManager = provider.GetRequiredService<IFeishuAppManager>();

            // Act + Assert：切换前走默认应用
            await bridge.GetTokenAsync();
            GetOrCreateAuthMock(DefaultKey).Verify(
                x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
                Times.Once);
            GetOrCreateAuthMock(HrKey).Verify(
                x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
                Times.Never);

            // Act：切换默认应用
            appManager.SetDefaultApp(HrKey);

            // Assert：桥接（DI 单例，代理实例不变）必须现取新默认应用——
            // 修复前（实例桥接）此处会继续调用旧默认应用的认证接口（跨应用凭据串号）。
            await bridge.GetTokenAsync();
            GetOrCreateAuthMock(DefaultKey).Verify(
                x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
                Times.Once);
            GetOrCreateAuthMock(HrKey).Verify(
                x => x.GetTenantAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    // ============================================================
    // TMR-P0-1（F1）：桥接注册为单例（解析两次同一实例）且 Dispose 为 no-op（T2/T3 辅助）
    // ============================================================

    [Fact]
    public void BridgedTokenManager_ShouldBeSingleton_AndDisposeNoOp()
    {
        // Arrange
        var provider = BuildProvider(CreateServiceCollection(), CreateConfigs());
        using (provider)
        {
            var bridge1 = provider.GetRequiredService<ITenantTokenManager>();
            var bridge2 = provider.GetRequiredService<ITenantTokenManager>();

            // Assert：代理为 DI 单例——消费方持有的引用稳定，切换默认应用时仅转义目标变化。
            bridge1.Should().BeSameAs(bridge2);

            // Assert：代理 Dispose 为显式 no-op——不释放任何资源（真实管理器生命周期归退休队列）。
            var act = () => (bridge1 as IDisposable)!.Dispose();
            act.Should().NotThrow();

            // Assert：Dispose 后代理仍可用（现取语义未被破坏）。
            var actGetToken = () => bridge1.GetTokenAsync();
            actGetToken.Should().NotThrowAsync();
        }
    }

    // ============================================================
    // TMR-P2-7（F7）：Resolver Try* 三态区分（T13）
    // ============================================================

    [Fact]
    public void TryGetTenantTokenManager_ShouldReturnNull_WhenDefaultAppInitFails()
    {
        // Arrange：DefaultAppKey 非空，但默认应用初始化失败
        var appManagerMock = new Mock<IFeishuAppManager>();
        appManagerMock.SetupGet(x => x.DefaultAppKey).Returns(DefaultKey);
        appManagerMock.SetupGet(x => x.DefaultTenantTokenManager)
            .Throws(new InvalidOperationException("默认应用初始化失败"));

        var resolver = new FeishuTokenManagerResolver(
            appManagerMock.Object, NullLogger<FeishuTokenManagerResolver>.Instance);

        // Act + Assert：按未命中返回 null（不抛异常），且异常被记录而非静默吞掉。
        resolver.TryGetTenantTokenManager().Should().BeNull();
    }

    [Fact]
    public void TryGetTenantTokenManager_ShouldReturnNull_WithoutTouchingManager_WhenDefaultAppKeyEmpty()
    {
        // Arrange：无默认应用（三态之一：未配置）
        var appManagerMock = new Mock<IFeishuAppManager>();
        appManagerMock.SetupGet(x => x.DefaultAppKey).Returns((string?)null);
        appManagerMock.SetupGet(x => x.DefaultTenantTokenManager)
            .Throws(new InvalidOperationException("不应被触及"));

        var resolver = new FeishuTokenManagerResolver(appManagerMock.Object);

        // Act + Assert
        resolver.TryGetTenantTokenManager().Should().BeNull();
    }

    [Fact]
    public void TryGetTenantTokenManager_ShouldNotSwallowOutOfMemory()
    {
        // Arrange：TMR-P2-7——OOM 不再被空 catch 吞掉（三态区分）。
        var appManagerMock = new Mock<IFeishuAppManager>();
        appManagerMock.SetupGet(x => x.DefaultAppKey).Returns(DefaultKey);
        appManagerMock.SetupGet(x => x.DefaultTenantTokenManager).Throws(new OutOfMemoryException());

        var resolver = new FeishuTokenManagerResolver(appManagerMock.Object);

        // Act + Assert
        var act = () => resolver.TryGetTenantTokenManager();
        act.Should().Throw<OutOfMemoryException>();
    }

    // ============================================================
    // TMR-P1-3（F3）：白名单收窄——FeishuAppRemovedException 不再落入瞬时白名单（T6 反射断言）
    // ============================================================

    [Fact]
    public void IsTransientInitFailure_ShouldExcludeFeishuAppRemovedException()
    {
        // Arrange
        var method = typeof(FeishuAppManager).GetMethod(
            "IsTransientInitFailure",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull("IsTransientInitFailure 是白名单收窄的落点");

        // Act + Assert
        ((bool)method!.Invoke(null, new object[] { new FeishuAppRemovedException(DefaultKey) })!)
            .Should().BeFalse("快照移除是确定性终态，禁止重建 Lazy");
        ((bool)method.Invoke(null, new object[] { new OperationCanceledException() })!)
            .Should().BeFalse();
        ((bool)method.Invoke(null, new object[] { new InvalidOperationException() })!)
            .Should().BeTrue("InvalidOperationException 仍在可重试白名单内（FeishuAppRemovedException 显式排除除外）");
        ((bool)method.Invoke(null, new object[] { new OutOfMemoryException() })!)
            .Should().BeFalse();
    }
}
