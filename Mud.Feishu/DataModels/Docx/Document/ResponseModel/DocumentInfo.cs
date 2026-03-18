// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Docx;

/// <summary>
/// <para>新建文档的文档信息</para>
/// </summary>
public class DocumentInfo
{
    /// <summary>
    /// <para>文档的唯一标识。你可以将 `https://sample.feishu.cn/docx/` 与该标识拼接，并将 sample 替换为实际域名，生成文档的 URL 链接。如 `https://sample.feishu.cn/docx/doxbcmEtbFrbbq10nPNu8gabcef`。</para>
    /// <para>必填：否</para>
    /// <para>示例值：doxbcmEtbFrbbq10nPNu8gabcef</para>
    /// <para>最大长度：27</para>
    /// <para>最小长度：27</para>
    /// </summary>
    [JsonPropertyName("document_id")]
    public string? DocumentId { get; set; }

    /// <summary>
    /// <para>文档版本 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("revision_id")]
    public int? RevisionId { get; set; }

    /// <summary>
    /// <para>文档标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：undefined</para>
    /// <para>最大长度：800</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>文档展示设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("display_setting")]
    public DocumentDisplaySetting? DisplaySetting { get; set; }



    /// <summary>
    /// <para>文档封面</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cover")]
    public DocumentCoverInfo? Cover { get; set; }


}