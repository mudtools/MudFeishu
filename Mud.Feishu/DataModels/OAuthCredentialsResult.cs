namespace Mud.Feishu.DataModels;

/// <summary>
/// 获取 user_access_token 的响应结果
/// </summary>
public class OAuthCredentialsResult : ApiResult
{
    /// <summary>
    /// user_access_token，仅在请求成功时返回
    /// </summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// user_access_token 的有效期，单位为秒，仅在请求成功时返回
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 用于刷新 user_access_token。该字段仅在请求成功且用户授予 offline_access 权限时返回。
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// refresh_token 的有效期，单位为秒，仅在返回 refresh_token 时返回。
    /// </summary>
    [JsonPropertyName("refresh_token_expires_in")]
    public int RefreshTokenExpiresIn { get; set; }

    /// <summary>
    /// 本次请求所获得的 access_token 所具备的权限列表，以空格分隔，仅在请求成功时返回
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// 值固定为 Bearer，仅在请求成功时返回
    /// </summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    /// <summary>
    /// 错误类型，仅在请求失败时返回
    /// </summary>
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// 具体的错误信息，仅在请求失败时返回
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
