// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Calendar;


/// <summary>
/// 日历变更
/// <para>当用户订阅日历变更事件后，如果用户日历列表内发生了日历变动，则会触发该事件。</para>
/// <para>事件类型:calendar.calendar.changed_v4</para>
/// <para>使用时请继承：<see cref="CalendarChangedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/calendar-v4/calendar/events/changed</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CalendarChanged, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class CalendarChangedResult : IEventResult
{
    /// <summary>
    /// <para>需要推送事件的用户列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id_list")]
    public UserIdInfo[]? UserIdList { get; set; }
}
