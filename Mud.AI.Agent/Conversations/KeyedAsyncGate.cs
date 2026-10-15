// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Mud.AI.Agent.Conversations;

/// <summary>
/// 进程内键控串行闸门引擎：<see cref="ConcurrentDictionary{TKey, TValue}"/> 键控
/// <see cref="SemaphoreSlim"/>——同键串行、跨键并行；键空闲（无活跃持有者）即回收信号量，
/// 防 long-tail 键泄漏。
/// </summary>
/// <remarks>
/// <para>
/// <b>形状（第七式「三段分离」的引擎段）</b>：只做"按 key 串行"这一机械语义；键构造与"空键怎么办"由调用方决定，
/// 引擎只拒绝 <see langword="null"/>/空白。等待时长由被守护的工作决定，故引擎<b>不</b>快速失败——需要它的宿主
/// 应在外层替换为带租约的实现（如分布式闸门），而非给引擎加超时配置。
/// </para>
/// <para>
/// <b>回收语义</b>：活跃计数归零即在全局锁内条件移除并 Dispose；条件移除（KVP 重载）同时保证「归零判定 → 移除」原子。
/// </para>
/// <para>
/// <b>取消语义</b>：等待被取消时只放弃登记、<b>不</b> Release——依赖 <see cref="SemaphoreSlim"/>
/// 「取消与获取竞态时许可不会丢失」的运行时契约（再 Release 会抛 <see cref="SemaphoreFullException"/>）；
/// 由 <c>KeyedAsyncGateCancellationStressTests</c> 在每个 TFM 上实证守护（备选方案见 <see cref="AcquireAsync"/> 内注释）。
/// </para>
/// </remarks>
public sealed class KeyedAsyncGate
{
    private readonly ConcurrentDictionary<string, GateEntry> _entries = new(StringComparer.Ordinal);
    private readonly object _collectLock = new();

    /// <summary>
    /// 取得 <paramref name="key"/> 的独占权；返回的句柄 <see cref="IDisposable.Dispose"/> 即放行。
    /// 同键串行、跨键并行；键空闲即回收其信号量。
    /// </summary>
    /// <param name="key">闸门键（非空；键的构造归调用方）。</param>
    /// <param name="cancellationToken">等待期间取消：只放弃登记、不 Release（见类备注）。</param>
    public async Task<IDisposable> AcquireAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("闸门键不能为空", nameof(key));
        }

        GateEntry entry;
        lock (_collectLock)
        {
            entry = _entries.GetOrAdd(key, static _ => new GateEntry());
            entry.ActiveCount++;
        }

        try
        {
            // 等待在锁外（可能长时间阻塞其他键的登记；WaitAsync 是唯一的开销点）。
            await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // 取消路径不 Release：依赖 SemaphoreSlim「取消与获取竞态时许可不会丢失」的运行时契约
            // ——此处再 Release 会超出最大计数（SemaphoreFullException）。仅登记等待放弃。
            //
            // 该契约不写死为"断言"，而由 KeyedAsyncGateCancellationStressTests 在每个 TFM 上实证守护
            //（无 SemaphoreFullException / 无许可泄漏 / 无 ObjectDisposedException）。
            // 若该用例在任一 TFM 失败 ⇒ 改用「WaitAsync(CancellationToken.None) + Task.WhenAny」的
            // 显式所有权方案（取消后已取得许可则必须 Release）。
            AbandonWait(key, entry);
            throw;
        }
        catch
        {
            AbandonWait(key, entry);
            throw;
        }

        return new GateHandle(this, key, entry);
    }

    private void ReleaseEntry(string key, GateEntry entry)
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
            if (((ICollection<KeyValuePair<string, GateEntry>>)_entries).Remove(new(key, entry)))
            {
                entry.Semaphore.Dispose();
            }
        }
    }

    /// <summary>等待未成功（取消/异常）时放弃登记：只减活跃计数、不 Release 信号量。</summary>
    private void AbandonWait(string key, GateEntry entry)
    {
        lock (_collectLock)
        {
            entry.ActiveCount--;
            if (entry.ActiveCount == 0
                && ((ICollection<KeyValuePair<string, GateEntry>>)_entries).Remove(new(key, entry)))
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

    private sealed class GateHandle(KeyedAsyncGate owner, string key, GateEntry entry) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return; // 幂等：finally + 显式双路径不重复放行。
            }

            owner.ReleaseEntry(key, entry);
        }
    }
}
