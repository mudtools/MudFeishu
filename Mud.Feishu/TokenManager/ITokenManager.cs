// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 全局令牌管理接口。
/// </summary>
public interface ITokenManager
{
    /// <summary>
    /// 获取应用身份 (tenant_access_token)访问令牌。
    /// </summary>
    /// <returns></returns>
    Task<string?> GetTenantAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户身份 (user_access_token)访问令牌。
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<string?> GetUserAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期令牌（可选方法）
    /// </summary>
    void CleanExpiredTokens();

    /// <summary>
    /// 获取缓存统计信息（用于监控）
    /// </summary>
    (int Total, int Expired) GetCacheStatistics();
}
