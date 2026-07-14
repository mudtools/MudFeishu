// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// 搜索云文档请求体
/// </summary>
public class SearchFileObjectRequest
{
    /// <summary>
    /// <para>指定搜索的关键字。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("search_key")]
    public string SearchKey { get; set; } = string.Empty;

    /// <summary>
    /// <para>指定搜索返回的文件数量。取值范围为 [0,50]。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    /// <summary>
    /// <para>指定搜索的偏移量，该参数最小为 0，即不偏移。该参数的值与返回的文件数量之和不得大于或等于 200（即 offset + count &lt; 200）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offset")]
    public int? Offset { get; set; }

    /// <summary>
    /// <para>文件所有者的 Open ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("owner_ids")]
    public string[]? OwnerIds { get; set; }

    /// <summary>
    /// <para>文件所在群的 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chat_ids")]
    public string[]? ChatIds { get; set; }

    /// <summary>
    /// <para>文件类型，支持以下枚举：</para>
    /// <para>- `doc`：文档，包括旧版文档（doc）和新版文档（docx）</para>
    /// <para>- `sheet`：电子表格</para>
    /// <para>- `slides`：幻灯片</para>
    /// <para>- `bitable`：多维表格</para>
    /// <para>- `mindnote`：思维笔记</para>
    /// <para>- `file`：文件</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("docs_types")]
    public string[]? DocsTypes { get; set; }
}
