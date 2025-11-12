namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// user_access_token 的基本请求参数
/// </summary>
public class OAuthTokenBaseRequest
{
    /// <summary>
    /// 授权类型。固定值：authorization_code
    /// </summary>
    [JsonPropertyName("grant_type")]
    public required string GrantType { get; set; }

    /// <summary>
    /// 应用的 App ID。
    /// <para>示例值：cli_a5ca35a685b0x26e</para>
    /// </summary>
    [JsonPropertyName("client_id")]
    public required string ClientId { get; set; }

    /// <summary>
    /// 应用的 App Secret。
    /// <para>示例值：baBqE5um9LbFGDy3X7LcfxQX1sqpXlwy</para>
    /// </summary>
    [JsonPropertyName("client_secret")]
    public required string ClientSecret { get; set; }
}

/// <summary>
/// 获取 user_access_token 的请求参数
/// </summary>
public class OAuthTokenRequest : OAuthTokenBaseRequest
{

    /// <summary>
    /// 授权码
    /// <para>示例值：a61hb967bd094dge949h79bbexd16dfe</para>
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    /// <summary>
    /// 在构造授权页页面链接时所拼接的应用回调地址。
    /// </summary>
    [JsonPropertyName("redirect_uri")]
    public string? RedirectUri { get; set; }

    /// <summary>
    /// 该参数用于缩减 user_access_token 的权限范围。
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// 在发起授权前，本地生成的随机字符串，用于 PKCE（Proof Key for Code Exchange）流程。使用 PKCE 时，该值为必填项。
    /// <para>示例值：TxYmzM4PHLBlqm5NtnCmwxMH8mFlRWl_ipie3O0aVzo</para>
    /// </summary>
    [JsonPropertyName("code_verifier")]
    public string? CodeVerifier { get; set; }
}
