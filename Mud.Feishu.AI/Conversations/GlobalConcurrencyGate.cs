// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Conversations;

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 全局在途背压闸门（T5 / D5）：实现 <see cref="IConversationGate"/> 接口作为装饰器，
/// 在内层 <see cref="KeyedConversationGate"/> 之前先获取全局信号量——
/// 先削峰（全局上限）、再排队（同键串行），事件入口代码零改动。
/// </summary>
/// <remarks>
/// <para>
/// <b>组合顺序（R.1.3 #5 修订）</b>：全局闸在前（先限制总量），会话闸在后（再保证同键串行）。
/// 本类型内部先获取 <see cref="SemaphoreSlim"/>、再委托内层闸门；释放时逆序（先释放会话闸、再释放全局信号量）。
/// </para>
/// <para>
/// <b>满额语义</b>：默认 <c>WaitForSlot</c>（带 <c>TurnQueueTimeoutSeconds</c> 超时），
/// 超时抛 <see cref="ConversationBusyException"/>（既有类型，幂等回滚 + 重投递）。
/// <c>FailFast</c> 模式立即抛异常（不等待）。
/// </para>
/// <para>
/// <b>分布式部署</b>：本闸门是<b>进程内</b>兜底；多实例总量限制仍需 Redis 闸门（文档化组合矩阵）。
/// </para>
/// <para>
/// <b>取消语义</b>：等待被取消时只放弃排队、<b>不</b> Release 信号量——
/// 与 <see cref="KeyedConversationGate"/> 同款取消纪律（依赖 .NET SemaphoreSlim 运行时契约）。
/// </para>
/// </remarks>
public sealed class GlobalConcurrencyGate : IConversationGate
{
    private readonly IConversationGate _inner;
    private readonly SemaphoreSlim _semaphore;
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
        _semaphore = new SemaphoreSlim(maxConcurrentTurns, maxConcurrentTurns);
        _busyMode = busyMode;
        _queueTimeout = turnQueueTimeoutSeconds > 0
            ? TimeSpan.FromSeconds(turnQueueTimeoutSeconds)
            : null;
    }

    /// <inheritdoc />
    public async Task<IConversationGateHandle> AcquireAsync(string conversationKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationKey))
            throw new ArgumentException("会话键不能为空", nameof(conversationKey));

        // FailFast 模式：立即检查，不等待
        if (_busyMode == BusyModes.FailFast && _semaphore.CurrentCount == 0)
        {
            throw new ConversationBusyException(conversationKey,
                $"全局并发已满（{_maxConcurrentTurns}/{_maxConcurrentTurns}），FailFast 模式立即拒绝");
        }

        // 获取全局信号量
        try
        {
            if (_queueTimeout is { } timeout)
            {
                // 带超时的等待
                using var linkedCts = cancellationToken.CanBeCanceled
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                    : null;

                if (linkedCts is not null)
                {
                    linkedCts.CancelAfter(timeout);
                    await _semaphore.WaitAsync(linkedCts.Token).ConfigureAwait(false);
                }
                else
                {
                    // 无外部取消，直接用超时 CTS
                    using var timeoutCts = new CancellationTokenSource(timeout);
                    await _semaphore.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
                }
            }
            else
            {
                // 不限等待（0 = 永远等待）
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // 超时触发（非外部取消）——抛 ConversationBusyException。
            // _queueTimeout 非 null 时才有超时语义（0 = 永远等待不会触发 OperationCanceledException）。
            var timeout = _queueTimeout ?? TimeSpan.Zero;
            throw new ConversationBusyException(conversationKey,
                $"全局并发队列等待超时（{timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s），"
                + $"当前在途 {_maxConcurrentTurns - _semaphore.CurrentCount}/{_maxConcurrentTurns}");
        }

        // 全局信号量已获取——委托内层闸门
        IConversationGateHandle innerHandle;
        try
        {
            innerHandle = await _inner.AcquireAsync(conversationKey, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // 内层失败——释放全局信号量（我们已持有它）
            _semaphore.Release();
            throw;
        }

        return new CompositeHandle(this, innerHandle);
    }

    private void ReleaseGlobal()
    {
        // Release 仅在持有成功后调用（计数恒为 1），不会 SemaphoreFullException。
        _semaphore.Release();
    }

    /// <summary>
    /// 组合句柄：Dispose 时逆序释放（先内层会话闸、再全局信号量）。
    /// </summary>
    private sealed class CompositeHandle(GlobalConcurrencyGate owner, IConversationGateHandle innerHandle) : IConversationGateHandle
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return; // 幂等

            // 逆序释放：先内层、后全局
            innerHandle.Dispose();
            owner.ReleaseGlobal();
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
