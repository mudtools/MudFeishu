// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// workflow 域：跨工具的场景级编排（复合工具）。
//
// 为什么是「普通工具」而不是新框架：工作流的每一步都经 FeishuToolBinding 执行——授权/审计/净化
// 与原子工具完全同一条链，模型选工作流 = 选整条链，不存在旁路。定义以编译期常量落地（AOT 友好）。
// 两个工作流都只读：产出是结构化报告与 Markdown，需要落盘时由模型改用 docx.* / markdown.*。

/// <summary>工具接口：workflow.meeting_summary（妙记搜索 → 逐场智能产物 → 结构化纪要汇总）。</summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：模型手拼「搜索 → 逐个读摘要 → 归纳」时步骤顺序与参数映射每次都不同，
/// 结果不可复现、无法审计到具体链路。工作流把这条链固化为声明式步骤，
/// 每步仍是受控工具调用（各自的 scope / 审计 / 净化全部生效）。
/// </para>
/// <para>
/// <b>无 <c>Source</c></b>：工作流不对应单个平台 API，而是两步 SDK 工具的组合；
/// 生成器对无 <c>Source</c> 的工具取 <c>risk=read</c> / <c>identity=tenant</c>，与只读语义一致。
/// </para>
/// <para>
/// <b>PII 口径</b>：妙记智能产物含参会人发言内容与待办（个人信息），报告会进入第三方模型上下文，
/// 故该工具按 <c>defaultDisabled=true</c> 登记，需宿主显式启用并确认合规口径。
/// </para>
/// </remarks>
[AgentTool("workflow.meeting_summary",
    Description = "一键汇总一段时间内的会议纪要：按关键词与时间窗搜索妙记，再逐个提取 AI 摘要、章节、待办与关键词，合成结构化 Markdown 报告。报告含参会人发言与待办（PII），结果会进入第三方模型上下文：默认不启用，需宿主显式启用并确认合规口径。支持 dry_run 预演与 resume_token 断点续跑。只读，需 minutes:minutes:readonly。",
    RequiredScopes = ["minutes:minutes:readonly"],
    IsWrite = false)]
// Source 刻意留空：本工具编排的是既有 minutes.* 工具，不直接映射任何 SDK 方法。
public interface IFeishuTenantWorkflowMeetingSummaryTool
{
    /// <summary>汇总会议纪要。</summary>
    /// <returns>结构化 JSON（meetings / report_markdown / completed_steps / resumed）；失败时附 resume_token。</returns>
    Task<string> SummarizeAsync(
        [ToolParameter("query", "搜索关键词（10~50 字符，通常是会议主题词；平台约束）", Required = true)] string query,
        [ToolParameter("start_time", "创建时间起点（RFC3339，如 2026-10-01T00:00:00+08:00；可选；窗口最长 1 个月）")] string? start_time = null,
        [ToolParameter("end_time", "创建时间终点（RFC3339；可选；与 start_time 成对使用）")] string? end_time = null,
        [ToolParameter("max_meetings", "最多汇总的会议数（可选，默认 5，范围 1~20；结果受出站长度预算约束）")] int? max_meetings = null,
        [ToolParameter("dry_run", "仅预演整条链（可选，默认 false）：返回步骤与参数预览，不调用任何下游工具")] bool? dry_run = null,
        [ToolParameter("resume_token", "断点续跑令牌（可选）：原样传入上一次失败结果中的 resume_token，跳过已完成步骤；仅同一工作流、同一组输入有效")] string? resume_token = null,
        CancellationToken cancellationToken = default);
}

/// <summary>工具接口：workflow.standup_report（日历议程 + 我的任务 → 站会日报）。</summary>
/// <remarks>
/// <para>
/// <b>身份前提</b>：第二步 <c>task.list_my_tasks</c> 是 <c>identity=user</c> 工具——宿主必须
/// 在执行上下文里提供当前用户 <c>UserId</c>（open_id），并在 <c>AllowedIdentities</c> 放行
/// <c>user</c>；缺任一项时嵌套绑定返回可执行的结构化错误，工作流原样转发。
/// </para>
/// <para>
/// <b>无 <c>Source</c></b>：同 <see cref="IFeishuTenantWorkflowMeetingSummaryTool"/>——工作流是
/// 工具组合，不直接映射 SDK 方法。
/// </para>
/// </remarks>
[AgentTool("workflow.standup_report",
    Description = "一键生成站会日报：读取指定日历时间窗内的日程，再以当前用户身份列出「我负责的」任务，汇总为 Markdown 日报。依赖 calendar.agenda 与 task.list_my_tasks 同时启用，且宿主须提供当前用户 open_id（FeishuToolContext.UserId）并在 AllowedIdentities 放行 user。支持 dry_run 预演与 resume_token 断点续跑。只读，需 calendar:calendar:readonly 与 task:task:readonly。",
    RequiredScopes = ["calendar:calendar:readonly", "task:task:readonly"],
    IsWrite = false)]
// Source 刻意留空：本工具编排 calendar.agenda + task.list_my_tasks，不直接映射 SDK 方法。
public interface IFeishuTenantWorkflowStandupReportTool
{
    /// <summary>生成站会日报。</summary>
    /// <returns>结构化 JSON（agenda / tasks / report_markdown / completed_steps / resumed）；失败时附 resume_token。</returns>
    Task<string> GenerateAsync(
        [ToolParameter("calendar_id", "日历 ID（形如 feishu.cn_xxx@group.calendar.feishu.cn）", Required = true)] string calendar_id,
        [ToolParameter("start_time", "时间窗起点（RFC3339，含时区，如 2026-10-20T00:00:00+08:00）", Required = true)] string start_time,
        [ToolParameter("end_time", "时间窗终点（RFC3339；须晚于 start_time）", Required = true)] string end_time,
        [ToolParameter("include_completed_tasks", "任务列表是否包含已完成任务（可选，默认 false）")] bool? include_completed_tasks = null,
        [ToolParameter("max_tasks", "最多汇总的任务数（可选，默认 20，范围 1~100；结果受出站长度预算约束）")] int? max_tasks = null,
        [ToolParameter("dry_run", "仅预演整条链（可选，默认 false）：返回步骤与参数预览，不调用任何下游工具")] bool? dry_run = null,
        [ToolParameter("resume_token", "断点续跑令牌（可选）：原样传入上一次失败结果中的 resume_token，跳过已完成步骤；仅同一工作流、同一组输入有效")] string? resume_token = null,
        CancellationToken cancellationToken = default);
}
