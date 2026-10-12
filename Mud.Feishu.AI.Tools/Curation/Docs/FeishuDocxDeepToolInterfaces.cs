// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Docx;

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Docx 深化（A2，+9 工具） ───────────────────────────
//
// 链路设计（"先读后改"闭环）：
//   get_document_info → list_block_children / get_block → create_block / update_block / create_descendant_blocks
//
// 跨域 Source：docx.get_markdown_content 使用 Drive 接口（IFeishuTenantV1DriveFiles.GetFileContentByFileTokenAsync），
// scope 归 drive:drive:readonly；工具名保持 docx.*（语义面向云文档正文）。

/// <summary>工具接口：docx.get_document_info（映射 <c>IFeishuTenantV1Docx.GetDocumentInfoAsync</c>）。</summary>
[FeishuTool("docx.get_document_info",
    Description = "获取文档基本信息（标题/版本号/文档 ID）。改文档前先确认目标的链路首环。只读，需 docx:document:readonly。",
    RequiredScopes = ["docx:document:readonly"],
    Source = nameof(IFeishuTenantV1Docx) + "." + nameof(IFeishuTenantV1Docx.GetDocumentInfoAsync))]
public interface IFeishuTenantDocxGetDocumentInfoTool
{
    /// <summary>获取文档基本信息。</summary>
    /// <returns>白名单投影后的 JSON 文本（document_id/revision_id/title）。</returns>
    Task<string> GetDocumentInfoAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx）", Required = true)] string document_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.get_markdown_content（跨域 Source：Drive 接口，scope 归 drive:drive:readonly）。</summary>
/// <remarks>
/// ⚠️ <b>跨域 Source</b>：使用 <c>IFeishuTenantV1DriveFiles.GetFileContentByFileTokenAsync</c>（Drive 接口），
/// 传入 <c>content_type=markdown</c> 获取新版文档的 Markdown 正文。工具名保持 <c>docx.*</c>（语义面向云文档正文）。
/// 仅支持新版文档（docx）；旧版文档（doc）不支持。
/// </remarks>
[FeishuTool("docx.get_markdown_content",
    Description = "读取飞书文档的 Markdown 正文（仅支持新版文档 docx）。跨域使用 Drive 接口获取内容，需 drive:drive:readonly。返回的 Markdown 可用于编辑后通过 docx.replace_document 写回。",
    RequiredScopes = ["drive:drive:readonly"],
    Source = nameof(IFeishuTenantV1DriveFiles) + "." + nameof(IFeishuTenantV1DriveFiles.GetFileContentByFileTokenAsync))]
public interface IFeishuTenantDocxGetMarkdownContentTool
{
    /// <summary>读取文档 Markdown 正文。</summary>
    /// <returns>Markdown 纯文本，超长截断并标记 truncated。</returns>
    Task<string> GetMarkdownContentAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx；仅支持新版文档）", Required = true)] string document_id,
        [ToolParameter("lang", "返回语言（可选，默认 zh）")] string? lang = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.get_block（映射 <c>IFeishuTenantV1DocxBlocks.GetBlockInfoAsync</c>）。</summary>
[FeishuTool("docx.get_block",
    Description = "获取文档中指定块的详情（块类型/文本/子块结构）。用于精确定位块后再编辑。只读，需 docx:document:readonly。",
    RequiredScopes = ["docx:document:readonly"],
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.GetBlockInfoAsync))]
public interface IFeishuTenantDocxGetBlockTool
{
    /// <summary>获取单块详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（block_id/block_type/text/children）。</returns>
    Task<string> GetBlockInfoAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx）", Required = true)] string document_id,
        [ToolParameter("block_id", "要查询的块 ID", Required = true)] string block_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.list_block_children（映射 <c>IFeishuTenantV1DocxBlocks.GetChildrenBlocksPageListAsync</c>）。</summary>
[FeishuTool("docx.list_block_children",
    Description = "列出指定块的所有子块（分页）。block_id 缺省为文档根块（等同列出文档顶层块）。用于定位要修改的块。只读，需 docx:document:readonly。",
    RequiredScopes = ["docx:document:readonly"],
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.GetChildrenBlocksPageListAsync))]
public interface IFeishuTenantDocxListBlockChildrenTool
{
    /// <summary>列出子块（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token）。</returns>
    Task<string> ListBlockChildrenAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx）", Required = true)] string document_id,
        [ToolParameter("block_id", "父块 ID（可选，默认文档根块 document_id）")] string? block_id = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.create_block（映射 <c>IFeishuTenantV1DocxBlocks.CreateBlockAsync</c>）。</summary>
/// <remarks>
/// 与 <c>docx.append_blocks</c> 的区别：<c>append_blocks</c> 是在文档根块下追加（<c>parent_block_id</c> 缺省=文档根）；
/// <c>create_block</c> 要求显式指定 <c>block_id</c>（父块），面向"在某个块下插入单个子块"的精确场景。
/// </remarks>
[FeishuTool("docx.create_block",
    Description = "在指定父块下创建单个子块（精确块级编辑）。与 docx.append_blocks 的区别：本工具要求显式指定父块 ID，面向'在某个块下插入单个子块'。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.CreateBlockAsync))]
public interface IFeishuTenantDocxCreateBlockTool
{
    /// <summary>创建单个子块。</summary>
    /// <returns>白名单投影后的 JSON 文本（block_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateBlockAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx）", Required = true)] string document_id,
        [ToolParameter("block_id", "父块 ID（在其下创建子块）", Required = true)] string block_id,
        [ToolParameter("block_type", "块类型（text/heading1~9/bullet/ordered/code/quote/todo/divider）", Required = true, EnumType = typeof(global::Mud.Feishu.DataModels.Docx.BlockTypes))] string block_type,
        [ToolParameter("text", "块文本内容（divider 忽略）")] string? text = null,
        [ToolParameter("index", "插入位置（可选，默认追加到末尾）")] int? index = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同 client_token 在 24 小时内至多成功创建一次")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.create_descendant_blocks（映射 <c>IFeishuTenantV1DocxBlocks.CreateDescendantBlockAsync</c>）。</summary>
/// <remarks>
/// 用于创建<b>结构化子树</b>（块树 + 索引）。深层 JSON 结构由绑定层校验 <c>children_id</c> 引用完整性，非法引用 → <c>invalid_args</c>。
/// </remarks>
[FeishuTool("docx.create_descendant_blocks",
    Description = "在指定块下创建结构化子树（带父子关系的多层块）。descendants 为块数组（JSON），children_id 指定顶层子块顺序。⚠️ children_id 中的临时 ID 必须在 descendants 中有对应块，否则返回 invalid_args。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.CreateDescendantBlockAsync))]
public interface IFeishuTenantDocxCreateDescendantBlocksTool
{
    /// <summary>创建结构化子树。</summary>
    /// <returns>白名单投影后的 JSON 文本（block_id_relations/document_revision_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateDescendantBlocksAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx）", Required = true)] string document_id,
        [ToolParameter("block_id", "父块 ID（在其下创建子树）", Required = true)] string block_id,
        [ToolParameter("descendants", "子孙块列表（JSON 数组字符串）。每项形如 {\"block_type\":\"text\",\"text\":\"内容\",\"block_id\":\"temp-1\"}，block_id 为临时 ID 供 children_id 引用", Required = true)] string descendants,
        [ToolParameter("children_id", "顶层子块的临时 ID 列表（JSON 数组字符串，如 [\"temp-1\",\"temp-2\"]），指定插入顺序", Required = true)] string children_id,
        [ToolParameter("index", "插入位置（可选，默认追加到末尾）")] int? index = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同 client_token 在 24 小时内至多成功创建一次")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.update_block（映射 <c>IFeishuTenantV1DocxBlocks.UpdateBlockAsync</c>）。</summary>
/// <remarks>
/// PATCH 语义天然幂等。仅支持文本块（<c>update_text_elements</c> 面），不支持表格结构。
/// 与 <c>docx.update_blocks</c>（批量）的区别：本工具改<b>单个块</b>，面向精确块级编辑。
/// 与 <c>docx.replace_document</c> 的边界：改少量块用 update_block，整篇重写用 replace_document。
/// </remarks>
[FeishuTool("docx.update_block",
    Description = "更新文档中单个块的文本内容（PATCH 语义天然幂等）。仅支持文本块，不支持表格结构。改少量块用本工具，整篇重写用 docx.replace_document。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DocxBlocks) + "." + nameof(IFeishuTenantV1DocxBlocks.UpdateBlockAsync))]
public interface IFeishuTenantDocxUpdateBlockTool
{
    /// <summary>更新单个块的文本。</summary>
    /// <returns>白名单投影后的 JSON 文本（block_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateBlockAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx）", Required = true)] string document_id,
        [ToolParameter("block_id", "要更新的块 ID", Required = true)] string block_id,
        [ToolParameter("text", "新的文本内容（将替换块的文本元素）", Required = true)] string text,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同 client_token 在 24 小时内至多成功一次")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.get_chat_announcement（映射 <c>IFeishuTenantV1DocxAnnouncement.GetChatAnnouncementBlocksPageListAsync</c>）。</summary>
[FeishuTool("docx.get_chat_announcement",
    Description = "读取群公告的块内容（分页）。群公告以文档形式存储，chat_id 来自 im.list_chats。只读，需 docx:document:readonly。",
    RequiredScopes = ["docx:document:readonly"],
    Source = nameof(IFeishuTenantV1DocxAnnouncement) + "." + nameof(IFeishuTenantV1DocxAnnouncement.GetChatAnnouncementBlocksPageListAsync))]
public interface IFeishuTenantDocxGetChatAnnouncementTool
{
    /// <summary>读取群公告块内容（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token）。</returns>
    Task<string> GetChatAnnouncementBlocksPageListAsync(
        [ToolParameter("chat_id", "群 ID（来自 im.list_chats）", Required = true)] string chat_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：docx.set_chat_announcement（映射 <c>IFeishuTenantV1DocxAnnouncement.BatchUpdateChatAnnouncementBlocksAsync</c>）。</summary>
/// <remarks>
/// ⚠️ 公告是<b>全员可见</b>的广播面 → 走"敏感工具纪律"第 1/4 条：
/// ① 描述中声明后果（结果全员可见）；④ 默认空名单不启用。
/// </remarks>
[FeishuTool("docx.set_chat_announcement",
    Description = "批量更新群公告中块的文本内容。⚠️ 群公告是全员可见的广播面，更新后所有群成员将看到新内容。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 docx:document。",
    RequiredScopes = ["docx:document"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1DocxAnnouncement) + "." + nameof(IFeishuTenantV1DocxAnnouncement.BatchUpdateChatAnnouncementBlocksAsync))]
public interface IFeishuTenantDocxSetChatAnnouncementTool
{
    /// <summary>批量更新群公告块文本。</summary>
    /// <returns>结构化文本（updated=成功条数）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> BatchUpdateChatAnnouncementBlocksAsync(
        [ToolParameter("chat_id", "群 ID（来自 im.list_chats）", Required = true)] string chat_id,
        [ToolParameter("blocks", "要更新的块列表（JSON 数组字符串）。每项形如 {\"block_id\":\"doxcnXxx\",\"text\":\"新文本\"}", Required = true)] string blocks,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同 client_token 在 24 小时内至多成功一次")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
