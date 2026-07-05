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
}
