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
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.WebSocket.Handlers;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket服务建造者，用于简化服务注册配置
/// </summary>
public class FeishuWebSocketServiceBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Type> _handlerTypes = new();
    private bool _multiHandlerMode = true;
    private bool _configured = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="services">服务集合</param>
    internal FeishuWebSocketServiceBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// 从配置文件配置选项
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu:WebSocket"</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder ConfigureFrom(IConfiguration configuration, string? sectionName = null)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var section = sectionName ?? "Feishu:WebSocket";
        _services.Configure<FeishuWebSocketOptions>(options => configuration.GetSection(section).Bind(options));
        _services.AddFeishuServicesBuilder(configuration, "Feishu")
                 .AddTenantTokenManager()
                 .AddAuthenticationApi();
        return this;
    }

    /// <summary>
    /// 使用委托配置选项
    /// </summary>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder ConfigureOptions(Action<FeishuWebSocketOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        _services.Configure(configureOptions);
        return this;
    }

    /// <summary>
    /// 启用单处理器模式
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder UseSingleHandler()
    {
        _multiHandlerMode = false;
        return this;
    }

    /// <summary>
    /// 启用多处理器模式
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder UseMultiHandler()
    {
        _multiHandlerMode = true;
        return this;
    }

    /// <summary>
    /// 添加事件处理器
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder AddHandler<THandler>()
        where THandler : class, IFeishuEventHandler
    {
        _handlerTypes.Add(typeof(THandler));
        _services.AddSingleton<IFeishuEventHandler, THandler>();
        _services.AddSingleton<THandler>();
        return this;
    }

    /// <summary>
    /// 添加事件处理器实例
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="handlerInstance">处理器实例</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder AddHandler<THandler>(THandler handlerInstance)
        where THandler : class, IFeishuEventHandler
    {
        if (handlerInstance == null)
            throw new ArgumentNullException(nameof(handlerInstance));

        _handlerTypes.Add(typeof(THandler));
        _services.AddSingleton<IFeishuEventHandler>(_ => handlerInstance);
        _services.AddSingleton<THandler>(_ => handlerInstance);
        return this;
    }

    /// <summary>
    /// 添加事件处理器工厂
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="handlerFactory">处理器工厂</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder AddHandler<THandler>(Func<IServiceProvider, THandler> handlerFactory)
        where THandler : class, IFeishuEventHandler
    {
        if (handlerFactory == null)
            throw new ArgumentNullException(nameof(handlerFactory));

        _handlerTypes.Add(typeof(THandler));
        _services.AddSingleton<IFeishuEventHandler>(handlerFactory);
        _services.AddSingleton<THandler>(handlerFactory);
        return this;
    }

    /// <summary>
    /// 应用自定义配置操作
    /// </summary>
    /// <param name="configureAction">配置操作</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder Apply(Action<FeishuWebSocketServiceBuilder> configureAction)
    {
        if (configureAction == null)
            throw new ArgumentNullException(nameof(configureAction));

        configureAction(this);
        return this;
    }

    /// <summary>
    /// 构建并注册服务
    /// </summary>
    /// <returns>服务集合，支持链式调用</returns>
    public IServiceCollection Build()
    {
        if (_configured)
            throw new InvalidOperationException("Build() 方法只能调用一次");

        ValidateConfiguration();
        RegisterServices();
        _configured = true;

        return _services;
    }

    /// <summary>
    /// 验证配置
    /// </summary>
    private void ValidateConfiguration()
    {
        if (!_handlerTypes.Any())
        {
            throw new InvalidOperationException(
                "至少需要注册一个事件处理器。请使用 AddHandler<T>() 方法添加处理器。");
        }
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    private void RegisterServices()
    {
        // 配置处理器模式
        _services.Configure<FeishuWebSocketOptions>(options => options.EnableMultiHandlerMode = _multiHandlerMode);

        // 注册事件处理器工厂
        RegisterEventHandlerFactory();

        // 注册核心服务
        RegisterCoreServices();
    }

    /// <summary>
    /// 注册事件处理器工厂
    /// </summary>
    private void RegisterEventHandlerFactory()
    {
        var defaultHandlerType = _handlerTypes.First();

        if (_multiHandlerMode)
        {
            _services.AddSingleton<IFeishuEventHandlerFactory>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<MultiFeishuEventHandlerFactory>>();
                var handlers = serviceProvider.GetRequiredService<IEnumerable<IFeishuEventHandler>>()
                    .Where(h => _handlerTypes.Contains(h.GetType()))
                    .ToList();
                var defaultHandler = serviceProvider.GetRequiredService(defaultHandlerType) as IFeishuEventHandler;
                return new MultiFeishuEventHandlerFactory(logger, handlers, defaultHandler);
            });
        }
        else
        {
            _services.AddSingleton<IFeishuEventHandlerFactory>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<DefaultFeishuEventHandlerFactory>>();
                var handlers = serviceProvider.GetRequiredService<IEnumerable<IFeishuEventHandler>>()
                    .Where(h => _handlerTypes.Contains(h.GetType()))
                    .ToList();
                var defaultHandler = serviceProvider.GetRequiredService(defaultHandlerType) as IFeishuEventHandler;
                return new DefaultFeishuEventHandlerFactory(logger, handlers, defaultHandler);
            });
        }
    }

    /// <summary>
    /// 注册核心服务
    /// </summary>
    private void RegisterCoreServices()
    {
        // 注册WebSocket客户端
        _services.AddSingleton<IFeishuWebSocketClient>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<FeishuWebSocketClient>>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var eventHandlerFactory = serviceProvider.GetRequiredService<IFeishuEventHandlerFactory>();
            var options = serviceProvider.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;
            return new FeishuWebSocketClient(logger, eventHandlerFactory, loggerFactory, options);
        });

        // 注册WebSocket管理器
        _services.AddSingleton<IFeishuWebSocketManager, FeishuWebSocketManager>();

        // 添加后台服务
        _services.AddHostedService<FeishuWebSocketHostedService>();
    }
}