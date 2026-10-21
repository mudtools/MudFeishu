// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools.Workflows;

/// <summary>
/// 复合工作流的定义表（S-4）：编译期常量，无 IO / 无反射 / 无配置读。
/// </summary>
/// <remarks>
/// <para>
/// <b>首批两个工作流的取舍</b>：只做<b>全只读</b>链。写工作流的风险不在编排而在"半途失败后的补偿"
/// （例如先建文档后更新表格，第二步失败要回滚第一步），而平台侧没有事务/补偿原语——
/// 与其给出一条会留下半成品数据的写工作流，不如先交付确定可重试的只读链（写工作流登记为后续项）。
/// </para>
/// <para>
/// <b>步骤工具名是硬编码字面量</b>：不从 <c>FeishuToolNames</c> 取是因为这些常量属于生成产物，
/// 而工作流定义必须能在编译期与"工具确实存在"这一事实对齐——由守卫
/// <c>WorkflowDefinitionContractGuards</c> 断言每个步骤名都在注册表中存在且只读。
/// </para>
/// </remarks>
internal static class FeishuWorkflowCatalog
{
    /// <summary>妙记速览工作流（工具名）。</summary>
    public const string MeetingSummary = "workflow.meeting_summary";

    /// <summary>站会报告工作流（工具名）。</summary>
    public const string StandupReport = "workflow.standup_report";

    private static readonly FeishuWorkflowDefinition MeetingSummaryDefinition = new(
        MeetingSummary,
        "妙记速览",
        [
            new FeishuWorkflowStep(1, "minutes.get", "妙记元信息（标题/时长/参与人）",
                static p => new Dictionary<string, object?> { ["minute_token"] = p["minute_token"] }),
            new FeishuWorkflowStep(2, "minutes.get_statistics", "发言分布统计",
                static p => new Dictionary<string, object?> { ["minute_token"] = p["minute_token"] }),
            new FeishuWorkflowStep(3, "minutes.get_artifacts", "逐字稿窗口（摘要与待办的原料）",
                static p => new Dictionary<string, object?>
                {
                    ["minute_token"] = p["minute_token"],
                    ["transcript_offset"] = Read(p, "transcript_offset") ?? 0,
                    ["transcript_limit"] = Read(p, "transcript_limit") ?? 6000,
                }),
        ],
        ComposeMeetingSummary);

    private static readonly FeishuWorkflowDefinition StandupReportDefinition = new(
        StandupReport,
        "站会报告",
        [
            new FeishuWorkflowStep(1, "calendar.agenda", "时间窗内的日程实例",
                static p => new Dictionary<string, object?>
                {
                    ["calendar_id"] = p["calendar_id"],
                    ["start"] = p["start"],
                    ["end"] = p["end"],
                }),
            new FeishuWorkflowStep(2, "task.list_my_tasks", "我负责的任务",
                static p => new Dictionary<string, object?>
                {
                    // 只取"未完成"：站会要的是今天还要推进的事；已完成项对站会无信息量且会挤占预算。
                    ["completed"] = false,
                    ["fetch_all"] = true,
                    ["max_items"] = Read(p, "max_items") ?? 50,
                }),
        ],
        ComposeStandupReport);

    /// <summary>按工具名取定义。</summary>
    /// <param name="toolName">工作流工具名。</param>
    /// <returns>定义；未登记时为 <see langword="null"/>。</returns>
    public static FeishuWorkflowDefinition? TryGet(string toolName) => toolName switch
    {
        MeetingSummary => MeetingSummaryDefinition,
        StandupReport => StandupReportDefinition,
        _ => null,
    };

    private static JsonNode? Step(IReadOnlyDictionary<int, JsonNode?> data, int index)
        => data.TryGetValue(index, out var node) ? node : null;

    private static object? Read(IReadOnlyDictionary<string, object?> parameters, string key)
        => parameters.TryGetValue(key, out var value) ? value : null;

    private static JsonObject ComposeMeetingSummary(IReadOnlyDictionary<int, JsonNode?> data)
    {
        var artifacts = Step(data, 3);
        var hasMore = JsonScan.TryReadBool(artifacts, "has_more");
        var nextOffset = JsonScan.TryReadInt(artifacts, "next_offset");

        var nextActions = new JsonArray();

        // AddNode（而非 Add<T>）：泛型 Add<T>(T) 带 RequiresUnreferencedCode/RequiresDynamicCode，
        // AOT 严格模式下直接报红（本仓既有纪律见 ToolResultText.AddNode）。
        nextActions.AddNode(
            hasMore == true && nextOffset is not null
                ? $"逐字稿未读完（has_more=true）：用 transcript_offset={nextOffset} 再调 workflow.meeting_summary 续读"
                : "逐字稿已读完：可直接从 transcript 中提取结论、决策与待办");
        nextActions.AddNode("摘要与待办提取由你基于 transcript 内容完成（飞书妙记未提供摘要接口）");

        return new JsonObject
        {
            ["minute"] = JsonScan.Clone(Step(data, 1)),
            ["statistics"] = JsonScan.Clone(Step(data, 2)),
            ["transcript"] = JsonScan.Clone(artifacts),
            ["next_actions"] = nextActions,
            ["omitted"] = data.ContainsKey(2) ? null : "include_statistics=false：本次未取发言统计",
        };
    }

    private static JsonObject ComposeStandupReport(IReadOnlyDictionary<int, JsonNode?> data)
    {
        var agenda = Step(data, 1);
        var tasks = Step(data, 2);

        var nextActions = new JsonArray();
        nextActions.AddNode("按 agenda 顺序列出今日会议，并标出与未完成任务相关的会议");
        nextActions.AddNode("任务列表若返回 page_token，需要更多任务时再用 task.list_my_tasks 翻页");

        return new JsonObject
        {
            ["agenda"] = JsonScan.Clone(agenda),
            ["my_tasks"] = JsonScan.Clone(tasks),
            ["counts"] = new JsonObject
            {
                ["agenda_items"] = JsonScan.CountItems(agenda),
                ["task_items"] = JsonScan.CountItems(tasks),
            },
            ["next_actions"] = nextActions,
        };
    }
}
