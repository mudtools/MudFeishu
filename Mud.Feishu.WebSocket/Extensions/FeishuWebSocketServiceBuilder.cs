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
using Mud.Feishu.Abstractions.Extensions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.WebSocket;
using System.Diagnostics.CodeAnalysis;
using Mud.Feishu.WebSocket.Handlers;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书WebSocket服务建造者，用于简化服务注册配置
/// </summary>
public class FeishuWebSocketServiceBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Type> _handlerTypes = new();
    private readonly List<Type> _interceptorTypes = new();
    // WS-04 修复：区分「类型注册」与「实例注册」两个集合。
    // 实例注册的处理器由 ScopedFeishuEventHandlerFactory 直接复用，不经过 DI 作用域解析，
    // 避免用户提供的实例被 DI 容器 Dispose（P1-6）。
    private readonly List<IFeishuEventHandler> _handlerInstances = new();
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
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式配置绑定（Configure<TOptions>）在裁剪下无法静态分析配置类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式配置绑定（Configure<TOptions>）在 AOT/动态代码生成环境下不可用")]
#endif
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
        // 使用 IConfigurationSection 重载绑定，确保 IOptionsMonitor<T> 能正确接收配置变更通知
        _services.Configure<FeishuWebSocketOptions>(configuration.GetSection(section));
        // R4：配置 JSON 兼容——扁平重连/证书键回填到 Reconnect/Certificate
        _services.Configure<FeishuWebSocketOptions>(o =>
        {
            o.ApplyLegacyFlatKeys(configuration.GetSection(section));
        });
        // 设置 AppKey 用于指标维度区分
        _services.Configure<FeishuWebSocketOptions>(o => o.AppKey = appKey);
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
    public FeishuWebSocketServiceBuilder AddHandler<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        THandler>()
        where THandler : class, IFeishuEventHandler
    {
        _handlerTypes.Add(typeof(THandler));
        // 处理器注册为 Scoped：由 ScopedFeishuEventHandlerFactory 在每次事件分发时
        // 创建独立 IServiceScope 解析并释放，保证处理器及其 Scoped 依赖（如 DbContext）
        // 的生命周期与单个事件对齐（P0-7）。
        _services.AddScoped<IFeishuEventHandler, THandler>();
        _services.AddScoped<THandler>();
        return this;
    }

    /// <summary>
    /// 添加事件处理器实例
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="handlerInstance">处理器实例</param>
    /// <returns>建造者实例，支持链式调用</returns>
    /// <remarks>
    /// WS-04 修复（P1-6）：用户提供的处理器实例不经过 DI 容器解析，
    /// 由 <see cref="ScopedFeishuEventHandlerFactory"/> 直接复用。
    /// 这避免了将实例注册为 Scoped 后，首个事件处理完成时 IServiceScope.Dispose
    /// 会连带释放用户实例的问题（Captive Dependency 的反面：实例被容器意外回收）。
    /// </remarks>
    public FeishuWebSocketServiceBuilder AddHandler<THandler>(THandler handlerInstance)
        where THandler : class, IFeishuEventHandler
    {
        if (handlerInstance == null)
            throw new ArgumentNullException(nameof(handlerInstance));

        _handlerTypes.Add(typeof(THandler));
        _handlerInstances.Add(handlerInstance);
        // 不再将用户实例注册到 DI 容器，避免容器 Dispose 回收用户实例
        return this;
    }

    /// <summary>
    /// 添加事件处理器工厂
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="handlerFactory">处理器工厂</param>
    /// <returns>建造者实例，支持链式调用</returns>
    /// <remarks>
    /// 工厂委托注册为 Scoped，每次事件分发时由 DI 在独立作用域内调用，
    /// 与 Webhook 模块保持一致。
    /// </remarks>
    public FeishuWebSocketServiceBuilder AddHandler<THandler>(Func<IServiceProvider, THandler> handlerFactory)
        where THandler : class, IFeishuEventHandler
    {
        if (handlerFactory == null)
            throw new ArgumentNullException(nameof(handlerFactory));

        _handlerTypes.Add(typeof(THandler));
        _services.AddScoped<IFeishuEventHandler>(handlerFactory);
        _services.AddScoped<THandler>(handlerFactory);
        return this;
    }

    /// <summary>
    /// 添加事件拦截器
    /// </summary>
    /// <typeparam name="TInterceptor">拦截器类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebSocketServiceBuilder AddInterceptor<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TInterceptor>()
        where TInterceptor : class, IFeishuEventInterceptor
    {
        _interceptorTypes.Add(typeof(TInterceptor));
        // 注册为 Singleton：拦截器是横切关注点组件（日志/指标/审计），按约定无请求级状态。
        // 若注册为 Scoped，会被同样为 Singleton 的 IFeishuEventInterceptor[] 消费者
        // 从根容器解析，构成 Captive Dependency（Development 环境下启动即抛异常）。
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
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
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
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
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
    /// <remarks>
    /// P0-7 修复：此前这里在 Singleton 工厂委托中直接通过根容器解析 Scoped 的
    /// <see cref="IFeishuEventHandler"/>，构成 Captive Dependency：
    /// Development 环境（默认开启 <c>ValidateScopes</c>）下应用启动即抛异常；
    /// 其它环境下处理器被单例永久持有，Scoped 依赖（如 DbContext）生命周期与实例均被破坏。
    /// 现改为注册 <see cref="ScopedFeishuEventHandlerFactory"/>，
    /// 由其在每次事件分发时创建并释放 <see cref="IServiceScope"/>。
    /// </remarks>
    private void RegisterEventHandlerFactory()
    {
        var defaultHandlerType = _handlerTypes.FirstOrDefault();
        var handlerTypes = _handlerTypes.ToArray();
        var handlerInstances = _handlerInstances.ToArray();

        _services.AddSingleton<IFeishuEventHandlerFactory>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ScopedFeishuEventHandlerFactory>>();
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            return new ScopedFeishuEventHandlerFactory(
                logger, scopeFactory, handlerTypes, defaultHandlerType, handlerInstances);
        });

        // 注册事件拦截器集合（单例，按注册顺序排序）
        // 说明：拦截器是横切关注点组件（日志、指标、审计），按约定无请求级状态，
        // 因此注册为 Singleton；若注册为 Scoped 会与 Singleton 消费者构成 Captive Dependency。
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
#if NET8_0_OR_GREATER
        // P1-9 修复：必须在任何 JSON 序列化/反序列化发生之前，
        // 将 WebSocketJsonContext 合并到 FeishuJsonDefaults 的 resolver 链。
        // 此前该调用在本模块内缺失（Webhook 模块已在 Builder 内调用），
        // 导致 AuthResponseMessage / PingMessage / PongMessage 等协议类型在
        // net8+ 上只能落到反射兜底，Native AOT/Trimming 下存在失败风险。
        Mud.Feishu.WebSocket.Extensions.FeishuWebSocketJsonResolverExtensions.ConfigureWebSocketResolver();
