// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

#if NET6_0_OR_GREATER
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书令牌注册托管服务
/// </summary>
/// <remarks>
/// <para>
/// TMA-08 / P1-7 修复（D6 契约）：
/// <list type="bullet">
/// <item>启动期默认不实例化全部应用；仅注册默认应用的令牌管理器到后台刷新服务。</item>
/// <item>全量预热改为显式选项 <c>FeishuAppOptions.WarmUpAllAppsOnStartup</c>（默认 false）。</item>
/// <item>逐应用 try/catch + LogError，单应用失败不阻断宿主启动。</item>
/// <item>订阅 ConfigurationChanged + AppInstantiated 事件做增量注册。</item>
/// </list>
/// </para>
/// <para>
/// TMA-24 修复注释：后台服务只管理 Timer 与刷新周期，不管理令牌管理器生命周期。
/// 令牌管理器的生命周期由 FeishuAppManager 管理（含退休队列）。
/// </para>
/// </remarks>
internal sealed class FeishuTokenRegistrationService : IHostedService
{
    private readonly IFeishuAppManager _appManager;
    private readonly ITokenRefreshBackgroundService _refreshService;
    private readonly ILogger<FeishuTokenRegistrationService> _logger;
    private readonly FeishuAppOptions _appOptions;

    public FeishuTokenRegistrationService(
        IFeishuAppManager appManager,
        ITokenRefreshBackgroundService refreshService,
        ILogger<FeishuTokenRegistrationService> logger,
        IOptions<FeishuAppOptions>? appOptions = null)
    {
        _appManager = appManager ?? throw new ArgumentNullException(nameof(appManager));
        _refreshService = refreshService ?? throw new ArgumentNullException(nameof(refreshService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appOptions = appOptions?.Value ?? new FeishuAppOptions();
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        // TMA-08：默认仅注册默认应用（触发懒加载），其余应用在首次访问时增量注册。
        var registered = 0;

        // 始终注册默认应用
        try
        {
            var defaultApp = _appManager.GetDefaultApp();
            registered += RegisterAppTokenManagers(defaultApp);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "启动期注册默认应用的令牌管理器失败。后台令牌刷新将退化为懒加载模式。");
        }

        // WarmUpAllAppsOnStartup=true 时逐应用预热，单应用失败不阻断
        if (_appOptions.WarmUpAllAppsOnStartup)
        {
            foreach (var appKey in _appManager.ConfiguredAppKeys)
            {
                // 默认应用已注册，跳过
                if (string.Equals(appKey, _appManager.DefaultConfig.AppKey, StringComparison.Ordinal))
                    continue;

                try
                {
                    var app = _appManager.GetApp(appKey);
                    registered += RegisterAppTokenManagers(app);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex,
                        "启动期预热应用 {AppKey} 失败，已跳过。其余应用不受影响。",
                        appKey);
                }
            }
        }

        _logger.LogInformation(
            "已将 {TokenCount} 个令牌管理器注册到后台刷新服务（WarmUpAllAppsOnStartup={WarmUp}）",
            registered, _appOptions.WarmUpAllAppsOnStartup);

        // 订阅配置变更事件，做增量注册
        if (_appManager is FeishuAppManager feishuAppManager)
        {
            feishuAppManager.ConfigurationChanged += OnConfigurationChanged;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        // TMA-24 修复注释：后台服务不管理令牌管理器生命周期，只管理 Timer。
        // 令牌管理器的清理由 FeishuAppManager.Dispose → 退休队列 → ODE 自清完成。
        if (_appManager is FeishuAppManager feishuAppManager)
        {
            feishuAppManager.ConfigurationChanged -= OnConfigurationChanged;
        }

        return Task.CompletedTask;
    }

    private void OnConfigurationChanged(object? sender, AppConfigurationChangedEventArgs e)
    {
        // TMA-08：增量注册——新增/更新应用时重新注册令牌管理器到后台刷新服务。
        // 同名键覆盖 → 新实例接管；旧实例由退休队列 Dispose 后 ODE 自清。
        try
        {
            if (e.ChangeType == AppConfigurationChangeType.Added ||
                e.ChangeType == AppConfigurationChangeType.Updated)
            {
                if (_appManager.TryGetApp(e.AppKey, out var app) && app != null)
                {
                    RegisterAppTokenManagers(app);
                    _logger.LogInformation(
                        "配置变更：已将应用 {AppKey} 的令牌管理器增量注册到后台刷新服务（{ChangeType}）",
                        e.AppKey, e.ChangeType);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "配置变更后增量注册应用 {AppKey} 的令牌管理器失败。",
                e.AppKey);
        }
    }

    private int RegisterAppTokenManagers(IFeishuAppContext app)
    {
        var count = 0;

        if (app.TenantTokenManager.SupportsBackgroundRefresh)
        {
            _refreshService.RegisterTokenManager(
                app.TenantTokenManager,
                $"tenant:{app.Config.AppKey}");
            count++;
        }

        if (app.AppTokenManager.SupportsBackgroundRefresh)
        {
            _refreshService.RegisterTokenManager(
                app.AppTokenManager,
                $"app:{app.Config.AppKey}");
            count++;
        }

        // 注意：不注册 UserTokenManager
        // 用户令牌是按需获取的（通过 OAuth 授权码换取），不适合后台预热
        return count;
    }
}
#endif
