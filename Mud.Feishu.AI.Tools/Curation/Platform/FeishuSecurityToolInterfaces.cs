// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── Security · 行为审计日志（F-1 P0 批首个域） ───────────────────────────

/// <summary>
/// 工具接口：security.query_audit_logs（映射 <c>IFeishuTenantV1SecurityAuditLog.GetAuditLogDataAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>F-1 P0 批（Security · 合规敏感）</b>：把"谁在什么时候动了哪份文档 / 权限 / 应用"变成可查事实——
/// 合规排查、离职审计、异常操作复盘都用它。此前该域零覆盖，模型只能靠用户口述。
/// </para>
/// <para>
/// <b>PII 纪律（DP-A5-1 档位 ①）</b>：结果含<b>他人</b>的成员 ID、IP、地理位置与设备信息 ⇒
/// 属 PII 出口：<b>默认不启用</b>（需宿主显式加入 <c>FeishuAgent:Tools</c>），且描述中必须声明
/// "结果进第三方模型上下文"的后果（由 <c>PiiToolDisciplineContractGuards</c> 机械锁定）。
/// </para>
/// <para>
/// <b>投影纪律</b>：只回填排查所需的判断依据（事件 / 操作者 / 对象 / 接收者 / 时间 / IP / 城市 / 设备型号 / OS），
/// <b>有意丢弃</b> Mac 地址（<c>audit_detail.mc</c>）与操作人所在企业编号——前者是最强设备指纹、
/// 后者对本类排查无增量信息，回填只扩大泄露面。
/// </para>
/// <para>
/// <b>有意不策展</b>：同域的 OpenAPI 审计日志（<c>ListOpenApiLogDataAsync</c>）留待下一片；
/// 设备管理（device_record / device_apply_record）与用户数据迁移（user_migration）均为<b>写类</b>，另立批次。
/// </para>
/// </remarks>
[FeishuTool("security.query_audit_logs",
    Description = "查询企业成员的【行为审计日志】——谁在什么时间、从哪个 IP/城市、对哪个对象（文档/会话/权限/应用）做了什么（事件名如 space_edit_doc、permission_change）。用于合规排查与异常操作复盘。⚠️ 该工具属 PII：结果含成员 user_id、IP 与设备/地理位置，会进入【第三方模型上下文】；【默认不启用】，需宿主显式加入白名单并确认合规口径。时间范围起止相差不能超过 30 天；建议 user_id_type 传 open_id（否则需字段权限 contact:user.employee_id:readonly）。只读，需 admin:audit_info:readonly。",
    RequiredScopes = ["admin:audit_info:readonly"],
    Source = nameof(IFeishuTenantV1SecurityAuditLog) + "." + nameof(IFeishuTenantV1SecurityAuditLog.GetAuditLogDataAsync))]
public interface IFeishuTenantSecurityQueryAuditLogsTool
{
    /// <summary>查询行为审计日志（分页）。</summary>
    /// <returns>白名单投影后的 JSON 文本（分页信封 items/has_more/page_token；条目含 event_id/unique_id/event_name/event_module/operator_type/operator_value/event_time/ip/department_ids/objects/recipients/audit_detail），超长截断并标记 truncated。</returns>
    Task<string> QueryAuditLogsAsync(
        [ToolParameter("oldest", "起始时间（可选，秒级时间戳；缺省=30 天前；与 latest 相差不能超过 30 天）")] int? oldest = null,
        [ToolParameter("latest", "结束时间（可选，秒级时间戳；缺省=当前时刻）")] int? latest = null,
        [ToolParameter("event_name", "事件名称精确筛选（可选，如 space_edit_doc / permission_change；名称对照飞书枚举值附录）")] string? event_name = null,
        [ToolParameter("event_module", "事件模块筛选（可选，整数，含义对照飞书枚举值附录）")] int? event_module = null,
        [ToolParameter("operator_type", "操作者类型（可选：user=用户 / bot=机器人；填 operator_value 时本项必填）")] string? operator_type = null,
        [ToolParameter("operator_value", "操作者值（可选；与 operator_type 成对使用）")] string? operator_value = null,
        [ToolParameter("user_id_type", "返回 ID 类型（可选：open_id / union_id / user_id；默认 user_id，需字段权限 contact:user.employee_id:readonly；建议传 open_id 以便与其它工具衔接）")] string? user_id_type = null,
        [ToolParameter("page_token", "分页游标（可选，来自上一次结果的 page_token）")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
