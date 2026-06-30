// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.VideoConferencing;


/// <summary>
/// 纪要生成
/// <para>当与用户有关联的纪要生成后，将会触发该事件。</para>
/// <para>事件类型:vc.note.generated_v1</para>
/// <para>使用时请继承：<see cref="VcNoteGeneratedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/events/generated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.VcNoteGenerated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
public class VcNoteGeneratedResult : IEventResult
{
    /// <summary>
    /// <para>纪要ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("note_id")]
    public string? NoteId { get; set; }

    /// <summary>
    /// <para>需要推送事件的用户列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `500`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscriber_ids")]
    public UserIdInfo[]? SubscriberIds { get; set; }
}
