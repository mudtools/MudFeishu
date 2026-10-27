// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Mud.Feishu.AI;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// 复合工作流域（<c>workflow.*</c>，2 枚只读）的链路用例。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域值得锁住的非显然事实</b>：① 工作流只经注册表里的 <c>Handler</c> 调用下游——该入口已被
/// 执行链包成授权 / 审计 / 脱敏，工作流不得直连执行器；② 依赖工具「已注册未启用」必须在任何嵌套
/// 调用前拦下（<c>TryGet</c> 对未启用同样成功，仅靠它等于开后门）；③ <c>dry_run</c> 先于依赖门，
/// 只回字段名、不回显参数值；④ <c>resume_token</c> 无服务端状态，靠「工作流 + 输入指纹 + 已完成
/// 步骤前缀」三重校验防串用；⑤ 每步审计各自独立投递，一次工作流的多次调用可按工具名归组。
/// </para>
/// </remarks>
public class WorkflowToolsTests
{
    private const string MeetingQuery = "产品评审会纪要汇总整理";
    private const string CalendarId = "feishu.cn_demo@group.calendar.feishu.cn";
    private const string WindowStart = "2026-10-20T00:00:00+08:00";
    private const string WindowEnd = "2026-10-21T00:00:00+08:00";

    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();
    private readonly FeishuToolContextAccessor _accessor = new();
    private readonly List<string> _calls = [];
    private readonly List<ToolExecutionAuditRecord> _audits = [];
    private readonly Dictionary<string, IReadOnlyDictionary<string, object?>> _lastArguments = [];
    private FeishuToolBinding? _binding;

