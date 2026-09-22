// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Extensions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using Mud.Feishu.Webhook.Utils;
using Mud.HttpUtils;
using System.Diagnostics.CodeAnalysis;

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
    private bool _autoRegisterEndpointExplicitlySet = false;
    private bool _configured = false;
    private Action<FeishuWebhookOptions>? _configureOptions;

    /// <summary>
    /// R5.0.1（X2）：已删除的请求日志开关的迁移提示「每进程一次」标记。
    /// </summary>
    private static int _removedRequestLoggingSwitchWarned;

    /// <summary>
    /// R5.0.1（X2）：已删除的配置键名，仅用于一次性迁移提示。
    /// </summary>
    /// <remarks>
    /// 单独提为常量是为了让本文件对已删除键名只保留**一处**字面量，便于配置审计脚本以单行
    /// <c>audit-allow</c> 标记精确放行，而不是整文件豁免。
    /// </remarks>
    // audit-allow: X2 migration probe must reference the removed key name exactly once
    private const string RemovedRequestLoggingKey = "EnableRequestLogging";

    /// <summary>
    /// R5/X4：已失效的端点注册开关「每进程一次」告警标记。
    /// </summary>
    private static int _removedAutoEndpointSwitchWarned;

    /// <summary>
    /// P1-1（R2）：应用处理器/拦截器注册与注册表冻结的一次性执行标记。
    /// </summary>
    /// <remarks>
    /// PostConfigure 随 <c>IOptionsMonitor</c> 的<b>每次</b> Options 缓存重建重放（任何配置热更都会触发）；
    /// 若不加以守卫，热更时 <see cref="FeishuWebhookTypeRegistry{T}.Register"/> 会对已冻结注册表抛
    /// <see cref="InvalidOperationException"/>，导致该次 Options 创建失败——此后中间件与服务读取
    /// <c>CurrentValue</c> 的每个 Webhook 请求都会 500，直至进程重启。
    /// </remarks>
    private int _registryInitialized;

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
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式配置绑定（ConfigurationBinder.Bind）在裁剪下无法静态分析配置类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式配置绑定（ConfigurationBinder.Bind）在 AOT/动态代码生成环境下不可用")]
#endif
    public FeishuWebhookServiceBuilder ConfigureFrom(IConfiguration configuration, string? sectionName = null)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var section = sectionName ?? DefaultConfigurationSection;
        _services.Configure<FeishuWebhookOptions>(options => configuration.GetSection(section).Bind(options));

        // R5.0.1（X2）：已删除的请求日志开关（该开关从未被运行时读取）。
        // 此处做一次性兼容探测：用户若仍配置该键，给出明确迁移指引，避免「配了但无声无息」。
        // 不能在 ConfigureFrom 就地 LogWarning——本方法在服务注册期执行，此时没有 ILogger；
        // 故委托给 PostConfigure<IServiceProvider>（与下方 RegisterOptions 的既有模式同形）。
        var migrationSection = configuration.GetSection(section);
        _services.AddOptions<FeishuWebhookOptions>().PostConfigure<IServiceProvider>((_, serviceProvider) =>
        {
            if (System.Threading.Interlocked.Exchange(ref _removedRequestLoggingSwitchWarned, 1) != 0)
                return;

            if (migrationSection.GetSection(RemovedRequestLoggingKey).Exists())
            {
                serviceProvider.GetService<ILogger<FeishuWebhookOptions>>()?.LogWarning(
                    "FeishuWebhook:{RemovedKey} 已移除（该开关从未被运行时读取）。" +
                    "请改用 Logging:LogLevel:Mud.Feishu.Webhook 控制 Webhook 模块日志级别。",
                    RemovedRequestLoggingKey);
            }
        });

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
        _autoRegisterEndpointExplicitlySet = true;
        return this;
    }

    /// <summary>
    /// 禁用自动端点注册
    /// </summary>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder DisableAutoEndpoint()
    {
        _autoRegisterEndpoint = false;
        _autoRegisterEndpointExplicitlySet = true;
        return this;
    }

    /// <summary>
    /// 添加事件处理器
    /// </summary>
    /// <typeparam name="THandler">处理器类型</typeparam>
    /// <returns>建造者实例，支持链式调用</returns>
    public FeishuWebhookServiceBuilder AddHandler<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        THandler>()
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
    public FeishuWebhookServiceBuilder AddInterceptor<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TInterceptor>()
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
    public FeishuWebhookServiceBuilder AddHandler<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        THandler>(string appKey)
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
    public FeishuWebhookServiceBuilder AddInterceptor<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TInterceptor>(string appKey)
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
    public FeishuWebhookServiceBuilder UseSignatureValidator<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TSignatureValidator>()
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
    public FeishuWebhookServiceBuilder UseTimestampValidator<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TTimestampValidator>()
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
    public FeishuWebhookServiceBuilder UseNonceValidator<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TNonceValidator>()
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
    public FeishuWebhookServiceBuilder UseSubscriptionValidator<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TSubscriptionValidator>()
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
    public FeishuWebhookServiceBuilder UseEncryptKeyProvider<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TEncryptKeyProvider>()
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
    public FeishuWebhookServiceBuilder UseCompositeValidator<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TCompositeValidator>()
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

        // 重复注册检测：同 (appKey, type) 重复 → 抛异常
        var duplicateHandler = _pendingHandlerRegistrations
            .GroupBy(x => (x.AppKey, x.HandlerType))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateHandler != null)
        {
            throw new InvalidOperationException(
                $"检测到重复注册的处理器: AppKey={duplicateHandler.Key.AppKey}, Type={duplicateHandler.Key.HandlerType.Name}");
        }

        var duplicateInterceptor = _pendingInterceptorRegistrations
            .GroupBy(x => (x.AppKey, x.InterceptorType))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateInterceptor != null)
        {
            throw new InvalidOperationException(
                $"检测到重复注册的拦截器: AppKey={duplicateInterceptor.Key.AppKey}, Type={duplicateInterceptor.Key.InterceptorType.Name}");
        }
    }

    /// <summary>
    /// 注册服务
    /// </summary>
    private void RegisterServices()
    {
        // AOT: 将 Webhook 专用 JsonSerializerContext 注入 FeishuJsonDefaults 的 resolver 链。
        // 必须在任何 JSON 序列化/反序列化发生前执行。ConfigureUserResolver 是幂等的累加模式。
#if NET8_0_OR_GREATER
        global::Mud.Feishu.Webhook.Extensions.FeishuWebhookJsonResolverExtensions.ConfigureWebhookResolver();
#endif

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
            // 仅在用户显式调用 EnableAutoEndpoint()/DisableAutoEndpoint() 时覆盖配置值
            // 否则尊重 appsettings.json 中的配置
#pragma warning disable CS0618 // R5/X4：该开关无运行时效果，仅为源码级兼容而保留
            if (_autoRegisterEndpointExplicitlySet)
                // audit-allow: X4 - the Obsolete switch must still be assigned so appsettings/Builder 兼容不失效
                options.AutoRegisterEndpoint = _autoRegisterEndpoint;
#pragma warning restore CS0618

            // 如果用户没有配置，使用默认配置
            if (options.AllowedHttpMethods == null || !options.AllowedHttpMethods.Any())
                options.AllowedHttpMethods = new HashSet<string> { "POST" };

            // 验证配置
            options.Validate();
        });

        // 使用 AddOptions 注册配置后处理
        _services.AddOptions<FeishuWebhookOptions>()
            .PostConfigure<IServiceProvider>((options, serviceProvider) =>
            {
                // R5/X4：AutoRegisterEndpoint 无运行时效果——对配置了 false 的部署给出显式告警，
                // 避免长期误解为「已关闭端点、不再收事件」。每进程仅告警一次。
#pragma warning disable CS0618 // 该开关无运行时效果，此处仅做迁移提示
                if (!options.AutoRegisterEndpoint
                    && System.Threading.Interlocked.Exchange(ref _removedAutoEndpointSwitchWarned, 1) == 0)
                {
                    serviceProvider.GetService<ILogger<FeishuWebhookOptions>>()?.LogWarning(
                        "FeishuWebhook:AutoRegisterEndpoint=false 不产生任何运行时效果：路由由 app.UseFeishuWebhook() " +
                        "显式注册，事件仍会被接收处理。若不需要 Webhook 处理，请移除该中间件调用；" +
                        "该属性将在下个 major 删除。");
                }
#pragma warning restore CS0618

                // P1-1（R2）：注册+冻结只在首次 Options 构建时执行（Interlocked 一次性守卫）。
                // PostConfigure 随 IOptionsMonitor 每次缓存重建重放，若不守卫，配置热更时
                // Register 会对已冻结注册表抛 InvalidOperationException → 所有后续请求 500。
                // options.Validate() 与上方告警逻辑留在守卫之外：验证与告警语义应随每次重建生效。
                if (System.Threading.Interlocked.Exchange(ref _registryInitialized, 1) == 0)
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

                    // 冻结注册表，杜绝运行时热注册竞态
                    handlerRegistry.Freeze();
                    interceptorRegistry.Freeze();
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
        // B2/R1.2 + C1：内存去重工厂——统一节优先，其次 DeduplicationOptions，否则 Consts
        // Redis 分布式实现若已先注册则 TryAdd 不会覆盖。
        _services.AddFeishuDeduplicationOptions();
        _services.TryAddSingleton<IFeishuEventDeduplicator>(sp =>
        {
            var logger = sp.GetService<ILogger<FeishuEventDeduplicator>>();
            var unified = sp.GetService<IOptions<FeishuDeduplicationOptions>>()?.Value;

            if (unified is { IsConfiguredFromConfiguration: true })
            {
                var mode = unified.Mode ?? FeishuDeduplicationOptions.DefaultMode;
                if (string.Equals(mode, FeishuDeduplicationOptions.ModeNone, StringComparison.OrdinalIgnoreCase))
                    return new NoopFeishuEventDeduplicator(logger as ILogger<NoopFeishuEventDeduplicator>);

                if (string.Equals(mode, FeishuDeduplicationOptions.ModeDistributed, StringComparison.OrdinalIgnoreCase)
                    && sp.GetService<IFeishuEventDeduplicator>() is null)
                {
                    logger?.LogWarning(
                        "FeishuDeduplication:Mode=Distributed 但未注册分布式去重实现，Webhook 回退内存去重。请先 AddFeishuRedisDeduplicators。");
                }

                var ttl = unified.ResolveEventTtl();
                if (ttl <= TimeSpan.Zero)
                    ttl = TimeSpan.FromMilliseconds(Consts.DefaultCacheExpirationMs);
                var processing = unified.ResolveEventProcessingTimeout();
                if (processing <= TimeSpan.Zero)
                    processing = TimeSpan.FromMilliseconds(Consts.DefaultProcessingTimeoutMs);
                var cleanup = unified.Event?.CleanupInterval is { } cl && cl > TimeSpan.Zero
                    ? cl
                    : TimeSpan.FromMilliseconds(Consts.DefaultCleanupIntervalMs);
                var maxSize = unified.Event?.MaxCacheSize ?? Consts.DefaultMaxCacheSize;

                return new FeishuEventDeduplicator(logger, ttl, cleanup, processing, maxSize);
            }

#pragma warning disable CS0618
            var dedup = sp.GetService<IOptions<DeduplicationOptions>>()?.Value;
#pragma warning restore CS0618
            if (dedup is not null)
                return new FeishuEventDeduplicator(dedup, logger);

            return new FeishuEventDeduplicator(
                logger,
                cacheExpiration: TimeSpan.FromMilliseconds(Consts.DefaultCacheExpirationMs),
                cleanupInterval: TimeSpan.FromMilliseconds(Consts.DefaultCleanupIntervalMs),
                processingTimeout: TimeSpan.FromMilliseconds(Consts.DefaultProcessingTimeoutMs),
                maxCacheSize: Consts.DefaultMaxCacheSize);
        });
        _services.TryAddSingleton<IFeishuNonceDistributedDeduplicator, FeishuNonceDistributedDeduplicator>();

        // 令牌自动刷新后台服务已在 AddFeishuAppBaseServices 中注册（由 Mud.HttpUtils 提供）。
        // R4：仅 EnableTokenBackgroundRefresh 控制 TokenRefreshBackgroundOptions.Enabled。
        // null = 沿用宿主既有默认（不覆盖）；true/false = 显式覆盖。
        _services.AddOptions<TokenRefreshBackgroundOptions>()
            .PostConfigure<IOptions<FeishuWebhookOptions>>((tokenOptions, webhookOptions) =>
            {
                var explicitRefresh = webhookOptions.Value.EnableTokenBackgroundRefresh;
                if (explicitRefresh.HasValue)
                    tokenOptions.Enabled = explicitRefresh.Value;
            });

        // 注册 HttpContext 访问器（用于在 SignatureValidator 中获取客户端 IP）
        _services.AddHttpContextAccessor();

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

        // WHF-R2/C3：中间件注册为 Singleton 使 IHost 关停时 Dispose 可达。
        // UseMiddleware<T> 检测到 DI 注册后从容器解析实例，随容器 Dispose 释放
        // _onChangeSubscription（MultiAppMiddleware）和 _cleanupTimer（RateLimitMiddleware）。
        _services.TryAddSingleton<FeishuMultiAppMiddleware>();
        _services.TryAddSingleton<FeishuRateLimitMiddleware>();

        // A1/WHF-R2：Nonce 去重多实例静默降级 → 启动期告警/阻断。
        // 与事件去重 :610-614 的 LogWarning 兜底口径对齐——Nonce 去重此前无等价告警。
        // 生产环境 Mode=Distributed + 内存 Nonce 实现 = 已知不可接受风险，fail-fast。
        _services.AddOptions<FeishuWebhookOptions>()
            .PostConfigure<IServiceProvider>((options, sp) =>
            {
                var unified = sp.GetService<IOptions<FeishuDeduplicationOptions>>()?.Value;
                var isDistributedIntent = unified is { IsConfiguredFromConfiguration: true }
                    && string.Equals(unified.Mode, FeishuDeduplicationOptions.ModeDistributed, StringComparison.OrdinalIgnoreCase);
                var nonceImpl = sp.GetService<IFeishuNonceDistributedDeduplicator>();
                var isMemoryNonce = nonceImpl is null or FeishuNonceDistributedDeduplicator;

                if (isDistributedIntent && isMemoryNonce)
                {
                    var logger = sp.GetService<ILogger<FeishuWebhookOptions>>();
                    var isProduction = sp.GetService<IEnvironmentService>()?.IsProduction == true;

                    if (isProduction)
                    {
                        throw new InvalidOperationException(
                            "FeishuDeduplication:Mode=Distributed 但 Nonce 去重为进程内内存实现。" +
                            "多实例部署下跨实例重放攻击不可检测。请调用 AddFeishuRedisDeduplicators() 注册 Redis 实现。");
                    }

                    logger?.LogWarning(
                        "FeishuDeduplication:Mode=Distributed 但 Nonce 去重为进程内内存实现。" +
                        "多实例部署下跨实例重放攻击不可检测。请调用 AddFeishuRedisDeduplicators() 注册 Redis 实现。");
                }
            });
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
        // NEW-REG-01 修复：无条件注册 HostedService，由其内部读取 IOptionsMonitor 决定是否启动
        // 不能在 PostConfigure 回调中注册，因为 ServiceProvider 构建后 IServiceCollection 修改无效
        _services.AddHostedService<FailedEventRetryService>();
    }
}