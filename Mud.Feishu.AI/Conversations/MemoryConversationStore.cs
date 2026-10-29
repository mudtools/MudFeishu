// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Conversations;

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 进程内内存会话存储（<see cref="IConversationStore"/> 的飞书侧薄壳；仅做适配）。
/// </summary>
/// <remarks>
/// <para>
/// TTL 软过期 + 惰性分摊清扫的<b>机制</b>在中立 <c>Mud.AI.Agent.Sessions.MemorySessionStore</c>；本壳保留的原因：
/// <see cref="IConversationStore"/> 是<b>已发布 SDK 的宿主可替换契约</b>，不能换成中立端口。职责有三：
/// ① 冻结契约桥接（签名不变 ⇒ 引用零改动）；② 注入 TTL 单一阈值源
/// <see cref="FeishuConversationOptions.DefaultSessionTtl"/>；③ <b>容量上限</b>（2026-10-28 G 项）。
/// </para>
/// <para>
/// 软过期只在<b>读同一键</b>时删除；写入按阈值 + 节流触发分摊清扫（无后台线程），语义与 Redis 服务端过期一致。
/// 容量驱逐按<b>首次登记序</b>驱逐最旧（同键续写不刷新位置）；驱逐后果与「TTL 过期后重建」同型。
/// 按条目数而非字节；需按字节淘汰的宿主改用 Redis 后端。
/// </para>
/// </remarks>
public sealed class MemoryConversationStore : IConversationStore
{
    internal const int SweepThreshold = MemorySessionStore.SweepThreshold;
    internal const int SweepEveryWrites = MemorySessionStore.SweepEveryWrites;
    internal const int SweepMaxRemoval = MemorySessionStore.SweepMaxRemoval;

    /// <summary>默认容量上限（10_000——单实例长跑宿主的新鲜会话窗口）。</summary>
    internal const int DefaultMaxEntries = 10_000;

    private readonly MemorySessionStore _inner;
    private readonly int _maxEntries;
    private readonly EvictionLedger _eviction = new();

    /// <summary>
    /// 初始化内存会话存储。
    /// </summary>
    /// <param name="ttl">会话 TTL（可空 ⇒ <see cref="FeishuConversationOptions.DefaultSessionTtl"/>）。</param>
    /// <param name="utcNow">UTC 时钟（测试注入用；缺省 <see cref="DateTimeOffset.UtcNow"/>）。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> 非正。</exception>
    public MemoryConversationStore(TimeSpan? ttl = null, Func<DateTimeOffset>? utcNow = null)
        : this(ttl, maxEntries: DefaultMaxEntries, utcNow)
    {
    }

    /// <summary>初始化内存会话存储（带容量上限：写入超限驱逐最旧条目）。</summary>
    /// <param name="ttl">会话 TTL（可空 ⇒ <see cref="FeishuConversationOptions.DefaultSessionTtl"/>）。</param>
    /// <param name="maxEntries">容量上限（条目数）。</param>
    /// <param name="utcNow">UTC 时钟（测试注入用；缺省 <see cref="DateTimeOffset.UtcNow"/>）。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> 非正，或 <paramref name="maxEntries"/> 非正。</exception>
    public MemoryConversationStore(TimeSpan? ttl, int maxEntries, Func<DateTimeOffset>? utcNow = null)
    {
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "容量上限必须为正");
        }

        _inner = new MemorySessionStore(ttl ?? FeishuConversationOptions.DefaultSessionTtl, utcNow);
        _maxEntries = maxEntries;
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _inner.GetAsync(key, cancellationToken).ConfigureAwait(false);

        // 读侧软过期 miss 后重置登记（新写入重新占据容量）。
        if (value is null)
        {
            _eviction.Remove(key);
        }

        return value;
    }

    /// <inheritdoc />
    public Task SaveAsync(string key, string serializedSession, CancellationToken cancellationToken = default)
    {
        _eviction.Track(key);
        EvictOverCapacity();
        return _inner.SaveAsync(key, serializedSession, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        _eviction.Remove(key);
        return _inner.DeleteAsync(key, cancellationToken);
    }

    /// <summary>当前驻留条目数（含已过期未回收者；诊断与用例观测面）。</summary>
    internal int Count => _inner.Count;

    /// <summary>清扫调用计数（诊断/测试观测面，用于断言节流效果）。</summary>
    internal int SweepCallCount => _inner.SweepCallCount;

    /// <summary>写入路径的容量驱逐：超限 ⇒ 驱逐最旧直至回到上限（被驱逐键同步从中立存储删除）。</summary>
    private void EvictOverCapacity()
    {
        while (_eviction.Count > _maxEntries)
        {
            if (!_eviction.TryEvictOldest(out var oldest))
            {
                return;
            }

            _inner.DeleteAsync(oldest, CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    /// <summary>容量驱逐登记表（插入序 = 驱逐优先序；O(1) 摊销：链表节点 + 字典索引）。</summary>
    private sealed class EvictionLedger
    {
        private readonly LinkedList<string> _order = [];
        private readonly Dictionary<string, LinkedListNode<string>> _index = new(StringComparer.Ordinal);

        public void Track(string key)
        {
            if (_index.ContainsKey(key))
            {
                return;
            }

            _index[key] = _order.AddLast(key);
        }

        public void Remove(string key)
        {
            // netstandard2.0 无 Remove(key, out node) 重载——TryGetValue + Remove（ns2.0 兼容）。
            if (_index.TryGetValue(key, out var node))
            {
                _index.Remove(key);
                _order.Remove(node);
            }
        }

        public bool TryEvictOldest(out string? key)
        {
            if (_order.First is { } first)
            {
                key = first.Value;
                _order.RemoveFirst();
                _index.Remove(key);
                return true;
            }

            key = null;
            return false;
        }

        public int Count => _order.Count;
    }
}
