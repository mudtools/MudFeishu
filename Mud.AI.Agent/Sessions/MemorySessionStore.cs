// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Mud.AI.Abstractions.Conversations;

namespace Mud.AI.Agent.Sessions;

/// <summary>
/// 进程内内存会话存储（中立默认实现：<see cref="ISessionStore"/> 面，测试/单机开箱可用）。
/// </summary>
/// <remarks>
/// <para>
/// 基于 <see cref="ConcurrentDictionary{TKey,TValue}"/>，TTL 为<b>软过期</b>：读取时判定
/// <c>{expireTimestampMs}|{payload}</c> 时间戳，过期即删除并视为 miss（对齐 Redis 实现的读侧有效性判定；
/// Redis 由服务器键过期 + 读侧时间戳双重兜底）。
/// </para>
/// <para>
/// <b>过期回收（R2-5）</b>：软过期只在<b>读同一键</b>时删除，<b>不再被读取</b>的键（一次性会话、被移出群的用户）
/// 永不回收 ⇒ 长跑宿主的内存随会话数单调增长。故在写入路径按阈值 + 节流触发一次<b>惰性分摊清扫</b>：
/// ≤<see cref="SweepThreshold"/> 条目时 O(1)；超过后按 <see cref="SweepEveryWrites"/> 次写入节流，
/// 单次成本上界 <see cref="SweepMaxRemoval"/>，<b>不引入后台线程/定时器</b>，语义与 Redis 服务端过期一致。
/// <b>能力边界</b>：清扫只回收<b>已过期</b>条目——TTL 窗口内的<b>新鲜</b>条目仍无数量上限（TTL 缓存的固有语义）；
/// 需要按数量/字节上限淘汰的宿主应改用分布式后端，本实现仅适合单实例部署与测试。
/// </para>
/// </remarks>
public sealed class MemorySessionStore : ISessionStore
{
    /// <summary>清扫触发阈值（条目数；达到则在下一次写入时做一次全量过期回收）。</summary>
    /// <remarks>分摊策略——节流触发（距上次清扫 ≥ <see cref="SweepEveryWrites"/> 次写入且条目数 ≥ 阈值），
    /// 避免稳态下每次写入全表扫描。单次成本上界为 O(n)（受 <see cref="SweepMaxRemoval"/> 限制回收条数）。</remarks>
    public const int SweepThreshold = 1024;

    /// <summary>两次清扫之间的最少写入次数（节流，避免每次写入都扫）。</summary>
    public const int SweepEveryWrites = 64;

    /// <summary>单次清扫最多回收的条目数（成本上界，与回收效果解耦）。</summary>
    public const int SweepMaxRemoval = 256;

    /// <summary>
    /// 中立默认 TTL（24 小时）。
    /// </summary>
    /// <remarks>
    /// <b>产品必须显式注入自己的 TTL 单一阈值源</b>（如飞书侧的 <c>FeishuConversationOptions.SessionTtl</c>），
    /// 本默认值只服务于「中立包独立使用」的场景（宿主/测试直接构造）——绝不作为产品默认值的来源，
    /// 否则「TTL 单一阈值源」这句契约会退化成两份可漂移的字面量。
    /// </remarks>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, StoreEntry> _entries = new(StringComparer.Ordinal);
    private readonly TimeSpan _ttl;
    private readonly Func<DateTimeOffset> _utcNow;

    // 清扫节流状态——距上次清扫的写入次数。
    private int _writesSinceSweep;

    /// <summary>清扫调用计数（诊断/测试观测面，用于断言节流效果）。</summary>
    public int SweepCallCount;

    /// <summary>
    /// 初始化内存会话存储。
    /// </summary>
    /// <param name="ttl">会话 TTL（可空 ⇒ <see cref="DefaultTtl"/>；非正即抛）。</param>
    /// <param name="utcNow">UTC 时钟（测试注入用；缺省 <see cref="DateTimeOffset.UtcNow"/>）。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> 非正。</exception>
    public MemorySessionStore(TimeSpan? ttl = null, Func<DateTimeOffset>? utcNow = null)
    {
        if (ttl.HasValue && ttl.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "会话 TTL 必须为正数");
        }

        _ttl = ttl ?? DefaultTtl;
        _utcNow = utcNow ?? (static () => DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_entries.TryGetValue(key, out var entry))
        {
            if (entry.ExpireAtMs > _utcNow().ToUnixTimeMilliseconds())
            {
                return Task.FromResult<string?>(entry.Payload);
            }

            // 软过期：惰性删除并视为 miss。经 ICollection.Remove 做值匹配删除（原子，
            // 且 netstandard2.0 无 KeyValuePair 版 TryRemove 重载）。
            ((ICollection<KeyValuePair<string, StoreEntry>>)_entries).Remove(
                new KeyValuePair<string, StoreEntry>(key, entry));
        }

        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task SaveAsync(string key, string serializedSession, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(serializedSession))
        {
            throw new ArgumentException("会话载荷不能为空", nameof(serializedSession));
        }

        // 节流触发——条目数 ≥ 阈值 **且** 距上次清扫 ≥ SweepEveryWrites 次写入。
        // 避免稳态下每次写入全表扫描（条目长期 ≥ SweepThreshold 时不得恒扫）。
        if (_entries.Count >= SweepThreshold
            && Interlocked.Increment(ref _writesSinceSweep) >= SweepEveryWrites)
        {
            _writesSinceSweep = 0;
            SweepExpired();
        }

        var expireAtMs = _utcNow().ToUnixTimeMilliseconds() + (long)_ttl.TotalMilliseconds;
        _entries[key] = new StoreEntry(serializedSession, expireAtMs);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _entries.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <summary>当前驻留条目数（含已过期未回收者；诊断与用例观测面）。</summary>
    public int Count => _entries.Count;

    /// <summary>回收已过期条目（值匹配删除，与读侧软过期同源判定；单次上限 <see cref="SweepMaxRemoval"/>）。</summary>
    /// <remarks>
    /// <see cref="ConcurrentDictionary{TKey,TValue}"/> 支持枚举期间移除；KVP 值匹配保证
    /// 不会误删「同键在枚举期间被覆盖写入」的新值（与读侧软过期同一手法）。
    /// </remarks>
    private void SweepExpired()
    {
        Interlocked.Increment(ref SweepCallCount);
        var nowMs = _utcNow().ToUnixTimeMilliseconds();
        var removed = 0;
        foreach (var pair in _entries)
        {
            if (pair.Value.ExpireAtMs <= nowMs)
            {
                ((ICollection<KeyValuePair<string, StoreEntry>>)_entries).Remove(pair);
                if (++removed >= SweepMaxRemoval)
                {
                    break;
                }
            }
        }
    }

    private sealed record StoreEntry(string Payload, long ExpireAtMs);
}
