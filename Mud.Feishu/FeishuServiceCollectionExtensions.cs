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
        // 添加配置验证
        services.AddOptions<FeishuOptions>()
               .Validate(options =>
                    !string.IsNullOrEmpty(options.AppId) &&
                    !string.IsNullOrEmpty(options.AppSecret),
                    "AppId and AppSecret are required for Feishu service")
               .ValidateOnStart();
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

        // 添加配置验证
        services.AddOptions<FeishuOptions>()
               .Validate(options =>
                    !string.IsNullOrEmpty(options.AppId) &&
                    !string.IsNullOrEmpty(options.AppSecret),
                    "AppId and AppSecret are required for Feishu service")
               .ValidateOnStart();
        return services.AddFeishuApiService();
    }

    /// <summary>
    /// 注册飞书 API 服务（核心方法）
    /// </summary>
    private static IServiceCollection AddFeishuApiService(this IServiceCollection services)
    {
        services.AddSingleton<ITokenManager, TenantTokenManagerWithCache>();
        return services.AddWebApiHttpClient()
                       .AddWebApiHttpClientWrap();
    }
}
