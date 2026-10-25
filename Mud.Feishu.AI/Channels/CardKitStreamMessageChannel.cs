// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.DataModels.Cards;
using Mud.Feishu.DataModels.CardElements;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.Channels;

/// <summary>
/// CardKit 会话内流式通道：创建卡片实体（streaming_mode）→ 以会话气泡发出（群聊/单聊通用）
/// → 增量 <c>StreamUpdateCardTextByIdAsync</c> 打字机更新 → 终态落地完整文本。
/// </summary>
/// <remarks>
/// <b>目标语义</b>：流式目标 = 会话 chatId（本通道不实现 <see cref="IChannelTargetResolver"/>——
/// 管线默认解析即 chatId，群聊不再降级编辑通道；被 <see cref="StreamingChannelChain"/> 排列在编辑通道之前，
/// Begin 失败仍自动降级）。
/// <para>
/// <b>协议三步</b>（官方 cardkit 流式）：① <c>POST /cardkit/v1/cards</c> 建实体（卡片 JSON 2.0，
/// <c>streaming_mode=true</c>，markdown 元素承载正文）；② <c>im/v1/messages</c> 发
/// <c>{"type":"card","data":{"card_id":…}}</c>（卡片实体只能发送一次、发送方须为创建方）；
/// ③ <c>PUT /cardkit/v1/cards/{card_id}/elements/{element_id}/content</c> 推<b>全量文本</b>（平台自算增量，
/// 打字机渲染）。<c>sequence</c> 每卡片严格递增（<see cref="NextSequence"/>，飞书 300317 拒绝回退/重复号）。
/// </para>
/// <para>
/// <b>失败语义</b>（与 <see cref="EditMessageChannel"/> 同一接口契约）：<see cref="BeginAsync"/> 失败向上抛
/// （降级链回退编辑通道，模型尚未调用）；单次增量更新失败只记日志（不中断模型流）；终态落地失败由
/// 基类 Flush 语义可感知。每次下游调用前经 <see cref="IFeishuAppContextScopeFactory.BeginScope"/> 切租户，finally 释放。
/// </para>
/// <para>
/// <b>平台边界</b>（登记，不防御）：卡片实体 30KB 上限——超长终态文本会在 Flush 落地时报错上抛（显式暴露
/// 而非静默截断）；流式模式在最后一次更新 10 分钟后自动关闭（不主动关闭：官方建议手动关，但终态钩子
/// 无取消令牌，fire-and-forget 反模式换不来等价收益）；卡片实体 14 天有效（会话流式回复生命周期远短于此）。
/// </para>
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
        telemetry: telemetry)
{
    /// <summary>流式正文承载元素的 element_id（卡片内自定义，1–20 字符）。</summary>
    internal const string StreamElementId = "stream_md";

    /// <summary>占位初始文本（可见的「正在输入」占位；空 content 会被飞书拒绝）。</summary>
    private const string PlaceholderText = "…";

    private readonly Mud.Feishu.IFeishuTenantV1Card _cardClient = cardClient ?? throw new ArgumentNullException(nameof(cardClient));
    private readonly Mud.Feishu.IFeishuTenantV1CardElements _elementsClient = elementsClient ?? throw new ArgumentNullException(nameof(elementsClient));
    private readonly Mud.Feishu.IFeishuTenantV1Message _messageClient = messageClient ?? throw new ArgumentNullException(nameof(messageClient));
    private readonly IFeishuAppContextScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger? _logger = logger;

    /// <summary>per-cardId sequence 计数（飞书要求同卡片操作序号严格递增；终态后移除防泄漏）。</summary>
    private readonly ConcurrentDictionary<string, long> _sequences = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public override async Task<string> BeginAsync(string appKey, string chatId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("appKey 不能为空", nameof(appKey));
        if (string.IsNullOrWhiteSpace(chatId))
            throw new ArgumentException("chatId 不能为空（CardKit 流式以会话气泡投放，群聊/单聊通用）", nameof(chatId));

        using var scope = _scopeFactory.BeginScope(appKey);
        var create = FeishuApiResultReader.Read(await _cardClient
            .CreateCardAsync(new CreateCardRequest { Type = "card_json", Data = BuildCardJson() }, cancellationToken)
            .ConfigureAwait(false));
        if (!create.Ok || string.IsNullOrEmpty(create.Data?.CardId))
        {
            // Begin 失败向上抛：降级链尝试下一通道 / 事件处理器回退非流式（模型尚未调用，零重复成本）。
            throw new InvalidOperationException(
                $"CardKit 流式卡片实体创建失败（chatId: {chatId}）: {create.ErrorText ?? "返回空 card_id"}");
        }

        var cardId = create.Data.CardId!;
        var send = FeishuApiResultReader.Read(await _messageClient
            .SendMessageAsync(BuildCardMessageRequest(chatId, cardId), "chat_id", cancellationToken)
            .ConfigureAwait(false));
        if (!send.Ok)
        {
            throw new InvalidOperationException(
                $"CardKit 流式卡片气泡投放失败（chatId: {chatId}, cardId: {cardId}）: {send.ErrorText ?? "发送失败"}");
        }

        _sequences[cardId] = 0;
        return cardId;
    }

    /// <inheritdoc />
    protected override async Task UpdateCoreAsync(string appKey, string chatId, string messageId, string fullText, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.BeginScope(appKey);
        var outcome = FeishuApiResultReader.Read(await _elementsClient
            .StreamUpdateCardTextByIdAsync(
                messageId,
                StreamElementId,
                new StreamUpdateTextRequest { Content = fullText, Sequence = (int)NextSequence(messageId) },
                cancellationToken)
            .ConfigureAwait(false));
        if (!outcome.Ok)
        {
            _logger?.LogWarning(
                "CardKit 流式文本更新失败（cardId: {CardId}）: {Error}——停留上一次成功内容", messageId, outcome.ErrorText);
        }
    }

    /// <inheritdoc />
    protected override Task OnUpdateFailedAsync(string messageId, Exception exception)
    {
        _logger?.LogWarning(exception, "CardKit 流式文本更新异常（cardId: {CardId}）——停留上一次成功内容", messageId);
        return Task.CompletedTask;
    }

    /// <summary>降级指标（<c>feishu.agent.tool.degraded</c>，维度 app_key/reason）——产品指标名与 app_key 维度留在产品侧（中立端口只承载 reason 文案）。</summary>
    protected override void OnUpdateDegraded(string appKey, string messageId, Exception exception)
        => FeishuToolDiagnostics.RecordDegraded(appKey, FeishuMetrics.DegradedReasons.ChannelUpdateFailed);

    /// <inheritdoc />
    /// <remarks>终结态后必须移除 per-cardId sequence 登记：键随流式回复单调新增（卡片实体 ID 永不重复），漏移除即让单例字典随回复次数线性增长。</remarks>
    protected override void OnFlushed(string messageId) => _sequences.TryRemove(messageId, out _);

    /// <summary>取下一次 sequence（同卡片严格递增；AddOrUpdate 对同 key 原子，并发调用各自拿到唯一递增值）。</summary>
    private long NextSequence(string cardId) => _sequences.AddOrUpdate(cardId, 1L, static (_, current) => current + 1);

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
}