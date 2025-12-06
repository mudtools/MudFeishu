// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------


// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.IM;

/// <summary>
/// 接收消息事件处理器
/// <para>机器人接收到用户发送的消息后触发此事件。</para>
/// <para>事件类型:im.message.receive_v1</para>
/// <para>使用时请继承：<see cref="MessageReceiveEventHandler"/></para>
/// <para>文档地址：<see href="https://open.feishu.cn/document/server-docs/im-v1/message/events/receive"/></para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.ReceiveMessage, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class MessageReceiveResult : IEventResult
{
    /// <summary>
    /// <para>事件的发送者</para>
    /// </summary>
    [JsonPropertyName("sender")]
    public MessageSender? Sender { get; set; }

    /// <summary>
    /// <para>事件中包含的消息内容</para>
    /// </summary>
    [JsonPropertyName("message")]
    public MessageContent? Message { get; set; }

}
