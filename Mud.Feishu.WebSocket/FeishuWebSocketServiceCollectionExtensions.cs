
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
using Mud.Feishu.WebSocket.Handlers;

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
    /// 注册单处理器模式的飞书WebSocket服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    /// <exception cref="InvalidOperationException">当没有注册任何事件处理器时抛出</exception>
    public static IServiceCollection AddFeishuWebSocketServiceWithSingleHandler(
        this IServiceCollection services,
        Action<FeishuWebSocketOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // 配置选项为单处理器模式
        services.Configure(configureOptions);
        services.Configure<FeishuWebSocketOptions>(options => options.EnableMultiHandlerMode = false);

        // 注册核心服务（在Core方法中会验证处理器是否已注册）
        RegisterCoreServices(services, typeof(IFeishuEventHandlerFactory));

        return services;
    }

    /// <summary>
    /// 注册单处理器模式的飞书WebSocket服务（带自定义处理器）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebSocketServiceWithSingleHandler<THandler>(
        this IServiceCollection services,
        Action<FeishuWebSocketOptions> configureOptions)
        where THandler : class, IFeishuEventHandler
    {
        return services.AddFeishuWebSocketServiceWithSingleHandler<THandler, THandler>(configureOptions);
    }

    /// <summary>
    /// 注册单处理器模式的飞书WebSocket服务（带自定义处理器和默认处理器）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebSocketServiceWithSingleHandler<THandler, TDefaultHandler>(
        this IServiceCollection services,
        Action<FeishuWebSocketOptions> configureOptions)
        where THandler : class, IFeishuEventHandler
        where TDefaultHandler : class, IFeishuEventHandler
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // 配置选项为单处理器模式
        services.Configure(configureOptions);
        services.Configure<FeishuWebSocketOptions>(options => options.EnableMultiHandlerMode = false);

        // 事件处理器必须由用户自己注册
        services.AddFeishuEventHandler<THandler>();
        services.AddFeishuEventHandler<TDefaultHandler>();

        // 注册单处理器工厂
        RegisterSingleHandlerFactory<TDefaultHandler>(services);

        // 注册其他核心服务
        RegisterCoreServices(services, typeof(IFeishuEventHandlerFactory));

        return services;
    }



    /// <summary>
    /// 注册多处理器模式的飞书WebSocket服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    /// <exception cref="InvalidOperationException">当没有注册任何事件处理器时抛出</exception>
    public static IServiceCollection AddFeishuWebSocketServiceWithMultiHandler(
        this IServiceCollection services,
        Action<FeishuWebSocketOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // 配置选项为多处理器模式
        services.Configure(configureOptions);
        services.Configure<FeishuWebSocketOptions>(options => options.EnableMultiHandlerMode = true);

        // 注册核心服务（在Core方法中会验证处理器是否已注册）
        RegisterCoreServices(services, typeof(IFeishuEventHandlerFactory));

        return services;
    }

    /// <summary>
    /// 注册多处理器模式的飞书WebSocket服务（带自定义处理器）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebSocketServiceWithMultiHandler<THandler>(
        this IServiceCollection services,
        Action<FeishuWebSocketOptions> configureOptions)
        where THandler : class, IFeishuEventHandler
    {
        return services.AddFeishuWebSocketServiceWithMultiHandler<THandler, THandler>(configureOptions);
    }

    /// <summary>
    /// 注册多处理器模式的飞书WebSocket服务（带自定义处理器和默认处理器）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuWebSocketServiceWithMultiHandler<THandler, TDefaultHandler>(
        this IServiceCollection services,
        Action<FeishuWebSocketOptions> configureOptions)
        where THandler : class, IFeishuEventHandler
        where TDefaultHandler : class, IFeishuEventHandler
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // 配置选项为多处理器模式
        services.Configure(configureOptions);
        services.Configure<FeishuWebSocketOptions>(options => options.EnableMultiHandlerMode = true);

        // 事件处理器必须由用户自己注册
        services.AddFeishuEventHandler<THandler>();
        services.AddFeishuEventHandler<TDefaultHandler>();

        // 注册多处理器工厂
        RegisterMultiHandlerFactory<TDefaultHandler>(services);

        // 注册其他核心服务
        RegisterCoreServices(services, typeof(IFeishuEventHandlerFactory));

        return services;
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
    /// 注册多个用户自定义事件处理器（泛型版本）
    /// </summary>
    /// <typeparam name="THandler1">第一个处理器类型</typeparam>
    /// <typeparam name="THandler2">第二个处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuEventHandlers<THandler1, THandler2>(this IServiceCollection services)
        where THandler1 : class, IFeishuEventHandler
        where THandler2 : class, IFeishuEventHandler
    {
        services.AddFeishuEventHandler<THandler1>();
        services.AddFeishuEventHandler<THandler2>();

        return services;
    }

    /// <summary>
    /// 注册多个用户自定义事件处理器（泛型版本）
    /// </summary>
    /// <typeparam name="THandler1">第一个处理器类型</typeparam>
    /// <typeparam name="THandler2">第二个处理器类型</typeparam>
    /// <typeparam name="THandler3">第三个处理器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection AddFeishuEventHandlers<THandler1, THandler2, THandler3>(this IServiceCollection services)
        where THandler1 : class, IFeishuEventHandler
        where THandler2 : class, IFeishuEventHandler
        where THandler3 : class, IFeishuEventHandler
    {
        services.AddFeishuEventHandler<THandler1>();
        services.AddFeishuEventHandler<THandler2>();
        services.AddFeishuEventHandler<THandler3>();

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
    /// 配置飞书WebSocket选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="configurationSection">配置节名称</param>
    private static IServiceCollection ConfigureFeishuWebSocketOptions(this IServiceCollection services, IConfiguration configuration, string configurationSection)
    {
        services.Configure<FeishuWebSocketOptions>(options => configuration.GetSection(configurationSection));
        return services;
    }





    /// <summary>
    /// 注册单处理器工厂
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    private static IServiceCollection RegisterSingleHandlerFactory<TDefaultHandler>(IServiceCollection services)
        where TDefaultHandler : class, IFeishuEventHandler
    {
        // 注册接口工厂
        services.AddSingleton<IFeishuEventHandlerFactory>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<FeishuEventHandlerFactory>>();
            var handlers = serviceProvider.GetRequiredService<IEnumerable<IFeishuEventHandler>>();
            var defaultHandler = serviceProvider.GetRequiredService<TDefaultHandler>();
            return new FeishuEventHandlerFactory(logger, handlers, defaultHandler);
        });

        // 注册具体工厂类型
        services.AddSingleton<FeishuEventHandlerFactory>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<FeishuEventHandlerFactory>>();
            var handlers = serviceProvider.GetRequiredService<IEnumerable<IFeishuEventHandler>>();
            var defaultHandler = serviceProvider.GetRequiredService<TDefaultHandler>();
            return new FeishuEventHandlerFactory(logger, handlers, defaultHandler);
        });

        return services;
    }

    /// <summary>
    /// 注册多处理器工厂
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    private static IServiceCollection RegisterMultiHandlerFactory<TDefaultHandler>(IServiceCollection services)
        where TDefaultHandler : class, IFeishuEventHandler
    {
        // 注册接口工厂
        services.AddSingleton<IFeishuEventHandlerFactory>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<MultiFeishuEventHandlerFactory>>();
            var handlers = serviceProvider.GetRequiredService<IEnumerable<IFeishuEventHandler>>();
            var defaultHandler = serviceProvider.GetRequiredService<TDefaultHandler>();
            return new MultiFeishuEventHandlerFactory(logger, handlers, defaultHandler);
        });

        // 注册具体工厂类型
        services.AddSingleton<MultiFeishuEventHandlerFactory>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<MultiFeishuEventHandlerFactory>>();
            var handlers = serviceProvider.GetRequiredService<IEnumerable<IFeishuEventHandler>>();
            var defaultHandler = serviceProvider.GetRequiredService<TDefaultHandler>();
            return new MultiFeishuEventHandlerFactory(logger, handlers, defaultHandler);
        });

        return services;
    }

    /// <summary>
    /// 注册核心服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="factoryType">工厂类型</param>
    /// <returns>服务集合</returns>
    private static IServiceCollection RegisterCoreServices(IServiceCollection services, Type factoryType)
    {
        // 注册WebSocket客户端
        services.AddSingleton<IFeishuWebSocketClient>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<FeishuWebSocketClient>>();
            var eventHandlerFactory = serviceProvider.GetRequiredService(factoryType);
            var options = serviceProvider.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;
            return new FeishuWebSocketClient(logger, eventHandlerFactory as IFeishuEventHandlerFactory, options);
        });

        // 注册WebSocket管理器
        services.AddSingleton<IFeishuWebSocketManager, FeishuWebSocketManager>();

        // 添加后台服务
        services.AddHostedService<FeishuWebSocketHostedService>();

        return services;
    }

    /// <summary>
    /// 注册飞书WebSocket服务（核心方法）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    /// <exception cref="InvalidOperationException">当没有注册任何事件处理器时抛出</exception>
    private static IServiceCollection AddFeishuWebSocketServiceCore(this IServiceCollection services)
    {
        // 验证用户是否注册了事件处理器
        services.AddSingleton<IFeishuEventHandlerFactory>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;
            var handlers = serviceProvider.GetRequiredService<IEnumerable<IFeishuEventHandler>>();

            if (!handlers.Any())
            {
                throw new InvalidOperationException(
                    "未注册任何事件处理器。请使用 AddFeishuEventHandler<T>() 方法注册至少一个事件处理器，" +
                    "或使用 AddFeishuWebSocketServiceWithSingleHandler<T>() / AddFeishuWebSocketServiceWithMultiHandler<T>() 方法。");
            }

            // 使用第一个注册的处理器作为默认处理器
            var defaultHandler = handlers.First();

            // 根据配置选择工厂实现
            if (options.EnableMultiHandlerMode)
            {
                var logger = serviceProvider.GetRequiredService<ILogger<MultiFeishuEventHandlerFactory>>();
                return new MultiFeishuEventHandlerFactory(logger, handlers, defaultHandler);
            }
            else
            {
                var logger = serviceProvider.GetRequiredService<ILogger<FeishuEventHandlerFactory>>();
                return new FeishuEventHandlerFactory(logger, handlers, defaultHandler);
            }
        });

        // 注册核心服务
        RegisterCoreServices(services, typeof(IFeishuEventHandlerFactory));

        return services;
    }
}
