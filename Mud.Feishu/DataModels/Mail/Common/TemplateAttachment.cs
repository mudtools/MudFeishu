// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>模板附件与内嵌图片列表</para>
/// </summary>
public class TemplateAttachment
{
    /// <summary>
    /// <para>附件文件名</para>
    /// <para>必填：否</para>
    /// <para>示例值：plan.xlsx</para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    /// <summary>
    /// <para>附件 id（Drive file_key，用于引用 Drive medias 上传接口返回的 file_key）</para>
    /// <para>必填：否</para>
    /// <para>示例值：boxcnrHpsg1QDqXPrJXWPwbqsKh</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>附件类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>最大值：2</para>
    /// <para>最小值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：普通附件</item>
    /// <item>2：超大附件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("attachment_type")]
    public int? AttachmentType { get; set; }

    /// <summary>
    /// <para>是否为内联图片，true 表示是内联图片</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("is_inline")]
    public bool? IsInline { get; set; }

    /// <summary>
    /// <para>内容 ID，HTML 中通过 cid: 协议引用该图片</para>
    /// <para>必填：否</para>
    /// <para>示例值：image1@example.com</para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("cid")]
    public string? Cid { get; set; }
}
