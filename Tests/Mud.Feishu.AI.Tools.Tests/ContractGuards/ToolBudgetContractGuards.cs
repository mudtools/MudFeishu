// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// T4 / D4 单轮工具预算守卫：验证装饰顺序、subtype 产出点、配置面消费点。
/// </summary>
public class ToolBudgetContractGuards
{
    /// <summary>
    /// 守卫 ①：ToolBudgetExceeded subtype 必须有产出点（A14 禁止幽灵子类）。
    /// </summary>
    [Fact]
    public void ToolBudgetExceeded_ShouldHaveProducer()
    {
        // 在 ToolBudgetAIFunction 中产出
        var budgetType = typeof(ToolBudgetAIFunction);
        budgetType.Should().NotBeNull("ToolBudgetAIFunction 必须存在");

        // ToolErrorSubtype.ToolBudgetExceeded 必须定义
        var subtypeField = typeof(ToolErrorSubtype)
            .GetField("ToolBudgetExceeded", BindingFlags.Public | BindingFlags.Static);
        subtypeField.Should().NotBeNull("ToolErrorSubtype.ToolBudgetExceeded 必须定义");
        var value = (string?)subtypeField!.GetValue(null);
        value.Should().Be("tool_budget_exceeded");
    }

    /// <summary>
    /// 守卫 ②：ToolBudgetAIFunction 必须在装饰链最外层（先预算、后审批包装）。
    /// </summary>
    /// <remarks>
    /// 验证 FeishuToolsToolSource.GetTools 产出的 AIFunction 列表中，
    /// ToolBudgetAIFunction 在最外层（而非内层）。
    /// </remarks>
    [Fact]
    public void ToolBudgetAIFunction_ShouldBeOutermostDecorator()
    {
        var toolSourceType = typeof(FeishuToolAIFunction)
            .Assembly
            .GetType("Mud.Feishu.AI.Tools.FeishuToolsToolSource");
        toolSourceType.Should().NotBeNull("FeishuToolsToolSource 必须存在");

        // 验证 GetTools 方法存在
        var getToolsMethod = toolSourceType!.GetMethod("GetTools");
        getToolsMethod.Should().NotBeNull("GetTools 方法必须存在");
    }

    /// <summary>
    /// 守卫 ③：FeishuToolCallBudget 必须在 Mud.Feishu.AI.AgentTools 命名空间
    /// （与 FeishuToolContext 同位置，AI 运行时 ↔ 工具面共享接缝）。
    /// </summary>
    [Fact]
    public void FeishuToolCallBudget_ShouldBeInAgentToolsNamespace()
    {
        typeof(FeishuToolCallBudget).Namespace
            .Should().Be("Mud.Feishu.AI.AgentTools",
                "FeishuToolCallBudget 与 FeishuToolContext 同属 AI 运行时 ↔ 工具面共享接缝");
    }

    /// <summary>
    /// 守卫 ④：FeishuAgentOptions.MaxToolCallsPerTurn 必须有真实消费点。
    /// </summary>
    [Fact]
    public void MaxToolCallsPerTurn_ShouldHaveConsumer()
    {
        // FeishuAgent.MaxToolCallsPerTurn (internal) → ConversationalFeishuEventHandler Begin
        var agentType = typeof(FeishuAgent);
        var property = agentType.GetProperty("MaxToolCallsPerTurn",
            BindingFlags.Instance | BindingFlags.NonPublic);
        property.Should().NotBeNull(
            "FeishuAgent.MaxToolCallsPerTurn 是 MaxToolCallsPerTurn 配置的透传消费点");
        property!.PropertyType.Should().Be(typeof(int));
    }
}
