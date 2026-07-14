// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;


/// <summary>
/// <para>工单详情</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class Ticket
{
    /// <summary>
    /// <para>工单ID</para>
    /// <para>[可以从工单列表里面取](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/list)</para>
    /// <para>[也可以订阅工单创建事件获取](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket/events/created)</para>
    /// <para>必填：是</para>
    /// <para>示例值：6626871355780366331</para>
    /// </summary>
    [JsonPropertyName("ticket_id")]
    public string TicketId { get; set; } = string.Empty;

    /// <summary>
    /// <para>服务台ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6626871355780366330</para>
    /// </summary>
    [JsonPropertyName("helpdesk_id")]
    public string? HelpdeskId { get; set; }

    /// <summary>
    /// <para>工单创建用户</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("guest")]
    public TicketUser? Guest { get; set; }


    /// <summary>
    /// <para>备注</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("comments")]
    public TicketComments? Comments { get; set; }


    /// <summary>
    /// <para>工单阶段：1. 机器人 2. 人工</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("ticket_type")]
    public int? TicketType { get; set; }

    /// <summary>
    /// <para>工单状态，1：已创建 2: 处理中 3: 排队中 4：待定 5：待用户响应 50: 被机器人关闭 51: 被客服关闭 52: 用户自己关闭</para>
    /// <para>必填：否</para>
    /// <para>示例值：50</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>工单评分，1：不满意，2:一般，3:满意</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("score")]
    public int? Score { get; set; }

    /// <summary>
    /// <para>工单创建时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1616920429000</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }

    /// <summary>
    /// <para>工单更新时间，没有值时为-1</para>
    /// <para>必填：否</para>
    /// <para>示例值：1616920429000</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }

    /// <summary>
    /// <para>工单结束时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1616920429000</para>
    /// </summary>
    [JsonPropertyName("closed_at")]
    public long? ClosedAt { get; set; }

    /// <summary>
    /// <para>不满意原因</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dissatisfaction_reason")]
    public I18nName? DissatisfactionReason { get; set; }


    /// <summary>
    /// <para>工单客服</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("agents")]
    public TicketUser[]? Agents { get; set; }

    /// <summary>
    /// <para>工单渠道，描述：</para>
    /// <para>9：Open API 2：二维码 14：分享 13：搜索 其他数字：其他渠道</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("channel")]
    public int? Channel { get; set; }

    /// <summary>
    /// <para>工单是否解决 1:没解决 2:已解决</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("solve")]
    public int? Solve { get; set; }

    /// <summary>
    /// <para>关单用户ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("closed_by")]
    public TicketUser? ClosedBy { get; set; }

    /// <summary>
    /// <para>工单协作者</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("collaborators")]
    public TicketUser[]? Collaborators { get; set; }

    /// <summary>
    /// <para>自定义字段列表，没有值时不设置</para>
    /// <para>下拉菜单的value对应工单字段里面的children.display_name</para>
    /// <para>[获取全部工单自定义字段](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket_customized_field/list-ticket-customized-fields)</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("customized_fields")]
    public CustomizedFieldDisplayItem[]? CustomizedFields { get; set; }


    /// <summary>
    /// <para>客服服务时长，客服最后一次回复时间距离客服进入时间间隔，单位分钟</para>
    /// <para>必填：否</para>
    /// <para>示例值：42624.95</para>
    /// </summary>
    [JsonPropertyName("agent_service_duration")]
    public float? AgentServiceDuration { get; set; }

    /// <summary>
    /// <para>客服首次回复时间距离客服进入时间的间隔(秒)</para>
    /// <para>必填：否</para>
    /// <para>示例值：123869</para>
    /// </summary>
    [JsonPropertyName("agent_first_response_duration")]
    public int? AgentFirstResponseDuration { get; set; }

    /// <summary>
    /// <para>机器人服务时间：客服进入时间距离工单创建时间的间隔，单位秒</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("bot_service_duration")]
    public int? BotServiceDuration { get; set; }

    /// <summary>
    /// <para>客服解决时长，从首位客服接入服务到工单关闭的用时，单位秒</para>
    /// <para>必填：否</para>
    /// <para>示例值：66</para>
    /// </summary>
    [JsonPropertyName("agent_resolution_time")]
    public int? AgentResolutionTime { get; set; }

    /// <summary>
    /// <para>工单实际处理时长，处理时长=解决时长-工单待定时长（将工单状态修改为待定后的时间），单位秒</para>
    /// <para>必填：否</para>
    /// <para>示例值：68</para>
    /// </summary>
    [JsonPropertyName("actual_processing_time")]
    public int? ActualProcessingTime { get; set; }

    /// <summary>
    /// <para>客服进入时间，单位毫秒</para>
    /// <para>必填：否</para>
    /// <para>示例值：1636444596000</para>
    /// </summary>
    [JsonPropertyName("agent_entry_time")]
    public long? AgentEntryTime { get; set; }

    /// <summary>
    /// <para>客服首次回复时间，单位毫秒</para>
    /// <para>必填：否</para>
    /// <para>示例值：1636444696000</para>
    /// </summary>
    [JsonPropertyName("agent_first_response_time")]
    public long? AgentFirstResponseTime { get; set; }

    /// <summary>
    /// <para>客服最后回复时间，单位毫秒</para>
    /// <para>必填：否</para>
    /// <para>示例值：1636444796000</para>
    /// </summary>
    [JsonPropertyName("agent_last_response_time")]
    public long? AgentLastResponseTime { get; set; }

    /// <summary>
    /// <para>主责客服</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agent_owner")]
    public TicketUser? AgentOwner { get; set; }

    /// <summary>
    /// <para>工单标签（仅工单含有工单标签会返回）</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10000</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("tags")]
    public TicketTag[]? Tags { get; set; }


}
