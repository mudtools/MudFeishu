// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.TokenManager;
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

        foreach (var config in configs)
        {
            var context = CreateAppContext(config);
            RegisterApp(config.AppKey, context, config.IsDefault);
        }

        if (!HasApp(GetDefaultApp().Config.AppKey))
        {
            throw new InvalidOperationException("未配置默认应用");
        }

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
    public new T GetWebApi<T>(string appKey) where T : IAppContextSwitcher
    {
        var service = _serviceProvider.GetService<T>();
        if (service == null)
            throw new InvalidOperationException($"未注册飞书API服务: {typeof(T).FullName}");
        service.UseApp(appKey);
        return service;
    }

    /// <summary>
    /// 获取默认应用的飞书API实例
    /// </summary>
    public new T GetDefaultWebApi<T>() where T : IAppContextSwitcher
    {
        var service = _serviceProvider.GetService<T>();
        if (service == null)
            throw new InvalidOperationException($"未注册飞书API服务: {typeof(T).FullName}");
        service.UseDefaultApp();
        return service;
    }

    /// <summary>
    /// 获取默认应用的飞书API实例（已废弃，请使用 GetDefaultWebApi）
    /// </summary>
    [Obsolete("请使用 GetDefaultWebApi<T>() 替代。此方法将在未来版本中移除。")]
    public new T GetDefalutWebApi<T>() where T : IAppContextSwitcher
    {
        return GetDefaultWebApi<T>();
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
        config.Validate();

        var currentUserContext = _serviceProvider.GetService<ICurrentUserContext>();
        var jsonSerializerOptions = _serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();
        var httpClient = CreateHttpClient(config, jsonSerializerOptions);
        var authenticationApi = _serviceProvider.GetRequiredService<IFeishuAuthentication>();
        var options = Options.Create(config);

        var tenantTokenManager = new TenantTokenManager(
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<TenantTokenManager>>());

        var appTokenManager = new AppTokenManager(
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<AppTokenManager>>());

        var userTokenManager = new UserTokenManager(
            currentUserContext,
            authenticationApi,
            options,
            _serviceProvider.GetRequiredService<ILogger<UserTokenManager>>());

        return new FeishuAppContext(
            config,
            tenantTokenManager,
            appTokenManager,
            userTokenManager,
            authenticationApi,
            httpClient);
    }

    /// <summary>
    /// 创建独立的HttpClient实例
    /// </summary>
    private IEnhancedHttpClient CreateHttpClient(FeishuAppConfig config, IOptions<JsonSerializerOptions> jsonSerializerOptions)
    {
        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient($"feishu-{config.AppKey}");
        var logger = _serviceProvider.GetRequiredService<ILogger<FeishuHttpClient>>();

        httpClient.BaseAddress = new Uri(config.BaseUrl ?? "https://open.feishu.cn");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "MudFeishuClient/1.0");
        httpClient.Timeout = TimeSpan.FromSeconds(config.TimeOut);

        return new FeishuHttpClient(httpClient, logger, config.EnableLogging, jsonSerializerOptions);
    }
}
