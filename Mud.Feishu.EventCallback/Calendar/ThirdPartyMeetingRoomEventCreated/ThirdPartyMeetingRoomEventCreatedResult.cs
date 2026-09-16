// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Calendar;

/// <summary>
/// 第三方会议室日程变动
/// <para>当添加了第三方会议室的日程发生变动时（创建/更新/删除）触发此事件，其中更新日程时，仅当更新日程时间后触发此事件。</para>
/// <para>事件类型:third_party_meeting_room_event_created</para>
/// <para>使用时请继承：<see cref="ThirdPartyMeetingRoomEventCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/calendar-v4/meeting-room-event/event/third-room-event-changes</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ThirdPartyMeetingRoomEventCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class ThirdPartyMeetingRoomEventCreatedResult : IEventResult
{
    /// <summary>
    /// <para>应用 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>租户 Key</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }

    /// <summary>
    /// <para>事件类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>事件发生时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_time")]
    public string? EventTime { get; set; }

    /// <summary>
    /// <para>日程的唯一标识</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    /// <summary>
    /// <para>重复日程的例外日程的唯一标识，时间戳格式。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("original_time")]
    public int? OriginalTime { get; set; }

    /// <summary>
    /// <para>日程 ID，格式为 `{Uid}_{Original time}`，`{Uid}` 是日程的唯一标识，`{Original time}` 是日程实例原始时间，非重复性日程和重复性日程取值为 0，重复性日程的例外日程取值为具体时间戳。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_id")]
    public string? EventId { get; set; }

    /// <summary>
    /// <para>日程开始时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start")]
    public EventTime? Start { get; set; }

    /// <summary>
    /// <para>日程结束时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end")]
    public EventTime? End { get; set; }


    /// <summary>
    /// <para>日程关联的会议室</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_rooms")]
    public string[]? MeetingRooms { get; set; }

    /// <summary>
    /// <para>会议室 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>日程的组织者</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("organizer")]
    public UserIdInfo? Organizer { get; set; }
}
