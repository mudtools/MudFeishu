// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using System.Diagnostics.CodeAnalysis;
using Mud.Feishu.Abstractions.Authentication.MultiApp;
using Mud.HttpUtils;
using Mud.HttpUtils.Observability;
using Mud.HttpUtils.Resilience;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书服务集合扩展方法
/// </summary>
public static class FeishuServiceCollectionExtensions
{
    /// <summary>
    /// 从配置文件读取配置
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式配置绑定（ConfigurationBinder.Bind）在裁剪下无法静态分析配置类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式配置绑定（ConfigurationBinder.Bind）在 AOT/动态代码生成环境下不可用")]
#endif
    public static IServiceCollection ConfigureFrom(this IServiceCollection services, IConfiguration configuration, string sectionName = "FeishuApps")
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var section = sectionName ?? "FeishuApps";
        services.Configure<List<FeishuAppConfig>>(options => configuration.GetSection(section).Bind(options));

        services.AddSingleton<IValidateOptions<FeishuAppConfig>, FeishuAppConfigValidator>();
        services.AddSingleton<IValidateOptions<List<FeishuAppConfig>>, FeishuAppConfigValidator>();

        return services;
    }

    /// <summary>
    /// 使用代码配置
    /// </summary>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection ConfigureOptions(this IServiceCollection services, Action<List<FeishuAppConfig>> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        services.Configure(configureOptions);
        services.AddSingleton<IValidateOptions<FeishuAppConfig>, FeishuAppConfigValidator>();
        services.AddSingleton<IValidateOptions<List<FeishuAppConfig>>, FeishuAppConfigValidator>();
        return services;
    }


    /// <summary>
    /// 注册多应用所需的基础服务（内部使用）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configs">飞书配置列表</param>
    /// <param name="configuration">宿主配置（可选）。用于绑定组件的 <c>MudHttpTokenRecovery</c> 配置节；
    /// 为 <c>null</c>（无 IConfiguration 的代码式注册路径）时该节不参与绑定。</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    /// <remarks>
    /// 此方法用于多应用系统，注册了基础依赖项但不注册全局TokenManager。
    /// </remarks>
    internal static IServiceCollection AddFeishuAppBaseServices(this IServiceCollection services, List<FeishuAppConfig> configs, IConfiguration? configuration = null)
    {
        UrlValidator.ConfigureAllowedDomains(["open.feishu.cn", "open.larksuite.com", "larksuite.com", "feishu.cn"]);

        // MUDHTTP-2.0.5 适配（BC-18 / MT-02）：组件生成代码的 UseApp / BeginScope(appKey) 由
        // 「未注册 IAppAccessAuthorizer 即静默放行」改为「默认拒绝」（调用时抛 InvalidOperationException）。
        // 本 SDK 的多应用切换入口（FeishuAppManager.GetWebApi）依赖生成实现的 UseApp(appKey)，
        // 而 appKey 始终来源于 FeishuAppConfig 注册表（未知 appKey 由 GetApp 校验并抛错），
        // 因此注册放行型授权器恢复多应用切换能力；宿主可先注册更严格的 IAppAccessAuthorizer
        // 实现（TryAdd 语义：先注册者胜出）实现租户级授权。
        services.TryAddSingleton<IAppAccessAuthorizer, AllowAllAppAccessAuthorizer>();

        // REG-01 修复：校验重复 AppKey，避免命名 HttpClient 重复注册导致的静默覆盖。
        // FeishuAppManager 构造函数仅发出警告（保持覆盖语义），但 HttpClient 层重复注册会
        // 导致 EnhancedHttpClientFactoryOptions.ClientFactories 同名键覆盖，行为不可预测。
        // 此处在注册阶段即抛出异常，使配置错误快速失败。
        var duplicateAppKey = configs.GroupBy(c => c.AppKey, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateAppKey != null)
        {
            throw new InvalidOperationException(
                $"检测到重复的 AppKey '{duplicateAppKey.Key}'。每个应用的 AppKey 必须唯一，" +
                $"以避免命名 HttpClient 与令牌管理器注册冲突。");
        }

        foreach (var config in configs)
        {
            var appKey = config.AppKey;
            var clientName = $"feishu-{appKey}";
            var baseAddress = config.BaseUrl ?? Consts.DefaultFeishuBaseUrl;
            bool allowCustomBaseUrl = config?.AllowCustomBaseUrl ?? false;
            var timeOut = config?.TimeOut ?? 30;
            // 显式标记默认应用：AddMudHttpClient 内部 setAsDefault=true 时强制覆盖 IEnhancedHttpClient 默认注册，
            // setAsDefault=false 时使用 TryAddTransient（已注册则跳过）。
            // 此前未传该参数（默认 false），导致默认 IEnhancedHttpClient 隐式绑定到 configs 列表中的第一个 AppKey，
            // 而非 IsDefault=true 的应用。现在通过显式传入确保默认 HttpClient 与 IsDefault=true 严格对应。
            bool isDefault = config!.IsDefault;

            // 令牌恢复由 FeishuAppManager.CreateAppContext 中创建的 TokenRecoveryEnhancedClient 实现，
            // 不再需要在 Handler 管道中注册 LazyFeishuTokenRecoveryHandler。
            // 这消除了 IFeishuAppManager 构造期间的循环依赖问题。
            var httpClientBuilder = services.AddMudHttpClient(
                clientName,
                client =>
                {
                    UrlValidator.ValidateBaseUrl(baseAddress, allowCustomBaseUrl);
                    client.BaseAddress = new Uri(baseAddress);
                    client.DefaultRequestHeaders.Add("User-Agent", "MudFeishuClient/1.0");
                    client.Timeout = TimeSpan.FromSeconds(timeOut);
                },
                setAsDefault: isDefault);
            // P1-1 修复说明：TracingDelegatingHandler 已由 AddMudHttpClient 内部通过
            // httpClientBuilder.AddHttpMessageHandler(() => new TracingDelegatingHandler()) 注册，
            // 无需在此重复链式调用。重复注册会导致同一 named client 的 HttpMessageHandlerBuilderActions
            // 包含两个 TracingDelegatingHandler 工厂委托，在批量测试场景下触发
            // "The 'InnerHandler' property must be null" 异常（HttpMessageHandlerBuilder 禁止复用 DelegatingHandler）。

            // ARC-7：BaseUrl / TimeOut 热更新。
            // 上面注册委托捕获的是**注册期**快照，而 IHttpClientFactory 的命名客户端配置委托虽然每次
            // CreateClient 都会执行，却不会重新读取最新配置 —— 因此 IConfiguration 变更后 BaseUrl 仍为旧值
            // （多区域切换 feishu.cn ↔ larksuite.com 只能靠重启）。
            // 这里追加一个带 IServiceProvider 的配置动作（官方支持的 DI 感知配置入口），
            // 在每次 CreateClient 时从 IOptionsMonitor<List<FeishuAppConfig>> 读取**当前**配置，
            // 仅在发生实际变化时覆盖 BaseAddress / Timeout，从而让热更新链路真正贯通到命名客户端。
            //
            // 为什么不用组件 HttpClientFactoryEnhancedClient.WithBaseAddress：
            // TokenRecoveryEnhancedClient（sealed）未重写 WithBaseAddress，基类实现返回的是
            // **普通 HttpClientFactoryEnhancedClient**，既丢失令牌恢复能力又会令 (TokenRecoveryEnhancedClient)
            // 强制转换抛 InvalidCastException。详见 .docs/MudHttpUtils-2.0.4-Review-Remediation-Plan.md 附录 B-1。
            httpClientBuilder.ConfigureHttpClient((sp, client) =>
            {
                var latest = ResolveLatestAppConfig(sp, appKey);
                if (latest == null)
                {
                    // 可选依赖缺失（如仅注册了 HttpClient 而未接配置管线）时保持注册期快照，行为与修复前一致。
                    return;
                }

                var latestBaseUrl = string.IsNullOrWhiteSpace(latest.BaseUrl)
                    ? Consts.DefaultFeishuBaseUrl
                    : latest.BaseUrl;
                if (!string.Equals(latestBaseUrl, baseAddress, StringComparison.Ordinal))
                {
                    UrlValidator.ValidateBaseUrl(latestBaseUrl, latest.AllowCustomBaseUrl);
                    client.BaseAddress = new Uri(latestBaseUrl);
                }

                if (latest.TimeOut > 0 && latest.TimeOut != timeOut)
                {
                    client.Timeout = TimeSpan.FromSeconds(latest.TimeOut);
                }
            });
        }

        var defaultConfig = configs.FirstOrDefault(c => c.IsDefault) ?? configs.FirstOrDefault();
        if (defaultConfig != null)
        {
            services.AddMudHttpResilienceDecorator(resilienceOptions =>
            {
                resilienceOptions.Retry.Enabled = true;
                resilienceOptions.Retry.MaxRetryAttempts = defaultConfig.RetryCount;
                resilienceOptions.Retry.DelayMilliseconds = defaultConfig.RetryDelayMs;
                resilienceOptions.Retry.UseExponentialBackoff = true;
                resilienceOptions.Timeout.Enabled = true;
                resilienceOptions.Timeout.TimeoutSeconds = defaultConfig.TimeOut;
                resilienceOptions.CircuitBreaker.Enabled = defaultConfig.CircuitBreakerEnabled;
                resilienceOptions.CircuitBreaker.FailureThreshold = defaultConfig.CircuitBreakerFailureThreshold;
                resilienceOptions.CircuitBreaker.SamplingDurationSeconds = defaultConfig.CircuitBreakerSamplingDurationSeconds;
                resilienceOptions.CircuitBreaker.BreakDurationSeconds = defaultConfig.CircuitBreakerBreakDurationSeconds;
                resilienceOptions.CircuitBreaker.MinimumThroughput = defaultConfig.CircuitBreakerMinimumThroughput;
            });
        }

        // P1-4: 注册 per-app 弹性策略解析器，使不同应用可使用独立的重试/超时/熝断配置。
        // DefaultHttpRequestExecutor 优先使用 per-app 解析器，未命中时回退到全局解析器（defaultConfig 配置）。
        //
        // ARC-7b：选项工厂改为读取**当前**配置（IOptionsMonitor）而非注册期快照 `configs`，
        // 否则配置热更新后「新增应用」永远拿不到 per-app 弹性策略（只能落到全局回退）。
        // 残余限制：AppResiliencePolicyResolver 会按 appKey 缓存已解析的策略实例，且清空缓存所需的
        // InvalidateAll 未暴露在 IAppResiliencePolicyResolver 接口上（仅具体类型可见），
        // 因此**已解析过**的应用其弹性参数变更仍需重启进程 —— 见组件侧需求 COMP-4。
        services.TryAddSingleton<IAppResiliencePolicyResolver>(sp =>
        {
            var logger = sp.GetService<ILogger<AppResiliencePolicyResolver>>();
            return new AppResiliencePolicyResolver(
                appKey => CreateResilienceOptionsFromConfig(ResolveLatestAppConfigs(sp) ?? configs, appKey),
                logger);
        });

        // ARC-6 修复：注册 IFeishuAuthentication 的源生成实现。
        // FeishuAppManager.CreateAppContext 通过 GetRequiredService<IFeishuAuthentication>() 获取认证 API，
        // 但此前从未调用生成器产出的注册扩展（HttpClientApiExtensions.AddAuthenticationWebApiHttpClient），
        // 导致任何触发应用上下文创建的路径都抛 "No service for type 'IFeishuAuthentication' has been registered."
        // ——「多应用上下文」这条核心链路完全不可达。
        // 必须放在 AddMudHttpClient 循环之后：该扩展内部用 TryAdd 注册 IHttpRequestExecutor / IBaseHttpClient，
        // 先注册者胜出，需保证 AddMudHttpClient 的 DI 装配版本优先。
        services.AddAuthenticationWebApiHttpClient();

        // ARC-2 Step 1：注册统一的飞书 HTTP 客户端工厂，收敛 CreateAppContext 内的散装装配。
        services.TryAddSingleton<IFeishuHttpClientFactory, FeishuHttpClientFactory>();

        // TMA-09 / P1-8 修复（D7 契约）：注册 per-app 认证 API 工厂。
        // 使认证/取令牌请求使用本应用的命名 HttpClient（per-app 端点），而非默认应用端点。
        // 若 AOT 门禁不允许 ActivatorUtilities，设 FeishuAppOptions.EnablePerAppAuthenticationClient=false 降级。
        services.TryAddSingleton<IFeishuAuthenticationFactory, PerAppFeishuAuthenticationFactory>();

        // ARC-1：注册多应用管理器行为选项（EnableConfigReload 默认 true，支持配置节覆盖）。
        services.AddOptions<FeishuAppOptions>();

        services.TryAddSingleton(_ => HttpClientExtensions.GetDefaultJsonSerializerOptions());
        // NEW-GEN-01 修复：同时注册 IOptions<JsonSerializerOptions>，与生成器构造函数契约对齐
        services.TryAddSingleton<IOptions<System.Text.Json.JsonSerializerOptions>>(sp => Microsoft.Extensions.Options.Options.Create(sp.GetRequiredService<System.Text.Json.JsonSerializerOptions>()));

        // ARC-3：把 SDK 认证/通用源生成上下文接入组件 IOptions<JsonSerializerOptions> 管道。
        // AddMudHttpClientJsonContext 仅在 net8+ 提供（组件 ServiceCollectionExtensions 中位于
        // #if NET8_0_OR_GREATER 块内），低 TFM 必须跳过，否则编译失败。
#if NET8_0_OR_GREATER
        services.AddMudHttpClientJsonContext(FeishuApiResultJsonContext.Default);
#endif

        // M-8 修复：使用条件检测避免覆盖用户已配置的 IMemoryCache 选项（如容量限制）。
        // AddMemoryCache() 会无条件注册 IOptions<MemoryCacheOptions> 配置委托，
        // 可能覆盖用户在 ConfigureServices 中通过 AddMemoryCache(options => { ... }) 设置的配置。
        // TryAddMemoryCache() 是 .NET 9+ API，此处使用 Any 检测以兼容 netstandard2.0/net6.0/net8.0。
        if (!services.Any(s => s.ServiceType == typeof(IMemoryCache)))
        {
            services.AddMemoryCache();
        }

        // 注意：必须先注册飞书用户上下文，再调用 AddTokenProvider()。
        // 原因：AddTokenProvider() 内部会 TryAddSingleton<ICurrentUserContext, DefaultCurrentUserContext<CurrentUserInfo>>(),
        // 若先调用，飞书的桥接注册会因 TryAddSingleton 语义（已存在则跳过）而失效，导致两个上下文实例状态不共享。
        services.TryAddSingleton<IFeishuCurrentUserContext, DefaultFeishuCurrentUserContext>();
        services.TryAddSingleton<ICurrentUserContext>(sp => sp.GetRequiredService<IFeishuCurrentUserContext>());
        services.AddTokenProvider();

        // C-3 修复：注册 IAppContextHolder，供生成的 TokenManager 模式实现类构造函数注入。
        // 代码生成器 ConstructorGenerator 在 TokenManager 模式下生成必需的 IAppContextHolder 构造函数参数（无默认值），
        // 若未注册此服务，DI 容器解析任何飞书 API 接口时将抛出 InvalidOperationException。
        // AddTokenProvider() 仅注册 ITokenProvider 和 ICurrentUserContext，不包含 IAppContextHolder。
        services.TryAddSingleton<IAppContextHolder, AsyncLocalAppContextSwitcher>();

        // M-9 改进：ITokenStore 注册检测。
        // 此处使用 Any 检测而非 TryAdd 是为了支持 Redis 预注册场景：
        // 用户通过 AddRedisTokenStore() 在 AddFeishuApp 之前注册自定义 ITokenStore，
        // 此处检测到已注册则跳过默认的 FeishuTokenStore。
        // RedisFeishuServiceBuilderExtensions 已通过抛出异常阻止"Redis 在 AddFeishuApp 之后调用"的错误顺序。
        // 注意：多应用场景下 FeishuAppManager.CreateAppContext 会为每个应用创建独立的 per-app FeishuTokenStore 实例，
        // 此处的 Singleton 注册仅作为单应用模式的向后兼容回退。
        if (!services.Any(s => s.ServiceType == typeof(ITokenStore)))
        {
            services.AddSingleton<FeishuTokenStore>();
            services.AddSingleton<ITokenStore>(sp => sp.GetRequiredService<FeishuTokenStore>());
        }

        if (!services.Any(s => s.ServiceType == typeof(IUserTokenStore)))
        {
            services.AddSingleton<FeishuUserTokenStore>();
            services.AddSingleton<IUserTokenStore>(sp => sp.GetRequiredService<FeishuUserTokenStore>());
        }

        // S-3 修复：注册 IFeishuTokenStoreFactory，替代 FeishuAppManager 中的 is FeishuTokenStore 类型检查。
        // 默认使用 PerAppFeishuTokenStoreFactory（per-app FeishuTokenStore 实例）；
        // Redis 等自定义存储通过预注册 PerAppRedisTokenStoreFactory 覆盖（TryAdd 语义：已存在则跳过）。
        services.TryAddSingleton<IFeishuTokenStoreFactory, PerAppFeishuTokenStoreFactory>();

        // MA-02 修复：注册 IFeishuTokenManagerFactory，替代 FeishuAppManager 中直接 new TenantTokenManager(...) 的硬编码方式。
        // 默认使用 DefaultFeishuTokenManagerFactory，行为与原实现完全一致。
        // 自定义实现可通过预注册覆盖（TryAdd 语义：已存在则跳过）。
        services.TryAddSingleton<IFeishuTokenManagerFactory, DefaultFeishuTokenManagerFactory>();

        // C-1 修复：注册 TokenRecoveryOptions，使 TokenRecoveryExecutor 可经
        // IOptionsMonitor<TokenRecoveryOptions> 获取恢复策略。
        // C-1 补强（配置绑定断层修复）：**仅调用 AddOptions<T>() 并不会绑定任何配置节** ——
        // "MudHttpTokenRecovery" 节会被静默忽略，TokenRecoveryOptions 永远取默认值
        // （RecoveryMaxRetries=1 / RefreshTimeoutSeconds=30 / MaxCachedRequestBodyBytes=1MB /
        // RefreshDedupWindowSeconds=2 / MaxDedupEntries=1024），使这些可调参数无法通过配置下发。
        // 组件侧等价入口为 AddMudHttpTokenRecoveryFromConfiguration(IConfiguration)（内部即
        // Configure + GetSection.Bind + 校验器 + 可解析实例）；此处按本仓库既有约定
        // （services.Configure<T>(o => section.Bind(o))，与 FeishuWebhook/OpenTelemetry/Redis 一致）等价实现，
        // 并经 ChangeTokenSource 补齐组件入口的热更新语义（见下方注释）。
        services.AddOptions<TokenRecoveryOptions>();
        if (configuration != null)
        {
            var tokenRecoverySection = configuration.GetSection(TokenRecoveryOptions.SectionName);
            services.Configure<TokenRecoveryOptions>(options => tokenRecoverySection.Bind(options));
            // 与组件 AddMudHttpTokenRecoveryFromConfiguration 的热更新语义对齐：注册 ChangeTokenSource，
            // 配置重载时使 IOptionsMonitor（TokenRecoveryExecutor / FeishuAppManager 经此读取）的共享缓存失效，
            // 下次解析重新执行上述 Bind 委托，从而读到重载后的值。
            // 采用「委托式 Bind + 显式 ChangeTokenSource」而非 Configure<T>(IConfiguration) 重载：
            // 后者经 BindConfiguration 的反射绑定调用点无法被配置绑定源生成器拦截（AOT-3，IL2026/IL3050 须保持 0）。
            services.AddSingleton<IOptionsChangeTokenSource<TokenRecoveryOptions>>(
                new ConfigurationChangeTokenSource<TokenRecoveryOptions>(Options.DefaultName, tokenRecoverySection));
        }

        // 与组件 AddMudHttpTokenRecoveryFromConfiguration 对齐：注册校验器（非法取值在选项解析期暴露，
        // 而非静默生效）与可直接解析的 TokenRecoveryOptions 实例（TMR-07，兼容以 TokenRecoveryOptions
        // 为参数的旧构造函数）。
        services.TryAddSingleton<IValidateOptions<TokenRecoveryOptions>, TokenRecoveryOptionsValidator>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<TokenRecoveryOptions>>().Value);

