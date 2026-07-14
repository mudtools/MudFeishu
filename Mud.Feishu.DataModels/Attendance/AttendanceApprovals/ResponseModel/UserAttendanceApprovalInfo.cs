// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceApprovals;


/// <summary>
/// <para>审批结果列表</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class UserAttendanceApprovalInfo
{
    /// <summary>
    /// <para>审批用户 ID，类型与employee_type的取值一致</para>
    /// <para>必填：是</para>
    /// <para>示例值：abd754f7</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批作用日期，格式yyyyMMdd</para>
    /// <para>必填：是</para>
    /// <para>示例值：20210104</para>
    /// </summary>
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// <para>外出信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("outs")]
    public UserAttendanceOutInfo[]? Outs { get; set; }


    /// <summary>
    /// <para>请假信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("leaves")]
    public UserAttendanceLeaveInfo[]? Leaves { get; set; }


    /// <summary>
    /// <para>加班信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("overtime_works")]
    public UserAttendanceOvertimeWorkInfo[]? OvertimeWorks { get; set; }

    /// <summary>
    /// <para>出差信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("trips")]
    public UserAttendanceTripInfo[]? Trips { get; set; }

    /// <summary>
    /// <para>计算时间所用的时区信息，为空是0时区</para>
    /// <para>必填：否</para>
    /// <para>示例值：Asia/Shanghai</para>
    /// </summary>
    [JsonPropertyName("time_zone")]
    public string? TimeZone { get; set; }
}
