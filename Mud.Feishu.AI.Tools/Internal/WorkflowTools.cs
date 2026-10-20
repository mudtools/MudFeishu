// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// workflow 域执行器：把跨工具的多步编排落成普通工具（<c>workflow.meeting_summary</c> /
/// <c>workflow.standup_report</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>零旁路</b>：嵌套调用取注册表中的 <see cref="FeishuToolDefinition.Handler"/>——该委托由
/// <c>FeishuToolRegistration.RegisterExecution</c> 包成 <c>FeishuToolBinding.ExecuteAsync</c>，
/// 授权 / 审计 / 净化 / 令牌前置探测逐步骤生效。<see cref="FeishuToolRegistry.TryGet"/> 对
/// 「已注册未启用」同样成功，故必须叠加 <see cref="FeishuToolRegistry.IsEnabled"/> 校验，
/// 否则工作流会成为禁用工具的后门。
/// </para>
/// <para>
/// <b>执行期解析注册表</b>：注册表构建要枚举域注册器，构造期反查会自我闭环（同
/// <see cref="ToolSearchTools"/> 的警示）。
/// </para>
/// <para>
/// <b>断点续跑</b>：<c>resume_token</c> = Base64(UTF-8 JSON)，携带版本 / 工作流 / 输入指纹 /
/// 已完成步骤数据，无服务端状态。只对同一工作流且同一组输入有效；工作流只读，重放无副作用。
/// </para>
/// </remarks>
internal sealed class WorkflowTools(
    IOptions<FeishuAgentOptions> options,
    IServiceProvider serviceProvider)
{
    private const int ResumeTokenVersion = 1;

    // 令牌与失败正文共享 MaxToolResultLength 预算（默认 4000）；两者之和超预算即被截断，
    // 而截断后的令牌不可用——故令牌上限必须显著小于结果预算。
    private const int MaxResumeTokenChars = 2400;
    private const int MaxResumePayloadBytes = 1700;

    private const int DefaultMeetingCount = 5;
    private const int MaxMeetingCount = 20;
    private const int DefaultTaskCount = 20;
    private const int MaxTaskCount = 100;

    private const int MaxSummaryChars = 320;
    private const int MaxTitleChars = 120;
    private const int MaxKeywordChars = 30;
    private const int MaxKeywordCount = 8;
    private const int MaxChapterTitles = 6;
    private const int MaxTodoItems = 8;
    private const int MaxTodoChars = 120;
    private const int MaxTaskSummaryChars = 160;
    private const int MaxAgendaItems = 40;
    private const int MaxReportChars = 1800;
    private const int MaxFailureDetailChars = 240;

    private static readonly WorkflowStep SearchStep =
        new("search", FeishuToolNames.MinutesSearch, "按关键词与时间窗搜索妙记");

    private static readonly WorkflowStep ArtifactsStep =
        new("artifacts", FeishuToolNames.MinutesGetArtifacts, "逐个提取 AI 摘要 / 章节 / 待办 / 关键词");

    private static readonly WorkflowStep AgendaStep =
        new("agenda", FeishuToolNames.CalendarAgenda, "读取时间窗内的日历议程");

    private static readonly WorkflowStep TasksStep =
        new("tasks", FeishuToolNames.TaskListMyTasks, "以当前用户身份读取「我负责的」任务");

    private static readonly WorkflowStep[] MeetingSteps = [SearchStep, ArtifactsStep];
    private static readonly WorkflowStep[] StandupSteps = [AgendaStep, TasksStep];

    private readonly FeishuAgentOptions _options = ToolExecutor.Require(options);
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private FeishuToolRegistry ToolRegistry => _serviceProvider.GetRequiredService<FeishuToolRegistry>();

    private IFeishuToolContextAccessor ContextAccessor =>
        _serviceProvider.GetRequiredService<IFeishuToolContextAccessor>();

    // ─────────────────────────── workflow.meeting_summary ───────────────────────────

    /// <summary><c>workflow.meeting_summary</c>：妙记搜索 → 智能产物 → 结构化纪要报告。</summary>
    [ToolHandler(typeof(IFeishuTenantWorkflowMeetingSummaryTool))]
    public Task<ToolResult> SummarizeMeetingAsync(
        IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WorkflowMeetingSummary, _options.MaxToolResultLength);
        return executor.RunAsync(() => RunMeetingSummaryAsync(executor.ToolName, arguments, cancellationToken));
    }

    private async Task<ToolResult> RunMeetingSummaryAsync(
        string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var args = WorkflowMeetingSummaryArgs.Unpack(arguments);

        var query = args.Query.Trim();
        if (query.Length is < 10 or > 50)
        {
            throw new ArgumentException("query 长度须在 10~50 字符之间（minutes.search 平台约束）；可换更具体的会议主题词");
        }

        var (startTime, endTime) = NormalizeMeetingWindow(args.StartTime, args.EndTime);
        var maxMeetings = Bound(args.MaxMeetings, DefaultMeetingCount, MaxMeetingCount);
        var dryRun = ToolDryRun.IsRequested(args.DryRun);
        var fingerprint = Fingerprint(
            ("query", query),
            ("start_time", startTime ?? string.Empty),
            ("end_time", endTime ?? string.Empty),
            ("max_meetings", maxMeetings.ToString(CultureInfo.InvariantCulture)));

        var completed = new List<CompletedStep>();
        if (!dryRun && !string.IsNullOrWhiteSpace(args.ResumeToken))
        {
            completed = DecodeResumeToken(args.ResumeToken!, toolName, fingerprint, MeetingSteps);
        }

        if (dryRun)
        {
            return ToolResultPipeline.OkJson(BuildDryRunEnvelope(
                toolName, MeetingSteps, completed, new JsonObject
                {
                    ["query_length"] = query.Length,
                    ["start_time"] = startTime,
                    ["end_time"] = endTime,
                    ["max_meetings"] = maxMeetings,
                }), _options.MaxToolResultLength);
        }

        if (EnsureStepsEnabled(toolName, MeetingSteps) is { } blocked)
        {
            return blocked;
        }

        var context = ContextAccessor.Current;
        if (context is null)
        {
            return ContextMissing(toolName);
        }

        var resumedSearch = FindCompleted(completed, SearchStep.Id);
        List<MeetingRef> meetingsRef;
        if (resumedSearch is not null)
        {
            meetingsRef = ReadMeetingRefsFromResume(resumedSearch);
            if (meetingsRef.Count > maxMeetings)
            {
                meetingsRef = meetingsRef.GetRange(0, maxMeetings);
            }
        }
        else
        {
            var searchOutcome = await InvokeStepAsync(
                SearchStep, BuildSearchArguments(query, startTime, endTime, maxMeetings), context, cancellationToken)
                .ConfigureAwait(false);
            if (searchOutcome.IsFailure)
            {
                return BuildFailure(toolName, fingerprint, completed, SearchStep, searchOutcome, "步骤 1/2");
            }

            if (!TryReadMeetingRefs(searchOutcome.Data!, maxMeetings, out meetingsRef, out var shapeError))
            {
                return DependencyShapeError(toolName, SearchStep, shapeError!);
            }

            completed.Add(new CompletedStep(SearchStep.Id, new JsonObject
            {
                ["minute_tokens"] = BuildMinuteTokenArray(meetingsRef),
            }));
        }

        var resumed = resumedSearch is not null;
        var meetings = new JsonArray();
        var report = new StringBuilder();
        report.Append("# 会议纪要汇总\n");
        if (startTime is not null)
        {
            report.Append("时间窗：").Append(startTime).Append(" ~ ").Append(endTime).Append('\n');
        }

        report.Append("会议数：").Append(meetingsRef.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');

        var reportTruncated = false;
        var nestedTruncated = false;

        for (var index = 0; index < meetingsRef.Count; index++)
        {
            var meeting = meetingsRef[index];
            var artifactOutcome = await InvokeStepAsync(
                ArtifactsStep,
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["minute_token"] = meeting.MinuteToken,
                    // 工作流只要摘要/章节/待办；逐字稿留给 minutes.get_artifacts 按需分段读取。
                    ["transcript_limit"] = 1,
                },
                context,
                cancellationToken).ConfigureAwait(false);

            if (artifactOutcome.IsFailure)
            {
                var position = (index + 1).ToString(CultureInfo.InvariantCulture)
                    + "/" + meetingsRef.Count.ToString(CultureInfo.InvariantCulture) + " 个妙记";
                return BuildFailure(toolName, fingerprint, completed, ArtifactsStep, artifactOutcome, position);
            }

            nestedTruncated |= artifactOutcome.Result.Truncated;

            if (!TryProjectMeeting(meeting, artifactOutcome.Data!, out var entry, out var projectError))
            {
                return DependencyShapeError(toolName, ArtifactsStep, projectError!);
            }

            meetings.AddNode(entry);
            var section = BuildMeetingSection(meetings.Count, meeting.Title, entry!);
            if (!reportTruncated && report.Length + section.Length <= MaxReportChars)
            {
                report.Append(section);
            }
            else
            {
                reportTruncated = true;
            }
        }

        if (reportTruncated)
        {
            report.Append("\n（报告超出长度预算，已省略其余场次；可减小 max_meetings 后重跑。）\n");
        }

        var envelope = new JsonObject
        {
            ["workflow"] = toolName,
            ["query"] = query,
            ["start_time"] = startTime ?? string.Empty,
            ["end_time"] = endTime ?? string.Empty,
            ["meeting_count"] = meetings.Count,
            ["meetings"] = meetings,
            ["report_markdown"] = report.ToString(),
            ["completed_steps"] = BuildStepsArray(MeetingSteps),
            ["resumed"] = resumed,
            ["note"] = "报告需落盘时用 docx.* / markdown.* 工具写入；逐字稿用 minutes.get_artifacts 按 transcript_offset/transcript_limit 分段读取。",
        };

        if (reportTruncated)
        {
            envelope["report_truncated"] = true;
        }

        if (nestedTruncated)
        {
            envelope["nested_truncated"] = true;
        }

        if (meetings.Count == 0)
        {
            envelope["notice"] = "时间窗内没有匹配的妙记；可放宽关键词或扩大时间窗。";
        }

        return ToolResultPipeline.OkJson(envelope, _options.MaxToolResultLength);
    }

    // ─────────────────────────── workflow.standup_report ───────────────────────────

    /// <summary><c>workflow.standup_report</c>：日历议程 + 我的任务 → Markdown 日报。</summary>
    [ToolHandler(typeof(IFeishuTenantWorkflowStandupReportTool))]
    public Task<ToolResult> GenerateStandupAsync(
        IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.WorkflowStandupReport, _options.MaxToolResultLength);
        return executor.RunAsync(() => RunStandupReportAsync(executor.ToolName, arguments, cancellationToken));
    }

    private async Task<ToolResult> RunStandupReportAsync(
        string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var args = WorkflowStandupReportArgs.Unpack(arguments);

        var calendarId = args.CalendarId.Trim();
        var startTime = args.StartTime.Trim();
        var endTime = args.EndTime.Trim();
        var startUtc = ParseRfc3339(startTime, "start_time");
        var endUtc = ParseRfc3339(endTime, "end_time");
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("end_time 须晚于 start_time");
        }

        var includeCompleted = args.IncludeCompletedTasks ?? false;
        var maxTasks = Bound(args.MaxTasks, DefaultTaskCount, MaxTaskCount);
        var dryRun = ToolDryRun.IsRequested(args.DryRun);
        var fingerprint = Fingerprint(
            ("calendar_id", calendarId),
            ("start_time", startTime),
            ("end_time", endTime),
            ("include_completed_tasks", includeCompleted ? "true" : "false"),
            ("max_tasks", maxTasks.ToString(CultureInfo.InvariantCulture)));

        var completed = new List<CompletedStep>();
        if (!dryRun && !string.IsNullOrWhiteSpace(args.ResumeToken))
        {
            completed = DecodeResumeToken(args.ResumeToken!, toolName, fingerprint, StandupSteps);
        }

        if (dryRun)
        {
            return ToolResultPipeline.OkJson(BuildDryRunEnvelope(
                toolName, StandupSteps, completed, new JsonObject
                {
                    ["calendar_id"] = calendarId,
                    ["start_time"] = startTime,
                    ["end_time"] = endTime,
                    ["include_completed_tasks"] = includeCompleted,
                    ["max_tasks"] = maxTasks,
                }), _options.MaxToolResultLength);
        }

        if (EnsureStepsEnabled(toolName, StandupSteps) is { } blocked)
        {
            return blocked;
        }

        var context = ContextAccessor.Current;
        if (context is null)
        {
            return ContextMissing(toolName);
        }

        var resumedAgenda = FindCompleted(completed, AgendaStep.Id);
        JsonObject agendaPage;
        if (resumedAgenda is not null)
        {
            agendaPage = ReadStoredAgenda(resumedAgenda);
        }
        else
        {
            var agendaOutcome = await InvokeStepAsync(
                AgendaStep,
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["calendar_id"] = calendarId,
                    ["start"] = startTime,
                    ["end"] = endTime,
                },
                context,
                cancellationToken).ConfigureAwait(false);

            if (agendaOutcome.IsFailure)
            {
                return BuildFailure(toolName, fingerprint, completed, AgendaStep, agendaOutcome, "步骤 1/2");
            }

            agendaPage = agendaOutcome.Data!;
            completed.Add(new CompletedStep(AgendaStep.Id, ProjectAgendaResume(agendaPage)));
        }

        var taskOutcome = await InvokeStepAsync(
            TasksStep, BuildTaskArguments(includeCompleted, maxTasks), context, cancellationToken)
            .ConfigureAwait(false);
        if (taskOutcome.IsFailure)
        {
            return BuildFailure(toolName, fingerprint, completed, TasksStep, taskOutcome, "步骤 2/2");
        }

        var nestedTruncated = taskOutcome.Result.Truncated;
        if (!TryProjectAgenda(agendaPage, out var agenda, out var agendaTruncated, out var agendaError))
        {
            return DependencyShapeError(toolName, AgendaStep, agendaError!);
        }

        if (!TryProjectTasks(taskOutcome.Data!, maxTasks, out var tasks, out var taskError))
        {
            return DependencyShapeError(toolName, TasksStep, taskError!);
        }

        var report = BuildStandupReport(startTime, endTime, agenda, tasks, out var reportTruncated);

        var envelope = new JsonObject
        {
            ["workflow"] = toolName,
            ["calendar_id"] = calendarId,
            ["start_time"] = startTime,
            ["end_time"] = endTime,
            ["agenda_count"] = agenda.Count,
            ["task_count"] = tasks.Count,
            ["agenda"] = agenda,
            ["tasks"] = tasks,
            ["report_markdown"] = report,
            ["completed_steps"] = BuildStepsArray(StandupSteps),
            ["resumed"] = resumedAgenda is not null,
            ["note"] = "任务列表以当前用户身份（FeishuToolContext.UserId）读取；需在 AllowedIdentities 放行 user。",
        };

        if (agendaTruncated)
        {
            envelope["agenda_truncated"] = true;
        }

        if (reportTruncated)
        {
            envelope["report_truncated"] = true;
        }

        if (nestedTruncated)
        {
            envelope["nested_truncated"] = true;
        }

        return ToolResultPipeline.OkJson(envelope, _options.MaxToolResultLength);
    }

    // ─────────────────────────── 步骤执行 ───────────────────────────

    private sealed record WorkflowStep(string Id, string ToolName, string Purpose);

    private sealed record CompletedStep(string Id, JsonNode? Data);

    private sealed record MeetingRef(string MinuteToken, string? Title);

    private sealed record StepOutcome(WorkflowStep Step, ToolResult Result, JsonObject? Data)
    {
        public bool IsFailure => Result.Error is not null;
    }

    /// <summary>取注册表中的执行入口并调用——该入口已由执行链包裹，禁止绕过它直连执行器。</summary>
    private async Task<StepOutcome> InvokeStepAsync(
        WorkflowStep step,
        IReadOnlyDictionary<string, object?> arguments,
        FeishuToolContext context,
        CancellationToken cancellationToken)
    {
        if (!ToolRegistry.TryGet(step.ToolName, out var definition) || definition is null)
        {
            return new StepOutcome(step, ToolResultPipeline.Error(
                step.ToolName,
                ToolErrorCategory.Internal,
                ToolErrorSubtype.Unexpected,
                $"工具 '{step.ToolName}' 未注册——该域客户端缺席时其工具不进注册表",
                _options.MaxToolResultLength), null);
        }

        var result = await definition.Handler(arguments, context, cancellationToken).ConfigureAwait(false);
        if (result.Error is not null)
        {
            return new StepOutcome(step, result, null);
        }

        if (!TryParseJsonObject(result.Text, out var data))
        {
            return new StepOutcome(step, ToolResultPipeline.Error(
                step.ToolName,
                ToolErrorCategory.Internal,
                ToolErrorSubtype.UpstreamError,
                $"工具 '{step.ToolName}' 返回了非 JSON 结果（可能被出站长度预算截断）——请减小结果规模后重试",
                _options.MaxToolResultLength), null);
        }

        return new StepOutcome(step, result, data);
    }

    /// <summary>依赖门：未注册 / 已注册未启用都在调用前给出可执行错误（不做旁路调用）。</summary>
    private ToolResult? EnsureStepsEnabled(string workflow, IReadOnlyList<WorkflowStep> steps)
    {
        var registry = ToolRegistry;
        foreach (var step in steps)
        {
            if (!registry.TryGet(step.ToolName, out _))
            {
                return ToolResultPipeline.Error(
                    workflow,
                    ToolErrorCategory.Internal,
                    ToolErrorSubtype.Unexpected,
                    $"工作流依赖的工具 '{step.ToolName}' 未注册（该域客户端缺席时其工具不进注册表）——请为宿主启用对应 API 后重新装配",
                    _options.MaxToolResultLength);
            }

            if (!registry.IsEnabled(step.ToolName))
            {
                return ToolResultPipeline.Error(
                    workflow,
                    ToolErrorCategory.Policy,
                    ToolErrorSubtype.ToolNotAllowed,
                    $"工作流依赖的工具 '{step.ToolName}' 已注册但未启用——工作流不做旁路调用；请把 '{step.ToolName}' 加入 {FeishuAgentOptions.SectionName}:{nameof(FeishuAgentOptions.Tools)} 后重试（其余步骤同样需要启用）",
                    _options.MaxToolResultLength,
                    suggestions: [$"在 FeishuAgent:Tools 中加入 {step.ToolName}"]);
            }
        }

        return null;
    }

    private ToolResult ContextMissing(string toolName)
        => ToolResultPipeline.Error(
            toolName,
            ToolErrorCategory.Internal,
            ToolErrorSubtype.Unexpected,
            "执行上下文缺失（IFeishuToolContextAccessor.Current 为 null）——工作流须经宿主 Agent 管线调用，宿主应在 RunAsync 前 Begin 工具上下文",
            _options.MaxToolResultLength);

    private ToolResult DependencyShapeError(string toolName, WorkflowStep step, string reason)
        => ToolResultPipeline.Error(
            toolName,
            ToolErrorCategory.Internal,
            ToolErrorSubtype.UpstreamError,
            $"下游工具 '{step.ToolName}' 的结果无法解析：{reason}。请减小结果规模（降低 max_*）或提高 {FeishuAgentOptions.SectionName}:{nameof(FeishuAgentOptions.MaxToolResultLength)} 后重试",
            _options.MaxToolResultLength);

    /// <summary>
    /// 嵌套失败转发：保留下游载荷（分类 / 子类 / api_code / scopes 原样），正文补工作流上下文与续跑令牌。
    /// </summary>
    private ToolResult BuildFailure(
        string workflow,
        string fingerprint,
        IReadOnlyList<CompletedStep> completed,
        WorkflowStep failedStep,
        StepOutcome outcome,
        string position)
    {
        var nested = outcome.Result.Error!;
        var body = new StringBuilder();
        body.Append("[tool_error] 工作流 ").Append(workflow)
            .Append(" 在步骤 ").Append(failedStep.Id).Append("（").Append(failedStep.ToolName)
            .Append("，").Append(position).Append("）失败。")
            .Append("已完成步骤：")
            .Append(completed.Count == 0 ? "（无）" : string.Join("、", completed.Select(static step => step.Id)))
            .Append("。下游错误：").Append(ClipFailureDetail(outcome.Result.Text));

        var token = completed.Count == 0 ? null : EncodeResumeToken(workflow, fingerprint, completed);
        if (token is not null)
        {
            body.Append("；resume_token=").Append(token)
                .Append("（下次用同一组输入并原样传入该参数，可跳过已完成步骤）");
        }
        else if (completed.Count == 0)
        {
            body.Append("；无可续跑步骤，修正后直接重跑即可（工作流只读）");
        }
        else
        {
            body.Append("；断点数据超过令牌上限，未附带 resume_token——工作流只读，可直接重跑");
        }

        var combined = ToolErrorFactory.Compose(nested, body.ToString());
        return ToolResultPipeline.FromErrorText(
            nested, ToolResultSanitizer.Sanitize(combined), _options.MaxToolResultLength);
    }

    // ─────────────────────────── 投影 ───────────────────────────

    private static Dictionary<string, object?> BuildSearchArguments(
        string query, string? startTime, string? endTime, int maxMeetings)
    {
        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["query"] = query,
            // fetch_all 让聚合层按 max_items 精确截取，而不是拿一整页再本地切片。
            ["fetch_all"] = true,
            ["max_items"] = maxMeetings,
        };

        if (startTime is not null)
        {
            arguments["create_time_start"] = startTime;
            arguments["create_time_end"] = endTime;
        }

        return arguments;
    }

    private static Dictionary<string, object?> BuildTaskArguments(bool includeCompleted, int maxTasks)
    {
        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["fetch_all"] = true,
            ["max_items"] = maxTasks,
        };

        if (includeCompleted)
        {
            arguments["completed"] = true;
        }

        return arguments;
    }

    private static bool TryReadMeetingRefs(
        JsonObject page, int maxMeetings, out List<MeetingRef> meetings, out string? error)
    {
        meetings = [];
        error = null;

        if (page["items"] is not JsonArray items)
        {
            error = "缺少 items 数组（结果可能被出站预算截断）";
            return false;
        }

        foreach (var node in items)
        {
            if (meetings.Count >= maxMeetings)
            {
                break;
            }

            if (node is not JsonObject item || ReadString(item["minute_token"]) is not { Length: > 0 } token)
            {
                continue;
            }

            meetings.Add(new MeetingRef(token, ReadString(item["display_info"])));
        }

        return true;
    }

    private static JsonArray BuildMinuteTokenArray(IReadOnlyList<MeetingRef> meetings)
    {
        var array = new JsonArray();
        foreach (var meeting in meetings)
        {
            array.AddNode(JsonValue.Create(meeting.MinuteToken));
        }

        return array;
    }

    private static bool TryProjectMeeting(
        MeetingRef meeting, JsonObject data, out JsonObject? entry, out string? error)
    {
        entry = null;
        error = null;

        // minutes.get_artifacts 的投影恒有 summary 键（值可为 null）；缺键说明结果被截断。
        if (!data.ContainsKey("summary"))
        {
            error = "缺少 summary 字段（结果可能被出站预算截断）";
            return false;
        }

        var keywords = new JsonArray();
        var keywordCount = 0;
        if (data["keywords"] is JsonArray keywordNodes)
        {
            foreach (var node in keywordNodes)
            {
                keywordCount++;
                if (keywords.Count >= MaxKeywordCount)
                {
                    continue;
                }

                var keyword = ReadString(node);
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keywords.AddNode(JsonValue.Create(Clip(keyword, MaxKeywordChars)));
                }
            }
        }

        var chapters = new JsonArray();
        var chapterCount = 0;
        if (data["chapters"] is JsonArray chapterNodes)
        {
            foreach (var node in chapterNodes)
            {
                chapterCount++;
                if (chapters.Count >= MaxChapterTitles || node is not JsonObject chapter)
                {
                    continue;
                }

                var title = ReadString(chapter["title"]);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    chapters.AddNode(JsonValue.Create(Clip(title, MaxTitleChars)));
                }
            }
        }

        var todos = new JsonArray();
        var todoCount = 0;
        if (data["todos"] is JsonArray todoNodes)
        {
            foreach (var node in todoNodes)
            {
                todoCount++;
                if (todos.Count >= MaxTodoItems || node is not JsonObject todo)
                {
                    continue;
                }

                todos.AddNode(new JsonObject
                {
                    ["content"] = ToolResultText.Truncate(ReadString(todo["content"]), MaxTodoChars),
                    ["is_done"] = ReadBool(todo["is_done"]),
                });
            }
        }

        entry = new JsonObject
        {
            ["minute_token"] = meeting.MinuteToken,
            ["title"] = Clip(meeting.Title, MaxTitleChars),
            ["summary"] = Clip(ReadString(data["summary"]), MaxSummaryChars),
            ["keywords"] = keywords,
            ["keyword_count"] = keywordCount,
            ["chapters"] = chapters,
            ["chapter_count"] = chapterCount,
            ["todos"] = todos,
            ["todo_count"] = todoCount,
        };

        return true;
    }

    private static string BuildMeetingSection(int index, string? title, JsonObject entry)
    {
        var builder = new StringBuilder();
        builder.Append("\n## ").Append(index.ToString(CultureInfo.InvariantCulture)).Append(". ")
            .Append(string.IsNullOrWhiteSpace(title) ? ReadString(entry["minute_token"]) : Clip(title, MaxTitleChars))
            .Append('\n');

        var summary = ReadString(entry["summary"]);
        builder.Append(string.IsNullOrWhiteSpace(summary) ? "（无 AI 摘要）" : summary).Append('\n');

        if (entry["chapters"] is JsonArray { Count: > 0 } chapters)
        {
            builder.Append("- 章节：");
            var first = true;
            foreach (var node in chapters)
            {
                var chapter = ReadString(node);
                if (string.IsNullOrWhiteSpace(chapter))
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(" / ");
                }

                builder.Append(chapter);
                first = false;
            }

            builder.Append('\n');
        }

        if (entry["todos"] is JsonArray { Count: > 0 } todos)
        {
            foreach (var node in todos)
            {
                if (node is not JsonObject todo)
                {
                    continue;
                }

                builder.Append("- [").Append(ReadBool(todo["is_done"]) ? "x" : " ").Append("] ")
                    .Append(ReadString(todo["content"])).Append('\n');
            }
        }

        return builder.ToString();
    }

    private static JsonObject ProjectAgendaResume(JsonObject page)
    {
        var items = new JsonArray();
        var truncated = false;
        if (page["items"] is JsonArray agendaItems)
        {
            foreach (var node in agendaItems)
            {
                if (items.Count >= MaxAgendaItems)
                {
                    truncated = true;
                    break;
                }

                if (node is not JsonObject item)
                {
                    continue;
                }

                items.AddNode(new JsonObject
                {
                    ["event_id"] = ReadString(item["event_id"]),
                    ["summary"] = Clip(ReadString(item["summary"]), MaxTitleChars),
                    ["start"] = ReadString(item["start"]),
                    ["end"] = ReadString(item["end"]),
                    ["status"] = ReadString(item["status"]),
                });
            }
        }

        return new JsonObject
        {
            ["items"] = items,
            ["truncated"] = truncated,
        };
    }

    private static bool TryProjectAgenda(
        JsonObject page, out JsonArray agenda, out bool truncated, out string? error)
    {
        agenda = new JsonArray();
        truncated = false;
        error = null;

        if (page["items"] is not JsonArray items)
        {
            error = "缺少 items 数组（结果可能被出站预算截断）";
            return false;
        }

        foreach (var node in items)
        {
            if (agenda.Count >= MaxAgendaItems)
            {
                truncated = true;
                break;
            }

            if (node is not JsonObject item)
            {
                continue;
            }

            agenda.AddNode(new JsonObject
            {
                ["event_id"] = ReadString(item["event_id"]),
                ["summary"] = Clip(ReadString(item["summary"]), MaxTitleChars),
                ["start"] = ReadString(item["start"]),
                ["end"] = ReadString(item["end"]),
                ["status"] = ReadString(item["status"]),
            });
        }

        return true;
    }

    private static bool TryProjectTasks(
        JsonObject page, int maxTasks, out JsonArray tasks, out string? error)
    {
        tasks = new JsonArray();
        error = null;

        if (page["items"] is not JsonArray items)
        {
            error = "缺少 items 数组（结果可能被出站预算截断）";
            return false;
        }

        foreach (var node in items)
        {
            if (tasks.Count >= maxTasks)
            {
                break;
            }

            if (node is not JsonObject item || ReadString(item["task_guid"]) is not { Length: > 0 } taskGuid)
            {
                continue;
            }

            tasks.AddNode(new JsonObject
            {
                ["task_guid"] = taskGuid,
                ["summary"] = Clip(ReadString(item["summary"]), MaxTaskSummaryChars),
                ["due"] = ReadString(item["due"]),
                ["completed"] = !string.IsNullOrEmpty(ReadString(item["completed_at"])),
            });
        }

        return true;
    }

    private static string BuildStandupReport(
        string startTime, string endTime, JsonArray agenda, JsonArray tasks, out bool reportTruncated)
    {
        reportTruncated = false;
        var report = new StringBuilder();
        report.Append("# 站会日报\n时间窗：").Append(startTime).Append(" ~ ").Append(endTime).Append('\n');
        report.Append("\n## 我的日程（").Append(agenda.Count.ToString(CultureInfo.InvariantCulture)).Append("）\n");

        foreach (var node in agenda)
        {
            if (node is not JsonObject item)
            {
                continue;
            }

            var line = "- " + FormatTime(ReadString(item["start"])) + " - " + FormatTime(ReadString(item["end"]))
                + " " + (ReadString(item["summary"]) ?? "(无标题)") + "\n";
            if (report.Length + line.Length > MaxReportChars)
            {
                reportTruncated = true;
                break;
            }

            report.Append(line);
        }

        report.Append("\n## 我的任务（").Append(tasks.Count.ToString(CultureInfo.InvariantCulture)).Append("）\n");
        foreach (var node in tasks)
        {
            if (node is not JsonObject item)
            {
                continue;
            }

            var due = ReadString(item["due"]);
            var line = "- [" + (ReadBool(item["completed"]) ? "x" : " ") + "] "
                + (ReadString(item["summary"]) ?? "(无标题)")
                + (string.IsNullOrEmpty(due) ? string.Empty : "（截止：" + FormatTime(due) + "）") + "\n";
            if (report.Length + line.Length > MaxReportChars)
            {
                reportTruncated = true;
                break;
            }

            report.Append(line);
        }

        if (reportTruncated)
        {
            report.Append("\n（报告超出长度预算，已省略其余条目；可减小 max_tasks 后重跑。）\n");
        }

        return report.ToString();
    }

    private static JsonArray BuildStepsArray(IReadOnlyList<WorkflowStep> steps)
    {
        var array = new JsonArray();
        foreach (var step in steps)
        {
            array.AddNode(new JsonObject
            {
                ["id"] = step.Id,
                ["tool"] = step.ToolName,
                ["status"] = "completed",
            });
        }

        return array;
    }

    private static JsonObject BuildDryRunEnvelope(
        string workflow,
        IReadOnlyList<WorkflowStep> steps,
        IReadOnlyList<CompletedStep> completed,
        JsonObject inputs)
    {
        var stepArray = new JsonArray();
        foreach (var step in steps)
        {
            stepArray.AddNode(new JsonObject
            {
                ["id"] = step.Id,
                ["tool"] = step.ToolName,
                ["purpose"] = step.Purpose,
                // 预演只回字段名与长度，不回显原始内容（dry_run 不得成为回显通道）。
                ["args_preview"] = DescribeStepArguments(step),
            });
        }

        var completedArray = new JsonArray();
        foreach (var step in completed)
        {
            completedArray.AddNode(JsonValue.Create(step.Id));
        }

        return new JsonObject
        {
            ["dry_run"] = true,
            ["workflow"] = workflow,
            ["inputs"] = inputs,
            ["steps"] = stepArray,
            ["completed_steps"] = completedArray,
            ["note"] = "预演不调用任何下游工具，也不校验下游工具是否启用；去掉 dry_run 后按同样输入执行。",
        };
    }

    private static JsonArray DescribeStepArguments(WorkflowStep step)
    {
        string[] names;
        if (string.Equals(step.Id, "search", StringComparison.Ordinal))
        {
            names = ["query", "create_time_start", "create_time_end", "fetch_all", "max_items"];
        }
        else if (string.Equals(step.Id, "artifacts", StringComparison.Ordinal))
        {
            names = ["minute_token", "transcript_limit"];
        }
        else if (string.Equals(step.Id, "agenda", StringComparison.Ordinal))
        {
            names = ["calendar_id", "start", "end"];
        }
        else
        {
            names = ["fetch_all", "max_items", "completed"];
        }

        var array = new JsonArray();
        foreach (var name in names)
        {
            array.AddNode(JsonValue.Create(name));
        }

        return array;
    }

    // ─────────────────────────── 断点续跑 ───────────────────────────

    private static CompletedStep? FindCompleted(IReadOnlyList<CompletedStep> completed, string stepId)
    {
        foreach (var step in completed)
        {
            if (string.Equals(step.Id, stepId, StringComparison.Ordinal))
            {
                return step;
            }
        }

        return null;
    }

    private static List<MeetingRef> ReadMeetingRefsFromResume(CompletedStep step)
    {
        if (step.Data?["minute_tokens"] is not JsonArray tokens)
        {
            throw new ArgumentException("resume_token 的 search 步骤数据损坏（缺少 minute_tokens）——请去掉 resume_token 从头执行");
        }

        var meetings = new List<MeetingRef>();
        foreach (var node in tokens)
        {
            if (ReadString(node) is { Length: > 0 } token)
            {
                meetings.Add(new MeetingRef(token, null));
            }
        }

        return meetings;
    }

    private static JsonObject ReadStoredAgenda(CompletedStep step)
    {
        if (step.Data?["items"] is not JsonArray)
        {
            throw new ArgumentException("resume_token 的 agenda 步骤数据损坏（缺少 items）——请去掉 resume_token 从头执行");
        }

        return new JsonObject
        {
            ["items"] = step.Data!["items"]!.DeepClone(),
        };
    }

    private static string? EncodeResumeToken(
        string workflow, string fingerprint, IReadOnlyList<CompletedStep> completed)
    {
        var done = new JsonArray();
        foreach (var step in completed)
        {
            done.AddNode(new JsonObject
            {
                ["id"] = step.Id,
                ["data"] = step.Data?.DeepClone(),
            });
        }

        var payload = new JsonObject
        {
            ["v"] = ResumeTokenVersion,
            ["wf"] = workflow,
            ["fp"] = fingerprint,
            ["done"] = done,
        };

        var bytes = Encoding.UTF8.GetBytes(payload.ToJsonString());
        if (bytes.Length > MaxResumePayloadBytes)
        {
            return null;
        }

        return Convert.ToBase64String(bytes);
    }

    private static List<CompletedStep> DecodeResumeToken(
        string token, string workflow, string fingerprint, IReadOnlyList<WorkflowStep> steps)
    {
        if (token.Length > MaxResumeTokenChars)
        {
            throw new ArgumentException(
                $"resume_token 过长（上限 {MaxResumeTokenChars.ToString(CultureInfo.InvariantCulture)} 字符）——请原样回传上一次失败结果中的令牌，不要修改");
        }

        string json;
        try
        {
            json = Encoding.UTF8.GetString(Convert.FromBase64String(token.Trim()));
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("resume_token 不是合法的 Base64——请原样回传上一次失败结果中的令牌", nameof(token), ex);
        }

        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("resume_token 的内容不是合法 JSON——请原样回传上一次失败结果中的令牌", nameof(token), ex);
        }

        if (parsed is not JsonObject payload)
        {
            throw new ArgumentException("resume_token 的内容不是 JSON 对象——请原样回传上一次失败结果中的令牌", nameof(token));
        }

        if (ReadInt(payload["v"]) != ResumeTokenVersion)
        {
            throw new ArgumentException("resume_token 版本不受支持——请去掉该参数从头执行", nameof(token));
        }

        if (!string.Equals(ReadString(payload["wf"]), workflow, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"resume_token 属于另一个工作流（{ReadString(payload["wf"]) ?? "未知"}）——断点续跑只对产生该令牌的工作流有效",
                nameof(token));
        }

        if (!string.Equals(ReadString(payload["fp"]), fingerprint, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "resume_token 与本次输入不匹配——断点续跑只对同一组输入有效；请去掉该参数从头执行",
                nameof(token));
        }

        if (payload["done"] is not JsonArray done)
        {
            throw new ArgumentException("resume_token 缺少 done 数组——请去掉该参数从头执行", nameof(token));
        }

        var completed = new List<CompletedStep>();
        foreach (var node in done)
        {
            if (node is not JsonObject item || ReadString(item["id"]) is not { Length: > 0 } id)
            {
                throw new ArgumentException("resume_token 的 done 条目缺少 id——请去掉该参数从头执行", nameof(token));
            }

            // 已完成的步骤必须是工作流的前缀：令牌只由本执行器生成，非前缀形态必然是伪造/损坏。
            if (completed.Count >= steps.Count
                || !string.Equals(steps[completed.Count].Id, id, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"resume_token 的步骤顺序非法（{id}）——请去掉该参数从头执行", nameof(token));
            }

            completed.Add(new CompletedStep(id, item["data"]?.DeepClone()));
        }

        return completed;
    }

    // ─────────────────────────── 通用 ───────────────────────────

    private static (string? Start, string? End) NormalizeMeetingWindow(string? start, string? end)
    {
        var hasStart = !string.IsNullOrWhiteSpace(start);
        var hasEnd = !string.IsNullOrWhiteSpace(end);
        if (hasStart != hasEnd)
        {
            throw new ArgumentException("start_time 与 end_time 须成对提供（或都不提供）");
        }

        if (!hasStart)
        {
            return (null, null);
        }

        var startTime = start!.Trim();
        var endTime = end!.Trim();
        var startUtc = ParseRfc3339(startTime, "start_time");
        var endUtc = ParseRfc3339(endTime, "end_time");
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("end_time 须晚于 start_time");
        }

        if (endUtc - startUtc > TimeSpan.FromDays(31))
        {
            throw new ArgumentException("minutes.search 的时间窗最长 31 天，请缩小 start_time/end_time 的跨度");
        }

        return (startTime, endTime);
    }

    /// <summary>RFC3339 严格解析（带时区；宽松解析会把无时区值按本地时区折算产生静默偏移）。</summary>
    private static DateTimeOffset ParseRfc3339(string value, string parameterName)
    {
        var trimmed = value.Trim();
        var hasOffset = trimmed.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
            || trimmed.IndexOf('+') > 0
            || trimmed.LastIndexOf('-') > 10; // 日期段的 '-'（位置 4/7）不算时区偏移

        if (!hasOffset
            || !DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new ArgumentException(
                $"{parameterName} 需为带时区的 RFC3339 格式（如 2026-10-01T14:00:00+08:00 或 2026-10-01T14:00:00Z），实际: {trimmed}");
        }

        return parsed;
    }

    private static int Bound(int? value, int defaultValue, int max)
    {
        var effective = value ?? defaultValue;
        return Math.Min(Math.Max(effective, 1), max);
    }

    private static string Fingerprint(params (string Name, string Value)[] fields)
    {
        var node = new JsonObject();
        foreach (var (name, value) in fields)
        {
            node[name] = value;
        }

        return node.ToJsonString();
    }

    private static bool TryParseJsonObject(string? text, out JsonObject? data)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            data = JsonNode.Parse(text!) as JsonObject;
            return data is not null;
        }
        catch (JsonException)
        {
            // 守卫白名单：不可解析即由调用方转结构化错误（此处返回 false，避免把分类信息扁平化）。
            return false;
        }
    }

    private static string ClipFailureDetail(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "（下游未返回文本）";
        }

        // 首行是结构化载荷（已由外层载荷承载），正文从第二行开始。
        var firstBreak = text!.IndexOf('\n');
        var detail = firstBreak >= 0 && firstBreak + 1 < text.Length ? text.Substring(firstBreak + 1) : text;
        return Clip(detail, MaxFailureDetailChars) ?? string.Empty;
    }

    private static string? ReadString(JsonNode? node)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var text))
        {
            return text;
        }

        return null;
    }

    private static bool ReadBool(JsonNode? node)
    {
        if (node is JsonValue value)
        {
            if (value.TryGetValue<bool>(out var flag))
            {
                return flag;
            }

            if (value.TryGetValue<string>(out var text) && bool.TryParse(text, out var parsed))
            {
                return parsed;
            }
        }

        return false;
    }

    private static int? ReadInt(JsonNode? node)
    {
        if (node is JsonValue value)
        {
            if (value.TryGetValue<int>(out var number))
            {
                return number;
            }

            if (value.TryGetValue<string>(out var text)
                && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static string? Clip(string? value, int max)
        => string.IsNullOrEmpty(value) || value!.Length <= max ? value : value.Substring(0, max) + "…";

    private static string FormatTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds)
            && milliseconds > 1_000_000_000_000
            && milliseconds < 4_000_000_000_000)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds)
                .ToString("MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString("MM-dd HH:mm", CultureInfo.InvariantCulture)
            : value!;
    }
}
