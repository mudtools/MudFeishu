// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Payroll;

namespace Mud.Feishu;


/// <summary>
/// 飞书薪酬发放（Payroll）「成本分摊」SDK 是一组服务端 OpenAPI 的封装，用于批量查询成本分摊方案、查询成本分摊报表明细与汇总数据。调用明细/汇总接口前，需打开「财务过账」开关并完成发布成本分摊报表。本接口全部端点为 payroll/v1，仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/payroll-v1/cost_allocation_plan/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Payroll")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1PayrollCostAllocation : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 批量查询成本分摊方案
    /// <para>根据期间分页批量查询成本分摊方案，仅返回期间内生效的方案列表。</para>
    /// <para>限频：5 次/秒。所需权限：payroll:cost_allocation_plan:read（获取成本分摊方案）。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/cost_allocation_plan/list">接口文档</see></para>
    /// </summary>
    /// <param name="pay_period">期间（必填），生成成本分摊报表对应的年月，格式为 yyyy-MM</param>
    /// <param name="page_size">分页大小（必填），取值范围 1~100</param>
    /// <param name="page_token">分页标记，第一次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回成本分摊方案分页列表（items：id/names/applicable_country_region/dimensions/cost_items，page_token、has_more）</returns>
    [Get("/open-apis/payroll/v1/cost_allocation_plans")]
    Task<FeishuApiResult<ListCostAllocationPlansResult>?> ListCostAllocationPlansAsync(
        [Query("pay_period")] string pay_period,
        [Query("page_size")] int page_size,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询成本分摊报表明细
    /// <para>根据报表方案、期间和报表类型获取成本分摊明细数据；调用前需打开「财务过账」开关并完成发布成本分摊报表。</para>
    /// <para>限频：5 次/秒。所需权限：payroll:cost_allocation_details:read（获取成本分摊报表明细数据）。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/cost_allocation_detail/list">接口文档</see></para>
    /// </summary>
    /// <param name="cost_allocation_plan_id">成本分摊方案 ID（必填），通过批量查询成本分摊方案接口获取</param>
    /// <param name="pay_period">期间（必填），成本分摊报表对应的年月，长度为 7 个字符</param>
    /// <param name="report_type">报表类型（必填）：0（默认）/ 1（计提）/ 2（实发）</param>
    /// <param name="page_size">分页大小（必填），取值范围 1~100</param>
    /// <param name="page_token">分页标记，第一次请求不填，翻页时取上一次返回的 page_token；报表已变更（错误码 2500005）时需不传 page_token 重新从头拉取</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回报表明细分页数据（cost_allocation_report_datas：数据维度汇总/成本项数据/employment_id、cost_allocation_report_names、pay_period，page_token、has_more）</returns>
    [Get("/open-apis/payroll/v1/cost_allocation_details")]
    Task<FeishuApiResult<ListCostAllocationDetailsResult>?> ListCostAllocationDetailsAsync(
        [Query("cost_allocation_plan_id")] string cost_allocation_plan_id,
        [Query("pay_period")] string pay_period,
        [Query("report_type")] int report_type,
        [Query("page_size")] int page_size,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询成本分摊报表汇总数据
    /// <para>根据算薪期间和成本分摊方案 ID 获取成本分摊汇总数据；调用前需在 Payroll 系统中打开「财务过账」开关并完成发布成本分摊报表。</para>
    /// <para>限频：5 次/秒。所需权限：payroll:cost_allocation_report:read（获取成本分摊报表汇总数据）。</para>
    /// <para><see href="https://open.feishu.cn/document/payroll-v1/cost_allocation_report/list">接口文档</see></para>
    /// </summary>
    /// <param name="cost_allocation_plan_id">成本分摊方案 ID（必填），通过批量查询成本分摊方案接口获取</param>
    /// <param name="pay_period">期间（必填），成本分摊数据对应的年月，格式为 yyyy-MM</param>
    /// <param name="report_type">报表类型（必填）：0（默认，未开通计提和实发功能时的报表类型）/ 1（计提）/ 2（实发）</param>
    /// <param name="page_size">分页大小（必填），取值范围 1~100</param>
    /// <param name="page_token">分页标记，第一次请求不填，翻页时取上一次返回的 page_token；报表已变更（错误码 2500005）时需不传 page_token 重新从头拉取</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回报表汇总分页数据（pay_period、cost_allocation_report_names、cost_allocation_report_datas：数据维度汇总/成本项数据（含发薪人数），page_token、has_more）</returns>
    [Get("/open-apis/payroll/v1/cost_allocation_reports")]
    Task<FeishuApiResult<ListCostAllocationReportsResult>?> ListCostAllocationReportsAsync(
        [Query("cost_allocation_plan_id")] string cost_allocation_plan_id,
        [Query("pay_period")] string pay_period,
        [Query("report_type")] int report_type,
        [Query("page_size")] int page_size,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
