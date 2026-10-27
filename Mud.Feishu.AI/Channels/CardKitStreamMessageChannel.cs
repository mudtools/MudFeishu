// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.DataModels.Cards;
using Mud.Feishu.DataModels.CardElements;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.Channels;

/// <summary>
/// CardKit 会话内流式通道：建卡片实体（<c>streaming_mode</c>）→ 以会话气泡投放 →
/// 增量推全量文本（打字机）→ 终态落地完整文本。
/// </summary>
/// <remarks>
/// <b>目标语义</b>：本通道实现 <see cref="IChannelTargetResolver"/>——会话事件取
/// <see cref="ConversationRequest.ChatId"/>；业务事件（审批 / 表格变更）无 <c>ChatId</c> 时取操作人
/// <c>open_id</c>（单聊气泡），故 p2p 事件同样具备流式能力。协议三步、平台边界（30KB / 10 分钟 /
/// 14 天 / 一次性发送 / sequence 语义）与失败语义见
/// <c>documents/AIAgent/decisions/ADR-0013-cardkit-streaming-boundaries.md</c>。
/// </remarks>
/// <param name="cardClient">卡片实体客户端（Tenant 身份）。</param>
/// <param name="elementsClient">卡片元素客户端（流式文本更新）。</param>
/// <param name="messageClient">消息客户端（会话气泡投放）。</param>
/// <param name="scopeFactory">租户上下文作用域工厂。</param>
/// <param name="options">Agent 配置（<see cref="FeishuAgentOptions.MaxStreamChunkLength"/> 消费点）。</param>
/// <param name="logger">日志（可空）。</param>
/// <param name="minUpdateInterval">最小更新间隔（缺省 800ms；测试可传 0 恢复仅分片阈值语义）。</param>
/// <param name="telemetry">中立通道遥测端口（可空）。</param>
public sealed class CardKitStreamMessageChannel(
    Mud.Feishu.IFeishuTenantV1Card cardClient,
    Mud.Feishu.IFeishuTenantV1CardElements elementsClient,
    Mud.Feishu.IFeishuTenantV1Message messageClient,
    IFeishuAppContextScopeFactory scopeFactory,
    IOptions<FeishuAgentOptions> options,
    ILogger<CardKitStreamMessageChannel>? logger = null,
    TimeSpan? minUpdateInterval = null,
    IAgentTelemetry? telemetry = null) : BufferedMessageChannel(
        options?.Value.MaxStreamChunkLength ?? throw new ArgumentNullException(nameof(options)),
        minUpdateInterval: minUpdateInterval,
        telemetry: telemetry), IChannelTargetResolver
{
    /// <summary>流式正文承载元素的 element_id（卡片内自定义，1–20 字符）。</summary>
    internal const string StreamElementId = "stream_md";

    /// <summary>占位初始文本（可见的「正在输入」占位；空 content 会被飞书拒绝）。</summary>
    private const string PlaceholderText = "…";

    /// <summary>单聊气泡投放的接收方类型（业务事件无 chatId 时按操作人 open_id 投放）。</summary>
    private const string ReceiveIdTypeOpenId = "open_id";

    /// <summary>open_id 形态前缀（<c>ou_</c>；单聊操作人与「本通道不适用」的判据同源）。</summary>
    internal const string OpenIdPrefix = "ou_";

    /// <summary>会话气泡投放的接收方类型（默认语义）。</summary>
    private const string ReceiveIdTypeChatId = "chat_id";

    private readonly Mud.Feishu.IFeishuTenantV1Card _cardClient = cardClient ?? throw new ArgumentNullException(nameof(cardClient));
    private readonly Mud.Feishu.IFeishuTenantV1CardElements _elementsClient = elementsClient ?? throw new ArgumentNullException(nameof(elementsClient));
    private readonly Mud.Feishu.IFeishuTenantV1Message _messageClient = messageClient ?? throw new ArgumentNullException(nameof(messageClient));
    private readonly IFeishuAppContextScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger? _logger = logger;

    /// <summary>per-cardId 流式状态（sequence / 同文去重 / 终态结果；终态后整体移除防泄漏）。</summary>
    private readonly ConcurrentDictionary<string, CardStreamState> _streams = new(StringComparer.Ordinal);

    /// <summary>
    /// 流式目标解析：会话 <c>ChatId</c> 优先；单聊回退操作人 <c>open_id</c>（气泡进人而非进群）；
    /// 群聊无 <c>ChatId</c> 或操作人不是 open_id 形态时返回 <see langword="null"/>（本通道不适用）。
    /// </summary>
    /// <remarks>
    /// <b>只接受 open_id 形态</b>（<c>ou_</c> 前缀）而非任意 SenderId：<c>user_id</c> 形态无法以
    /// <c>receive_id_type=open_id</c> 投放，猜错会白建一张卡片实体（孤儿）。判定失败即不流式，
    /// 回退非流式回复——不引入平台 API 调用（本方法是同步契约）。
    /// </remarks>
    /// <param name="request">已规范化的会话请求。</param>
    /// <returns>流式目标；本通道不适用时为 <see langword="null"/>。</returns>
    public string? ResolveStreamTarget(ConversationRequest request)
    {
        if (request is null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.ChatId))
        {
            return request.ChatId;
        }

        return !request.Scope.IsGroup && IsOpenId(request.SenderId) ? request.SenderId : null;
    }

    /// <inheritdoc />
    public override async Task<string> BeginAsync(string appKey, string chatId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("appKey 不能为空", nameof(appKey));
        if (string.IsNullOrWhiteSpace(chatId))
            throw new ArgumentException("流式目标不能为空（会话 chatId 或单聊操作人 open_id）", nameof(chatId));

        using var scope = _scopeFactory.BeginScope(appKey);
        var create = FeishuApiResultReader.Read(await _cardClient
            .CreateCardAsync(new CreateCardRequest { Type = "card_json", Data = BuildCardJson() }, cancellationToken)
            .ConfigureAwait(false));
        if (!create.Ok || string.IsNullOrEmpty(create.Data?.CardId))
        {
            // Begin 失败向上抛：降级链尝试下一通道 / 事件处理器回退非流式（模型尚未调用，零重复成本）。
            throw new InvalidOperationException(
                $"CardKit 流式卡片实体创建失败（target: {chatId}）: {create.ErrorText ?? "返回空 card_id"}");
        }

        var cardId = create.Data.CardId!;
        var send = FeishuApiResultReader.Read(await _messageClient
            .SendMessageAsync(BuildCardMessageRequest(chatId, cardId), ReceiveIdTypeFor(chatId), cancellationToken)
            .ConfigureAwait(false));
        if (!send.Ok)
        {
            // 实体已建、气泡未出 ⇒ 平台侧留下一张无主卡片（无删除接口，14 天自然过期）。
            // 这是唯一会产生平台残留资源的失败路径，必须可观测（R3-12 降级族）。
            FeishuToolDiagnostics.RecordDegraded(appKey, FeishuMetrics.DegradedReasons.ChannelBeginOrphaned);
            throw new InvalidOperationException(
                $"CardKit 流式卡片气泡投放失败（target: {chatId}, cardId: {cardId}）: {send.ErrorText ?? "发送失败"}");
        }

        _streams[cardId] = new CardStreamState(appKey);
        return cardId;
    }

    /// <inheritdoc />
    protected override async Task UpdateCoreAsync(string appKey, string chatId, string messageId, string fullText, CancellationToken cancellationToken)
    {
        // 终态之后（或非经 Begin 的外部直调）不重放写入：既不重置 sequence（平台会以 300317 拒绝回退号），
        // 也不让字典键重新生长。
        if (!_streams.TryGetValue(messageId, out var state))
        {
            _logger?.LogWarning(
                "CardKit 流式写入缺少会话状态（cardId: {CardId}）——非经 BeginAsync 的写入或终态后的迟到写入，已跳过",
                messageId);
            return;
        }

        if (state.IsSameAsLastPushed(fullText))
        {
            return;
        }

        using var scope = _scopeFactory.BeginScope(appKey);
        var outcome = FeishuApiResultReader.Read(await _elementsClient
            .StreamUpdateCardTextByIdAsync(
                messageId,
                StreamElementId,
                new StreamUpdateTextRequest { Content = fullText, Sequence = state.NextSequence() },
                cancellationToken)
            .ConfigureAwait(false));

        state.Record(outcome.Ok, outcome.ErrorText, fullText);

        if (!outcome.Ok)
        {
            _logger?.LogWarning(
                "CardKit 流式文本更新失败（cardId: {CardId}）: {Error}——停留上一次成功内容", messageId, outcome.ErrorText);
        }
    }

    /// <inheritdoc />
    protected override Task OnUpdateFailedAsync(string messageId, Exception exception)
    {
        if (_streams.TryGetValue(messageId, out var state))
        {
            state.Record(ok: false, exception.Message, text: null);
        }

        _logger?.LogWarning(exception, "CardKit 流式文本更新异常（cardId: {CardId}）——停留上一次成功内容", messageId);
        return Task.CompletedTask;
    }

    /// <summary>降级指标（<c>feishu.agent.tool.degraded</c>，维度 app_key/reason）——产品指标名与 app_key 维度留在产品侧（中立端口只承载 reason 文案）。</summary>
    protected override void OnUpdateDegraded(string appKey, string messageId, Exception exception)
        => FeishuToolDiagnostics.RecordDegraded(appKey, FeishuMetrics.DegradedReasons.ChannelUpdateFailed);

    /// <summary>
    /// 终态钩子：基类在终态落地<b>之后</b>无条件调用（含异常与取消路径），此处承担两件事——
    /// ① 终态未落地时留下 Error + 指标（<b>可观测</b>：基类 <c>FlushAsync</c> 为 sealed，产品侧无法抛出/回滚）；
    /// ② 移除 per-cardId 状态（通道为 Singleton，键单调新增即泄漏）。
    /// </summary>
    /// <param name="messageId">占位消息 ID（本通道即 cardId）。</param>
    protected override void OnFlushed(string messageId)
    {
        if (_streams.TryRemove(messageId, out var state) && !state.LastOk)
        {
            _logger?.LogError(
                "CardKit 流式终态未落地（cardId: {CardId}，appKey: {AppKey}）: {Error}——用户看到的是半截内容",
                messageId, state.AppKey, state.LastError);
            FeishuToolDiagnostics.RecordDegraded(state.AppKey, FeishuMetrics.DegradedReasons.ChannelTerminalFailed);
        }
    }

    /// <summary>投放接收方类型：目标为 open_id 形态 ⇒ <c>open_id</c>；否则 <c>chat_id</c>。</summary>
    /// <remarks>
    /// <b>与 <see cref="ResolveStreamTarget"/> 的准入形态同源</b>：本通道只把 <c>ou_</c> 形态的目标当作单聊操作人，
    /// 而 <c>receive_id</c> 形态与类型一一对应（<c>oc_</c> = 会话、<c>ou_</c> = 用户），故分类无歧义、
    /// 也不依赖管线是否保留环境量里的 <c>ChatId</c>（少一处跨包隐式耦合）。
    /// </remarks>
    private static string ReceiveIdTypeFor(string target)
        => IsOpenId(target) ? ReceiveIdTypeOpenId : ReceiveIdTypeChatId;

    /// <summary>是否为 open_id 形态（<c>ou_</c> 前缀；只有该形态能以 <c>receive_id_type=open_id</c> 投放）。</summary>
    private static bool IsOpenId(string? id)
        => !string.IsNullOrEmpty(id) && id!.StartsWith(OpenIdPrefix, StringComparison.Ordinal);

    /// <summary>构造流式卡片 JSON 2.0（不带 header：30KB 卡片预算全部留给模型正文；streaming_mode 为流式更新前置）。</summary>
    private static string BuildCardJson()
        => new JsonObject
        {
            ["schema"] = "2.0",
            ["config"] = new JsonObject { ["streaming_mode"] = true },
            ["body"] = new JsonObject
            {
                ["elements"] = new JsonArray(new JsonObject
                {
                    ["tag"] = "markdown",
                    ["content"] = PlaceholderText,
                    ["element_id"] = StreamElementId,
                }),
            },
        }.ToJsonString();

    /// <summary>构造卡片实体消息（<c>{"type":"card","data":{"card_id":…}}</c>——官方「共享卡片实体」content 形态）。</summary>
    private static SendMessageRequest BuildCardMessageRequest(string chatId, string cardId)
        => new()
        {
            ReceiveId = chatId,
            MsgType = "interactive",
            Content = new JsonObject
            {
                ["type"] = "card",
                ["data"] = new JsonObject { ["card_id"] = cardId },
            }.ToJsonString(),
        };

    /// <summary>
    /// 单张卡片的流式状态（<see cref="ConcurrentDictionary{TKey,TValue}"/> 值为它，终态整体移除）。
    /// </summary>
    /// <remarks>
    /// <b>并发契约</b>：同一 <c>messageId</c> 的写入与收尾由调用方<b>顺序 await</b>
    /// （<see cref="IMessageChannel"/> 明文契约），故实例内字段是单写者语义，无需加锁；
    /// 不同卡片之间互不相干。
    /// </remarks>
    private sealed class CardStreamState(string appKey)
    {
        private string? _lastPushedText;
        private int _sequence;

        /// <summary>租户键（<see cref="OnFlushed"/> 拿不到 appKey，故随状态一起留存）。</summary>
        public string AppKey { get; } = appKey;

        /// <summary>最后一次下游调用的结果（初值 true：尚未发生失败）。</summary>
        public bool LastOk { get; private set; } = true;

        /// <summary>最后一次失败的原文（成功时为 <see langword="null"/>）。</summary>
        public string? LastError { get; private set; }

        /// <summary>与最后一次<b>成功</b>推送的全文相同（同文去重：省掉一次必然等价的 PUT）。</summary>
        /// <param name="fullText">待推送全文。</param>
        /// <returns>相同则 <see langword="true"/>。</returns>
        public bool IsSameAsLastPushed(string fullText)
            => _lastPushedText is not null && string.Equals(_lastPushedText, fullText, StringComparison.Ordinal);

        /// <summary>取下一次 sequence（同卡片严格递增，飞书 300317 拒绝回退/重复号）。</summary>
        /// <returns>sequence。</returns>
        public int NextSequence() => ++_sequence;

        /// <summary>记录一次下游结果。</summary>
        /// <param name="ok">是否成功。</param>
        /// <param name="error">失败原文。</param>
        /// <param name="text">本次成功落地的全文（失败传 <see langword="null"/>，保留上一条成功内容）。</param>
        public void Record(bool ok, string? error, string? text)
        {
            LastOk = ok;
            LastError = ok ? null : error;

            if (ok)
            {
                _lastPushedText = text;
            }
        }
    }
}
