using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu;

/// <summary>
/// 飞书认证授权相关的API
/// </summary>
[HttpClientApi]
public interface IFeishuAuthenticationApi
{
    /// <summary>
    /// 获取自建应用获取 tenant_access_token。
    /// </summary>
    /// <param name="credentials">应用唯一标识及应用秘钥信息</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <remarks>
    /// <para>tenant_access_token 的最大有效期是 2 小时。</para>
    /// <para>剩余有效期小于 30 分钟时，调用本接口会返回一个新的 tenant_access_token，这会同时存在两个有效的 tenant_access_token。</para>
    /// <para>剩余有效期大于等于 30 分钟时，调用本接口会返回原有的 tenant_access_token。</para>
    /// </remarks>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal")]
    Task<TenantAppCredentialResult> GetTenantAccessTokenAsync(
        [Body] AppCredentials credentials,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 自建应用获取 app_access_token
    /// </summary>
    /// <param name="credentials">应用唯一标识及应用秘钥信息</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <remarks>
    /// <para>app_access_token 的最大有效期是 2 小时。</para>
    /// <para>剩余有效期小于 30 分钟时，调用本接口会返回一个新的 app_access_token，这会同时存在两个有效的 app_access_token。</para>
    /// <para>剩余有效期大于等于 30 分钟时，调用本接口会返回原有的 app_access_token。</para>
    /// </remarks>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal")]
    Task<AppCredentialResult> GetAppAccessTokenAsync(
        [Body] AppCredentials credentials,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 user_access_token,OAuth 令牌接口，可用于获取 user_access_token 以及 refresh_token。user_access_token 为用户访问凭证，使用该凭证可以以用户身份调用 OpenAPI。refresh_token 为刷新凭证，可以用来获取新的 user_access_token。
    /// </summary>
    /// <param name="credentials">获取 user_access_token 的请求参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/authen/v2/oauth/token")]
    Task<OAuthCredentialsResult> GetOAuthenAccessTokenAsync(
        [Body] OAuthTokenRequest credentials,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新 user_access_token
    /// </summary>
    /// <param name="credentials">OAuth 令牌接口，可用于刷新 user_access_token 以及获取新的 refresh_token。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/authen/v2/oauth/token")]
    Task<OAuthCredentialsResult> GetOAuthenRefreshAccessTokenAsync(
        [Body] OAuthRefreshTokenRequest credentials,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 本接口用于发起用户授权，应用在用户同意授权后将获得授权码 code。请注意授权码的有效期为 5 分钟，且只能被使用一次。
    /// </summary>
    /// <param name="client_id">应用的 App ID，可以在开发者后台的凭证与基础信息页面查看 App ID。</param>
    /// <param name="response_type">应用通知授权服务器所需的授权类型，对于授权码流程，固定值code</param>
    /// <param name="redirect_uri">应用重定向地址，在用户授权成功后会跳转至该地址，同时会携带 code 以及 state 参数（如有传递 state 参数）。</param>
    /// <param name="scope">用户需要增量授予应用的权限。</param>
    /// <param name="state">用来维护请求和回调之间状态的附加字符串，在授权完成回调时会原样回传此参数。应用可以根据此字符串来判断上下文关系，同时该参数也可以用以防止 CSRF 攻击，请务必校验 state 参数前后是否一致。</param>
    /// <param name="code_challenge">用于通过 PKCE（Proof Key for Code Exchange）流程增强授权码的安全性。</param>
    /// <param name="code_challenge_method">生成 code_challenge 的方法。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Get("https://accounts.feishu.cn/open-apis/authen/v1/authorize")]
    Task<AuthorizeResult> GetAuthorizeAsync(
       [Query] string client_id,
       [Query] string response_type,
       [Query] string redirect_uri,
       [Query] string? scope = null,
       [Query] string? state = null,
       [Query] string? code_challenge = null,
       [Query] string? code_challenge_method = null,
       CancellationToken cancellationToken = default);
}
