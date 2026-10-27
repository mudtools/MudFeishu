// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Docx（2 个） ───────────────────────────

/// <summary>工具接口：docx.get_raw_content（映射 <c>IFeishuTenantV1Docx.GetDocumentRawContentAsync</c>）。</summary>
[AgentTool("docx.get_raw_content",
    Description = "读取飞书文档的纯文本正文；document_id 可来自 wiki.get_node 的 obj_token 或 search.doc_wiki 结果的 token。只读，需 docx:document:readonly。",
    RequiredScopes = ["docx:document:readonly"],
    Source = nameof(IFeishuTenantV1Docx) + "." + nameof(IFeishuTenantV1Docx.GetDocumentRawContentAsync))]
public interface IFeishuTenantDocxRawContentTool
{
    /// <summary>读取文档纯文本正文。</summary>
    /// <returns>正文纯文本，超长截断并标记 truncated。</returns>
    Task<string> GetDocumentRawContentAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx；wiki 文档传 wiki.get_node 返回的 obj_token）", Required = true)] string document_id,
        [ToolParameter("lang", "文档语言（可选，0=中文）")] int? lang = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.get_document_blocks（映射 <c>IFeishuTenantV1Docx.GetDocumentBlocksPageListAsync</c>）。</summary>
[AgentTool("docx.get_document_blocks",
    Description = "分块读取飞书文档结构（block_id/block_type/文本），表格/代码块等结构化场景使用；document_id 可来自 wiki.get_node 的 obj_token。粒度=全篇扁平块：单块详情用 docx.get_block，某块的直接子块用 docx.list_block_children。只读，需 docx:document:readonly。",
    RequiredScopes = ["docx:document:readonly"],
    Source = nameof(IFeishuTenantV1Docx) + "." + nameof(IFeishuTenantV1Docx.GetDocumentBlocksPageListAsync))]
public interface IFeishuTenantDocxDocumentBlocksTool
{
    /// <summary>分块读取文档（500 块/页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> GetDocumentBlocksPageListAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx；wiki 文档传 wiki.get_node 返回的 obj_token）", Required = true)] string document_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}
