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
/// <c>MaxHistoryMessages/2</c>，条数阈值启用时再钳制到 <c>SummaryThreshold-2</c> 以下，
/// 保证重建后不再立即越限。
/// </para>
/// <para>
/// <b>切片合法性（工具组保护）</b>：切片起点<b>不得</b>落在工具调用组内——
/// assistant 的 <c>FunctionCallContent</c> 与其后的 <c>FunctionResultContent</c> 必须成对保留，
/// 否则下发给 OpenAI 兼容端点即 400（tool 消息缺少前驱 tool_calls），且保存不了会话 ⇒
/// 重投递再次摘要、再次 400，会话永久不可用。故起点遇工具相关消息时向更早处回退，
/// 全部历史都在工具组内时本轮直接跳过压缩。
/// </para>
/// <para>
/// <b>收敛保证</b>：重建后必须低于 token 预算，否则下一轮立即再次触发摘要（每轮一次模型调用、
/// 摘要被反复摘要）。两步实现：①保留窗起点取「条数窗口」与「token 预算窗口」的<b>较晚者</b>——
/// 超预算时收缩保留窗、把被挤出的旧消息交给摘要（而非丢弃最新内容）；
/// ②重建后仍越限（如摘要本体较大）时从保留窗<b>头部整组回退</b>。边界：
/// 单条消息即超预算时无法收敛到预算内，兜底收缩到「摘要 + 最后一条」即停，不会死循环。
/// </para>
/// <para>
/// <b>失败隔离</b>：摘要模型调用失败只记日志并跳过本轮压缩（退化为既有裁剪行为），
/// 绝不中断主对话流程。会话历史读写走 MAF
/// <c>AgentSessionExtensions.TryGetInMemoryChatHistory/SetInMemoryChatHistory</c>
/// （MAF 内部源生成序列化，AOT 安全；本类不做任何反射序列化）。
/// </para>
/// <para>
/// <b>为什么不用 MAF 的 Compaction 管线（B3.1 否决，勿重造）</b>：MAF 1.20.0 的
/// <c>CompactionProvider</c>/<c>PipelineCompactionStrategy</c>/<c>SummarizationCompactionStrategy</c>
/// 均为 <c>[Experimental]</c>，且二者叠加会造成历史<b>双份无界增长</b>：
/// <c>InMemoryChatHistoryProvider.StoreChatHistoryAsync</c> 原样追加请求+响应消息，
/// 若移除其 <c>ChatReducer</c>（Compaction 替换方案的必要前提），<c>State.Messages</c> 只增不减；
/// 同时 <c>CompactionProvider</c> 把**含被排除组**的 <c>MessageGroups</c> 也写入会话状态袋
/// ⇒ 落库 JSON 同时保存两份会话历史且均无界（Redis 后端按 key 存整包），违反「落库体积可控」。
/// 待 MAF 提供「存储侧压缩」语义后再评估迁移。
/// </para>
/// </remarks>
public sealed class ConversationSummarizer
{
    /// <summary>摘要提示词（system 侧指令；摘要产物以 system 消息注入历史）。</summary>
    private const string SummaryInstruction =
        "你是会话纪要助手。把下面的对话历史压缩成一份简洁的中文要点纪要：保留用户的目标、已确认的事实、"
        + "涉及的飞书资源（表格/文档 ID 等）与未完成的待办；不要评论、不要回答问题，只输出纪要正文。";

    /// <summary>参与摘要的每条历史文本截断长度（P2D-3b 摘要输入压缩：纪要质量由「条数多、要点全」补偿）。</summary>
    internal const int TranscriptPerMessageLimit = 500;

    /// <summary>摘要模型调用超时钳制（P2D-3b：超时视为失败跳过，不让摘要拖死主对话）。</summary>
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

    // R3-09：已删除 ShouldSummarize(int) 单参重载——全仓零调用方，主流程走双参重载。

