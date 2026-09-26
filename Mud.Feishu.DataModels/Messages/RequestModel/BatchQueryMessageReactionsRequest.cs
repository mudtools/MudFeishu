// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 批量查询消息表情回复请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/im-v1/message-reaction/batch_query"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Messages")]
public class BatchQueryMessageReactionsRequest
{
    /// <summary>
    /// <para>要查询的消息列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("queries")]
    public MessageReactionQuery[]? Queries { get; set; }

    /// <summary>
    /// <para>每个消息最多返回多少个表情。</para>
    /// <para>必填：否</para>
    /// <para>默认值：10</para>
    /// </summary>
    [JsonPropertyName("page_size_per_message")]
    public int? PageSizePerMessage { get; set; }

    /// <summary>
    /// <para>待查询的表情类型，支持的枚举值参考表情文案说明中的 emoji_type 值。</para>
    /// <para>**注意**：该参数为可选参数，不传入该参数时将查询消息内所有的表情回复。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reaction_type")]
    public string? ReactionType { get; set; }
}

/// <summary>
/// 批量查询表情回复时的单条消息查询条件
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Messages")]
public class MessageReactionQuery
{
    /// <summary>
    /// <para>消息 ID。</para>
    /// <para>必填：否</para>
    /// <para>示例值：om_8964d1b4*********2b31383276113</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }
}
