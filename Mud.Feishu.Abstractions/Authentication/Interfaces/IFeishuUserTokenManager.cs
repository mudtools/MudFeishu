// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书用户令牌管理器接口，提供获取用户信息等相关方法。
/// </summary>
public interface IFeishuUserTokenManager : IUserTokenManager
{
    /// <summary>
    /// 将用户令牌信息存储到缓存中，与指定的用户标识关联。
    /// 通常在通过授权码获取令牌后调用，以便后续通过 userId 管理令牌。
    /// </summary>
    /// <param name="userId">用户的唯一标识符（如 OpenId）。</param>
    /// <param name="tokenInfo">用户令牌信息。</param>
    /// <param name="cancellationToken">用于取消异步操作的取消令牌。</param>
    /// <returns>异步任务。</returns>
    Task StoreUserTokenAsync(string userId, UserTokenInfo tokenInfo, CancellationToken cancellationToken = default);
}
