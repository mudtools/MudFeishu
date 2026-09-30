// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Mud.Feishu.AI.Diagnostics;

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// 飞书 Agent 主循环：<c>MAF <see cref="AIAgent"/></c> 的组合实现。
/// </summary>
/// <remarks>
/// <para>
/// <c>ChatClientAgent</c> 负责「模型→生成」，本类负责「会话解析 + 选项装配 + 飞书遥测注入」
/// （Phase 0 §3.2：组合而非继承，在进入 MAF 前插入飞书特有装配）。
/// 内部经 <see cref="InMemoryChatHistoryProvider"/> + <see cref="MessageCountingChatReducer"/>
/// 施加 <see cref="FeishuAgentOptions.MaxHistoryMessages"/> 历史裁剪窗。
/// </para>
/// <para>
/// 会话持久化：<see cref="IConversationStore"/> 只存取序列化后的会话 JSON，本类是
/// 序列化的唯一归属（<c>SerializeSessionAsync</c>/<c>DeserializeSessionAsync</c> 委托内部
/// MAF Agent），<see cref="GetOrCreateSessionAsync"/> / <see cref="SaveSessionAsync"/>
/// 为事件入口（Phase 1）提供按键存取门面。会话 JSON 损坏时按 miss 处理并重建
/// （Phase 0 §8 回滚策略），保证旧格式数据不会让事件循环崩溃。
/// </para>
/// </remarks>
public sealed class FeishuAgent : AIAgent
{
    /// <summary>ChatHistoryProvider 的状态键（会话历史在 MAF 状态袋内的命名空间）。</summary>
    /// <remarks>会话摘要器（<c>ConversationSummarizer</c>）经 MAF 扩展按同一键读写历史，键须同源。</remarks>
    internal const string ChatHistoryStateKey = "feishu.agent.history";

    /// <summary>
    /// P4-4（outbox）：「已生成但未确认送达」的回复文本状态键。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="PendingReplyTurnStateKey"/> 成对使用：后者记录该文本所属的轮次标识，
    /// 用于判定重投递事件是否就是同一轮（否则是陈旧条目，应丢弃而非补发）。
    /// 两个键都只存 <see cref="string"/>——刻意不引入自定义类型，避免 AOT 下为它准备
    /// <c>JsonTypeInfo</c>（状态袋序列化走 MAF 自带解析器）。
    /// </remarks>
    internal const string PendingReplyStateKey = "feishu.agent.pending_reply";

    /// <summary>
    /// P4-4（outbox）：<see cref="PendingReplyStateKey"/> 所属轮次的标识。
    /// </summary>
    internal const string PendingReplyTurnStateKey = "feishu.agent.pending_reply_turn";

