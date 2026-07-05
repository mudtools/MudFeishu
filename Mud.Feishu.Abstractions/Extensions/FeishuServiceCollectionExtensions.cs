// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
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
    /// <returns>服务集合实例。支持链式调用</returns>
    /// <remarks>
    /// 此方法用于多应用系统，注册了基础依赖项但不注册全局TokenManager。
    /// </remarks>
    internal static IServiceCollection AddFeishuAppBaseServices(this IServiceCollection services, List<FeishuAppConfig> configs)
    {
        UrlValidator.ConfigureAllowedDomains(["open.feishu.cn", "open.larksuite.com", "larksuite.com", "feishu.cn"]);

        foreach (var config in configs)
        {
            var clientName = $"feishu-{config.AppKey}";
            var baseAddress = config.BaseUrl ?? "https://open.feishu.cn";
            bool allowCustomBaseUrl = config?.AllowCustomBaseUrl ?? false;
            var timeOut = config?.TimeOut ?? 30;
            // 显式标记默认应用：AddMudHttpClient 内部 setAsDefault=true 时强制覆盖 IEnhancedHttpClient 默认注册，
            // setAsDefault=false 时使用 TryAddTransient（已注册则跳过）。
            // 此前未传该参数（默认 false），导致默认 IEnhancedHttpClient 隐式绑定到 configs 列表中的第一个 AppKey，
            // 而非 IsDefault=true 的应用。现在通过显式传入确保默认 HttpClient 与 IsDefault=true 严格对应。
            bool isDefault = config.IsDefault;

            services.AddMudHttpClient(
                clientName,
                client =>
                {
                    UrlValidator.ValidateBaseUrl(baseAddress, allowCustomBaseUrl);
                    client.BaseAddress = new Uri(baseAddress);
                    client.DefaultRequestHeaders.Add("User-Agent", "MudFeishuClient/1.0");
                    client.Timeout = TimeSpan.FromSeconds(timeOut);
                },
                setAsDefault: isDefault);
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

        services.TryAddSingleton(_ => HttpClientExtensions.GetDefaultJsonSerializerOptions());

        services.AddMemoryCache();

        // 注意：必须先注册飞书用户上下文，再调用 AddTokenProvider()。
        // 原因：AddTokenProvider() 内部会 TryAddSingleton<ICurrentUserContext, DefaultCurrentUserContext<CurrentUserInfo>>(),
        // 若先调用，飞书的桥接注册会因 TryAddSingleton 语义（已存在则跳过）而失效，导致两个上下文实例状态不共享。
        services.TryAddSingleton<IFeishuCurrentUserContext, DefaultFeishuCurrentUserContext>();
        services.TryAddSingleton<Mud.HttpUtils.ICurrentUserContext>(sp => sp.GetRequiredService<IFeishuCurrentUserContext>());
        services.AddTokenProvider();

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

        // 注册令牌主动刷新后台服务（由 Mud.HttpUtils 提供，按目标框架自动选择实现）。
        // 此前该服务仅在 Webhook 模块注册，纯 SDK 使用场景下 Token 仅懒加载刷新，
        // 首次请求延迟增加且无法享受"过期前主动刷新"预热。
        // 现统一在基础服务中注册，Webhook 模块保留配置覆盖即可。
        services.AddTokenRefreshBackgroundService();

        // 启用后台刷新服务（Mud.HttpUtils 默认 Enabled=false，需显式启用）
        services.AddOptions<TokenRefreshBackgroundOptions>()
            .PostConfigure<IOptions<List<FeishuAppConfig>>>((tokenOptions, appOptions) =>
            {
                var defaultConfig = appOptions.Value.FirstOrDefault(c => c.IsDefault) ?? appOptions.Value.FirstOrDefault();
                if (defaultConfig != null)
                {
                    tokenOptions.Enabled = true;
                }
            });

        return services;
    }

}
