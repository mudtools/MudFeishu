// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Tools;

/// <summary>
/// Phase 1 只读工具接口集（§3.3.2 清单：6 域 10 个，全部 Tenant 身份）。
/// </summary>
/// <remarks>
/// <para>
/// 只声明模型可见 Schema（工具名/描述/扁平参数）；强类型接口调用、参数映射、
/// <c>FeishuApiResult</c> 解包与结果裁剪由执行链（<c>FeishuToolBinding</c> + 分域执行器）承担。
/// 复杂请求体（<c>QueryRecordsRequest</c>/<c>SearchDocWikiRequest</c>）一律由绑定层构造，
/// 模型只见标量/标量数组（§3.3.1 原则 2）。
/// </para>
/// <para>
/// <c>page_size</c>/<c>sort_type</c>/<c>container_id_type</c>/<c>user_id_type</c> 等运维性参数
/// 不进 Schema（绑定层补齐/钳制）；<c>page_token</c> 对模型可见（多页追问）。
/// scope 字符串为占位，落地时对照开放平台控制台核对回填（契约守卫只锁「工具名↔scope 存在性」）。
/// </para>
/// </remarks>

// ─────────────────────────── Bitable（3 个） ───────────────────────────

/// <summary>工具接口：bitable.list_tables（映射 <c>IFeishuTenantV1BitableAppTable.GetAppTablePageListAsync</c>）。</summary>
[FeishuTool("bitable.list_tables",
    Description = "列出多维表格中的全部数据表，返回 table_id/name/revision；先于 bitable.list_fields、bitable.query_records 使用。只读，需 bitable:app:readonly。",
    RequiredScopes = ["bitable:app:readonly"])]
public interface IFeishuBitableListTablesTool
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
    RequiredScopes = ["bitable:app:readonly"])]
public interface IFeishuBitableListFieldsTool
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
    RequiredScopes = ["bitable:app:readonly"])]
public interface IFeishuBitableQueryRecordsTool
{
    /// <summary>查询记录（分页；filter 简化文法由绑定层解析为官方过滤结构）。</summary>
    /// <returns>白名单投影后的 JSON 文本（record_id/fields），超长截断并标记 truncated。</returns>
    Task<string> QueryRecordsPageListAsync(
        [ToolParameter("app_token", "多维表格 AppToken（形如 bascnXxx）", Required = true)] string app_token,
        [ToolParameter("table_id", "数据表 ID（形如 tblXxx）", Required = true)] string table_id,
        [ToolParameter("view_id", "视图 ID（可选）")] string? view_id = null,
        [ToolParameter("field_names", "只返回这些字段（可选，字符串数组；缺省返回全部字段）")] string[]? field_names = null,
        [ToolParameter("filter", "简化筛选式（可选）：字段 = 值 或 字段 contains 值，and 连接，最多 5 个子句，如：status = \"done\" and owner contains 张三")] string? filter = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Docx（1 个） ───────────────────────────

/// <summary>工具接口：docx.get_raw_content（映射 <c>IFeishuTenantV1Docx.GetDocumentRawContentAsync</c>）。</summary>
[FeishuTool("docx.get_raw_content",
    Description = "读取飞书文档的纯文本正文；document_id 可来自 wiki.get_node 的 obj_token 或 search.doc_wiki 结果的 token。只读，需 docx:document:readonly。",
    RequiredScopes = ["docx:document:readonly"])]
public interface IFeishuDocxRawContentTool
{
    /// <summary>读取文档纯文本正文。</summary>
    /// <returns>正文纯文本，超长截断并标记 truncated。</returns>
    Task<string> GetDocumentRawContentAsync(
        [ToolParameter("document_id", "文档 ID（形如 doxcnXxx；wiki 文档传 wiki.get_node 返回的 obj_token）", Required = true)] string document_id,
        [ToolParameter("lang", "文档语言（可选，0=中文）")] int? lang = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Wiki（2 个） ───────────────────────────

/// <summary>工具接口：wiki.get_node（映射 <c>IFeishuTenantV2WikiNodes.GetNodeSpaceInfoAsync</c>）。</summary>
[FeishuTool("wiki.get_node",
    Description = "解析知识库节点信息（node_token/title/obj_type/obj_token）；obj_token 可传给 docx.get_raw_content 读取正文。只读，需 wiki:wiki:readonly。",
    RequiredScopes = ["wiki:wiki:readonly"])]
public interface IFeishuWikiGetNodeTool
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
    RequiredScopes = ["wiki:wiki:readonly"])]
public interface IFeishuWikiListNodesTool
{
    /// <summary>列出子节点（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items/has_more/page_token），超长截断并标记 truncated。</returns>
    Task<string> GetSpaceNodesPageListAsync(
        [ToolParameter("space_id", "知识空间 ID（形如 7xxx）", Required = true)] string space_id,
        [ToolParameter("parent_node_token", "父节点 token（可选；缺省列出空间顶层节点）")] string? parent_node_token = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

// ─────────────────────────── Search（1 个） ───────────────────────────

/// <summary>工具接口：search.doc_wiki（映射 <c>IFeishuTenantV2SearchDocWiki.SearchDocWikiAsync</c>）。</summary>
[FeishuTool("search.doc_wiki",
    Description = "云文档与知识库全文搜索，返回标题/摘要/URL；结果的 token 可传给 docx.get_raw_content、url 对应节点可传给 wiki.get_node。query 上限 30 字符。只读，需 search:docs:readonly。",
    RequiredScopes = ["search:docs:readonly"])]
public interface IFeishuSearchDocWikiTool
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

// ─────────────────────────── IM（1 个） ───────────────────────────

/// <summary>工具接口：im.get_history_messages（映射 <c>IFeishuTenantV1Message.GetHistoryMessageAsync</c>）。</summary>
[FeishuTool("im.get_history_messages",
    Description = "读取群聊的历史消息（chat_id 可由事件上下文获得），按创建时间倒序返回最近消息预览。只读，需 im:message:readonly。",
    RequiredScopes = ["im:message:readonly"])]
public interface IFeishuImHistoryTool
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

// ─────────────────────────── Sheets（2 个） ───────────────────────────

/// <summary>工具接口：sheets.list_sheets（映射 <c>IFeishuTenantV3Spreadsheets.GetSpreadsheetSheetsByTokenAsync</c>）。</summary>
[FeishuTool("sheets.list_sheets",
    Description = "列出电子表格的全部工作表（sheet_id/title/index）；sheet_id 供 sheets.get_range_values 构造 range。只读，需 sheets:spreadsheet:readonly。",
    RequiredScopes = ["sheets:spreadsheet:readonly"])]
public interface IFeishuSheetsListTool
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
    RequiredScopes = ["sheets:spreadsheet:readonly"])]
public interface IFeishuSheetsRangeTool
{
    /// <summary>读取单元格区域数据。</summary>
    /// <returns>区域值网格 JSON 文本（range/values），超长截断并标记 truncated。</returns>
    Task<string> GetRangeDataAsync(
        [ToolParameter("spreadsheet_token", "电子表格 token（形如 shtcnXxx）", Required = true)] string spreadsheet_token,
        [ToolParameter("range", "单元格区域（形如 ShtXxx!A1:C100）", Required = true)] string range,
        [ToolParameter("value_render_option", "取值格式（可选：ToString / FormattedValue / UnformattedValue）")] string? value_render_option = null,
        CancellationToken cancellationToken = default);
}
