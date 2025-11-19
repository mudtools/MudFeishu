// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
    public static IServiceCollection AddFeishuApiService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FeishuOptions>(configuration)
                .AddOptions<FeishuOptions>()
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
    public static IServiceCollection AddFeishuApiService(this IServiceCollection services, string configurationSection = "Feishu")
    {
        services.Configure<FeishuOptions>(
            services.BuildServiceProvider()
                   .GetRequiredService<IConfiguration>()
                   .GetSection(configurationSection))
               .AddOptions<FeishuOptions>()
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
        return services.AddSingleton<ITokenManager, TokenManagerWithCache>()
                       .AddWebApiHttpClient()
                       .AddWebApiHttpClientWrap();
    }
}
