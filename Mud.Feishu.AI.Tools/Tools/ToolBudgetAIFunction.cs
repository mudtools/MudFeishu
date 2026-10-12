// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.AI;
using Mud.Feishu.AI.AgentTools;

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// 工具预算装饰器（T4 / D4）：包装 <see cref="AIFunction"/>，在调用前检查
/// <see cref="FeishuToolCallBudget"/> 是否越限；越限时短路返回结构化错误
/// （<c>category=Policy, subtype=tool_budget_exceeded</c>），不调下游。
/// </summary>
/// <remarks>
/// <para>
/// <b>装饰顺序</b>：预算装饰器在<b>最外层</b>（先预算、后审批包装）——
/// 预算耗尽时应拦截一切工具调用（含需审批的写工具），不应先走审批管线再被预算拒绝
/// （那会浪费一次审批交互）。
/// </para>
/// <para>
/// <b>为什么用结构化错误而非抛异常</b>：异常触发幂等回滚 + 重投递 ⇒ 重复计费
/// （与 R2-9 空回复守卫同一推理）。
/// </para>
/// <para>
/// <b>预算缺失 = 不限制</b>：当 <see cref="FeishuToolCallBudget.Current"/> 为
/// <see langword="null"/> 时（如 MCP 路径或未走事件入口的直调），装饰器透传不拦截。
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

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var budget = FeishuToolCallBudget.Current;
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
            return FeishuToolResult.FromError(errorText);
        }

        return await _inner.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }
}
