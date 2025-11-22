// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.RoleMembers;

namespace Mud.Feishu;

/// <summary>
/// 角色成员是指角色内添加的一个或多个用户。
/// </summary>
/// <remarks>
/// <para>可以将角色设置为审批流程的审批人，这样该角色内的成员均可处理审批。</para>
/// <para>同时，每一个角色成员都可以设置管理范围，以便指定不同成员管理不同的部门。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/functional_role-member/resource-introduction"/></para>
/// </remarks>
[HttpClientApi(RegistryGroupName = "Organization")]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuV3RoleMemberService))]
public interface IFeishuV3RoleMemberApi
{
    /// <summary>
    /// 在指定角色内添加一个或多个成员。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="role_id">角色 ID。</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id</param>
    /// <param name="roleMembersRequest"> 角色成员的用户 ID 列表请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/functional_roles/{role_id}/members/batch_create")]
    Task<FeishuApiResult<RoleAssignmentResult>> BatchAddMemberAsync(
                 [Token][Header("Authorization")] string tenant_access_token,
                 [Path] string role_id,
                 [Body] RoleMembersRequest roleMembersRequest,
                 [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
                 CancellationToken cancellationToken = default);

    /// <summary>
    /// 为指定角色内的一个或多个角色成员设置管理范围。管理范围是指角色成员可以管理的部门范围。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="role_id">角色 ID。</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型，默认值：open_department_id。</param>
    /// <param name="membersScopeRequest"> 角色成员管理范围请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/functional_roles/{role_id}/members/scopes")]
    Task<FeishuApiResult<RoleAssignmentResult>> BatchAddMembersSopesAsync(
               [Token][Header("Authorization")] string tenant_access_token,
               [Path] string role_id,
               [Body] RoleMembersScopeRequest membersScopeRequest,
               [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
               [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
               CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询指定角色内的指定成员的管理范围。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="role_id">角色 ID。</param>
    /// <param name="member_id">角色成员的用户 ID，ID 类型需要和查询参数 user_id_type 的取值保持一致。</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型，默认值：open_department_id。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/functional_roles/{role_id}/members/{member_id}")]
    Task<FeishuApiResult<RoleMemberScopeResult>> GetMembersSopesAsync(
              [Token][Header("Authorization")] string tenant_access_token,
              [Path] string role_id,
              [Path] string member_id,
              [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
              [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
              CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询指定角色内的所有成员信息，包括成员的用户 ID、管理范围。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="role_id">角色 ID。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型，默认值：open_department_id。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("https://open.feishu.cn/open-apis/contact/v3/functional_roles/{role_id}/members")]
    Task<FeishuApiResult<RoleMemberScopeResult>> GetMembersAsync(
              [Token][Header("Authorization")] string tenant_access_token,
              [Path] string role_id,
              [Query("page_size")] int page_size = 10,
              [Query("page_token")] string? page_token = null,
              [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
              [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
              CancellationToken cancellationToken = default);

    /// <summary>
    /// 在指定角色内删除一个或多个成员。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="role_id">角色 ID。</param>
    /// <param name="roleMembersRequest">需删除的角色成员的用户 ID 列表请求体。</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/contact/v3/functional_roles/{role_id}/members/batch_delete")]
    Task<FeishuApiResult<RoleAssignmentResult>> DeleteMembersByRoleIdAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Path] string role_id,
         [Body] RoleMembersRequest roleMembersRequest,
         [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);
}
