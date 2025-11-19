// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.UserGroupMember;

namespace Mud.Feishu;

/// <summary>
/// 用户组内可以添加部门或用户，部门和用户均属于用户组成员。使用用户组成员 API，可以在用户组内添加、移除、查询成员。
/// </summary>
[HttpClientApi]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuV3UserGroupMember))]
public interface IFeishuV3UserGroupMemberApi
{
    /// <summary>
    /// 向指定的普通用户组内添加成员。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="group_id">用户组 ID。</param>
    /// <param name="groupMemberRequest">添加用户组成员请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/group/{group_id}/member/add")]
    Task<FeishuNullDataApiResult> AddMemberAsync(
        [Token][Header("Authorization")] string tenant_access_token,
        [Path] string group_id,
        [Body] UserGroupMemberRequest groupMemberRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 向指定的普通用户组内添加一个或多个成员。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="group_id">用户组 ID。</param>
    /// <param name="groupMemberRequest">批量添加用户组成员请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("https://open.feishu.cn/open-apis/contact/v3/group/{group_id}/member/batch_add")]
    Task<FeishuApiResult<BatchAddMemberResult>> BatchAddMemberAsync(
           [Token][Header("Authorization")] string tenant_access_token,
           [Path] string group_id,
           [Body] BatchMembersRequest groupMemberRequest,
           CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询指定用户组内的成员列表，列表内主要包括成员 ID 信息。
    /// </summary>
    /// <param name="group_id">用户组 ID。</param>
    /// <param name="member_id_type">用户组成员 ID 类型。默认值：open_id</param>
    /// <param name="member_type">用户组成员类型。默认值：user</param>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/group/{group_id}/member/simplelist")]
    Task<FeishuApiResult<MemberListRequest>> GetMemberListByGroupIdAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Path] string group_id,
         [Query("page_size")] int page_size = 10,
         [Query("page_token")] string? page_token = null,
         [Query("member_id_type")] string? member_id_type = Consts.User_Id_Type,
         [Query("member_type")] string? member_type = "user",
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除指定普通用户组内的某一成员。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="group_id">用户组 ID。</param>
    /// <param name="groupMemberRequest">移除用户组成员请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/group/{group_id}/member/remove")]
    Task<FeishuNullDataApiResult> RemoveMemberAsync(
           [Token][Header("Authorization")] string tenant_access_token,
           [Path] string group_id,
           [Body] UserGroupMemberRequest groupMemberRequest,
           CancellationToken cancellationToken = default);


    /// <summary>
    /// 从指定普通用户组内移除一个或多个成员。
    /// </summary>
    /// <param name="tenant_access_token">以应用身份调用 API，可读写的数据范围由应用自身的 数据权限范围 决定。</param>
    /// <param name="group_id">用户组 ID。</param>
    /// <param name="groupMemberRequest">批量移除用户组成员请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("https://open.feishu.cn/open-apis/contact/v3/group/{group_id}/member/batch_remove")]
    Task<FeishuNullDataApiResult> BatchRemoveMemberAsync(
           [Token][Header("Authorization")] string tenant_access_token,
           [Path] string group_id,
           [Body] BatchMembersRequest groupMemberRequest,
           CancellationToken cancellationToken = default);
}
