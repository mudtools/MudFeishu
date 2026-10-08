// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// 运行时 schema 自省与万能兜底（R6 / S3：补齐官方 CLI 的 schema read + api call 能力）。
// </summary>
// <remarks>
// <para>
// <b>为什么需要它们</b>：SDK 有 1228 个方法，AI 工具面只策展了 87 个。未策展的方法对模型完全不可见——
// 模型既不知道方法签名，也没有调用途径。<c>feishu.schema_read</c> 让模型按方法名或关键字查询任意 SDK
// 方法的 HTTP/路由/参数/令牌/风险事实（数据源是编译期 <c>FeishuToolMethodCatalog</c>）；
// <c>feishu.api_call</c> 让模型通过方法名 + 参数字典调用任意已查到的方法（万能兜底）。
// </para>
// <para>
// <b>安全边界</b>：<c>feishu.api_call</c> 默认 <c>dry_run=true</c>（只返回预览，不调用下游）。
// 实际调用需宿主授权（<c>IToolExecutionAuthorizer</c>），且写操作（risk ≥ 1）必须显式确认。
// </para>
// </remarks>

/// <summary>
/// 工具接口：feishu.schema_read（运行时 schema 自省；无 SDK 源，只读编译期方法目录）。
/// </summary>
/// <remarks>
/// 给定方法限定名（如 <c>IFeishuTenantV1OkrPeriod.ListPeriodsAsync</c>）或关键字，
/// 返回该方法的 HTTP 方法、路由模板、路径/查询/请求体参数、令牌身份、风险分级、是否已策展。
/// 数据源是编译期 <c>FeishuToolMethodCatalog</c>，无下游调用。
/// </remarks>
[FeishuTool("feishu.schema_read",
    Description = "查询任意飞书 SDK 方法的签名事实（HTTP 方法 / 路由 / 参数 / 令牌 / 风险 / 是否已策展为工具）。当需要调用一个当前工具集未覆盖的 API 时，先用本工具查它的方法签名，再用 feishu.api_call 发起调用。只读编译期目录，不调用下游。",
    RequiredScopes = ["feishu:base"])]
public interface IFeishuTenantSchemaReadTool
{
    /// <summary>按方法名或关键字查询 SDK 方法签名。</summary>
    /// <returns>JSON 文本：方法条目数组（每条含 interface/method/http/route/token_kind/module/path_params/query_params/body_type/risk/curated/curated_tool），超长截断。</returns>
    Task<string> SchemaReadAsync(
        [ToolParameter("method", "方法限定名（如 IFeishuTenantV1OkrPeriod.ListPeriodsAsync）；提供时精确匹配单个方法", Required = false)] string? method,
        [ToolParameter("keyword", "关键字（如 Okr / Calendar / attendance）；按方法名/接口名/模块大小写不敏感搜索", Required = false)] string? keyword,
        [ToolParameter("module", "模块名（如 Okr / Bitable / Calendar）；只返回该模块下的方法", Required = false)] string? module,
        [ToolParameter("limit", "最大返回条数（默认 20，上限 100）", Required = false)] int? limit,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：feishu.api_call（万能兜底调用器；通过方法名 + 参数字典调用任意 SDK 方法）。
/// </summary>
/// <remarks>
/// <para>
/// <b>使用流程</b>：① 用 <c>feishu.schema_read</c> 查方法签名 → ② 用本工具传入方法名 + path/query/body 参数 → ③ 获得响应。
/// </para>
/// <para>
/// <b>安全</b>：默认 <c>dry_run=true</c>（只返回预览：构造的 URL + HTTP 方法 + 参数摘要，不调用下游）。
/// <c>dry_run=false</c> 时实际发起 HTTP 调用；写操作（risk ≥ 1）须宿主授权。
/// </para>
/// </remarks>
[FeishuTool("feishu.api_call",
    Description = "通过方法名 + 参数字典调用任意飞书 SDK 方法（万能兜底）。使用前先用 feishu.schema_read 查方法签名。默认 dry_run=true 只预览不调用；dry_run=false 时实际发起 HTTP 请求。写操作须宿主授权。",
    RequiredScopes = ["feishu:base"],
    IsWrite = true)]
public interface IFeishuTenantApiCallTool
{
    /// <summary>通用 API 调用。</summary>
    /// <returns>dry_run=true 时返回预览 JSON（method/http/url/path_params/query_params/body）；dry_run=false 时返回下游响应 JSON，超长截断。</returns>
    Task<string> ApiCallAsync(
        [ToolParameter("method", "方法限定名（如 IFeishuTenantV1OkrPeriod.ListPeriodsAsync），须先用 feishu.schema_read 查到", Required = true)] string method,
        [ToolParameter("path_params", "路径参数 JSON 对象（如 {\"period_id\":\"xxx\"}）；无路径参数时省略", Required = false)] string? path_params,
        [ToolParameter("query_params", "查询参数 JSON 对象（如 {\"page_size\":20}）；无查询参数时省略", Required = false)] string? query_params,
        [ToolParameter("body", "请求体 JSON 字符串；无请求体时省略", Required = false)] string? body,
        [ToolParameter("dry_run", "仅预览不调用（默认 true）：返回构造的 URL + HTTP 方法 + 参数摘要，不调用下游", Required = false)] bool? dry_run,
        CancellationToken cancellationToken = default);
}