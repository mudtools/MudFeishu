// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────── security · 应用 API 调用日志（F-1 收官切片） ───────────────

/// <summary>
/// 工具接口：security.list_openapi_logs（映射 <c>IFeishuTenantV1SecurityOpenApiLog.ListOpenApiLogDataAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么补它</b>：与 <c>security.query_audit_logs</c>（<b>人在控制台/客户端做了什么</b>）互补——
/// 本工具回答"<b>哪个应用/接口在哪一刻被调了什么、返回什么状态</b>"，是集成排障的第一现场；
/// 缺它时模型只能让用户"去看日志"，拿不到证据。
/// </para>
/// <para>
/// <b>PII 纪律（DP-A5-1 档位 ①）</b>：结果含调用方 IP，且日志详情里的 <c>payload</c> / <c>response</c> /
/// <c>query_param</c> 可能携带业务数据（甚至个人信息）⇒ 属 PII 出口：<b>默认不启用</b>（需宿主显式加入
/// <c>FeishuAgent:Tools</c>），描述必须声明"结果进第三方模型上下文"（由 <c>PiiToolDisciplineContractGuards</c> 机械锁定），
/// 并在 <c>scope-authority.json</c> 的 <c>piiTools</c> 登记。
/// </para>
/// <para>
/// <b>投影纪律</b>：<b>丢弃 payload / response / query_param</b>（可能含业务数据或个人信息的原始载荷），
/// 只返回"路径 + 方法 + 状态码"这类<b>足以定位问题、不足以还原数据</b>的最小证据集——
/// 需要完整载荷时应走宿主的日志系统，而不是让模型上下文承载原始请求体。
/// </para>
/// <para>
/// <b>DP-B1-0 页不进</b>：只暴露 <c>page_token</c>，页大小固定走 SDK 默认（10）。
/// </para>
/// </remarks>
[FeishuTool("security.list_openapi_logs",
    Description = "查询企业应用的【OpenAPI 调用日志】——哪个 app_id/哪个接口（path+method）在什么时间被调用、返回什么状态码、来自哪个 IP。用于集成排障（接口报错/鉴权失败/调用异常）取证。⚠️ 该工具属 PII：结果含调用方 IP，会进入【第三方模型上下文】；【默认不启用】，需宿主显式加入白名单并确认合规口径；原始请求体 payload / 响应体 response / 查询参数不返回（避免业务数据入上下文）。时间范围起止相差不能超过 30 天。只读，需 admin:audit_info:readonly。",
    RequiredScopes = ["admin:audit_info:readonly"],
    Source = nameof(IFeishuTenantV1SecurityOpenApiLog) + "." + nameof(IFeishuTenantV1SecurityOpenApiLog.ListOpenApiLogDataAsync))]
public interface IFeishuTenantSecurityListOpenapiLogsTool
{
    /// <summary>按时间窗查询应用 API 调用日志。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 id/api_key/event_time/app_id/ip/log_detail{path,method,status_code}），超长截断并标记 truncated。</returns>
    Task<string> ListOpenApiLogsAsync(
        [ToolParameter("oldest", "起始时间（可选，秒级时间戳；缺省=30 天前；与 latest 相差不能超过 30 天）")] int? oldest = null,
        [ToolParameter("latest", "结束时间（可选，秒级时间戳；缺省=当前时刻）")] int? latest = null,
        [ToolParameter("api_key", "按开放平台 API 名称筛选（可选，如 im/v1/messages；不填=全部接口）")] string? api_key = null,
        [ToolParameter("app_id", "按调用方应用 ID 筛选（可选；排查某个自建应用时传）")] string? app_id = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        [ToolParameter("fetch_all", "自动翻页取完全部结果（可选，默认 false）；启用后在预算内循环翻页，触达上限时返回 truncated=true + next_page_token")] bool? fetch_all = null,
        [ToolParameter("max_items", "结果预算上限（可选，默认 200，硬上限 1000）；仅在 fetch_all=true 时生效")] int? max_items = null,
        CancellationToken cancellationToken = default);
}
