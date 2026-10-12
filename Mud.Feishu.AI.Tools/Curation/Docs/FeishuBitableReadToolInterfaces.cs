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
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
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
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
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
