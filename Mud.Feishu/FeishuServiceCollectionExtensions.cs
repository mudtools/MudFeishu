using Microsoft.Extensions.Configuration;
using Mud.Feishu;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书相关服务注册扩展方法
/// </summary>
public static class FeishuServiceCollectionExtensions
{
    /// <summary>
    /// 注册飞书 API 服务（使用配置节）
    /// </summary>
    public static IServiceCollection AddFeishuApiService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FeishuOptions>(configuration);
        return services.AddFeishuApiService();
    }

    /// <summary>
    /// 注册飞书 API 服务（使用配置节名称）
    /// </summary>
    public static IServiceCollection AddFeishuApiService(
        this IServiceCollection services,
        string configurationSection = "Feishu")
    {
        services.Configure<FeishuOptions>(
            services.BuildServiceProvider()
                   .GetRequiredService<IConfiguration>()
                   .GetSection(configurationSection));
        return services.AddFeishuApiService();
    }

    /// <summary>
    /// 注册飞书 API 服务（核心方法）
    /// </summary>
    private static IServiceCollection AddFeishuApiService(this IServiceCollection services)
    {
        return services.AddWebApiHttpClient()
                      .AddWebApiHttpClientWrap();
    }
}
