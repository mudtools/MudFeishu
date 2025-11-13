namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 刷新令牌请求参数
/// </summary>
public class OAuthRefreshTokenRequest : OAuthTokenBaseRequest
{
    /// <summary>
    /// 刷新令牌，用于刷新 user_access_token 以及 refresh_token。
    ///</summary>
    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; set; }
}
