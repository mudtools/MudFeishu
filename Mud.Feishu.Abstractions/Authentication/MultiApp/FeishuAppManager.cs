// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Internal;
using System.Text.Json;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用管理器实现
/// </summary>
/// <remarks>
/// 负责管理系统中所有飞书应用的创建、获取、移除等操作。
/// 每个应用拥有独立的配置、缓存和TokenManager实例。
/// 继承 DefaultAppManager&lt;FeishuAppContext&gt; 获得通用的应用管理能力。
/// </remarks>
internal class FeishuAppManager : DefaultAppManager<IFeishuAppContext>, IFeishuAppManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FeishuAppManager> _logger;

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

        var configList = configs as IList<FeishuAppConfig> ?? configs.ToList();

        foreach (var config in configList)
        {
            var context = CreateAppContext(config);
            RegisterApp(config.AppKey, context, config.IsDefault);
        }

        var defaultApp = GetDefaultApp();
        if (defaultApp == null || !HasApp(defaultApp.Config.AppKey))
        {
            throw new InvalidOperationException("未配置默认应用");
        }

        WarnResilienceConfigMismatch(configList);

        _logger.LogInformation("飞书应用管理器初始化完成，共加载 {Count} 个应用", GetAllApps().Count());
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
    /// 创建应用上下文
    /// </summary>
    private FeishuAppContext CreateAppContext(FeishuAppConfig config)
    {
        var currentUserContext = _serviceProvider.GetService<IFeishuCurrentUserContext>();
        var jsonSerializerOptions = Options.Create(_serviceProvider.GetRequiredService<JsonSerializerOptions>());

        var httpClient = CreateHttpClient(config);
        var authenticationApi = _serviceProvider.GetService<IFeishuAuthentication>()
            ?? (IFeishuAuthentication)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(FeishuAuthentication), jsonSerializerOptions, httpClient);
        var options = Options.Create(config);


        var tenantTokenManager = new TenantTokenManager(
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<TenantTokenManager>>(),
            _serviceProvider.GetService<ITokenStore>());

        var appTokenManager = new AppTokenManager(
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<AppTokenManager>>(),
            _serviceProvider.GetService<ITokenStore>());

        var userTokenManager = new UserTokenManager(
            currentUserContext,
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<UserTokenManager>>(),
            _serviceProvider.GetService<IUserTokenStore>());

        return new FeishuAppContext(
            config,
            tenantTokenManager,
            appTokenManager,
            userTokenManager,
            authenticationApi,
            httpClient);
    }

    private IEnhancedHttpClient CreateHttpClient(FeishuAppConfig config)
    {
        var httpClientResolver = _serviceProvider.GetRequiredService<IHttpClientResolver>();
        var clientName = $"feishu-{config.AppKey}";
        return httpClientResolver.GetClient(clientName);
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
            _logger.LogWarning(
                "多应用模式下弹性策略（重试、超时、熔断）为全局共享配置，当前使用默认应用 '{DefaultAppKey}' 的配置。" +
                "以下应用的自定义 Resilience 配置将被忽略: {IgnoredApps}。" +
                "这是 Mud.HttpUtils 框架的设计限制，所有命名客户端共享同一组弹性策略。",
                defaultConfig.AppKey,
                string.Join(", ", nonDefaultConfigs.Select(c => c.AppKey)));
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
