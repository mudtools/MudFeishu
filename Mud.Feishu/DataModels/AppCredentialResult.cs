namespace Mud.Feishu.DataModels;

/// <summary>
/// 自建应用认证响应结果
/// </summary>
public class AppCredentialResult : TenantAppCredentialResult
{
    /// <summary>
    /// 应用访问凭证
    /// </summary>
    [JsonPropertyName("app_access_token")]
    public string? AppAccessToken { get; set; }
}
