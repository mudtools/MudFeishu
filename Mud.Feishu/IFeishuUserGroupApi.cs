namespace Mud.Feishu;

/// <summary>
/// 飞书用户组相关的API接口函数。
/// </summary>
[HttpClientApi]
[HttpClientApiWrap(TokenManage = nameof(ITokenManage), WrapInterface = "IFeishuUserGroup")]
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
    Task<FeishuApiResult<UserGroupCreateResult>> CreateUserGroupAsync(
        [Token][Header("Authorization")] string user_access_token,
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
    Task<FeishuNullDataApiResult> UpdateUserGroupAsync(
       [Token][Header("Authorization")] string user_access_token,
       [Path] string group_id,
       [Body] UserGroupUpdateRequest groupUpdateRequest,
       [Query("user_id_type")] string? user_id_type = null,
       [Query("department_id_type")] string? department_id_type = null,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过用户组 ID 查询指定用户组的基本信息，包括用户组名称、成员数量和类型等。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="group_id">用户组 ID。</param>
    /// <param name="user_id_type">此次调用中的用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/group/{group_id}")]
    Task<FeishuApiResult<UserGroupQueryResult>> GetUserGroupByIdAsync(
      [Token][Header("Authorization")] string user_access_token,
      [Path] string group_id,
      [Query("user_id_type")] string? user_id_type = null,
      [Query("department_id_type")] string? department_id_type = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询当前租户下的用户组列表，列表内包含用户组的 ID、名字、成员数量和类型等信息。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="type">用户组类型。可选值有：1：普通用户组 2：动态用户组 默认值：1</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/group/simplelist")]
    Task<FeishuApiResult<UserGroupListDataResult>> GetUserGroupsAsync(
     [Token][Header("Authorization")] string user_access_token,
     [Query("page_size")] int page_size = 10,
     [Query("page_token")] string? page_token = null,
     [Query("type")] int type = 1,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询指定用户所属的用户组列表。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="member_id">成员 ID。ID 类型与 member_id_type 取值保持一致。</param>
    /// <param name="member_id_type">成员 ID 类型。</param>
    /// <param name="group_type">用户组类型。可选值有：1：普通用户组 2：动态用户组</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="type"></param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/group/member_belong")]
    Task<FeishuApiResult<UserBelongGroupListResult>> GetUserBelongGroupsAsync(
    [Token][Header("Authorization")] string user_access_token,
    [Query("member_id")] string member_id,
    [Query("member_id_type")] string? member_id_type = null,
    [Query("group_type")] int? group_type = null,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("type")] int type = 1,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定用户组。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="group_id">需删除的用户组 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/contact/v3/group/{group_id}")]
    Task<FeishuNullDataApiResult> DeleteUserGroupByIdAsync(
      [Token][Header("Authorization")] string user_access_token,
      [Path] string group_id,
      CancellationToken cancellationToken = default);
}
