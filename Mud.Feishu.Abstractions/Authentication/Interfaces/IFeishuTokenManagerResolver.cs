// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书令牌管理器解析器接口
/// </summary>
/// <remarks>
/// 多应用模式下按应用键解析令牌管理器的推荐入口，替代直接注入 <see cref="ITenantTokenManager"/> /
/// <see cref="IAppTokenManager"/> / <see cref="IUserTokenManager"/>——后者只暴露默认应用的实例，
/// 无法切换到非默认应用。所有 <c>Get*</c> 成员在指定应用不存在时抛 <see cref="InvalidOperationException"/>，
/// <c>TryGet*</c> 成员返回 <c>null</c>；各成员的 <c>appKey</c> 为 <c>null</c> 时取默认应用。
/// </remarks>
public interface IFeishuTokenManagerResolver
{
    /// <summary>
    /// 获取租户令牌管理器
    /// </summary>
    /// <param name="appKey">应用键，为 null 时使用默认应用</param>
    /// <returns>指定应用的租户令牌管理器</returns>
    /// <exception cref="InvalidOperationException">当指定应用不存在时抛出</exception>
    ITenantTokenManager GetTenantTokenManager(string? appKey = null);

    /// <summary>
    /// 获取应用令牌管理器
    /// </summary>
    /// <param name="appKey">应用键，为 null 时使用默认应用</param>
    /// <returns>指定应用的应用令牌管理器</returns>
    /// <exception cref="InvalidOperationException">当指定应用不存在时抛出</exception>
    IAppTokenManager GetAppTokenManager(string? appKey = null);

    /// <summary>
    /// 获取用户令牌管理器
    /// </summary>
    /// <param name="appKey">应用键，为 null 时使用默认应用</param>
    /// <returns>指定应用的用户令牌管理器</returns>
    /// <exception cref="InvalidOperationException">当指定应用不存在时抛出</exception>
    IFeishuUserTokenManager GetUserTokenManager(string? appKey = null);

    /// <summary>
    /// 尝试获取租户令牌管理器
    /// </summary>
    /// <param name="appKey">应用键，为 null 时使用默认应用</param>
    /// <returns>指定应用的租户令牌管理器；如果应用不存在则返回 null</returns>
    ITenantTokenManager? TryGetTenantTokenManager(string? appKey = null);

    /// <summary>
    /// 尝试获取应用令牌管理器
    /// </summary>
    /// <param name="appKey">应用键，为 null 时使用默认应用</param>
    /// <returns>指定应用的应用令牌管理器；如果应用不存在则返回 null</returns>
    IAppTokenManager? TryGetAppTokenManager(string? appKey = null);

    /// <summary>
    /// 尝试获取用户令牌管理器
    /// </summary>
    /// <param name="appKey">应用键，为 null 时使用默认应用</param>
    /// <returns>指定应用的用户令牌管理器；如果应用不存在则返回 null</returns>
    IFeishuUserTokenManager? TryGetUserTokenManager(string? appKey = null);
}