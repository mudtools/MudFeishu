// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Internal;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用管理器实现
/// </summary>
/// <remarks>
/// 负责管理系统中所有飞书应用的创建、获取、移除等操作。
/// 每个应用拥有独立的配置、缓存和TokenManager实例。
/// 继承 DefaultAppManager&lt;FeishuAppContext&gt; 获得通用的应用管理能力。
/// <para>
/// M-1 修复：采用懒加载（Lazy Init）策略，构造函数仅预注册所有应用的 Lazy 上下文并通过反射预置基类
/// <c>_defaultAppKey</c> 字段，不立即创建任何应用实例。应用在首次访问 GetApp/GetDefaultApp/TryGetApp 时按需创建，
/// 减少启动延迟并避免构造阶段强制要求所有 DI 依赖（如 IMemoryCache）就绪。
/// </para>
/// <para>
/// M-2 修复：类可见性从 internal 改为 public，允许用户继承并覆盖 <see cref="CreateAppContext"/> 以支持自定义 <see cref="IFeishuAppContext"/>。
/// </para>
/// </remarks>
public class FeishuAppManager : DefaultAppManager<IFeishuAppContext>, IFeishuAppManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FeishuAppManager> _logger;
    private readonly ConcurrentDictionary<string, Lazy<FeishuAppContext>> _lazyContexts = new();
    private readonly List<FeishuAppConfig> _configs;
    // A-1 修复：本类私有的默认应用键，替代反射设置基类 _defaultAppKey 的反模式。
    // 构造阶段赋值（单线程），AddApp/RemoveApp 时同步更新，GetDefaultApp 直接读取。
    private string? _defaultAppKey;

    /// <summary>
    /// 初始化飞书应用管理器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="configs">应用配置列表</param>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
    public FeishuAppManager(
        IServiceProvider serviceProvider,
        IEnumerable<FeishuAppConfig> configs,
        ILogger<FeishuAppManager> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configs = (configs as IList<FeishuAppConfig> ?? configs.ToList()).ToList();

        if (_configs.Count == 0)
            throw new InvalidOperationException("未配置任何飞书应用");

        // M-1 修复：预注册所有应用的 Lazy 上下文，但不立即创建。
        // 注意：不在此处做"重复 AppKey"校验，保持与原始 API 契约一致——后注册的同名应用覆盖先注册的。
        // 若需严格校验，可在调用方通过 config.Validate() 或自定义逻辑实现。
        foreach (var config in _configs)
        {
            var capturedConfig = config;
            _lazyContexts[config.AppKey] = new Lazy<FeishuAppContext>(
                () => CreateAppContext(capturedConfig),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        // 校验：必须存在标记为 IsDefault=true 的应用，否则抛出异常（对应 MultiApp_NoDefaultApp_ShouldThrowOnRegistration 契约）。
        // 注意：不在此处预创建默认应用，避免强制要求所有 DI 依赖（如 IMemoryCache）在构造阶段就绪，
        // 默认应用将在首次访问 GetDefaultApp()/Default*TokenManager 时按需懒加载创建。
        var defaultConfig = _configs.FirstOrDefault(c => c.IsDefault);
        if (defaultConfig == null)
            throw new InvalidOperationException("未配置默认应用，请在至少一个 FeishuAppConfig 上设置 IsDefault = true。");

        // A-1 修复：不再通过反射设置基类私有字段 _defaultAppKey（反模式且基类字段无法加锁）。
        // 改为本类私有字段，GetDefaultApp() 直接读取，AddApp/RemoveApp 同步更新。
        _defaultAppKey = defaultConfig.AppKey;

        _logger.LogInformation("飞书应用管理器初始化完成，共配置 {Count} 个应用，默认应用: {AppKey}",
            _configs.Count, defaultConfig.AppKey);

        WarnResilienceConfigMismatch(_configs);

        // S-1 修复：启动期检测 IEncryptionProvider 注册状态。
        // 若未注册，使用 [Body(EnableEncrypt=true)] 的 API 将在首次请求时抛 InvalidOperationException（运行时失败而非编译期诊断）。
        // 此处发出警告使失败模式提前可见，便于用户在首次请求前补注册。
        if (_serviceProvider.GetService<IEncryptionProvider>() == null)
        {
            _logger.LogWarning(
                "未注册 IEncryptionProvider。若使用 [Body(EnableEncrypt=true)] 标记的 API，" +
                "首次请求将抛出 InvalidOperationException。请通过 AddMudHttpClient 配置 AesEncryptionOptions 或注册自定义 IEncryptionProvider。");
        }
    }

    /// <summary>
    /// 获取或创建指定应用的上下文（懒加载）
    /// </summary>
    /// <param name="appKey">应用唯一标识</param>
    /// <returns>应用上下文实例</returns>
    /// <exception cref="InvalidOperationException">当应用未配置或创建失败时抛出</exception>
    private FeishuAppContext GetOrCreateContext(string appKey)
    {
        if (_lazyContexts.TryGetValue(appKey, out var lazy))
        {
            try
            {
                var context = lazy.Value;
                // 注册到基类字典中（如果尚未注册）
                // 注意：必须使用 base.HasApp 检查"是否已注册到基类字典"，
                // 而非使用 HasApp（后者会同时检查 _lazyContexts，导致永远跳过 RegisterApp）。
                if (!base.HasApp(appKey))
                {
                    var config = _configs.First(c => c.AppKey == appKey);
                    RegisterApp(appKey, context, config.IsDefault);
                }
                return context;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"应用 '{appKey}' 初始化失败: {ex.Message}", ex);
            }
        }

        throw new InvalidOperationException(
            $"未找到应用标识为 '{appKey}' 的应用上下文。请先调用 RegisterApp 注册应用。");
    }

    /// <inheritdoc />
    public override IFeishuAppContext GetApp(string appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("应用标识不能为空", nameof(appKey));

        // 先检查已注册的应用（包括懒加载已创建的）
        if (TryGetApp(appKey, out var context) && context != null)
            return context;

        // M-1 修复：未注册时尝试懒加载创建
        return GetOrCreateContext(appKey);
    }

    /// <inheritdoc />
    /// <remarks>
    /// M-1 修复补充：基类 <see cref="DefaultAppManager{TAppContext}.HasApp"/> 仅检查已实例化的应用字典，
    /// 不会感知 <c>_lazyContexts</c> 中预注册但尚未实例化的应用。重写后同时检查两个字典，
    /// 确保"已配置但未访问"的应用也能被正确识别为"已注册"。
    /// </remarks>
    public override bool HasApp(string appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            return false;

        // 已实例化的应用：基类字典已记录
        if (base.HasApp(appKey))
            return true;

        // 已配置但尚未实例化的应用：Lazy 字典已记录
        return _lazyContexts.ContainsKey(appKey);
    }

    /// <inheritdoc />
    /// <remarks>
    /// M-1 修复补充：基类 <see cref="DefaultAppManager{TAppContext}.TryGetApp"/> 仅查询已实例化的应用字典，
    /// 对已配置但未实例化的应用返回 false。重写后：
    /// <list type="bullet">
    /// <item>已实例化：直接返回（与基类一致）。</item>
    /// <item>已配置但未实例化：触发 Lazy 创建并注册到基类字典，返回 true。</item>
    /// <item>创建过程抛异常：捕获并返回 false（保持 Try* 语义不抛异常）。</item>
    /// <item>未配置：返回 false（与基类一致）。</item>
    /// </list>
    /// </remarks>
    public override bool TryGetApp(string appKey, out IFeishuAppContext? appContext)
    {
        if (string.IsNullOrWhiteSpace(appKey))
        {
            appContext = default;
            return false;
        }

        // 已实例化的应用：基类字典已记录
        if (base.TryGetApp(appKey, out appContext) && appContext != null)
            return true;

        // 已配置但尚未实例化的应用：触发 Lazy 创建
        if (_lazyContexts.TryGetValue(appKey, out var lazy))
        {
            try
            {
                var context = lazy.Value;
                // 注册到基类字典以便后续快速查找
                if (!base.HasApp(appKey))
                {
                    var config = _configs.First(c => c.AppKey == appKey);
                    RegisterApp(appKey, context, config.IsDefault);
                }
                appContext = context;
                return true;
            }
            catch
            {
                // 保持 Try* 语义：创建失败时返回 false 而非抛出异常
                appContext = default;
                return false;
            }
        }

        // 未配置的应用
        appContext = default;
        return false;
    }

    /// <inheritdoc />
    public override IEnumerable<IFeishuAppContext> GetAllApps()
    {
        // 强制创建所有预配置的应用，确保它们被注册到基类字典中
        foreach (var appKey in _lazyContexts.Keys)
        {
            GetOrCreateContext(appKey);
        }
        return base.GetAllApps();
    }

    /// <inheritdoc />
    /// <remarks>
    /// M-1 修复补充：由于采用懒加载，应用可能仅存在于 <c>_lazyContexts</c> 而尚未注册到基类 <c>_apps</c> 字典。
    /// 此时 <see cref="DefaultAppManager{TAppContext}.RemoveApp"/> 会返回 false，与"应用已知即应可移除"的语义不符。
    /// 重写后：只要应用在任一字典中存在，即返回 true；并清理两个字典中的记录。
    /// </remarks>
    public override bool RemoveApp(string appKey)
    {
        var wasInLazy = _lazyContexts.TryRemove(appKey, out _);
        var wasInBase = base.RemoveApp(appKey);

        // A-1 修复：若移除的是当前默认应用，清空本类默认应用键
        if ((wasInLazy || wasInBase) && _defaultAppKey == appKey)
            _defaultAppKey = null;

        return wasInLazy || wasInBase;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A-1 修复：不再依赖基类反射设置的 _defaultAppKey，直接读取本类私有字段。
    /// 基类 GetDefaultApp() 为非虚方法，若调用方强制转换为 DefaultAppManager&lt;IFeishuAppContext&gt; 并调用基类版本，
    /// 会因基类 _defaultAppKey 未设置而抛出——该场景属非常规用法，错误信息清晰可接受。
    /// </remarks>
    public new IFeishuAppContext GetDefaultApp()
    {
        var defaultKey = _defaultAppKey;
        if (string.IsNullOrEmpty(defaultKey))
            throw new InvalidOperationException("未设置默认应用。请在注册应用时设置 isDefault = true。");

        return GetApp(defaultKey!);
    }

    /// <summary>
    /// 租户令牌管理器
    /// </summary>
    public ITenantTokenManager DefaultTenantTokenManager => GetDefaultApp().TenantTokenManager;

    /// <summary>
    /// 应用令牌管理器
    /// </summary>
    public IAppTokenManager DefaultAppTokenManager => GetDefaultApp().AppTokenManager;

    /// <summary>
    /// 用户令牌管理器
    /// </summary>
    public IFeishuUserTokenManager DefaultUserTokenManager => GetDefaultApp().UserTokenManager;

    /// <summary>
    /// 默认应用配置
    /// </summary>
    public FeishuAppConfig DefaultConfig => GetDefaultApp().Config;

    /// <summary>
    /// 运行时添加应用
    /// </summary>
    public IFeishuAppContext AddApp(FeishuAppConfig config)
    {
        config.Validate();

        if (HasApp(config.AppKey))
            throw new InvalidOperationException($"应用 {config.AppKey} 已存在");

        var context = CreateAppContext(config);
        RegisterApp(config.AppKey, context, config.IsDefault);
        // 同步注册到懒加载字典，使 GetAllApps 能正确枚举
        _lazyContexts[config.AppKey] = new Lazy<FeishuAppContext>(() => context);

        // A-1 修复：若新应用标记为默认，更新本类的默认应用键
        if (config.IsDefault)
            _defaultAppKey = config.AppKey;

        return context;
    }

    /// <summary>
    /// 根据应用键获取飞书API实例
    /// </summary>
    /// <remarks>
    /// 重写基类方法，从 DI 容器获取已注册的服务并调用 UseApp 切换应用上下文。
    /// </remarks>
    public override IFeishuAppContext GetWebApi<IFeishuAppContext>(string appKey)
    {
        var service = _serviceProvider.GetService<IFeishuAppContext>();
        if (service == null)
            throw new InvalidOperationException($"未注册飞书API服务: {typeof(IFeishuAppContext).FullName}");
        service.UseApp(appKey);
        return service;
    }

    /// <summary>
    /// 获取默认应用的飞书API实例
    /// </summary>
    /// <remarks>
    /// 重写基类方法，从 DI 容器获取已注册的服务并调用 UseDefaultApp 切换应用上下文。
    /// </remarks>
    public override IFeishuAppContext GetDefaultWebApi<IFeishuAppContext>()
    {
        var service = _serviceProvider.GetService<IFeishuAppContext>();
        if (service == null)
            throw new InvalidOperationException($"未注册飞书API服务: {typeof(IFeishuAppContext).FullName}");
        service.UseDefaultApp();
        return service;
    }


    bool IAppManager<IFeishuAppContext>.TryGetApp(string appKey, out IFeishuAppContext? appContext)
    {
        var result = TryGetApp(appKey, out var ctx);
        appContext = ctx;
        return result;
    }

    void IAppManager<IFeishuAppContext>.RegisterApp(string appKey, IFeishuAppContext appContext, bool isDefault)
    {
        if (appContext is not FeishuAppContext ctx)
            throw new ArgumentException("应用上下文必须是 FeishuAppContext 类型", nameof(appContext));
        RegisterApp(appKey, ctx, isDefault);
    }

    async Task IAppManager<IFeishuAppContext>.RegisterAppAsync(string appKey, IFeishuAppContext appContext, bool isDefault, CancellationToken cancellationToken)
    {
        if (appContext is not FeishuAppContext ctx)
            throw new ArgumentException("应用上下文必须是 FeishuAppContext 类型", nameof(appContext));
        await RegisterAppAsync(appKey, ctx, isDefault, cancellationToken);
    }

    void IAppManager<IFeishuAppContext>.UpdateApp(string appKey, IFeishuAppContext appContext)
    {
        if (appContext is not FeishuAppContext ctx)
            throw new ArgumentException("应用上下文必须是 FeishuAppContext 类型", nameof(appContext));
        UpdateApp(appKey, ctx);
    }

    void IAppManager<IFeishuAppContext>.RegisterSwitcherFactory<TContextSwitcher>(Func<IFeishuAppContext, TContextSwitcher> factory)
    {
        RegisterSwitcherFactory<TContextSwitcher>(ctx => factory(ctx));
    }

    /// <summary>
    /// 创建应用上下文（可由子类重写以支持自定义 <see cref="IFeishuAppContext"/>）
    /// </summary>
    /// <param name="config">应用配置</param>
    /// <returns>飞书应用上下文实例</returns>
    protected virtual FeishuAppContext CreateAppContext(FeishuAppConfig config)
    {
        var currentUserContext = _serviceProvider.GetService<IFeishuCurrentUserContext>();
        var jsonSerializerOptions = Options.Create(_serviceProvider.GetRequiredService<JsonSerializerOptions>());
        var memoryCache = _serviceProvider.GetRequiredService<IMemoryCache>();
        var clientName = $"feishu-{config.AppKey}";

        // === 步骤 1：创建基础 HttpClient（不含恢复，供 AuthenticationApi 使用） ===
        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        var encryptionProvider = _serviceProvider.GetService<IEncryptionProvider>();
        var enhancedOptions = CreateEnhancedHttpClientOptions(clientName);

        var basicHttpClient = new HttpClientFactoryEnhancedClient(
            httpClientFactory, clientName, encryptionProvider, enhancedOptions);

        // === 步骤 2：创建 AuthenticationApi（使用基础 HttpClient） ===
        var authenticationApi = (IFeishuAuthentication)ActivatorUtilities.CreateInstance(
            _serviceProvider, typeof(FeishuAuthentication), jsonSerializerOptions, basicHttpClient);
        var options = Options.Create(config);

        // === 步骤 3：创建 TokenManager（依赖 AuthenticationApi） ===
        // C-2 修复：为每个应用创建独立的 FeishuTokenStore / FeishuUserTokenStore 实例（含 AppKey 隔离维度）
        // 若 DI 中注册的是自定义 ITokenStore（如 RedisTokenStore），则直接使用 DI 实例（假设其已处理多应用隔离）
        ITokenStore tokenStore;
        IUserTokenStore? userTokenStore;

        var diTokenStore = _serviceProvider.GetService<ITokenStore>();
        if (diTokenStore is FeishuTokenStore)
        {
            var feishuTokenStore = new FeishuTokenStore(memoryCache, config.AppKey);
            tokenStore = feishuTokenStore;
            userTokenStore = new FeishuUserTokenStore(feishuTokenStore, memoryCache, config.AppKey);
        }
        else
        {
            tokenStore = diTokenStore!;
            userTokenStore = _serviceProvider.GetService<IUserTokenStore>();
        }

        var tenantTokenManager = new TenantTokenManager(
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<TenantTokenManager>>(),
            tokenStore);

        var appTokenManager = new AppTokenManager(
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<AppTokenManager>>(),
            tokenStore);

        var userTokenManager = new UserTokenManager(
            currentUserContext,
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<UserTokenManager>>(),
            userTokenStore);

        // === 步骤 4：创建恢复 HttpClient（含令牌恢复，供业务 API 使用） ===
        var recoveryOptions = _serviceProvider.GetService<IOptions<TokenRecoveryOptions>>()?.Value;
        var recoveryLogger = _serviceProvider.GetService<ILogger<TokenRecoveryEnhancedClient>>();

        var recoveryExecutor = new TokenRecoveryExecutor(
            tenantTokenManager,
            userTokenManager as IUserTokenManager,
            currentUserContext as ICurrentUserContext,
            recoveryOptions,
            recoveryLogger);

        var recoveryHttpClient = new TokenRecoveryEnhancedClient(
            httpClientFactory, clientName, recoveryExecutor,
            encryptionProvider, enhancedOptions);

        // === 步骤 5：创建应用上下文（使用恢复 HttpClient） ===
        return new FeishuAppContext(
            config,
            tenantTokenManager,
            appTokenManager,
            userTokenManager,
            authenticationApi,
            recoveryHttpClient,
            _serviceProvider);
    }

    /// <summary>
    /// 从 DI 容器构建 EnhancedHttpClientOptions，与 AddMudHttpClient 内部 CreateEnhancedClient 逻辑保持一致。
    /// </summary>
    private EnhancedHttpClientOptions CreateEnhancedHttpClientOptions(string clientName)
    {
        var options = new EnhancedHttpClientOptions
        {
            Logger = _serviceProvider.GetService<ILogger<HttpClientFactoryEnhancedClient>>(),
            RequestInterceptors = _serviceProvider.GetServices<IHttpRequestInterceptor>(),
            ResponseInterceptors = _serviceProvider.GetServices<IHttpResponseInterceptor>(),
            SensitiveDataMasker = _serviceProvider.GetService<ISensitiveDataMasker>()
        };

        var optionsMonitor = _serviceProvider.GetService<IOptionsMonitor<MudHttpClientApplicationOptions>>();
        if (optionsMonitor != null)
        {
            var appOptions = optionsMonitor.CurrentValue;
            if (appOptions.Clients.TryGetValue(clientName, out var clientOptions))
            {
                options.AllowCustomBaseUrls = clientOptions.AllowCustomBaseUrls;
            }
        }

        return options;
    }

    private void WarnResilienceConfigMismatch(IList<FeishuAppConfig> configs)
    {
        if (configs.Count <= 1)
            return;

        var defaultConfig = configs.FirstOrDefault(c => c.IsDefault) ?? configs.FirstOrDefault();
        if (defaultConfig == null)
            return;

        var nonDefaultConfigs = configs
            .Where(c => c != defaultConfig && HasResilienceMismatch(c, defaultConfig))
            .ToList();

        if (nonDefaultConfigs.Count > 0)
        {
            // A-3 修复：方法名 WarnResilienceConfigMismatch 暗示 Warning 级别，原用 LogInformation 与语义不符，
            // 且降低了多应用弹性策略不一致的可观测性。改回 LogWarning。
            _logger.LogWarning(
                "多应用模式下检测到不同应用具有不同的弹性策略配置（重试、超时、熔断）。" +
                "Per-App 弹性策略已启用，各应用将使用独立的策略配置。默认应用 '{DefaultAppKey}' 的配置用于全局回退（如 ResilientHttpClient 装饰器）。",
                defaultConfig.AppKey);
        }
    }

    private static bool HasResilienceMismatch(FeishuAppConfig app, FeishuAppConfig defaultApp)
    {
        return app.RetryCount != defaultApp.RetryCount
            || app.RetryDelayMs != defaultApp.RetryDelayMs
            || app.TimeOut != defaultApp.TimeOut
            || app.CircuitBreakerEnabled != defaultApp.CircuitBreakerEnabled
            || app.CircuitBreakerFailureThreshold != defaultApp.CircuitBreakerFailureThreshold
            || app.CircuitBreakerSamplingDurationSeconds != defaultApp.CircuitBreakerSamplingDurationSeconds
            || app.CircuitBreakerBreakDurationSeconds != defaultApp.CircuitBreakerBreakDurationSeconds
            || app.CircuitBreakerMinimumThroughput != defaultApp.CircuitBreakerMinimumThroughput;
    }
}
