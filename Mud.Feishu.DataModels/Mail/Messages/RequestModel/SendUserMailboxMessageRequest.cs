// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// 发送邮件请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class SendUserMailboxMessageRequest
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
    /// <para>eml数据</para>
    /// <para>必填：否</para>
    /// <para>示例值：U3ViamVjdDogSGVsbG8hCkZyb206ICJtaWtlIiA8bWlrZUBtaWtlLmNvbT4KTWltZS1WZXJzaW9uOiAxLjAKQ29udGVudC1UeXBlOiBtdWx0aXBhcnQvYWx0ZXJuYXRpdmU7CiBib3VuZGFyeT1iMjhmYTIyNGExZWU2ZDY3ZjE3OTViNGUxZDEwM2Q3MTBlNzM5ZWVmYjFmZjlmOWQ4NWI4M2NlOTRmMTEKRGF0ZTogV2VkLCAyMyBKdWwgMjAyNSAxNTo0NDoxOCArMDgwMApNZXNzYWdlLUlkOiA8bW9ja3V1aWRtZXNzYWdlX2lkQGxhcmsuY29tPgpUbzogImphY2siIDxqYWNrQGphY2suY29tPgoKLS1iMjhmYTIyNGExZWU2ZDY3ZjE3OTViNGUxZDEwM2Q3MTBlNzM5ZWVmYjFmZjlmOWQ4NWI4M2NlOTRmMTEKQ29udGVudC1UcmFuc2Zlci1FbmNvZGluZzogN2JpdApDb250ZW50LVR5cGU6IHRleHQvcGxhaW47IGNoYXJzZXQ9VVRGLTgKCldlbGNvbWUgdG8gTGFyayBtYWlsIQotLWIyOGZhMjI0YTFlZTZkNjdmMTc5NWI0ZTFkMTAzZDcxMGU3MzllZWZiMWZmOWY5ZDg1YjgzY2U5NGYxMQo=</para>
    /// </summary>
    [JsonPropertyName("raw")]
    public string? Raw { get; set; }

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
    /// <para>正文</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxx</para>
    /// </summary>
    [JsonPropertyName("body_html")]
    public string? BodyHtml { get; set; }

    /// <summary>
    /// <para>正文纯文本</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxx</para>
    /// </summary>
    [JsonPropertyName("body_plain_text")]
    public string? BodyPlainText { get; set; }

    /// <summary>
    /// <para>附件</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("attachments")]
    public MailAttachmentInfo[]? Attachments { get; set; }


    /// <summary>
    /// <para>去重键</para>
    /// <para>必填：否</para>
    /// <para>示例值：abc-ddd-eee-fff-ggg</para>
    /// </summary>
    [JsonPropertyName("dedupe_key")]
    public string? DedupeKey { get; set; }

    /// <summary>
    /// <para>EML中发件人信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("head_from")]
    public MailAddress? HeadFrom { get; set; }
}
