// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Calendar;

/// <summary>
/// 会议室状态信息变更
/// <para>会议室被创建、更新、删除或者被预定时，将会触发此事件。</para>
/// <para>事件类型:meeting_room.meeting_room.status_changed_v1</para>
/// <para>使用时请继承：<see cref="RoomStatusChangedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/calendar-v4/meeting-room-event/event/status_changed</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.RoomStatusChanged, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
public class RoomStatusChangedResult : IEventResult
{
    /// <summary>
    /// <para>会议室名称。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_name")]
    public string? RoomName { get; set; }

    /// <summary>
    /// <para>会议室 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }
}
