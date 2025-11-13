using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu;

/// <summary>
/// 企业人员信息管理相关的API
/// </summary>
[HttpClientApi]
public interface IFeishuUserApi
{
    /// <summary>
    /// 调用该接口向通讯录创建一个用户（该动作可以理解为员工入职）。成功创建用户后，系统会以短信或邮件的形式向用户发送邀请，用户在同意邀请后方可访问企业或团队。
    /// </summary>
    /// <param name="user_access_token">tenant_access_token</param>
    /// <param name="userModel">创建的用户请求体。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="client_token">用于幂等判断是否为同一请求，避免重复创建。请参考参数示例值，传入自定义的 client_token。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/users")]
    Task<ApiResult<CreateOrUpdateUserResult>> CreateUserAsync(
        [Header("Authorization")] string user_access_token,
        [Body] CreateUserRequest userModel,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("client_token")] string? client_token = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 调用该接口更新通讯录中指定用户的信息，包括名称、邮箱、手机号、所属部门以及自定义字段等信息。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="user_id">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="userModel">用于更新的用户请求体。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Patch("https://open.feishu.cn/open-apis/contact/v3/users/{user_id}")]
    Task<ApiResult<CreateOrUpdateUserResult>> UpdateUserAsync(
        [Header("Authorization")] string user_access_token,
        [Path] string user_id,
        [Body] UpdateUserRequest userModel,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户 ID
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="user_id">用户 ID，ID 类型与查询参数 user_id_type 的取值保持一致。</param>
    /// <param name="updateUserId">自定义新的用户 user_id。长度不能超过 64 字符。</param>
    /// /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Patch("https://open.feishu.cn/open-apis/contact/v3/users/{user_id}/update_user_id")]
    Task<ApiResult<object>> UpdateUserIdAsync([Header("Authorization")] string user_access_token, [Path] string user_id, [Body] UpdateUserIdRequest updateUserId, [Query("user_id_type")] string? user_id_type = null, CancellationToken cancellationToken = default);
}
