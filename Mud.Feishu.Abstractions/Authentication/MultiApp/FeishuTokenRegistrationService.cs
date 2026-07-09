// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET6_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书令牌注册托管服务
/// </summary>
/// <remarks>
/// 在应用启动时，将所有已注册飞书应用的令牌管理器注册到 <see cref="ITokenRefreshBackgroundService"/>，
/// 使后台刷新服务能够定时预热和刷新所有应用的令牌。
///
/// 此前 <see cref="ITokenRefreshBackgroundService"/> 虽然被注册并启用，但其内部令牌管理器字典始终为空，
/// 导致后台令牌刷新功能形同虚设（令牌仅依赖懒加载刷新，首次请求延迟高且无法享受"过期前主动刷新"预热）。
///
/// 本服务在 <see cref="StartAsync"/> 中遍历 <see cref="IFeishuAppManager"/> 中的所有应用，
/// 将每个应用的 TenantTokenManager 和 AppTokenManager 注册到后台刷新服务。
/// （UserTokenManager 不注册，因为用户令牌是按需获取的，不适合后台预热。）
///
/// 依赖 <see cref="IServiceProvider"/> 而非直接依赖 <see cref="ITokenRefreshBackgroundService"/>，
/// 以避免循环依赖：ITokenRefreshBackgroundService 在 NET6+ 中以 IHostedService 形式注册，
/// 直接注入会导致 IHostedService → FeishuTokenRegistrationService → ITokenRefreshBackgroundService → IHostedService 循环。
/// </remarks>
internal sealed class FeishuTokenRegistrationService : IHostedService
{
    private readonly IFeishuAppManager _appManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FeishuTokenRegistrationService> _logger;

    /// <summary>
    /// 初始化 <see cref="FeishuTokenRegistrationService"/> 实例
    /// </summary>
    /// <param name="appManager">飞书应用管理器</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException">当任何必需参数为 null 时抛出</exception>
    public FeishuTokenRegistrationService(
        IFeishuAppManager appManager,
        IServiceProvider serviceProvider,
        ILogger<FeishuTokenRegistrationService> logger)
    {
        _appManager = appManager ?? throw new ArgumentNullException(nameof(appManager));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        // 从 IHostedService 集合中查找 ITokenRefreshBackgroundService 实例
        // NET6+ 中 TokenRefreshHostedService 以 IHostedService 形式注册，不直接暴露为 ITokenRefreshBackgroundService
        var hostedServices = _serviceProvider.GetServices<IHostedService>();
        var refreshService = hostedServices.OfType<ITokenRefreshBackgroundService>().FirstOrDefault();

        if (refreshService == null)
        {
            _logger.LogWarning("未找到 ITokenRefreshBackgroundService 实例，跳过令牌管理器注册");
            return Task.CompletedTask;
        }

        var apps = _appManager.GetAllApps();
        var registered = 0;

        foreach (var app in apps)
        {
            // 注册租户令牌管理器
            refreshService.RegisterTokenManager(
                app.TenantTokenManager,
                $"tenant:{app.Config.AppKey}");
            registered++;

            // 注册应用令牌管理器
            refreshService.RegisterTokenManager(
                app.AppTokenManager,
                $"app:{app.Config.AppKey}");
            registered++;

            // 注意：不注册 UserTokenManager
            // 用户令牌是按需获取的（通过 OAuth 授权码换取），不适合后台预热
        }

        _logger.LogInformation(
            "已将 {AppCount} 个飞书应用的 {TokenCount} 个令牌管理器注册到后台刷新服务",
            apps.Count(),
            registered);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        // TokenRefreshBackgroundService 自身管理令牌管理器的生命周期
        // 此处无需额外清理
        return Task.CompletedTask;
    }
}
#endif