#endif

        // 注册事件去重服务（单例，根据 EventDeduplication.Mode 选择实现）
        // C1：FeishuDeduplication 新节存在时 Mode/Ttl 字段级优先
        if (!_services.Any(s => s.ServiceType == typeof(IFeishuEventDeduplicator)))
        {
            _services.AddFeishuDeduplicationOptions();
            _services.AddSingleton<IFeishuEventDeduplicator>(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var options = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>().CurrentValue;
                var unified = serviceProvider.GetService<IOptions<Mud.Feishu.Abstractions.Configuration.FeishuDeduplicationOptions>>()?.Value;
                var unifiedActive = unified is { IsConfiguredFromConfiguration: true };

                var mode = unifiedActive
                    ? (unified!.Mode ?? Mud.Feishu.Abstractions.Configuration.FeishuDeduplicationOptions.ModeInMemory)
                    : options.EventDeduplication.Mode.ToString();

                var cacheExpiration = unifiedActive && unified!.Event?.Ttl is { } uTtl && uTtl > TimeSpan.Zero
                    ? uTtl
                    : options.EventDeduplication.CacheExpiration;
                var cleanupInterval = unifiedActive && unified!.Event?.CleanupInterval is { } uCl && uCl > TimeSpan.Zero
                    ? uCl
                    : options.EventDeduplication.CleanupInterval;
                var processingTimeout = unifiedActive && unified!.Event?.ProcessingTimeout is { } uPt && uPt > TimeSpan.Zero
                    ? uPt
                    : options.EventDeduplication.ProcessingTimeout;
                var maxCacheSize = unifiedActive && unified!.Event?.MaxCacheSize is { } uMs
                    ? uMs
                    : options.EventDeduplication.MaxCacheSize;

                // None 模式：注册空实现，不进行去重
                if (string.Equals(mode, "None", StringComparison.OrdinalIgnoreCase)
                    || (!unifiedActive && mode == EventDeduplicationMode.None.ToString()))
                {
                    var noopLogger = loggerFactory.CreateLogger<NoopFeishuEventDeduplicator>();
                    return new NoopFeishuEventDeduplicator(noopLogger);
                }

                var logger = loggerFactory.CreateLogger<FeishuEventDeduplicator>();

                // Distributed 模式：检测是否已注册分布式实现，未注册时记录警告并降级为内存实现
                if (string.Equals(mode, "Distributed", StringComparison.OrdinalIgnoreCase)
                    || (!unifiedActive && mode == EventDeduplicationMode.Distributed.ToString()))
                {
                    logger.LogWarning(
                        "EventDeduplication.Mode=Distributed 但未注册 IFeishuEventDeduplicator 的分布式实现，降级为内存去重。" +
                        "请通过 services.AddSingleton<IFeishuEventDeduplicator, RedisFeishuEventDistributedDeduplicator>() 注册 Redis 实现");
                }

                return new FeishuEventDeduplicator(
                    logger,
                    cacheExpiration,
                    cleanupInterval,
                    processingTimeout,
                    maxCacheSize);
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
                var options = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>().CurrentValue;
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
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>().CurrentValue;
            return new ReconnectionOrchestrator(logger, strategy, manager, options);
        });

        // 注册SessionManager（单例）
        if (!_services.Any(s => s.ServiceType == typeof(SessionManager)))
        {
            _services.AddSingleton<SessionManager>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<SessionManager>>();
                var options = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>().CurrentValue;
                return new SessionManager(logger, options);
            });
        }

        // 注册MessageSequenceValidator（单例）
        if (!_services.Any(s => s.ServiceType == typeof(MessageSequenceValidator)))
        {
            _services.AddSingleton<MessageSequenceValidator>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<MessageSequenceValidator>>();
                var options = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>().CurrentValue;
                return new MessageSequenceValidator(logger, options);
            });
        }

        // WS-03 修复：注册并发控制服务（背压闸门），与 Webhook 模块对齐
        _services.AddSingleton<FeishuWebSocketConcurrencyService>();
        _services.AddHostedService<FeishuWebSocketConcurrencyService>(
            serviceProvider => serviceProvider.GetRequiredService<FeishuWebSocketConcurrencyService>());

        // 注册WebSocket客户端
        // F4 修复：改为注入 IOptionsMonitor<FeishuWebSocketOptions>，保证配置热更新一致性。
        // 此前一次性捕获 CurrentValue，FeishuWebSocketManager 每次读取 → 两者行为不一致。
        // 说明：该 AddSingleton 工厂 lambda 无法直接添加 Requires 标注，此处用 pragma 屏蔽
        // FeishuWebSocketClient 构造函数（带标注）反射式序列化的 IL 警告。
