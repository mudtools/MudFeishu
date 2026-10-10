// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Spark 妙搭（R7 / A6 新域） ───────────────────────────

/// <summary>
/// 工具接口：spark.list_apps（映射 <c>IFeishuTenantV1SparkApp.GetAppListAsync</c>）。
/// </summary>
[FeishuTool("spark.list_apps",
    Description = "获取妙搭应用列表（tenant 身份）。需 spark:app:readonly 权限。",
    RequiredScopes = ["spark:app:readonly"],
    Source = nameof(IFeishuTenantV1SparkApp) + "." + nameof(IFeishuTenantV1SparkApp.GetAppListAsync))]
public interface IFeishuTenantSparkListAppsTool
{
    Task<string> GetAppListAsync(
        [ToolParameter("page_size", "分页大小（可选，默认 20，上限 100）")] int? page_size = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.get_app_analytics（映射 <c>IFeishuTenantV1SparkApp.GetAppAnalyticsOverviewAsync</c>）。
/// </summary>
[FeishuTool("spark.get_app_analytics",
    Description = "获取妙搭应用用量/访问概览（tenant 身份）。需 spark:app:readonly 权限。",
    RequiredScopes = ["spark:app:readonly"],
    Source = nameof(IFeishuTenantV1SparkApp) + "." + nameof(IFeishuTenantV1SparkApp.GetAppAnalyticsOverviewAsync))]
public interface IFeishuTenantSparkGetAppAnalyticsTool
{
    Task<string> GetAppAnalyticsOverviewAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.create_app（映射 <c>IFeishuUserV1SparkApp.CreateAppAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A6：<b>闭环首步</b>——创建妙搭应用。user 身份。
/// </remarks>
[FeishuTool("spark.create_app",
    Description = "创建妙搭应用（user 身份，闭环首步）。需 spark:app 权限。宿主须在 AllowedIdentities 放行 user。",
    RequiredScopes = ["spark:app"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1SparkApp) + "." + nameof(IFeishuUserV1SparkApp.CreateAppAsync))]
public interface IFeishuUserSparkCreateAppTool
{
    Task<string> CreateAppAsync(
        [ToolParameter("name", "应用名称", Required = true)] string name,
        [ToolParameter("desc", "应用描述（可选）")] string? desc = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.patch_app（映射 <c>IFeishuUserV1SparkApp.PatchAppAsync</c>）。
/// </summary>
[FeishuTool("spark.patch_app",
    Description = "修改妙搭应用元信息（user 身份，幂等）。需 spark:app 权限。",
    RequiredScopes = ["spark:app"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1SparkApp) + "." + nameof(IFeishuUserV1SparkApp.PatchAppAsync))]
public interface IFeishuUserSparkPatchAppTool
{
    Task<string> PatchAppAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("name", "应用名称（可选）")] string? name = null,
        [ToolParameter("desc", "应用描述（可选）")] string? desc = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.get_app_visibility（映射 <c>IFeishuUserV1SparkApp.GetAppVisibilityAsync</c>）。
/// </summary>
[FeishuTool("spark.get_app_visibility",
    Description = "获取妙搭应用可用范围（user 身份）。需 spark:app:readonly 权限。",
    RequiredScopes = ["spark:app:readonly"],
    Source = nameof(IFeishuUserV1SparkApp) + "." + nameof(IFeishuUserV1SparkApp.GetAppVisibilityAsync))]
public interface IFeishuUserSparkGetAppVisibilityTool
{
    Task<string> GetAppVisibilityAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.update_app_visibility（映射 <c>IFeishuUserV1SparkApp.UpdateAppVisibilityAsync</c>）。
/// </summary>
/// <remarks>
/// ⚠️ 扩大可达范围 → 敏感操作。
/// </remarks>
[FeishuTool("spark.update_app_visibility",
    Description = "修改妙搭应用可用范围（user 身份）。⚠️ 扩大可达范围属敏感操作。需 spark:app 权限。",
    RequiredScopes = ["spark:app"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1SparkApp) + "." + nameof(IFeishuUserV1SparkApp.UpdateAppVisibilityAsync))]
public interface IFeishuUserSparkUpdateAppVisibilityTool
{
    Task<string> UpdateAppVisibilityAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("visibility_json", "可见范围配置 JSON", Required = true)] string visibility_json,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.list_tables（映射 <c>IFeishuUserV1SparkAppTable.GetTableListAsync</c>）。
/// </summary>
[FeishuTool("spark.list_tables",
    Description = "获取妙搭应用数据表列表（user 身份）。需 spark:table 权限。",
    RequiredScopes = ["spark:table"],
    Source = nameof(IFeishuUserV1SparkAppTable) + "." + nameof(IFeishuUserV1SparkAppTable.GetTableListAsync))]
public interface IFeishuUserSparkListTablesTool
{
    Task<string> GetTableListAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("page_size", "分页大小（可选，默认 20）")] int? page_size = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.query_table_records（映射 <c>IFeishuUserV1SparkAppTable.GetTableRecordListAsync</c>）。
/// </summary>
[FeishuTool("spark.query_table_records",
    Description = "分页查询妙搭数据表记录（user 身份）。需 spark:table 权限。",
    RequiredScopes = ["spark:table"],
    Source = nameof(IFeishuUserV1SparkAppTable) + "." + nameof(IFeishuUserV1SparkAppTable.GetTableRecordListAsync))]
public interface IFeishuUserSparkQueryTableRecordsTool
{
    Task<string> GetTableRecordListAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("table_id", "数据表 ID", Required = true)] string table_id,
        [ToolParameter("page_size", "分页大小（可选，默认 20）")] int? page_size = null,
        [ToolParameter("page_token", "分页游标（可选）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.add_table_records（映射 <c>IFeishuUserV1SparkAppTable.PostTableRecordsAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A6：插入/可选合并（upsert 布尔 → 绑定层映射请求头 Prefer: resolution=merge-duplicates）。
/// </remarks>
[FeishuTool("spark.add_table_records",
    Description = "向妙搭数据表添加记录（user 身份）。可选 upsert=true 启用合并模式。记录数组上限 500 条。需 spark:table 权限。",
    RequiredScopes = ["spark:table"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1SparkAppTable) + "." + nameof(IFeishuUserV1SparkAppTable.PostTableRecordsAsync))]
public interface IFeishuUserSparkAddTableRecordsTool
{
    Task<string> PostTableRecordsAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("table_id", "数据表 ID", Required = true)] string table_id,
        [ToolParameter("records_json", "记录数组 JSON（每个对象为字段名→值）", Required = true)] string records_json,
        [ToolParameter("upsert", "是否启用合并模式（可选，默认 false）")] bool? upsert = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：spark.update_table_records（映射 <c>IFeishuUserV1SparkAppTable.PatchTableRecordsAsync</c>）。
/// </summary>
/// <remarks>
/// R7 / A6：按 filter 条件更新（PostgREST 语法，如 age=gt.10）。filter 必填。
/// </remarks>
[FeishuTool("spark.update_table_records",
    Description = "按条件更新妙搭数据表记录（user 身份）。filter 必填（PostgREST 语法，如 age=gt.10）。需 spark:table 权限。",
    RequiredScopes = ["spark:table"],
    IsWrite = true,
    Source = nameof(IFeishuUserV1SparkAppTable) + "." + nameof(IFeishuUserV1SparkAppTable.PatchTableRecordsAsync))]
public interface IFeishuUserSparkUpdateTableRecordsTool
{
    Task<string> PatchTableRecordsAsync(
        [ToolParameter("app_id", "妙搭应用 ID", Required = true)] string app_id,
        [ToolParameter("table_id", "数据表 ID", Required = true)] string table_id,
        [ToolParameter("filter", "更新条件（PostgREST 语法，如 age=gt.10）", Required = true)] string filter,
        [ToolParameter("record_json", "更新字段 JSON 对象", Required = true)] string record_json,
        CancellationToken cancellationToken = default);
}
