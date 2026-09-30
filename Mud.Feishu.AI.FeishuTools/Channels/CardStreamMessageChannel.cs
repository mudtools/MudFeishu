// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Text;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.DataModels.CardMessageStream;

namespace Mud.Feishu.AI.FeishuTools.Channels;

/// <summary>
/// 应用消息卡片流通道（AI-FD-D12 P2D-2a 流式正解）：Create 占位卡片（得 <c>biz_id</c>）→
/// 增量更新 <c>preview</c>（分片缓冲语义与 <see cref="EditMessageChannel"/> 同构：达
/// <see cref="FeishuAgentOptions.MaxStreamChunkLength"/> 才更新一次）→ 终态落地完整文本。
/// </summary>
/// <remarks>
/// <para>
/// <b>目标语义</b>：应用消息卡片（feed card）按<b>用户 open_id</b> 投放（<c>user_ids</c>），
/// 与编辑通道的 chat_id 不同——本通道实现 <see cref="IMessageChannelTargetResolver"/>，
/// 单聊场景解析为发送者 open_id（<see cref="ConversationRequest.SenderId"/>）；
/// 群聊不适用（返回 null，降级链/事件处理器回退编辑通道）。
/// 降级链（<see cref="StreamingChannelChain"/>）经流式环境量按子通道各自重解析，事件处理器零感知。
/// </para>
/// <para>
/// <b>失败语义</b>（与 <see cref="EditMessageChannel"/> 完全一致，同一接口契约）：
/// <see cref="BeginAsync"/> 失败向上抛（调用方/降级链回退下一通道或非流式路径，模型尚未调用、
/// 零重复成本）；单次增量更新失败只记日志（<see cref="BufferedMessageChannel"/> 异常隔离契约，
/// 不中断模型流）——卡片停留在上一次成功内容。
/// </para>
/// <para>
/// 每次下游调用前经 <see cref="IFeishuAppContextScopeFactory.BeginScope"/> 切换租户上下文
/// （与工具执行链同一事实来源；TMA2-20），finally 释放。
/// </para>
/// </remarks>
/// <remarks>
/// 初始化 <see cref="CardStreamMessageChannel"/>。
/// </remarks>
/// <param name="cardClient">应用消息卡片流客户端（Tenant 身份）。</param>
/// <param name="scopeFactory">租户上下文作用域工厂。</param>
/// <param name="options">Agent 配置（<see cref="FeishuAgentOptions.MaxStreamChunkLength"/> 消费点）。</param>
/// <param name="logger">日志（可空）。</param>
/// <param name="minUpdateInterval">最小更新间隔（缺省 800ms；测试可传 0 恢复仅分片阈值语义）。</param>
public sealed class CardStreamMessageChannel(
    IFeishuTenantV2AppCardMessageStream cardClient,
    IFeishuAppContextScopeFactory scopeFactory,
    IOptions<FeishuAgentOptions> options,
    ILogger<CardStreamMessageChannel>? logger = null,
    TimeSpan? minUpdateInterval = null) : BufferedMessageChannel(options, minUpdateInterval), IMessageChannelTargetResolver
{
    /// <summary>占位卡片初始预览文本（可见的「正在输入」占位）。</summary>
    private const string PlaceholderPreview = "…";

    /// <summary>占位卡片标题（终端用户可见）。</summary>
    private const string CardTitle = "正在回复";

    /// <summary>更新字段：3 = 预览（流式正文落点）。</summary>
    private const string UpdateFieldPreview = "3";

    private readonly IFeishuTenantV2AppCardMessageStream _cardClient = cardClient ?? throw new ArgumentNullException(nameof(cardClient));
    private readonly IFeishuAppContextScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ConcurrentDictionary<string, string> _bizIdToTarget = new(StringComparer.Ordinal);
    private readonly ILogger? _logger = logger;

    /// <inheritdoc />
    public string? ResolveStreamTarget(ConversationRequest request)
    {
        if (request is null || request.Scope.IsGroup)
        {
            // 应用消息卡片按用户投放，群聊不适用——交回默认解析（降级链转编辑通道）。
            return null;
        }

        return string.IsNullOrWhiteSpace(request.SenderId) ? null : request.SenderId;
    }

    /// <inheritdoc />
    public async override Task<string> BeginAsync(string appKey, string chatId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("appKey 不能为空", nameof(appKey));
        if (string.IsNullOrWhiteSpace(chatId))
            throw new ArgumentException("流式目标不能为空（应用消息卡片为接收用户 open_id）", nameof(chatId));

        using var scope = _scopeFactory.BeginScope(appKey);
        var outcome = FeishuApiResultReader.Read(await _cardClient
            .CreateCardMessageStreamAsync(new CreateAppCardMessageStreamRequest
            {
                AppMessageCard = new OpenAppMessageCard
                {
                    Title = CardTitle,
                    Preview = PlaceholderPreview,
                },
                UserIds = [chatId],
            }, cancellationToken: cancellationToken)
            .ConfigureAwait(false));
        if (!outcome.Ok || string.IsNullOrEmpty(outcome.Data!.BizId))
        {
            // Begin 失败向上抛：降级链尝试下一通道 / 事件处理器回退非流式（模型尚未调用，零重复成本）。
            throw new InvalidOperationException(
                $"应用消息卡片流占位创建失败（target: {chatId}）: {outcome.ErrorText ?? "返回空 biz_id"}");
        }

        var bizId = outcome.Data.BizId!;
        _bizIdToTarget[bizId] = chatId;
        return bizId;
    }

    /// <inheritdoc />
    protected override async Task UpdateCoreAsync(string appKey, string chatId, string messageId, string fullText, CancellationToken cancellationToken)
    {
        // 按用户投放目标在 Begin 时登记；缺失（外部直调）时跳过更新并告警。
        if (!_bizIdToTarget.TryGetValue(messageId, out var userId))
        {
            _logger?.LogWarning(
                "应用消息卡片流更新缺少投放目标（bizId: {BizId}）——非经 BeginAsync 的 messageId，跳过", messageId);
            return;
        }

        using var scope = _scopeFactory.BeginScope(appKey);
        var outcome = FeishuApiResultReader.Read(await _cardClient
            .UpdateCardMessageStreamAsync(new UpdateAppCardMessageStreamRequest
            {
                FeedCards =
                [
                    new UserOpenAppFeedCardUpdater
                    {
                        UserId = userId,
                        AppMessageCard = new OpenAppMessageCard
                        {
                            Title = CardTitle,
                            Preview = fullText,
                        },
                        UpdateFields = [UpdateFieldPreview],
                    },
                ],
            }, cancellationToken: cancellationToken)
            .ConfigureAwait(false));
        if (!outcome.Ok)
        {
            _logger?.LogWarning(
                "应用消息卡片流增量更新失败（bizId: {BizId}）: {Error}——停留上一次成功内容",
                messageId, outcome.ErrorText);
        }
        else if (outcome.Data?.FailedCards is { Length: > 0 } failedCards)
        {
            _logger?.LogWarning(
                "应用消息卡片流增量更新部分失败（bizId: {BizId}，失败 {Count} 张）——停留上一次成功内容",
                messageId, failedCards.Length);
        }
    }

    /// <inheritdoc />
    protected override Task OnUpdateFailedAsync(string messageId, Exception exception)
    {
        _logger?.LogWarning(exception, "应用消息卡片流增量更新异常（bizId: {BizId}）——停留上一次成功内容", messageId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// R2-02：终结态落地后移除"bizId → 投放用户 open_id"登记。该键是<b>每次新会话单调新增的业务 ID</b>
    /// （永不重复），此前无移除点 ⇒ 单例字典随流式回复次数线性增长。
    /// </remarks>
    protected override void OnFlushed(string messageId) => _bizIdToTarget.TryRemove(messageId, out _);

}
