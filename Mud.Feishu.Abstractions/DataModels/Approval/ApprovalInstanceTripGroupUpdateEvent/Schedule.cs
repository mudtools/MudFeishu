// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Approval;

/// <summary>
/// 出差详细信息
/// </summary>
public class Schedule
{
    /// <summary>
    /// <para>出发地。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("departure")]
    public string? Departure { get; set; }

    /// <summary>
    /// <para>出发地 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("departure_id")]
    public string? DepartureId { get; set; }

    /// <summary>
    /// <para>目的地。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("destination")]
    public string? Destination { get; set; }

    /// <summary>
    /// <para>目的地 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("destination_ids")]
    public string[]? DestinationIds { get; set; }

    /// <summary>
    /// <para>备注信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }

    /// <summary>
    /// <para>交通工具。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("transportation")]
    public string? Transportation { get; set; }

    /// <summary>
    /// <para>出差开始时间。示例格式：2022-08-25 12:00:00</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("trip_start_time")]
    public string? TripStartTime { get; set; }

    /// <summary>
    /// <para>出差结束时间。示例格式：2022-08-25 12:00:00</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("trip_end_time")]
    public string? TripEndTime { get; set; }

    /// <summary>
    /// <para>出差时长。单位：秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("trip_interval")]
    public string? TripInterval { get; set; }

    /// <summary>
    /// <para>出差类型。可能值：</para>
    /// <para>- 单程</para>
    /// <para>- 往返</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("trip_type")]
    public string? TripType { get; set; }
}
