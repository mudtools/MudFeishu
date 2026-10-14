// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Mud.Feishu.AI.Diagnostics;

using Mud.Feishu.Abstractions.Conversations;

using Mud.AI.Agent.Events;

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// 飞书 Agent 主循环：<c>MAF <see cref="DelegatingAIAgent"/></c> 的产品派生。
/// </summary>
/// <remarks>
/// <c>ChatClientAgent</c> 负责「模型→生成」，本类负责「会话解析 + 选项装配 + 飞书遥测注入」（组合而非继承，在进入 MAF 前插入飞书特有装配）；
/// 内部经 <see cref="InMemoryChatHistoryProvider"/> + <see cref="MessageCountingChatReducer"/> 施加
/// <see cref="FeishuAgentOptions.MaxHistoryMessages"/> 历史裁剪窗。
/// <para>
/// <b>委托语义的归属</b>：内层仍是<b>组合</b>（<c>ChatClientAgent</c> 实例），而 <c>Id</c>/<c>Name</c>/<c>Description</c>/<c>GetService</c>/
/// 会话三钩子/Run 两钩子由上游 <see cref="DelegatingAIAgent"/> 提供——本类<b>不再手写</b>这些转发成员。前置条件是上游 <c>GetService</c>
/// 必须「先判自身」：否则 <c>GetService(typeof(AIAgent))</c> 命中内层 <c>ChatClientAgent</c>，宿主经该引用调用将绕过本类注入的飞书遥测与摘要
/// （由 <c>AgentContractGuards</c> 的行为断言 + <c>FeishuAgentTests</c> 锁定）。
/// </para>
/// <para>
/// <b>会话持久化</b>：<see cref="IConversationStore"/> 只存取序列化后的会话 JSON，本类是序列化的唯一归属
/// （<c>SerializeSessionAsync</c>/<c>DeserializeSessionAsync</c> 经 <see cref="DelegatingAIAgent"/> 委托内层 MAF Agent）；
/// <see cref="GetOrCreateSessionAsync"/> / <see cref="SaveSessionAsync"/> 为事件入口提供按键存取门面；会话 JSON 损坏时按 miss 处理并重建，
/// 保证旧格式数据不会让事件循环崩溃。
/// </para>
/// </remarks>
public sealed class FeishuAgent : DelegatingAIAgent, IAgentSessionFacade
{
    /// <summary>ChatHistoryProvider 的状态键（会话历史在 MAF 状态袋内的命名空间）。</summary>
    /// <remarks>
    /// 会话摘要器（<c>ConversationSummarizer</c>）经 MAF 扩展按同一键读写历史，键须同源。
    /// D4：字面量是**产品事实**，故常量声明留在飞书侧；中性门面（<c>AgentSessionStore</c>）
    /// 只以参数接收，绝不硬编码本前缀。
    /// </remarks>
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

    private readonly FeishuAgentOptions _options;

    /// <summary>
    /// N3：中立运行期配置（由 <see cref="FeishuAgentOptions.ToRuntimeOptions"/> 投影）。
    /// </summary>
    /// <remarks>
    /// 事件入口与中立机制读这个，而不是直接读产品 Options——这样「产品事实 →
    /// 中立值类型」只有<b>一处</b>翻译点（本属性），后续把机制搬进 <c>Mud.AI.Agent</c>
    /// 时不需要再回来改编排层。
    /// </remarks>
    private readonly AgentRuntimeOptions _runtime;

    private readonly AgentSessionStore _sessionStore;
    private readonly ConversationSummarizer? _summarizer;
    private readonly ILogger? _logger;

    /// <summary>
    /// N2-f/N5：中立遥测端口（<c>IAgentTelemetry</c>）的飞书实现——无状态，故为共享单例。
    /// </summary>
    /// <remarks>
    /// 命名面（<c>feishu.agent.*</c> Span / 指标）由实现决定，中立机制只经端口埋点（②式）：
    /// 这使「迁了机制就丢了可观测性」不再可能（摘要器是最先消费它的中立类型）。
    /// </remarks>
    internal static readonly FeishuAgentTelemetry AgentTelemetry = new();

