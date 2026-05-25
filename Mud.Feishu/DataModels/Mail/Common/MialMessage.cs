// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>会话中的邮件列表</para>
/// </summary>
public class MialMessage
{
    /// <summary>
    /// <para>主题</para>
    /// <para>必填：否</para>
    /// <para>示例值：邮件标题</para>
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// <para>收件人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("to")]
    public MailAddress[]? Tos { get; set; }

    /// <summary>
    /// <para>抄送</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cc")]
    public MailAddress[]? Ccs { get; set; }

    /// <summary>
    /// <para>密送</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bcc")]
    public MailAddress[]? Bccs { get; set; }

    /// <summary>
    /// <para>发件人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("head_from")]
    public MailAddress? HeadFrom { get; set; }

    /// <summary>
    /// <para>正文(base64url)</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxx</para>
    /// </summary>
    [JsonPropertyName("body_html")]
    public string? BodyHtml { get; set; }

    /// <summary>
    /// <para>创建/收/发信时间（毫秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1682377086000</para>
    /// </summary>
    [JsonPropertyName("internal_date")]
    public string? InternalDate { get; set; }

    /// <summary>
    /// <para>邮件状态，1（收信）2（发信）3（草稿）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("message_state")]
    public int? MessageState { get; set; }

    /// <summary>
    /// <para>RFC协议id</para>
    /// <para>必填：否</para>
    /// <para>示例值：ay0azrJDvbs3FJAg@outlook.com</para>
    /// </summary>
    [JsonPropertyName("smtp_message_id")]
    public string? SmtpMessageId { get; set; }

    /// <summary>
    /// <para>邮件id</para>
    /// <para>必填：否</para>
    /// <para>示例值：tfuh9N4WnzU6jdDw=</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>邮件附件列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：501</para>
    /// </summary>
    [JsonPropertyName("attachments")]
    public MailAttachment[]? Attachments { get; set; }


    /// <summary>
    /// <para>正文纯文本(base64url)</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxxx</para>
    /// </summary>
    [JsonPropertyName("body_plain_text")]
    public string? BodyPlainText { get; set; }

    /// <summary>
    /// <para>会话id</para>
    /// <para>必填：否</para>
    /// <para>示例值：tfuh9N4WnzU6jdDw=</para>
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string? ThreadId { get; set; }

    /// <summary>
    /// <para>邮件正文纯文本内容的前100个字符，基于base64url编码，用于快速预览邮件核心内容，无需解码完整正文</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxxx</para>
    /// </summary>
    [JsonPropertyName("body_preview")]
    public string? BodyPreview { get; set; }

    /// <summary>
    /// <para>标签ID</para>
    /// <para>必填：否</para>
    /// <para>最大长度：500</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("label_ids")]
    public string[]? LabelIds { get; set; }

    /// <summary>
    /// <para>文件夹ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：INBOX</para>
    /// </summary>
    [JsonPropertyName("folder_id")]
    public string? FolderId { get; set; }

    /// <summary>
    /// <para>In-Reply-To邮件头</para>
    /// <para>必填：否</para>
    /// <para>示例值：06d20.dbf451a3.808a.475a.acc9.1363dfd20f36@larksuite.com</para>
    /// </summary>
    [JsonPropertyName("in_reply_to")]
    public string? InReplyTo { get; set; }

    /// <summary>
    /// <para>Reply-To邮件头</para>
    /// <para>必填：否</para>
    /// <para>示例值：06d20.dbf451a3.808a.475a.acc9.1363dfd20f36@larksuite.com</para>
    /// </summary>
    [JsonPropertyName("reply_to")]
    public string? ReplyTo { get; set; }

    /// <summary>
    /// <para>邮件优先级</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：无优先级</item>
    /// <item>1：高优先级</item>
    /// <item>3：正常优先级</item>
    /// <item>5：低优先级</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("priority_type")]
    public string? PriorityType { get; set; }

    /// <summary>
    /// <para>安全信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("security_level")]
    public MailSecurityLevel? SecurityLevel { get; set; }


    /// <summary>
    /// <para>References邮件头</para>
    /// <para>必填：否</para>
    /// <para>示例值：&lt;5678.abcd@test.com&gt;\r\n\t&lt;1234.abcd@message-id&gt;</para>
    /// </summary>
    [JsonPropertyName("references")]
    public string? References { get; set; }
}