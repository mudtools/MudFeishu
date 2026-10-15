// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Conversations;
using Mud.Feishu.Abstractions.Conversations;

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 全局在途背压闸门（T5 / D5）：实现 <see cref="IConversationGate"/> 接口作为装饰器，
/// 在内层 <see cref="KeyedConversationGate"/> 之前先取得全局在途许可——
/// 先削峰（全局上限）、再排队（同键串行），事件入口代码零改动。
/// </summary>
/// <remarks>
/// <para>
/// <b>三段分离（第七式 / 第八式）</b>：削峰机械语义在中立引擎
/// <see cref="ConcurrencyLimiter"/> 内（满额/超时以 <see langword="null"/> 表达）；
/// 本壳只做两件事——契约适配（<see cref="IConversationGateHandle"/>）与<b>异常语义</b>
/// （把 <see langword="null"/> 翻译成 <see cref="ConversationBusyException"/>，文案由产品决定）。
/// </para>
/// <para>
/// <b>组合顺序</b>：全局许可在前（先限制总量），会话闸在后（再保证同键串行）；释放逆序
/// （先释放会话闸、再归还全局许可）。
/// </para>
/// <para>
/// <b>分布式部署</b>：本闸门是<b>进程内</b>兜底；多实例总量限制仍需 Redis 闸门（文档化组合矩阵）。
/// </para>
/// </remarks>
public sealed class GlobalConcurrencyGate : IConversationGate
{
    private readonly IConversationGate _inner;
    private readonly ConcurrencyLimiter _limiter;
    private readonly int _maxConcurrentTurns;
    private readonly TimeSpan? _queueTimeout;
    private readonly BusyModes _busyMode;

    /// <summary>
    /// 初始化 <see cref="GlobalConcurrencyGate"/>。
    /// </summary>
    /// <param name="inner">内层会话闸门（如 <see cref="KeyedConversationGate"/>）。</param>
    /// <param name="maxConcurrentTurns">全局最大并发轮数。</param>
    /// <param name="turnQueueTimeoutSeconds">队列等待超时（秒；0 = 不限等待）。</param>
    /// <param name="busyMode">满额语义：<c>WaitForSlot</c>（默认）或 <c>FailFast</c>。</param>
    public GlobalConcurrencyGate(
        IConversationGate inner,
        int maxConcurrentTurns,
        int turnQueueTimeoutSeconds = 30,
        BusyModes busyMode = BusyModes.WaitForSlot)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _maxConcurrentTurns = maxConcurrentTurns;
        _busyMode = busyMode;
        _queueTimeout = turnQueueTimeoutSeconds > 0
            ? TimeSpan.FromSeconds(turnQueueTimeoutSeconds)
            : null;
        _limiter = new ConcurrencyLimiter(_maxConcurrentTurns, _queueTimeout, busyMode == BusyModes.FailFast);
    }

    /// <inheritdoc />
    /// <exception cref="ConversationBusyException">满额（FailFast）或队列等待超时。</exception>
    public async Task<IConversationGateHandle> AcquireAsync(
        string conversationKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationKey))
        {
            throw new ArgumentException("会话键不能为空", nameof(conversationKey));
        }

        var lease = await _limiter.TryAcquireAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new ConversationBusyException(conversationKey, BuildBusyMessage());

        IConversationGateHandle innerHandle;
        try
        {
            innerHandle = await _inner.AcquireAsync(conversationKey, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // 内层失败：归还已取得的全局许可，再原样抛出（异常类型不得被吃掉）。
            lease.Dispose();
            throw;
        }

        return new CompositeHandle(lease, innerHandle);
    }

    /// <summary>
    /// 满额文案：<c>FailFast</c> 为拒绝，其余为队列超时（两种文案与在途计数口径保持既有约定）。
    /// </summary>
    /// <remarks>
    /// 不限等待（<c>turnQueueTimeoutSeconds = 0</c> ⇒ 引擎超时为 <see langword="null"/>）时引擎<b>永不</b>
    /// 返回 <see langword="null"/>（等到取得或被取消），故非 <c>FailFast</c> 分支必有超时值。
    /// </remarks>
    private string BuildBusyMessage()
        => _busyMode == BusyModes.FailFast
            ? $"全局并发已满（{_limiter.InFlight}/{_maxConcurrentTurns}），FailFast 模式立即拒绝"
            : $"全局并发队列等待超时（{_queueTimeout!.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s），"
              + $"当前在途 {_limiter.InFlight}/{_maxConcurrentTurns}";

    /// <summary>
    /// 组合句柄：Dispose 时逆序释放（先内层会话闸、再归还全局许可）。
    /// </summary>
    /// <remarks>
    /// 不自行加幂等标志：内层句柄与引擎释放器都是幂等的（<see cref="IConversationGateHandle"/> 的
    /// 契约即"重复 Dispose 幂等"），重复调用不产生副作用。
    /// </remarks>
    private sealed class CompositeHandle(IDisposable lease, IConversationGateHandle innerHandle) : IConversationGateHandle
    {
        public void Dispose()
        {
            innerHandle.Dispose();
            lease.Dispose();
        }
    }
}

/// <summary>
/// 全局闸门满额语义（T5 / D5）。
/// </summary>
public enum BusyModes
{
    /// <summary>等待空位（默认，带队列超时）。</summary>
    WaitForSlot,

    /// <summary>立即失败（抛 <see cref="ConversationBusyException"/>）。</summary>
    FailFast,
}
