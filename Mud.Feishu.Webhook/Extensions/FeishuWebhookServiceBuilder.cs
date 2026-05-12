// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using Mud.Feishu.Webhook.Utilities;
using Mud.HttpUtils;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书Webhook服务建造者，用于简化服务注册配置
/// </summary>
public class FeishuWebhookServiceBuilder
{
    private const string DefaultConfigurationSection = "FeishuWebhook";
    private readonly IServiceCollection _services;
    private readonly List<Type> _handlerTypes = new();
    private readonly List<Type> _interceptorTypes = new();
    private readonly List<(string AppKey, Type HandlerType)> _pendingHandlerRegistrations = new();
    private readonly List<(string AppKey, Type InterceptorType)> _pendingInterceptorRegistrations = new();
    private bool _enableHealthChecks = true;
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

        var section = sectionName ?? DefaultConfigurationSection;
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
    /// 添加事件拦截器
    /// </summary>
    /// <typeparam name="TInterceptor">拦截器类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder AddInterceptor<TInterceptor>()
        where TInterceptor : class, IFeishuEventInterceptor
    {
        _interceptorTypes.Add(typeof(TInterceptor));
        _services.AddScoped<IFeishuEventInterceptor, TInterceptor>();
        _services.AddScoped<TInterceptor>();
        return this;
    }

    /// <summary>
    /// 添加事件拦截器实例
    /// </summary>
    /// <typeparam name="TInterceptor">拦截器类型</typeparam>
    /// <param name="interceptorInstance">拦截器实例</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder AddInterceptor<TInterceptor>(TInterceptor interceptorInstance)
        where TInterceptor : class, IFeishuEventInterceptor
    {
        if (interceptorInstance == null)
            throw new ArgumentNullException(nameof(interceptorInstance));

        _interceptorTypes.Add(typeof(TInterceptor));
        _services.AddScoped<IFeishuEventInterceptor>(_ => interceptorInstance);
        _services.AddScoped<TInterceptor>(_ => interceptorInstance);
        return this;
    }

    /// <summary>
    /// 添加事件拦截器工厂
    /// </summary>
    /// <typeparam name="TInterceptor">拦截器类型</typeparam>
    /// <param name="interceptorFactory">拦截器工厂</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder AddInterceptor<TInterceptor>(Func<IServiceProvider, TInterceptor> interceptorFactory)
        where TInterceptor : class, IFeishuEventInterceptor
    {
        if (interceptorFactory == null)
            throw new ArgumentNullException(nameof(interceptorFactory));

        _interceptorTypes.Add(typeof(TInterceptor));
        _services.AddScoped<IFeishuEventInterceptor>(interceptorFactory);
        _services.AddScoped<TInterceptor>(interceptorFactory);
        return this;
    }

    /// <summary>
    /// 为指定应用添加事件处理器（多应用模式）
    /// 处理器仅注册到指定应用的 HandlerRegistry，不会注册到全局 IFeishuEventHandler 集合
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <param name="appKey">应用键</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder AddHandler<THandler>(string appKey)
        where THandler : class, IFeishuEventHandler
    {
        if (string.IsNullOrEmpty(appKey))
            throw new ArgumentException("应用键不能为空", nameof(appKey));

        _handlerTypes.Add(typeof(THandler));
        _pendingHandlerRegistrations.Add((appKey, typeof(THandler)));
        _services.AddScoped<THandler>();
        // 注意：不再全局注册到 IFeishuEventHandler，防止跨应用处理器泄漏
        return this;
    }

    /// <summary>
    /// 为指定应用添加事件拦截器（多应用模式）
    /// 拦截器仅注册到指定应用的 InterceptorRegistry，不会注册到全局 IFeishuEventInterceptor 集合
    /// </summary>
    /// <typeparam name="TInterceptor">拦截器类型</typeparam>
    /// <param name="appKey">应用键</param>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder AddInterceptor<TInterceptor>(string appKey)
        where TInterceptor : class, IFeishuEventInterceptor
    {
        if (string.IsNullOrEmpty(appKey))
            throw new ArgumentException("应用键不能为空", nameof(appKey));

        _interceptorTypes.Add(typeof(TInterceptor));
        _pendingInterceptorRegistrations.Add((appKey, typeof(TInterceptor)));
        _services.AddScoped<TInterceptor>();
        // 注意：不再全局注册到 IFeishuEventInterceptor，防止跨应用拦截器泄漏
        return this;
    }

