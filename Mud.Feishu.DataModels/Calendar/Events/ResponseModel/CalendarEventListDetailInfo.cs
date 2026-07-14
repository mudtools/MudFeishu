// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;


/// <summary>
/// <para>日程列表，当返回为空时，请根据has_more的值判断是否还有更多数据。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class CalendarEventListDetailInfo
{
    /// <summary>
    /// <para>日程 ID。后续可通过该 ID 查询、更新或删除日程信息。更多信息可参见[日程 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar-event/introduction)。</para>
    /// <para>必填：是</para>
    /// <para>示例值：00592a0e-7edf-4678-bc9d-1b77383ef08e_0</para>
    /// </summary>
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// <para>日程组织者的日历 ID。关于日历 ID 可参见[日历 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar/introduction)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </summary>
    [JsonPropertyName("organizer_calendar_id")]
    public string? OrganizerCalendarId { get; set; }

    /// <summary>
    /// <para>日程标题。</para>
    /// <para>必填：否</para>
    /// <para>示例值：日程标题</para>
    /// <para>最大长度：1000</para>
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// <para>日程描述。</para>
    /// <para>必填：否</para>
    /// <para>示例值：日程描述</para>
    /// <para>最大长度：40960</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>日程开始时间。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public CalendarTimeInfo StartTime { get; set; } = new();



    /// <summary>
    /// <para>日程结束时间。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public CalendarTimeInfo EndTime { get; set; } = new();

    /// <summary>
    /// <para>视频会议信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("vchat")]
    public CalendarEventVchatData? Vchat { get; set; }

    /// <summary>
    /// <para>日程公开范围。仅新建日程时对所有参与人生效，之后修改该属性仅对当前身份生效。</para>
    /// <para>必填：否</para>
    /// <para>示例值：default</para>
    /// <para>可选值：<list type="bullet">
    /// <item>default：默认权限，跟随日历权限，即默认仅向他人显示是否忙碌</item>
    /// <item>public：公开，显示日程详情</item>
    /// <item>private：私密，仅自己可见详情</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("visibility")]
    public string? Visibility { get; set; }

    /// <summary>
    /// <para>参与人权限。</para>
    /// <para>必填：否</para>
    /// <para>示例值：can_see_others</para>
    /// <para>可选值：<list type="bullet">
    /// <item>none：无法编辑日程、无法邀请其它参与人、无法查看参与人列表</item>
    /// <item>can_see_others：无法编辑日程、无法邀请其它参与人、可以查看参与人列表</item>
    /// <item>can_invite_others：无法编辑日程、可以邀请其它参与人、可以查看参与人列表</item>
    /// <item>can_modify_event：可以编辑日程、可以邀请其它参与人、可以查看参与人列表</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("attendee_ability")]
    public string? AttendeeAbility { get; set; }

    /// <summary>
    /// <para>日程占用的忙闲状态。仅新建日程时对所有参与人生效，之后修改该属性仅对当前身份生效。</para>
    /// <para>必填：否</para>
    /// <para>示例值：busy</para>
    /// <para>可选值：<list type="bullet">
    /// <item>busy：忙碌</item>
    /// <item>free：空闲</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("free_busy_status")]
    public string? FreeBusyStatus { get; set; }

    /// <summary>
    /// <para>日程地点。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("location")]
    public CalendarEventLocation? Location { get; set; }


    /// <summary>
    /// <para>日程颜色，由颜色 RGB 值的 int32 表示。</para>
    /// <para>**说明**：</para>
    /// <para>- 仅对当前身份生效。</para>
    /// <para>- 取值为 0 或 -1 时，表示默认跟随日历颜色。</para>
    /// <para>- 客户端展示时会映射到色板上最接近的一种颜色。</para>
    /// <para>必填：否</para>
    /// <para>示例值：-1</para>
    /// </summary>
    [JsonPropertyName("color")]
    public int? Color { get; set; }

    /// <summary>
    /// <para>日程提醒列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reminders")]
    public CalendarReminder[]? Reminders { get; set; }


    /// <summary>
    /// <para>重复日程的重复性规则，规则格式可参见 [rfc5545](https://datatracker.ietf.org/doc/html/rfc5545#section-3.3.10)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：FREQ=DAILY;INTERVAL=1</para>
    /// <para>最大长度：2000</para>
    /// </summary>
    [JsonPropertyName("recurrence")]
    public string? Recurrence { get; set; }

    /// <summary>
    /// <para>日程状态。</para>
    /// <para>必填：否</para>
    /// <para>示例值：confirmed</para>
    /// <para>可选值：<list type="bullet">
    /// <item>tentative：未回应</item>
    /// <item>confirmed：已确认</item>
    /// <item>cancelled：日程已取消</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>日程是否是一个重复日程的例外日程。了解例外日程，可参见[例外日程](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar-event/introduction#71c5ec78)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_exception")]
    public bool? IsException { get; set; }

    /// <summary>
    /// <para>例外日程对应的原重复日程的 event_id。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1cd45aaa-fa70-4195-80b7-c93b2e208f45</para>
    /// </summary>
    [JsonPropertyName("recurring_event_id")]
    public string? RecurringEventId { get; set; }

    /// <summary>
    /// <para>日程的创建时间（秒级时间戳）。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1602504000</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>日程自定义信息，控制日程详情页的 UI 展示。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("schemas")]
    public CalendarSchema[]? Schemas { get; set; }


    /// <summary>
    /// <para>日程组织者信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_organizer")]
    public CalendarEventEventOrganizer? EventOrganizer { get; set; }


    /// <summary>
    /// <para>日程的 app_link，跳转到具体的某个日程。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://applink.larkoffice.com/client/calendar/event/detail?calendarId=7039673579105026066&amp;key=aeac9c56-aeb1-4179-a21b-02f278f59048&amp;originalTime=0&amp;startTime=1700496000</para>
    /// </summary>
    [JsonPropertyName("app_link")]
    public string? AppLink { get; set; }

    /// <summary>
    /// <para>日程附件</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("attachments")]
    public CalendarAttachmentInfo[]? Attachments { get; set; }

}
