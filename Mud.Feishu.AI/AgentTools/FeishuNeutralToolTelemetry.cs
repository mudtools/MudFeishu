using Mud.Feishu.Abstractions.Metrics;

namespace Mud.Feishu.AI.AgentTools;

/// <summary>
/// 中立工具遥测端口（<c>IAgentToolTelemetry</c>）在产品侧的落点。
/// </summary>
/// <remarks>
/// <para>
/// <b>口径事实</b>：端口签名不带租户键（中立机制不认识 app_key），故本组指标<b>无 app_key 维度</b>；
/// 按租户拆分的口径仍由 <see cref="FeishuToolDiagnostics.RecordExecution"/> / <c>RecordDuration</c> 承担。
/// 两组指标名不同（<c>feishu.agent.tool.*</c> vs <c>feishu.tool.*</c>），同一调用各记一次，不构成重复计数。
/// </para>
/// <para>
/// <b>为什么单独成文件</b>：<see cref="FeishuToolDiagnostics"/> 受注释预算冻结值约束（只允许收敛），
/// 新增一族记录点不该借"再固化一次"放行，故独立成文件并接受新文件硬上限（注释 ≤ 120 行、单块 ≤ 20 行）。
/// </para>
/// </remarks>
internal static class FeishuNeutralToolTelemetry
{
    /// <summary>记录一次工具调用：调用计数（tool/write/outcome/truncated）+ 耗时分布（tool/write）。</summary>
    /// <param name="toolName">工具名（有界维度）。</param>
    /// <param name="isWrite">是否写工具。</param>
    /// <param name="durationMs">耗时毫秒。</param>
    /// <param name="truncated">结果是否被截断。</param>
    /// <param name="errorCategory">失败分类字面量（<see langword="null"/> = 成功）。</param>
    public static void RecordInvocation(
        string toolName, bool isWrite, long durationMs, bool truncated, string? errorCategory)
    {
        var outcome = string.IsNullOrEmpty(errorCategory) ? FeishuMetrics.ToolOutcomes.Ok : errorCategory!;
        FeishuMetrics.AgentToolInvoked.Add(1, new TagList
        {
            { FeishuMetrics.Tags.Tool, toolName },
            { FeishuMetrics.Tags.IsWrite, isWrite },
            { FeishuMetrics.Tags.Outcome, outcome },
            { FeishuMetrics.Tags.Truncated, truncated },
        });

        FeishuMetrics.AgentToolInvokedDuration.Record(durationMs, new TagList
        {
            { FeishuMetrics.Tags.Tool, toolName },
            { FeishuMetrics.Tags.IsWrite, isWrite },
        });
    }

    /// <summary>记录一次工具重试（attempt 从 1 起，闭集维度）。</summary>
    /// <param name="toolName">工具名。</param>
    /// <param name="attempt">第几次重试。</param>
    public static void RecordRetry(string toolName, int attempt)
        => FeishuMetrics.AgentToolRetried.Add(1, new TagList
        {
            { FeishuMetrics.Tags.Tool, toolName },
            { FeishuMetrics.Tags.Attempt, attempt },
        });

    /// <summary>记录一次 RAG 检索：召回条数 + 注入字符数（注入 0 说明三级闸全拦截，必须可观测）。</summary>
    /// <param name="chunks">召回并注入的条数。</param>
    /// <param name="injectedChars">注入 prompt 的字符数。</param>
    public static void RecordRetrieval(int chunks, int injectedChars)
    {
        if (chunks > 0)
        {
            FeishuMetrics.AgentRetrievalChunks.Add(chunks);
        }

        FeishuMetrics.AgentRetrievalInjectedChars.Record(injectedChars);
    }

    /// <summary>记录一次人工审批请求（写工具停在「等待确认」）。</summary>
    /// <param name="toolName">工具名。</param>
    public static void RecordApprovalRequested(string toolName)
        => FeishuMetrics.AgentApprovalRequested.Add(1, new TagList { { FeishuMetrics.Tags.Tool, toolName } });

    /// <summary>记录一次人工审批解决（批准/拒绝都记，等待时长为 0 表示无法取得而非杜撰）。</summary>
    /// <param name="toolName">工具名。</param>
    /// <param name="approved">是否批准。</param>
    /// <param name="waitMs">人类等待毫秒。</param>
    public static void RecordApprovalResolved(string toolName, bool approved, long waitMs)
    {
        FeishuMetrics.AgentApprovalResolved.Add(1, new TagList
        {
            { FeishuMetrics.Tags.Tool, toolName },
            {
                FeishuMetrics.Tags.Outcome,
                approved ? FeishuMetrics.ApprovalOutcomes.Approved : FeishuMetrics.ApprovalOutcomes.Denied
            },
        });

        FeishuMetrics.AgentApprovalWait.Record(waitMs, new TagList { { FeishuMetrics.Tags.Tool, toolName } });
    }
}
