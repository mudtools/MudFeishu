// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.IM;

/// <summary>
/// 用户进入与机器人的会话
/// <para>用户进入与机器人的会话时触发此事件。</para>
/// <para>事件类型:im.chat.access_event.bot_p2p_chat_entered_v1</para>
/// <para>使用时请继承：<see cref="ChatBotP2pChatEnteredEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat-access_event/events/bot_p2p_chat_entered</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ChatBotP2pChatEntered, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "IM")]
public class ChatBotP2pChatEnteredResult : IEventResult
{
    /// <summary>
    /// <para>会话 ID，详情参见[群 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat-id-description)。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// <para>操作者的 ID。用户不同 ID 类型介绍参考[用户身份概述](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public UserIdInfo? OperatorId { get; set; }

    /// <summary>
    /// <para>会话内用户可见的最新消息 ID，可通过[获取指定消息的内容](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/message/get)接口获取消息内容。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("last_message_id")]
    public string? LastMessageId { get; set; }

    /// <summary>
    /// <para>会话内用户可见的最新消息发送时间（毫秒时间戳）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("last_message_create_time")]
    public string? LastMessageCreateTime { get; set; }
}
