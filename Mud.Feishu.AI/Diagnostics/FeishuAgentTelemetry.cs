// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using Mud.AI.Abstractions.Observability;

namespace Mud.Feishu.AI.Diagnostics;

/// <summary>
/// 中立遥测端口（<see cref="IAgentTelemetry"/>）的<b>飞书实现</b>：把中立事件点映射回
/// 既有 <see cref="FeishuAgentDiagnostics"/> 的 Span/指标/<c>feishu.*</c> 命名面。
/// </summary>
/// <remarks>
/// <para>
/// <b>命名面零变化（R2-13）</b>：本类只做「端口调用 → 既有诊断方法」的转发，
/// Span 名（<c>feishu.agent.*</c>）、标签名、指标名与聚合口径<b>一个字节不改</b>——
/// 宿主仪表盘与告警规则因此不受中立化影响。守卫断言的是「同一事件点产出同一标签集合」。
/// </para>
/// <para>
/// <b>为什么端口返回 <see cref="IDisposable"/></b>：产品侧映射为 <see cref="Activity"/>
/// （它本身就是 <see cref="IDisposable"/>），故本实现的句柄就是 Activity 本身——
/// 不做任何包装对象，避免每个事件点多一次分配。
/// </para>
/// <para>
/// <b>无监听器时全链路零成本</b>：<c>StartActivity</c> 在无监听器时返回 <see langword="null"/>，
/// 各标记方法以 <c>as Activity</c> 判空后直接返回（不产生任何字符串拼接）。
/// </para>
/// </remarks>
public sealed class FeishuAgentTelemetry : IAgentTelemetry
{
    /// <inheritdoc />
    public IDisposable? StartTurn(string conversationKey, string? tenantKey)
        => FeishuAgentDiagnostics.StartConversationActivity(conversationKey, tenantKey ?? string.Empty);

    /// <inheritdoc />
    public void GateWaited(IDisposable? turn, double milliseconds)
        => (turn as Activity)?.SetTag(FeishuAgentDiagnostics.TagGateWaitMs, milliseconds);

    /// <inheritdoc />
    public void Replayed(IDisposable? turn)
        => (turn as Activity)?.SetTag(FeishuAgentDiagnostics.TagReplayed, true);

    /// <inheritdoc />
    public void Streamed(IDisposable? turn)
        => (turn as Activity)?.SetTag(FeishuAgentDiagnostics.TagStreamed, true);

    /// <inheritdoc />
    public void LlmDuration(string agentName, long milliseconds)
        => FeishuAgentDiagnostics.RecordLlmDuration(agentName, milliseconds);

    /// <inheritdoc />
    public void Degraded(string reason)
        // 中立机制不知道租户 ⇒ 经单参重载记「未归属」降级（省略 app_key 维度，不填伪值）。
        => Mud.Feishu.AI.AgentTools.FeishuToolDiagnostics.RecordDegraded(reason);

    /// <inheritdoc />
    public IDisposable? StartSummarize(int summarizedCount, int retainedCount)
        => FeishuAgentDiagnostics.StartSummarizeActivity(summarizedCount, retainedCount);

    /// <inheritdoc />
    public void SummarizeCompleted(IDisposable? scope, bool converged)
    {
        if (scope is not Activity activity)
        {
            return;
        }

        activity.SetTag(FeishuAgentDiagnostics.TagSummarized, true);
        if (!converged)
        {
            activity.SetTag(FeishuAgentDiagnostics.TagSummarizeUnconverged, true);
        }
    }

    /// <inheritdoc />
    public IDisposable? StartRun(string agentName, bool streaming)
        => FeishuAgentDiagnostics.StartRunActivity(
            agentName,
            streaming ? FeishuAgentDiagnostics.OperationRunStreaming : FeishuAgentDiagnostics.OperationRun);

    /// <inheritdoc />
    public void RunCompleted(IDisposable? scope, bool failed, long? inputTokens, long? outputTokens, long? totalTokens)
    {
        var hasUsage = inputTokens is not null || outputTokens is not null || totalTokens is not null;

        if (scope is Activity activity)
        {
            if (failed)
            {
                activity.SetStatus(ActivityStatusCode.Error);
            }

            // 三项全空时跳过，避免无谓分配（标签名与逐项判空口径经既有 RecordUsage 保持一致）。
            if (hasUsage)
            {
                FeishuAgentDiagnostics.RecordUsage(activity, new UsageDetails
                {
                    InputTokenCount = inputTokens,
                    OutputTokenCount = outputTokens,
                    TotalTokenCount = totalTokens,
                });
            }
        }

        // 成本计数挂在指标出口上，不挂在 Activity 上：无监听器时宿主仍要能算账
        // （本类的「零成本」承诺只适用于 Span 侧，指标侧的判空成本本就是一次整数比较）。
        if (hasUsage)
        {
            FeishuAgentDiagnostics.RecordTokenUsage(inputTokens, outputTokens, totalTokens);
        }
    }
}
