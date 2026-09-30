// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Conversations;

namespace Mud.Feishu.AI.FeishuTools.Channels;

/// <summary>
/// 流式通道降级链（AI-FD-D12 P2D-2a）：<see cref="IMessageChannel"/> 复合实现——
/// <see cref="BeginAsync"/> 按注册顺序尝试各子通道（首选卡片流，失败自动尝试编辑通道），
/// 选中的子通道承载本轮 messageId 生命周期（<see cref="WriteStreamAsync"/>/<see cref="FlushAsync"/>
/// 定向委托）；事件处理器零感知。
/// </summary>
/// <remarks>
/// <para>
/// <b>目标重解析</b>：子通道目标语义不同（卡片流 = 接收用户 open_id、编辑通道 = chat_id）。
/// 链经 <see cref="StreamingRequestContext"/> 环境量拿到会话请求后，对每个子通道按其
/// <see cref="IMessageChannelTargetResolver"/> 各自解析目标；解析为 null 表示该子通道对本次
/// 事件不适用（如卡片流不投群聊），直接跳到下一子通道。环境量缺失（外部直调）时按入参原样使用。
/// </para>
/// <para>
/// <b>互斥语义</b>：同一 messageId 生命周期只落在一个子通道（Begin 时即锁定），无混用态。
/// 全部子通道 Begin 失败时抛最后一个异常——事件处理器回退非流式路径（模型尚未调用，零重复成本）。
/// </para>
/// <para>
/// <b>归属契约（R3-15）</b>：<see cref="WriteStreamAsync"/>/<see cref="FlushAsync"/> 只接受本链
/// <see cref="BeginAsync"/> 返回的 messageId——未登记的 messageId 一律 fail-fast（不回退首选通道），
/// 杜绝「增量写到非归属子通道」的静默错投。
/// </para>
/// </remarks>
public sealed class StreamingChannelChain : IMessageChannel, IMessageChannelTargetResolver
{
    private readonly IReadOnlyList<IMessageChannel> _channels;
    private readonly ConcurrentDictionary<string, IMessageChannel> _messageOwners = new(StringComparer.Ordinal);
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="StreamingChannelChain"/>。
    /// </summary>
    /// <param name="logger">日志（可空）。</param>
    /// <param name="channels">子通道（按优先级降序；至少一个）。</param>
    public StreamingChannelChain(ILogger? logger, params IReadOnlyList<IMessageChannel> channels)
    {
        _logger = logger;
        _channels = channels?.Where(static c => c is not null).ToArray() ?? [];
        if (_channels.Count == 0)
            throw new ArgumentException("降级链至少需要一个流式通道", nameof(channels));
    }

    /// <inheritdoc />
    public string? ResolveStreamTarget(ConversationRequest request)
    {
        if (request is null)
        {
            return null;
        }

        // 第一个声明了目标语义且对本次事件适用的子通道给出链目标（事件处理器据此一次性调用 Begin）。
        foreach (var channel in _channels)
        {
            if (channel is IMessageChannelTargetResolver resolver)
            {
                var target = resolver.ResolveStreamTarget(request);
                if (!string.IsNullOrEmpty(target))
                {
                    return target;
                }
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<string> BeginAsync(string appKey, string chatId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("appKey 不能为空", nameof(appKey));

        var ambient = StreamingRequestContext.Current;
        Exception? lastError = null;
        foreach (var channel in _channels)
        {
            // 不适用（目标解析为 null）优先于失败：卡片流对群聊直接跳过，不产生无效调用。
            // 环境量缺失（外部直调）时按入参原样使用。
            string? target;
            if (channel is IMessageChannelTargetResolver resolver)
            {
                target = ambient is not null ? resolver.ResolveStreamTarget(ambient) : chatId;
            }
            else
            {
                target = chatId;
            }

            if (string.IsNullOrEmpty(target))
            {
                continue;
            }

            try
            {
                var messageId = await channel.BeginAsync(appKey, target!, cancellationToken).ConfigureAwait(false);
                _messageOwners[messageId] = channel;
                return messageId;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 能力不可用（未开通/无权限/限流形态）→ 尝试下一通道；全部失败再上抛（回退非流式）。
                lastError = ex;
                _logger?.LogWarning(ex, "流式通道 {Channel} 占位创建失败，尝试降级到下一通道（appKey: {AppKey}）",
                    channel.GetType().Name, appKey);
            }
        }

        throw lastError ?? new InvalidOperationException("流式通道降级链为空，无法创建占位消息");
    }

    /// <inheritdoc />
    /// <remarks>
    /// 并发契约同 <see cref="IMessageChannel"/>：同一 <paramref name="messageId"/> 的写与 Flush 须顺序 <c>await</c>。
    /// </remarks>
    public Task WriteStreamAsync(string appKey, string chatId, string messageId, string delta, CancellationToken cancellationToken = default)
        => ResolveOwner(messageId).WriteStreamAsync(appKey, ResolveTarget(messageId, chatId), messageId, delta, cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// R2-02：本链不继承 <c>BufferedMessageChannel</c>（无缓冲语义），故自行承担「终结态落地后清空
    /// 本 messageId 的归属登记」——且必须在 <c>finally</c> 中：Flush 抛异常时归属同样已终结，
    /// 留着会让单例字典随流式回复次数线性增长，且额外钉住整条子通道实例引用。
    /// </remarks>
    public async Task FlushAsync(string appKey, string chatId, string messageId, CancellationToken cancellationToken = default)
    {
        var target = ResolveTarget(messageId, chatId);
        try
        {
            await ResolveOwner(messageId).FlushAsync(appKey, target, messageId, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _messageOwners.TryRemove(messageId, out _);
        }
    }

    /// <summary>
    /// 解析 messageId 的归属子通道。
    /// </summary>
    /// <remarks>
    /// <b>未登记即抛（R3-15）</b>：此前未命中回退 <c>_channels[0]</c>——把「未经本链 Begin 的
    /// messageId」静默路由到首选通道，可能把增量写到错误的通道（如降级期由编辑通道承载的
    /// messageId 落到卡片流）。归属登记是本链的<b>唯一</b>路由依据，缺失即契约违背，fail-fast
    /// 比静默错投更安全。
    /// </remarks>
    /// <exception cref="InvalidOperationException">messageId 未在本链登记（未先经 <see cref="BeginAsync"/>）。</exception>
    private IMessageChannel ResolveOwner(string messageId)
        => _messageOwners.TryGetValue(messageId, out var owner)
            ? owner
            : throw new InvalidOperationException(
                "流式消息未在本链登记——WriteStream/Flush 只能用于本链 BeginAsync 返回的 messageId（R3-15："
                + "静默回退首选通道会把增量写到错误的子通道）");

    private string ResolveTarget(string messageId, string fallback)
    {
        var owner = ResolveOwner(messageId);
        var ambient = StreamingRequestContext.Current;
        return owner is IMessageChannelTargetResolver resolver && ambient is not null
            ? resolver.ResolveStreamTarget(ambient) ?? fallback
            : fallback;
    }
}
