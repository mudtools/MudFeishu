// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>飞书多维表格高级权限允许用户针对单一数据表设置哪些用户可以查看、编辑指定的行，或是设置针对某用户可以编辑的列。。</para>
/// <para>高级权限接口分为 自定义角色 和 协作者 两部分，多维表格的 所有者 或者 有可管理权限 的用户可通过接口设置高级权限，管理高级权限的协作者。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/advanced-permission/advanced-permission-guide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV2BitableRole : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 新增自定义角色
    /// <para>新增多维表格高级权限中自定义的角色。</para>
    /// <para><see href="https://open.feishu.cn/document/docs/bitable-v1/advanced-permission/app-role/create-2">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>   
    /// <param name="createRoleRequest">新增自定义角色请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/base/v2/apps/{app_token}/roles")]
    Task<FeishuApiResult<RoleOpsResult>?> CreateRoleAsync(
        [Path] string app_token,
        [Body] CreateRoleRequest createRoleRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新自定义角色
    /// <para>更新多维表格高级权限中自定义的角色。</para>
    /// <para><see href="https://open.feishu.cn/document/docs/bitable-v1/advanced-permission/app-role/update-2">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>   
    /// <param name="role_id">
    /// <para>多维表格高级权限中自定义角色的唯一标识，以 rol 开头。获取方式：通过[列出自定义角色](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-role/list)接口获取。</para>
    /// <para>示例值：roljRpwIUt</para>
    /// </param>
    /// <param name="updateRoleRequest">更新自定义角色请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/base/v2/apps/{app_token}/roles/{role_id}")]
    Task<FeishuApiResult<RoleOpsResult>?> UpdateRoleAsync(
       [Path] string app_token,
       [Path] string role_id,
       [Body] UpdateRoleRequest updateRoleRequest,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 列出自定义角色
    /// <para>列出多维表格高级权限中用户自定义的角色。</para>
    /// <para><see href="https://open.feishu.cn/document/docs/bitable-v1/advanced-permission/app-role/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>   
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/base/v2/apps/{app_token}/roles")]
    Task<FeishuApiPageListTotalResult<AppRoleInfo>?> GetRolesPageListAsync(
      [Path] string app_token,
      [Query("page_size")] int page_size = 20,
      [Query("page_token")] string? page_token = null,
      CancellationToken cancellationToken = default);



    /// <summary>
    /// 删除自定义角色
    /// <para>删除多维表格高级权限中自定义的角色。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/advanced-permission/app-role/delete">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>   
    /// <param name="role_id">
    /// <para>多维表格高级权限中自定义角色的唯一标识，以 rol 开头。获取方式：通过[列出自定义角色](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-role/list)接口获取。</para>
    /// <para>示例值：roljRpwIUt</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}")]
    Task<FeishuNullDataApiResult?> DeleteRoleAsync(
        [Path] string app_token,
        [Path] string role_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 新增协作者
    /// <para>新增多维表格高级权限中自定义角色的协作者。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/advanced-permission/app-role/delete">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>   
    /// <param name="role_id">
    /// <para>多维表格高级权限中自定义角色的唯一标识，以 rol 开头。获取方式：通过[列出自定义角色](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-role/list)接口获取。</para>
    /// <para>示例值：roljRpwIUt</para>
    /// </param>
    /// <param name="addRoleMemberRequest">新增协作者请求体</param>
    /// <param name="member_id_type">
    /// <para>必填：否</para>
    /// <para>协作者 ID 的类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：以 open_id 来识别协作者。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>union_id：以 union_id 来识别协作者。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>user_id：以 user_id 来识别协作者。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>chat_id：以 chat_id 来识别协作者。获取方式参考[群 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat-id-description)</item>
    /// <item>department_id：以 department_id 来识别协作者。调用前，请确保应用有部门的可见性，参考[配置应用可用范围](https://open.feishu.cn/document/home/introduction-to-scope-and-authorization/availability)。获取 department_id 方式参考[部门资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/department/field-overview)</item>
    /// <item>open_department_id：以 open_department_id 来识别协作者。调用前，请确保应用有部门的可见性，参考[配置应用可用范围](https://open.feishu.cn/document/home/introduction-to-scope-and-authorization/availability)。获取 open_department_id 方式参考[部门资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/department/field-overview)</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param> 
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}/members")]
    Task<FeishuNullDataApiResult?> AddRoleMemberAsync(
      [Path] string app_token,
      [Path] string role_id,
      [Body] AddRoleMemberRequest addRoleMemberRequest,
      [Query("member_id_type")] string member_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量新增协作者
    /// <para>批量新增多维表格高级权限中自定义角色的协作者。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/advanced-permission/app-role-member/batch_create">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>   
    /// <param name="role_id">
    /// <para>多维表格高级权限中自定义角色的唯一标识，以 rol 开头。获取方式：通过[列出自定义角色](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-role/list)接口获取。</para>
    /// <para>示例值：roljRpwIUt</para>
    /// </param>
    /// <param name="addRoleMemberRequest">新增协作者请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}/members/batch_create")]
    Task<FeishuNullDataApiResult?> AddRoleMembersAsync(
        [Path] string app_token,
        [Path] string role_id,
        [Body] AddRoleMembersRequest addRoleMemberRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 分页列出协作者
    /// <para>分页列出多维表格高级权限中自定义角色的协作者。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/advanced-permission/app-role-member/list">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>   
    /// <param name="role_id">
    /// <para>多维表格高级权限中自定义角色的唯一标识，以 rol 开头。获取方式：通过[列出自定义角色](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-role/list)接口获取。</para>
    /// <para>示例值：roljRpwIUt</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}/members")]
    Task<FeishuApiPageListResult<AppRoleMember>?> GetRoleMembersPageListAsync(
       [Path] string app_token,
       [Path] string role_id,
       [Query("page_size")] int page_size = 20,
       [Query("page_token")] string? page_token = null,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除协作者
    /// <para>删除多维表格高级权限中自定义角色的协作者。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/advanced-permission/app-role-member/delete">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>   
    /// <param name="role_id">
    /// <para>多维表格高级权限中自定义角色的唯一标识，以 rol 开头。获取方式：通过[列出自定义角色](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-role/list)接口获取。</para>
    /// <para>示例值：roljRpwIUt</para>
    /// </param>
    /// <param name="member_id">
    /// <para>高级权限中自定义角色协作者的 ID，需与查询参数中 member_id_type 的类型需一致。获取 ID 方式参考 member_id_type 参数描述。</para>
    /// <para>示例值：ou_7dab8a3d3cdcc9da365777c7ad53uew2</para>
    /// </param>
    /// <param name="member_id_type">
    /// <para>必填：否</para>
    /// <para>协作者 ID 的类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：以 open_id 来识别协作者。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>union_id：以 union_id 来识别协作者。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>user_id：以 user_id 来识别协作者。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>chat_id：以 chat_id 来识别协作者。获取方式参考[群 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat-id-description)</item>
    /// <item>department_id：以 department_id 来识别协作者。调用前，请确保应用有部门的可见性，参考[配置应用可用范围](https://open.feishu.cn/document/home/introduction-to-scope-and-authorization/availability)。获取 department_id 方式参考[部门资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/department/field-overview)</item>
    /// <item>open_department_id：以 open_department_id 来识别协作者。调用前，请确保应用有部门的可见性，参考[配置应用可用范围](https://open.feishu.cn/document/home/introduction-to-scope-and-authorization/availability)。获取 open_department_id 方式参考[部门资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/department/field-overview)</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param> 
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}/members/{member_id}")]
    Task<FeishuNullDataApiResult?> DeleteRoleMemberAsync(
      [Path] string app_token,
      [Path] string role_id,
      [Path] string member_id,
      [Query("member_id_type")] string member_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量删除协作者
    /// <para>删除多维表格高级权限中自定义角色的协作者。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/advanced-permission/app-role-member/batch_delete">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>   
    /// <param name="role_id">
    /// <para>多维表格高级权限中自定义角色的唯一标识，以 rol 开头。获取方式：通过[列出自定义角色](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-role/list)接口获取。</para>
    /// <para>示例值：roljRpwIUt</para>
    /// </param>
    /// <param name="deleteRoleMembersRequest">批量删除协作者请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}/members/batch_delete")]
    Task<FeishuNullDataApiResult?> DeleteRoleMembersAsync(
         [Path] string app_token,
         [Path] string role_id,
         [Body] DeleteRoleMembersRequest deleteRoleMembersRequest,
         CancellationToken cancellationToken = default);
}