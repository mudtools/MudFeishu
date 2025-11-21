// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 消息结果基类，包含所有消息类型的共同属性
/// </summary>
public abstract class BaseMessageResult
{
    /// <summary>
    /// 消息 ID
    /// <para>示例值："om_dc13264520392913993dd051dba21dcf"</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// 根消息 ID，用于标识消息串的根消息
    /// <para>示例值："om_dc13264520392913993dd051dba21dcf"</para>
    /// </summary>
    [JsonPropertyName("root_id")]
    public string? RootId { get; set; }

    /// <summary>
    /// 父消息 ID，用于标识消息串的父消息
    /// <para>示例值："om_dc13264520392913993dd051dba21dcf"</para>
    /// </summary>
    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    /// <summary>
    /// 消息类型
    /// <para>可选值有：text、post、image、file、audio、media、sticker、interactive、share_chat、share_user、system 等</para>
    /// <para>示例值："text"</para>
    /// </summary>
    [JsonPropertyName("msg_type")]
    public string? MsgType { get; set; }

    /// <summary>
    /// 消息创建时间
    /// <para>示例值："2024-01-01 00:00:00"</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// 消息更新时间
    /// <para>示例值："2024-01-01 00:00:00"</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// 消息是否已删除
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    /// <summary>
    /// 消息是否已更新
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("updated")]
    public bool Updated { get; set; }

    /// <summary>
    /// 会话 ID
    /// <para>示例值："oc_a0553eda9014c201e6969b478895c230"</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// 消息发送者信息
    /// </summary>
    [JsonPropertyName("sender")]
    public MessageSender? Sender { get; set; }

    /// <summary>
    /// 消息体内容
    /// </summary>
    [JsonPropertyName("body")]
    public MessageBody? Body { get; set; }
}