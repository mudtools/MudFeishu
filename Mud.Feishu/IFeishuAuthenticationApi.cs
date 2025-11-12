using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu;

/// <summary>
/// 飞书认证授权相关的API
/// </summary>
[HttpClientApi]
public interface IFeishuAuthenticationApi
{
    /// <summary>
    /// 通过 user_access_token 获取相关用户信息。
    /// </summary>
    /// <param name="user_access_token">user_access_token 以登录用户身份调用 API，可读写的数据范围由用户可读写的数据范围决定。参考获取 user_access_token。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/authen/v1/user_info")]
    Task<ApiResult<UserData>> GetUserInfoAsync([Header("Authorization")] string user_access_token);

    /// <summary>
    /// 该接口用于退出用户的登录态
    /// </summary>
    /// <param name="user_access_token">user_access_token 以登录用户身份调用 API</param>
    /// <param name="user_id_type">用户 ID 类型，非必填项。</param>
    /// <param name="logoutRequest">退出登录请求体。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/passport/v1/sessions/logout")]
    Task<ApiResult<object>> LogoutAsync([Header("Authorization")] string user_access_token, [Query("user_id_type")] string user_id_type, [Body] LogoutRequest logoutRequest);

    /// <summary>
    /// 获取自建应用获取 tenant_access_token。
    /// </summary>
    /// <param name="credentials">应用唯一标识及应用秘钥信息</param>
    /// <remarks>
    /// <para>tenant_access_token 的最大有效期是 2 小时。</para>
    /// <para>剩余有效期小于 30 分钟时，调用本接口会返回一个新的 tenant_access_token，这会同时存在两个有效的 tenant_access_token。</para>
    /// <para>剩余有效期大于等于 30 分钟时，调用本接口会返回原有的 tenant_access_token。</para>
    /// </remarks>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal")]
    Task<TenantAppCredentialResult> GetTenantAccessToken([Body] AppCredentials credentials);

    /// <summary>
    /// 自建应用获取 app_access_token
    /// </summary>
    /// <param name="credentials">应用唯一标识及应用秘钥信息</param>
    /// <remarks>
    /// <para>app_access_token 的最大有效期是 2 小时。</para>
    /// <para>剩余有效期小于 30 分钟时，调用本接口会返回一个新的 app_access_token，这会同时存在两个有效的 app_access_token。</para>
    /// <para>剩余有效期大于等于 30 分钟时，调用本接口会返回原有的 app_access_token。</para>
    /// </remarks>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal")]
    Task<AppCredentialResult> GetAppAccessToken([Body] AppCredentials credentials);
}
