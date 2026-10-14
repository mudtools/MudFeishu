// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Search（1 个） ───────────────────────────

/// <summary>工具接口：search.doc_wiki（映射 <c>IFeishuTenantV2SearchDocWiki.SearchDocWikiAsync</c>）。</summary>
[AgentTool("search.doc_wiki",
    Description = "云文档与知识库全文搜索，返回标题/摘要/URL；结果的 token 可传给 docx.get_raw_content、url 对应节点可传给 wiki.get_node。query 上限 30 字符。只读，需 search:docs:readonly。",
    RequiredScopes = ["search:docs:readonly"],
    Source = nameof(IFeishuTenantV2SearchDocWiki) + "." + nameof(IFeishuTenantV2SearchDocWiki.SearchDocWikiAsync))]
public interface IFeishuTenantSearchDocWikiTool
{
    /// <summary>云文档/知识库搜索（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（title/url/owner/doc_type），超长截断并标记 truncated。</returns>
    Task<string> SearchDocWikiAsync(
        [ToolParameter("query", "搜索关键词（≤30 字符）", Required = true)] string query,
        [ToolParameter("search_in", "搜索范围（可选：doc=云文档 / wiki=知识库 / both=两者，默认 both）")] string? search_in = null,
        [ToolParameter("folder_tokens", "限定云文档所在文件夹 token（可选，字符串数组）")] string[]? folder_tokens = null,
        [ToolParameter("space_ids", "限定知识空间 ID（可选，字符串数组）")] string[]? space_ids = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}
