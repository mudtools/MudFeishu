// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.AI.Agent.Tools;
using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Tools.Catalog;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// S-4：复合工作流端到端演示（<c>workflow.meeting_summary</c> / <c>workflow.standup_report</c>）。
/// </summary>
/// <remarks>
/// <para>
/// 本模式不经过模型：直接经 <see cref="FeishuToolRegistry"/> 调用两个工作流工具，验证
/// ① 整链 dry_run 只回步骤计划；② 真实执行的每步仍走注册表执行链（授权 / 审计 / 净化零旁路）；
/// ③ 站会日报的 user 身份前提（用户令牌缺失时回可执行的 <c>user_auth_required</c>）。
/// </para>
/// <para>
/// 装配与工具冒烟同构（统一节 <c>FeishuDemo</c> 的凭证 + 全域 API），但**不注册模型**——
/// 工作流是确定性工具链，模型凭据缺失不影响本模式。共享只读白名单刻意不含
/// <c>workflow.meeting_summary</c>（PII <c>defaultDisabled</c>），本模式在自己的白名单里显式加入。
/// </para>
/// </remarks>
internal static class WorkflowDemo
{
    private const string StandupPlaceholderCalendarId = "（未配置 FeishuWorkflowDemo:CalendarId）";

    /// <summary>运行复合工作流演示。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务。</returns>
    public static async Task RunAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = WorkflowDemoSettings.FromConfiguration(configuration);
        settings.Validate();

        var appKey = DemoAgentDefaults.DefaultAppKey;
        var feishuConfig = DemoAppConfig.CreateSingleApp(appKey, settings.AppId, settings.AppSecret);

        var services = new ServiceCollection()
            .AddDemoLogging(configuration)
            .AddFeishuApp(feishuConfig, "FeishuApps")
            .AddFeishuServices(builder => builder.AddAllApis());

        services.AddSingleton(Options.Create(new FeishuAgentOptions
        {
            Instructions = "workflow-demo",
            // workflow.meeting_summary 不在共享白名单（PII 默认不启用）：本模式是它的显式启用点。
            Tools = [.. DemoAgentDefaults.ReadonlyToolWhitelist, FeishuToolNames.WorkflowMeetingSummary],
            // 站会日报的第二步 task.list_my_tasks 是 user 身份，须显式放行。
            AllowedIdentities = ["tenant", "user"],
        }));
        services.AddFeishuTools();

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var accessor = provider.GetRequiredService<IFeishuToolContextAccessor>();

        Console.WriteLine($"已启用工具 {registry.EnabledTools.Count} 个（含 workflow 2 个）");
        Console.WriteLine("本模式不调用模型：工作流是确定性工具链，模型凭据缺失不影响运行。");

        var (standupStart, standupEnd) = TodayWindow();
        var calendarId = settings.CalendarId ?? StandupPlaceholderCalendarId;

        var meetingArguments = MeetingArguments(settings, dryRun: true);
        var standupArguments = StandupArguments(settings, calendarId, standupStart, standupEnd, dryRun: true);
        var context = new FeishuToolContext(appKey, "workflow-demo", UserId: settings.UserId);

        using (accessor.Begin(context))
        {
            Print("workflow.meeting_summary · dry_run", await InvokeAsync(
                registry, FeishuToolNames.WorkflowMeetingSummary, meetingArguments, context, cancellationToken));

            Print("workflow.standup_report · dry_run", await InvokeAsync(
                registry, FeishuToolNames.WorkflowStandupReport, standupArguments, context, cancellationToken));

            meetingArguments.Remove("dry_run");
            Print("workflow.meeting_summary · 真实执行", await InvokeAsync(
                registry, FeishuToolNames.WorkflowMeetingSummary, meetingArguments, context, cancellationToken));

            if (settings.CalendarId is null || settings.UserId is null)
            {
                Console.WriteLine();
                Console.WriteLine("=== workflow.standup_report · 跳过真实执行 ===");
                Console.WriteLine(
                    $"未配置 {WorkflowDemoSettings.SectionName}:CalendarId / {WorkflowDemoSettings.SectionName}:UserId——"
                    + "补齐两者（并在宿主侧存在该用户的用户令牌）后重跑本模式即可看到站会日报。");
                return;
            }

            standupArguments.Remove("dry_run");
            Print("workflow.standup_report · 真实执行", await InvokeAsync(
                registry, FeishuToolNames.WorkflowStandupReport, standupArguments, context, cancellationToken));
        }
    }

    private static Dictionary<string, object?> MeetingArguments(WorkflowDemoSettings settings, bool dryRun)
    {
        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["query"] = settings.MeetingQuery,
            ["max_meetings"] = 5,
            ["dry_run"] = dryRun,
        };

        if (settings.MeetingStart is { } start && settings.MeetingEnd is { } end)
        {
            arguments["start_time"] = start;
            arguments["end_time"] = end;
        }

        return arguments;
    }

    private static Dictionary<string, object?> StandupArguments(
        WorkflowDemoSettings settings, string calendarId, string start, string end, bool dryRun)
        => new(StringComparer.Ordinal)
        {
            ["calendar_id"] = calendarId,
            ["start_time"] = start,
            ["end_time"] = end,
            ["include_completed_tasks"] = settings.IncludeCompletedTasks,
            ["dry_run"] = dryRun,
        };

    /// <summary>站会日报的时间窗取本机自然日（与日历议程的 RFC3339 + 时区约束一致）。</summary>
    private static (string Start, string End) TodayWindow()
    {
        var now = DateTimeOffset.Now;
        var start = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        return (Format(start), Format(start.AddDays(1)));

        static string Format(DateTimeOffset value)
            => value.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    private static Task<ToolResult> InvokeAsync(
        FeishuToolRegistry registry,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        FeishuToolContext context,
        CancellationToken cancellationToken)
    {
        if (!registry.TryGet(toolName, out var definition) || definition is null)
        {
            throw new InvalidOperationException(
                $"工具 '{toolName}' 未注册——请确认对应域客户端已装配（本模式用 AddAllApis）");
        }

        if (!registry.IsEnabled(toolName))
        {
            throw new InvalidOperationException(
                $"工具 '{toolName}' 已注册但未启用——本模式的白名单应包含它");
        }

        return definition.Handler(arguments, context, cancellationToken);
    }

    private static void Print(string title, ToolResult result)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
        Console.WriteLine(result.Error is null
            ? "状态：成功"
            : $"状态：失败（{result.Error.Category}/{result.Error.Subtype}）");
        Console.WriteLine(result.Text);
    }
}
