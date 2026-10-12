// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Helpdesk;

/// <summary>
/// 服务台工单创建
/// <para>监听服务台的工单创建事件。</para>
/// <para>事件类型:helpdesk.ticket.created_v1</para>
/// <para>使用时请继承：<see cref="HelpdeskTicketCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/helpdesk-v1/ticket-management/ticket/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HelpdeskTicketCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Helpdesk")]
public class HelpdeskTicketCreatedResult : IEventResult
{
    /// <summary>
    /// <para>工单 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ticket_id")]
    public string? TicketId { get; set; }

    /// <summary>
    /// <para>服务台 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("helpdesk_id")]
    public string? HelpdeskId { get; set; }

    /// <summary>
    /// <para>工单创建者</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("guest")]
    public HelpdeskTicketUser? Guest { get; set; }

    /// <summary>
    /// <para>工单阶段。枚举值有：</para>
    /// <para>- 1：机器人</para>
    /// <para>- 2：人工</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("stage")]
    public int? Stage { get; set; }

    /// <summary>
    /// <para>工单状态。枚举值有：</para>
    /// <para>- 1：已创建</para>
    /// <para>- 2：处理中</para>
    /// <para>- 3：排队中</para>
    /// <para>- 4：待处理</para>
    /// <para>- 5：待用户回复</para>
    /// <para>- 50：机器人关闭</para>
    /// <para>- 51：客服关闭</para>
    /// <para>- 52：用户自己关闭</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>工单评分。枚举值有：</para>
    /// <para>- 1：不满意</para>
    /// <para>- 2：一般</para>
    /// <para>- 3：满意</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("score")]
    public int? Score { get; set; }

    /// <summary>
    /// <para>工单创建时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }

    /// <summary>
    /// <para>工单更新时间，无值为 -1</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }

    /// <summary>
    /// <para>工单关闭时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("closed_at")]
    public long? ClosedAt { get; set; }

    /// <summary>
    /// <para>工单渠道。枚举值有：</para>
    /// <para>- 9：Open API</para>
    /// <para>- 2：二维码</para>
    /// <para>- 14：分享</para>
    /// <para>- 13：搜索</para>
    /// <para>- 其他：其他渠道</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("channel")]
    public int? Channel { get; set; }

    /// <summary>
    /// <para>工单是否解决。枚举值有：</para>
    /// <para>- 1：未解决</para>
    /// <para>- 2：已解决</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("solve")]
    public int? Solve { get; set; }

    /// <summary>
    /// <para>自定义字段列表。注意：该事件不返回自定义字段的值，可获取工单 ID 后调用查询指定工单详情接口获取自定义字段值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_fields")]
    public HelpdeskCustomizedFieldDisplayItem[]? CustomizedFields { get; set; }

    /// <summary>
    /// <para>会话 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }
}
