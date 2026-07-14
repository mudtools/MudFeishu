// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>会议室预定列表</para>
/// </summary>
public class RoomMeetingReservation
{

    /// <summary>
    /// <para>会议室ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：omm_4de32cf10a4358788ff4e09e37ebbf9b</para>
    /// </summary>
    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }

    /// <summary>
    /// <para>会议室名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：VIP Meeting Room</para>
    /// </summary>
    [JsonPropertyName("room_name")]
    public string? RoomName { get; set; }

    /// <summary>
    /// <para>会议标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：飞书邀请的日程</para>
    /// </summary>
    [JsonPropertyName("event_title")]
    public string? EventTitle { get; set; }

    /// <summary>
    /// <para>预定人</para>
    /// <para>必填：否</para>
    /// <para>示例值：kehan</para>
    /// </summary>
    [JsonPropertyName("reserver")]
    public string? Reserver { get; set; }

    /// <summary>
    /// <para>预定人ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_1234567(UserID);cli_123123(BotID)</para>
    /// </summary>
    [JsonPropertyName("reserver_user_id")]
    public string? ReserverUserId { get; set; }

    /// <summary>
    /// <para>预定人所属部门</para>
    /// <para>必填：否</para>
    /// <para>示例值：development</para>
    /// </summary>
    [JsonPropertyName("department_of_reserver")]
    public string? DepartmentOfReserver { get; set; }

    /// <summary>
    /// <para>邀约人数</para>
    /// <para>必填：否</para>
    /// <para>示例值：5</para>
    /// </summary>
    [JsonPropertyName("guests_number")]
    public string? GuestsNumber { get; set; }

    /// <summary>
    /// <para>接受人数</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("accepted_number")]
    public string? AcceptedNumber { get; set; }

    /// <summary>
    /// <para>会议开始时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022.12.17 21:00:00 (GMT+08:00)</para>
    /// </summary>
    [JsonPropertyName("event_start_time")]
    public string? EventStartTime { get; set; }

    /// <summary>
    /// <para>会议结束时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022.12.17 22:00:00 (GMT+08:00)</para>
    /// </summary>
    [JsonPropertyName("event_end_time")]
    public string? EventEndTime { get; set; }

    /// <summary>
    /// <para>会议时长</para>
    /// <para>必填：否</para>
    /// <para>示例值：1:00:00</para>
    /// </summary>
    [JsonPropertyName("event_duration")]
    public string? EventDuration { get; set; }

    /// <summary>
    /// <para>会议室预定状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：预定成功</para>
    /// </summary>
    [JsonPropertyName("reservation_status")]
    public string? ReservationStatus { get; set; }

    /// <summary>
    /// <para>签到设备</para>
    /// <para>必填：否</para>
    /// <para>示例值：签到板</para>
    /// </summary>
    [JsonPropertyName("check_in_device")]
    public string? CheckInDevice { get; set; }

    /// <summary>
    /// <para>会议室签到状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：已签到</para>
    /// </summary>
    [JsonPropertyName("room_check_in_status")]
    public string? RoomCheckInStatus { get; set; }

    /// <summary>
    /// <para>会议室签到时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022.12.09 13:35:30 (GMT+08:00)</para>
    /// </summary>
    [JsonPropertyName("check_in_time")]
    public string? CheckInTime { get; set; }

    /// <summary>
    /// <para>是否提前释放</para>
    /// <para>必填：否</para>
    /// <para>示例值：已释放（手动释放）</para>
    /// </summary>
    [JsonPropertyName("is_release_early")]
    public string? IsReleaseEarly { get; set; }

    /// <summary>
    /// <para>释放人</para>
    /// <para>必填：否</para>
    /// <para>示例值：kehan</para>
    /// </summary>
    [JsonPropertyName("releasing_person")]
    public string? ReleasingPerson { get; set; }

    /// <summary>
    /// <para>释放时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022.12.20 11:25:15 (GMT+08:00)</para>
    /// </summary>
    [JsonPropertyName("releasing_time")]
    public string? ReleasingTime { get; set; }
}