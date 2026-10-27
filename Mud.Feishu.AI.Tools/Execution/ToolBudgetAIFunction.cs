// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任。
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;
using Mud.AI.Agent.Tools;
using Mud.Feishu.AI.AgentTools;

namespace Mud.Feishu.AI.Tools.Execution;

/// <summary>
/// 工具预算装饰器（T4 / D4）：包装 <see cref="AIFunction"/>，在调用前检查
/// <see cref="ToolCallBudget"/> 是否越限；越限时短路返回结构化错误
/// （<c>category=Policy, subtype=tool_budget_exceeded</c>），不调下游。
/// </summary>
/// <remarks>
/// <para>
/// <b>装饰顺序</b>：预算装饰器在<b>审批标记之内、工具桥之外</b>——写工具的最终外层必须是 MEAI
/// <see cref="ApprovalRequiredAIFunction"/>（MAF 用类型判定审批），预算装饰器若套在其外会让审批标记
/// 不可见、HITL 静默失效（守卫 <c>ToolBudgetContractGuards.ApprovalMarker_ShouldStayOutermost_BeforeBudgetDecorator</c>）。
/// 后果是"预算耗尽"发生在审批之后：用户可能先被问一次审批再被预算拒绝，这是保人工确认语义的代价。
/// </para>
/// <para>
/// <b>只有一处装饰逻辑</b>：契约成员的转发由中立基座 <see cref="DelegatingToolFunction"/> 单点承担
/// （AOT 纪律：零反射、虚分派直调内层），本类只实现 <see cref="InvokeCoreAsync"/>。
/// </para>
/// <para>
/// <b>为什么用结构化错误而非抛异常</b>：异常触发幂等回滚 + 重投递 ⇒ 重复计费。
/// </para>
/// <para>
/// <b>预算缺失 = 不限制</b>：<see cref="ToolCallBudget.Current"/> 为 <see langword="null"/> 时
/// （MCP 路径或未走事件入口的直调）透传不拦截。
/// </para>
/// </remarks>
/// <param name="inner">被包装的工具函数。</param>
internal sealed class ToolBudgetAIFunction(AIFunction inner) : DelegatingToolFunction(inner)
{
    /// <summary>
    /// 被包装的内层函数（契约守卫与诊断用——预算装饰器位于审批标记之内、工具桥之外）。
    /// </summary>
    /// <remarks>不参与运行时执行路径：执行走基类的 <see cref="DelegatingToolFunction.Inner"/>。</remarks>
    internal AIFunction Wrapped => Inner;

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments? arguments,
        CancellationToken cancellationToken)
    {
        var budget = ToolCallBudget.Current;
        if (budget is null)
        {
            // 预算未设置（MCP 路径或直调）——透传不拦截。
            return await Inner.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
        }

        if (!budget.TryConsume())
        {
            // 越限：短路返回**结构化**错误（Error 载荷非空），不调下游。
            var reason = $"本轮工具调用预算已用尽（{budget.Used}/{budget.Max}）；"
                + "请基于已有信息直接回答，不要再次调用工具";
            return ToolResultPipeline.Error(
                Inner.Name,
                ToolErrorCategory.Policy,
                ToolErrorSubtype.ToolBudgetExceeded,
                FeishuToolBinding.StructuredError(
                    Inner.Name,
                    ToolErrorCategory.Policy,
                    reason,
                    subtype: ToolErrorSubtype.ToolBudgetExceeded),
                maxLength: 0);
        }

        return await Inner.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }
}