    /// <summary>
    /// 构造期装配产物（内层 Agent + guidance 装配结果）。
    /// </summary>
    /// <remarks>
    /// 为什么需要它：基类 <see cref="DelegatingAIAgent"/> 的构造需要内层 Agent，
    /// 而基类构造初始化器先于本类构造体执行——装配必须在<b>公开构造的初始化器</b>里完成。
    /// 用本类型把「内层 Agent」与「guidance 结果」一并带回私有构造，
    /// 保证 <c>Compose</c> 只求值一次（否则截断告警会重复一次）。
    /// </remarks>
    private sealed record AgentAssembly(ChatClientAgent InnerAgent, GuidanceResult Guidance);

    /// <summary>
    /// 初始化 <see cref="FeishuAgent"/>。
    /// </summary>
    /// <param name="chatClient">模型客户端（OpenAI-compatible 或任意 MEAI 适配器）。</param>
    /// <param name="options">Agent 配置（构造时执行 <see cref="FeishuAgentOptions.Validate"/>）。</param>
    /// <param name="conversationStore">会话存储；为 <see langword="null"/> 时不持久化（仅内存会话）。</param>
    /// <param name="loggerFactory">日志工厂（可空，MAF 内部日志兜底关闭）。</param>
    /// <param name="services">服务提供器（MAF 高级装配用，可空）。SDK 自身的 DI 装配（<c>AddFeishuAgent</c>）
    /// <b>不传</b>根容器——工具与 guidance 在装配期已解析为实例、历史由显式
    /// <c>InMemoryChatHistoryProvider</c> 提供，MAF 侧无需再经容器解析。宿主传入时必须保证
    /// <b>不解析 Scoped 服务</b>（本类型是 Singleton，Scoped 依赖会被钉住成为 Captive Dependency）。</param>
    /// <param name="tools">暴露给模型的工具（可空）。来源：容器内全部 <see cref="AIFunction"/>
    /// 注册（如 <c>Mud.Feishu.AI.Tools</c> 经白名单 MapTool 后桥接产出）；
    /// 为空/空集时保持 Phase 0 裸模型行为。</param>
    /// <param name="domainGuidance">已启用工具所属域的 guidance 资产（可空）。
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
        IReadOnlyList<GuidanceBlock>? domainGuidance = null,
        ConversationSummarizer? summarizer = null)
        : this(
            Assemble(chatClient, options, loggerFactory, services, tools, domainGuidance),
            chatClient,
            options,
            conversationStore,
            loggerFactory,
            summarizer)
    {
    }

    /// <summary>
    /// 装配构造（H1）：接收集中式装配产物并把内层 Agent 交给基类。
    /// </summary>
    private FeishuAgent(
        AgentAssembly assembly,
        IChatClient chatClient,
        FeishuAgentOptions options,
        IConversationStore? conversationStore,
        ILoggerFactory? loggerFactory,
        ConversationSummarizer? summarizer)
        : base(assembly.InnerAgent)
    {
        _options = options;
        _runtime = options.ToRuntimeOptions();
        _logger = loggerFactory?.CreateLogger<FeishuAgent>();
        Guidance = assembly.Guidance;

        // P1-H3：会话门面中性化——「按键存取 + 坏值自愈 + 序列化往返」的机制搬进
        // Mud.AI.Agent.Sessions.AgentSessionStore；飞书侧只提供两个产品事实：
        // ① 存储契约的防腐层（IConversationStore → 中立端口 ISessionStore，公开签名因此零变化）；
        // ② 历史状态键 ChatHistoryStateKey（字面量留在本类，绝不进中立包）。
        // codec 传 this：序列化/反序列化的归属方是本 Agent（经基类委托内层 ChatClientAgent）。
        _sessionStore = new AgentSessionStore(
            this,
            conversationStore is null ? null : new ConversationStoreSessionAdapter(conversationStore),
            ChatHistoryStateKey,
            _logger);

        // R3-13：Summarizer DI 化——接受外部注入的替身实例；未注入时按阈值自动创建（保留既有语义）。
        // N2-f：摘要机制已中立化（Mud.AI.Agent.Conversations），此处注入四个产品事实——
        // ① 配置投影（ToRuntimeOptions）；② 历史状态键；③ 飞书提示词；④ 工具轨迹标识键白名单。
        _summarizer = summarizer ?? (options.SummaryThreshold > 0 || options.MaxHistoryTokens > 0
            ? new ConversationSummarizer(
                chatClient,
                options.ToRuntimeOptions(),
                ChatHistoryStateKey,
                telemetry: AgentTelemetry,
                summaryInstruction: FeishuSummarizerFacts.SummaryInstruction,
                idKeys: FeishuSummarizerFacts.IdKeys,
                logger: loggerFactory?.CreateLogger<ConversationSummarizer>())
            : null);
    }

    /// <summary>
    /// 构造期一次性装配：fail-fast 校验 → guidance 组装 → 内层 <c>ChatClientAgent</c>。
    /// </summary>
    /// <remarks>
    /// 在公开构造的初始化器里调用 ⇒ 基类构造体执行前即抛 <see cref="InvalidOperationException"/>
    /// （Phase 0 §8：工厂启动即失败，不透传非法状态）。
    /// </remarks>
    private static AgentAssembly Assemble(
        IChatClient chatClient,
        FeishuAgentOptions options,
        ILoggerFactory? loggerFactory,
        IServiceProvider? services,
        IReadOnlyList<AIFunction>? tools,
        IReadOnlyList<GuidanceBlock>? domainGuidance)
    {
        if (chatClient is null)
            throw new ArgumentNullException(nameof(chatClient));
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        // 构造即校验（Phase 0 §8：工厂启动即失败，不透传非法状态）。
        options.Validate();

        var logger = loggerFactory?.CreateLogger<FeishuAgent>();

        // 精确计数可观测：ChatTokenCounter 是 static 无状态工具类（不引入日志面），故「token 预算实际
        // 走字符估算」这一配置/依赖不匹配由本条告警 + ChatTokenCounter.InitializationFailure 断言暴露。
        // 仅在 MaxHistoryTokens 实际启用时告警：未启用时估算与否无后果，避免噪声。
        if (options.MaxHistoryTokens > 0 && !ChatTokenCounter.IsExactCount)
        {
#if NET8_0_OR_GREATER
            var reason = ChatTokenCounter.InitializationFailure ?? "未知原因（编码器初始化失败）";
#else
            const string reason = "当前 TFM 不支持精确计数（netstandard2.0 / net6.0 走字符估算回退）";
#endif
            logger?.LogWarning(
                "token 精确计数不可用，MaxHistoryTokens={Budget} 的预算将走字符估算（CJK≈1 token/字）：{Reason}",
                options.MaxHistoryTokens, reason);
        }

        // 指令装配的唯一消费点（WP6）：宿主指令 + 已启用域的 guidance。
        // 截断信号落在返回值上（GuidanceResult.Truncated），此处只把"丢了哪些域"写进日志——
        // 断言口在 Guidance 属性与 Compose 的返回值上（用例不依赖日志基建）。
        // 预算由飞书侧显式注入（H2 中立化：中性装配器不持有产品预算）。
        var guidance = GuidanceComposer.Compose(
            options.Instructions, domainGuidance, FeishuGuidanceBudget.MaxGuidanceLength);
        if (guidance.Truncated)
        {
            logger?.LogWarning(
                "域 guidance 超过上限 {MaxLength} 字符（宿主指令 {HostLength} 字符，guidance {GuidanceLength} 字符），"
                + "已丢弃 {OmittedCount} 个域：{OmittedDomains}——请精简 Guidance/*.md 或减少同时启用的域"
                + "（guidance 段长度含分隔符）",
                FeishuGuidanceBudget.MaxGuidanceLength,
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

        return new AgentAssembly(new ChatClientAgent(chatClient, agentOptions, loggerFactory, services), guidance);
    }

    /// <summary>域 guidance 装配结果（含 <see cref="GuidanceResult.Truncated"/> 与丢弃清单；构造期一次性求值）。</summary>
    /// <remarks>
    /// R5 消费点：宿主启动冒烟/健康检查与 <c>GuidanceComposerTests</c> 的可断言面——
    /// 超限丢弃不再只以日志形式存在。
    /// </remarks>
    public GuidanceResult Guidance { get; }

    /// <summary>
    /// N3：中立运行期配置（投影产物）——事件入口 / 中立机制的唯一配置读取面。
    /// </summary>
    internal AgentRuntimeOptions RuntimeOptions => _runtime;

    /// <summary>
    /// T4 / D4：单轮工具调用预算上限（源 <see cref="FeishuAgentOptions.MaxToolCallsPerTurn"/>；
    /// 事件入口读取此值开启工具调用预算作用域）。
    /// </summary>
    internal int MaxToolCallsPerTurn => _runtime.MaxToolCallsPerTurn;

    /// <summary>
    /// 流式文本抑制选项（源 <see cref="FeishuAgentOptions.DeferTextUntilTurnComplete"/>；
    /// 事件入口读取此值决定流式增量推送策略）。
    /// </summary>
    internal bool DeferTextUntilTurnComplete => _runtime.DeferTextUntilTurnComplete;

    /// <summary>Agent 展示名（覆写 <see cref="AIAgent.Name"/>；不可用 <c>new</c> 遮蔽——那会使经 <see cref="AIAgent"/> 引用取值恒为基类默认 <see langword="null"/>）。</summary>
    /// <remarks>
    /// 取值与上游 <see cref="DelegatingAIAgent.Name"/>（<c>InnerAgent.Name</c>）一致（内层
    /// <c>ChatClientAgentOptions.Name</c> 即 <see cref="FeishuAgentOptions.Name"/>）；
    /// 保留显式覆写是为了让「本类是名字的所有者」这一事实在源码上可见，并由
    /// <c>FeishuAgent_Name_ShouldBeOverrideNotHide</c> 锁定。
    /// </remarks>
    public override string? Name => _options.Name;

    /// <summary>
    /// 按会话键读取会话；miss（不存在、TTL 过期、载荷损坏）时创建新会话。
    /// </summary>
    /// <remarks>
    /// N2-c：本方法与 <see cref="SaveSessionAsync"/> 的签名与中立端口
    /// <see cref="Mud.AI.Agent.Events.IAgentSessionFacade"/> 逐字一致 ⇒ 本类直接实现该端口
    /// （零改造），中立管线与审批续跑闭环因此不需引用任何产品类型。
    /// </remarks>
    /// <param name="conversationKey">由 <see cref="ConversationKeyBuilder"/> 构造的会话键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>可用会话。</returns>
    public Task<AgentSession> GetOrCreateSessionAsync(string conversationKey, CancellationToken cancellationToken = default)
        => _sessionStore.GetOrCreateAsync(conversationKey, cancellationToken);

    /// <summary>会话历史在框架状态袋内的状态键（产品事实；<see cref="IAgentSessionFacade"/> 的实现）。</summary>
    public string HistoryStateKey => ChatHistoryStateKey;

    /// <summary>
    /// 序列化并保存会话（刷新存储 TTL）。
    /// </summary>
    /// <param name="conversationKey">由 <see cref="ConversationKeyBuilder"/> 构造的会话键。</param>
    /// <param name="session">要持久化的会话（须由本 Agent 创建）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task SaveSessionAsync(string conversationKey, AgentSession session, CancellationToken cancellationToken = default)
        => _sessionStore.SaveAsync(conversationKey, session, cancellationToken);

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
            var response = await InnerAgent
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

        // 失败可见性：迭代器不能在含 catch 的 try 内 yield（CS1626），故把「枚举 + 异常时标记 Span Error」
        // 下沉到非迭代器包装层；try/finally 在迭代器中合法。
        // 纪律：本流式路径与 RunCoreAsync 必须同样记录耗时与 token 用量（否则 IM 会话默认路径的
        // P95/P99 与用量可观测性出现缺口），且时长只进数值直方图（维度 agent）——
        // 不得新增 appKey/sessionId 等 tag（键只进 Span）。
        var llmStopwatch = System.Diagnostics.Stopwatch.StartNew();
        UsageDetails? lastUsage = null;
        try
        {
            await foreach (var update in MarkFailureOnEnumerateAsync(
                InnerAgent.RunStreamingAsync(messages, session, options, cancellationToken), activity)
                .ConfigureAwait(false))
            {
                // token 用量（与 RunCoreAsync 对齐）：MAF 在流中经 UsageContent 上报，取最后一次
                // （其 Details 即本轮累计值）；累积逻辑必须放在循环体内——
                // MarkFailureOnEnumerateAsync 是无状态静态包装，不承载状态。
                if (update.Contents.OfType<UsageContent>().LastOrDefault() is { } usage)
                {
                    lastUsage = usage.Details;
                }

                yield return update;
            }
        }
        finally
        {
            FeishuAgentDiagnostics.RecordLlmDuration(_options.Name, llmStopwatch.ElapsedMilliseconds);
            // 用量只写 Span 标量属性（原则 8：键只进 Span、不入 Metrics tag）。
            FeishuAgentDiagnostics.RecordUsage(activity, lastUsage);
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
}
