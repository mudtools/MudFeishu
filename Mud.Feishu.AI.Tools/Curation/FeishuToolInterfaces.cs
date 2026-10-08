// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// 只读工具接口集：工具名/描述/scope = 人的策展声明；Source = 该能力落地的 SDK 客户端接口与方法。
// </summary>
// <remarks>
// <para>
// 只声明模型可见 Schema（工具名/描述/扁平参数）；强类型接口调用、参数映射、
// <c>FeishuApiResult</c> 解包与结果白名单投影由执行链（<c>FeishuToolBinding</c> + 分域执行器）承担。
// 复杂请求体（<c>QueryRecordsRequest</c>/<c>SearchDocWikiRequest</c>）一律由绑定层构造，
// 模型只见标量/标量数组（§3.3.1 原则 2）。
// </para>
// <para>
// <c>page_size</c>/<c>sort_type</c>/<c>container_id_type</c>/<c>user_id_type</c> 等运维性参数
// 不进 Schema（绑定层补齐/钳制）。<b>page_token 保留在 Schema</b>：它是多页追问的唯一手段，
// 且注入「自动翻页」会让单次工具调用把 N 页结果塞进上下文，与 <c>MaxToolResultLength</c> 截断策略冲突
// （见方案 §6 AT-B09 的处置修订）。
// </para>
// <para>
// <b>Source 挂钩</b>：源生成器据此在编译期交叉校验并派生 HTTP 路由、风险分级与返回形状；
// 写错即 MUDFT019 构建失败。知识库检索工具（<c>knowledge.search</c>）绑定的是
// <c>IRetriever</c> 门面而非 SDK 接口，故不声明 Source。
// </para>
// </remarks>

// ─────────────────────────── Bitable（4 个） ───────────────────────────

/// <summary>工具接口：bitable.list_tables（映射 <c>IFeishuTenantV1BitableAppTable.GetAppTablePageListAsync</c>）。</summary>
[FeishuTool("bitable.list_tables",
    Description = "列出多维表格中的全部数据表，返回 table_id/name/revision；先于 bitable.list_fields、bitable.query_records 使用。只读，需 bitable:app:readonly。",
    RequiredScopes = ["bitable:app:readonly"],
    Source = nameof(IFeishuTenantV1BitableAppTable) + "." + nameof(IFeishuTenantV1BitableAppTable.GetAppTablePageListAsync))]
