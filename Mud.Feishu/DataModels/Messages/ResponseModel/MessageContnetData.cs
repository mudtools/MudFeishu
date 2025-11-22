// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 消息内容数据，继承自基础消息结果，扩展了额外的属性
/// </summary>
public class MessageContentData : BaseMessageResult
{
    /// <summary>
    /// 线程 ID，用于标识消息串
    /// <para>示例值："thread_123456789"</para>
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string? ThreadId { get; set; }

    /// <summary>
    /// 消息中提及的用户列表
    /// </summary>
    [JsonPropertyName("mentions")]
    public List<MessageMention>? Mentions { get; set; }

    /// <summary>
    /// 上层消息 ID
    /// <para>示例值："om_dc13264520392913993dd051dba21dcf"</para>
    /// </summary>
    [JsonPropertyName("upper_message_id")]
    public string? UpperMessageId { get; set; }
}
