// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Mail;


/// <summary>
/// 收信通知
/// <para>你需要在应用中配置事件订阅，这样才可以在事件触发时接收到事件数据。</para>
/// <para>事件类型:mail.user_mailbox.event.message_received_v1</para>
/// <para>使用时请继承：<see cref="MessageReceivedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/mail-v1/user_mailbox-event/events/message_received</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.MessageReceived, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MessageReceivedResult : IEventResult
{
    /// <summary>
    /// <para>收信的邮箱</para>
    /// <para>**字段权限要求**：</para>
    /// <para>- mail:user_mailbox.event.mail_address:read : 获取事件中的邮箱地址字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mail_address")]
    public string? MailAddress { get; set; }

    /// <summary>
    /// <para>邮件 id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>收到邮件的邮箱类型</para>
    /// <para>**可选值有**：</para>
    /// <para>1:个人邮箱,2:公共邮箱</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`1` ～ `2`</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：个人邮箱</item>
    /// <item>2：公共邮箱</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("mailbox_type")]
    public int? MailboxType { get; set; }

    /// <summary>
    /// <para>订阅者</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscriber")]
    public MailSubscriber? Subscriber { get; set; }

}


/// <summary>
/// 订阅者
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MailSubscriber
{
    /// <summary>
    /// <para>收到邮件的用户 id 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public UserIdInfo[]? UserIds { get; set; }
}
