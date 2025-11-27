
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.Handlers;

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
    /// 使用建造者模式注册飞书WebSocket服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>WebSocket服务建造者</returns>
    public static FeishuWebSocketServiceBuilder AddFeishuWebSocketBuilder(this IServiceCollection services)
    {
        return new FeishuWebSocketServiceBuilder(services);
    }

    /// <summary>
    /// 注册单处理器模式的飞书WebSocket服务（简化版本）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebSocketServiceWithSingleHandler(
        this IServiceCollection services,
        Action<FeishuWebSocketOptions> configureOptions)
    {
        return services
            .AddFeishuWebSocketBuilder()
            .ConfigureOptions(configureOptions)
            .UseSingleHandler()
            .Build();
    }

    /// <summary>
    /// 注册单处理器模式的飞书WebSocket服务（带处理器类型）
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebSocketServiceWithSingleHandler<THandler>(
        this IServiceCollection services,
        Action<FeishuWebSocketOptions> configureOptions)
        where THandler : class, IFeishuEventHandler
    {
        return services
            .AddFeishuWebSocketBuilder()
            .ConfigureOptions(configureOptions)
            .UseSingleHandler()
            .AddHandler<THandler>()
            .Build();
    }

    /// <summary>
    /// 注册多处理器模式的飞书WebSocket服务（简化版本）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebSocketServiceWithMultiHandler(
        this IServiceCollection services,
        Action<FeishuWebSocketOptions> configureOptions)
    {
        return services
            .AddFeishuWebSocketBuilder()
            .ConfigureOptions(configureOptions)
            .UseMultiHandler()
            .Build();
    }

    /// <summary>
    /// 注册多处理器模式的飞书WebSocket服务（带处理器类型）
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebSocketServiceWithMultiHandler<THandler>(
        this IServiceCollection services,
        Action<FeishuWebSocketOptions> configureOptions)
        where THandler : class, IFeishuEventHandler
    {
        return services
            .AddFeishuWebSocketBuilder()
            .ConfigureOptions(configureOptions)
            .UseMultiHandler()
            .AddHandler<THandler>()
            .Build();
    }

    /// <summary>
    /// 注册用户自定义事件处理器（泛型版本）
    /// </summary>
    /// <typeparam name="THandler">处理器类型，必须实现IFeishuEventHandler接口</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuEventHandler<THandler>(this IServiceCollection services)
        where THandler : class, IFeishuEventHandler
    {
        // 注册为单例
        services.AddSingleton<IFeishuEventHandler, THandler>();
        services.AddSingleton<THandler>();

        return services;
    }

    /// <summary>
    /// 注册事件处理器实例
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="handlerImplementation">处理器实例工厂</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuEventHandler<THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> handlerImplementation)
        where THandler : class, IFeishuEventHandler
    {
        services.AddSingleton<IFeishuEventHandler>(handlerImplementation);
        services.AddSingleton<THandler>(handlerImplementation);

        return services;
    }

    /// <summary>
    /// 注册事件处理器实例（无依赖注入版本）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="handlerInstance">处理器实例</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuEventHandler<THandler>(
        this IServiceCollection services,
        THandler handlerInstance)
        where THandler : class, IFeishuEventHandler
    {
        if (handlerInstance == null)
            throw new ArgumentNullException(nameof(handlerInstance));

        services.AddSingleton<IFeishuEventHandler>(_ => handlerInstance);
        services.AddSingleton<THandler>(_ => handlerInstance);

        return services;
    }

    /// <summary>
    /// 注册飞书WebSocket核心服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    private static IServiceCollection AddFeishuWebSocketServiceCore(this IServiceCollection services)
    {
        // 注册默认事件处理器（如果没有任何处理器被注册）
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFeishuEventHandler, DefaultFeishuEventHandler>());

        // 使用建造者模式注册核心服务
        return services
            .AddFeishuWebSocketBuilder()
            .UseMultiHandler() // 默认使用多处理器模式
            .AddHandler<DefaultFeishuEventHandler>()
            .Build();
    }

    /// <summary>
    /// 配置飞书WebSocket选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="configurationSection">配置节名称</param>
    private static IServiceCollection ConfigureFeishuWebSocketOptions(this IServiceCollection services, IConfiguration configuration, string configurationSection)
    {
        services.Configure<FeishuWebSocketOptions>(options => configuration.GetSection(configurationSection).Bind(options));
        return services;
    }
}