    /// <summary>
    /// 会话历史是否需要摘要（AI-FD-D12 P2D-3a：条数与 token 双窗口，<b>先触发者生效</b>——
    /// 条数阈值 ≤0 视为禁用、token 上限 0 视为不启用；升级不破坏既有条数语义）。
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
            // 状态袋的值是**惰性**反序列化的：内层载荷损坏在**首次**类型化读取时才抛。
            // 会话入口（FeishuAgent.GetOrCreateSessionAsync）已做急切校验并删除坏值（R2-2①），
            // 此处是纵深防御——未来若出现新的读取入口，也不得让异常逃出摘要器毒化会话。
            if (!session.TryGetInMemoryChatHistory(out history, FeishuAgent.ChatHistoryStateKey, null))
            {
                return false;
            }
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            _logger?.LogWarning(ex, "会话历史状态不可用，本轮跳过压缩（由会话入口的坏值自愈路径重建）");
            return false;
        }

        // netstandard2.0 的 BCL 不带 out 参数的 NotNullWhen 注解，需显式判空（对齐既有风格）。
        if (history is null || history.Count == 0)
        {
            return false;
        }

        // token 维度判定（P2D-3a）：超限时摘要先于条数窗口触发（先触发者生效）。
        var historyTokens = ChatTokenCounter.CountMessages(history);
        var countTriggered = _summaryThreshold > 0 && history.Count >= _summaryThreshold;
        var tokenTriggered = _maxHistoryTokens > 0 && historyTokens >= _maxHistoryTokens;
        if (!countTriggered && !tokenTriggered)
        {
            return false;
        }

        // 不可收敛边界（R2-11）：历史已缩到「摘要 + 1 条」且触发者是 token 维度时，
        // 摘要无法把 token 量降下来——继续摘要只会每轮多一次模型调用，并把"摘要的摘要"反复压缩。
        // 仅在 token 维度单独触发时短路：条数维度触发时（含 SummaryThreshold ≤ 2 的极端配置）
        // 压缩仍能降低条数，必须保留原有语义。
        if (!countTriggered && tokenTriggered && history.Count <= 2)
        {
            _logger?.LogWarning(
                "单条消息即超 token 预算（{Tokens} ≥ {Budget}），摘要无法收敛——本轮跳过压缩；"
                + "请提高 MaxHistoryTokens 或截断超长消息", historyTokens, _maxHistoryTokens);
            return false;
        }

        var retain = Math.Min(_retainRecentCount, history.Count - 1);

        // 保留窗起点 = 条数窗口与 token 预算窗口的**较晚者**（P1-1）：
        // 若「保留窗自身」已占满预算，重建后必然再次越限 ⇒ 每轮重复摘要且摘要被反复摘要。
        // 以「把更多旧内容交给摘要」的方式收缩保留窗（而非丢弃最新内容），保留窗 token 量因此天然低于预算。
        var start = Math.Max(history.Count - retain, ResolveStartByTokenBudget(history));

        // 切片起点只能向「更早」扩张：不得落在工具调用组内（assistant 的 FunctionCallContent
        // 与其后的 FunctionResultContent 必须成对保留），否则下发给 OpenAI 兼容端点即 400
        //（tool 消息缺少前驱 tool_calls）⇒ 保存不了会话 ⇒ 重投递再次摘要、再次 400，会话永久不可用。
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
        var retainedCount = history.Count;

        string? summaryText;
        try
        {
            summaryText = await SummarizeCoreAsync(older, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // 摘要调用超时钳制（P2D-3b）：超时视为失败跳过——调用方 token 未取消时不得拖死主对话。
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
            // 失败隔离：退化为既有裁剪行为，绝不中断主对话（Phase 2 §8 风险应对）。
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

        // 收敛兜底（P1-1）：重建后若仍越限（摘要 + 保留窗合计超预算），从保留窗**头部**整组回退——
        // 保留最新消息、丢弃最旧的未摘要消息。绝不从尾部删除：那会让模型丢失最新上下文
        // （指标收敛、语义倒退）。内层循环保证回退后头部不是孤立 tool 消息（§工具组保护）。
        // 边界：单条消息 token 量 ≥ 预算时无法收敛到预算内，此处收缩到「摘要 + 最后一条」即停，
        // 不会死循环；下一轮在同一位置再次触发摘要属配置与内容不匹配的必然结果。
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

        // 不可收敛可观测（R2-11）：收缩到「摘要 + 最后一条」仍越限时标记 Span，
        // 让宿主能区分「配置/内容不匹配」与「摘要失效」，而不是只看到"每轮都在摘要"。
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
    /// 预算维度只会让保留窗<b>变小</b>（起点后移 = 更多旧消息交给摘要），不会变小再变大：
    /// 调用方以 <c>Math.Max(条数起点, 本方法结果)</c> 合并——保留窗自身的 token 量因此天然 &lt; 预算，
    /// 重建后不会立即再次越限（P1-1 收敛）。被挤出的消息进入摘要输入而非被丢弃。
    /// 返回值钳制在 <c>[0, count-1]</c>：至少保留最后一条（当前上下文）。
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

        // token-only 配置（SummaryThreshold=0，P2-6 起合法且生效）不得把保留窗压到 1 条。
        return options.SummaryThreshold > 0
            ? Math.Min(retain, Math.Max(1, options.SummaryThreshold - 2))
            : retain;
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

            // 摘要输入压缩（P2D-3b）：每条截断，纪要质量由「条数多、要点全」补偿。
            var compressed = text.Length <= TranscriptPerMessageLimit ? text : text.Substring(0, TranscriptPerMessageLimit);
            transcript.Append('[').Append(message.Role.Value).Append("] ").AppendLine(compressed);
        }

        if (transcript.Length == 0)
        {
            return null;
        }

        // 摘要调用超时钳制（P2D-3b，常量 30 秒）：链接调用方 token，超时视为失败（外层隔离路径承接）。
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
}
