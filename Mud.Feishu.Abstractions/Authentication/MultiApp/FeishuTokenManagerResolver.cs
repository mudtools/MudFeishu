// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书令牌管理器解析器实现
/// </summary>
/// <remarks>
/// 通过 <see cref="IFeishuAppManager"/> 解析指定应用的令牌管理器。
/// 当 <paramref name="appKey"/> 为 null 时，返回默认应用的令牌管理器。
/// 这是多应用模式下获取令牌管理器的推荐方式。
/// </remarks>
internal sealed class FeishuTokenManagerResolver : IFeishuTokenManagerResolver
{
    private readonly IFeishuAppManager _appManager;

    /// <summary>
    /// 初始化 <see cref="FeishuTokenManagerResolver"/> 实例
    /// </summary>
    /// <param name="appManager">飞书应用管理器</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="appManager"/> 为 null 时抛出</exception>
    public FeishuTokenManagerResolver(IFeishuAppManager appManager)
    {
        _appManager = appManager ?? throw new ArgumentNullException(nameof(appManager));
    }

    /// <inheritdoc />
    public ITenantTokenManager GetTenantTokenManager(string? appKey = null)
    {
        return string.IsNullOrEmpty(appKey)
            ? _appManager.DefaultTenantTokenManager
            : _appManager.GetApp(appKey!).TenantTokenManager;
    }

    /// <inheritdoc />
    public IAppTokenManager GetAppTokenManager(string? appKey = null)
    {
        return string.IsNullOrEmpty(appKey)
            ? _appManager.DefaultAppTokenManager
            : _appManager.GetApp(appKey!).AppTokenManager;
    }

    /// <inheritdoc />
    public IFeishuUserTokenManager GetUserTokenManager(string? appKey = null)
    {
        return string.IsNullOrEmpty(appKey)
            ? _appManager.DefaultUserTokenManager
            : _appManager.GetApp(appKey!).UserTokenManager;
    }
}
