// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>创建日程请求体</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class CreateCalendarEventRequest
{
    /// <summary>
    /// <para>日程标题。</para>
    /// <para>**注意**：为确保数据安全，系统会自动检测日程标题内容，当包含 **晋升、绩效、述职、调薪、调级、复议、申诉、校准、答辩** 中任一关键词时，该日程不会生成会议纪要。</para>
    /// <para>必填：否</para>
    /// <para>示例值：日程标题</para>
    /// <para>最大长度：1000</para>
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// <para>日程描述。支持解析Html标签。</para>
    /// <para>**注意**：可以通过Html标签来实现部分富文本格式，但是客户端生成的富文本格式并不是通过Html标签实现，如果通过客户端生成富文本描述后，再通过API更新描述，会导致客户端原来的富文本格式丢失。</para>
    /// <para>必填：否</para>
    /// <para>示例值：日程描述</para>
    /// <para>最大长度：40960</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>更新日程时，是否给日程参与人发送 Bot 通知。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true：发送通知</para>
    /// <para>- false：不发送通知</para>
    /// <para>**默认值**：true</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("need_notification")]
    public bool? NeedNotification { get; set; }

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
    public CalendarsVchatInfo? Vchat { get; set; }

    /// <summary>
    /// <para>日程公开范围，新建日程默认为 `default`。</para>
    /// <para>**注意**：该参数仅在新建日程时，对所有参与人生效。如果后续更新日程修改了该参数值，则仅对当前身份生效。</para>
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
    /// <para>**默认值**：none</para>
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
    /// <para>**注意**：该参数仅在新建日程时，对所有参与人生效。如果后续更新日程时修改了该参数值，则仅对当前身份生效。</para>
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
    /// <para>日程地点，不传值则默认为空。</para>
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
    /// <para>必填：否</para>
    /// <para>示例值：-1</para>
    /// </summary>
    [JsonPropertyName("color")]
    public int? Color { get; set; }

    /// <summary>
    /// <para>日程提醒列表。不传值则默认为空。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reminders")]
    public CalendarReminder[]? Reminders { get; set; }


    /// <summary>
    /// <para>重复日程的重复性规则，规则设置方式参考[rfc5545](https://datatracker.ietf.org/doc/html/rfc5545#section-3.3.10)。</para>
    /// <para>**默认值**：空，表示当前日程不是重复日程。</para>
    /// <para>**注意**：</para>
    /// <para>- COUNT 和</para>
    /// <para>UNTIL 不支持同时出现。</para>
    /// <para>- 预定会议室重复日程长度不得超过两年。</para>
    /// <para>必填：否</para>
    /// <para>示例值：FREQ=DAILY;INTERVAL=1</para>
    /// <para>最大长度：2000</para>
    /// </summary>
    [JsonPropertyName("recurrence")]
    public string? Recurrence { get; set; }

    /// <summary>
    /// <para>日程自定义信息，控制日程详情页的 UI 展示。不传值则默认为空。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("schemas")]
    public CalendarSchema[]? Schemas { get; set; }

    /// <summary>
    /// <para>日程附件。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("attachments")]
    public CalendarAttachment[]? Attachments { get; set; }

    /// <summary>
    /// <para>日程签到设置，为空则不进行日程签到设置。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_check_in")]
    public CalendarEventCheckIn? EventCheckIn { get; set; }
}
