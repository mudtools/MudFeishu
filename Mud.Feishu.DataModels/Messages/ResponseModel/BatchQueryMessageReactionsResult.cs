// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 批量查询消息表情回复响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/im-v1/message-reaction/batch_query"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Messages")]
public class BatchQueryMessageReactionsResult
{
    /// <summary>
    /// <para>成功获取到的表情列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("success_msg_reaction_details")]
    public MessageReactionDetail[]? SuccessMsgReactionDetails { get; set; }

    /// <summary>
    /// <para>每条消息上所有表情的数量统计。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("success_msg_reaction_counts")]
    public MessageReactionCount[]? SuccessMsgReactionCounts { get; set; }

    /// <summary>
    /// <para>未成功获取的消息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("fail_msg_reaction_details")]
    public MessageReactionFailDetail[]? FailMsgReactionDetails { get; set; }
}

/// <summary>
/// 单条消息的表情回复明细
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Messages")]
public class MessageReactionDetail
{
    /// <summary>
    /// <para>消息 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>是否还有更多项。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("has_more")]
    public bool? HasMore { get; set; }

    /// <summary>
    /// <para>分页标记，当 has_more 为 true 时会同时返回新的 page_token。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>表情实体列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("message_reaction_items")]
    public MessageReactionItem[]? MessageReactionItems { get; set; }
}

/// <summary>
/// 消息表情实体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Messages")]
public class MessageReactionItem
{
    /// <summary>
    /// <para>表情回复 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reaction_id")]
    public string? ReactionId { get; set; }

    /// <summary>
    /// <para>表情类型。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reaction_type")]
    public string? ReactionType { get; set; }

    /// <summary>
    /// <para>进行操作的用户 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public string? OperatorId { get; set; }

    /// <summary>
    /// <para>操作时间（秒级时间戳）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("action_time")]
    public string? ActionTime { get; set; }
}

/// <summary>
/// 单条消息上不同表情的数量统计
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Messages")]
public class MessageReactionCount
{
    /// <summary>
    /// <para>消息 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>消息上不同表情的数量。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reaction_count")]
    public MessageReactionCountItem[]? ReactionCount { get; set; }
}

/// <summary>
/// 单个表情的数量
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Messages")]
public class MessageReactionCountItem
{
    /// <summary>
    /// <para>表情类型。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reaction_type")]
    public string? ReactionType { get; set; }

    /// <summary>
    /// <para>该表情的数量。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }
}

/// <summary>
/// 未成功获取表情的消息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Messages")]
public class MessageReactionFailDetail
{
    /// <summary>
    /// <para>消息 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>获取表情失败的原因。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("fail_reason")]
    public string? FailReason { get; set; }
}