    private readonly ChatClientAgent _innerAgent;
    private readonly FeishuAgentOptions _options;
    private readonly IConversationStore? _conversationStore;
    private readonly ConversationSummarizer? _summarizer;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="FeishuAgent"/>。
    /// </summary>
    /// <param name="chatClient">模型客户端（OpenAI-compatible 或任意 MEAI 适配器）。</param>
    /// <param name="options">Agent 配置（构造时执行 <see cref="FeishuAgentOptions.Validate"/>）。</param>
    /// <param name="conversationStore">会话存储；为 <see langword="null"/> 时不持久化（仅内存会话）。</param>
    /// <param name="loggerFactory">日志工厂（可空，MAF 内部日志兜底关闭）。</param>
    /// <param name="services">服务提供器（MAF 工具解析用，可空）。</param>
    /// <param name="tools">暴露给模型的工具（可空）。来源：容器内全部 <see cref="AIFunction"/>
    /// 注册（如 <c>Mud.Feishu.AI.FeishuTools</c> 经白名单 MapTool 后桥接产出）；
    /// 为空/空集时保持 Phase 0 裸模型行为。</param>
    /// <param name="domainGuidance">已启用工具所属域的 guidance 资产（可空；WP6 / AT-F09）。
    /// 追加在宿主 <c>Instructions</c> <b>之后</b>（宿主指令优先）；为空/空集时指令与 Phase 0 完全一致。</param>
    /// <param name="summarizer">渐进式会话摘要器（可空；R3-13 DI 化——为空时按阈值自动创建，非空时直接使用）。</param>
    /// <exception cref="InvalidOperationException">配置非法（fail-fast）。</exception>
    public FeishuAgent(
        IChatClient chatClient,
        FeishuAgentOptions options,
        IConversationStore? conversationStore = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null,
        IReadOnlyList<AIFunction>? tools = null,
        IReadOnlyList<FeishuGuidanceBlock>? domainGuidance = null,
        ConversationSummarizer? summarizer = null)
    {
        if (chatClient is null)
            throw new ArgumentNullException(nameof(chatClient));
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        // 构造即校验（Phase 0 §8：工厂启动即失败，不透传非法状态）。
        options.Validate();
        _options = options;
        _conversationStore = conversationStore;
        _logger = loggerFactory?.CreateLogger<FeishuAgent>();

        // R3-13：Summarizer DI 化——接受外部注入的替身实例；未注入时按阈值自动创建（保留既有语义）。
        _summarizer = summarizer ?? (options.SummaryThreshold > 0 || options.MaxHistoryTokens > 0
            ? new ConversationSummarizer(chatClient, options, loggerFactory?.CreateLogger<ConversationSummarizer>())
            : null);

        // 精确计数可观测（R2-4）：原先 ChatTokenCounter 的初始化失败被 catch 静默吞掉，
        // 「token 预算实际走字符估算」这一配置/依赖不匹配完全不可见。此处借宿主 logger 告警一次
        //（ChatTokenCounter 是 internal static 工具类，不引入日志面——R1 否决 3）。
        // 仅在 MaxHistoryTokens 实际启用时告警：未启用时估算与否无后果，避免噪声。
        if (options.MaxHistoryTokens > 0 && !ChatTokenCounter.IsExactCount)
        {
#if NET8_0_OR_GREATER
            var reason = ChatTokenCounter.InitializationFailure ?? "未知原因（编码器初始化失败）";
#else
            const string reason = "当前 TFM 不支持精确计数（netstandard2.0 / net6.0 走字符估算回退）";
#endif
            _logger?.LogWarning(
                "token 精确计数不可用，MaxHistoryTokens={Budget} 的预算将走字符估算（CJK≈1 token/字）：{Reason}",
                options.MaxHistoryTokens, reason);
        }

        // 指令装配的唯一消费点（WP6）：宿主指令 + 已启用域的 guidance。
        // 截断信号落在返回值上（FeishuGuidanceResult.Truncated），此处只把"丢了哪些域"写进日志——
        // 断言口在 Guidance 属性与 Compose 的返回值上（用例不依赖日志基建）。
        var guidance = FeishuGuidanceComposer.Compose(options.Instructions, domainGuidance);
        Guidance = guidance;
        if (guidance.Truncated)
        {
            _logger?.LogWarning(
                "域 guidance 超过上限 {MaxLength} 字符（宿主指令 {HostLength} 字符，guidance {GuidanceLength} 字符），"
                + "已丢弃 {OmittedCount} 个域：{OmittedDomains}——请精简 Guidance/*.md 或减少同时启用的域"
                + "（guidance 段长度含分隔符）",
                FeishuGuidanceComposer.MaxGuidanceLength,
                options.Instructions.Length,
                guidance.Instructions.Length - options.Instructions.Length,
                guidance.OmittedDomains.Count,
                string.Join(",", guidance.OmittedDomains));
        }

        var agentOptions = new ChatClientAgentOptions
        {
            Name = options.Name,
            ChatOptions = new ChatOptions
            {
                Instructions = guidance.Instructions,
                Tools = tools is { Count: > 0 } ? [.. tools] : null,
            },
            ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions
            {
                StateKey = ChatHistoryStateKey,
                ChatReducer = new MessageCountingChatReducer(options.MaxHistoryMessages),
            }),
        };

