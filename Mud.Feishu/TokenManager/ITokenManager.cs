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
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户身份 (user_access_token)访问令牌。
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    //Task<string?> GetUserTokenAsync(CancellationToken cancellationToken = default);
}
