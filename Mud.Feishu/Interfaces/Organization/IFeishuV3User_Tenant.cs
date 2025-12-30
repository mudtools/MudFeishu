// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu;

/// <summary>
/// 飞书用户是飞书通讯录中的基础资源，对应企业组织架构中的成员实体。
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/user/field-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Organization", InheritedFrom = nameof(FeishuV3User))]
[Header(Consts.Authorization)]
public interface IFeishuTenantV3User : IFeishuV3User
{
    /// <summary>
    /// 向通讯录创建一个用户（该动作可以理解为员工入职）。成功创建用户后，系统会以短信或邮件的形式向用户发送邀请，用户在同意邀请后方可访问企业或团队。
    /// </summary>
    /// <param name="userModel">创建的用户请求体。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="client_token">用于幂等判断是否为同一请求，避免重复创建。请参考参数示例值，传入自定义的 client_token。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/contact/v3/users")]
    Task<FeishuApiResult<CreateOrUpdateUserResult>?> CreateUserAsync(
        [Body] CreateUserRequest userModel,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
        [Query("client_token")] string? client_token = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户 ID
    /// </summary>
    /// <param name="user_id">用户 ID，ID 类型与查询参数 user_id_type 的取值保持一致。</param>
    /// <param name="updateUserId">自定义新的用户 user_id。长度不能超过 64 字符。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/contact/v3/users/{user_id}/update_user_id")]
    Task<FeishuNullDataApiResult?> UpdateUserIdAsync(
        [Path] string user_id,
        [Body] UpdateUserIdRequest updateUserId,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过手机号或邮箱获取一个或多个用户的 ID （包括 user_id、open_id、union_id）与状态信息。
    /// </summary>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="queryRequest">查询参数请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>

    [Post("/open-apis/contact/v3/users/batch_get_id")]
    Task<FeishuApiResult<UserQueryListResult>?> GetBatchUsersAsync(
      [Body] UserQueryRequest queryRequest,
      [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过用户名关键词搜索其他用户的信息，包括用户头像、用户名、用户所在部门、用户 user_id 以及 open_id。
    /// </summary>
    /// <param name="query">搜索关键词，接口通过传入的关键词搜索相匹配的用户名。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/search/v1/user")]
    Task<FeishuApiResult<UserSearchListResult>?> GetUsersByKeywordAsync(
     [Query("query")] string query,
     [Query("page_size")] int page_size = 10,
     [Query("page_token")] string? page_token = null,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// 从通讯录内删除一个指定用户（该动作可以理解为员工离职），删除时可通过请求参数将用户所有的群组、文档、日程和应用等数据转让至他人。
    /// </summary>
    /// <param name="user_id">用户 ID。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="deleteSettingsRequest">用户删除参数请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/contact/v3/users/{user_id}")]
    Task<FeishuNullDataApiResult?> DeleteUserByIdAsync(
       [Path] string user_id,
       [Body] DeleteSettingsRequest deleteSettingsRequest,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于恢复已删除用户（已离职的成员）。
    /// </summary>
    /// <param name="user_id">用户 ID。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="resurrectUserRequest">恢复已删除用户操作的请求体。</param>
    /// <param name="department_id_type">部门 ID，ID 类型与 department_id_type 的取值保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/contact/v3/users/{user_id}/resurrect")]
    Task<FeishuNullDataApiResult?> ResurrectUserByIdAsync(
      [Path] string user_id,
      [Body] ResurrectUserRequest resurrectUserRequest,
      [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
      [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 该接口用于退出用户的登录态
    /// </summary>
    /// <param name="user_id_type">用户 ID 类型，非必填项。</param>
    /// <param name="logoutRequest">退出登录请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/passport/v1/sessions/logout")]
    Task<FeishuNullDataApiResult?> LogoutAsync(
        [Body] LogoutRequest logoutRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于返回调用 JSAPI 临时调用凭证，使用该凭证调用 JSAPI 时，请求不会被拦截。
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/jssdk/ticket/get")]
    Task<FeishuApiResult<TicketData>?> GetJsTicketAsync(CancellationToken cancellationToken = default);

}
