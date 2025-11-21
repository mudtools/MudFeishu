// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? GrantType
    { get; set; }

    /// <summary>
    /// 应用的 App ID。
    /// <para>示例值：cli_a5ca35a685b0x26e</para>
    /// </summary>
    [JsonPropertyName("client_id")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? ClientId
    { get; set; }

    /// <summary>
    /// 应用的 App Secret。
    /// <para>示例值：baBqE5um9LbFGDy3X7LcfxQX1sqpXlwy</para>
    /// </summary>
    [JsonPropertyName("client_secret")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? ClientSecret
    { get; set; }
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
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? Code
    { get; set; }

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
