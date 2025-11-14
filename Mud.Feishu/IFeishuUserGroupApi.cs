using Mud.Feishu.DataModels.UserGroup;

namespace Mud.Feishu;

/// <summary>
/// 飞书用户组相关的API接口函数。
/// </summary>
[HttpClientApi]
public interface IFeishuUserGroupApi
{
    /// <summary>
    /// 创建用户组。用户组是飞书通讯录中基础实体之一，在用户组内可添加用户或部门资源。各类业务权限管控可以与用户组关联，从而实现高效便捷的成员权限管控。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="groupInfoRequest">创建用户组请求体。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/group")]
    Task<ApiResult<UserGroupCreateResult>> CreateUserGroupAsync(
        [Header("Authorization")] string user_access_token,
        [Body] UserGroupInfoRequest groupInfoRequest,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户组。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="groupUpdateRequest">更新用户组请求体。</param>
    /// <param name="group_id">用户组 ID。用户组 ID 可在创建用户组时从返回值中获取，你也可以调用查询用户组列表接口，获取用户组的 ID。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    [Patch("https://open.feishu.cn/open-apis/contact/v3/group/{group_id}")]
    Task<ApiResult<UserGroupCreateResult>> UpdateUserGroupAsync(
       [Header("Authorization")] string user_access_token,
       [Path] string group_id,
       [Body] UserGroupUpdateRequest groupUpdateRequest,
       [Query("user_id_type")] string? user_id_type = null,
       [Query("department_id_type")] string? department_id_type = null,
       CancellationToken cancellationToken = default);
}
