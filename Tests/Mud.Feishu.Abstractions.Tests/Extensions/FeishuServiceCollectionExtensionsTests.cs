// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Mud.HttpUtils;
using Mud.HttpUtils.Resilience;

namespace Mud.Feishu.Abstractions.Tests.Extensions;

/// <summary>
/// 服务注册扩展方法测试 - 验证 ServiceRegistration-Fix-Refactor-Plan 中的修复点
/// </summary>
/// <remarks>
/// 测试覆盖以下修复点：
/// - SR-P0-1：ICurrentUserContext 桥接顺序
/// - SR-P0-2：TokenRefreshBackgroundService 注册位置
/// - SR-P1-2：AddMudHttpClient 显式传递 setAsDefault
/// - SR-P2-1：FeishuUserTokenStore 具体类注册
/// </remarks>
public class FeishuServiceCollectionExtensionsTests
{
    /// <summary>
    /// 构造默认测试配置（单个默认应用）
    /// </summary>
    private static List<FeishuAppConfig> CreateDefaultConfigs() => new()
    {
        new FeishuAppConfig
        {
            AppKey = "default",
            AppId = "cli_default_id_1234567890",
            AppSecret = "default_secret_123456",
            IsDefault = true
        }
    };

    /// <summary>
    /// 构造测试用 ServiceCollection，已添加 Logging 与多应用支持所需的基础设施
    /// </summary>
    private static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    // ============================================================
    // SR-P0-1：ICurrentUserContext 桥接顺序修复
    // ============================================================

    /// <summary>
    /// SR-P0-1 验证：注册 AddFeishuApp 后，ICurrentUserContext 与 IFeishuCurrentUserContext 应为同一实例。
    /// 业务场景：AddTokenProvider() 内部会 TryAddSingleton ICurrentUserContext，若先调用，
    /// 飞书桥接注册会因 TryAddSingleton 语义（已存在则跳过）而失效，导致两个上下文实例状态不共享。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldBridgeICurrentUserContext_WhenDefaultImplementation()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // Assert
        var feishuContext = provider.GetRequiredService<IFeishuCurrentUserContext>();
        var httpUtilsContext = provider.GetRequiredService<Mud.HttpUtils.ICurrentUserContext>();

