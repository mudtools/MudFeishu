// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Helpdesk;

/// <summary>
/// 服务台工单更新
/// <para>监听服务台的工单状态变更事件。注意：更新前的工单信息仅返回发生变更的字段，未变更的字段不会出现。</para>
/// <para>事件类型:helpdesk.ticket.updated_v1</para>
/// <para>使用时请继承：<see cref="HelpdeskTicketUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HelpdeskTicketUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Helpdesk")]
public class HelpdeskTicketUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>更新后的工单信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("object")]
    public HelpdeskTicketEvent? Object { get; set; }

    /// <summary>
    /// <para>更新前的工单信息，仅包含发生变更的字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("old_object")]
    public HelpdeskTicketEventUpdateInfo? OldObject { get; set; }
}
