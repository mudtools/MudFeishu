// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Message;

/// <summary>
/// 消息内容
/// </summary>
public class MessageContent
{
    /// <summary>
    /// <para>消息 ID。由系统生成的唯一 ID 标识，基于该 ID 可以[回复消息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/message/reply) 或其他管理消息的操作。</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>根消息 ID，仅在回复消息场景会有返回值。了解 root_id 可参见[消息管理概述](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/message/intro)。</para>
    /// </summary>
    [JsonPropertyName("root_id")]
    public string? RootId { get; set; }

    /// <summary>
    /// <para>父消息 ID，仅在回复消息场景会有返回值。了解 parent_id 可参见[消息管理概述](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/message/intro)。</para>
    /// </summary>
    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    /// <summary>
    /// <para>消息发送时间（毫秒）</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>消息更新时间（毫秒）</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>消息所在的群组 ID。</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// <para>消息所属的话题 ID（不返回说明该消息非话题消息）。</para>
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string? ThreadId { get; set; }

    /// <summary>
    /// <para>消息所在的群组类型</para>
    /// <para>**可能值有**：</para>
    /// <para>- `p2p`：单聊</para>
    /// <para>- `group`： 群组</para>
    /// </summary>
    [JsonPropertyName("chat_type")]
    public string? ChatType { get; set; }

    /// <summary>
    /// <para>消息类型，可能返回的消息类型以及详细介绍，参见[接收消息内容](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/im-v1/message/events/message_content)。</para>
    /// </summary>
    [JsonPropertyName("message_type")]
    public string? MessageType { get; set; }

    /// <summary>
    /// <para>消息内容，JSON 结构序列化后的字符串，不同消息类型（`msg_type`）对应不同内容。</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>被提及用户的信息</para>
    /// </summary>
    [JsonPropertyName("mentions")]
    public MentionUser[]? Mentions { get; set; }

    /// <summary>
    /// <para>用户代理数据，仅在接收事件的机器人具备==获取客户端用户代理信息（im:user_agent:read）==权限时返回</para>
    /// </summary>
    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; set; }
}
