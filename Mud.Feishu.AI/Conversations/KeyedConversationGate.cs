// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 进程内会话闸门（AI-FD-D12 P2D-1 默认实现）：<see cref="ConcurrentDictionary{TKey, TValue}"/>
/// 键控 <see cref="SemaphoreSlim"/>——同键串行、跨键并行；键空闲（无活跃持有者）即回收信号量，
/// 防 long-tail 会话键泄漏（对齐 WebSocket 信号量治理 I9 精神）。
/// </summary>
/// <remarks>
/// <para>
/// <c>AddFeishuAgent</c> 默认注册本实现；多实例部署经 Mud.Feishu.Redis 的
/// <c>AddFeishuRedisConversationGate</c> 替换。键必须由 <see cref="ConversationKeyBuilder"/> 构造
/// （D8 精神：闸门不自行拼接键）。
/// </para>
/// <para>
/// 回收语义：活跃计数归零即在全局锁内条件移除并 Dispose（比「长期闲置定时回收」更紧）；
/// <see cref="ConcurrentDictionary{TKey, TValue}"/> 的条件移除（KVP 重载）保证「归零判定 → 移除」
/// 原子，持有期竞态不会误删活跃闸门。
/// </para>
/// <para>
/// <b>取消语义（R2-10）</b>：等待被取消时只放弃登记、<b>不</b> Release——依赖 .NET
/// <see cref="SemaphoreSlim"/>「取消与获取竞态时许可不会丢失」的运行时契约（此时再 Release 会
/// 超出最大计数并抛 <see cref="SemaphoreFullException"/>）。该契约不写死为断言，而由
/// <c>KeyedConversationGateCancellationStressTests</c> 在每个 TFM 上实证守护；
/// 若其失败 ⇒ 改用「<c>WaitAsync(CancellationToken.None)</c> + <c>Task.WhenAny</c>」的显式所有权方案。
/// </para>
/// <para>
/// <b>不快速失败</b>：本实现等待直至取得许可（进程内串行语义），等待时长由它所守护的工作
/// （一次模型回合）决定，无法给出通用正确的超时值。需要「快速失败」的宿主应替换为 Redis 闸门
/// （<c>ConversationBusyException</c> 的分布式租约语义），而非给本实现加超时配置。
/// </para>
/// </remarks>
public sealed class KeyedConversationGate : IConversationGate
{
    private readonly ConcurrentDictionary<string, GateEntry> _entries = new(StringComparer.Ordinal);
    private readonly object _collectLock = new();

    /// <inheritdoc />
    public async Task<IConversationGateHandle> AcquireAsync(string conversationKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationKey))
            throw new ArgumentException("会话键不能为空", nameof(conversationKey));

        GateEntry entry;
        lock (_collectLock)
        {
            entry = _entries.GetOrAdd(conversationKey, static _ => new GateEntry());
            entry.ActiveCount++;
        }

        try
        {
            // 等待在锁外（可能长时间阻塞其他键的登记；WaitAsync 是唯一的开销点）。
            await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // 取消路径不 Release：依赖 .NET SemaphoreSlim「取消与获取竞态时许可不会丢失」的运行时契约
            // ——此处再 Release 会超出最大计数（SemaphoreFullException）。仅登记等待放弃。
            //
            // R2-10：该契约不写死为"断言"，而由 KeyedConversationGateCancellationStressTests
            // 在每个 TFM 上实证守护（无 SemaphoreFullException / 无许可泄漏 / 无 ObjectDisposedException）。
            // 若该用例在任一 TFM 失败 ⇒ 改用「WaitAsync(CancellationToken.None) + Task.WhenAny」的
            // 显式所有权方案（取消后已取得许可则必须 Release），见 R2 方案 §0.5.2。
            AbandonWait(conversationKey, entry);
            throw;
        }
        catch
        {
            AbandonWait(conversationKey, entry);
            throw;
        }

        return new GateHandle(this, conversationKey, entry);
    }

    private void ReleaseEntry(string conversationKey, GateEntry entry)
    {
        // Release 仅在持有成功后调用（计数恒为 1），不会 SemaphoreFullException。
        entry.Semaphore.Release();

        lock (_collectLock)
        {
            entry.ActiveCount--;
            if (entry.ActiveCount != 0)
            {
                return;
            }

            // 条件移除（同键同条目才移除）+ 释放信号量：新 Acquire 必然拿到新条目，不会撞上已释放信号量。
            if (((ICollection<KeyValuePair<string, GateEntry>>)_entries).Remove(new(conversationKey, entry)))
            {
                entry.Semaphore.Dispose();
            }
        }
    }

    /// <summary>等待未成功（取消/异常）时放弃登记：只减活跃计数、不 Release 信号量。</summary>
    private void AbandonWait(string conversationKey, GateEntry entry)
    {
        lock (_collectLock)
        {
            entry.ActiveCount--;
            if (entry.ActiveCount == 0
                && ((ICollection<KeyValuePair<string, GateEntry>>)_entries).Remove(new(conversationKey, entry)))
            {
                entry.Semaphore.Dispose();
            }
        }
    }

    private sealed class GateEntry
    {
        internal SemaphoreSlim Semaphore { get; } = new(1, 1);

        internal int ActiveCount { get; set; }
    }

    private sealed class GateHandle(KeyedConversationGate owner, string conversationKey, GateEntry entry) : IConversationGateHandle
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return; // 幂等：finally + 显式双路径不重复放行。
            }

            owner.ReleaseEntry(conversationKey, entry);
        }
    }
}
