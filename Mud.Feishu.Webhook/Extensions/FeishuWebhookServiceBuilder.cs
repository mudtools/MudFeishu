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
    /// <summary>
    /// 仅“全局注册”（<c>AddHandler&lt;T&gt;()</c> 无 appKey 重载）的处理器类型（R3-P1-4）。
    /// 与 <see cref="_handlerTypes"/> 的区别：后者混装全局与应用专属，不可用于选取默认处理器。
    /// </summary>
    private readonly List<Type> _globalHandlerTypes = new();
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
        _globalHandlerTypes.Add(typeof(THandler));   // R3-P1-4：仅“全局注册”可充当默认处理器
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
        _globalHandlerTypes.Add(typeof(THandler));   // R3-P1-4：实例注册同样是“全局注册”
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
        _globalHandlerTypes.Add(typeof(THandler));   // R3-P1-4：工厂注册同样是“全局注册”
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

        // R3-P0-5：把“启动期阻断”从隐式副作用变成显式机制。
        // 此前全仓无启动期校验——FeishuWebhookOptions 经 IOptionsMonitor 惰性构建，
        // 唯一让它在启动期被构建的，是 FeishuWebhookConcurrencyService（Singleton + HostedService）
        // 构造函数里读了一次 CurrentValue。任何对该行的改动都会让 D1（内存 Nonce 生产阻断）与
        // D6（多应用 ExpectedAppId 强制）静默退化为“首个请求 500”——服务已起来、已开始收流量才炸，
        // 比不阻断更糟。此处显式声明：所有配置校验一律在宿主启动期完成，失败即启动失败。
        //
        // 注：不用 AddOptions<T>().ValidateOnStart()——该扩展方法在 net6.0 目标下同时存在于
        // Microsoft.Extensions.Hosting 引用程序集与 Microsoft.Extensions.Options 包中，
        // 会产生 CS0121 二义性错误。托管服务方式在全部 TFM（含 netstandard2.0）上行为一致。
        _services.AddHostedService(sp => new WebhookOptionsStartupValidator(
            sp,
            sp.GetService<ILogger<WebhookOptionsStartupValidator>>()));
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

                // R3-P0-4：此处**不得**解析 IFeishuEventDeduplicator——本工厂正在构造该服务，
                // 自解析会无限递归并导致 StackOverflowException（不可捕获，进程终止）。
                // 原实现写作 `sp.GetService<IFeishuEventDeduplicator>() is null`：`is null` 判据
                // 强制先求值 GetService，而 TryAddSingleton 仅在本服务无其它注册时才注册本工厂，
                // 于是「走到该分支」的前提（未注册 Redis）正是使其解析回本工厂的充分条件 → 递归。
                // “Mode=Distributed 但实现仍为内存”的检测已下沉到 RegisterCoreServices 的
                // PostConfigure<IServiceProvider>（与 R3-P0-1 的 Nonce 形态检查同处一个委托）。

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

        // R3-P0-1 + R3-P0-4：去重「实现形态」与「部署形态」绑定检查（**单一** PostConfigure）。
        //
        // R3-P0-1（D1）：安全能力必须与部署形态显式绑定。原守卫只在「显式配置了
        // FeishuDeduplication:Mode=Distributed 且该节来自配置」时才触发，而默认路径
        // （不写该节 → IsConfiguredFromConfiguration=false，Mode 默认 InMemory）既无阻断也无告警，
        // 生产多实例下跨实例重放不可检测却完全静默。现改为按「实现形态」三态处理。
        //
        // R3-P0-4：同时承载事件去重的「Mode=Distributed 但实现仍为内存」告警——
        // 该检测原先位于 IFeishuEventDeduplicator 工厂内部并自解析该服务（递归 → StackOverflow），
        // 现下沉至此。两个检测合并为一个委托，避免 Options 每次重建跑两遍、告警次序不确定。
        //
        // 启动期保证：本 PostConfigure 在 Options 构建时执行；由 R3-P0-5 的 ValidateOnStart()
        // 保证其发生在宿主启动期，而非首个请求。
        _services.AddOptions<FeishuWebhookOptions>()
            .PostConfigure<IServiceProvider>((options, sp) =>
            {
                var unified = sp.GetService<IOptions<FeishuDeduplicationOptions>>()?.Value;
                var isDistributedIntent = unified is { IsConfiguredFromConfiguration: true }
                    && string.Equals(unified.Mode, FeishuDeduplicationOptions.ModeDistributed, StringComparison.OrdinalIgnoreCase);
                var nonceImpl = sp.GetService<IFeishuNonceDistributedDeduplicator>();
                var isMemoryNonce = nonceImpl is null or FeishuNonceDistributedDeduplicator;

                var logger = sp.GetService<ILogger<FeishuWebhookOptions>>();
                var isProduction = sp.GetService<IEnvironmentService>()?.IsProduction == true;

                // ── 事件去重形态告警（原位于工厂内，R3-P0-4 下沉至此）──
                if (isDistributedIntent && sp.GetService<IFeishuEventDeduplicator>() is FeishuEventDeduplicator)
                {
                    logger?.LogWarning(
                        "FeishuDeduplication:Mode=Distributed 但事件去重仍为进程内内存实现，多实例下事件幂等性不成立。" +
                        "请先 AddFeishuRedisDeduplicators() 再构建 Webhook 服务。");
                }

                if (!isMemoryNonce)
                    return;   // 已接入分布式 Nonce 实现：无需干预

                const string risk = "进程内内存 Nonce 去重仅对单实例有效；多实例/负载均衡下跨实例重放攻击不可检测。";

                if (isProduction && !options.AllowInMemoryNonceDedupInProduction)
                {
                    // D1：安全能力的部署形态必须显式绑定——默认路径也必须阻断，
                    // 而非只在显式 Distributed 意图时阻断。
                    throw new InvalidOperationException(
                        $"生产环境检测到 Nonce 去重为{risk}" +
                        "请调用 AddFeishuRedisDeduplicators() 注册 Redis 实现；" +
                        "若确为单实例部署，请显式设置 FeishuWebhook:AllowInMemoryNonceDedupInProduction=true 以承担风险。");
                }

                if (isProduction)
                {
                    logger?.LogWarning(
                        "生产环境已显式允许内存 Nonce 去重（FeishuWebhook:AllowInMemoryNonceDedupInProduction=true）。" +
                        risk + "请确保本部署为单实例且无水平扩容计划。");
                }
                else
                {
                    logger?.LogWarning(
                        (isDistributedIntent ? "FeishuDeduplication:Mode=Distributed 但 Nonce 去重仍为" : "Nonce 去重使用") +
                        "进程内内存实现。" + risk + "生产环境请接入 Redis。");
                }
            });
    }


    /// <summary>
    /// 注册事件处理器工厂
    /// </summary>
    private void RegisterEventHandlerFactory()
    {
        // R3-P1-4：默认处理器只能取自“全局注册”的处理器。
        // 此前用 _handlerTypes.First()——而 AddHandler<T>(appKey) 同样写入该列表，
        // 于是 AddHandler<AppAHandler>("appA").AddHandler<GlobalHandler>() 会让 AppAHandler 成为
        // 全局默认处理器，其它应用（appB）的事件在回退路径被 A 的处理器处理 → 跨应用语义错误，
        // 且因应用版重载也注册了 AddScoped<THandler>()，GetRequiredService 不会抛，故障静默。
        var defaultHandlerType = _globalHandlerTypes.Count > 0
            ? _globalHandlerTypes[0]
            : throw new InvalidOperationException(
                "至少需要一个全局注册的事件处理器（AddHandler<T>()）作为默认处理器。" +
                "应用专属处理器（AddHandler<T>(appKey)）不得充当全局默认处理器——否则会产生跨应用事件串扰。");

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