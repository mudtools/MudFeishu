// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>替换后的完整模板内容（全量替换）</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MailTemplate
{
    /// <summary>
    /// <para>模板名称，不超过 100 字符</para>
    /// <para>必填：是</para>
    /// <para>示例值：销售跟进模板</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>邮件主题，不超过 1000 字符</para>
    /// <para>必填：否</para>
    /// <para>示例值：关于本周订单跟进</para>
    /// <para>最大长度：1000</para>
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// <para>模板正文（HTML 或纯文本）。单模板正文大小上限 3 MB（3 \* 1024 \* 1024 字节），超过将返回错误码 1230006 template content size limit exceeded。</para>
    /// <para>必填：否</para>
    /// <para>示例值：&lt;p&gt;Hi ${name},&lt;/p&gt;</para>
    /// <para>最大长度：3145728</para>
    /// </summary>
    [JsonPropertyName("template_content")]
    public string? TemplateContent { get; set; }

    /// <summary>
    /// <para>是否为纯文本模式。`true` 表示模板正文按纯文本渲染，`false` 表示按 HTML 渲染。默认 `false`（HTML 模式）。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("is_plain_text_mode")]
    public bool? IsPlainTextMode { get; set; }

    /// <summary>
    /// <para>默认收件人地址列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("tos")]
    public MailAddress[]? Tos { get; set; }


    /// <summary>
    /// <para>默认抄送地址列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("ccs")]
    public MailAddress[]? Ccs { get; set; }

    /// <summary>
    /// <para>默认密送地址列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：100</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("bccs")]
    public MailAddress[]? Bccs { get; set; }

    /// <summary>
    /// <para>模板附件与内嵌图片列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("attachments")]
    public TemplateAttachment[]? Attachments { get; set; }
}
