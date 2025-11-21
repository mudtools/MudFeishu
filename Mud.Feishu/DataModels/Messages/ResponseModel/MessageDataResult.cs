// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 消息数据结果类，表示从飞书获取的消息相关信息
/// </summary>
public class MessageDataResult : BaseMessageResult
{
    /// <summary>
    /// 线程ID，用于标识消息所属的线程
    /// <para>示例值："thread_123456"</para>
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string? ThreadId { get; set; }

    /// <summary>
    /// 消息中提及的用户列表
    /// <para>示例值：[{"user_id":"ou_7d8a6e6df7621556ce0d21922b676706ccs","name":"张三"}]</para>
    /// </summary>
    [JsonPropertyName("mentions")]
    public List<MessageMention> Mentions { get; set; } = [];

    /// <summary>
    /// 上层消息ID
    /// <para>示例值："om_dc13264520392913993dd051dba21dcf"</para>
    /// </summary>
    [JsonPropertyName("upper_message_id")]
    public string? UpperMessageId { get; set; }
}