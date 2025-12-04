// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书服务集合建造者扩展方法
/// </summary>
public static class FeishuServiceCollectionBuilderExtensions
{
    /// <summary>
    /// 创建飞书服务建造者
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>飞书服务建造者实例</returns>
    public static FeishuServiceBuilder AddFeishuServices(this IServiceCollection services)
    {
        return new FeishuServiceBuilder(services);
    }

    /// <summary>
    /// 使用配置文件创建飞书服务建造者
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>飞书服务建造者实例</returns>
    public static FeishuServiceBuilder AddFeishuServicesBuilder(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        return services.AddFeishuServices().ConfigureFrom(configuration, sectionName);
    }

    /// <summary>
    /// 使用代码配置创建飞书服务建造者
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>飞书服务建造者实例</returns>
    public static FeishuServiceBuilder AddFeishuServicesBuilder(this IServiceCollection services, Action<FeishuOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        return services.AddFeishuServices().ConfigureOptions(configureOptions);
    }

    /// <summary>
    /// 快速注册飞书令牌管理服务（单模块注册）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuTokenManagers(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        return services.AddFeishuServicesBuilder(configuration, sectionName)
                     .AddTokenManagers()
                     .Build();
    }

    /// <summary>
    /// 快速注册飞书组织管理服务（单模块注册）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuOrganizationApi(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        return services.AddFeishuServicesBuilder(configuration, sectionName)
                     .AddOrganizationApi()
                     .Build();
    }

    /// <summary>
    /// 快速注册飞书消息管理服务（单模块注册）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuMessageApi(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        return services.AddFeishuServicesBuilder(configuration, sectionName)
                     .AddMessageApi()
                     .Build();
    }

    /// <summary>
    /// 快速注册飞书群聊管理服务（单模块注册）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuChatGroupApi(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        return services.AddFeishuServicesBuilder(configuration, sectionName)
                     .AddChatGroupApi()
                     .Build();
    }

    /// <summary>
    /// 快速注册飞书所有服务（全功能注册）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuAllServices(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        return services.AddFeishuServicesBuilder(configuration, sectionName)
                     .AddAllApis()
                     .Build();
    }

    /// <summary>
    /// 根据模块注册飞书服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="modules">要注册的模块</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuModules(this IServiceCollection services, IConfiguration configuration, FeishuModule[] modules, string sectionName = "Feishu")
    {
        if (modules == null || modules.Length == 0)
            throw new ArgumentException("至少需要指定一个模块", nameof(modules));

        return services.AddFeishuServicesBuilder(configuration, sectionName)
                     .AddModules(modules)
                     .Build();
    }

    /// <summary>
    /// 保持向后兼容的完整服务注册方法
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuApiService(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        return services.AddFeishuAllServices(configuration, sectionName);
    }

    /// <summary>
    /// 保持向后兼容的完整服务注册方法（使用选项配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuApiService(this IServiceCollection services, Action<FeishuOptions> configureOptions)
    {
        return services.AddFeishuServicesBuilder(configureOptions)
                     .AddAllApis()
                     .Build();
    }
}