// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Mud.HttpUtils.Resilience;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// Per-App 弹性策略集成测试。
/// 验证 IAppResiliencePolicyResolver 在 Feishu SDK 中的注册和解析行为。
/// </summary>
public class PerAppResilienceTests
{
    /// <summary>
    /// 构造测试用多应用配置列表
    /// </summary>
    private static List<FeishuAppConfig> CreateMultiAppConfigs() => new()
    {
        new FeishuAppConfig
        {
            AppKey = "default",
            AppId = "cli_default_id_1234567890",
            AppSecret = "default_secret_123456",
            IsDefault = true,
            RetryCount = 3,
            RetryDelayMs = 1000,
            TimeOut = 30
        },
        new FeishuAppConfig
        {
            AppKey = "hr-app",
            AppId = "cli_hr_app_id_1234567890",
            AppSecret = "hr_secret_12345678",
            RetryCount = 5,
            RetryDelayMs = 2000,
            TimeOut = 60,
            CircuitBreakerEnabled = false
        }
    };

    /// <summary>
    /// AddFeishuApp 注册后应能从 DI 容器解析 IAppResiliencePolicyResolver
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldRegisterIAppResiliencePolicyResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddFeishuApp(CreateMultiAppConfigs());
        var provider = services.BuildServiceProvider();

        // Assert
        var resolver = provider.GetService<IAppResiliencePolicyResolver>();
        Assert.NotNull(resolver);
    }

    /// <summary>
    /// IAppResiliencePolicyResolver 应为已注册的应用创建对应的 IResiliencePolicyResolver
    /// </summary>
    [Fact]
    public void ResolveResolver_ShouldReturnResolver_WhenAppKeyExists()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(CreateMultiAppConfigs());
        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IAppResiliencePolicyResolver>();

        // Act
        var defaultResolver = resolver.ResolveResolver("default");
        var hrResolver = resolver.ResolveResolver("hr-app");

        // Assert
        Assert.NotNull(defaultResolver);
        Assert.NotNull(hrResolver);
        Assert.NotSame(defaultResolver, hrResolver);
    }

    /// <summary>
    /// IAppResiliencePolicyResolver 对未知应用键应返回 null（回退到全局解析器）
    /// </summary>
    [Fact]
    public void ResolveResolver_ShouldReturnNull_WhenAppKeyDoesNotExist()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(CreateMultiAppConfigs());
        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IAppResiliencePolicyResolver>();

        // Act
        var result = resolver.ResolveResolver("unknown-app");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// IAppResiliencePolicyResolver 对空 appKey 应返回 null
    /// </summary>
    [Fact]
    public void ResolveResolver_ShouldReturnNull_WhenAppKeyIsEmpty()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(CreateMultiAppConfigs());
        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IAppResiliencePolicyResolver>();

        // Act
        var result = resolver.ResolveResolver("");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 同一 appKey 多次调用应返回缓存的同一实例
    /// </summary>
    [Fact]
    public void ResolveResolver_ShouldReturnSameInstance_WhenCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(CreateMultiAppConfigs());
        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IAppResiliencePolicyResolver>();

        // Act
        var first = resolver.ResolveResolver("default");
        var second = resolver.ResolveResolver("default");

        // Assert
        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    /// <summary>
    /// FeishuAppContext.AppKey 应返回 Config.AppKey
    /// </summary>
    [Fact]
    public void FeishuAppContext_AppKey_ShouldReturnConfigAppKey()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test-app-key",
            AppId = "cli_test_app_id_1234567890",
            AppSecret = "test_secret_12345678"
        };

        var authMock = new Mock<IFeishuAuthentication>();
        var httpMock = new Mock<IEnhancedHttpClient>();
        var optionsMock = new Mock<IOptions<FeishuAppConfig>>();
        optionsMock.Setup(x => x.Value).Returns(config);

        var tenantTokenManager = new TenantTokenManager(
            authMock.Object, optionsMock.Object, new Mock<ILogger<TenantTokenManager>>().Object);
        var appTokenManager = new AppTokenManager(
            authMock.Object, optionsMock.Object, new Mock<ILogger<AppTokenManager>>().Object);
        var userTokenManager = new UserTokenManager(
            new Mock<IFeishuCurrentUserContext>().Object,
            authMock.Object, optionsMock.Object, new Mock<ILogger<UserTokenManager>>().Object);

        var appContext = new FeishuAppContext(
            config, tenantTokenManager, appTokenManager, userTokenManager,
            authMock.Object, httpMock.Object);

        // Act
        var appKey = appContext.AppKey;

        // Assert
        Assert.Equal(config.AppKey, appKey);
    }

    /// <summary>
    /// 不同应用应通过 IAppResiliencePolicyResolver 获取不同的弹性策略配置
    /// </summary>
    [Fact]
    public void ResolveResolver_ShouldCreateDifferentPolicies_WhenAppsHaveDifferentConfigs()
    {
        // Arrange
        var configs = CreateMultiAppConfigs();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configs);
        var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IAppResiliencePolicyResolver>();

        // Act - 解析两个不同应用的 resolver
        var defaultResolver = resolver.ResolveResolver("default");
        var hrResolver = resolver.ResolveResolver("hr-app");

        // Assert - 两个 resolver 都不为 null，且是不同实例
        Assert.NotNull(defaultResolver);
        Assert.NotNull(hrResolver);
        Assert.NotSame(defaultResolver, hrResolver);
    }
}
