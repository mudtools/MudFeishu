// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu;

/// <summary>
/// 飞书应用管理接口，用于查询企业安装的应用列表、用户可用的应用、应用可用范围，检查可用/禁用名单，更新应用可用范围与通讯录权限范围，以及查询应用管理员信息。
/// <para><see href="https://open.feishu.cn/document/application-v6/admin/list">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Application")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV6ApplicationAdmin : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取企业安装的应用列表。
    /// <para>查询企业安装的应用列表，只能被企业自建应用调用。</para>
    /// <para><see href="https://open.feishu.cn/document/application-v6/admin/list">接口文档</see></para>
    /// </summary>
    /// <param name="lang">
    /// <para>指定返回结果的语言</para>
    /// <para>示例值：zh_cn</para>
    /// </param>
    /// <param name="page_size">分页大小。默认值：50</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="status">应用状态筛选。0：已停用；1：已启用；2：未启用</param>
    /// <param name="payment_type">付费类型筛选。0：免费；1：付费</param>
    /// <param name="owner_type">应用提供方筛选。0：飞书科技；1：飞书合作伙伴；2：企业内部成员</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/application/v6/applications")]
    Task<FeishuApiResult<GetApplicationsResult>?> GetApplicationsPageListAsync(
      [Query] string lang,
      [Query] int? page_size = Consts.PageSize_50,
      [Query] string? page_token = null,
      [Query] int? status = null,
      [Query] int? payment_type = null,
      [Query] int? owner_type = null,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户可用的应用。
    /// <para>查询用户可用的应用列表。open_id 与 user_id 至少传一个，user_id 优先于 open_id。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/admin/obtain-the-apps-available-to-a-user">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">目标用户的 user_id。与 open_id 至少传一个，优先级高于 open_id</param>
    /// <param name="open_id">目标用户的 open_id。与 user_id 至少传一个</param>
    /// <param name="page_size">每页最大数量（最大 100），传 0 表示自动取最大值</param>
    /// <param name="page_token">分页起始位置，不传表示从头开始</param>
    /// <param name="lang">
    /// <para>应用信息展示的语言版本</para>
    /// <para>可选值：zh_cn、zh_hk、zh_tw、en_us、ja_jp、ko_kr、es_es、pt-br、th_th、vi_vn、id_id</para>
    /// <para>示例值：zh_cn</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/application/v1/user/visible_apps")]
    Task<FeishuApiResult<GetUserVisibleAppsResult>?> GetUserVisibleAppsPageAsync(
      [Query] string? user_id = null,
      [Query] string? open_id = null,
      [Query] int? page_size = null,
      [Query] string? page_token = null,
      [Query] string? lang = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取应用可用范围。
    /// <para>查询应用在当前企业的可用范围，包括可用/禁用的部门、用户与用户群，以及是否全员可见。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/admin/obtain-the-app-availability-in-an-organization">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_a3a3d00b40b8d01b</para>
    /// </param>
    /// <param name="user_page_size">本次拉取的最大用户数量（最大 1000，传 0 表示 1000）</param>
    /// <param name="user_page_token">分页拉取可使用应用的用户起始位置，不传表示从头开始</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/application/v2/app/visibility")]
    Task<FeishuApiResult<GetAppVisibilityResult>?> GetAppVisibilityAsync(
      [Query] string app_id,
      [Query] int? user_page_size = null,
      [Query] string? user_page_token = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询用户或部门是否在应用的可用或禁用名单。
    /// <para>查询用户、部门、用户组是否在应用的可用（白）或禁用（黑）名单中。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/admin/check_white_black_list">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID。查询其他应用信息时必须申请「获取应用信息」权限，仅查询本应用时可传入本应用的 app_id</para>
    /// <para>示例值：cli_a3a3d00b40b8d01b</para>
    /// </param>
    /// <param name="request">黑白名单查询请求体</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="department_id_type">部门 ID 类型，默认值：department_id。可选值：department_id、open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/application/v6/applications/{app_id}/visibility/check_white_black_list")]
    Task<FeishuApiResult<CheckVisibilityWhiteBlackListResult>?> CheckVisibilityWhiteBlackListAsync(
      [Path] string app_id,
      [Body] CheckVisibilityWhiteBlackListRequest request,
      [Query] string? user_id_type = Consts.User_Id_Type,
      [Query] string? department_id_type = "department_id",
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新应用可用范围。
    /// <para>更新指定应用的可用范围（支持当前企业内自建应用或已安装的商店应用），包括可用人员与禁用人员。更新后对线上立即生效。</para>
    /// <para><see href="https://open.feishu.cn/document/application-v6/admin/patch-5">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_9b445f5258795107</para>
    /// </param>
    /// <param name="request">更新应用可用范围请求体</param>
    /// <param name="department_id_type">部门 ID 类型，默认值：open_department_id。可选值：open_department_id、department_id</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/application/v6/applications/{app_id}/visibility")]
    Task<FeishuNullDataApiResult?> UpdateApplicationVisibilityAsync(
      [Path] string app_id,
      [Body] UpdateApplicationVisibilityRequest request,
      [Query] string? department_id_type = Consts.Department_Id_Type,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新应用通讯录权限范围。
    /// <para>更新自建应用或已安装的商店应用的通讯录权限范围，更新后线上立即生效。通讯录权限范围指应用调用通讯录相关接口时可获取的部门和用户数据范围。</para>
    /// <para><see href="https://open.feishu.cn/document/application-v6/admin/patch-4">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_dsfjksdfee1</para>
    /// </param>
    /// <param name="request">更新应用通讯录权限范围请求体</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="department_id_type">部门 ID 类型，默认值：open_department_id。可选值：open_department_id、department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/application/v6/applications/{app_id}/contacts_range")]
    Task<FeishuNullDataApiResult?> UpdateApplicationContactsRangeAsync(
      [Path] string app_id,
      [Body] UpdateApplicationContactsRangeRequest request,
      [Query] string? user_id_type = Consts.User_Id_Type,
      [Query] string? department_id_type = Consts.Department_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询应用管理员列表。
    /// <para>查询审核该应用的管理员列表，返回最近的 10 个管理员账号的 ID。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/admin/query-app-administrator-list">接口文档</see></para>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/user/v4/app_admin_user/list")]
    Task<FeishuApiResult<GetAppAdminUsersResult>?> GetAppAdminUsersAsync(
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取应用管理员管理范围。
    /// <para>获取应用管理员的管理范围，即应用管理员管理哪些部门。employee_id 与 open_id 二选一传入。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/admin/obtain-an-app-admin%E2%80%99s-management-permissions">接口文档</see></para>
    /// </summary>
    /// <param name="open_id">管理员的 open_id，与 employee_id 二选一传入</param>
    /// <param name="employee_id">管理员的 employee_id（等价于通讯录 v3 中的 user_id），与 open_id 二选一传入</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/contact/v1/user/admin_scope/get")]
    Task<FeishuApiResult<GetAppAdminScopeResult>?> GetAppAdminScopeAsync(
      [Query] string? open_id = null,
      [Query] string? employee_id = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验应用管理员。
    /// <para>查询用户是否为应用管理员。此处的管理员指可进入企业管理后台审核与管理应用的管理员，而非应用开发者。open_id 与 employee_id 至少传一个，同时传入时以 open_id 为准。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/admin/verify-app-admin">接口文档</see></para>
    /// </summary>
    /// <param name="open_id">用户的 open_id，与 employee_id 至少传一个，同时传入时以 open_id 为准</param>
    /// <param name="employee_id">用户的 employee_id（等价于通讯录 v3 中的 user_id），与 open_id 至少传一个</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/application/v3/is_user_admin")]
    Task<FeishuApiResult<VerifyAppAdminResult>?> VerifyAppAdminAsync(
      [Query] string? open_id = null,
      [Query] string? employee_id = null,
      CancellationToken cancellationToken = default);
}
