// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// TMA2-07 / C-5②：令牌注册逻辑抽取为 internal static helper，
/// 使 net8/10 单测可直接覆盖注册逻辑而无需 HostedService 载体。
/// </summary>
/// <remarks>
/// ns2.0 路径无 HostedService 载体，按 C-5② 决策仅文档声明限制。
/// 此 helper 在 net6+ 路径被 <c>FeishuTokenRegistrationService</c> 调用，
/// 也可被测试直接调用。
/// </remarks>
internal static class FeishuTokenRegistrationHelper
{
    /// <summary>
    /// 将应用的租户/应用令牌管理器注册到后台刷新服务（同名键覆盖）。
    /// </summary>
    /// <param name="app">应用上下文</param>
    /// <param name="refreshService">后台令牌刷新服务</param>
    /// <returns>注册的令牌管理器数量</returns>
    public static int RegisterAppTokenManagers(IFeishuAppContext app, ITokenRefreshBackgroundService refreshService)
    {
        var count = 0;

        if (app.TenantTokenManager.SupportsBackgroundRefresh)
        {
            refreshService.RegisterTokenManager(
                app.TenantTokenManager,
                $"tenant:{app.Config.AppKey}");
            count++;
        }

        if (app.AppTokenManager.SupportsBackgroundRefresh)
        {
            refreshService.RegisterTokenManager(
                app.AppTokenManager,
                $"app:{app.Config.AppKey}");
            count++;
        }

        // 注意：不注册 UserTokenManager
        // 用户令牌是按需获取的（通过 OAuth 授权码换取），不适合后台预热
        return count;
    }
}
