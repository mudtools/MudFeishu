// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 批量查询发薪明细请求体（发薪日起止时间与发薪活动 ID 列表不得均为空；系统每次最多扫描 50 个发薪活动）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class QueryPaymentDetailsRequest
{
    /// <summary>
    /// <para>页码，第一页从 1 开始（1~100000）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("page_index")]
    public int? PageIndex { get; set; }

    /// <summary>
    /// <para>每页大小，范围 [1, 100]</para>
    /// <para>必填：是</para>
    /// <para>示例值：10</para>
    /// </summary>
    [JsonPropertyName("page_size")]
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>算薪项 ID 列表（0~100000 个）。传空时返回发薪明细中所有的算薪项；不为空时只返回与 acct_item_ids 存在交集的算薪项</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7202076988667019333"]</para>
    /// </summary>
    [JsonPropertyName("acct_item_ids")]
    public string[]? AcctItemIds { get; set; }

    /// <summary>
    /// <para>员工的飞书人事雇佣 ID 列表（必填，1~100 个），通过搜索员工信息接口获取（查询入参 user_id_type 应为 people_corehr_id）</para>
    /// <para>必填：是</para>
    /// <para>示例值：["7202076988667019222"]</para>
    /// </summary>
    [JsonPropertyName("employee_ids")]
    public string[]? EmployeeIds { get; set; }

    /// <summary>
    /// <para>发薪日开始时间，格式 YYYY-MM-dd，[pay_period_start_date, pay_period_end_date] 为左闭右闭区间，最大间隔 12 个月</para>
    /// <para>必填：否</para>
    /// <para>示例值：2024-01-01</para>
    /// </summary>
    [JsonPropertyName("pay_period_start_date")]
    public string? PayPeriodStartDate { get; set; }

    /// <summary>
    /// <para>发薪日结束时间，格式 YYYY-MM-dd，[pay_period_start_date, pay_period_end_date] 为左闭右闭区间，最大间隔 12 个月</para>
    /// <para>必填：否</para>
    /// <para>示例值：2024-01-31</para>
    /// </summary>
    [JsonPropertyName("pay_period_end_date")]
    public string? PayPeriodEndDate { get; set; }

    /// <summary>
    /// <para>发薪活动 ID 列表（0~50 个），可通过查询发薪活动列表接口获取</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7202076988667019308"]</para>
    /// </summary>
    [JsonPropertyName("activity_ids")]
    public string[]? ActivityIds { get; set; }

    /// <summary>
    /// <para>是否需要查询算薪明细的分段信息；不传或传 false 时只返回发薪活动明细数据，传 true 时同时返回发薪明细对应的算薪明细分段数据</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("include_segment_data")]
    public bool? IncludeSegmentData { get; set; }
}
