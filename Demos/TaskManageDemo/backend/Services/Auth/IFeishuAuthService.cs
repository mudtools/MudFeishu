// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using TaskManageDemo.Backend.Models.DTOs;

namespace TaskManageDemo.Backend.Services.Auth;

/// <summary>
/// 飞书 OAuth 认证服务接口
/// </summary>
public interface IFeishuAuthService
{
    /// <summary>
    /// 获取飞书 OAuth 授权链接
    /// </summary>
    OAuthUrlResponse GetOAuthUrl(string? state = null, string? redirectUri = null);

    /// <summary>
    /// 使用授权码登录
    /// </summary>
    Task<LoginResponse?> LoginWithCodeAsync(string code, string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新飞书用户令牌
    /// </summary>
    Task<TokenRefreshResponse?> RefreshTokenAsync(string openId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取飞书用户详细信息
    /// </summary>
    Task<FeishuUserDetail?> GetUserDetailAsync(string openId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据授权码获取飞书用户信息（用于注册流程）
    /// </summary>
    Task<FeishuUserInfoForRegistration?> GetUserInfoByCodeAsync(string code, CancellationToken cancellationToken = default);
}
