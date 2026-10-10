// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Spark 妙搭（R7 / A6 新域，10 个工具） ───────────────────────────
//
// 链路设计（"建应用 → 配可见范围 → 写数据表"的产出闭环）：
//   list_apps → create_app → patch_app → get_app_visibility → update_app_visibility
//     → list_tables → query_table_records → add_table_records / update_table_records
//
// ⚠️ 不策展项（R7 DP-A6-2，A10 二进制/本地文件防线）：
//   · upload_html_release / upload_app_icon —— 入参为 [FormContent] 本地文件路径，模型无文件系统；
//   · upload_storage / download_storage —— 含 byte[]；
//   · execute_sql —— 任意 SQL 执行是比 feishu.api_call 更宽的越权通道，无法机械校验表归属；
//   · batch_update_table_records —— 约束反直觉且与 update_table_records 重叠。

/// <summary>
/// 工具接口：spark.list_apps（映射 <c>IFeishuTenantV1SparkApp.GetAppListAsync</c>）。
/// </summary>
/// <remarks>R7 / A6：应用清单入口（tenant 身份）。</remarks>
[FeishuTool("spark.list_apps",
    Description = "获取妙搭应用列表（tenant 身份）。分页工具：可选 fetch_all=true 在预算内自动翻页取完全部应用。需 spark:app:readonly 权限。",
    RequiredScopes = ["spark:app:readonly"],
    Source = nameof(IFeishuTenantV1SparkApp) + "." + nameof(IFeishuTenantV1SparkApp.GetAppListAsync))]
