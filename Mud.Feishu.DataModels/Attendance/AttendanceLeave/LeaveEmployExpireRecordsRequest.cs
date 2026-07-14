// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceLeave;

/// <summary>
/// 通过过期时间获取发放记录请求体
/// </summary>
public class LeaveEmployExpireRecordsRequest
{
    /// <summary>
    /// <para>员工ID，与user_id_type保持一致</para>
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
    /// <para>失效最早日期，格式为yyyy-MM-dd</para>
    /// <para>必填：是</para>
    /// <para>示例值：2023-04-10</para>
    /// </summary>
    [JsonPropertyName("start_expiration_date")]
    public string StartExpirationDate { get; set; } = string.Empty;

    /// <summary>
    /// <para>失效最晚日期，格式为yyyy-MM-dd</para>
    /// <para>必填：是</para>
    /// <para>示例值：2023-05-10</para>
    /// </summary>
    [JsonPropertyName("end_expiration_date")]
    public string EndExpirationDate { get; set; } = string.Empty;

    /// <summary>
    /// <para>时间偏移，东八区：480 8*60， 如果没有这个参数，默认东八区</para>
    /// <para>必填：否</para>
    /// <para>示例值：480</para>
    /// </summary>
    [JsonPropertyName("time_offset")]
    public int? TimeOffset { get; set; }
}
