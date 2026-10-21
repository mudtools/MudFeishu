// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.AI.Tools.Catalog;
using Mud.Feishu.AI.Tools.Workflows;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// workflow.* 执行器（S-4 复合工作流）：把定义表交给 <see cref="FeishuWorkflowRunner"/> 顺序执行，
/// 再把「步骤轨迹 + 报告」投影成工具结果。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么执行器这么薄</b>：编排、失败中断、续跑坐标都在 runner 里（与工具面解耦、可单测）；
/// 执行器只做三件产品侧的事——入参校验/夹逼（<c>page_size</c>、<c>transcript_limit</c>、<c>start_at</c>）、
/// 解析定义、把结果与失败分别投影成 <c>OkJson</c> / 结构化错误。
/// </para>
/// <para>
/// <b>为什么注入 <see cref="IServiceProvider"/> 而不是 <see cref="FeishuToolRegistry"/></b>：
/// 注册表由 <c>BuildRegistry</c> 构造，而域注册器又要在构造过程中解析本执行器——
/// 直接注入注册表会形成「执行器 → 注册表 → 注册器 → 执行器」的 DI 环（实测无限递归：
/// 转储里可见 <c>AddFeishuWorkflowToolsCore</c> 的工厂帧反复入栈）。注册表只在<b>执行期</b>才会被
/// 用到（解析步骤工具），故改为按需从根提供器取——构造期零依赖，环自然消失。
/// </para>
/// <para>
/// <b>失败必须是失败</b>：中途失败的链路返回带 <c>Error</c> 载荷的结果（并附 <c>resume_from</c>），
/// 不能返回"看起来成功的半份报告"——模型会把半份报告当成完整结论继续推理。
/// </para>
/// </remarks>
/// <param name="options">Agent 配置（取结果长度预算）。</param>
/// <param name="serviceProvider">根服务提供器（执行期按需取注册表；构造期不使用）。</param>
/// <param name="toolContextAccessor">会话上下文访问器（可空软缺席）。</param>
internal sealed class WorkflowTools(
    IOptions<FeishuAgentOptions> options,
    IServiceProvider serviceProvider,
    IFeishuToolContextAccessor? toolContextAccessor = null)
{
    /// <summary>任务条数上限（防止模型把预算填成 9999 把上下文打爆）。</summary>
    private const int MaxTaskPageSize = 100;

    /// <summary>逐字稿窗口上限（与 <c>minutes.get_artifacts</c> 的平台上限一致）。</summary>
    private const int MaxTranscriptLimit = 50000;

    /// <summary>逐字稿窗口默认值（一次能装进上下文又不至于挤掉别的信息）。</summary>
    private const int DefaultTranscriptLimit = 6000;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IFeishuToolContextAccessor? _toolContextAccessor = toolContextAccessor;

    /// <summary>workflow.meeting_summary：妙记详情 → 统计 → 逐字稿窗口 → 结构化报告。</summary>
    /// <param name="arguments">模型入参。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>工作流结果（成功 = 报告 JSON；失败 = 结构化错误 + 续跑坐标）。</returns>
    [ToolHandler(typeof(IFeishuTenantWorkflowMeetingSummaryTool))]
    public Task<ToolResult> RunMeetingSummaryAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WorkflowMeetingSummary, _maxResultLength);

        return executor.RunAsync(async () =>
        {
            var args = WorkflowMeetingSummaryArgs.Unpack(arguments);
            var minuteToken = RequireToken(args.MinuteToken);

            var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["minute_token"] = minuteToken,
                ["transcript_limit"] = Clamp(args.TranscriptLimit, 1, MaxTranscriptLimit, DefaultTranscriptLimit),
            };

            var includeStatistics = args.IncludeStatistics ?? true;
            return await RunCoreAsync(
                executor,
                FeishuWorkflowCatalog.MeetingSummary,
                parameters,
                args.StartAt,
                args.DryRun,
                includeStatistics,
                cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>workflow.standup_report：日程议程 → 我的任务 → 站会材料。</summary>
    /// <param name="arguments">模型入参。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>工作流结果（成功 = 报告 JSON；失败 = 结构化错误 + 续跑坐标）。</returns>
    [ToolHandler(typeof(IFeishuTenantWorkflowStandupReportTool))]
    public Task<ToolResult> RunStandupReportAsync(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WorkflowStandupReport, _maxResultLength);

        return executor.RunAsync(async () =>
        {
            var args = WorkflowStandupReportArgs.Unpack(arguments);

            var calendarId = Require(executor.ToolName, args.CalendarId, "calendar_id");
            var start = Require(executor.ToolName, args.Start, "start");
            var end = Require(executor.ToolName, args.End, "end");

            var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["calendar_id"] = calendarId,
                ["start"] = start,
                ["end"] = end,
                // 模型只给"预算条数"，页大小是平台实现细节（DP-B1-0：页不进、预算进）。
                ["max_items"] = Clamp(args.MaxTasks, 1, MaxTaskPageSize, 50),
            };

            return await RunCoreAsync(
                executor,
                FeishuWorkflowCatalog.StandupReport,
                parameters,
                args.StartAt,
                args.DryRun,
                includeStatistics: true,
                cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>执行定义并把结论投影成工具结果（成功/失败两条出口）。</summary>
    private async Task<ToolResult> RunCoreAsync(
        ToolExecutor executor,
        string workflowToolName,
        IReadOnlyDictionary<string, object?> parameters,
        int? startAt,
        bool? dryRun,
        bool includeStatistics,
        CancellationToken cancellationToken)
    {
        var definition = FeishuWorkflowCatalog.TryGet(workflowToolName)
            ?? throw new ArgumentException("未知的工作流定义（装配错误）", nameof(workflowToolName));

        var effectiveStartAt = Math.Max(1, startAt ?? 1);
        var effectiveDryRun = dryRun ?? false;

        // include_statistics=false 时把第 2 步整体跳过（0 = 不执行任何步骤，改用"从第 3 步开始"实现）。
        var steps = definition;
        if (!includeStatistics && workflowToolName == FeishuWorkflowCatalog.MeetingSummary && effectiveStartAt <= 2)
        {
            steps = definition with
            {
                Steps = definition.Steps.Where(static s => s.Index != 2).ToArray(),
            };
        }

        var context = _toolContextAccessor?.Current
            ?? throw new InvalidOperationException("工作流执行需要 FeishuToolContext（由执行链注入）");

        // 执行期才取注册表（构造期取会形成 DI 环，见类注释）。
        var registry = (FeishuToolRegistry)_serviceProvider.GetService(typeof(FeishuToolRegistry))!;
        var runner = new FeishuWorkflowRunner(registry);

        var run = await runner
            .RunAsync(steps, parameters, context, effectiveStartAt, effectiveDryRun, cancellationToken)
            .ConfigureAwait(false);

        var payload = BuildPayload(run);
        if (!run.Failed)
        {
            return ToolResultPipeline.OkJson(payload, _maxResultLength);
        }

        var failed = run.Outcomes.LastOrDefault(static o => o.Status == FeishuWorkflowStepStatuses.Failed);
        var reason = $"工作流 {workflowToolName} 在步骤 {failed?.Index}（{failed?.ToolName}）失败："
            + $"resume_from={run.ResumeFrom} 可断点续跑；详情 "
            + payload.ToJsonString();

        return ToolResultPipeline.Error(
            workflowToolName,
            ToolErrorCategory.Api,
            ToolErrorSubtype.UpstreamError,
            reason,
            _maxResultLength);
    }

    private static JsonObject BuildPayload(FeishuWorkflowRun run)
    {
        var steps = new JsonArray();
        foreach (var outcome in run.Outcomes)
        {
            // AddNode（而非 Add<T>）：泛型 Add<T>(T) 带 RequiresUnreferencedCode/RequiresDynamicCode，
            // AOT 严格模式下直接报红（本仓既有纪律见 ToolResultText.AddNode）。
            steps.AddNode(new JsonObject
            {
                ["index"] = outcome.Index,
                ["tool"] = outcome.ToolName,
                ["purpose"] = outcome.Purpose,
                ["status"] = outcome.Status,
                ["duration_ms"] = outcome.DurationMs,
                ["error"] = outcome.Error,
            });
        }

        return new JsonObject
        {
            ["workflow"] = run.Definition.ToolName,
            ["title"] = run.Definition.Title,
            ["dry_run"] = run.DryRun,
            ["status"] = run.Failed ? "failed" : "ok",
            ["steps"] = steps,
            ["report"] = run.Report,
            ["resume_from"] = run.ResumeFrom,
        };
    }

    private static string RequireToken(string? minuteToken)
        => string.IsNullOrWhiteSpace(minuteToken)
            ? throw new ArgumentException("缺少必填参数 minute_token（形如 obcnXxx）")
            : minuteToken.Trim();

    private static string Require(string toolName, string? value, string parameter)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"工具 {toolName} 缺少必填参数 {parameter}")
            : value.Trim();

    private static int Clamp(int? value, int min, int max, int fallback)
    {
        var candidate = value ?? fallback;
        return candidate < min ? min : candidate > max ? max : candidate;
    }
}
