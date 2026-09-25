// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）猎头供应商入口域 SDK 是一组服务端 OpenAPI 的封装，用于猎头供应商查询（按 ID/名称/条件搜索）、猎头供应商下猎头账号的查询与禁用/取消禁用，以及人才猎头保护期的设置与查询。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/agency/batch_query"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireAgency : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 搜索猎头供应商列表
    /// <para>按猎头供应商 ID 列表或关键字、筛选项查询供应商信息，传 agency_supplier_id_list 时以其为准、其余查询字段失效；暂不支持查询「邀请中」的供应商。</para>
    /// <para>限频：10 次/秒。所需权限：hire:agency:readonly（获取猎头供应商信息）或 hire:agency（更新猎头供应商信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）、hire:agency.email:readonly（管理员邮箱）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/agency/batch_query">接口文档</see></para>
    /// </summary>
    /// <param name="request">搜索请求体（agency_supplier_id_list 最多 20 个；keyword 可传名称或邮箱；filter_list 支持 cooperation_create_time 范围及 cooperation_status/supplier_area/label_id_list 值筛选）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="page_size">每页数量，最大 20，默认 10</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回猎头供应商分页列表（items、has_more、page_token）</returns>
    [Post("/open-apis/hire/v1/agencies/batch_query")]
    Task<FeishuApiResult<BatchQueryAgencyResult>?> BatchQueryAgencyAsync(
        [Body] BatchQueryAgencyRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("page_token")] string? page_token = null,
        [Query("page_size")] int? page_size = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取猎头供应商信息
    /// <para>按猎头供应商 ID 获取猎头供应商信息，返回名称与供应商联系人。</para>
    /// <para>限频：10 次/秒。所需权限：hire:agency:readonly（获取猎头供应商信息）或 hire:agency（更新猎头供应商信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/agency/get">接口文档</see></para>
    /// </summary>
    /// <param name="agency_id">猎头供应商 ID，示例值：6898173495386147079</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回猎头供应商信息（agency）</returns>
    [Get("/open-apis/hire/v1/agencies/{agency_id}")]
    Task<FeishuApiResult<GetAgencyResult>?> GetAgencyAsync(
        [Path] string agency_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 按名称查询猎头供应商
    /// <para>按猎头供应商名称精准匹配查询（区分大小写）。</para>
    /// <para>限频：10 次/秒。所需权限：hire:agency:readonly（获取猎头供应商信息）或 hire:agency（更新猎头供应商信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/agency/query">接口文档</see></para>
    /// </summary>
    /// <param name="name">猎头供应商名称，精准匹配（区分大小写），示例值：超越猎头公司</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回猎头供应商列表（items）</returns>
    [Get("/open-apis/hire/v1/agencies/query")]
    Task<FeishuApiResult<QueryAgencyResult>?> QueryAgencyAsync(
        [Query("name")] string name,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询猎头供应商下猎头列表
    /// <para>按猎头供应商 ID 分页查询其下的猎头账号，可按猎头状态与角色过滤。</para>
    /// <para>限频：10 次/秒。所需权限：hire:agency_account:readonly（查询猎头供应商下猎头信息）或 hire:agency_account（更新猎头供应商下猎头信息）。字段权限：hire:agency.email:readonly（用户邮箱）、hire:agency.mobile:readonly（用户手机号）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/agency/get_agency_account">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（supplier_id 必填；status 猎头状态 0 正常/1 已禁用/2 自助停用；role 角色 0 管理员/1 顾问）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="page_size">每页数量，最大 20，默认 10</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回猎头账号分页列表（items、has_more、page_token）</returns>
    [Post("/open-apis/hire/v1/agencies/get_agency_account")]
    Task<FeishuApiResult<GetAgencyAccountResult>?> GetAgencyAccountAsync(
        [Body] GetAgencyAccountRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("page_token")] string? page_token = null,
        [Query("page_size")] int? page_size = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 禁用/取消禁用猎头
    /// <para>按猎头 ID 对猎头执行禁用或取消禁用；被禁用的猎头不能推荐候选人与被分配职位。</para>
    /// <para>限频：10 次/秒。所需权限：hire:agency_account（更新猎头供应商下猎头信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/agency/operate_agency_account">接口文档</see></para>
    /// </summary>
    /// <param name="request">操作请求体（option 必填：1 禁用/2 取消禁用；id 必填：猎头 ID；reason 禁用原因，option 为 1 时必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/agencies/operate_agency_account")]
    Task<FeishuNullDataApiResult?> OperateAgencyAccountAsync(
        [Body] OperateAgencyAccountRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 设置猎头保护期
    /// <para>设置指定人才的猎头保护期；当「飞书招聘」内置的保护期功能不满足需求时，可通过此接口自定义人才的保护期。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:agency（更新猎头供应商信息）。字段权限：contact:user.employee_id:readonly（取 user_id 时必填）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/agency/protect">接口文档</see></para>
    /// </summary>
    /// <param name="request">设置请求体（talent_id、supplier_id、consultant_id、protect_create_time、protect_expire_time 必填；comment、current_salary、expected_salary 选填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/agencies/protect")]
    Task<FeishuNullDataApiResult?> ProtectAgencyAsync(
        [Body] ProtectAgencyRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询猎头保护期信息
    /// <para>查询指定人才的猎头保护期信息列表，包含保护期起止时间、猎头供应商与猎头顾问信息；若人才已入职，还会返回入职时所在的保护期信息。</para>
    /// <para>限频：10 次/秒。所需权限：hire:agency:readonly（获取猎头供应商信息）或 hire:agency（更新猎头供应商信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/agency/protect_search">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（talent_id 必填：人才 ID）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回保护期信息（is_onboarded、onboarded_in_protection、onboarded_protection、protection_list）</returns>
    [Post("/open-apis/hire/v1/agencies/protection_period/search")]
    Task<FeishuApiResult<SearchAgencyProtectionResult>?> SearchAgencyProtectionAsync(
        [Body] SearchAgencyProtectionRequest request,
        CancellationToken cancellationToken = default);
}
