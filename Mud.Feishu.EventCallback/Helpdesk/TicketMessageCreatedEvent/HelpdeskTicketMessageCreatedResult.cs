// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Helpdesk;

/// <summary>
/// 服务台工单消息创建
/// <para>监听服务台的工单消息事件。</para>
/// <para>事件类型:helpdesk.ticket_message.created_v1</para>
/// <para>使用时请继承：<see cref="HelpdeskTicketMessageCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket-message/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HelpdeskTicketMessageCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Helpdesk")]
public class HelpdeskTicketMessageCreatedResult : IEventResult
{
    /// <summary>
    /// <para>工单消息 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ticket_message_id")]
    public string? TicketMessageId { get; set; }

    /// <summary>
    /// <para>消息 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>消息类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("msg_type")]
    public string? MsgType { get; set; }

    /// <summary>
    /// <para>位置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("position")]
    public string? Position { get; set; }

    /// <summary>
    /// <para>发送者用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sender_id")]
    public UserIdInfo? SenderId { get; set; }

    /// <summary>
    /// <para>发送者类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sender_type")]
    public int? SenderType { get; set; }

    /// <summary>
    /// <para>文本内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// <para>所属工单信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ticket")]
    public HelpdeskTicketMessageTicket? Ticket { get; set; }

    /// <summary>
    /// <para>事件 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_id")]
    public string? EventId { get; set; }

    /// <summary>
    /// <para>会话 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// <para>消息内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("content")]
    public HelpdeskTicketMessageContent? Content { get; set; }
}
