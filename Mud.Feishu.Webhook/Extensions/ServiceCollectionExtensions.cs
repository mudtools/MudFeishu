// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 使用建造者模式注册飞书 Webhook 事件接收与处理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>Webhook 服务建造者</returns>
    private static FeishuWebhookServiceBuilder CreateFeishuWebhookBuilder(this IServiceCollection services)
    {
        return new FeishuWebhookServiceBuilder(services);
    }

    /// <summary>
    /// 添加飞书 Webhook 事件接收与处理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"FeishuWebhook"</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static FeishuWebhookServiceBuilder CreateFeishuWebhookServiceBuilder(
        this IServiceCollection services,
        IConfiguration configuration,
        string? sectionName = null)
    {
        return services.CreateFeishuWebhookBuilder()
                       .ConfigureFrom(configuration, sectionName);
    }

    /// <summary>
    /// 添加飞书 Webhook 事件接收与处理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>服务集合</returns>
    public static FeishuWebhookServiceBuilder CreateFeishuWebhookServiceBuilder(
        this IServiceCollection services,
        Action<FeishuWebhookOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        return services.CreateFeishuWebhookBuilder()
                       .ConfigureOptions(configureOptions);
    }

    /// <summary>
    /// 添加飞书 Webhook 事件接收与处理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="sectionName">配置节名称</param>
    /// <returns>服务集合</returns>
    /// <exception cref="InvalidOperationException">未在服务集合中找到 <see cref="IConfiguration"/> 注册。</exception>
    public static FeishuWebhookServiceBuilder CreateFeishuWebhookServiceBuilder(
        this IServiceCollection services,
        string sectionName = "FeishuWebhook")
    {
        // P-1 修复：移除 BuildServiceProvider() 反模式（会在 ConfigureServices 阶段创建临时容器，
        // 导致单例被多次实例化、配置变更不生效、实现 IDisposable 的单例资源泄漏）。
        // 改为直接从已注册的服务描述符中提取 IConfiguration 单例实例。
        var configuration = services
            .FirstOrDefault(d => d.ServiceType == typeof(IConfiguration))?
            .ImplementationInstance as IConfiguration
            ?? throw new InvalidOperationException(
                "未找到 IConfiguration 注册。请使用接受 IConfiguration 参数的 CreateFeishuWebhookServiceBuilder 重载，" +
                "或确保在调用此方法前已注册 IConfiguration（如通过 AddFeishuApp 或框架默认注册）。");
        return services.CreateFeishuWebhookServiceBuilder(configuration, sectionName);
    }
}
