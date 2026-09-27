// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Payroll;

namespace Mud.Feishu;


/// <summary>
/// 飞书薪酬发放（Payroll）「发薪活动与发薪明细」SDK 是一组服务端 OpenAPI 的封装，用于封存发薪活动、查询发薪活动列表、查询发薪活动明细列表以及按员工批量查询发薪明细。本接口全部端点为 payroll/v1，仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/payroll-v1/payment_activity/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Payroll")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1PayrollPayment : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 封存发薪活动
    /// <para>根据发薪活动 ID 对发薪活动进行封存；仅当发薪活动状态为审批通过时方可进行封存。</para>
    /// <para>限频：10 次/分钟。所需权限：payroll:payment_activity:archive（封存发薪活动）。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/payment_activity/archive">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（activity_id 发薪活动 ID 必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/payroll/v1/payment_activitys/archive")]
    Task<FeishuNullDataApiResult?> ArchivePaymentActivityAsync(
        [Body] ArchivePaymentActivityRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询发薪活动列表
    /// <para>根据「发薪日起止范围」「发薪活动状态」和「分页参数」查询发薪活动列表。</para>
    /// <para>限频：5 次/秒。所需权限：payroll:payment_activity:read（获取发薪活动列表数据）。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/payment_activity/list">接口文档</see></para>
    /// </summary>
    /// <param name="pay_period_start_date">发薪日开始时间（必填），格式 YYYY-MM-dd，[pay_period_start_date, pay_period_end_date] 为左闭右闭区间</param>
    /// <param name="pay_period_end_date">发薪日结束时间（必填），格式 YYYY-MM-dd，[pay_period_start_date, pay_period_end_date] 为左闭右闭区间</param>
    /// <param name="page_size">分页大小（必填），取值范围 [1, 100]</param>
    /// <param name="page_token">分页标记，第一次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="statuses">发薪活动审批状态列表（0~100 个）：100（待确认名单）/ 150（待提交审批）/ 200（审批中）/ 300（审批被拒绝）/ 350（审批被撤回）/ 360（审批被撤销）/ 375（审批通过）/ 400（已封存）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回发薪活动分页列表（payment_activitys：activity_id/activity_names/pay_date/total_number_of_payroll/calculation_activities/activity_status，page_token、has_more）</returns>
    [Get("/open-apis/payroll/v1/payment_activitys")]
    Task<FeishuApiResult<ListPaymentActivitysResult>?> ListPaymentActivitysAsync(
        [Query("pay_period_start_date")] string pay_period_start_date,
        [Query("pay_period_end_date")] string pay_period_end_date,
        [Query("page_size")] int page_size,
        [Query("page_token")] string? page_token = null,
        [Query("statuses")] int[]? statuses = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询发薪活动明细列表
    /// <para>根据「发薪活动 ID」和「分页参数」查询发薪活动明细列表和关联的算薪明细分段数据；当前接口仅支持查询某个发薪活动下的所有发薪明细数据。</para>
    /// <para>限频：5 次/秒。所需权限：payroll:payment_activity_details:read（获取发薪明细数据）。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/payment_detail/list">接口文档</see></para>
    /// </summary>
    /// <param name="activity_id">发薪活动 ID（必填），通过查询发薪活动列表接口获取</param>
    /// <param name="page_index">页码（必填），第一页从 1 开始，取值范围 1~100000</param>
    /// <param name="page_size">每页大小（必填），范围 [1, 100]</param>
    /// <param name="include_segment_data">是否需要查询算薪明细的分段信息；不传或传 false 时只返回发薪活动明细数据，传 true 时同时返回算薪明细分段数据</param>
    /// <param name="acct_item_ids">算薪项 ID 列表（0~100 个）；传空时返回发薪明细中所有的算薪项，不为空时只返回与 acct_item_ids 存在交集的算薪项</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回发薪活动明细分页数据（payment_activity_details：employee_id/payment_details（算薪项值与分段数据），total）</returns>
    [Get("/open-apis/payroll/v1/payment_activity_details")]
    Task<FeishuApiResult<ListPaymentActivityDetailsResult>?> ListPaymentActivityDetailsAsync(
        [Query("activity_id")] string activity_id,
        [Query("page_index")] int page_index,
        [Query("page_size")] int page_size,
        [Query("include_segment_data")] bool? include_segment_data = null,
        [Query("acct_item_ids")] string[]? acct_item_ids = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量查询发薪明细
    /// <para>根据发薪活动 ID 列表、发薪日起止时间和飞书人事雇佣 ID 列表分页查询发薪明细列表和关联的算薪明细分段数据。「发薪日起止时间」与「发薪活动 ID 列表」不得均为空；系统每次最多扫描 50 个发薪活动；数据取自发薪活动，调用前请先创建发薪活动并完成算薪活动关联。</para>
    /// <para>限频：1 次/秒。所需权限：payroll:payment_details:read（获取发薪明细数据 V2）。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/payment_detail/query">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（page_index/page_size/employee_ids 必填；pay_period_start_date 与 pay_period_end_date、activity_ids 不得均为空；acct_item_ids 可选；include_segment_data 可选）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回发薪明细分页数据（payment_details：employee_id/activity_id/payment_accounting_items，total）</returns>
    [Post("/open-apis/payroll/v1/payment_detail/query")]
    Task<FeishuApiResult<QueryPaymentDetailsResult>?> QueryPaymentDetailsAsync(
        [Body] QueryPaymentDetailsRequest request,
        CancellationToken cancellationToken = default);
}
