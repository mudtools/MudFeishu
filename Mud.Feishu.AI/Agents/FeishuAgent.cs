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

    private readonly ChatClientAgent _innerAgent;
    private readonly FeishuAgentOptions _options;
    private readonly IConversationStore? _conversationStore;
    private readonly ConversationSummarizer? _summarizer;

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
    /// <exception cref="InvalidOperationException">配置非法（fail-fast）。</exception>
    public FeishuAgent(
        IChatClient chatClient,
        FeishuAgentOptions options,
        IConversationStore? conversationStore = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null,
        IReadOnlyList<AIFunction>? tools = null)
    {
        if (chatClient is null)
            throw new ArgumentNullException(nameof(chatClient));
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        // 构造即校验（Phase 0 §8：工厂启动即失败，不透传非法状态）。
        options.Validate();
        _options = options;
        _conversationStore = conversationStore;

        // 渐进式会话摘要（Phase 2 §3.2）：阈值启用（≥4，Validate 保证）时挂载，
        // 摘要与主对话共用同一模型客户端与历史状态键。
        _summarizer = options.SummaryThreshold > 0
            ? new ConversationSummarizer(chatClient, options, loggerFactory?.CreateLogger<ConversationSummarizer>())
            : null;

        var agentOptions = new ChatClientAgentOptions
        {
            Name = options.Name,
            ChatOptions = new ChatOptions
            {
                Instructions = options.Instructions,
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

    /// <summary>Agent 展示名。</summary>
    public new string Name => _options.Name;

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
                return await DeserializeSessionAsync(
                    document.RootElement.Clone(),
                    jsonSerializerOptions: null,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                // 损坏载荷视为 miss（Phase 0 §8）：丢弃旧数据、重建会话，不让事件循环崩溃。
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
        var llmStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await _innerAgent
            .RunAsync(messages, session, options, cancellationToken)
            .ConfigureAwait(false);
        FeishuAgentDiagnostics.RecordLlmDuration(_options.Name, llmStopwatch.ElapsedMilliseconds);

        FeishuAgentDiagnostics.RecordUsage(activity, response.Usage);
        return response;
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

        await foreach (var update in _innerAgent
            .RunStreamingAsync(messages, session, options, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return update;
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
    public override object? GetService(Type serviceType, object? serviceKey = null)
        => _innerAgent.GetService(serviceType, serviceKey) ?? base.GetService(serviceType, serviceKey);
}