    public WorkflowToolsTests()
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Returns(() => new NoopDisposable());
    }

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);

    private static ToolResult SearchResult()
        => ToolResult.FromText(
            """{"items":[{"minute_token":"min_1","display_info":"产品周会"}],"has_more":false}""");

    private static ToolResult ArtifactsResult()
        => ToolResult.FromText(
            """{"summary":"讨论了 3.1 路线图与发布节奏","keywords":["路线图","发布"],"chapters":[{"title":"路线图"},{"title":"排期"}],"todos":[{"content":"发布 3.1.0","is_done":false}]}""");

    private static ToolResult AgendaResult()
        => ToolResult.FromText(
            """{"items":[{"event_id":"ev_1","summary":"每日站会","start":"2026-10-20T09:00:00+08:00","end":"2026-10-20T09:15:00+08:00","status":"confirmed"}],"has_more":false}""");

    private static ToolResult TasksResult()
        => ToolResult.FromText(
            """{"items":[{"task_guid":"tg_1","summary":"整理发布清单","due":"2026-10-21T18:00:00+08:00","completed_at":null}],"has_more":false}""");

    private FeishuToolDefinition Definition(
        string name, Func<ToolResult> downstream, bool useBinding = false)
    {
        FeishuToolDefinition? definition = null;
        definition = new FeishuToolDefinition(
            name,
            "测试工具",
            [],
            IsWrite: false,
            Risk: FeishuToolRisk.Read,
            Identity: FeishuToolIdentityNames.Tenant,
            Handler: (args, context, ct) =>
            {
                _calls.Add(name);
                _lastArguments[name] = args;
                return useBinding
                    ? _binding!.ExecuteAsync(definition!, args, context, _ => Task.FromResult(downstream()), ct)
                    : Task.FromResult(downstream());
            });

        return definition;
    }

    private WorkflowTools CreateTools(FeishuToolRegistry registry)
    {
        var services = new ServiceCollection();
        services.AddSingleton(registry);
        services.AddSingleton<IFeishuToolContextAccessor>(_accessor);
        var provider = services.BuildServiceProvider();

        _accessor.Begin(new FeishuToolContext("app_key", "conv_key", UserId: "ou_1"));

        return new WorkflowTools(
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            provider);
    }

    private FeishuToolRegistry Registry(params FeishuToolDefinition[] definitions)
    {
        var registry = new FeishuToolRegistry();
        foreach (var definition in definitions)
        {
            registry.Register(definition);
            registry.MapTool(definition.Name);
        }

        return registry;
    }

    private static JsonObject Payload(ToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    private static string? ExtractResumeToken(ToolResult result)
    {
        var match = Regex.Match(result.ToString(), @"resume_token=([A-Za-z0-9+/=]+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static ToolResult PlatformBusy()
        => ToolResultPipeline.Error(
            FeishuToolNames.MinutesGetArtifacts,
            ToolErrorCategory.Retryable,
            ToolErrorSubtype.UpstreamError,
            "平台繁忙，请稍后重试",
            maxLength: 4000);

    // ───────────────────── meeting_summary ─────────────────────

    [Fact]
    public async Task SummarizeMeeting_ShouldRunSearchThenArtifacts_AndBuildReport()
    {
        var registry = Registry(
            Definition(FeishuToolNames.MinutesSearch, SearchResult),
            Definition(FeishuToolNames.MinutesGetArtifacts, ArtifactsResult));
        var tools = CreateTools(registry);

        var result = await tools.SummarizeMeetingAsync(
            Args(("query", MeetingQuery), ("start_time", WindowStart), ("end_time", WindowEnd)),
            CancellationToken.None);

        result.Error.Should().BeNull();
        _calls.Should().Equal(
            [FeishuToolNames.MinutesSearch, FeishuToolNames.MinutesGetArtifacts],
            "步骤必须按声明的顺序执行，且每步只调用一次");

        var payload = Payload(result);
        payload["meeting_count"]!.GetValue<int>().Should().Be(1);
        payload["report_markdown"]!.GetValue<string>().Should().Contain("讨论了 3.1 路线图")
            .And.Contain("- [ ] 发布 3.1.0");
        payload["completed_steps"]!.AsArray().Select(static step => step!["id"]!.GetValue<string>())
            .Should().Equal(["search", "artifacts"]);
        payload["resumed"]!.GetValue<bool>().Should().BeFalse();

        _lastArguments[FeishuToolNames.MinutesSearch]["create_time_start"].Should().Be(WindowStart);
        _lastArguments[FeishuToolNames.MinutesGetArtifacts]["transcript_limit"].Should().Be(1,
            "工作流只要摘要/章节/待办，逐字稿留给 minutes.get_artifacts 按需读取");
    }

    [Fact]
    public async Task SummarizeMeeting_ShouldReturnActionablePolicyError_WhenDependencyRegisteredButDisabled()
    {
        var registry = new FeishuToolRegistry()
            .Register(Definition(FeishuToolNames.MinutesSearch, SearchResult))
            .Register(Definition(FeishuToolNames.MinutesGetArtifacts, ArtifactsResult));
        registry.MapTool(FeishuToolNames.MinutesSearch);

        var tools = CreateTools(registry);
        var result = await tools.SummarizeMeetingAsync(
            Args(("query", MeetingQuery)), CancellationToken.None);

        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be("policy");
        result.Error.Subtype.Should().Be(ToolErrorSubtype.ToolNotAllowed);
        result.ToString().Should().Contain(FeishuToolNames.MinutesGetArtifacts)
            .And.Contain("已注册但未启用");
        _calls.Should().BeEmpty("依赖门必须在任何嵌套调用之前拦下——工作流不得成为禁用工具的后门");
    }

    [Fact]
    public async Task SummarizeMeeting_ShouldNotInvokeDownstream_WhenDryRun()
    {
        var tools = CreateTools(new FeishuToolRegistry());

        var result = await tools.SummarizeMeetingAsync(
            Args(("query", MeetingQuery), ("max_meetings", 3), ("dry_run", true)),
            CancellationToken.None);

        result.Error.Should().BeNull("预演先于依赖门执行，未注册依赖不应影响预演");
        _calls.Should().BeEmpty("dry_run 不得调用任何下游工具");

        var payload = Payload(result);
        payload["dry_run"]!.GetValue<bool>().Should().BeTrue();
        payload["inputs"]!["max_meetings"]!.GetValue<int>().Should().Be(3);
        var steps = payload["steps"]!.AsArray();
        steps.Should().HaveCount(2);
        steps[0]!["args_preview"]!.AsArray().Select(static item => item!.GetValue<string>())
            .Should().Contain(["query", "fetch_all", "max_items"]);
        result.ToString().Should().NotContain(MeetingQuery,
            "预演只回字段名，不得回显参数值（dry_run 不是回显通道）");
    }

    [Fact]
    public async Task SummarizeMeeting_ShouldResumeFromToken_AndSkipCompletedSearch()
    {
        var artifactsShouldFail = true;
        var registry = Registry(
            Definition(FeishuToolNames.MinutesSearch, SearchResult),
            Definition(FeishuToolNames.MinutesGetArtifacts, () => artifactsShouldFail
                ? PlatformBusy()
                : ArtifactsResult()));
        var tools = CreateTools(registry);

        var first = await tools.SummarizeMeetingAsync(
            Args(("query", MeetingQuery)), CancellationToken.None);

        first.Error.Should().NotBeNull();
        var token = ExtractResumeToken(first);
        token.Should().NotBeNull("失败结果必须附带续跑令牌，否则「断点续跑」只是描述");
        _calls.Should().Equal([FeishuToolNames.MinutesSearch, FeishuToolNames.MinutesGetArtifacts]);

        artifactsShouldFail = false;
        var second = await tools.SummarizeMeetingAsync(
            Args(("query", MeetingQuery), ("resume_token", token)),
            CancellationToken.None);

        second.Error.Should().BeNull();
        _calls.Count(c => c == FeishuToolNames.MinutesSearch).Should().Be(1,
            "续跑必须跳过已完成的 search 步骤，不能重复拉取下游");
        _calls.Count(c => c == FeishuToolNames.MinutesGetArtifacts).Should().Be(2);

        var payload = Payload(second);
        payload["resumed"]!.GetValue<bool>().Should().BeTrue();
        payload["meeting_count"]!.GetValue<int>().Should().Be(1,
            "search 结果从令牌恢复，会议列表不得丢失");
    }

    [Fact]
    public async Task SummarizeMeeting_ShouldRejectResumeToken_WhenInputsChanged()
    {
        var artifactsShouldFail = true;
        var registry = Registry(
            Definition(FeishuToolNames.MinutesSearch, SearchResult),
            Definition(FeishuToolNames.MinutesGetArtifacts, () => artifactsShouldFail
                ? PlatformBusy()
                : ArtifactsResult()));
        var tools = CreateTools(registry);

        var first = await tools.SummarizeMeetingAsync(
            Args(("query", MeetingQuery)), CancellationToken.None);
        var token = ExtractResumeToken(first);
        token.Should().NotBeNull();

        artifactsShouldFail = false;
        var second = await tools.SummarizeMeetingAsync(
            Args(("query", "另一个会议主题关键词"), ("resume_token", token)),
            CancellationToken.None);

        second.Error.Should().NotBeNull();
        second.Error!.Category.Should().Be("validation");
        second.ToString().Should().Contain("同一组输入",
            "输入变化后旧令牌必须拒绝——否则续跑会拼出另一份报告");
        _calls.Count(c => c == FeishuToolNames.MinutesSearch).Should().Be(1,
            "令牌校验失败必须发生在任何嵌套调用之前");
    }

    // ───────────────────── standup_report ─────────────────────

    [Fact]
    public async Task Standup_ShouldRunAgendaThenTasks_AndBuildReport()
    {
        var registry = Registry(
            Definition(FeishuToolNames.CalendarAgenda, AgendaResult),
            Definition(FeishuToolNames.TaskListMyTasks, TasksResult));
        var tools = CreateTools(registry);

        var result = await tools.GenerateStandupAsync(
            Args(
                ("calendar_id", CalendarId),
                ("start_time", WindowStart),
                ("end_time", WindowEnd),
                ("include_completed_tasks", true)),
            CancellationToken.None);

        result.Error.Should().BeNull();
        _calls.Should().Equal([FeishuToolNames.CalendarAgenda, FeishuToolNames.TaskListMyTasks]);

        var payload = Payload(result);
        payload["agenda_count"]!.GetValue<int>().Should().Be(1);
        payload["task_count"]!.GetValue<int>().Should().Be(1);
        payload["report_markdown"]!.GetValue<string>().Should().Contain("每日站会")
            .And.Contain("整理发布清单");

        var agendaArgs = _lastArguments[FeishuToolNames.CalendarAgenda];
        agendaArgs["calendar_id"].Should().Be(CalendarId);
        agendaArgs["start"].Should().Be(WindowStart);
        agendaArgs["end"].Should().Be(WindowEnd);

        var taskArgs = _lastArguments[FeishuToolNames.TaskListMyTasks];
        taskArgs["fetch_all"].Should().Be(true);
        taskArgs["max_items"].Should().Be(20);
        taskArgs["completed"].Should().Be(true, "include_completed_tasks 必须透传给下游");
    }

    [Fact]
    public async Task Standup_ShouldRejectInvalidWindow_BeforeCallingDownstream()
    {
        var registry = Registry(
            Definition(FeishuToolNames.CalendarAgenda, AgendaResult),
            Definition(FeishuToolNames.TaskListMyTasks, TasksResult));
        var tools = CreateTools(registry);

        var result = await tools.GenerateStandupAsync(
            Args(
                ("calendar_id", CalendarId),
                ("start_time", WindowEnd),
                ("end_time", WindowStart)),
            CancellationToken.None);

        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be("validation");
        result.ToString().Should().Contain("end_time 须晚于 start_time");
        _calls.Should().BeEmpty("参数校验必须先于任何嵌套调用");
    }

    // ───────────────────── 审计隔离 ─────────────────────

    [Fact]
    public async Task EachStep_ShouldWriteDistinguishableAudit()
    {
        _binding = new FeishuToolBinding(
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            auditSink: new RecordingSink(_audits));

        var registry = Registry(
            Definition(FeishuToolNames.MinutesSearch, SearchResult, useBinding: true),
            Definition(FeishuToolNames.MinutesGetArtifacts, ArtifactsResult, useBinding: true));
        var tools = CreateTools(registry);

        var result = await tools.SummarizeMeetingAsync(
            Args(("query", MeetingQuery)), CancellationToken.None);

        result.Error.Should().BeNull();
        _audits.Select(static record => record.ToolName).Should().Equal(
            [FeishuToolNames.MinutesSearch, FeishuToolNames.MinutesGetArtifacts],
            "每步走同一执行链 ⇒ 审计天然按步骤可区分（不能只留一条工作流级记录）");
        _audits.Should().OnlyContain(static record => record.AppKey == "app_key");
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class RecordingSink(List<ToolExecutionAuditRecord> records) : IToolExecutionAuditSink
    {
        public Task WriteAsync(ToolExecutionAuditRecord record, CancellationToken cancellationToken = default)
        {
            records.Add(record);
            return Task.CompletedTask;
        }
    }
}
