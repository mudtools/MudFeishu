// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 搜索消息请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/im-v1/message/search"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Messages")]
public class SearchMessageRequest
{
    /// <summary>
    /// <para>搜索关键词，长度范围 0 ~ 50 字符。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    /// <para>消息过滤器。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("filter")]
    public MessageSearchFilter? Filter { get; set; }
}

/// <summary>
/// 消息搜索过滤器
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Messages")]
public class MessageSearchFilter
{
    /// <summary>
    /// <para>消息创建者 ID 列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("from_ids")]
    public string[]? FromIds { get; set; }

    /// <summary>
    /// <para>消息所在的会话 ID 列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chat_ids")]
    public string[]? ChatIds { get; set; }

    /// <summary>
    /// <para>开始与结束时间，不需要同时传入，但是 end_time 需要大于 start_time。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("time_range")]
    public MessageSearchTimeRange? TimeRange { get; set; }

    /// <summary>
    /// <para>包含某些附件类型。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("include_attachment_types")]
    public string[]? IncludeAttachmentTypes { get; set; }

    /// <summary>
    /// <para>来源类型（用户消息、机器人消息）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("from_types")]
    public string[]? FromTypes { get; set; }

    /// <summary>
    /// <para>消息中包含 at 的用户 ID（包含 at all）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("at_chatter_ids")]
    public string[]? AtChatterIds { get; set; }

    /// <summary>
    /// <para>会话类型（单聊、群聊），单选。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chat_type")]
    public string? ChatType { get; set; }

    /// <summary>
    /// <para>是否 at 过我，默认否（包含 at all）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_at_me")]
    public bool? IsAtMe { get; set; }

    /// <summary>
    /// <para>过滤来源类型。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("exclude_from_types")]
    public string[]? ExcludeFromTypes { get; set; }
}

/// <summary>
/// 消息搜索时间区间
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Messages")]
public class MessageSearchTimeRange
{
    /// <summary>
    /// <para>开始时间（iso8601，需要小于 end_time）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>结束时间（iso8601，需要大于 start_time）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }
}
