// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Helpdesk;

/// <summary>
/// 工单消息所属工单信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Helpdesk")]
public class HelpdeskTicketMessageTicket
{
    /// <summary>
    /// <para>工单 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ticket_id")]
    public string? TicketId { get; set; }

    /// <summary>
    /// <para>备注</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("comments")]
    public HelpdeskTicketComments? Comments { get; set; }

    /// <summary>
    /// <para>工单阶段。枚举值有：</para>
    /// <para>- 1：机器人</para>
    /// <para>- 2：人工</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ticket_type")]
    public int? TicketType { get; set; }

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
    /// <para>不满意原因列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dissatisfaction_reason")]
    public string[]? DissatisfactionReason { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_fields")]
    public HelpdeskCustomizedFieldDisplayItem[]? CustomizedFields { get; set; }

    /// <summary>
    /// <para>客服服务时长，即客服最后一次回复时间与客服进入时间的间隔，单位为分钟</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agent_service_duration")]
    public double? AgentServiceDuration { get; set; }

    /// <summary>
    /// <para>客服首次回复时间与客服进入时间的间隔，单位为秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agent_first_response_duration")]
    public long? AgentFirstResponseDuration { get; set; }

    /// <summary>
    /// <para>机器人服务时长，即客服进入时间与工单创建时间的间隔，单位为秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bot_service_duration")]
    public long? BotServiceDuration { get; set; }

    /// <summary>
    /// <para>客服解决时长，即客服进入时间与工单关闭时间的间隔，单位为秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agent_resolution_time")]
    public long? AgentResolutionTime { get; set; }

    /// <summary>
    /// <para>工单实际解决时长，即客服进入时间与工单关闭时间的间隔，单位为秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("actual_processing_time")]
    public long? ActualProcessingTime { get; set; }

    /// <summary>
    /// <para>客服进入时间，单位为毫秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agent_entry_time")]
    public long? AgentEntryTime { get; set; }

    /// <summary>
    /// <para>客服首次回复时间，单位为毫秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agent_first_response_time")]
    public long? AgentFirstResponseTime { get; set; }

    /// <summary>
    /// <para>客服最后一次回复时间，单位为毫秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agent_last_response_time")]
    public long? AgentLastResponseTime { get; set; }

    /// <summary>
    /// <para>负责客服</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agent_owner")]
    public HelpdeskTicketUserInfo? AgentOwner { get; set; }
}
