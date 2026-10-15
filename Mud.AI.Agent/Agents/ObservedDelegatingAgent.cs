// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Mud.AI.Abstractions.Observability;
using Mud.AI.Agent.Conversations;

namespace Mud.AI.Agent.Agents;

/// <summary>
/// 运行期编织基类：把「摘要先于 Run → 运行 Span → 耗时直方图 → token 用量 → 失败标注」这套
/// 与模型无关的机制固化在基类，派生类只提供事实（Agent 名、遥测实现、摘要器）。
/// </summary>
/// <remarks>
/// <para>
/// <b>次序是契约</b>：摘要必须在进入模型调用<b>之前</b>跑完，否则本轮历史仍会越限；摘要自身已做
/// 失败隔离，摘不下来不得影响主对话。
/// </para>
/// <para>
/// <b>失败可见且异常语义原样</b>：Span 标 Error 后<b>不包装</b>地抛出——上层幂等回滚依赖原异常
/// 类型；耗时记录放 <c>finally</c>，失败样本同样入直方图（否则 P95/P99 只反映成功路径）。
/// </para>
/// <para>
/// <b>维度纪律</b>：耗时只有「数值 + Agent 名」两个维度，会话/租户键只进
/// <see cref="IAgentTelemetry.StartRun"/> 的 Span——不得为指标新增高基数 tag。
/// </para>
/// </remarks>
public abstract class ObservedDelegatingAgent : DelegatingAIAgent
{
    private readonly string _agentName;
    private readonly IAgentTelemetry _telemetry;
    private readonly ConversationSummarizer? _summarizer;

    /// <summary>
    /// 初始化基类并接管运行期编织。
    /// </summary>
    /// <param name="innerAgent">内层 Agent（模型调用的归属方）。</param>
    /// <param name="agentName">Agent 名（遥测的唯一指标维度）。</param>
    /// <param name="telemetry">遥测端口（可空 ⇒ 空实现，零采集）。</param>
    /// <param name="summarizer">会话历史摘要器（可空 ⇒ 不做摘要）。</param>
    protected ObservedDelegatingAgent(
        AIAgent innerAgent,
        string? agentName,
        IAgentTelemetry? telemetry = null,
        ConversationSummarizer? summarizer = null)
        : base(innerAgent)
    {
        _agentName = agentName ?? string.Empty;
        _telemetry = telemetry ?? NoopAgentTelemetry.Instance;
        _summarizer = summarizer;
    }

    /// <inheritdoc />
    protected sealed override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        CancellationToken cancellationToken = default)
    {
        using var scope = _telemetry.StartRun(_agentName, streaming: false);
        await SummarizeIfNeededAsync(session, cancellationToken).ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await InnerAgent
                .RunAsync(messages, session, options, cancellationToken)
                .ConfigureAwait(false);

            _telemetry.RunCompleted(
                scope,
                failed: false,
                response.Usage?.InputTokenCount,
                response.Usage?.OutputTokenCount,
                response.Usage?.TotalTokenCount);
            return response;
        }
        catch (Exception)
        {
            _telemetry.RunCompleted(scope, failed: true, null, null, null);
            throw;
        }
        finally
        {
            _telemetry.LlmDuration(_agentName, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    protected sealed override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var scope = _telemetry.StartRun(_agentName, streaming: true);
        await SummarizeIfNeededAsync(session, cancellationToken).ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();
        var failed = false;
        UsageDetails? lastUsage = null;

        // 迭代器不得在含 catch 的 try 内 yield（CS1626）：失败标注下沉到枚举包装层，本层只留 try/finally。
        try
        {
            await foreach (var update in MarkFailureOnEnumerateAsync(
                InnerAgent.RunStreamingAsync(messages, session, options, cancellationToken),
                onFailure: () => failed = true,
                cancellationToken).ConfigureAwait(false))
            {
                // 用量在流中经 UsageContent 上报，取最后一次（其 Details 即本轮累计值）。
                if (update.Contents.OfType<UsageContent>().LastOrDefault() is { } usage)
                {
                    lastUsage = usage.Details;
                }

                yield return update;
            }
        }
        finally
        {
            _telemetry.LlmDuration(_agentName, stopwatch.ElapsedMilliseconds);
            _telemetry.RunCompleted(
                scope,
                failed,
                lastUsage?.InputTokenCount,
                lastUsage?.OutputTokenCount,
                lastUsage?.TotalTokenCount);
        }
    }

    /// <summary>摘要先于 Run：历史越限时先压缩；摘要器缺失或会话为空时不做任何事。</summary>
    private Task SummarizeIfNeededAsync(AgentSession? session, CancellationToken cancellationToken)
        => _summarizer is not null && session is not null
            ? _summarizer.SummarizeIfNeededAsync(session, cancellationToken)
            : Task.CompletedTask;

    /// <summary>枚举包装：底层枚举抛出时先回调标注失败，再<b>原样</b>抛出（不改异常类型）。</summary>
    private static async IAsyncEnumerable<AgentResponseUpdate> MarkFailureOnEnumerateAsync(
        IAsyncEnumerable<AgentResponseUpdate> source,
        Action onFailure,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var enumerator = source.GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            AgentResponseUpdate current;
            try
            {
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    break;
                }

                current = enumerator.Current;
            }
            catch
            {
                onFailure();
                throw;
            }

            // yield 位于 catch 之外（CS1626 约束）。
            yield return current;
        }
    }
}
