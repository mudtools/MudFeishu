// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>日程签到设置，为空则不进行日程签到设置。</para>
/// </summary>
public class CalendarEventCheckIn
{

    /// <summary>
    /// <para>是否启用日程签到。</para>
    /// <para>必填：是</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("enable_check_in")]
    public bool EnableCheckIn { get; set; }

    /// <summary>
    /// <para>日程签到开始时间。</para>
    /// <para>**注意**：签到开始时间不能大于或者等于签到结束时间。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("check_in_start_time")]
    public CheckInTime? CheckInStartTime { get; set; }



    /// <summary>
    /// <para>日程签到结束时间。</para>
    /// <para>**注意**：签到开始时间不能大于或者等于签到结束时间。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("check_in_end_time")]
    public CheckInTime? CheckInEndTime { get; set; }

    /// <summary>
    /// <para>签到开始时是否自动发送签到通知给参与者</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("need_notify_attendees")]
    public bool? NeedNotifyAttendees { get; set; }
}