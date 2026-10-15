// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
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
/// <b>装饰顺序</b>：预算装饰器在<b>审批标记之内、工具桥之外</b>——
/// 写工具的最终外层必须是 MEAI <see cref="ApprovalRequiredAIFunction"/>（MAF 用类型判定审批），
/// 预算装饰器若套在其外会让审批标记不可见、HITL 静默失效（见 <c>FeishuToolsToolSource.GetTools</c>）。
/// 后果是"预算耗尽"发生在审批之后：用户可能先被问一次审批再被预算拒绝，这是为保住人工确认语义
/// 付出的可接受代价（正确性优先于少一次交互）。
/// </para>
/// <para>
/// <b>为什么用结构化错误而非抛异常</b>：异常触发幂等回滚 + 重投递 ⇒ 重复计费
/// （与 R2-9 空回复守卫同一推理）。
/// </para>
/// <para>
/// <b>预算缺失 = 不限制</b>：当 <see cref="ToolCallBudget.Current"/> 为
/// <see langword="null"/> 时（如 MCP 路径或未走事件入口的直调），装饰器透传不拦截。
/// </para>
/// <para>
/// <b>N2-d 中立化</b>：预算类型已迁 <c>Mud.AI.Agent.Tools.ToolCallBudget</c>（共享接缝的
/// 正确归属是中立运行期包）；产品侧只消费，不持有。
/// </para>
/// </remarks>
internal sealed class ToolBudgetAIFunction : AIFunction
{
    private readonly AIFunction _inner;

    /// <summary>
    /// 初始化 <see cref="ToolBudgetAIFunction"/>。
    /// </summary>
    /// <param name="inner">被包装的工具函数。</param>
    public ToolBudgetAIFunction(AIFunction inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public override string Name => _inner.Name;

    /// <inheritdoc />
    public override string Description => _inner.Description;

    /// <inheritdoc />
    public override JsonElement JsonSchema => _inner.JsonSchema;

    /// <summary>
    /// 被包装的内层函数（契约守卫与诊断用——预算装饰器位于审批标记之内、工具桥之外）。
    /// </summary>
    /// <remarks>
    /// 供守卫"剥掉预算装饰器再判定真实包装形态"（如只读工具是否被审批标记包裹）。
    /// 不参与运行时执行路径。
    /// </remarks>
    internal AIFunction Inner => _inner;

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var budget = ToolCallBudget.Current;
        if (budget is null)
        {
            // 预算未设置（MCP 路径或直调）——透传不拦截。
            return await _inner.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
        }

        if (!budget.TryConsume())
        {
            // 越限：短路返回结构化错误，不调下游。
            var reason = $"本轮工具调用预算已用尽（{budget.Used}/{budget.Max}）；"
                + "请基于已有信息直接回答，不要再次调用工具";
            var errorText = FeishuToolBinding.StructuredError(
                _inner.Name,
                ToolErrorCategory.Policy,
                reason,
                subtype: ToolErrorSubtype.ToolBudgetExceeded);
            return ToolResult.FromError(errorText);
        }

        return await _inner.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }
}
