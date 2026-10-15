// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Authentication.MultiApp;
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
    // C-1 补强：MudHttpTokenRecovery 配置节绑定（配置绑定断层修复）
    // ============================================================

    /// <summary>
    /// C-1 补强验证：经 <c>AddFeishuApp(IConfiguration)</c> 注册后，<c>MudHttpTokenRecovery</c> 配置节
    /// 必须被真正绑定到 <see cref="Mud.HttpUtils.TokenRecoveryOptions"/>。
    /// 业务场景：此前仅调用 <c>services.AddOptions&lt;TokenRecoveryOptions&gt;()</c>（不绑定任何配置节），
    /// 该节被**静默忽略**，RecoveryMaxRetries / RefreshTimeoutSeconds 等在配置中设置后不生效，
    /// 与 <c>AddFeishuAppBaseServices</c> 中"可通过 IConfiguration 的 MudHttpTokenRecovery 节自定义"的注释不符。
    /// </summary>
    /// <remarks>
    /// F-04（Mud.HttpUtils 3.0.x，B6）：绑定路径改为「纯 DTO 投影 <c>TokenRecoveryConfiguration</c>
    /// + Apply 逐字段映射」（消除 SYSLIB1100/1101），本用例同时覆盖数值/字符串/枚举三类字段，
    /// 证明 Apply 映射链与原直接绑定等价。
    /// </remarks>
    [Fact]
    public void AddFeishuApp_ShouldBindTokenRecoveryOptions_FromConfigurationSection()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuApps:0:AppKey"] = "default",
                ["FeishuApps:0:AppId"] = "cli_default_id_1234567890",
                ["FeishuApps:0:AppSecret"] = "default_secret_123456",
                ["FeishuApps:0:IsDefault"] = "true",
                ["MudHttpTokenRecovery:RecoveryMaxRetries"] = "5",
                ["MudHttpTokenRecovery:RefreshTimeoutSeconds"] = "7.5",
                ["MudHttpTokenRecovery:TokenScheme"] = "Bearer",
                ["MudHttpTokenRecovery:BufferingMode"] = "MemoryOnly",
                ["MudHttpTokenRecovery:MaxCachedRequestBodyBytes"] = "2097152"
            })
            .Build();
        var services = CreateServiceCollection();

        // Act
        services.AddFeishuApp(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<Mud.HttpUtils.TokenRecoveryOptions>>().Value;
        options.RecoveryMaxRetries.Should().Be(5,
            "MudHttpTokenRecovery:RecoveryMaxRetries 必须绑定（否则该配置节被静默忽略）");
        options.RefreshTimeoutSeconds.Should().Be(7.5,
            "MudHttpTokenRecovery:RefreshTimeoutSeconds 必须绑定");
        options.TokenScheme.Should().Be("Bearer", "F-04：字符串字段经 TokenRecoveryConfiguration 投影仍须绑定");
        options.BufferingMode.Should().Be(RequestBodyBufferingMode.MemoryOnly,
            "F-04：枚举字段经 TokenRecoveryConfiguration 投影仍须绑定");
        options.MaxCachedRequestBodyBytes.Should().Be(2097152,
            "F-04：数值字段经 TokenRecoveryConfiguration 投影仍须绑定");

        // 与组件 AddMudHttpTokenRecoveryFromConfiguration 对齐：校验器 + 可直接解析的实例。
        provider.GetServices<IValidateOptions<Mud.HttpUtils.TokenRecoveryOptions>>()
            .Should().Contain(v => v is Mud.HttpUtils.TokenRecoveryOptionsValidator,
                "应注册组件的 TokenRecoveryOptionsValidator，使非法取值在选项解析期暴露而非静默生效");
        provider.GetRequiredService<Mud.HttpUtils.TokenRecoveryOptions>().RecoveryMaxRetries.Should().Be(5,
            "TMR-07：TokenRecoveryOptions 应可直接解析，且与 IOptions<T> 同源");
    }

    /// <summary>
    /// F-04 防退化守卫：配置绑定路径（纯 DTO 投影 + <c>TokenRecoveryOptionsExtensions.Apply</c>）
    /// <b>不得触碰</b> <c>TokenInvalidationDetector</c> 等编程式注入面——Apply 契约约定只映射
    /// <c>TokenRecoveryConfiguration</c> 携带的基元字段，宿主以 <c>PostConfigure</c> 注入的判定器
    /// 必须原样保留。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldPreserveProgrammaticDetector_WhenConfigurationBound()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuApps:0:AppKey"] = "default",
                ["FeishuApps:0:AppId"] = "cli_default_id_1234567890",
                ["FeishuApps:0:AppSecret"] = "default_secret_123456",
                ["FeishuApps:0:IsDefault"] = "true",
                ["MudHttpTokenRecovery:RecoveryMaxRetries"] = "5"
            })
            .Build();
        var services = CreateServiceCollection();
        services.AddFeishuApp(configuration);
        var detector = new Mock<ITokenInvalidationDetector>().Object;
        // PostConfigure 晚于 Configure 链（含 Apply）执行，模拟宿主编程式注入。
        services.PostConfigure<Mud.HttpUtils.TokenRecoveryOptions>(o => o.TokenInvalidationDetector = detector);

        // Act
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<Mud.HttpUtils.TokenRecoveryOptions>>().Value;

        // Assert
        options.TokenInvalidationDetector.Should().BeSameAs(detector,
            "F-04：Apply 不得触碰 TokenInvalidationDetector 等编程式注入面（否则宿主注入的判定器被静默丢弃）");
        options.RecoveryMaxRetries.Should().Be(5, "配置绑定路径仍须生效");
    }

    // ============================================================
    // C-1 补强：TokenRecoveryOptions 热更新（IOptionsChangeTokenSource 注册）
    // ============================================================

    /// <summary>
    /// C-1 补强验证：经 <c>AddFeishuApp(IConfiguration)</c> 注册后，<see cref="IOptionsMonitor{TokenRecoveryOptions}"/>
    /// 必须随 <c>MudHttpTokenRecovery</c> 配置节运行时重载而刷新（组件
    /// <c>AddMudHttpTokenRecoveryFromConfiguration</c> 的热更新语义）。
    /// 业务场景：仅调用委托式 <c>Configure&lt;T&gt;(o =&gt; section.Bind(o))</c> 不注册
    /// <c>IOptionsChangeTokenSource</c>，IOptionsMonitor 缓存在首解析后冻结——配置重载后
    /// TokenRecoveryExecutor / FeishuAppManager 仍读到旧恢复策略。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldReloadTokenRecoveryOptions_WhenConfigurationReloads()
    {
        // Arrange — 使用可重载的内存配置源（等价于 appsettings.json 的 reload token 触发路径）。
        var reloadableSource = new ReloadableMemoryConfigurationSource(new Dictionary<string, string?>
        {
            ["FeishuApps:0:AppKey"] = "default",
            ["FeishuApps:0:AppId"] = "cli_default_id_1234567890",
            ["FeishuApps:0:AppSecret"] = "default_secret_123456",
            ["FeishuApps:0:IsDefault"] = "true",
            ["MudHttpTokenRecovery:RecoveryMaxRetries"] = "3",
            ["MudHttpTokenRecovery:TokenScheme"] = "S1",
        });
        var configuration = new ConfigurationBuilder()
            .Add(reloadableSource)
            .Build();
        var services = CreateServiceCollection();

        // Act & Assert — 初始绑定生效。
        services.AddFeishuApp(configuration);
        using var provider = services.BuildServiceProvider();

        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<Mud.HttpUtils.TokenRecoveryOptions>>();
        optionsMonitor.CurrentValue.RecoveryMaxRetries.Should().Be(3,
            "初始配置应绑定（否则 MudHttpTokenRecovery 节被静默忽略）");

        // Act — 模拟配置运行时重载（FileConfigurationSource.Reload 的等价内存版：写 Data + 触发 reload token）。
        reloadableSource.Provider.Data["MudHttpTokenRecovery:RecoveryMaxRetries"] = "7";
        reloadableSource.Provider.Reload();

        // Assert — IOptionsMonitor 必须读到重载后的值；未注册 IOptionsChangeTokenSource 时缓存不失效，仍为 3。
        optionsMonitor.CurrentValue.RecoveryMaxRetries.Should().Be(7,
            "IOptionsMonitor 必须随配置重载刷新（IOptionsChangeTokenSource 已注册）");
    }

    /// <summary>
    /// 可重载的内存配置源：模拟 JSON 文件源的运行时重载行为（<see cref="ConfigurationProvider.Set"/>
    /// 不触发 reload token，故用显式 <see cref="ReloadableMemoryConfigurationProvider.Reload"/> 触发）。
    /// </summary>
    private sealed class ReloadableMemoryConfigurationSource : IConfigurationSource
    {
        private readonly ReloadableMemoryConfigurationProvider _provider;

        public ReloadableMemoryConfigurationSource(Dictionary<string, string?> initialData)
        {
            _provider = new ReloadableMemoryConfigurationProvider(initialData);
        }

        public ReloadableMemoryConfigurationProvider Provider => _provider;

        public IConfigurationProvider Build(IConfigurationBuilder builder) => _provider;
    }

    private sealed class ReloadableMemoryConfigurationProvider : ConfigurationProvider
    {
        public ReloadableMemoryConfigurationProvider(Dictionary<string, string?> initialData)
        {
            foreach (var (key, value) in initialData)
            {
                Data[key] = value;
            }
        }

        public new IDictionary<string, string?> Data => base.Data;

        public void Reload() => OnReload();
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
    /// <remarks>
    /// TMR-P0-1（F1）：桥接从「实例桥接」改为「解析桥接」（转发代理）。代理 DI 单例，
    /// 成员调用现取当前默认应用的管理器——因此代理实例与 <c>Resolver.GetTenantTokenManager()</c>
    /// 返回的真实管理器<b>不再是同一实例</b>（这是有意的行为变更：跟随 SetDefaultApp/热更新）。
    /// 此处断言代理的单例性与类型；"跟随默认应用"的行为语义由
    /// <c>TokenManagerBridgeProxyTests.BridgedTenantTokenManager_ShouldResolveCurrentDefaultApp_AfterSetDefaultApp</c> 覆盖。
    /// </remarks>
    [Fact]
    public void AddFeishuApp_ShouldRegister_ITenantTokenManager_InDiContainer()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();

        // Act
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        // Assert：解析两次为同一实例（DI 单例代理），且为转发代理类型。
        var first = provider.GetService<ITenantTokenManager>();
        var second = provider.GetService<ITenantTokenManager>();
        first.Should().NotBeNull("ITenantTokenManager 应已桥接注册到 DI 容器");
        first.Should().BeSameAs(second, "桥接代理应为 DI 单例");
        first.Should().BeOfType<ForwardingTenantTokenManager>(
            "桥接应为解析代理（每次成员调用现取当前默认应用的管理器）");
    }

    /// <summary>
    /// SR-P0-3 验证：注册 AddFeishuApp 后，IAppTokenManager 应可从 DI 容器直接解析。
    /// </summary>
    /// <remarks>
    /// TMR-P0-1（F1）：与 ITenantTokenManager 同理——桥接为解析代理（解析代理单例 + 类型断言）。
    /// </remarks>
    [Fact]
    public void AddFeishuApp_ShouldRegister_IAppTokenManager_InDiContainer()
    {
        var services = CreateServiceCollection();
        var configs = CreateDefaultConfigs();
        services.AddFeishuApp(configs);
        using var provider = services.BuildServiceProvider();

        var appTokenManager = provider.GetService<IAppTokenManager>();
        var second = provider.GetService<IAppTokenManager>();
        appTokenManager.Should().NotBeNull();
        appTokenManager.Should().BeSameAs(second, "桥接代理应为 DI 单例");
        appTokenManager.Should().BeOfType<ForwardingAppTokenManager>(
            "桥接应为解析代理（每次成员调用现取当前默认应用的管理器）");
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

    // ============================================================
    // F-07：域名单并集语义（增量注入，不覆盖宿主域名）
    // ============================================================

    /// <summary>
    /// F-07 防退化守卫：AddFeishuApp 注入飞书域名必须为<b>并集</b>语义——宿主在注册前
    /// 自行 <c>UrlValidator.AddAllowedDomain</c> 的自定义域名不得被清掉
    /// （旧 <c>ConfigureAllowedDomains</c> 整体替换语义会静默丢弃宿主域名的回归面）。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldPreserveHostAllowedDomains_WhenMergingFeishuDomains()
    {
        var original = UrlValidator.GetAllowedDomains().ToArray();
        try
        {
            // Arrange：宿主在 AddFeishuApp 之前注册自定义域名。
            UrlValidator.AddAllowedDomain("work.weixin.qq.com");
            var services = CreateServiceCollection();

            // Act
            services.AddFeishuApp(CreateDefaultConfigs());

            // Assert
            var domains = UrlValidator.GetAllowedDomains();
            domains.Should().Contain("work.weixin.qq.com", "宿主预注册的自定义域名必须保留（并集语义）");
            domains.Should().Contain("open.feishu.cn", "飞书默认域名必须注入");
        }
        finally
        {
            // 还原全局白名单，避免污染同进程其他 UrlValidator 相关用例。
            UrlValidator.ConfigureAllowedDomains(original);
        }
    }

    // ============================================================
    // F-09：默认授权器 = 注册表白名单（IFeishuAppManager.HasApp 谓词）
    // ============================================================

    /// <summary>
    /// F-09 防退化守卫：AddFeishuApp 必须注册默认 <see cref="IAppAccessAuthorizer"/>（注册表白名单语义）：
    /// 已注册 appKey 放行；未注册 / 空白 / null appKey 一律拒绝（fail-closed）。
    /// 回归面：退回放行型授权器（AllowAllAppAccessAuthorizer）或取消默认注册（BC-18 默认拒绝）。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldAuthorizeOnlyRegisteredAppKeys_WhenDefaultAuthorizerResolved()
    {
        // Arrange
        var services = CreateServiceCollection();
        services.AddFeishuApp(CreateDefaultConfigs());
        using var provider = services.BuildServiceProvider();

        // Act
        var authorizer = provider.GetRequiredService<IAppAccessAuthorizer>();

        // Assert
        authorizer.CanSwitchTo("default").Should().BeTrue("已注册的默认应用必须获得授权");
        authorizer.CanSwitchTo("unregistered-app").Should().BeFalse("未注册 appKey 必须拒绝");
        authorizer.CanSwitchTo("").Should().BeFalse("空白 appKey 必须 fail-closed 拒绝");
        authorizer.CanSwitchTo(null!).Should().BeFalse("null appKey 必须 fail-closed 拒绝");
    }

    /// <summary>
    /// F-09 动态性：运行时 <c>AddApp</c> 新增的应用必须<b>即时</b>获得授权——
    /// 谓词经 IFeishuAppManager.HasApp 每次调用现算，不是注册期快照。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldAuthorizeRuntimeAddedApp_AfterAddApp()
    {
        // Arrange — AddApp 会急切创建应用上下文（命名 HttpClient 走 IHttpClientFactory），测试补 mock 基建。
        var services = CreateServiceCollection();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());
        services.AddSingleton(httpClientFactoryMock.Object);
        services.AddFeishuApp(CreateDefaultConfigs());
        using var provider = services.BuildServiceProvider();

        var authorizer = provider.GetRequiredService<IAppAccessAuthorizer>();
        authorizer.CanSwitchTo("runtime-app").Should().BeFalse("新增前必须拒绝");

        // Act
        var appManager = provider.GetRequiredService<IFeishuAppManager>();
        appManager.AddApp(new FeishuAppConfig
        {
            AppKey = "runtime-app",
            AppId = "cli_runtime_id_12345678",
            AppSecret = "runtime_secret_123456"
        });

        // Assert
        authorizer.CanSwitchTo("runtime-app").Should().BeTrue("AddApp 后必须即时获得授权（谓词动态判定）");
    }

    /// <summary>
    /// F-09 宿主优先：宿主在 AddFeishuApp 之前注册的 <see cref="IAppAccessAuthorizer"/> 必须胜出
    /// （TryAdd 语义，先注册者胜），默认白名单不得覆盖宿主的更严格授权策略。
    /// </summary>
    [Fact]
    public void AddFeishuApp_ShouldKeepHostAuthorizer_WhenRegisteredBeforeAddFeishuApp()
    {
        // Arrange — 宿主授权器只放行 "host-managed"（比默认注册表白名单更严格）。
        var services = CreateServiceCollection();
        services.AddSingleton<IAppAccessAuthorizer>(new AppKeyAllowListAuthorizer(new[] { "host-managed" }));

        // Act
        services.AddFeishuApp(CreateDefaultConfigs());
        using var provider = services.BuildServiceProvider();

        // Assert
        var authorizer = provider.GetRequiredService<IAppAccessAuthorizer>();
        authorizer.CanSwitchTo("host-managed").Should().BeTrue("宿主授权器名单内放行");
        authorizer.CanSwitchTo("default").Should().BeFalse("宿主授权器必须胜出：默认应用虽已注册，但不在宿主名单内");
    }
}
