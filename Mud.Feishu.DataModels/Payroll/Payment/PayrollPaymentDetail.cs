// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Payroll;

/// <summary>
/// 发薪明细（payment_detail）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Payroll")]
public class PayrollPaymentDetail
{
    /// <summary>
    /// <para>员工的飞书人事雇佣 ID（搜索员工信息接口的 user_id_type 应为 people_corehr_id）</para>
    /// <para>示例值：7202076988667019308</para>
    /// </summary>
    [JsonPropertyName("employee_id")]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// <para>发薪明细所在的发薪活动 ID</para>
    /// <para>示例值：7202076988667019308</para>
    /// </summary>
    [JsonPropertyName("activity_id")]
    public string? ActivityId { get; set; }

    /// <summary>
    /// <para>发薪明细详情</para>
    /// </summary>
    [JsonPropertyName("payment_accounting_items")]
    public PayrollPaymentAccountingItem[]? PaymentAccountingItems { get; set; }
}