#if NET6_0_OR_GREATER
        // SR-P0-2 修复：FeishuTokenRegistrationService 必须在 TokenRefreshBackgroundService 之前注册为 IHostedService，
        // 确保应用启动时先注册令牌管理器，再启动后台刷新服务。否则后台服务启动时会误报"未注册任何令牌管理器"。
        // IHostedService 的 StartAsync 按注册顺序执行，DI 解析在所有注册完成后才发生，因此此处注册顺序不影响依赖解析。
        services.AddHostedService<FeishuTokenRegistrationService>();
#endif

        // 注册令牌主动刷新后台服务（由 Mud.HttpUtils 提供，按目标框架自动选择实现）。
        // 此前该服务仅在 Webhook 模块注册，纯 SDK 使用场景下 Token 仅懒加载刷新，
        // 首次请求延迟增加且无法享受"过期前主动刷新"预热。
        // 现统一在基础服务中注册，Webhook 模块保留配置覆盖即可。
        services.AddTokenRefreshBackgroundService();

        // 启用后台刷新服务（Mud.HttpUtils 默认 Enabled=false，需显式启用）
        // S-4 修复说明：PostConfigure 在选项首次解析时执行一次，无法感知运行时动态移除应用。
        // REG-02 修复说明：运行时移除全部应用后后台服务仍会运行（但找不到令牌则空转，无功能副作用）。
        // 为减少不必要的空转，在 PostConfigure 中通过 IServiceProvider 延迟解析 IFeishuAppManager，
        // 若所有应用已被移除则禁用后台刷新。注意：此检查仅在选项首次解析时生效，
        // 运行时动态移除应用后如需立即停止后台服务，应调用 ITokenRefreshBackgroundService.StopAsync()。
        services.AddOptions<TokenRefreshBackgroundOptions>()
            .PostConfigure<IOptions<List<FeishuAppConfig>>>((tokenOptions, appOptions) =>
            {
                // 仅当存在至少一个已配置应用时启用后台刷新
                if (appOptions.Value.Count > 0)
                {
                    tokenOptions.Enabled = true;
                }
            });

        // 注册 Mud.HttpUtils 健康检查（Token 刷新 + 熔断器）
        // Token 刷新健康检查：5 分钟窗口，失败率 ≥30% Degraded，≥50% Unhealthy
        // 熔断器健康检查：任何 Open → Unhealthy
        services.AddMudHttpHealthChecks(options =>
        {
            options.TokenRefresh.WindowSeconds = 300;
            options.TokenRefresh.DegradedThreshold = 0.3;
            options.TokenRefresh.CriticalThreshold = 0.5;
            options.TokenRefresh.MinSampleSize = 3;

            options.CircuitBreaker.MaxOpenCount = 0;
            options.CircuitBreaker.MaxHalfOpenCount = 2;
        });

        // ENH-1：按需为令牌存储工厂叠加加密装饰器。
        // 必须放在**本方法末尾**——需在 IFeishuTokenStoreFactory 的默认注册（PerAppFeishuTokenStoreFactory）
        // 以及 Redis 等其他扩展先行注册之后，才能捕获到「最后生效的那个描述符」。
        DecorateTokenStoreFactoryForEncryption(services);

        return services;
    }

    /// <summary>
    /// ENH-1：把 <see cref="IFeishuTokenStoreFactory"/> 的既有注册包上加密装饰器。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 采用「捕获并替换最后一个描述符」的方式而非直接改具体类型：令牌存储工厂的具体实现
    /// 可能是本包默认的 <c>PerAppFeishuTokenStoreFactory</c>，也可能是
    /// <c>Mud.Feishu.Redis</c> 在 <c>AddFeishuApp</c> 之前注册的 <c>PerAppRedisTokenStoreFactory</c>，
    /// 装饰器不需要知道具体类型。
    /// </para>
    /// <para>
    /// 是否真正启用（<see cref="FeishuAppOptions.EnableTokenEncryption"/> 与 <c>IEncryptionProvider</c> 是否注册）
    /// 在**解析时**判断，因此调用顺序（<c>AddFeishuRedisTokenStore</c> 在 <c>AddFeishuApp</c> 之前/之后）
    /// 与配置绑定时机都不影响结果。
    /// </para>
    /// </remarks>
    private static void DecorateTokenStoreFactoryForEncryption(IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(IFeishuTokenStoreFactory));
        if (descriptor == null)
        {
            return;
        }

        // TMA-19 / P2-7 修复：加密装饰器幂等守卫。
        // 检测是否已装饰：若实现工厂返回的是 EncryptedFeishuTokenStoreFactory 则跳过。
        // 通过检查 ImplementationFactory 的目标类型（若为已装饰的工厂类型则跳过）。
        if (descriptor.ImplementationType == typeof(EncryptedFeishuTokenStoreFactory))
        {
            return;
        }

        services.Remove(descriptor);
        services.AddSingleton<IFeishuTokenStoreFactory>(sp =>
        {
            var inner = CreateTokenStoreFactoryFromDescriptor(sp, descriptor);

            var options = sp.GetService<IOptions<FeishuAppOptions>>()?.Value;
            if (options == null || !options.EnableTokenEncryption)
            {
                // TMA-21 / P2-12 修复：非内存存储且未加密时发出告警。
                // 检测是否使用 Redis 等持久化存储（非 PerAppFeishuTokenStoreFactory 即视为持久化）
                if (inner is not PerAppFeishuTokenStoreFactory)
                {
                    sp.GetService<ILogger<EncryptedFeishuTokenStoreFactory>>()?.LogWarning(
                        "令牌将以明文写入持久化存储（Redis 等），建议开启加密（FeishuAppOptions.EnableTokenEncryption = true）。");
                }
                return inner;
            }

            var encryption = sp.GetService<IEncryptionProvider>();
            if (encryption == null)
            {
                sp.GetService<ILogger<EncryptedFeishuTokenStoreFactory>>()?.LogWarning(
                    "FeishuAppOptions.EnableTokenEncryption = true，但未注册 IEncryptionProvider，" +
                    "令牌将以明文存储。请调用 AddMudHttpAesEncryption() 注册 AES 加密提供程序，或注册自定义 IEncryptionProvider。");
                return inner;
            }

            return new EncryptedFeishuTokenStoreFactory(
                inner,
                encryption,
                sp.GetService<ILogger<EncryptedFeishuTokenStoreFactory>>());
        });
    }

    /// <summary>
    /// 依据原始 <see cref="ServiceDescriptor"/> 的具体注册形式重建实例。
    /// </summary>
    private static IFeishuTokenStoreFactory CreateTokenStoreFactoryFromDescriptor(IServiceProvider sp, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is IFeishuTokenStoreFactory instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory != null)
        {
            return (IFeishuTokenStoreFactory)descriptor.ImplementationFactory(sp);
        }

        if (descriptor.ImplementationType != null)
        {
            return (IFeishuTokenStoreFactory)ActivatorUtilities.GetServiceOrCreateInstance(sp, descriptor.ImplementationType);
        }

        throw new InvalidOperationException(
            $"无法从 IFeishuTokenStoreFactory 的注册描述符（{descriptor.Lifetime}）构建实例。");
    }

    /// <summary>
    /// ARC-7：从 <see cref="IOptionsMonitor{T}"/> 读取指定应用在**当前时刻**的配置。
    /// </summary>
    /// <param name="serviceProvider">服务提供者（来自 <c>ConfigureHttpClient(IServiceProvider, HttpClient)</c>）。</param>
    /// <param name="appKey">应用唯一标识（严格序数比较）。</param>
    /// <returns>匹配到的配置；未接入配置管线或应用已下线时返回 <c>null</c>。</returns>
    /// <remarks>
    /// <para>
    /// 该读取发生在每次 <c>IHttpClientFactory.CreateClient</c> 时（而非注册期），因此能拿到
    /// <c>IConfiguration</c> 重载后的最新值。成本为一次 OptionsMonitor 缓存读取 + 一次线性遍历
    /// （应用数量为个位数），且仅在创建客户端时发生，不在请求热路径上。
    /// </para>
    /// <para>
    /// 使用 <see cref="IOptionsMonitor{T}"/> 而非 DI 中的 <c>List&lt;FeishuAppConfig&gt;</c> 单例：
    /// 后者是注册期快照，热更新不会更新它（见 <c>FeishuMultiAppExtensions.RegisterCoreServicesWithoutAppManager</c>）。
    /// </para>
    /// </remarks>
    internal static FeishuAppConfig? ResolveLatestAppConfig(IServiceProvider serviceProvider, string appKey)
    {
        var current = ResolveLatestAppConfigs(serviceProvider);
        if (current == null)
        {
            return null;
        }

        for (var i = 0; i < current.Count; i++)
        {
            var candidate = current[i];
            if (candidate != null && string.Equals(candidate.AppKey, appKey, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// ARC-7 / ARC-7b：读取当前生效的应用配置快照；未接入配置管线时返回 <c>null</c>。
    /// </summary>
    /// <param name="serviceProvider">服务提供者。</param>
    /// <returns>当前配置列表；不可用时返回 <c>null</c>（调用方回退到注册期快照）。</returns>
    internal static List<FeishuAppConfig>? ResolveLatestAppConfigs(IServiceProvider serviceProvider)
    {
        var monitor = serviceProvider.GetService<IOptionsMonitor<List<FeishuAppConfig>>>();
        var current = monitor?.CurrentValue;
        return current is { Count: > 0 } ? current : null;
    }

    private static ResilienceOptions? CreateResilienceOptionsFromConfig(List<FeishuAppConfig> configs, string appKey)
    {
        // NEW-REG-03 修复：AppKey 应严格大小写敏感，统一使用 StringComparison.Ordinal
        var config = configs.FirstOrDefault(c =>
            string.Equals(c.AppKey, appKey, StringComparison.Ordinal));
        if (config == null)
            return null;

        return new ResilienceOptions
        {
            Retry =
            {
                Enabled = true,
                MaxRetryAttempts = config.RetryCount,
                DelayMilliseconds = config.RetryDelayMs,
                UseExponentialBackoff = true
            },
            Timeout =
            {
                Enabled = true,
                TimeoutSeconds = config.TimeOut
            },
            CircuitBreaker =
            {
                Enabled = config.CircuitBreakerEnabled,
                FailureThreshold = config.CircuitBreakerFailureThreshold,
                SamplingDurationSeconds = config.CircuitBreakerSamplingDurationSeconds,
                BreakDurationSeconds = config.CircuitBreakerBreakDurationSeconds,
                MinimumThroughput = config.CircuitBreakerMinimumThroughput
            }
        };
    }

}
