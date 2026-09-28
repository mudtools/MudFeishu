// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.OkrV2;

/// <summary>
/// OKR v2 富文本段落元素
/// </summary>
public class ContentParagraphElementV2
{
    /// <summary>
    /// <para>段落元素类型：textRun 文本元素、docsLink 文档链接元素、mention @人员元素</para>
    /// <para>示例值：textRun</para>
    /// </summary>
    [JsonPropertyName("paragraph_element_type")]
    public string? ParagraphElementType { get; set; }

    /// <summary>
    /// <para>文本片段，paragraph_element_type 为 textRun 时返回</para>
    /// </summary>
    [JsonPropertyName("text_run")]
    public ContentTextRunV2? TextRun { get; set; }

    /// <summary>
    /// <para>文档引用，paragraph_element_type 为 docsLink 时返回，可根据链接自动识别标题</para>
    /// </summary>
    [JsonPropertyName("docs_link")]
    public ContentDocsLinkV2? DocsLink { get; set; }

    /// <summary>
    /// <para>@人员提及，paragraph_element_type 为 mention 时返回</para>
    /// </summary>
    [JsonPropertyName("mention")]
    public ContentMention? Mention { get; set; }
}
