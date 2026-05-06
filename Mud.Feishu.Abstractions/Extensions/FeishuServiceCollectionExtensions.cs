// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Configuration;
using Mud.HttpUtils;
using Mud.HttpUtils.Resilience;
using System.Text.Json;

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
        foreach (var config in configs)
        {
            var clientName = $"feishu-{config.AppKey}";
            var baseAddress = config.BaseUrl ?? "https://open.feishu.cn";
            var allowCustomBaseUrl = config.AllowCustomBaseUrl;
            var timeOut = config.TimeOut;

            ValidateFeishuBaseUrl(baseAddress, allowCustomBaseUrl);

            services.AddMudHttpClient(
                clientName,
                client =>
                {
                    client.BaseAddress = new Uri(baseAddress);
                    client.DefaultRequestHeaders.Add("User-Agent", "MudFeishuClient/1.0");
                    client.Timeout = TimeSpan.FromSeconds(timeOut);
                });
        }

        var firstConfig = configs.FirstOrDefault();
        if (firstConfig != null)
        {
            services.AddMudHttpResilienceDecorator(resilienceOptions =>
            {
                resilienceOptions.Retry.Enabled = true;
                resilienceOptions.Retry.MaxRetryAttempts = firstConfig.RetryCount;
                resilienceOptions.Retry.DelayMilliseconds = firstConfig.RetryDelayMs;
                resilienceOptions.Retry.UseExponentialBackoff = true;
                resilienceOptions.Timeout.Enabled = true;
                resilienceOptions.Timeout.TimeoutSeconds = firstConfig.TimeOut;
            });
        }

        if (configs.Count > 1)
        {
            var nonDefaultConfigs = configs.Where(c => c != firstConfig && (c.RetryCount != firstConfig.RetryCount || c.RetryDelayMs != firstConfig.RetryDelayMs || c.TimeOut != firstConfig.TimeOut)).ToList();
            if (nonDefaultConfigs.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MudFeishu] 多应用模式下弹性策略（重试、超时）为全局共享配置，当前使用默认应用 '{firstConfig!.AppKey}' 的配置。" +
                    $"以下应用的自定义 Resilience 配置将被忽略: {string.Join(", ", nonDefaultConfigs.Select(c => c.AppKey))}");
            }
        }

        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

        services.AddMemoryCache();

        services.AddTokenProvider();
        services.AddCurrentUserContext();

        if (!services.Any(s => s.ServiceType == typeof(ITokenStore)))
        {
            services.AddSingleton<FeishuTokenStore>();
            services.AddSingleton<ITokenStore>(sp => sp.GetRequiredService<FeishuTokenStore>());
        }

        if (!services.Any(s => s.ServiceType == typeof(IUserTokenStore)))
            services.AddSingleton<IUserTokenStore, FeishuUserTokenStore>();

        return services;
    }

    /// <summary>
    /// 验证飞书 BaseUrl 是否安全（简化版，仅验证飞书官方域名）
    /// </summary>
    private static void ValidateFeishuBaseUrl(string? baseUrl, bool allowCustomBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return;

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            !Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
        {
            throw new ArgumentException($"URL 格式无效: {baseUrl}", nameof(baseUrl));
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"仅允许 HTTPS 协议，当前协议: {uri.Scheme}");
        }
    }
}
