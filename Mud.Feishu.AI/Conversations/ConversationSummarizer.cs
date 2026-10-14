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
/// 渐进式会话摘要器：历史越限时把「保留窗以外」的旧消息压成一条 system 纪要，替换原历史写回会话
/// （<see cref="FeishuAgentOptions.SummaryThreshold"/> 与 <see cref="FeishuAgentOptions.MaxHistoryTokens"/> 的消费点）。
/// </summary>
/// <remarks>
/// 四条不可破坏的约束（改动前先跑 <c>ConversationSummarizer*Tests</c>）：
/// <list type="number">
/// <item>切片起点不得落在工具调用组内：assistant 的 <c>FunctionCallContent</c> 必须与其后的
/// <c>FunctionResultContent</c> 成对保留，否则下发 400（tool 消息缺少前驱 tool_calls）且会话保存失败
/// ⇒ 重投递再次摘要、再次 400，会话永久不可用。</item>
/// <item>重建后必须低于 token 预算，否则每轮重复摘要（摘要被反复摘要）：保留窗起点取条数窗与
/// token 预算窗的较晚者；仍越限则从保留窗头部整组回退；单条即超预算时收缩到「摘要 + 最后一条」即停。</item>
/// <item>摘要模型调用失败或超时只记日志并跳过本轮压缩，绝不中断主对话。</item>
/// <item>会话历史读写走 MAF <c>AgentSessionExtensions.TryGetInMemoryChatHistory/SetInMemoryChatHistory</c>
/// （MAF 源生成序列化，AOT 安全；本类不做反射序列化）。</item>
/// </list>
/// 与 MAF Compaction 管线的取舍（勿重造）：documents/AIAgent/decisions/ADR-0001-summarizer-vs-maf-compaction.md
/// </remarks>
public sealed class ConversationSummarizer
{
    /// <summary>摘要提示词（system 侧指令；摘要产物以 system 消息注入历史）。</summary>
    private const string SummaryInstruction =
        "你是会话纪要助手。把下面的对话历史压缩成一份简洁的中文要点纪要：保留用户的目标、已确认的事实、"
        + "涉及的飞书资源（表格/文档 ID 等）与未完成的待办；不要评论、不要回答问题，只输出纪要正文。";

    /// <summary>摘要输入中每条历史文本的截断长度（纪要完整度由条数补偿，防大历史灌爆摘要调用）。</summary>
    internal const int TranscriptPerMessageLimit = 500;

    /// <summary>工具轨迹行上限（字符数）。</summary>
    internal const int ToolTraceLineLimit = 200;

    /// <summary>摘要模型调用超时钳制（超时视为失败跳过，不拖死主对话）。</summary>
    internal static readonly TimeSpan SummaryCallTimeout = TimeSpan.FromSeconds(30);