#pragma warning disable IL2026, IL3050
        _services.AddSingleton<IFeishuWebSocketClient>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<FeishuWebSocketClient>>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var eventHandlerFactory = serviceProvider.GetRequiredService<IFeishuEventHandlerFactory>();
            var interceptors = serviceProvider.GetRequiredService<IFeishuEventInterceptor[]>();
            // F4：传入 IOptionsMonitor 而非 CurrentValue 快照
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();
            var seqIdDeduplicator = serviceProvider.GetService<IFeishuSeqIDDeduplicator>();
            var sessionManager = serviceProvider.GetService<SessionManager>();
            var sequenceValidator = serviceProvider.GetService<MessageSequenceValidator>();
            // P1-5 修复：注入事件级去重器。此前 FeishuWebSocketClient 不接受该依赖，
            // 导致 EventDeduplication 配置（默认 InMemory）在 WebSocket 路径上完全失效。
            var eventDeduplicator = serviceProvider.GetService<IFeishuEventDeduplicator>();
            // WS-03：注入并发控制服务
            var concurrencyService = serviceProvider.GetRequiredService<FeishuWebSocketConcurrencyService>();
            // F7 修复：注入统一去重中间件（可选）
            var unifiedDedupMiddleware = serviceProvider.GetService<IUnifiedDeduplicationMiddleware>();
            return new FeishuWebSocketClient(
                logger, eventHandlerFactory, loggerFactory,
                eventDeduplicator, interceptors, options: optionsMonitor.CurrentValue,
                seqIdDeduplicator, sessionManager, sequenceValidator,
                concurrencyService, optionsMonitor, unifiedDedupMiddleware);
        });
