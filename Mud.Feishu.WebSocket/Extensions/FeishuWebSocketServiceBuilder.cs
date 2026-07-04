// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.WebSocket;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书WebSocket服务建造者，用于简化服务注册配置
/// </summary>
public class FeishuWebSocketServiceBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Type> _handlerTypes = new();
    private readonly List<Type> _interceptorTypes = new();
    private bool _configured = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="services">服务集合</param>
    internal FeishuWebSocketServiceBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        // 注册配置验证器
        _services.AddSingleton<IValidateOptions<FeishuWebSocketOptions>, FeishuWebSocketOptionsValidator>();
    }

    /// <summary>
    /// 从配置文件配置选项
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"WebSocket"</param>
    /// <param name="appKey">应用键，默认为 "default"</param>
    /// <returns>建造者实例，支持链式调用</returns>
    /// <remarks>
    /// 注意：使用此方法前需要先注册多应用支持（AddFeishuApp）。
    /// </remarks>
    public FeishuWebSocketServiceBuilder ConfigureFrom(
        IConfiguration configuration,
        string sectionName = "FeishuWebSocket",
        string appKey = "default")
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("应用键不能为空", nameof(appKey));

        var section = sectionName ?? "WebSocket";
        _services.Configure<FeishuWebSocketOptions>(options => configuration.GetSection(section).Bind(options));
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
    /// 添加事件拦截器
    /// </summary>
    /// <typeparam name="TInterceptor">拦截器类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder AddInterceptor<TInterceptor>()
        where TInterceptor : class, IFeishuEventInterceptor
    {
        _interceptorTypes.Add(typeof(TInterceptor));
        _services.AddSingleton<IFeishuEventInterceptor, TInterceptor>();
        _services.AddSingleton<TInterceptor>();
        return this;
    }

    /// <summary>
    /// 添加事件拦截器实例
    /// </summary>
    /// <typeparam name="TInterceptor">拦截器类型</typeparam>
    /// <param name="interceptorInstance">拦截器实例</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder AddInterceptor<TInterceptor>(TInterceptor interceptorInstance)
        where TInterceptor : class, IFeishuEventInterceptor
    {
        if (interceptorInstance == null)
            throw new ArgumentNullException(nameof(interceptorInstance));

        _interceptorTypes.Add(typeof(TInterceptor));
        _services.AddSingleton<IFeishuEventInterceptor>(_ => interceptorInstance);
        _services.AddSingleton<TInterceptor>(_ => interceptorInstance);
        return this;
    }

    /// <summary>
    /// 添加事件拦截器工厂
    /// </summary>
    /// <typeparam name="TInterceptor">拦截器类型</typeparam>
    /// <param name="interceptorFactory">拦截器工厂</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder AddInterceptor<TInterceptor>(Func<IServiceProvider, TInterceptor> interceptorFactory)
        where TInterceptor : class, IFeishuEventInterceptor
    {
        if (interceptorFactory == null)
            throw new ArgumentNullException(nameof(interceptorFactory));

        _interceptorTypes.Add(typeof(TInterceptor));
        _services.AddSingleton<IFeishuEventInterceptor>(interceptorFactory);
        _services.AddSingleton<TInterceptor>(interceptorFactory);
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

        _services.AddSingleton<IFeishuEventHandlerFactory>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<DefaultFeishuEventHandlerFactory>>();
            var handlers = serviceProvider.GetRequiredService<IEnumerable<IFeishuEventHandler>>()
                .Where(h => _handlerTypes.Contains(h.GetType()))
                .ToList();
            var defaultHandler = serviceProvider.GetService(defaultHandlerType) as IFeishuEventHandler
                ?? handlers.FirstOrDefault(h => h.GetType() == defaultHandlerType);
            return new DefaultFeishuEventHandlerFactory(logger, handlers, defaultHandler!);
        });

        // 注册事件拦截器集合（单例，按注册顺序排序）
        _services.AddSingleton<IFeishuEventInterceptor[]>(serviceProvider =>
        {
            return serviceProvider.GetRequiredService<IEnumerable<IFeishuEventInterceptor>>()
                .Where(i => _interceptorTypes.Contains(i.GetType()))
                .OrderBy(i => _interceptorTypes.IndexOf(i.GetType()))
                .ToArray();
        });
    }

    /// <summary>
    /// 注册核心服务
    /// </summary>
    private void RegisterCoreServices()
    {
        // 注册事件去重服务（单例，如果未手动注册分布式去重则使用内存实现）
        if (!_services.Any(s => s.ServiceType == typeof(IFeishuEventDeduplicator)))
        {
            _services.AddSingleton<IFeishuEventDeduplicator>(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var options = serviceProvider.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;
                var logger = loggerFactory.CreateLogger<FeishuEventDeduplicator>();

                return new FeishuEventDeduplicator(
                    logger,
                    options.EventDeduplication.CacheExpiration,
                    options.EventDeduplication.CleanupInterval);
            });
        }

        // 注册SeqID去重服务（单例，如果未手动注册则使用内存实现）
        if (!_services.Any(s => s.ServiceType == typeof(IFeishuSeqIDDeduplicator)))
        {
            _services.AddSingleton<IFeishuSeqIDDeduplicator, FeishuSeqIDDeduplicator>();
        }

        // 注册重连策略（单例，如果未手动注册则使用指数退避策略）
        if (!_services.Any(s => s.ServiceType == typeof(IReconnectStrategy)))
        {
            _services.AddSingleton<IReconnectStrategy>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;
                var logger = serviceProvider.GetService<ILogger<ExponentialBackoffReconnectStrategy>>();
                return new ExponentialBackoffReconnectStrategy(options, logger);
            });
        }

        // 注册重连协调器（单例）
        _services.AddSingleton<IReconnectionOrchestrator>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ReconnectionOrchestrator>>();
            var strategy = serviceProvider.GetRequiredService<IReconnectStrategy>();
            var manager = serviceProvider.GetRequiredService<IFeishuWebSocketManager>();
            var options = serviceProvider.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;
            return new ReconnectionOrchestrator(logger, strategy, manager, options);
        });

        // 注册SessionManager（单例）
        if (!_services.Any(s => s.ServiceType == typeof(SessionManager)))
        {
            _services.AddSingleton<SessionManager>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<SessionManager>>();
                var options = serviceProvider.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;
                return new SessionManager(logger, options);
            });
        }

        // 注册MessageSequenceValidator（单例）
        if (!_services.Any(s => s.ServiceType == typeof(MessageSequenceValidator)))
        {
            _services.AddSingleton<MessageSequenceValidator>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<MessageSequenceValidator>>();
                var options = serviceProvider.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;
                return new MessageSequenceValidator(logger, options);
            });
        }

        // 注册WebSocket客户端
        _services.AddSingleton<IFeishuWebSocketClient>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<FeishuWebSocketClient>>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var eventHandlerFactory = serviceProvider.GetRequiredService<IFeishuEventHandlerFactory>();
            var interceptors = serviceProvider.GetRequiredService<IFeishuEventInterceptor[]>();
            var options = serviceProvider.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;
            var seqIdDeduplicator = serviceProvider.GetService<IFeishuSeqIDDeduplicator>();
            var sessionManager = serviceProvider.GetService<SessionManager>();
            var sequenceValidator = serviceProvider.GetService<MessageSequenceValidator>();
            return new FeishuWebSocketClient(logger, eventHandlerFactory, loggerFactory, interceptors, options, seqIdDeduplicator, sessionManager, sequenceValidator);
        });

        // 注册WebSocket管理器
        _services.AddSingleton<IFeishuWebSocketManager, FeishuWebSocketManager>();

        // 设置 WebSocket 连接数提供器
        Mud.Feishu.Abstractions.Metrics.FeishuMetrics.WebSocketConnectionCountProvider = () => WebSocketConnectionManager.ConnectionCount;

        // 添加后台服务
        _services.AddHostedService<FeishuWebSocketHostedService>();

#if NET8_0_OR_GREATER
        // 注册健康检查（仅在 .NET 8+ 框架可用）
        _services.AddSingleton<FeishuWebSocketHealthCheck>();
#endif
    }
}