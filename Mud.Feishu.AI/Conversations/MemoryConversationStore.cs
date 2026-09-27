// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 进程内内存会话存储（测试/单机默认实现）。
/// </summary>
/// <remarks>
/// <para>
/// 基于 <see cref="ConcurrentDictionary{TKey,TValue}"/>，TTL 为<b>软过期</b>：读取时判定
/// <c>{expireTimestampMs}|{payload}</c> 时间戳，过期即删除并视为 miss（对齐 Redis 实现的
/// 读侧有效性判定；Redis 由服务器键过期 + 读侧时间戳双重兜底）。
/// </para>
/// <para>
/// 仅适合单实例部署与测试；分布式部署请使用 Mud.Feishu.Redis 的
/// <c>RedisConversationStore</c>（接口注入替换本实现）。
/// </para>
/// </remarks>
public sealed class MemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, StoreEntry> _entries = new(StringComparer.Ordinal);
    private readonly TimeSpan _ttl;
    private readonly Func<DateTimeOffset> _utcNow;

    /// <summary>
    /// 初始化内存会话存储。
    /// </summary>
    /// <param name="ttl">会话 TTL（必须为正；DI 注册默认取 <see cref="Mud.Feishu.Abstractions.Configuration.FeishuConversationOptions.SessionTtl"/>）。</param>
    /// <param name="utcNow">UTC 时钟（测试注入用；缺省 <see cref="DateTimeOffset.UtcNow"/>）。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> 非正。</exception>
    public MemoryConversationStore(TimeSpan? ttl = null, Func<DateTimeOffset>? utcNow = null)
    {
        if (ttl.HasValue && ttl.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ttl), "会话 TTL 必须为正数");

        _ttl = ttl ?? TimeSpan.FromHours(24);
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
            throw new ArgumentException("会话载荷不能为空", nameof(serializedSession));

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

    private sealed record StoreEntry(string Payload, long ExpireAtMs);
}
