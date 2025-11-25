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
    private const string DefaultConfigurationSection = "Feishu";
    private const string ValidationErrorMessage = "飞书服务需要在配置文件中正确配置 AppId 和 AppSecret。";

    /// <summary>
    /// 注册飞书 API 服务（使用配置节）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuApiService(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddFeishuApiService(configuration, DefaultConfigurationSection);
    }

    /// <summary>
    /// 注册飞书 API 服务（使用配置节名称）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="configurationSection">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuApiService(this IServiceCollection services, IConfiguration configuration, string configurationSection)
    {
        services.ConfigureFeishuOptions(configuration, configurationSection);
        services.ValidateFeishuOptions();

        return services.AddFeishuApiServiceCore();
    }

    /// <summary>
    /// 注册飞书 API 服务（使用选项）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuApiService(this IServiceCollection services, Action<FeishuOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        services.Configure(configureOptions);
        services.ValidateFeishuOptions();

        return services.AddFeishuApiServiceCore();
    }

    /// <summary>
    /// 配置飞书选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="configurationSection">配置节名称</param>
    private static IServiceCollection ConfigureFeishuOptions(this IServiceCollection services, IConfiguration configuration, string configurationSection)
    {
        services.Configure<FeishuOptions>(options =>
        {
            options.AppId = configuration[$"{configurationSection}:AppId"];
            options.AppSecret = configuration[$"{configurationSection}:AppSecret"];
        });

        return services;
    }

    /// <summary>
    /// 验证飞书选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    private static IServiceCollection ValidateFeishuOptions(this IServiceCollection services)
    {
        services.AddOptions<FeishuOptions>()
                .Validate(options => ValidateFeishuOptionsInternal(options), ValidationErrorMessage)
                .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// 内部验证飞书选项的方法
    /// </summary>
    /// <param name="options">飞书选项</param>
    /// <returns>验证结果</returns>
    private static bool ValidateFeishuOptionsInternal(FeishuOptions options) =>
        !string.IsNullOrEmpty(options.AppId) && !string.IsNullOrEmpty(options.AppSecret);

    /// <summary>
    /// 注册飞书 API 服务（核心方法）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    private static IServiceCollection AddFeishuApiServiceCore(this IServiceCollection services) =>
        services
            .AddSingleton<ITenantTokenManager, TenantTokenManager>()
            .AddSingleton<IAppTokenManager, AppTokenManager>()
            .AddSingleton<IUserTokenManager, UserTokenManager>()
            .AddOrganizationWebApiHttpClient()
            .AddMessageWebApiHttpClient();
}
