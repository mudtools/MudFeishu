// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书Webhook服务建造者，用于简化服务注册配置
/// </summary>
public class FeishuWebhookServiceBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Type> _handlerTypes = new();
    private bool _enableHealthChecks = true;
    private bool _enableMetrics = true;
    private bool _autoRegisterEndpoint = true;
    private bool _configured = false;
    private Action<FeishuWebhookOptions>? _configureOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="services">服务集合</param>
    internal FeishuWebhookServiceBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// 从配置文件配置选项
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"FeishuWebhook"</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder ConfigureFrom(IConfiguration configuration, string? sectionName = null)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var section = sectionName ?? "FeishuWebhook";
        _services.Configure<FeishuWebhookOptions>(options => configuration.GetSection(section).Bind(options));
        return this;
    }

    /// <summary>
    /// 使用委托配置选项
    /// </summary>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder ConfigureOptions(Action<FeishuWebhookOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        _configureOptions = configureOptions;
        return this;
    }

    /// <summary>
    /// 启用健康检查
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder EnableHealthChecks()
    {
        _enableHealthChecks = true;
        return this;
    }

    /// <summary>
    /// 禁用健康检查
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder DisableHealthChecks()
    {
        _enableHealthChecks = false;
        return this;
    }

    /// <summary>
    /// 启用性能指标收集
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder EnableMetrics()
    {
        _enableMetrics = true;
        return this;
    }

    /// <summary>
    /// 禁用性能指标收集
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder DisableMetrics()
    {
        _enableMetrics = false;
        return this;
    }

    /// <summary>
    /// 启用自动端点注册
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder EnableAutoEndpoint()
    {
        _autoRegisterEndpoint = true;
        return this;
    }

    /// <summary>
    /// 禁用自动端点注册
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder DisableAutoEndpoint()
    {
        _autoRegisterEndpoint = false;
        return this;
    }

    /// <summary>
    /// 添加事件处理器
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder AddHandler<THandler>()
        where THandler : class, IFeishuEventHandler
    {
        _handlerTypes.Add(typeof(THandler));
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
    public FeishuWebhookServiceBuilder AddHandler<THandler>(THandler handlerInstance)
        where THandler : class, IFeishuEventHandler
    {
        if (handlerInstance == null)
            throw new ArgumentNullException(nameof(handlerInstance));

        _handlerTypes.Add(typeof(THandler));
        _services.AddScoped<IFeishuEventHandler>(_ => handlerInstance);
        _services.AddScoped<THandler>(_ => handlerInstance);
        return this;
    }

    /// <summary>
    /// 添加事件处理器工厂
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="handlerFactory">处理器工厂</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder AddHandler<THandler>(Func<IServiceProvider, THandler> handlerFactory)
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
    /// 应用自定义配置操作
    /// </summary>
    /// <param name="configureAction">配置操作</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder Apply(Action<FeishuWebhookServiceBuilder> configureAction)
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

        // 验证 FeishuWebhookOptions 配置
        // 注意：由于此时服务还未完全注册，无法通过 IOptions<T> 获取配置
        // 配置验证将在 PostConfigure 阶段完成
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    private void RegisterServices()
    {
        // 配置选项
        RegisterOptions();

        // 注册核心服务
        RegisterCoreServices();

        // 注册性能监控组件
        if (_enableMetrics)
        {
            RegisterMetricsServices();
        }

        // 注册事件处理器工厂
        RegisterEventHandlerFactory();

        // 注册健康检查支持
        if (_enableHealthChecks)
        {
            RegisterHealthCheckServices();
        }
    }

    /// <summary>
    /// 注册选项配置
    /// </summary>
    private void RegisterOptions()
    {
        // 应用自定义配置
        if (_configureOptions != null)
        {
            _services.Configure(_configureOptions);
        }

        // 确保基本配置存在
        _services.PostConfigure<FeishuWebhookOptions>(options =>
        {
            // 设置默认值
            options.AutoRegisterEndpoint = _autoRegisterEndpoint;

            // 如果用户没有配置，使用默认配置
            if (string.IsNullOrEmpty(options.RoutePrefix))
                options.RoutePrefix = "feishu/Webhook";

            if (options.AllowedHttpMethods == null || !options.AllowedHttpMethods.Any())
                options.AllowedHttpMethods = new HashSet<string> { "POST" };

            if (options.MaxRequestBodySize == 0)
                options.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB

            if (options.EventHandlingTimeoutMs == 0)
                options.EventHandlingTimeoutMs = 30000;

            if (options.MaxConcurrentEvents == 0)
                options.MaxConcurrentEvents = 10;

            // 验证配置
            options.Validate();
        });
    }

    /// <summary>
    /// 注册核心服务
    /// </summary>
    private void RegisterCoreServices()
    {
        _services.TryAddSingleton<FeishuWebhookNonceDeduplicator>();
        _services.TryAddScoped<IFeishuEventValidator, FeishuEventValidator>();
        _services.TryAddScoped<IFeishuEventDecryptor, FeishuEventDecryptor>();
        _services.TryAddScoped<IFeishuWebhookService, FeishuWebhookService>();
        _services.TryAddSingleton<FeishuWebhookConcurrencyService>();
        _services.TryAddSingleton<FeishuWebhookEventDeduplicator>();
    }

    /// <summary>
    /// 注册性能监控服务
    /// </summary>
    private void RegisterMetricsServices()
    {
        _services.TryAddSingleton<MetricsCollector>();
    }

    /// <summary>
    /// 注册事件处理器工厂
    /// </summary>
    private void RegisterEventHandlerFactory()
    {
        var defaultHandlerType = _handlerTypes.First();

        _services.TryAddScoped<IFeishuEventHandlerFactory>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<DefaultFeishuEventHandlerFactory>>();
            var handlers = serviceProvider.GetRequiredService<IEnumerable<IFeishuEventHandler>>()
                .Where(h => _handlerTypes.Contains(h.GetType()))
                .ToList();
            var defaultHandler = serviceProvider.GetRequiredService(defaultHandlerType) as IFeishuEventHandler
                ?? throw new InvalidOperationException($"无法获取默认处理器: {defaultHandlerType.Name}");
            return new DefaultFeishuEventHandlerFactory(logger, handlers, defaultHandler);
        });
    }

    /// <summary>
    /// 注册健康检查服务
    /// </summary>
    private void RegisterHealthCheckServices()
    {
        // 暂时简化健康检查实现，避免复杂反射
        try
        {
            _services.AddHealthChecks();
        }
        catch (Exception ex)
        {
            // 健康检查注册失败不应该影响主要功能
            System.Diagnostics.Debug.WriteLine($"健康检查注册失败: {ex.Message}");
        }
    }
}