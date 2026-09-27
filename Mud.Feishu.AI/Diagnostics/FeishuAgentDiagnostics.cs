// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Diagnostics;

/// <summary>
/// Agent 运行时遥测：模型调用 Span 与飞书维度属性。
/// </summary>
/// <remarks>
/// <para>
/// 复用既有 <c>FeishuActivitySource</c>（<c>Mud.Feishu</c>）——宿主经 Mud.Feishu.OpenTelemetry
/// 的 <c>AddSource</c> 已注册该源，Agent Span 零配置并入既有链路（总体设计 §7.3）。
/// 飞书维度属性：<c>feishu.agent.name</c>、<c>feishu.agent.operation</c>、
/// <c>feishu.llm.input_tokens</c> / <c>output_tokens</c> / <c>total_tokens</c>；
/// 后续阶段在工具/检索 Span 上追加 <c>feishu.app_key</c> / <c>feishu.tenant</c> /
/// <c>feishu.conversation_id</c> / <c>feishu.tool_name</c>。
/// </para>
/// <para>
/// 敏感治理：Span 属性只允许结构化标量（ID/名称/token 计数），模型输入输出文本<b>不得</b>入属性
/// （对齐日志最小暴露 D5 精神）。
/// </para>
/// </remarks>
internal static class FeishuAgentDiagnostics
{
    /// <summary>Agent 操作名：非流式运行。</summary>
    public const string OperationRun = "run";

    /// <summary>Agent 操作名：流式运行。</summary>
    public const string OperationRunStreaming = "run_streaming";

    /// <summary>Agent 操作名：会话历史摘要压缩。</summary>
    public const string OperationSummarize = "summarize";

    /// <summary>Span 属性：Agent 展示名。</summary>
    public const string TagAgentName = "feishu.agent.name";

    /// <summary>Span 属性：操作名。</summary>
    public const string TagOperation = "feishu.agent.operation";

    /// <summary>Span 属性：本轮是否发生了历史摘要重建。</summary>
    public const string TagSummarized = "feishu.agent.summarized";

    /// <summary>Span 属性：本轮回复是否经流式通道送达。</summary>
    public const string TagStreamed = "feishu.agent.streamed";

    /// <summary>Span 属性：会话闸门获取等待耗时（毫秒；P2D-1——键维度只进 Span，不进 Metrics tag，原则 8）。</summary>
    public const string TagGateWaitMs = "feishu.conversation.gate_wait_ms";

    /// <summary>Span 属性：输入 token 数。</summary>
    public const string TagInputTokens = "feishu.llm.input_tokens";

    /// <summary>Span 属性：输出 token 数。</summary>
    public const string TagOutputTokens = "feishu.llm.output_tokens";

    /// <summary>Span 属性：总 token 数。</summary>
    public const string TagTotalTokens = "feishu.llm.total_tokens";

    /// <summary>
    /// 开启一次 Agent 运行 Span（无监听器时 StartActivity 返回 null，调用方须判空）。
    /// </summary>
    /// <param name="agentName">Agent 展示名。</param>
    /// <param name="operation">操作名（<see cref="OperationRun"/> / <see cref="OperationRunStreaming"/>）。</param>
    /// <returns>Activity（可能为 null）。</returns>
    public static Activity? StartRunActivity(string agentName, string operation)
    {
        var activity = FeishuActivitySource.Instance.StartActivity(
            $"feishu.agent.{operation}", ActivityKind.Internal);
        activity?.SetTag(TagAgentName, agentName);
        activity?.SetTag(TagOperation, operation);
        return activity;
    }

    /// <summary>
    /// 开启一次「事件→会话→模型→回复」会话管线 Span。
    /// </summary>
    /// <param name="conversationKey">会话键（由 ConversationKeyBuilder 构造）。</param>
    /// <param name="appKey">应用唯一标识。</param>
    /// <returns>Activity（可能为 null）。</returns>
    public static Activity? StartConversationActivity(string conversationKey, string appKey)
    {
        var activity = FeishuActivitySource.Instance.StartActivity(
            "feishu.agent.conversation", ActivityKind.Internal);
        activity?.SetTag("feishu.conversation.key", conversationKey)
                .SetTag("feishu.app_key", appKey);
        return activity;
    }

    /// <summary>
    /// 开启一次会话历史摘要 Span（属性只记条数，不记消息内容——日志最小暴露 D5）。
    /// </summary>
    /// <param name="summarizedCount">被压缩的旧消息条数。</param>
    /// <param name="retainedCount">保留的最近消息条数。</param>
    /// <returns>Activity（可能为 null）。</returns>
    public static Activity? StartSummarizeActivity(int summarizedCount, int retainedCount)
    {
        var activity = FeishuActivitySource.Instance.StartActivity(
            $"feishu.agent.{OperationSummarize}", ActivityKind.Internal);
        activity?.SetTag(TagOperation, OperationSummarize)
                .SetTag("feishu.agent.summarized_messages", summarizedCount)
                .SetTag("feishu.agent.retained_messages", retainedCount);
        return activity;
    }

    /// <summary>
    /// P1D-5：记录模型调用耗时指标（<c>feishu.agent.llm.duration</c> 毫秒直方图；维度 agent——
    /// 高基数纪律（原则 8）：conversation/chat/user 键不入 tags）。
    /// </summary>
    /// <param name="agentName">Agent 展示名。</param>
    /// <param name="durationMs">耗时毫秒。</param>
    public static void RecordLlmDuration(string agentName, long durationMs)
    {
        var metricsTags = new TagList { { "agent", agentName } };
        Mud.Feishu.Abstractions.Metrics.FeishuMetrics.AgentLlmDuration.Record(durationMs, metricsTags);
    }

    /// <summary>
    /// 记录模型返回的 token 用量到 Span。
    /// </summary>
    /// <param name="activity">当前 Activity（可为 null）。</param>
    /// <param name="usage">MAF 返回的用量（可能为 null）。</param>
    public static void RecordUsage(Activity? activity, UsageDetails? usage)
    {
        if (activity is null || usage is null)
            return;

        if (usage.InputTokenCount is { } input)
            activity.SetTag(TagInputTokens, input);
        if (usage.OutputTokenCount is { } output)
            activity.SetTag(TagOutputTokens, output);
        if (usage.TotalTokenCount is { } total)
            activity.SetTag(TagTotalTokens, total);
    }
}
