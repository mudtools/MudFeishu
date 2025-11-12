namespace Mud.Feishu.DataModels;

/// <summary>
/// 自建应用租户认证响应结果
/// </summary>
public class TenantAppCredentialResult : ApiResult
{
    /// <summary>
    /// 租户访问凭证
    /// </summary>
    [JsonPropertyName("tenant_access_token")]
    public string? TenantAccessToken { get; set; }

    /// <summary>
    /// tenant_access_token 的过期时间，单位为秒
    /// </summary>
    [JsonPropertyName("expire")]
    public int Expire { get; set; } = 0;
}
