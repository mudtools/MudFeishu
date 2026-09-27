// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Diagnostics;

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 渐进式会话摘要器（Phase 2 §3.2）：历史超过阈值时，把「保留窗以外」的旧消息压缩为
/// 一条系统摘要消息，替换原历史写回会话。
/// </summary>
/// <remarks>
/// <para>
/// <b>渐进式语义</b>：摘要以 <c>ChatMessage(role: system)</c> 形式驻留在会话历史内
/// （即「缓存进 session」）——下一轮触发时旧消息连同上一次摘要一起被重新压缩，
/// 不存在独立摘要缓存，也不每轮重复摘要（重建后历史条数低于阈值，直到新增消息再次越限）。
/// </para>
/// <para>
/// <b>与裁剪窗的关系</b>：<see cref="FeishuAgentOptions.MaxHistoryMessages"/> 由
/// <c>MessageCountingChatReducer</c> 在每次运行时折叠（模型可见窗口）；本摘要在其之上，
/// 把「将被折叠丢掉」的内容先压成要点，避免硬截断丢失上下文。保留窗取
/// <c>MaxHistoryMessages/2</c> 并钳制到 <c>SummaryThreshold-2</c> 以下，保证重建后不再立即越限。
/// </para>
/// <para>
/// <b>失败隔离</b>：摘要模型调用失败只记日志并跳过本轮压缩（退化为既有裁剪行为），
/// 绝不中断主对话流程。会话历史读写走 MAF
/// <c>AgentSessionExtensions.TryGetInMemoryChatHistory/SetInMemoryChatHistory</c>
/// （MAF 内部源生成序列化，AOT 安全；本类不做任何反射序列化）。
/// </para>
/// </remarks>
public sealed class ConversationSummarizer
{
    /// <summary>摘要提示词（system 侧指令；摘要产物以 system 消息注入历史）。</summary>
    private const string SummaryInstruction =
        "你是会话纪要助手。把下面的对话历史压缩成一份简洁的中文要点纪要：保留用户的目标、已确认的事实、"
        + "涉及的飞书资源（表格/文档 ID 等）与未完成的待办；不要评论、不要回答问题，只输出纪要正文。";

    private readonly IChatClient _chatClient;
    private readonly int _summaryThreshold;
    private readonly int _retainRecentCount;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="ConversationSummarizer"/>。
    /// </summary>
    /// <param name="chatClient">模型客户端（与 Agent 同一模型面）。</param>
    /// <param name="options">Agent 配置（<see cref="FeishuAgentOptions.SummaryThreshold"/> 与
    /// <see cref="FeishuAgentOptions.MaxHistoryMessages"/> 消费点）。</param>
    /// <param name="logger">日志（可空）。</param>
    public ConversationSummarizer(IChatClient chatClient, FeishuAgentOptions options, ILogger? logger = null)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        _summaryThreshold = options.SummaryThreshold;
        _retainRecentCount = ResolveRetainCount(options);
        _logger = logger;
    }

    /// <summary>会话历史是否需要摘要（阈值判定，0 = 禁用；仅诊断/测试用，主流程直接调 <see cref="SummarizeIfNeededAsync"/>）。</summary>
    public bool ShouldSummarize(int historyCount) => _summaryThreshold > 0 && historyCount >= _summaryThreshold;

    /// <summary>
    /// 按需压缩会话历史（阈值未触发时为无副作用 no-op）。
    /// </summary>
    /// <param name="session">MAF 会话（历史以 <c>InMemoryChatHistoryProvider</c> 状态驻留其中）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>是否发生了摘要重建。</returns>
    public async Task<bool> SummarizeIfNeededAsync(AgentSession session, CancellationToken cancellationToken = default)
    {
        if (session is null)
            throw new ArgumentNullException(nameof(session));

        if (!session.TryGetInMemoryChatHistory(out var history, FeishuAgent.ChatHistoryStateKey, null))
        {
            return false;
        }

        // netstandard2.0 的 BCL 不带 out 参数的 NotNullWhen 注解，需显式判空（对齐既有风格）。
        if (history is null || history.Count == 0 || !ShouldSummarize(history.Count))
        {
            return false;
        }

        var retain = Math.Min(_retainRecentCount, history.Count - 1);
        var older = history.GetRange(0, history.Count - retain);
        var recent = history.GetRange(history.Count - retain, retain);

        using var activity = FeishuAgentDiagnostics.StartSummarizeActivity(older.Count, recent.Count);

        string? summaryText;
        try
        {
            summaryText = await SummarizeCoreAsync(older, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 失败隔离：退化为既有裁剪行为，绝不中断主对话（Phase 2 §8 风险应对）。
            _logger?.LogWarning(ex, "会话摘要生成失败，本轮跳过压缩（历史 {Count} 条）", history.Count);
            return false;
        }

        if (string.IsNullOrWhiteSpace(summaryText))
        {
            _logger?.LogWarning("会话摘要返回空内容，本轮跳过压缩（历史 {Count} 条）", history.Count);
            return false;
        }

        var rebuilt = new List<ChatMessage>(recent.Count + 1)
        {
            new(ChatRole.System, $"[历史要点纪要]\n{summaryText.Trim()}"),
        };
        rebuilt.AddRange(recent);
        session.SetInMemoryChatHistory(rebuilt, FeishuAgent.ChatHistoryStateKey, null);

        activity?.SetTag(FeishuAgentDiagnostics.TagSummarized, true);
        return true;
    }

    /// <summary>保留窗大小：<c>MaxHistoryMessages/2</c>，并钳制到 <c>阈值-2</c> 以下（保证重建后低于阈值，防「每轮重复摘要」）。</summary>
    private static int ResolveRetainCount(FeishuAgentOptions options)
    {
        var threshold = Math.Max(2, options.SummaryThreshold);
        var retain = Math.Max(1, options.MaxHistoryMessages / 2);
        return Math.Min(retain, Math.Max(1, threshold - 2));
    }

    /// <summary>一次模型调用生成要点纪要（只取消息文本；工具调用噪声不入摘要输入）。</summary>
    private async Task<string?> SummarizeCoreAsync(IReadOnlyList<ChatMessage> older, CancellationToken cancellationToken)
    {
        var transcript = new StringBuilder();
        foreach (var message in older)
        {
            var text = message.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            transcript.Append('[').Append(message.Role.Value).Append("] ").AppendLine(text);
        }

        if (transcript.Length == 0)
        {
            return null;
        }

        var response = await _chatClient
            .GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, SummaryInstruction),
                new ChatMessage(ChatRole.User, transcript.ToString()),
            ], cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.Text;
    }
}
