// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mud.Feishu.Abstractions.Utilities;
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
            services.AddHttpClient($"feishu-{config.AppKey}")
                    .ConfigureFeishuHttpClient(config);
        }

        // 注册JSON配置
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

        return services;
    }


    private static IHttpClientBuilder ConfigureFeishuHttpClient(
        this IHttpClientBuilder builder,
        FeishuAppConfig? config,
        Action<HttpClient, IServiceProvider>? additionalConfig = null)
    {
        return builder
        .ConfigureHttpClient((serviceProvider, client) =>
        {
            // 基础配置
            string baseUrl = config?.BaseUrl ?? "https://open.feishu.cn";
            bool allowCustomBaseUrl = config?.AllowCustomBaseUrl ?? false;

            // 验证 BaseUrl 是否安全（SSRF 防护）
            UrlValidator.ValidateBaseUrl(baseUrl, allowCustomBaseUrl);

            int timeOut = config?.TimeOut ?? 60;

            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("User-Agent", "MudFeishuClient/1.0");
            client.Timeout = TimeSpan.FromSeconds(timeOut);

            // 额外的配置
            additionalConfig?.Invoke(client, serviceProvider);
        })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                // 启用更安全的SSL/TLS协议
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
                // 自动重定向
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,
                // 支持连接池复用
                MaxConnectionsPerServer = 100,
            })
            .AddPolicyHandler(request =>
            {
                // 使用自定义重试策略，区分 4xx 和 5xx 错误
                int retryCount = config?.RetryCount ?? 3;
                int retryDelayMs = config?.RetryDelayMs ?? 1000;
                return HttpRetryPolicyBuilder.BuildRetryPolicy(retryCount, retryDelayMs);
            });
    }
}
