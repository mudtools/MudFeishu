// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

/// <summary>
/// 工具接口：workflow.meeting_summary（复合工作流：妙记详情 → 统计 → 逐字稿窗口 → 结构化报告）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是工作流而不是原子工具</b>：模型若自己连调四个工具，中间任何一步的入参错误都会让它
/// 重新规划整条链（步数翻倍、token 翻倍）。工作流把"取哪几段、怎么拼"固化在代码里，
/// 模型只给起点参数，拿到的是<b>一份可读报告 + 明确的续读坐标</b>。
/// </para>
/// <para>
/// <b>执行语义</b>：每一步都是<b>受控工具调用</b>——走 <c>FeishuToolBinding</c> 的完整链路
/// （授权门、用户令牌预检、结果净化、审计、重试、预算），工作流本身<b>不绕过</b>任何一环，
/// 因此审计里能看到 <c>步骤工具名</c> 逐条记录（不是一条"workflow"黑盒事件）。
/// </para>
/// <para>
/// <b>平台边界（诚实说明）</b>：飞书妙记<b>没有</b>"摘要"接口，本工作流产出的是
/// 「元信息 + 发言统计 + 逐字稿窗口」的事实报告，摘要与待办提取由模型基于报告完成
/// （报告里给出续读 offset 与提取口径提示）。
/// </para>
/// </remarks>
[AgentTool("workflow.meeting_summary",
    Description = "妙记速览工作流：给定 minute_token，一次完成「妙记元信息 + 发言统计 + 逐字稿窗口」的取数与结构化汇总，返回可读报告（含逐字稿 total_length / next_offset，长录音可按 offset 续读）。摘要与待办提取由你基于报告内容完成（飞书未提供摘要接口）。只读链，需 minutes:minutes:readonly。",
    RequiredScopes = ["minutes:minutes:readonly"])]
public interface IFeishuTenantWorkflowMeetingSummaryTool
{
    /// <summary>执行妙记速览工作流。</summary>
    /// <returns>白名单投影后的 JSON 文本（steps 步骤轨迹 + report 汇总 + resume_from 续跑坐标）。</returns>
    Task<string> RunMeetingSummaryAsync(
        [ToolParameter("minute_token", "妙记 token（形如 obcnXxx）", Required = true)] string minute_token,
        [ToolParameter("include_statistics", "是否取发言统计（可选，默认 true）；关掉可省一次调用")] bool? include_statistics = null,
        [ToolParameter("transcript_limit", "逐字稿窗口字符数（可选，默认 6000，范围 1~50000）")] int? transcript_limit = null,
        [ToolParameter("start_at", "从第几步开始执行（1 起，默认 1；上一轮结果里的 resume_from 直接填这里做断点续跑）")] int? start_at = null,
        [ToolParameter("dry_run", "只返回执行计划不实际取数（可选，默认 false）")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：workflow.standup_report（复合工作流：日历议程 → 我的任务 → 站会报告）。
/// </summary>
/// <remarks>
/// <para>
/// <b>跨域只读聚合</b>：议程（<c>calendar.agenda</c>，租户身份）与我的任务（<c>task.list_my_tasks</c>，
/// 用户身份）来自两个域、两种令牌。工作流负责"两次取数的顺序与拼装"，令牌语义仍由各自步骤的
/// 预检决定（缺用户令牌时该步骤失败并给出 <c>resume_from</c>，不会把整条链拖垮成黑盒）。
/// </para>
/// <para>
/// <b>为什么把 page_size 暴露给模型</b>：任务量差异很大（5 条 vs 200 条），固定值要么漏要么爆预算；
/// 上限由执行器夹逼（≤100），不交给模型自由决定。
/// </para>
/// </remarks>
[AgentTool("workflow.standup_report",
    Description = "站会报告工作流：给定日历与时间窗，取回窗内日程 + 我负责的任务，输出结构化站会材料（今日议程 / 未完成任务 / 建议关注项）。时间窗用 RFC3339（含时区）。只读链，需 calendar:calendar:readonly + task:task:readonly。",
    RequiredScopes = ["calendar:calendar:readonly", "task:task:readonly"])]
public interface IFeishuTenantWorkflowStandupReportTool
{
    /// <summary>执行站会报告工作流。</summary>
    /// <returns>白名单投影后的 JSON 文本（steps 步骤轨迹 + report 汇总 + resume_from 续跑坐标）。</returns>
    Task<string> RunStandupReportAsync(
        [ToolParameter("calendar_id", "日历 ID（形如 feishu.cn_xxx@group.calendar.feishu.cn）", Required = true)] string calendar_id,
        [ToolParameter("start", "窗口起点（RFC3339，含时区，如 2026-10-20T00:00:00+08:00）", Required = true)] string start,
        [ToolParameter("end", "窗口终点（RFC3339，须晚于 start）", Required = true)] string end,
        [ToolParameter("max_tasks", "一次最多取多少条任务（可选，默认 50，上限 100；这是**预算**不是页大小，翻页由执行器按平台页大小处理）")] int? max_tasks = null,
        [ToolParameter("start_at", "从第几步开始执行（1 起，默认 1；上一轮结果里的 resume_from 直接填这里做断点续跑）")] int? start_at = null,
        [ToolParameter("dry_run", "只返回执行计划不实际取数（可选，默认 false）")] bool? dry_run = null,
        CancellationToken cancellationToken = default);
}
