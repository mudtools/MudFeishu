// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Internal;
using Mud.Feishu.TokenManager;
using System.Text.Json;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用管理器实现
/// </summary>
/// <remarks>
/// 负责管理系统中所有飞书应用的创建、获取、移除等操作。
/// 每个应用拥有独立的配置、缓存和TokenManager实例。
/// </remarks>
internal class FeishuAppManager : IFeishuAppManager
{
    /// <summary>
    /// 服务提供者
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 应用上下文字典
    /// </summary>
    private readonly Dictionary<string, FeishuAppContext> _apps;

    /// <summary>
    /// 日志记录器
    /// </summary>
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
        _apps = new Dictionary<string, FeishuAppContext>(StringComparer.OrdinalIgnoreCase);

        // 初始化所有应用
        foreach (var config in configs)
        {
            CreateAppContext(config);
        }

        // 验证必须有默认应用（已在配置加载阶段设置）
        if (!_apps.Values.Any(a => a.Config.IsDefault))
        {
            throw new InvalidOperationException("未配置默认应用");
        }

        _logger.LogInformation("飞书应用管理器初始化完成，共加载 {Count} 个应用", _apps.Count);
    }

    /// <summary>
    /// 根据应用键获取飞书API实例
    /// </summary>
    /// <typeparam name="T">飞书API类型</typeparam>
    /// <param name="appKey">应用键</param>
    /// <returns>指定应用的飞书API实例</returns>
    public T GetWebApi<T>(string appKey) where T : IAppContextSwitcher
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
    /// <typeparam name="T">飞书API类型</typeparam>
    /// <returns>默认应用的飞书API实例</returns>
    public T GetDefalutWebApi<T>() where T : IAppContextSwitcher
    {
        var service = _serviceProvider.GetService<T>();
        if (service == null)
            throw new InvalidOperationException($"未注册飞书API服务: {typeof(T).FullName}");
        service.UseDefaultApp();
        return service;
    }


    /// <summary>
    /// 租户令牌管理器
    /// </summary>
    /// <remarks>
    /// 用于获取和管理租户访问令牌（Tenant Access Token）。
    /// 租户令牌用于租户级别的权限验证。
    /// </remarks>
    public ITenantTokenManager DefaultTenantTokenManager => GetDefaultApp().TenantTokenManager;

    /// <summary>
    /// 应用令牌管理器
    /// </summary>
    /// <remarks>
    /// 用于获取和管理应用身份访问令牌（App Access Token）。
    /// 应用令牌用于应用级别的权限验证。
    /// </remarks>
    public IAppTokenManager DefaultAppTokenManager => GetDefaultApp().AppTokenManager;

    /// <summary>
    /// 用户令牌管理器
    /// </summary>
    /// <remarks>
    /// 用于获取和管理用户访问令牌（User Access Token）。
    /// 用户令牌通过OAuth授权流程获取，需要用户授权。
    /// </remarks>
    public IFeishuUserTokenManager DefaultUserTokenManager => GetDefaultApp().UserTokenManager;


    /// <summary>
    /// 应用配置
    /// </summary>
    /// <remarks>
    /// 包含此应用的所有配置信息，如AppId、AppSecret、BaseUrl等。
    /// </remarks>
    public FeishuAppConfig DefaultConfig => GetDefaultApp().Config;

    /// <summary>
    /// 获取默认应用上下文
    /// </summary>
    public IFeishuAppContext GetDefaultApp()
    {
        var defaultApp = _apps.Values.FirstOrDefault(a => a.Config.IsDefault)
            ?? throw new InvalidOperationException("未配置默认应用");

        return defaultApp;
    }

    /// <summary>
    /// 获取指定应用上下文
    /// </summary>
    public IFeishuAppContext GetApp(string appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            return GetDefaultApp();

        if (_apps.TryGetValue(appKey, out var app))
            return app;

        throw new KeyNotFoundException($"未找到飞书应用: {appKey}");
    }

    /// <summary>
    /// 尝试获取应用上下文
    /// </summary>
    public bool TryGetApp(string appKey, out IFeishuAppContext? appContext)
    {
        if (string.IsNullOrWhiteSpace(appKey))
        {
            try
            {
                appContext = GetDefaultApp();
                return true;
            }
            catch
            {
                appContext = null;
                return false;
            }
        }

        var result = _apps.TryGetValue(appKey, out var appContext1);
        appContext = appContext1;
        return result;
    }

    /// <summary>
    /// 获取所有已注册的应用
    /// </summary>
    public IEnumerable<IFeishuAppContext> GetAllApps()
    {
        return _apps.Values;
    }

    /// <summary>
    /// 检查应用是否存在
    /// </summary>
    public bool HasApp(string appKey)
    {
        return _apps.ContainsKey(appKey);
    }

    /// <summary>
    /// 运行时添加应用
    /// </summary>
    public IFeishuAppContext AddApp(FeishuAppConfig config)
    {
        config.Validate();

        if (_apps.ContainsKey(config.AppKey))
            throw new InvalidOperationException($"应用 {config.AppKey} 已存在");

        var context = CreateAppContext(config);
        return context;
    }

    /// <summary>
    /// 移除应用
    /// </summary>
    public bool RemoveApp(string appKey)
    {
        if (!_apps.TryGetValue(appKey, out var app))
            return false;

        // 如果是最后一个应用，不允许移除
        if (_apps.Count == 1)
            throw new InvalidOperationException("不能移除唯一的飞书应用");

        if (app.Config.IsDefault && _apps.Count > 1)
            throw new InvalidOperationException("不能移除默认应用");

        app.Dispose();
        return _apps.Remove(appKey);
    }

    /// <summary>
    /// 创建应用上下文
    /// </summary>
    private FeishuAppContext CreateAppContext(FeishuAppConfig config)
    {
        config.Validate();

        var memoryCacheLogger = _serviceProvider.GetRequiredService<ILogger<MemoryTokenCache>>();
        var currentUserContext = _serviceProvider.GetService<ICurrentUserContext>();
        var appCache = new MemoryTokenCache(memoryCacheLogger, config.TokenRefreshThreshold);
        var prefixedCache = new PrefixedTokenCache(appCache, config.AppKey);

        var userTokenCacheLogger = _serviceProvider.GetRequiredService<ILogger<MemoryUserTokenCache>>();
        var appUserTokenCache = new MemoryUserTokenCache(userTokenCacheLogger, config.TokenRefreshThreshold);
        var prefixedUserTokenCache = new PrefixedUserTokenCache(appUserTokenCache, config.AppKey);

        var jsonSerializerOptions = _serviceProvider.GetRequiredService<IOptions<JsonSerializerOptions>>();

        var httpClient = CreateHttpClient(config, jsonSerializerOptions);

        var authenticationApi = new FeishuAuthentication(jsonSerializerOptions, httpClient);

        var tokenManagerLogger = _serviceProvider.GetRequiredService<ILogger<TokenManagerWithCache>>();

        var options = Options.Create(config);

        var tenantTokenManager = new TenantTokenManager(
            authenticationApi,
            options,
            tokenManagerLogger,
            prefixedCache);

        var appTokenManager = new AppTokenManager(
            authenticationApi,
            options,
            tokenManagerLogger,
            prefixedCache);

        var userTokenManagerLogger = _serviceProvider.GetRequiredService<ILogger<TokenManager.UserTokenManager>>();
        var userTokenManager = new UserTokenManager(
            authenticationApi,
            currentUserContext,
            options,
            userTokenManagerLogger,
            prefixedUserTokenCache);

        var context = new FeishuAppContext(
            config,
            tenantTokenManager,
            appTokenManager,
            userTokenManager,
            authenticationApi,
            prefixedCache,
            httpClient);

        _apps[config.AppKey] = context;

        _logger.LogInformation("创建飞书应用上下文: {AppKey} (AppId: {AppId})",
            config.AppKey, config.AppId);

        return context;
    }

    /// <summary>
    /// 创建独立的HttpClient实例
    /// </summary>
    private IEnhancedHttpClient CreateHttpClient(FeishuAppConfig config, IOptions<JsonSerializerOptions> jsonSerializerOptions)
    {
        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient($"feishu-{config.AppKey}");
        var logger = _serviceProvider.GetRequiredService<ILogger<FeishuHttpClient>>();

        // 配置HttpClient
        httpClient.BaseAddress = new Uri(config.BaseUrl ?? "https://open.feishu.cn");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "MudFeishuClient/1.0");
        httpClient.Timeout = TimeSpan.FromSeconds(config.TimeOut);

        return new FeishuHttpClient(httpClient, logger,
            config.EnableLogging,
            jsonSerializerOptions,
            new EnhancedHttpClientOptions { AllowCustomBaseUrl = config.AllowCustomBaseUrl });
    }
}