public interface IFeishuTenantSparkListAppsTool
{
    /// <summary>列出妙搭应用（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items 含 app_id/name/status/online_url）。</returns>
    Task<string> GetAppListAsync(
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.get_app_analytics（映射 <c>IFeishuTenantV1SparkApp.GetAppAnalyticsOverviewAsync</c>）。
/// </summary>
[FeishuTool("spark.get_app_analytics",
    Description = "获取妙搭应用运营数据总览（活跃用户/新增用户/页面浏览，tenant 身份）。start_time/end_time 为秒级 Unix 时间戳字符串（如 \"1705312800\"），须 end_time >= start_time。需 spark:app:readonly 权限。",
    RequiredScopes = ["spark:app:readonly"],
    Source = nameof(IFeishuTenantV1SparkApp) + "." + nameof(IFeishuTenantV1SparkApp.GetAppAnalyticsOverviewAsync))]
public interface IFeishuTenantSparkGetAppAnalyticsTool
{
    /// <summary>获取应用运营数据总览。</summary>
    /// <returns>白名单投影后的 JSON 文本（active_users/signups/page_views 各自的 value/diff）。</returns>
    Task<string> GetAppAnalyticsOverviewAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("start_time", "统计起始时间，秒级 Unix 时间戳字符串（如 \"1705312800\"）", Required = true)] string start_time,
        [ToolParameter("end_time", "统计结束时间，秒级 Unix 时间戳字符串（如 \"1705399200\"）", Required = true)] string end_time,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.create_app（映射 <c>IFeishuUserV1SparkApp.CreateAppAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A6：<b>闭环首步</b>——创建妙搭应用（user 身份，纯 JSON 请求体）。
/// </remarks>
[FeishuTool("spark.create_app",
    Description = "创建妙搭应用（user 身份，闭环首步）。返回新应用的 app_id，后续用 spark.patch_app / spark.list_tables 继续。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 spark:app 权限。",
    RequiredScopes = ["spark:app"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1SparkApp) + "." + nameof(IFeishuUserV1SparkApp.CreateAppAsync))]
public interface IFeishuUserSparkCreateAppTool
{
    /// <summary>创建应用。</summary>
    /// <returns>白名单投影后的 JSON 文本（app_id/name）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> CreateAppAsync(
        [ToolParameter("name", "应用名称", Required = true)] string name,
        [ToolParameter("app_type", "应用类型（可选，官方 app_type 取值）")] string? app_type = null,
        [ToolParameter("desc", "应用描述（可选）")] string? desc = null,
        [ToolParameter("icon_url", "应用图标 URL（可选）")] string? icon_url = null,
        [ToolParameter("dry_run", "仅预演不创建（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.patch_app（映射 <c>IFeishuUserV1SparkApp.PatchAppAsync</c>）。
/// </summary>
[FeishuTool("spark.patch_app",
    Description = "修改妙搭应用元信息（名称/描述/图标，user 身份；PATCH 语义天然幂等）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 spark:app 权限。",
    RequiredScopes = ["spark:app"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1SparkApp) + "." + nameof(IFeishuUserV1SparkApp.PatchAppAsync))]
public interface IFeishuUserSparkPatchAppTool
{
    /// <summary>修改应用元信息。</summary>
    /// <returns>白名单投影后的 JSON 文本（app_id/name）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> PatchAppAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("name", "应用名称（可选）")] string? name = null,
        [ToolParameter("desc", "应用描述（可选）")] string? desc = null,
        [ToolParameter("icon_url", "应用图标 URL（可选）")] string? icon_url = null,
        [ToolParameter("dry_run", "仅预演不修改（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.get_app_visibility（映射 <c>IFeishuUserV1SparkApp.GetAppVisibilityAsync</c>）。
/// </summary>
[FeishuTool("spark.get_app_visibility",
    Description = "获取妙搭应用的可用范围配置（scope/users/departments/chats，user 身份）。修改前先用本工具确认当前范围。需 spark:app:readonly 权限。",
    RequiredScopes = ["spark:app:readonly"],
    Source = nameof(IFeishuUserV1SparkApp) + "." + nameof(IFeishuUserV1SparkApp.GetAppVisibilityAsync))]
public interface IFeishuUserSparkGetAppVisibilityTool
{
    /// <summary>获取可用范围。</summary>
    /// <returns>白名单投影后的 JSON 文本（scope/require_login/apply_config 与三类主体列表）。</returns>
    Task<string> GetAppVisibilityAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("user_id_type", "用户 ID 类型（可选：open_id / union_id / user_id，默认 open_id）")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.update_app_visibility（映射 <c>IFeishuUserV1SparkApp.UpdateAppVisibilityAsync</c>）。
/// </summary>
/// <remarks>
/// ⚠️ 扩大可达范围 → 走"敏感工具纪律"：描述中声明后果（可能使应用对外可见）+ 默认空名单不启用。
/// </remarks>
[FeishuTool("spark.update_app_visibility",
    Description = "修改妙搭应用的可用范围（user 身份）。⚠️ 此操作会扩大应用可达范围（scope=Public 即对全员/外部可见），敏感操作，建议先 spark.get_app_visibility 确认当前范围并用 dry_run=true 预演。scope 取值：Public（公开）/ Tenant（本租户）/ Range（指定范围，须配 users/departments/chats）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），需 spark:app 权限。",
    RequiredScopes = ["spark:app"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1SparkApp) + "." + nameof(IFeishuUserV1SparkApp.UpdateAppVisibilityAsync))]
public interface IFeishuUserSparkUpdateAppVisibilityTool
{
    /// <summary>修改可用范围。</summary>
    /// <returns>结构化文本（app_id/scope/updated）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> UpdateAppVisibilityAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("visibility_json", "可见范围配置 JSON 对象字符串，形如 {\"scope\":\"Range\",\"users\":[\"ou_xxx\"],\"departments\":[],\"chats\":[],\"require_login\":true}；scope 必填，取值 Public/Tenant/Range，users/departments/chats 仅 Range 时生效", Required = true)] string visibility_json,
        [ToolParameter("dry_run", "仅预演不修改（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.list_tables（映射 <c>IFeishuUserV1SparkAppTable.GetTableListAsync</c>）。
/// </summary>
[FeishuTool("spark.list_tables",
    Description = "获取妙搭应用的数据表列表（含字段定义，user 身份）。分页工具：可选 fetch_all=true 自动翻页。拿到表名后再用 spark.query_table_records 取记录。需 spark:table 权限。",
    RequiredScopes = ["spark:table"],
    Source = nameof(IFeishuUserV1SparkAppTable) + "." + nameof(IFeishuUserV1SparkAppTable.GetTableListAsync))]
public interface IFeishuUserSparkListTablesTool
{
    /// <summary>列出数据表（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items 含 name/description/columns）。</returns>
    Task<string> GetTableListAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.query_table_records（映射 <c>IFeishuUserV1SparkAppTable.GetTableRecordListAsync</c>）。
/// </summary>
/// <remarks>R7 / A6：分页（进自动翻页范围）；filter/order 为 PostgREST 语法。</remarks>
[FeishuTool("spark.query_table_records",
    Description = "查询妙搭数据表记录（user 身份）。filter/order 为 PostgREST 语法（filter 如 age=gt.10；order 如 created_at.desc）。分页工具：可选 fetch_all=true 自动翻页。需 spark:table 权限。",
    RequiredScopes = ["spark:table"],
    Source = nameof(IFeishuUserV1SparkAppTable) + "." + nameof(IFeishuUserV1SparkAppTable.GetTableRecordListAsync))]
public interface IFeishuUserSparkQueryTableRecordsTool
{
    /// <summary>查询记录（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（items 为记录数组 + total/has_more/page_token）。</returns>
    Task<string> GetTableRecordListAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("table_id", "数据表名（table_name）", Required = true)] string table_id,
        [ToolParameter("select", "返回字段列表（可选，逗号分隔；缺省返回全部字段）")] string? select = null,
        [ToolParameter("filter", "过滤条件（可选，PostgREST 语法，如 age=gt.10）")] string? filter = null,
        [ToolParameter("order", "排序（可选，PostgREST 语法，如 created_at.desc）")] string? order = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.add_table_records（映射 <c>IFeishuUserV1SparkAppTable.PostTableRecordsAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A6：插入/可选合并（<c>upsert=true</c> → 绑定层映射请求头 <c>Prefer: resolution=merge-duplicates</c>）。
/// </remarks>
[FeishuTool("spark.add_table_records",
    Description = "向妙搭数据表添加记录（user 身份）。records_json 为记录数组 JSON，单次上限 500 条。upsert=true 时按主键合并（映射请求头 Prefer: resolution=merge-duplicates），缺省为纯插入。⚠️ 非幂等：不带 upsert 时重复调用会产生重复记录。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 spark:table 权限。",
    RequiredScopes = ["spark:table"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1SparkAppTable) + "." + nameof(IFeishuUserV1SparkAppTable.PostTableRecordsAsync))]
public interface IFeishuUserSparkAddTableRecordsTool
{
    /// <summary>添加记录。</summary>
    /// <returns>白名单投影后的 JSON 文本（record_ids）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> PostTableRecordsAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("table_id", "数据表名（table_name）", Required = true)] string table_id,
        [ToolParameter("records_json", "记录数组 JSON（每个对象为字段名→值），形如 [{\"name\":\"张三\",\"age\":18}]，上限 500 条", Required = true)] string records_json,
        [ToolParameter("upsert", "是否启用合并模式（可选，默认 false）；true 时同主键记录将被更新而非新增")] bool? upsert = null,
        [ToolParameter("dry_run", "仅预演不写入（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.update_table_records（映射 <c>IFeishuUserV1SparkAppTable.PatchTableRecordsAsync</c>）。
/// </summary>
/// <remarks>R7 / A6：按 filter 条件更新（PostgREST 语法，如 age=gt.10）。filter 必填。</remarks>
[FeishuTool("spark.update_table_records",
    Description = "按条件更新妙搭数据表记录（user 身份）。filter 必填（PostgREST 语法，如 age=gt.10），会更新**所有命中记录**；record_json 为要修改的字段对象。批量/条件更新天然幂等（同 filter 重放结果一致）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 spark:table 权限。",
    RequiredScopes = ["spark:table"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1SparkAppTable) + "." + nameof(IFeishuUserV1SparkAppTable.PatchTableRecordsAsync))]
public interface IFeishuUserSparkUpdateTableRecordsTool
{
    /// <summary>按条件更新记录。</summary>
    /// <returns>白名单投影后的 JSON 文本（record_ids）；<c>dry_run=true</c> 时返回请求摘要且不调用下游。</returns>
    Task<string> PatchTableRecordsAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("table_id", "数据表名（table_name）", Required = true)] string table_id,
        [ToolParameter("filter", "更新条件（PostgREST 语法，如 age=gt.10；必填，防止全表误更新）", Required = true)] string filter,
        [ToolParameter("record_json", "更新字段 JSON 对象，形如 {\"age\":20}", Required = true)] string record_json,
        [ToolParameter("dry_run", "仅预演不更新（可选，默认 false）：返回将要下发的 method/path 与请求体字段摘要，不调用下游")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