        _innerAgent = new ChatClientAgent(chatClient, agentOptions, loggerFactory, services);
    }

    /// <summary>
    /// 域 guidance 装配结果（含 <see cref="FeishuGuidanceResult.Truncated"/> 与丢弃清单；构造期一次性求值）。
    /// </summary>
    /// <remarks>
    /// R5 消费点：宿主启动冒烟/健康检查与 <c>FeishuGuidanceComposerTests</c> 的可断言面——
    /// 超限丢弃不再只以日志形式存在。
    /// </remarks>
    public FeishuGuidanceResult Guidance { get; }

    /// <summary>Agent 展示名（覆写 <see cref="AIAgent.Name"/>；不可用 <c>new</c> 遮蔽——那会使经 <see cref="AIAgent"/> 引用取值恒为基类默认 <see langword="null"/>）。</summary>
    public override string? Name => _options.Name;

    /// <inheritdoc />
    /// <remarks>
    /// 对齐 MAF <c>DelegatingAIAgent.IdCore</c> 的组合约定（委托内层）：本类与内层 <c>ChatClientAgent</c>
    /// 是同一逻辑 Agent 的两层，Id 必须一致——否则 <c>AgentResponse.AgentId</c>（由内层 Id 打标）
    /// 与 <c>FeishuAgent.Id</c> 指向不同标识，宿主按 AgentId 关联遥测/编排指标会得到「未知 Agent」。
    /// </remarks>
    protected override string? IdCore => _innerAgent.Id;

    /// <summary>
    /// 按会话键读取会话；miss（不存在、TTL 过期、载荷损坏）时创建新会话。
    /// </summary>
    /// <param name="conversationKey">由 <see cref="ConversationKeyBuilder"/> 构造的会话键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>可用会话。</returns>
    public async Task<AgentSession> GetOrCreateSessionAsync(string conversationKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(conversationKey))
            throw new ArgumentException("会话键不能为空", nameof(conversationKey));

        var stored = _conversationStore is null
            ? null
            : await _conversationStore.GetAsync(conversationKey, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(stored))
        {
            try
            {
                using var document = JsonDocument.Parse(stored!);
                var session = await DeserializeSessionAsync(
                    document.RootElement.Clone(),
                    jsonSerializerOptions: null,
                    cancellationToken).ConfigureAwait(false);

                // 急切校验（R2-2）：状态袋的值是**惰性**反序列化的（MAF AgentSessionStateBagValue 只包
                // JsonElement、不解析）——内层载荷损坏要到首次类型化读取才抛，而首次读取发生在
                // ConversationSummarizer.SummarizeIfNeededAsync（无 catch）与 Provider 内部，均在本守护区之外。
                // 不在此触发 ⇒ 异常逃出守护区 ⇒ 坏值永不删除 ⇒ 会话在其 TTL 内永久毒化（每次重投递同点再抛）。
                // 代价：每次会话恢复多一次历史反序列化（历史经 MessageCountingChatReducer 有界，成本可接受）。
                _ = session.TryGetInMemoryChatHistory(out _, ChatHistoryStateKey, null);
                return session;
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or NotSupportedException)
            {
                // 损坏/旧格式载荷视为 miss（Phase 0 §8）：**删除坏值** + 重建会话，不让事件循环崩溃。
                // MAF ChatClientAgentSession.Deserialize 对「合法 JSON 但根不是对象」（"[]"/"123"/"\"str\""）
                // 抛 ArgumentException；若不纳入 catch 面，坏载荷会在每次重投递时再次失败（毒事件循环，
                // 且坏数据永不自愈）。InvalidOperationException/NotSupportedException 一并纳入：
                // 「重建 + 删除」对结构不符/旧格式都是安全处置（丢一轮历史 << 毒事件循环）。
                _logger?.LogWarning(ex, "会话载荷不可用，已删除并重建（conversationKey: {ConversationKey}）", conversationKey);
                if (_conversationStore is not null)
                {
                    await _conversationStore.DeleteAsync(conversationKey, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return await CreateSessionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 序列化并保存会话（刷新存储 TTL）。
    /// </summary>
    /// <param name="conversationKey">由 <see cref="ConversationKeyBuilder"/> 构造的会话键。</param>
    /// <param name="session">要持久化的会话（须由本 Agent 创建）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SaveSessionAsync(string conversationKey, AgentSession session, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(conversationKey))
            throw new ArgumentException("会话键不能为空", nameof(conversationKey));
        if (session is null)
            throw new ArgumentNullException(nameof(session));
        if (_conversationStore is null)
            return;

        var serialized = await SerializeSessionAsync(session, jsonSerializerOptions: null, cancellationToken).ConfigureAwait(false);
        await _conversationStore.SaveAsync(conversationKey, serialized.GetRawText(), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        CancellationToken cancellationToken = default)
    {
        using var activity = FeishuAgentDiagnostics.StartRunActivity(
            _options.Name, FeishuAgentDiagnostics.OperationRun);

        // 历史越限时先做渐进式摘要（Phase 2 §3.2）：失败隔离，绝不影响主对话。
        if (_summarizer is not null && session is not null)
        {
            await _summarizer.SummarizeIfNeededAsync(session, cancellationToken).ConfigureAwait(false);
        }

        // 模型调用耗时指标（P1D-5：feishu.agent.llm.duration；维度 agent——原则 8 无键维度）。
        // 耗时记录放 finally（P2-8）：失败样本同样入直方图，否则 P95/P99 只反映成功路径。
        var llmStopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await _innerAgent
                .RunAsync(messages, session, options, cancellationToken)
                .ConfigureAwait(false);

            FeishuAgentDiagnostics.RecordUsage(activity, response.Usage);
            return response;
        }
        catch (Exception)
        {
            // 失败可见性（P2-8 / B3.3）：Span 标 Error，异常语义原样保留（不包装）。
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            FeishuAgentDiagnostics.RecordLlmDuration(_options.Name, llmStopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = FeishuAgentDiagnostics.StartRunActivity(
            _options.Name, FeishuAgentDiagnostics.OperationRunStreaming);

        if (_summarizer is not null && session is not null)
        {
            await _summarizer.SummarizeIfNeededAsync(session, cancellationToken).ConfigureAwait(false);
        }

        // 失败可见性（P2-8 / B3.3）：迭代器不能在含 catch 的 try 内 yield（CS1626），
        // 故把「枚举 + 异常时标记 Span Error」下沉到非迭代器包装层。
        //
        // R3-7：流式路径同样记录模型调用耗时（与非流式 RunCoreAsync 对齐）——
        // 此前只有非流式入直方图，流式（IM 会话默认路径）的 P95/P99 因此缺失。
        // try/finally 在迭代器中**合法**（CS1626 只禁止 try 块内 yield 与 catch 共存）。
        // 纪律：时长只进数值直方图（维度 agent），不得新增 appKey/sessionId 等 tag（原则 8：键只进 Span）。
        var llmStopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await foreach (var update in MarkFailureOnEnumerateAsync(
                _innerAgent.RunStreamingAsync(messages, session, options, cancellationToken), activity)
                .ConfigureAwait(false))
            {
                yield return update;
            }
        }
        finally
        {
            FeishuAgentDiagnostics.RecordLlmDuration(_options.Name, llmStopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 枚举包装：底层枚举抛出时把 Span 标为 Error 后**原样抛出**（不改异常类型，幂等回滚语义依赖原类型）。
    /// </summary>
    /// <param name="source">被包装的流。</param>
    /// <param name="activity">当前运行 Span（可空）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private static async IAsyncEnumerable<AgentResponseUpdate> MarkFailureOnEnumerateAsync(
        IAsyncEnumerable<AgentResponseUpdate> source,
        Activity? activity,
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
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }

            // yield 位于 catch 之外（CS1626 约束）。
            yield return current;
        }
    }

    /// <inheritdoc />
    protected override async ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
        => await _innerAgent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    protected override async ValueTask<JsonElement> SerializeSessionCoreAsync(
        AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions,
        CancellationToken cancellationToken = default)
        => await _innerAgent
            .SerializeSessionAsync(session, jsonSerializerOptions, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    protected override async ValueTask<AgentSession> DeserializeSessionCoreAsync(
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions,
        CancellationToken cancellationToken = default)
        => await _innerAgent
            .DeserializeSessionAsync(serializedState, jsonSerializerOptions, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    /// <remarks>
    /// 次序对齐 MAF <see cref="AIAgent.GetService"/>：**先判自身**再退内层。
    /// 否则 <c>GetService(typeof(AIAgent))</c> 会命中内层 <c>ChatClientAgent</c>（它也是 <see cref="AIAgent"/>），
    /// 宿主经该引用调用将绕过本类注入的飞书遥测与摘要。
    /// </remarks>
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType is null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (serviceKey is null && serviceType.IsInstanceOfType(this))
        {
            return this;
        }

        return _innerAgent.GetService(serviceType, serviceKey) ?? base.GetService(serviceType, serviceKey);
    }
}