    private readonly IChatClient _chatClient;
    private readonly int _summaryThreshold;
    private readonly int _retainRecentCount;
    private readonly int _maxHistoryTokens;
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
        _maxHistoryTokens = options.MaxHistoryTokens;
        _logger = logger;
    }

    /// <summary>
    /// 会话历史是否越限（条数与 token 双窗口，先触发者生效；阈值 ≤0 视为该项禁用）。
    /// </summary>
    /// <param name="historyCount">历史消息条数。</param>
    /// <param name="historyTokens">历史消息 token 总量（<see cref="ChatTokenCounter"/>）。</param>
    public bool ShouldSummarize(int historyCount, int historyTokens)
        => (_summaryThreshold > 0 && historyCount >= _summaryThreshold)
           || (_maxHistoryTokens > 0 && historyTokens >= _maxHistoryTokens);

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

        List<ChatMessage>? history;
        try
        {
            // 状态袋的值是惰性反序列化的：内层载荷损坏到首次类型化读取时才抛。
            // 会话入口（FeishuAgent.GetOrCreateSessionAsync）已做急切校验并清理坏值；此处是纵深防御，
            // 异常不得逃出摘要器毒化会话。
            if (!session.TryGetInMemoryChatHistory(out history, FeishuAgent.ChatHistoryStateKey, null))
            {
                return false;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // catch 面收口（不得是枚举白名单）：历史载荷的首次类型化读取就在此处，白名单之外的异常会
            // 逃出摘要器并穿过调用方 ⇒ 幂等回滚 ⇒ 重投递同点再抛 ⇒ 会话永久毒化。
            // 取消必须原样传播，不得被吞成「跳过压缩」。
            _logger?.LogWarning(ex, "会话历史状态不可用，本轮跳过压缩（由会话入口的坏值自愈路径重建）");
            return false;
        }

        // netstandard2.0 的 BCL 不带 out 参数的 NotNullWhen 注解，需显式判空（对齐既有风格）。
        if (history is null || history.Count == 0)
        {
            return false;
        }

        // token 维度判定：超限时摘要先于条数窗口触发。
        var historyTokens = ChatTokenCounter.CountMessages(history);
        var countTriggered = _summaryThreshold > 0 && history.Count >= _summaryThreshold;
        var tokenTriggered = _maxHistoryTokens > 0 && historyTokens >= _maxHistoryTokens;
        if (!countTriggered && !tokenTriggered)
        {
            return false;
        }

        // 不可收敛边界：历史已缩到「摘要 + 1 条」且触发者只有 token 维度时，摘要降不下 token 量——
        // 继续摘要只会每轮多一次模型调用并反复压缩「摘要的摘要」。仅在 token 维度单独触发时短路：
        // 条数维度触发时（含 SummaryThreshold ≤ 2 的极端配置）压缩仍能降低条数，须保留既有语义。
        if (!countTriggered && tokenTriggered && history.Count <= 2)
        {
            _logger?.LogWarning(
                "单条消息即超 token 预算（{Tokens} ≥ {Budget}），摘要无法收敛——本轮跳过压缩；"
                + "请提高 MaxHistoryTokens 或截断超长消息", historyTokens, _maxHistoryTokens);
            return false;
        }

        var retain = Math.Min(_retainRecentCount, history.Count - 1);

        // 保留窗起点取条数窗口与 token 预算窗口的较晚者：保留窗自身占满预算时，收缩保留窗
        // （把更多旧内容交给摘要，而非丢弃最新内容）才能让重建后不再立即越限。
        var start = Math.Max(history.Count - retain, ResolveStartByTokenBudget(history));

        // 切片起点只能向更早扩张：起点落在工具调用组内会拆散成对的函数调用/结果（见类型 remarks 第 1 条）。
        while (start > 0 && IsToolRelated(history[start]))
        {
            start--;
        }

        if (start <= 0)
        {
            // 全部历史都在工具调用组内：本轮不压缩（宁可跳过，也不产出非法历史）。
            _logger?.LogWarning("会话历史整体处于工具调用组内，本轮跳过压缩（历史 {Count} 条）", history.Count);
            return false;
        }

        var older = history.GetRange(0, start);
        var recent = history.GetRange(start, history.Count - start);

        using var activity = FeishuAgentDiagnostics.StartSummarizeActivity(older.Count, recent.Count);

        // netstandard2.0 的 BCL 缺少流转注解：历史计数局部化供 catch 路径使用。
        var historyCount = history!.Count;

        string? summaryText;
        try
        {
            summaryText = await SummarizeCoreAsync(older, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // 超时视为失败跳过：调用方 token 未取消时不得拖死主对话。
            _logger?.LogWarning("会话摘要生成超时（{Timeout} 秒），本轮跳过压缩（历史 {Count} 条）",
                SummaryCallTimeout.TotalSeconds, historyCount);
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 失败隔离：退化为既有裁剪行为，绝不中断主对话。
            _logger?.LogWarning(ex, "会话摘要生成失败，本轮跳过压缩（历史 {Count} 条）", historyCount);
            return false;
        }

        if (string.IsNullOrWhiteSpace(summaryText))
        {
            _logger?.LogWarning("会话摘要返回空内容，本轮跳过压缩（历史 {Count} 条）", history.Count);
            return false;
        }

        var rebuilt = new List<ChatMessage>(recent.Count + 1)
        {
            new(ChatRole.System, $"[历史要点纪要]\n{summaryText!.Trim()}"),
        };
        rebuilt.AddRange(recent);

        // 收敛兜底：重建后仍越限时从保留窗头部整组回退（保留最新消息、丢弃最旧的未摘要消息；
        // 绝不从尾部删，否则模型丢失最新上下文）。内层循环保证头部不是孤立 tool 消息。
        if (_maxHistoryTokens > 0)
        {
            while (rebuilt.Count > 2 && ChatTokenCounter.CountMessages(rebuilt) >= _maxHistoryTokens)
            {
                do
                {
                    rebuilt.RemoveAt(1);
                }
                while (rebuilt.Count > 2 && IsToolRelated(rebuilt[1]));
            }
        }

        // 不可收敛可观测：收缩到「摘要 + 最后一条」仍越限时标记 Span，
        // 让宿主区分「配置/内容不匹配」与「摘要失效」，而非只看到每轮都在摘要。
        if (_maxHistoryTokens > 0 && rebuilt.Count == 2 && ChatTokenCounter.CountMessages(rebuilt) >= _maxHistoryTokens)
        {
            activity?.SetTag(FeishuAgentDiagnostics.TagSummarizeUnconverged, true);
            _logger?.LogWarning(
                "单条消息即超 token 预算（{Tokens} ≥ {Budget}），摘要无法收敛——请提高 MaxHistoryTokens 或截断超长消息",
                ChatTokenCounter.CountMessages(rebuilt), _maxHistoryTokens);
        }

        session.SetInMemoryChatHistory(rebuilt, FeishuAgent.ChatHistoryStateKey, null);

        activity?.SetTag(FeishuAgentDiagnostics.TagSummarized, true);
        return true;
    }

    /// <summary>
    /// 按 token 预算求保留窗起点：从最新一条向前累加，取「token 量 &lt; 预算」的后缀的<b>最早</b>起点。
    /// </summary>
    /// <remarks>
    /// 起点后移会让保留窗变小（更多旧消息交给摘要），不会变大：调用方以
    /// <c>Math.Max(条数起点, 本方法结果)</c> 合并，故保留窗 token 量天然 &lt; 预算。
    /// 返回值钳制在 <c>[0, count-1]</c>：至少保留最后一条。
    /// </remarks>
    /// <param name="history">会话历史。</param>
    /// <returns>预算维度的保留窗起点。</returns>
    private int ResolveStartByTokenBudget(IReadOnlyList<ChatMessage> history)
    {
        if (_maxHistoryTokens <= 0)
        {
            return 0;
        }

        var tokens = 0;
        var budgetStart = history.Count;
        for (var i = history.Count - 1; i >= 0; i--)
        {
            var next = tokens + ChatTokenCounter.Count(history[i].Text);
            if (next >= _maxHistoryTokens)
            {
                break;
            }

            tokens = next;
            budgetStart = i;
        }

        // 单条即超预算时 budgetStart == count：钳到 count-1，避免保留窗为空（丢掉全部上下文）。
        return Math.Min(budgetStart, history.Count - 1);
    }

    /// <summary>消息是否属于工具调用链（tool 结果，或携带工具调用/结果的 assistant/user 消息）。</summary>
    private static bool IsToolRelated(ChatMessage message)
        => message.Role == ChatRole.Tool
           || message.Contents.Any(static content => content is FunctionCallContent or FunctionResultContent);

    /// <summary>保留窗大小：<c>MaxHistoryMessages/2</c>；条数阈值启用时再钳制到 <c>阈值-2</c> 以下（保证重建后低于阈值）。</summary>
    private static int ResolveRetainCount(FeishuAgentOptions options)
    {
        var retain = Math.Max(1, options.MaxHistoryMessages / 2);

        // token-only 配置（SummaryThreshold=0，合法）不得把保留窗压到 1 条。
        return options.SummaryThreshold > 0
            ? Math.Min(retain, Math.Max(1, options.SummaryThreshold - 2))
            : retain;
    }

    /// <summary>
    /// 一次模型调用生成要点纪要，输入含工具轨迹压缩行（结果正文与参数值一律不入摘要输入）。
    /// </summary>
    /// <remarks>
    /// 工具调用压缩为 <c>[tool] name(标识类键) → 结果长度</c>，使摘要后「做过什么」可追溯。
    /// </remarks>
    private async Task<string?> SummarizeCoreAsync(IReadOnlyList<ChatMessage> older, CancellationToken cancellationToken)
    {
        var transcript = new StringBuilder();
        foreach (var message in older)
        {
            // 先生成工具轨迹行（来自 assistant 消息的 FunctionCallContent）。
            var toolLines = BuildToolTraceLines(message);
            if (toolLines is not null)
            {
                transcript.Append(toolLines);
                continue;
            }

            var text = message.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            // 每条截断：纪要完整度由条数补偿。
            var compressed = text.Length <= TranscriptPerMessageLimit ? text : text.Substring(0, TranscriptPerMessageLimit);
            transcript.Append('[').Append(message.Role.Value).Append("] ").AppendLine(compressed);
        }

        if (transcript.Length == 0)
        {
            return null;
        }

        // 链接调用方 token；超时视为失败，由外层隔离路径承接。
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(SummaryCallTimeout);

        var response = await _chatClient
            .GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, SummaryInstruction),
                new ChatMessage(ChatRole.User, transcript.ToString()),
            ], cancellationToken: timeoutCts.Token)
            .ConfigureAwait(false);

        return response.Text;
    }

    /// <summary>
    /// 从 assistant 消息的 <c>FunctionCallContent</c> 生成压缩工具轨迹行
    /// （<c>[tool] name(标识类键=值) → 结果长度</c>，每行上限 <see cref="ToolTraceLineLimit"/> 字符）。
    /// </summary>
    /// <remarks>
    /// 不回填结果正文（防大结果灌进摘要调用）与参数值；只保留标识类键的值，其余键名保留、值省略。
    /// </remarks>
    private static string? BuildToolTraceLines(ChatMessage message)
    {
        var functionCalls = message.Contents.OfType<FunctionCallContent>().ToList();
        if (functionCalls.Count == 0)
            return null;

        var resultContents = message.Contents.OfType<FunctionResultContent>().ToList();

        var sb = new StringBuilder();
        for (var i = 0; i < functionCalls.Count; i++)
        {
            var call = functionCalls[i];
            var toolName = call.Name ?? "unknown";

            // 标识类参数键白名单：只保留这些键的值，其余键名保留、值省略
            var idKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "app_token", "table_id", "chat_id", "message_id", "record_id",
                "file_token", "task_guid", "doc_token", "wiki_token",
            };

            var args = call.Arguments ?? new Dictionary<string, object?>();
            var idArgs = args
                .Where(kv => idKeys.Contains(kv.Key))
                .Select(kv => $"{kv.Key}={kv.Value}")
                .ToList();
            var argSummary = idArgs.Count > 0
                ? $"({string.Join(", ", idArgs)})"
                : string.Empty;

            var resultSummary = string.Empty;
            if (i < resultContents.Count && resultContents[i].Result is { } result)
            {
                var resultStr = result.ToString();
                if (!string.IsNullOrEmpty(resultStr))
                {
                    resultSummary = $" → {resultStr.Length} 字符";
                }
            }

            var line = $"[tool] {toolName}{argSummary}{resultSummary}";
            if (line.Length > ToolTraceLineLimit)
            {
                line = line.Substring(0, ToolTraceLineLimit);
            }

            sb.AppendLine(line);
        }

        return sb.ToString();
    }
}
