
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket服务注册扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string DefaultConfigurationSection = "Feishu:WebSocket";

    /// <summary>
    /// 注册飞书WebSocket服务（使用配置节）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static FeishuWebSocketServiceBuilder AddFeishuWebSocketServiceBuilder(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddFeishuWebSocketServiceBuilder(configuration, DefaultConfigurationSection);
    }

    /// <summary>
    /// 注册飞书WebSocket服务（使用选项）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static FeishuWebSocketServiceBuilder AddFeishuWebSocketServiceBuilder(this IServiceCollection services, Action<FeishuWebSocketOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        var builder = new FeishuWebSocketServiceBuilder(services);
        return builder.ConfigureOptions(configureOptions);
    }

    /// <summary>
    /// 使用建造者模式注册飞书WebSocket服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"WebSocket"</param>
    /// <returns>WebSocket服务建造者</returns>
    public static FeishuWebSocketServiceBuilder AddFeishuWebSocketServiceBuilder(this IServiceCollection services, IConfiguration configuration, string? sectionName = null)
    {
        var builder = new FeishuWebSocketServiceBuilder(services);
        return builder.ConfigureFrom(configuration, sectionName);
    }
}