    /// <summary>
    /// 使用自定义签名验证器
    /// </summary>
    /// <typeparam name="TSignatureValidator">自定义签名验证器类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder UseSignatureValidator<TSignatureValidator>()
        where TSignatureValidator : class, ISignatureValidator
    {
        _services.AddScoped<ISignatureValidator, TSignatureValidator>();
        return this;
    }

    /// <summary>
    /// 使用自定义时间戳验证器
    /// </summary>
    /// <typeparam name="TTimestampValidator">自定义时间戳验证器类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder UseTimestampValidator<TTimestampValidator>()
        where TTimestampValidator : class, ITimestampValidator
    {
        _services.AddScoped<ITimestampValidator, TTimestampValidator>();
        return this;
    }

    /// <summary>
    /// 使用自定义 Nonce 验证器
    /// </summary>
    /// <typeparam name="TNonceValidator">自定义 Nonce 验证器类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder UseNonceValidator<TNonceValidator>()
        where TNonceValidator : class, INonceValidator
    {
        _services.AddScoped<INonceValidator, TNonceValidator>();
        return this;
    }

    /// <summary>
    /// 使用自定义订阅验证器
    /// </summary>
    /// <typeparam name="TSubscriptionValidator">自定义订阅验证器类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder UseSubscriptionValidator<TSubscriptionValidator>()
        where TSubscriptionValidator : class, ISubscriptionValidator
    {
        _services.AddScoped<ISubscriptionValidator, TSubscriptionValidator>();
        return this;
    }

    /// <summary>
    /// 使用自定义加密密钥提供程序
    /// </summary>
    /// <typeparam name="TEncryptKeyProvider">自定义加密密钥提供程序类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    /// <remarks>
    /// 使用场景：
    /// 1. 从 Azure KeyVault 获取密钥
    /// 2. 从 AWS Secrets Manager 获取密钥
    /// 3. 从环境变量获取密钥
    /// 4. 从自定义密钥管理服务获取密钥
    /// </remarks>
    public FeishuWebhookServiceBuilder UseEncryptKeyProvider<TEncryptKeyProvider>()
        where TEncryptKeyProvider : class, IEncryptKeyProvider
    {
        _services.AddScoped<IEncryptKeyProvider, TEncryptKeyProvider>();
        return this;
    }

