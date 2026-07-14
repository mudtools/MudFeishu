// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceApprovals;

/// <summary>
/// <para>出差信息。</para>
/// <para>目前仅支持全天出差（未满全天则按全天计入）。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class UserAttendanceTripData
{
    /// <summary>
    /// <para>开始时间，时间格式为 yyyy-MM-dd HH:mm:ss</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021-01-04 09:00:00</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>结束时间，时间格式为 yyyy-MM-dd HH:mm:ss</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021-01-04 19:00:00</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>出差理由</para>
    /// <para>必填：是</para>
    /// <para>示例值：培训</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批通过时间，时间格式为 yyyy-MM-dd HH:mm:ss</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021-01-04 12:00:00</para>
    /// </summary>
    [JsonPropertyName("approve_pass_time")]
    public string ApprovePassTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批申请时间，时间格式为 yyyy-MM-dd HH:mm:ss</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021-01-04 11:00:00</para>
    /// </summary>
    [JsonPropertyName("approve_apply_time")]
    public string ApproveApplyTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>出差记录的唯一幂等键，用于避免出差记录重复创建，可以填入三方的出差记录id</para>
    /// <para>必填：否</para>
    /// <para>示例值：1233432312</para>
    /// </summary>
    [JsonPropertyName("idempotent_id")]
    public string? IdempotentId { get; set; }

    /// <summary>
    /// <para>更正流程实例 ID。该字段由系统自动生成，在写入审批结果时，无需传入该参数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("correct_process_id")]
    public string[]? CorrectProcessId { get; set; }

    /// <summary>
    /// <para>撤销流程实例 ID。该字段由系统自动生成，在写入审批结果时，无需传入该参数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cancel_process_id")]
    public string[]? CancelProcessId { get; set; }

    /// <summary>
    /// <para>发起流程实例 ID。该字段由系统自动生成，在写入审批结果时，无需传入该参数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string[]? ProcessId { get; set; }

    /// <summary>
    /// <para>出发地（只有一个）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("departure")]
    public AttendanceRegionPlace? Departure { get; set; }


    /// <summary>
    /// <para>目的地（可写多个）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("destinations")]
    public AttendanceRegionPlace[]? Destinations { get; set; }

    /// <summary>
    /// <para>交通工具（1 飞机，2 火车，3 汽车，4 高铁/动车，5 船，6 其他）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("transportation")]
    public int[]? Transportation { get; set; }

    /// <summary>
    /// <para>出差类型(1:单程 2:往返)</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("trip_type")]
    public int? TripType { get; set; }

    /// <summary>
    /// <para>出差备注</para>
    /// <para>必填：否</para>
    /// <para>示例值：出差备注</para>
    /// </summary>
    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}
