
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket服务注册扩展方法
/// </summary>
public static class FeishuWebSocketServiceCollectionExtensions
{
    private const string DefaultConfigurationSection = "Feishu:WebSocket";

    /// <summary>
    /// 注册飞书WebSocket服务（使用配置节）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebSocketService(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddFeishuWebSocketService(configuration, DefaultConfigurationSection);
    }

    /// <summary>
    /// 注册飞书WebSocket服务（使用配置节名称）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="configurationSection">配置节名称，默认为"Feishu:WebSocket"</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebSocketService(this IServiceCollection services, IConfiguration configuration, string configurationSection)
    {
        services.ConfigureFeishuWebSocketOptions(configuration, configurationSection);
        return services.AddFeishuWebSocketServiceCore();
    }

    /// <summary>
    /// 注册飞书WebSocket服务（使用选项）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebSocketService(this IServiceCollection services, Action<FeishuWebSocketOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        services.Configure(configureOptions);
        return services.AddFeishuWebSocketServiceCore();
    }

    /// <summary>
    /// 配置飞书WebSocket选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="configurationSection">配置节名称</param>
    private static IServiceCollection ConfigureFeishuWebSocketOptions(this IServiceCollection services, IConfiguration configuration, string configurationSection)
    {
        services.Configure<FeishuWebSocketOptions>(configuration.GetSection(configurationSection));
        return services;
    }

    /// <summary>
    /// 注册飞书WebSocket服务（核心方法）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    private static IServiceCollection AddFeishuWebSocketServiceCore(this IServiceCollection services)
    {
        // 注册WebSocket客户端，确保FeishuWebSocketOptions正确传递
        services.AddSingleton<IFeishuWebSocketClient>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<FeishuWebSocketClient>>();
            var options = serviceProvider.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;
            return new FeishuWebSocketClient(logger, options);
        });

        // 注册WebSocket管理器
        services.AddSingleton<IFeishuWebSocketManager, FeishuWebSocketManager>();

        // 添加后台服务，用于自动启动和管理WebSocket连接
        services.AddHostedService<FeishuWebSocketHostedService>();

        return services;
    }
}
