// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Configuration;
using Mud.HttpUtils;
using Mud.HttpUtils.Resilience;
using System.Net;
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
    /// 添加令牌缓存服务（自定义实现）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddTokenCache<TCacheImplementation>(this IServiceCollection services)
        where TCacheImplementation : class, ITokenCache
    {
        services.TryAddSingleton<ITokenCache, TCacheImplementation>();
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
            var retryCount = config.RetryCount;
            var retryDelayMs = config.RetryDelayMs;

            // 验证 BaseUrl 是否安全（SSRF 防护）
            ValidateFeishuBaseUrl(baseAddress, allowCustomBaseUrl);

            services.AddMudHttpClient(
                clientName,
                client =>
                {
                    client.BaseAddress = new Uri(baseAddress);
                    client.DefaultRequestHeaders.Add("User-Agent", "MudFeishuClient/1.0");
                    client.Timeout = TimeSpan.FromSeconds(timeOut);
                });

            services.AddMudHttpResilienceDecorator(resilienceOptions =>
            {
                resilienceOptions.Retry.Enabled = true;
                resilienceOptions.Retry.MaxRetryAttempts = retryCount;
                resilienceOptions.Retry.DelayMilliseconds = retryDelayMs;
                resilienceOptions.Retry.UseExponentialBackoff = true;
                resilienceOptions.Timeout.Enabled = true;
                resilienceOptions.Timeout.TimeoutSeconds = timeOut;
            });
        }

        // 注册JSON配置
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

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

        if (!allowCustomBaseUrl)
        {
            var host = uri.Host.ToLowerInvariant();
            var allowedDomains = new[] { "open.feishu.cn", "open.larksuite.com", "feishu.cn", "larksuite.com" };
            bool isAllowed = allowedDomains.Any(domain =>
                host == domain || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                throw new InvalidOperationException(
                    $"域名 '{uri.Host}' 不在飞书官方白名单中。如需使用自定义域名，请设置 AllowCustomBaseUrl=true（注意安全风险）。");
            }
        }
    }
}
