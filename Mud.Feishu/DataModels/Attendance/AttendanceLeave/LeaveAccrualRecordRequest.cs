// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceLeave;

/// <summary>
/// 修改发放记录请求体
/// </summary>
public class LeaveAccrualRecordRequest
{
    /// <summary>
    /// <para>发放记录的唯一ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6893014062142064135</para>
    /// </summary>
    [JsonPropertyName("leave_granting_record_id")]
    public string LeaveGrantingRecordId { get; set; } = string.Empty;

    /// <summary>
    /// <para>员工ID，类型对应user_id_type</para>
    /// <para>必填：是</para>
    /// <para>示例值：6982509313466189342</para>
    /// </summary>
    [JsonPropertyName("employment_id")]
    public string EmploymentId { get; set; } = string.Empty;

    /// <summary>
    /// <para>假期类型ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：7111688079785723436</para>
    /// </summary>
    [JsonPropertyName("leave_type_id")]
    public string LeaveTypeId { get; set; } = string.Empty;

    /// <summary>
    /// <para>修改发放记录原因</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public LangText[] Reasons { get; set; } = [];

    /// <summary>
    /// <para>时间偏移，东八区：480 8*60</para>
    /// <para>必填：否</para>
    /// <para>示例值：480</para>
    /// </summary>
    [JsonPropertyName("time_offset")]
    public int? TimeOffset { get; set; }

    /// <summary>
    /// <para>失效日期，格式"2020-01-01"</para>
    /// <para>必填：否</para>
    /// <para>示例值：2020-01-01</para>
    /// </summary>
    [JsonPropertyName("expiration_date")]
    public string? ExpirationDate { get; set; }

    /// <summary>
    /// <para>修改发放数量</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("quantity")]
    public string? Quantity { get; set; }
}