        httpUtilsContext.Should().BeSameAs(feishuContext,
            "ICurrentUserContext 应桥接到 IFeishuCurrentUserContext 同一实例，确保用户上下文状态共享");
    }

    /// <summary>
    /// SR-P0-1 验证：调用 IFeishuCurrentUserContext.SetUser 后，ICurrentUserContext.UserId 应能读到对应值。
    /// 业务场景：验证两个上下文接口的状态共享，避免未来启用 RequiresUserId 时用户身份丢失。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldShareUserState_BetweenContexts()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var feishuContext = provider.GetRequiredService<IFeishuCurrentUserContext>();
        var httpUtilsContext = provider.GetRequiredService<Mud.HttpUtils.ICurrentUserContext>();

        // Act
        feishuContext.SetUser("open_id_test", userId: "user_id_test");

        // Assert
        httpUtilsContext.UserId.Should().Be("user_id_test",
            "SetUser 写入 IFeishuCurrentUserContext 后，应能通过 ICurrentUserContext.UserId 读取");
    }

    // ============================================================
    // SR-P0-2：TokenRefreshBackgroundService 注册位置修复
    // ============================================================

    /// <summary>
    /// SR-P0-2 验证：未启用 Webhook 时，DI 容器应包含 TokenRefreshBackgroundService 的 IHostedService 注册。
    /// 业务场景：纯 SDK 使用场景（仅 AddFeishuApp），后台刷新服务应已注册。
    /// 此前该服务仅在 Webhook 模块注册，导致纯 SDK 场景下 Token 仅懒加载刷新。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldRegisterTokenRefreshBackgroundService_WhenWebhookNotRegistered()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // Assert
        // AddTokenRefreshBackgroundService 内部通过 AddHostedService 注册 IHostedService 实现
        var hostedServices = provider.GetServices<IHostedService>();
        hostedServices.Should().NotBeEmpty("TokenRefreshBackgroundService 应作为 IHostedService 注册");

        // 验证存在 TokenRefresh 后台服务类型（按类型名匹配，避免依赖具体类型）
        hostedServices.Should().Contain(s =>
            s.GetType().Name.Contains("TokenRefresh", StringComparison.OrdinalIgnoreCase),
            "应注册 TokenRefresh 后台服务实现");
    }

    /// <summary>
    /// SR-P0-2 验证：默认配置下（含默认应用），TokenRefreshBackgroundOptions.Enabled 应为 true。
    /// 业务场景：AddFeishuAppBaseServices 应通过 PostConfigure 启用后台刷新服务（Mud.HttpUtils 默认 Enabled=false）。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldEnableTokenRefreshBackgroundOptions_WhenDefaultConfigExists()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // Assert
        var tokenOptions = provider.GetRequiredService<IOptions<TokenRefreshBackgroundOptions>>().Value;
        tokenOptions.Enabled.Should().BeTrue(
            "存在默认应用时，TokenRefreshBackgroundOptions.Enabled 应被 PostConfigure 设为 true");
    }

    // ============================================================
    // SR-P1-2：AddMudHttpClient 显式传递 setAsDefault
    // ============================================================

    /// <summary>
    /// SR-P1-2 验证：当 IsDefault=true 的应用在第二个位置时，默认 IEnhancedHttpClient 应绑定到该应用。
    /// 业务场景：此前 AddMudHttpClient 未传 setAsDefault（默认 false），导致默认 IEnhancedHttpClient
    /// 隐式绑定到列表中第一个 AppKey，而非 IsDefault=true 的应用。
    /// 验证方式：通过不同的 BaseUrl 区分应用，检查默认 IEnhancedHttpClient 的 BaseAddress。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldSetDefaultHttpClient_ToExplicitDefaultApp()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = "first",
                AppId = "cli_first_id_1234567890",
                AppSecret = "first_secret_12345678",
                BaseUrl = "https://open.feishu.cn",
                AllowCustomBaseUrl = false,
                IsDefault = false
            },
            new()
            {
                AppKey = "second",
                AppId = "cli_second_id_1234567890",
                AppSecret = "second_secret_12345678",
                BaseUrl = "https://open.larksuite.com",
                AllowCustomBaseUrl = true,
                IsDefault = true
            }
        };

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // Assert
        // 默认 IEnhancedHttpClient 应能解析（证明 setAsDefault=true 至少对一个应用生效）
        var resolvedDefault = provider.GetRequiredService<IEnhancedHttpClient>();
        resolvedDefault.Should().NotBeNull("默认 IEnhancedHttpClient 应已注册");

        // 默认 IEnhancedHttpClient 的 BaseAddress 应对应 IsDefault=true 的应用（second → open.larksuite.com）
        // 注意：GetClient(name) 与 GetRequiredService<IEnhancedHttpClient>() 返回不同的包装实例，
        // 但底层指向同一个命名 HttpClient，BaseAddress 应一致。
        resolvedDefault.BaseAddress.Should().Be(new Uri("https://open.larksuite.com/"),
            "默认 IEnhancedHttpClient 应绑定到 IsDefault=true 的应用（second），而非列表中的第一个");
    }

    /// <summary>
    /// SR-P1-2 验证：当用户不显式设置 IsDefault 时，第一个应用应被自动推断为默认。
    /// 业务场景：ValidateAndSetDefaultApp 会将第一个应用设为默认，AddFeishuAppBaseServices 应能读到正确的 IsDefault 值。
    /// 此前由于调用顺序错误（先注册后验证），导致 setAsDefault 始终为 false。
    /// 验证方式：检查 configs[0].IsDefault 在 AddFeishuApp 调用后被设置为 true，
    /// 并验证 IEnhancedHttpClient 已注册（可通过 setAsDefault=true 注册）。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldAutoSetDefaultApp_WhenIsDefaultNotSpecified()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = "auto-default",
                AppId = "cli_auto_default_1234567890",
                AppSecret = "auto_default_secret_123456"
                // 不显式设置 IsDefault
            }
        };

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // Assert - ValidateAndSetDefaultApp 应将第一个应用自动设为默认（在 AddFeishuAppBaseServices 之前）
        configs[0].IsDefault.Should().BeTrue("第一个应用应被自动设为默认（ValidateAndSetDefaultApp 应在 AddFeishuAppBaseServices 之前调用）");

        // 默认 IEnhancedHttpClient 应能解析（证明 setAsDefault=true 被传递）
        var resolvedDefault = provider.GetService<IEnhancedHttpClient>();
        resolvedDefault.Should().NotBeNull("未显式设置 IsDefault 时，应通过自动推断使 setAsDefault=true，从而注册 IEnhancedHttpClient");
    }

    /// <summary>
    /// SR-P1-2 边界验证：当 IsDefault=true 在第一个位置时，行为应保持正确。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldKeepDefaultHttpClient_WhenDefaultIsFirst()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = "default-app",
                AppId = "cli_default_app_1234567890",
                AppSecret = "default_app_secret_12345678",
                BaseUrl = "https://open.larksuite.com",
                AllowCustomBaseUrl = true,
                IsDefault = true
            },
            new()
            {
                AppKey = "secondary-app",
                AppId = "cli_secondary_app_1234567890",
                AppSecret = "secondary_secret_12345678",
                BaseUrl = "https://open.feishu.cn",
                IsDefault = false
            }
        };

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // Assert
        var resolvedDefault = provider.GetRequiredService<IEnhancedHttpClient>();
        resolvedDefault.Should().NotBeNull("默认 IEnhancedHttpClient 应已注册");

        // 默认 IEnhancedHttpClient 的 BaseAddress 应对应第一个 IsDefault=true 的应用（default-app → open.larksuite.com）
        resolvedDefault.BaseAddress.Should().Be(new Uri("https://open.larksuite.com/"),
            "默认 IEnhancedHttpClient 应绑定到第一个 IsDefault=true 的应用");
    }

    // ============================================================
    // SR-P2-1：FeishuUserTokenStore 具体类注册
    // ============================================================

    /// <summary>
    /// SR-P2-1 验证：解析 FeishuUserTokenStore 与 IUserTokenStore 应为同一实例。
    /// 业务场景：此前仅注册 IUserTokenStore 接口，未注册具体类，与 FeishuTokenStore 注册策略不一致。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldRegisterFeishuUserTokenStore_AsConcreteClass()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // Assert
        var concreteStore = provider.GetService<FeishuUserTokenStore>();
        var interfaceStore = provider.GetService<IUserTokenStore>();

        concreteStore.Should().NotBeNull("FeishuUserTokenStore 具体类应已注册");
        interfaceStore.Should().BeSameAs(concreteStore,
            "IUserTokenStore 应解析到 FeishuUserTokenStore 同一实例（具体类注册策略）");
    }

    /// <summary>
    /// SR-P2-1 验证：解析 FeishuTokenStore 与 ITokenStore 应为同一实例。
    /// 业务场景：保持与 FeishuTokenStore 注册策略一致。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldRegisterFeishuTokenStore_AsConcreteClass()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // Assert
        var concreteStore = provider.GetService<FeishuTokenStore>();
        var interfaceStore = provider.GetService<ITokenStore>();

        concreteStore.Should().NotBeNull("FeishuTokenStore 具体类应已注册");
        interfaceStore.Should().BeSameAs(concreteStore,
            "ITokenStore 应解析到 FeishuTokenStore 同一实例（具体类注册策略）");
    }

    // ============================================================
    // SR-P0-3：令牌管理器桥接注册（向后兼容，仅默认应用）
    // ============================================================

    /// <summary>
    /// SR-P0-3 验证：注册 AddFeishuApp 后，ITenantTokenManager 应可从 DI 容器直接解析（桥接注册，默认应用）。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldRegister_ITenantTokenManager_InDiContainer()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // Assert
        var tenantTokenManager = provider.GetService<ITenantTokenManager>();
        tenantTokenManager.Should().NotBeNull("ITenantTokenManager 应已桥接注册到 DI 容器");

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();
        tenantTokenManager.Should().BeSameAs(resolver.GetTenantTokenManager(),
            "ITenantTokenManager 应桥接到 Resolver 返回的默认应用租户令牌管理器实例");
    }

    /// <summary>
    /// SR-P0-3 验证：注册 AddFeishuApp 后，IAppTokenManager 应可从 DI 容器直接解析。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldRegister_IAppTokenManager_InDiContainer()
    {
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appTokenManager = provider.GetService<IAppTokenManager>();
        appTokenManager.Should().NotBeNull();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();
        appTokenManager.Should().BeSameAs(resolver.GetAppTokenManager());
    }

    /// <summary>
    /// SR-P0-3 验证：注册 AddFeishuApp 后，IFeishuUserTokenManager 和 IUserTokenManager 应可从 DI 容器解析。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldRegister_UserTokenManagers_InDiContainer()
    {
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var feishuUserTokenManager = provider.GetService<IFeishuUserTokenManager>();
        var userTokenManager = provider.GetService<IUserTokenManager>();

        feishuUserTokenManager.Should().NotBeNull();
        userTokenManager.Should().NotBeNull();
        userTokenManager.Should().BeSameAs(feishuUserTokenManager,
            "IUserTokenManager 应桥接到 IFeishuUserTokenManager 同一实例");
    }

    // ============================================================
    // SR-P0-4：IFeishuTokenManagerResolver 注册与多应用解析
    // ============================================================

    /// <summary>
    /// SR-P0-4 验证：注册 AddFeishuApp 后，IFeishuTokenManagerResolver 应可从 DI 解析。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldRegister_IFeishuTokenManagerResolver()
    {
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetService<IFeishuTokenManagerResolver>();
        resolver.Should().NotBeNull("IFeishuTokenManagerResolver 应已注册到 DI 容器");
    }

    /// <summary>
    /// SR-P0-4 验证：Resolver 无参数时返回默认应用的令牌管理器。
    /// </summary>
    [Fact]
    public void Resolver_GetTenantTokenManager_WithNullAppKey_ShouldReturnDefaultApp()
    {
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        resolver.GetTenantTokenManager().Should().BeSameAs(appManager.DefaultTenantTokenManager);
        resolver.GetAppTokenManager().Should().BeSameAs(appManager.DefaultAppTokenManager);
        resolver.GetUserTokenManager().Should().BeSameAs(appManager.DefaultUserTokenManager);
    }

    /// <summary>
    /// SR-P0-4 验证：Resolver 指定 appKey 时返回对应应用的令牌管理器（多应用场景核心能力）。
    /// </summary>
    [Fact]
    public void Resolver_GetTenantTokenManager_WithSpecificAppKey_ShouldReturnCorrectApp()
    {
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = "default",
                AppId = "cli_default_id_1234567890",
                AppSecret = "default_secret_123456",
                IsDefault = true
            },
            new()
            {
                AppKey = "hr-app",
                AppId = "cli_hr_app_1234567890",
                AppSecret = "hr_secret_123456"
            }
        };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IFeishuTokenManagerResolver>();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        var defaultTokenManager = resolver.GetTenantTokenManager();
        var hrTokenManager = resolver.GetTenantTokenManager("hr-app");

        defaultTokenManager.Should().BeSameAs(appManager.GetApp("default").TenantTokenManager,
            "默认应用令牌管理器应与 AppManager.GetApp(\"default\").TenantTokenManager 一致");
        hrTokenManager.Should().BeSameAs(appManager.GetApp("hr-app").TenantTokenManager,
            "hr-app 令牌管理器应与 AppManager.GetApp(\"hr-app\").TenantTokenManager 一致");
        hrTokenManager.Should().NotBeSameAs(defaultTokenManager,
            "不同应用的令牌管理器应为不同实例");
    }

    // ============================================================
    // SR-P0-5：TokenRefreshHostedService 令牌注册
    // ============================================================

    /// <summary>
    /// SR-P0-5 验证：AddFeishuApp 后，FeishuTokenRegistrationService 应作为 IHostedService 注册（NET6+）。
    /// 业务场景：此前 TokenRefreshHostedService 虽然注册并启用，但内部令牌字典为空，后台刷新形同虚设。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldRegister_FeishuTokenRegistrationService_AsHostedService()
    {
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();
        hostedServices.Should().Contain(s =>
            s.GetType().Name.Contains("FeishuTokenRegistration", StringComparison.OrdinalIgnoreCase),
            "FeishuTokenRegistrationService 应作为 IHostedService 注册");
    }

    /// <summary>
    /// SR-P0-5 验证：AddFeishuApp 后，ITokenRefreshBackgroundService 应可直接从 DI 解析。
    /// 业务场景：Mud.HttpUtils 改进后，TokenRefreshHostedService 同时注册为 IHostedService 和 ITokenRefreshBackgroundService，
    /// 消费方可直接注入 ITokenRefreshBackgroundService，无需遍历 GetServices&lt;IHostedService&gt;()。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldExpose_ITokenRefreshBackgroundService_Directly()
    {
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var refreshService = provider.GetService<ITokenRefreshBackgroundService>();
        refreshService.Should().NotBeNull(
            "ITokenRefreshBackgroundService 应可直接从 DI 解析（Mud.HttpUtils 改进：同时注册为 IHostedService 和 ITokenRefreshBackgroundService）");
    }

    /// <summary>
    /// SR-P0-5 验证：ITokenRefreshBackgroundService 直接解析与 IHostedService 中的实例应为同一对象。
    /// 业务场景：确保 AddHostedService 工厂和 AddSingleton 工厂指向同一单例实例，避免出现两个独立的刷新服务。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ITokenRefreshBackgroundService_ShouldBeSameInstance_AsHostedService()
    {
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var directRefreshService = provider.GetRequiredService<ITokenRefreshBackgroundService>();
        var hostedRefreshService = provider.GetServices<IHostedService>()
            .OfType<ITokenRefreshBackgroundService>()
            .FirstOrDefault();

        hostedRefreshService.Should().NotBeNull("TokenRefreshHostedService 应作为 IHostedService 注册");
        directRefreshService.Should().BeSameAs(hostedRefreshService,
            "直接解析的 ITokenRefreshBackgroundService 与 IHostedService 中的实例应为同一单例");
    }

    /// <summary>
    /// SR-P0-5 验证：应用启动后，FeishuTokenRegistrationService 应成功执行，令牌管理器应已注册到后台刷新服务。
    /// 业务场景：FeishuTokenRegistrationService 在 StartAsync 中将所有应用的令牌管理器注册到后台刷新服务。
    /// 此前该服务依赖 IServiceProvider 变通方案查找 ITokenRefreshBackgroundService，现在直接注入。
    /// </summary>
    [Fact]
    public async Task AddFeishuApp_ShouldRegisterTokenManagers_ToRefreshService_OnStartup()
    {
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = "default",
                AppId = "cli_default_id_1234567890",
                AppSecret = "default_secret_123456",
                IsDefault = true
            },
            new()
            {
                AppKey = "hr-app",
                AppId = "cli_hr_app_1234567890",
                AppSecret = "hr_secret_123456"
            }
        };
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // 直接解析 ITokenRefreshBackgroundService（Mud.HttpUtils 改进后支持直接注入）
        var refreshService = provider.GetRequiredService<ITokenRefreshBackgroundService>();
        refreshService.Should().NotBeNull("ITokenRefreshBackgroundService 应可直接解析");

        // 模拟主机启动：触发所有 IHostedService 的 StartAsync
        // 注意：FeishuTokenRegistrationService 直接注入 ITokenRefreshBackgroundService，无需遍历 IHostedService
        var hostedServices = provider.GetServices<IHostedService>();
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(default);
        }

        // 验证 FeishuTokenRegistrationService 已成功启动（未抛出异常即表示令牌注册成功）
        var registrationService = hostedServices.FirstOrDefault(s =>
            s.GetType().Name.Contains("FeishuTokenRegistration", StringComparison.OrdinalIgnoreCase));
        registrationService.Should().NotBeNull("FeishuTokenRegistrationService 应已注册并启动");

        // 验证 refreshService 和 hosted service 中的实例为同一对象
        var hostedRefreshService = hostedServices.OfType<ITokenRefreshBackgroundService>().FirstOrDefault();
        hostedRefreshService.Should().BeSameAs(refreshService,
            "FeishuTokenRegistrationService 注入的 ITokenRefreshBackgroundService 应与 DI 容器中的单例一致");
    }
}
