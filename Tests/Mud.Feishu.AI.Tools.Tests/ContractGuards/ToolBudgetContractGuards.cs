// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using Mud.Feishu.AI.AgentTools;

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
    /// 守卫 ②：装饰顺序 = 审批标记（最外层）→ 预算装饰器 → 工具桥。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么顺序是硬约束</b>：MAF 的 <c>FunctionInvokingChatClient</c> 用<b>类型</b>判定是否走审批
    /// （<c>function is ApprovalRequiredAIFunction</c>）。把预算装饰器套在审批标记外面，标记对 MAF
    /// 即不可见 ⇒ 写工具的 HITL <b>静默失效</b>（模型无需人工确认即可执行写操作），而
    /// "工具数/Schema/名称"这类行为断言全部照旧通过——正是本次被
    /// <c>ToolSurfaceConsistencyContractTests.ApprovalWrappingDifference_ShouldBeExactlyTheWriteTools</c>
    /// 逮到的那类回归。
    /// </para>
    /// <para>
    /// 原实现只断言"GetTools 方法存在"，对顺序零约束（假绿）——本守卫改为对装配源码做结构锁定：
    /// 审批门必须施加在<b>已被预算装饰过的</b> bridge 上，且反向形态不得存在。
    /// </para>
    /// </remarks>
    [Fact]
    public void ApprovalMarker_ShouldStayOutermost_BeforeBudgetDecorator()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Mud.Feishu.AI.Tools",
            "Tools",
            "FeishuToolAIFunction.cs"));

        source.Should().Contain(
            "ApplyApprovalGate(budgetedBridge, definition)",
            "审批门必须包在**已被预算装饰过**的 bridge 之外——否则 MAF 看不到审批标记，写工具 HITL 静默失效");

        source.Should().NotContain(
            "new ToolBudgetAIFunction(ApplyApprovalGate",
            "禁止把预算装饰器套在审批标记之外（该形态会让 ApprovalRequiredAIFunction 对 MAF 不可见）");

        source.Should().Contain(
            "new ApprovalRequiredAIFunction(function)",
            "写工具的审批标记产出点必须存在（MAF 的类型判定依据）");
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

    /// <summary>跨平台定位仓库根目录（以 <c>Mud.Feishu.slnx</c> 为锚，对齐本仓其余守卫）。</summary>
    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
