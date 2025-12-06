// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Message;

/// <summary>
/// 新增消息表情回复事件处理器
/// <para>应用订阅该事件后，消息被添加表情回复时会触发此事件。</para>
/// <para>事件体包含被添加表情回复的消息 message_id、添加表情回复的操作人 ID、表情类型、添加时间等信息。</para>
/// <para>事件类型:im.message.reaction.created_v1</para>
/// <para>使用时请继承：<see cref="MessageReactionCreatedEventHandler"/></para>
/// <para>文档地址：<see href="https://open.feishu.cn/document/server-docs/im-v1/message-reaction/event/created"/> </para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.MessageReactionCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class MessageReactionCreatedResult : IEventResult
{
    /// <summary>
    /// <para>消息 ID。</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>表情回复的资源类型。</para>

    /// </summary>
    [JsonPropertyName("reaction_type")]
    public Emoji? ReactionType { get; set; }

    /// <summary>
    /// <para>操作人类型。可能值有：</para>
    /// <para>- user：用户，此时 user_id 参数有返回值。</para>
    /// <para>- app：应用，此时 app_id 参数有返回值。</para>    
    /// </summary>
    [JsonPropertyName("operator_type")]
    public string? OperatorType { get; set; }

    /// <summary>
    /// <para>用户 ID。</para>    
    /// </summary>
    [JsonPropertyName("user_id")]
    public UserIdInfo? UserId { get; set; }

    /// <summary>
    /// <para>应用 ID。</para>

    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>添加表情回复的时间戳。单位：ms</para>    
    /// </summary>
    [JsonPropertyName("action_time")]
    public string? ActionTime { get; set; }
}

