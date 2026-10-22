// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.AI.Tools.Catalog;
using Mud.Feishu.AI.Tools.Workflows;

namespace Mud.Feishu.AI.Tools.Tests.Workflows;

/// <summary>
/// 复合工作流执行器（S-4）：顺序执行、失败即停、断点续跑、预演与白名单边界。
/// </summary>
public class FeishuWorkflowRunnerTests
{
    private static FeishuToolDefinition Tool(string name, Func<ToolResult> result, bool isWrite = false)
        => new(
            name,
            "测试工具",
            ["test:read"],
            isWrite,
            isWrite ? FeishuToolRisk.HighRiskWrite : FeishuToolRisk.Read,
            FeishuToolIdentityNames.Tenant,
            (_, _, _) => Task.FromResult(result()));

    private static FeishuToolRegistry Registry(params FeishuToolDefinition[] tools)
    {
        var registry = new FeishuToolRegistry();
        foreach (var tool in tools)
        {
            registry.Register(tool);
            registry.MapTool(tool.Name);
        }

        return registry;
    }

    private static FeishuWorkflowDefinition Definition(params FeishuWorkflowStep[] steps)
        => new(
            "workflow.test",
            "测试工作流",
            steps,
            static data => new JsonObject
            {
                ["first"] = JsonScan.Clone(data.TryGetValue(1, out var node) ? node : null),
                ["steps_with_data"] = data.Count,
            });

    private static FeishuWorkflowStep Step(int index, string tool)
        => new(index, tool, $"第 {index} 步", static _ => new Dictionary<string, object?> { ["k"] = "v" });

    private static Task<FeishuWorkflowRun> RunAsync(
        FeishuToolRegistry registry,
        FeishuWorkflowDefinition definition,
        int startAt = 1,
        bool dryRun = false)
        => new FeishuWorkflowRunner(registry).RunAsync(
            definition,
            new Dictionary<string, object?>(),
            new FeishuToolContext("appA"),
            startAt,
            dryRun,
            CancellationToken.None);

    [Fact]
    public async Task Run_ShouldExecuteStepsInOrder_AndComposeReport()
    {
        var registry = Registry(
            Tool("test.step1", static () => ToolResult.FromText("""{"items":[]}""")),
            Tool("test.step2", static () => ToolResult.FromText("""{"ok":true}""")));

        var run = await RunAsync(registry, Definition(Step(1, "test.step1"), Step(2, "test.step2")));

        run.Failed.Should().BeFalse();
        run.Outcomes.Select(static o => o.Status).Should().AllBeEquivalentTo(FeishuWorkflowStepStatuses.Ok);
        run.Outcomes.Select(static o => o.Index).Should().ContainInOrder(1, 2);
        run.Report!["steps_with_data"]!.GetValue<int>().Should().Be(2);
    }

    [Fact]
    public async Task Run_StepFailure_ShouldStopAndReportResumeFrom()
    {
        var registry = Registry(
            Tool("test.step1", static () => ToolResult.FromText("""{"ok":true}""")),
            Tool("test.step2", static () => ToolResultPipeline.Error(
                "test.step2", ToolErrorCategory.Api, ToolErrorSubtype.UpstreamError, "boom", 1024)),
            Tool("test.step3", static () => ToolResult.FromText("""{"never":true}""")));

        var run = await RunAsync(registry, Definition(Step(1, "test.step1"), Step(2, "test.step2"), Step(3, "test.step3")));

        run.Failed.Should().BeTrue();
        run.ResumeFrom.Should().Be(2, "续跑坐标必须指向失败的这一步，而不是下一步");
        run.Outcomes.Should().HaveCount(2, "失败即停：第 3 步不得执行");
        run.Report.Should().BeNull("半份报告不得伪装成完整报告");
    }

    [Fact]
    public async Task Run_StartAt_ShouldSkipCompletedSteps()
    {
        var registry = Registry(
            Tool("test.step1", static () => throw new InvalidOperationException("被跳过的步骤不得执行")),
            Tool("test.step2", static () => ToolResult.FromText("""{"ok":true}""")));

        var run = await RunAsync(registry, Definition(Step(1, "test.step1"), Step(2, "test.step2")), startAt: 2);

        run.Failed.Should().BeFalse();
        run.Outcomes[0].Status.Should().Be(FeishuWorkflowStepStatuses.Skipped);
        run.Outcomes[1].Status.Should().Be(FeishuWorkflowStepStatuses.Ok);
    }

    [Fact]
    public async Task Run_DryRun_ShouldPlanOnly()
    {
        var registry = Registry(Tool("test.step1", static () => throw new InvalidOperationException("预演不得取数")));

        var run = await RunAsync(registry, Definition(Step(1, "test.step1")), dryRun: true);

        run.DryRun.Should().BeTrue();
        run.Outcomes.Single().Status.Should().Be(FeishuWorkflowStepStatuses.Planned);
        run.Report.Should().BeNull();
        run.Failed.Should().BeFalse();
    }

    [Fact]
    public async Task Run_DisabledStepTool_ShouldFailInsteadOfBypassingWhitelist()
    {
        // 注册了但不在启用集里（白名单未放行）= 宿主策略边界，工作流不得旁路。
        var registry = new FeishuToolRegistry();
        registry.Register(Tool("test.step1", static () => ToolResult.FromText("{}")));

        var run = await RunAsync(registry, Definition(Step(1, "test.step1")));

        run.Failed.Should().BeTrue();
        run.Outcomes.Single().Status.Should().Be(FeishuWorkflowStepStatuses.Failed);
        run.Outcomes.Single().Error.Should().Contain("未启用");
    }

    [Fact]
    public void Catalog_EveryStepTool_ShouldExistInContractTable_AndBeReadOnly()
    {
        var goldenTools = FeishuToolNames.All.ToHashSet(StringComparer.Ordinal);

        foreach (var name in new[] { FeishuWorkflowCatalog.MeetingSummary, FeishuWorkflowCatalog.StandupReport })
        {
            var definition = FeishuWorkflowCatalog.TryGet(name);
            definition.Should().NotBeNull();

            foreach (var step in definition!.Steps)
            {
                goldenTools.Should().Contain(step.ToolName,
                    $"工作流 {name} 的步骤工具必须是真实存在的策展工具（不存在即运行期必然失败）");
                FeishuToolNames.WriteAll.Should().NotContain(step.ToolName,
                    $"首批工作流是只读链：步骤 {step.ToolName} 不得是写工具");
            }
        }
    }
}