    /// <summary>
    /// 使用自定义组合验证器（完全替换默认实现）
    /// </summary>
    /// <typeparam name="TCompositeValidator">自定义组合验证器类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder UseCompositeValidator<TCompositeValidator>()
        where TCompositeValidator : class, IFeishuEventValidator
    {
        _services.AddScoped<IFeishuEventValidator, TCompositeValidator>();
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

        // 注册事件处理器工厂
        RegisterEventHandlerFactory();

        // 注册失败事件重试服务
        RegisterRetryServices();

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
        _services.AddSingleton<IValidateOptions<FeishuWebhookOptions>, FeishuWebhookOptionsValidator>();
        // 注意：FeishuAppWebhookOptions 不作为 IOptions<T> 独立注册，IValidateOptions 永远不会被框架自动调用。
        // 应用级配置的验证已在 FeishuWebhookOptions.Validate() 中通过遍历 Apps 字典完成。
        _services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();

        // 应用自定义配置
        if (_configureOptions != null)
        {
            _services.Configure(_configureOptions);
        }

        // 确保基本配置存在，并注册多应用的处理器和拦截器
        _services.PostConfigure<FeishuWebhookOptions>(options =>
        {
            // 设置默认值
            options.AutoRegisterEndpoint = _autoRegisterEndpoint;

            // 如果用户没有配置，使用默认配置
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

        // 使用 AddOptions 注册配置后处理
        _services.AddOptions<FeishuWebhookOptions>()
            .PostConfigure<IServiceProvider>((options, serviceProvider) =>
            {
                // 注册多应用的处理器和拦截器到共享注册表
                var handlerRegistry = serviceProvider.GetRequiredService<FeishuWebhookHandlerRegistry>();
                foreach (var (appKey, handlerType) in _pendingHandlerRegistrations)
                {
                    handlerRegistry.Register(appKey, handlerType);
                }

                var interceptorRegistry = serviceProvider.GetRequiredService<FeishuWebhookInterceptorRegistry>();
                foreach (var (appKey, interceptorType) in _pendingInterceptorRegistrations)
                {
                    interceptorRegistry.Register(appKey, interceptorType);
                }
            });
    }

    /// <summary>
    /// 注册核心服务
    /// </summary>
    private void RegisterCoreServices()
    {
        // 单实例服务（包含 IHostedService）
        _services.AddSingleton<FeishuWebhookConcurrencyService>();
        _services.AddHostedService(sp => sp.GetRequiredService<FeishuWebhookConcurrencyService>());
        _services.TryAddSingleton<IFeishuEventDeduplicator, FeishuEventDeduplicator>();
        _services.TryAddSingleton<IFeishuNonceDistributedDeduplicator, FeishuNonceDistributedDeduplicator>();

        // 注册令牌自动刷新后台服务（由 EnableBackgroundProcessing 配置控制是否启用）
        // 使用 Mud.HttpUtils 提供的扩展方法，自动根据目标框架选择正确的实现
        _services.AddTokenRefreshBackgroundService();

        // 将 FeishuWebhookOptions.EnableBackgroundProcessing 映射到 TokenRefreshBackgroundOptions.Enabled
        _services.AddOptions<TokenRefreshBackgroundOptions>()
            .PostConfigure<IOptions<FeishuWebhookOptions>>((tokenOptions, webhookOptions) =>
            {
                tokenOptions.Enabled = webhookOptions.Value.EnableBackgroundProcessing;
            });

        // 注册多应用注册表（单例，所有应用共享）
        _services.TryAddSingleton<FeishuWebhookHandlerRegistry>();
        _services.TryAddSingleton<FeishuWebhookInterceptorRegistry>();

        // 注册 AppKey 上下文访问器（单例，基于 AsyncLocal 的线程安全实现）
        // 同时注册 IAppKeyAccessor 和 IWebhookAppKeyAccessor，指向同一个实例
        _services.TryAddSingleton<WebhookAppKeyAccessor>();
        _services.TryAddSingleton<IAppKeyAccessor>(sp => sp.GetRequiredService<WebhookAppKeyAccessor>());
        _services.TryAddSingleton<IWebhookAppKeyAccessor>(sp => sp.GetRequiredService<WebhookAppKeyAccessor>());

        // 注册工具服务（单例）
        _services.TryAddSingleton<IEnvironmentService, EnvironmentService>();

        // 注册加密密钥提供程序（默认从配置文件读取）
        _services.TryAddScoped<IEncryptKeyProvider, DefaultEncryptKeyProvider>();

        // 注册专门的验证器（作用域服务）
        _services.TryAddScoped<ISignatureValidator, SignatureValidator>();
        _services.TryAddScoped<ITimestampValidator, TimestampValidator>();
        _services.TryAddScoped<INonceValidator, NonceValidator>();
        _services.TryAddScoped<ISubscriptionValidator, SubscriptionValidator>();

        // 注册组合验证器作为原接口的实现（向后兼容）
        _services.TryAddScoped<IFeishuEventValidator, CompositeFeishuEventValidator>();

        // 其他作用域服务
        _services.TryAddScoped<IFeishuEventDecryptor, FeishuEventDecryptor>();
        _services.TryAddScoped<IFeishuWebhookService, FeishuWebhookService>();
        _services.TryAddScoped<ISecurityAuditService, SecurityAuditService>();
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

        // 注册事件拦截器集合（作用域，按注册顺序排序）
        _services.TryAddScoped<IFeishuEventInterceptor[]>(serviceProvider =>
        {
            return serviceProvider.GetRequiredService<IEnumerable<IFeishuEventInterceptor>>()
                .Where(i => _interceptorTypes.Contains(i.GetType()))
                .OrderBy(i => _interceptorTypes.IndexOf(i.GetType()))
                .ToArray();
        });
    }

    /// <summary>
    /// 注册健康检查服务
    /// </summary>
    private void RegisterHealthCheckServices()
    {
        try
        {
            _services.AddHealthChecks()
                .AddCheck<FeishuWebhookHealthCheck>("feishu-webhook");
        }
        catch (Exception ex)
        {
            // 健康检查注册失败不应该影响主要功能
            // 注意：此处无法使用 ILogger，因为服务尚未构建完成
            // 使用 Debug 输出作为最后的日志手段
            System.Diagnostics.Debug.WriteLine($"健康检查注册失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 注册失败事件重试服务
    /// </summary>
    private void RegisterRetryServices()
    {
        _services.TryAddSingleton<IFailedEventStore, InMemoryFailedEventStore>();

        _services.PostConfigure<FeishuWebhookOptions>(options =>
        {
            if (options.Retry.EnableRetry)
            {
                _services.AddHostedService<FailedEventRetryService>();
            }
        });
    }
}