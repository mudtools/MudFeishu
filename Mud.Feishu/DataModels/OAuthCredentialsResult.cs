// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels;

/// <summary>
/// 获取 user_access_token 的响应结果
/// </summary>
public class OAuthCredentialsResult : FeishuApiResult
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
