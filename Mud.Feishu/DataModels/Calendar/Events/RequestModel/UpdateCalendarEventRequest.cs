// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// 更新日程请求体
/// </summary>
public class UpdateCalendarEventRequest
{
    /// <summary>
    /// <para>日程标题。</para>
    /// <para>**默认值**：空，表示不更新该字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：团队周会</para>
    /// <para>最大长度：1000</para>
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// <para>日程描述。</para>
    /// <para>**注意**：目前 API 方式不支持编辑富文本描述。如果日程描述通过客户端编辑为富文本内容，则使用 API 更新描述会导致富文本格式丢失。</para>
    /// <para>**默认值**：空，表示不更新该字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：讨论项目进展</para>
    /// <para>最大长度：40960</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>更新日程时，是否给日程参与人发送 Bot 通知。</para>
    /// <para>**默认值**：空，表示不更新该字段</para>
    /// <para>**可选值有**：</para>
    /// <para>- true：发送通知</para>
    /// <para>- false：不发送通知</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("need_notification")]
    public bool? NeedNotification { get; set; }

    /// <summary>
    /// <para>日程开始时间。需要与end_time同时有值才会生效。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public CalendarTimeInfo? StartTime { get; set; }


    /// <summary>
    /// <para>日程结束时间。需要与start_time同时有值才会生效。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public CalendarTimeInfo? EndTime { get; set; }

    /// <summary>
    /// <para>视频会议信息。不传值则表示不更新该字段。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("vchat")]
    public CalendarsVchatInfo? Vchat { get; set; }


    /// <summary>
    /// <para>日程公开范围。</para>
    /// <para>**注意**：更新日程时如果修改了该参数值，则仅对当前身份生效。</para>
    /// <para>**默认值**：空，表示不更新该字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：default</para>
    /// <para>可选值：<list type="bullet">
    /// <item>default：默认权限，即跟随日历权限，默认仅向他人显示是否忙碌</item>
    /// <item>public：公开，显示日程详情</item>
    /// <item>private：私密，仅自己可见详情</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("visibility")]
    public string? Visibility { get; set; }

    /// <summary>
    /// <para>参与人权限。</para>
    /// <para>**默认值**：空，表示不更新该字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：can_see_others</para>
    /// <para>可选值：<list type="bullet">
    /// <item>none：无法编辑日程、无法邀请其他参与人、无法查看参与人列表</item>
    /// <item>can_see_others：无法编辑日程、无法邀请其他参与人、可以查看参与人列表</item>
    /// <item>can_invite_others：无法编辑日程、可以邀请其他参与人、可以查看参与人列表</item>
    /// <item>can_modify_event：可以编辑日程、可以邀请其他参与人、可以查看参与人列表</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("attendee_ability")]
    public string? AttendeeAbility { get; set; }

    /// <summary>
    /// <para>日程占用的忙闲状态，新建日程默认为 `busy`。</para>
    /// <para>**注意**：更新日程时如果修改了该参数值，则仅对当前身份生效。</para>
    /// <para>**默认值**：空，表示不更新该字段</para>
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
    /// <para>日程地点。不传值则表示不更新该字段。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("location")]
    public CalendarEventLocation? Location { get; set; }

    /// <summary>
    /// <para>日程颜色，取值通过颜色 RGB 值的 int32 表示。</para>
    /// <para>**注意**：</para>
    /// <para>- 该参数仅对当前身份生效。</para>
    /// <para>- 客户端展示时会映射到色板上最接近的一种颜色。</para>
    /// <para>- 取值为 0 或 -1 时，默认跟随日历颜色。</para>
    /// <para>**默认值**：空，表示不更新该字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：-1</para>
    /// </summary>
    [JsonPropertyName("color")]
    public int? Color { get; set; }

    /// <summary>
    /// <para>日程提醒列表。不传值则表示不更新该字段。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reminders")]
    public CalendarReminder[]? Reminders { get; set; }


    /// <summary>
    /// <para>重复日程的重复性规则，规则设置方式参考[rfc5545](https://datatracker.ietf.org/doc/html/rfc5545#section-3.3.10)。</para>
    /// <para>**注意**：</para>
    /// <para>- COUNT 和 UNTIL 不支持同时出现。</para>
    /// <para>- 预定会议室重复日程长度不得超过两年。</para>
    /// <para>**默认值**：空，表示不更新该字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：FREQ=DAILY;INTERVAL=1</para>
    /// <para>最大长度：2000</para>
    /// </summary>
    [JsonPropertyName("recurrence")]
    public string? Recurrence { get; set; }

    /// <summary>
    /// <para>日程自定义信息，控制日程详情页的 UI 展示。schemas字段不传值则表示不更新该字段。</para>
    /// <para>**注意：**</para>
    /// <para>1. schemas传值的情况下，每次都是覆盖更新，即用传入的列表去更新原来的列表。</para>
    /// <para>2. 可以使用[]空列表来清空schemas原来的数据。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("schemas")]
    public CalendarSchema[]? Schemas { get; set; }


    /// <summary>
    /// <para>日程附件。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("attachments")]
    public UpdateAttachmentData[]? Attachments { get; set; }


    /// <summary>
    /// <para>日程签到设置，为空则不进行日程签到设置。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_check_in")]
    public CalendarEventCheckIn? EventCheckIn { get; set; }

}