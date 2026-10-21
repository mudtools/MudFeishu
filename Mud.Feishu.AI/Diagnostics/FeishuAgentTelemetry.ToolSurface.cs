using Mud.AI.Abstractions.Observability;
using Mud.Feishu.AI.AgentTools;

namespace Mud.Feishu.AI.Diagnostics;

/// <summary>
/// <see cref="FeishuAgentTelemetry"/> 的工具面分片（<c>IAgentToolTelemetry</c> 半边 + 成本核算 hook）。
/// DI 注册的是同一实例；主文件受注释预算冻结，新增能力面按「新分片」追加。
/// </summary>
public sealed partial class FeishuAgentTelemetry : IAgentToolTelemetry
{
    private readonly IFeishuUsageMeter? _usageMeter;
    private readonly IFeishuToolContextAccessor? _contextAccessor;

    /// <summary>初始化遥测实现（依赖可空：缺席时行为与无遥测一致）。</summary>
    /// <param name="usageMeter">成本核算出口（可空）。</param>
    /// <param name="contextAccessor">执行上下文访问器（可空；缺席时用量无租户/会话维度）。</param>
    public FeishuAgentTelemetry(
        IFeishuUsageMeter? usageMeter = null,
        IFeishuToolContextAccessor? contextAccessor = null)
    {
        _usageMeter = usageMeter;
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public void ToolInvoked(string toolName, bool isWrite, long durationMs, bool truncated, string? errorCategory)
        => FeishuNeutralToolTelemetry.RecordInvocation(toolName, isWrite, durationMs, truncated, errorCategory);

    /// <inheritdoc />
    public void ToolRetried(string toolName, int attempt)
        => FeishuNeutralToolTelemetry.RecordRetry(toolName, attempt);

    /// <inheritdoc />
    public void RetrievalPerformed(int chunks, int injectedChars)
        => FeishuNeutralToolTelemetry.RecordRetrieval(chunks, injectedChars);

    /// <inheritdoc />
    public void ApprovalRequested(string toolName)
        => FeishuNeutralToolTelemetry.RecordApprovalRequested(toolName);

    /// <inheritdoc />
    public void ApprovalResolved(string toolName, bool approved, long waitMs)
        => FeishuNeutralToolTelemetry.RecordApprovalResolved(toolName, approved, waitMs);

    /// <summary>token 用量落成本核算出口（端口不带租户键 ⇒ 租户/会话取自环境上下文）。</summary>
    /// <remarks>取不到上下文时不记账——把用量记到虚构会话上会让账对不上。</remarks>
    /// <param name="inputTokens">输入 token（可空）。</param>
    /// <param name="outputTokens">输出 token（可空）。</param>
    private void RecordUsageMeter(long? inputTokens, long? outputTokens)
    {
        if (_usageMeter is null || (inputTokens is null && outputTokens is null))
        {
            return;
        }

        var context = _contextAccessor?.Current;
        if (context is null || string.IsNullOrEmpty(context.AppKey))
        {
            return;
        }

        _usageMeter.RecordModelUsage(context.AppKey, context.ConversationKey, inputTokens, outputTokens);
    }
}