public interface IFeishuTenantBitableListTablesTool
{
    /// <summary>列出数据表（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> ListTablesPageListAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.list_fields（映射 <c>IFeishuTenantV1BitableField.GetFieldsPageListAsync</c>）。</summary>
[FeishuTool("bitable.list_fields",
    Description = "列出数据表的字段定义（field_id/name/type），查询前先了解字段结构，配合 bitable.query_records 的 field_names/filter 使用。只读，需 bitable:app:readonly。",
    RequiredScopes = ["bitable:app:readonly"],
    Source = nameof(IFeishuTenantV1BitableField) + "." + nameof(IFeishuTenantV1BitableField.GetFieldsPageListAsync))]
public interface IFeishuTenantBitableListFieldsTool
{
    /// <summary>列出字段定义（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本，超长截断并标记 truncated。</returns>
    Task<string> ListFieldsPageListAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx，来自 bitable.list_tables）", Required = true)] string table_id,
        [ToolParameter("view_id", "视图 ID（可选）")] string? view_id = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.query_records（映射 <c>IFeishuTenantV1BitableRecord.QueryRecordsPageListAsync</c>）。</summary>
[FeishuTool("bitable.query_records",
    Description = "按条件查询多维表格记录（先经 bitable.list_tables 获取 table_id，经 bitable.list_fields 了解字段）；filter 为简化筛选式，如 status = \"done\" and owner contains 张三。只读，需 bitable:app:readonly。",
    RequiredScopes = ["bitable:app:readonly"],
    Source = nameof(IFeishuTenantV1BitableRecord) + "." + nameof(IFeishuTenantV1BitableRecord.QueryRecordsPageListAsync))]
public interface IFeishuTenantBitableQueryRecordsTool
{
    /// <summary>查询记录（分页；filter 简化文法由绑定层解析为官方过滤结构）。</summary>
    /// <returns>白名单投影后的 JSON 文本（record_id/fields），超长截断并标记 truncated。</returns>
    Task<string> QueryRecordsPageListAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx）", Required = true)] string table_id,
        [ToolParameter("view_id", "视图 ID（可选）")] string? view_id = null,
        [ToolParameter("field_names", "只返回这些字段（可选，字符串数组；缺省返回全部字段）")] string[]? field_names = null,
        [ToolParameter("filter", "简化筛选式（可选）：字段 = 值 或 字段 contains 值，and 连接，最多 5 个子句，如：status = \"done\" and owner contains 张三")] string? filter = null,
        [ToolParameter("sort", "排序子句（可选，字符串数组，最多 3 个）：形如 字段:asc 或 字段:desc，如 [\"status:desc\", \"name:asc\"]")] string[]? sort = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：bitable.get_records_by_ids（映射 <c>IFeishuTenantV1BitableRecord.GetRecordsAsync</c>）。</summary>
[FeishuTool("bitable.get_records_by_ids",
    Description = "按 record_id 批量获取多维表格记录（最多 100 条）——bitable.query_records 翻页后的精取链。只读，需 bitable:app:readonly。",
    RequiredScopes = ["bitable:app:readonly"],
    Source = nameof(IFeishuTenantV1BitableRecord) + "." + nameof(IFeishuTenantV1BitableRecord.GetRecordsAsync))]
public interface IFeishuTenantBitableRecordsByIdsTool
{
    /// <summary>按 ID 批量取记录。</summary>
    /// <returns>白名单投影后的 JSON 文本（record_id/fields/absent_record_ids），超长截断并标记 truncated。</returns>
    Task<string> GetRecordsAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx）", Required = true)] string table_id,
        [ToolParameter("record_ids", "记录 ID 数组（形如 recXxx，最多 100 条）", Required = true)] string[] record_ids,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：bitable.list_views（映射 <c>IFeishuTenantV1BitableView.GetViewsPageListAsync</c>）。
/// </summary>
/// <remarks>
/// R5 / F-11：补齐"**先看视图、再取记录**"的链路首环。此前模型的唯一路径是
/// <c>bitable.query_records</c>（面向整表），无法按视图（筛选/分组后的视角）取数。
/// </remarks>
[FeishuTool("bitable.list_views",
    Description = "列出数据表下的全部视图（名称/类型/可见范围）。取记录前先用它确定 view_id，再传给 bitable.query_records 按视图取数。只读，需 bitable:app:readonly。",
    RequiredScopes = ["bitable:app:readonly"],
    Source = nameof(IFeishuTenantV1BitableView) + "." + nameof(IFeishuTenantV1BitableView.GetViewsPageListAsync))]
public interface IFeishuTenantBitableListViewsTool
{
    /// <summary>列出视图。</summary>
    /// <returns>白名单投影后的 JSON 文本（views: view_id/view_name/view_type + total）。</returns>
    Task<string> ListViewsAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx）", Required = true)] string table_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：bitable.get_view（映射 <c>IFeishuTenantV1BitableView.GetViewAsync</c>）。
/// </summary>
[FeishuTool("bitable.get_view",
    Description = "按 view_id 获取单个视图的详情（名称/类型/可见范围）。确认某个视图的具体配置时使用。只读，需 bitable:app:readonly。",
    RequiredScopes = ["bitable:app:readonly"],
    Source = nameof(IFeishuTenantV1BitableView) + "." + nameof(IFeishuTenantV1BitableView.GetViewAsync))]
public interface IFeishuTenantBitableGetViewTool
{
    /// <summary>获取视图详情。</summary>
    /// <returns>白名单投影后的 JSON 文本（view_id/view_name/view_type）。</returns>
    Task<string> GetViewAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx）", Required = true)] string table_id,
        [ToolParameter("view_id", "视图 ID（形如 veiwXxx，可由 bitable.list_views 获得）", Required = true)] string view_id,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Docx（2 个） ───────────────────────────

/// <summary>工具接口：docx.get_raw_content（映射 <c>IFeishuTenantV1Docx.GetDocumentRawContentAsync</c>）。</summary>
[FeishuTool("docx.get_raw_content",
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
[FeishuTool("docx.get_document_blocks",
    Description = "分块读取飞书文档结构（block_id/block_type/文本），表格/代码块等结构化场景使用；document_id 可来自 wiki.get_node 的 obj_token。只读，需 docx:document:readonly。",
    RequiredScopes = ["docx:document:readonly"],
    Source = nameof(IFeishuTenantV1Docx) + "." + nameof(IFeishuTenantV1Docx.GetDocumentBlocksPageListAsync))]
public interface IFeishuTenantDocxDocumentBlocksTool
{
    /// <summary>分块读取文档（500 块/页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> GetDocumentBlocksPageListAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx；wiki 文档传 wiki.get_node 返回的 obj_token）", Required = true)] string document_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Wiki（2 个） ───────────────────────────

/// <summary>工具接口：wiki.get_node（映射 <c>IFeishuTenantV2WikiNodes.GetNodeSpaceInfoAsync</c>）。</summary>
[FeishuTool("wiki.get_node",
    Description = "解析知识库节点信息（node_token/title/obj_type/obj_token）；obj_token 可传给 docx.get_raw_content 读取正文。只读，需 wiki:wiki:readonly。",
    RequiredScopes = ["wiki:wiki:readonly"],
    Source = nameof(IFeishuTenantV2WikiNodes) + "." + nameof(IFeishuTenantV2WikiNodes.GetNodeSpaceInfoAsync))]
public interface IFeishuTenantWikiGetNodeTool
{
    /// <summary>获取节点信息（单对象）。</summary>
    /// <returns>白名单投影后的 JSON 文本（node 对象）。</returns>
    Task<string> GetNodeSpaceInfoAsync(
        [ToolParameter("token", "知识库节点 token（node_token，形如 wikcnXxx）", Required = true)] string token,
        [ToolParameter("obj_type", "对象类型（可选，默认 wiki）")] string? obj_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：wiki.list_nodes（映射 <c>IFeishuTenantV2WikiNodes.GetSpaceNodesPageListAsync</c>）。</summary>
[FeishuTool("wiki.list_nodes",
    Description = "列出知识空间（或某父节点下）的子节点列表；node_token 可传给 wiki.get_node 解析详情。只读，需 wiki:wiki:readonly。",
    RequiredScopes = ["wiki:wiki:readonly"],
    Source = nameof(IFeishuTenantV2WikiNodes) + "." + nameof(IFeishuTenantV2WikiNodes.GetSpaceNodesPageListAsync))]
public interface IFeishuTenantWikiListNodesTool
{
    /// <summary>列出子节点（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> GetSpaceNodesPageListAsync(
        [ToolParameter("space_id", "知识空间 ID（形如 7xxx）", Required = true)] string space_id,
        [ToolParameter("parent_node_token", "父节点 token（可选；缺省列出空间顶层节点）")] string? parent_node_token = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Wiki 写（R5 / F-11） ───────────────────────────

/// <summary>
/// 工具接口：wiki.create_node（映射 <c>IFeishuTenantV2WikiNodes.CreateSpaceNodeAsync</c>）。
/// </summary>
/// <remarks>
/// R5 / F-11：此前 wiki 域<b>只有读面</b>（get_node / list_nodes），模型能看知识库但不能改。
/// </remarks>
[FeishuTool("wiki.create_node",
    Description = "在知识空间下创建节点（新建一篇 wiki 文档/普通页面）。space_id 可由 wiki.list_nodes 的结果推断。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。",
    RequiredScopes = ["wiki:wiki"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2WikiNodes) + "." + nameof(IFeishuTenantV2WikiNodes.CreateSpaceNodeAsync))]
public interface IFeishuTenantWikiCreateNodeTool
{
    /// <summary>创建知识空间节点。</summary>
    /// <returns>白名单投影后的 JSON 文本（node_token/obj_token/title/obj_type）。</returns>
    Task<string> CreateNodeAsync(
        [ToolParameter("space_id", "知识空间 ID（形如 7xxx）", Required = true)] string space_id,
        [ToolParameter("title", "节点标题", Required = true)] string title,
        [ToolParameter("obj_type", "节点对象类型（可选：docx=文档 / sheet=表格 / mindnote=思维笔记 / bitable=多维表格 / file=文件，默认 docx）")] string? obj_type = null,
        [ToolParameter("parent_node_token", "父节点 token（可选；缺省创建到空间顶层）")] string? parent_node_token = null,
        [ToolParameter("node_type", "节点类型（可选：origin=普通节点 / shortcut=快捷方式，默认 origin）")] string? node_type = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：wiki.move_node（映射 <c>IFeishuTenantV2WikiNodes.MoveSpaceNodeAsync</c>）。
/// </summary>
[FeishuTool("wiki.move_node",
    Description = "移动知识空间节点（改父节点或换空间）——用于知识库整理、归档。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。",
    RequiredScopes = ["wiki:wiki"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2WikiNodes) + "." + nameof(IFeishuTenantV2WikiNodes.MoveSpaceNodeAsync))]
public interface IFeishuTenantWikiMoveNodeTool
{
    /// <summary>移动节点。</summary>
    /// <returns>白名单投影后的 JSON 文本（node_token/parent_node_token/space_id）。</returns>
    Task<string> MoveNodeAsync(
        [ToolParameter("space_id", "节点当前所在空间 ID", Required = true)] string space_id,
        [ToolParameter("node_token", "要移动的节点 token", Required = true)] string node_token,
        [ToolParameter("target_parent_token", "目标父节点 token（可选；缺省移到目标空间顶层）")] string? target_parent_token = null,
        [ToolParameter("target_space_id", "目标空间 ID（可选；缺省在当前空间内移动）")] string? target_space_id = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：wiki.move_docs_to_space（映射 <c>IFeishuTenantV2WikiNodes.MoveDocsToWikiSpaceNodeAsync</c>）。
/// </summary>
/// <remarks>
/// "**把文档挪进知识库**"是高频诉求，而 SDK 的节点移动只对已在 wiki 中的节点有效；
/// 本工具走平台提供的<b>文档迁入</b>接口，是该场景的唯一正确入口。
/// </remarks>
[FeishuTool("wiki.move_docs_to_space",
    Description = "把已有云文档（docx/sheet/bitable 等）迁移进知识空间，成为 wiki 节点——'把这份文档挪进知识库'的首选入口。⚠️ 平台以异步任务执行，返回 task_id。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 wiki:wiki。",
    RequiredScopes = ["wiki:wiki"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV2WikiNodes) + "." + nameof(IFeishuTenantV2WikiNodes.MoveDocsToWikiSpaceNodeAsync))]
public interface IFeishuTenantWikiMoveDocsToSpaceTool
{
    /// <summary>把文档迁入知识空间（异步任务）。</summary>
    /// <returns>白名单投影后的 JSON 文本（wiki_token/task_id/applied）。</returns>
    Task<string> MoveDocsToSpaceAsync(
        [ToolParameter("space_id", "目标知识空间 ID", Required = true)] string space_id,
        [ToolParameter("obj_token", "要迁入的文档 token（形如 doxcnXxx）", Required = true)] string obj_token,
        [ToolParameter("parent_wiki_token", "目标父节点 token（可选；缺省放到空间顶层）")] string? parent_wiki_token = null,
        [ToolParameter("obj_type", "文档类型（可选：docx / sheet / bitable / mindnote / file，默认 docx）")] string? obj_type = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Search（1 个） ───────────────────────────

/// <summary>工具接口：search.doc_wiki（映射 <c>IFeishuTenantV2SearchDocWiki.SearchDocWikiAsync</c>）。</summary>
[FeishuTool("search.doc_wiki",
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
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── IM（5 个：2 只读 + 1 只读会话治理 + 1 写会话治理 + 1 只读搜索） ───────────────────────────

/// <summary>工具接口：im.get_history_messages（映射 <c>IFeishuTenantV1Message.GetHistoryMessageAsync</c>）。</summary>
[FeishuTool("im.get_history_messages",
    Description = "读取群聊的历史消息（chat_id 可由事件上下文获得），按创建时间倒序返回最近消息预览。只读，需 im:message:readonly。",
    RequiredScopes = ["im:message:readonly"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.GetHistoryMessageAsync))]
public interface IFeishuTenantImHistoryTool
{
    /// <summary>读取历史消息（分页，按创建时间倒序）。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id/create_time/sender_id/message_type/content 预览），超长截断并标记 truncated。</returns>
    Task<string> GetHistoryMessageAsync(
        [ToolParameter("chat_id", "群聊 ID（形如 ocXxx）", Required = true)] string chat_id,
        [ToolParameter("start_time", "起始时间（可选，RFC3339，如 2026-09-27T00:00:00+08:00）")] string? start_time = null,
        [ToolParameter("end_time", "结束时间（可选，RFC3339）")] string? end_time = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.get_message_content（映射 <c>IFeishuTenantV1Message.GetContentListByMessageIdAsync</c>）。</summary>
[FeishuTool("im.get_message_content",
    Description = "按 message_id 回查单条消息的完整内容——与 im.get_history_messages 组成两步链（历史消息列表 → 指定消息内容）。只读，需 im:message:readonly。",
    RequiredScopes = ["im:message:readonly"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.GetContentListByMessageIdAsync))]
public interface IFeishuTenantImMessageContentTool
{
    /// <summary>单条消息内容回查。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id/msg_type/body/mentions），超长截断并标记 truncated。</returns>
    Task<string> GetContentAsync(
        [ToolParameter("message_id", "消息 ID（形如 omXxx，来自 im.get_history_messages 或事件上下文）", Required = true)] string message_id,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.list_chat_members（映射 <c>IFeishuTenantV1ChatGroupMember.GetMemberPageListByIdAsync</c>）。</summary>
[FeishuTool("im.list_chat_members",
    Description = "分页列出群聊成员（member_id/name/tenant_key）——'这个群里有哪些人'的多步流程地基。chat_id 可由事件上下文获得。只读，需 im:chat:readonly。",
    RequiredScopes = ["im:chat:readonly"],
    Source = nameof(IFeishuTenantV1ChatGroupMember) + "." + nameof(IFeishuTenantV1ChatGroupMember.GetMemberPageListByIdAsync))]
public interface IFeishuTenantImListChatMembersTool
{
    /// <summary>列出群成员（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token/member_total），超长截断并标记 truncated。</returns>
    Task<string> GetMemberPageListByIdAsync(
        [ToolParameter("chat_id", "群聊 ID（形如 ocXxx）", Required = true)] string chat_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.reply_message（映射 <c>IFeishuTenantV1Message.ReplyMessageAsync</c>）。</summary>
[FeishuTool("im.reply_message",
    Description = "回复指定消息，形成话题串避免刷屏。message_id 来自 im.get_history_messages 或事件上下文；content 为 JSON 字符串（msg_type=text 时如 {\"text\":\"回复内容\"}）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。",
    RequiredScopes = ["im:message"],
    IsWrite = true,
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.ReplyMessageAsync))]
public interface IFeishuTenantImReplyMessageTool
{
    /// <summary>回复消息。</summary>
    /// <returns>白名单投影后的 JSON 文本（message_id）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> ReplyMessageAsync(
        [ToolParameter("message_id", "待回复的消息 ID（形如 omXxx）", Required = true)] string message_id,
        [ToolParameter("msg_type", "消息类型（text/post/image/file/audio/media/sticker/interactive/share_chat/share_user）", Required = true)] string msg_type,
        [ToolParameter("content", "消息内容 JSON 字符串（msg_type=text 时如 {\"text\":\"回复内容\"}）", Required = true)] string content,
        [ToolParameter("reply_in_thread", "是否以话题形式回复（可选）。留空时：若当前会话处于话题中则自动为 true，否则 false。仅在需要脱离话题、直接回主会话时才显式传 false。")] bool? reply_in_thread = null, [ToolParameter("idempotency_key", "幂等键（可选）：相同 uuid 在 1 小时内至多成功回复一条。省略时不保证幂等。")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不回复（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：im.search_messages（映射 <c>IFeishuTenantV1Message.SearchMessageAsync</c>）。</summary>
[FeishuTool("im.search_messages",
    Description = "按关键词搜索可见会话中的消息——支持按会话/发送者/时间过滤。返回消息 ID 与命中片段预览，可用 im.get_message_content 回查完整内容。只读，需 im:message:readonly。",
    RequiredScopes = ["im:message:readonly"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.SearchMessageAsync))]
public interface IFeishuTenantImSearchMessagesTool
{
    /// <summary>搜索消息（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/total/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> SearchMessageAsync(
        [ToolParameter("query", "搜索关键词（≤50 字符）", Required = true)] string query,

        [ToolParameter("chat_ids", "限定会话 ID 列表（可选，字符串数组）")] string[]? chat_ids = null,
        [ToolParameter("from_ids", "限定发送者 ID 列表（可选，字符串数组）")] string[]? from_ids = null,
        [ToolParameter("chat_type", "会话类型过滤（可选：p2p=单聊 / group=群聊）")] string? chat_type = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Sheets（2 个） ───────────────────────────

/// <summary>工具接口：sheets.list_sheets（映射 <c>IFeishuTenantV3Spreadsheets.GetSpreadsheetSheetsByTokenAsync</c>）。</summary>
[FeishuTool("sheets.list_sheets",
    Description = "列出电子表格的全部工作表（sheet_id/title/index）；sheet_id 供 sheets.get_range_values 构造 range。只读，需 sheets:spreadsheet:readonly。",
    RequiredScopes = ["sheets:spreadsheet:readonly"],
    Source = nameof(IFeishuTenantV3Spreadsheets) + "." + nameof(IFeishuTenantV3Spreadsheets.GetSpreadsheetSheetsByTokenAsync))]
public interface IFeishuTenantSheetsListTool
{
    /// <summary>列出工作表。</summary>
    /// <returns>白名单投影后的 JSON 文本（items）。</returns>
    Task<string> GetSpreadsheetSheetsByTokenAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：sheets.get_range_values（映射 <c>IFeishuTenantV3SpreadsheetData.GetRangeDataAsync</c>）。</summary>
[FeishuTool("sheets.get_range_values",
    Description = "读取工作表单元格区域数据；range 形如 ShtXxx!A1:C100（sheet_id 来自 sheets.list_sheets），建议先小范围取数。只读，需 sheets:spreadsheet:readonly。",
    RequiredScopes = ["sheets:spreadsheet:readonly"],
    Source = nameof(IFeishuTenantV3SpreadsheetData) + "." + nameof(IFeishuTenantV3SpreadsheetData.GetRangeDataAsync))]
public interface IFeishuTenantSheetsRangeTool
{
    /// <summary>读取单元格区域数据。</summary>
    /// <returns>区域值网格 JSON 文本（range/values），超长截断并标记 truncated。</returns>
    Task<string> GetRangeDataAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("range", "单元格区域（形如 ShtXxx!A1:C100）", Required = true)] string range,
        [ToolParameter("value_render_option", "取值格式（可选：ToString / FormattedValue / UnformattedValue）")] string? value_render_option = null,
        CancellationToken cancellationToken = default);
}

// ────────── R5 / F-3：IM 域补齐（thread / 群管理 / 撤回转发 / 已读） ──────────
// ⚠️ SDK 方法名缺陷说明（U-19：本轮不改 SDK，只在工具名与注释中用正确语义）：
//   · forward_message 落到 SDK 的 ReceiveMessageAsync（实为 POST /messages/{id}/forward，不是"接收消息"）
//   · 已读用户落到 GetMessageReadUsesAsync（"Uses" 应为 "Users"）
//   · 群信息落到 GetChatGroupInoByIdAsync（"Ino" 应为 "Info"）

/// <summary>im.get_thread_messages：读取话题（thread）内的消息。</summary>
[FeishuTool("im.get_thread_messages",
    Description = "读取某个话题（thread）内的消息——群话题场景下用 thread_id 取代 chat_id 读话题内容。thread_id 可由事件上下文获得。只读，需 im:message:readonly。",
    RequiredScopes = ["im:message:readonly"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.GetHistoryMessageAsync))]
    public interface IFeishuTenantImGetThreadMessagesTool
{
    /// <summary>读取话题消息（分页，按创建时间倒序）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> GetThreadMessagesAsync(
        [ToolParameter("thread_id", "话题 ID（形如 omt_xxx，来自事件上下文或 im.reply_message 返回）", Required = true)] string thread_id,
        [ToolParameter("page_size", "每页条数（可选，默认 50）")] int? page_size = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>im.revoke_message：撤回自己发出的消息。</summary>
[FeishuTool("im.revoke_message", IsWrite = true,
    Description = "撤回一条自己发出的消息（发错内容时纠正）。只能撤回本 Bot 发送的消息；message_id 来自事件上下文或 im.get_history_messages。high-risk-write：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message。建议先 dry_run 预演确认目标消息。",
    RequiredScopes = ["im:message"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.RevokeMessageAsync))]
    public interface IFeishuTenantImRevokeMessageTool
{
    /// <summary>撤回消息。</summary>
    /// <returns>结构化文本（ok=true 表示已受理）。</returns>
    Task<string> RevokeMessageAsync(
        [ToolParameter("message_id", "待撤回的消息 ID（形如 omXxx）", Required = true)] string message_id,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）：返回将要下发的 method/path，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>im.forward_message：转发单条消息到指定会话。</summary>
[FeishuTool("im.forward_message", IsWrite = true,
    Description = "把一条消息转发给用户或群。receive_id 来自 im.search_user / 事件上下文。⚠️ 底层 SDK 方法名为 ReceiveMessageAsync 但语义是转发（POST /messages/{id}/forward），不是接收消息。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message:send_as_bot。",
    RequiredScopes = ["im:message:send_as_bot"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.ReceiveMessageAsync))]
public interface IFeishuTenantImForwardMessageTool
{
    /// <summary>转发消息。</summary>
    /// <returns>结构化文本（转发结果 message_id）。</returns>
    Task<string> ForwardMessageAsync(
        [ToolParameter("message_id", "待转发的消息 ID（形如 omXxx）", Required = true)] string message_id,
        [ToolParameter("receive_id", "接收方 ID（open_id / user_id / union_id 之一）", Required = true)] string receive_id,
        [ToolParameter("receive_id_type", "接收方 ID 类型（可选：open_id / user_id / union_id，默认 open_id）")] string? receive_id_type = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同键的重复请求不会重复转发；建议由调用方给出稳定值，不要用随机数")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>im.forward_thread：转发整个话题。</summary>
[FeishuTool("im.forward_thread", IsWrite = true,
    Description = "把整个话题（thread）转发给用户或群——一次性把讨论上下文带过去。thread_id 来自事件上下文。⚠️ 底层 SDK 方法名为 ReceiveThreadsAsync 但语义是转发话题。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 im:message:send_as_bot。",
    RequiredScopes = ["im:message:send_as_bot"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.ReceiveThreadsAsync))]
    public interface IFeishuTenantImForwardThreadTool
{
    /// <summary>转发话题。</summary>
    /// <returns>结构化文本（转发结果 thread_id）。</returns>
    Task<string> ForwardThreadAsync(
        [ToolParameter("thread_id", "待转发的话题 ID（形如 omt_xxx）", Required = true)] string thread_id,
        [ToolParameter("receive_id", "接收方 ID（open_id / user_id / union_id 之一）", Required = true)] string receive_id,
        [ToolParameter("receive_id_type", "接收方 ID 类型（可选：open_id / user_id / union_id，默认 open_id）")] string? receive_id_type = null,
        [ToolParameter("idempotency_key", "幂等键（可选）：相同键的重复请求不会重复转发")] string? idempotency_key = null,
        [ToolParameter("dry_run", "仅预演不执行（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>im.get_message_read_users：查询消息已读用户。</summary>
[FeishuTool("im.get_message_read_users",
    Description = "查询某条消息已被哪些人读到（user_id + 读取时间）。⚠️ 底层 SDK 方法名拼写为 GetMessageReadUsesAsync（Uses 应为 Users），此处按正确语义命名。只读，需 im:message:readonly。",
    RequiredScopes = ["im:message:readonly"],
    Source = nameof(IFeishuTenantV1Message) + "." + nameof(IFeishuTenantV1Message.GetMessageReadUsesAsync))]
    public interface IFeishuTenantImGetMessageReadUsersTool
{
    /// <summary>查询已读用户（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token）。</returns>
    Task<string> GetMessageReadUsersAsync(
        [ToolParameter("message_id", "目标消息 ID（形如 omXxx）", Required = true)] string message_id,
        [ToolParameter("page_size", "每页条数（可选，默认 50）")] int? page_size = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>im.get_chat：读取群基础信息。</summary>
[FeishuTool("im.get_chat",
    Description = "读取群基础信息（群名/描述/成员数/群主）。chat_id 可由事件上下文获得。⚠️ 底层 SDK 方法名拼写为 GetChatGroupInoByIdAsync（Ino 应为 Info），此处按正确语义命名。只读，需 im:chat:readonly。",
    RequiredScopes = ["im:chat:readonly"],
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.GetChatGroupInoByIdAsync))]
    public interface IFeishuTenantImGetChatTool
{
    /// <summary>读取群信息。</summary>
    /// <returns>白名单投影后的 JSON 文本（chat_id/name/description/user_count/owner_id）。</returns>
    Task<string> GetChatAsync(
        [ToolParameter("chat_id", "群 ID（形如 ocXxx）", Required = true)] string chat_id,
        CancellationToken cancellationToken = default);
}

/// <summary>im.search_chats：按关键词搜索群。</summary>
[FeishuTool("im.search_chats",
    Description = "按关键词搜索群聊（返回 chat_id/name/描述）——用户只记得群名片段时的入口。只读，需 im:chat:readonly。",
    RequiredScopes = ["im:chat:readonly"],
    Source = nameof(IFeishuTenantV1ChatGroup) + "." + nameof(IFeishuTenantV1ChatGroup.GetChatGroupPageListByKeywordAsync))]
    public interface IFeishuTenantImSearchChatsTool
{
    /// <summary>搜索群（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token）。</returns>
    Task<string> SearchChatsAsync(
        [ToolParameter("query", "搜索关键词（群名片段）", Required = true)] string query,
        [ToolParameter("page_size", "每页条数（可选，默认 50）")] int? page_size = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
