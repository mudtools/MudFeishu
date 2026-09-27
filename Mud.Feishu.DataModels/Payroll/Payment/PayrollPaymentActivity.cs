// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 发薪活动（payment_activity）信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollPaymentActivity
{
    /// <summary>
    /// <para>发薪活动唯一标识</para>
    /// <para>示例值：7202076988667019308</para>
    /// </summary>
    [JsonPropertyName("activity_id")]
    public string? ActivityId { get; set; }

    /// <summary>
    /// <para>发薪活动名称（多语言）</para>
    /// </summary>
    [JsonPropertyName("activity_names")]
    public I18nContent[]? ActivityNames { get; set; }

    /// <summary>
    /// <para>发薪活动发薪日期</para>
    /// <para>示例值：2020-10-31</para>
    /// </summary>
    [JsonPropertyName("pay_date")]
    public string? PayDate { get; set; }

    /// <summary>
    /// <para>发薪总笔数</para>
    /// </summary>
    [JsonPropertyName("total_number_of_payroll")]
    public int? TotalNumberOfPayroll { get; set; }

    /// <summary>
    /// <para>关联的算薪活动个数</para>
    /// </summary>
    [JsonPropertyName("number_of_calculation_activities")]
    public int? NumberOfCalculationActivities { get; set; }

    /// <summary>
    /// <para>发薪活动关联的算薪活动详情</para>
    /// </summary>
    [JsonPropertyName("calculation_activities")]
    public PayrollCalculationActivity[]? CalculationActivities { get; set; }

    /// <summary>
    /// <para>发薪活动审批状态：100（待确认发薪名单）/ 150（待提交审批）/ 200（审批中）/ 300（审批被拒绝）/ 350（审批被撤回）/ 360（审批被撤销）/ 375（审批通过）/ 400（已封存）</para>
    /// </summary>
    [JsonPropertyName("activity_status")]
    public int? ActivityStatus { get; set; }
}