#pragma warning restore IL2026, IL3050

        // 注册WebSocket管理器
        _services.AddSingleton<IFeishuWebSocketManager, FeishuWebSocketManager>();

        // P1-5/P1-6 修复：WebSocket 指标观察器在 FeishuWebSocketHostedService 中初始化，
        // 以便从 IOptionsMonitor<FeishuWebSocketOptions> 解析实际 AppKey，
        // 避免此处硬编码 "websocket" 字面量导致多应用指标无法区分。
        // 此处仅注册服务，观察器赋值延迟到 hosted service 构造时执行（DI 已就绪）。

        // P1-8 修复：AddHostedService<T>() 只会注册 IHostedService→T 的描述符，
        // MS.DI 不会按具体类型 T 解析。FeishuWebSocketHealthCheck 的构造函数依赖
        // 具体类型 FeishuWebSocketHostedService，若不显式注册该实现类型，
        // 解析健康检查时必然抛 "Unable to resolve service for type ..."。
        // 这里先注册实现类型，再让 AddHostedService 复用同一实例，保证单例唯一。
        _services.AddSingleton<FeishuWebSocketHostedService>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<FeishuWebSocketHostedService>>();
            var manager = serviceProvider.GetRequiredService<IFeishuWebSocketManager>();
            var orchestrator = serviceProvider.GetRequiredService<IReconnectionOrchestrator>();
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();
            var concurrency = serviceProvider.GetRequiredService<FeishuWebSocketConcurrencyService>();
            return new FeishuWebSocketHostedService(logger, manager, orchestrator, options, concurrency);
        });
        _services.AddHostedService<FeishuWebSocketHostedService>(
            serviceProvider => serviceProvider.GetRequiredService<FeishuWebSocketHostedService>());

        // 注册健康检查（全目标框架可用）
        // F2/F3：健康检查依赖 IReconnectionOrchestrator 获取熔断状态
        _services.AddSingleton<FeishuWebSocketHealthCheck>(serviceProvider =>
        {
            var hostedService = serviceProvider.GetRequiredService<FeishuWebSocketHostedService>();
            var orchestrator = serviceProvider.GetRequiredService<IReconnectionOrchestrator>();
            var logger = serviceProvider.GetService<ILogger<FeishuWebSocketHealthCheck>>();
            return new FeishuWebSocketHealthCheck(hostedService, orchestrator, logger);
        });
    }
}