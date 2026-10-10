// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// 待确认快照的<b>进程内实现</b>（R7 / C4a 默认实现）。
/// </summary>
/// <remarks>
/// <para>
/// <b>键</b>：<c>(AppKey, RequestId)</c> 复合键——多租户宿主下不同租户恰好用了同一个
/// 框架请求标识时<b>不得</b>互相覆盖或互相消费（跨租户误伤，与令牌域 F2-1 同类形态）。
/// </para>
/// <para>
/// <b>时钟可注入</b>：<c>clock</c> 参数（默认 <see cref="DateTimeOffset.UtcNow"/>）让过期语义
/// 可被<b>确定性</b>单测覆盖——不用墙钟等待（历史教训：墙钟上界断言在并发负载下必然抖动）。
/// </para>
/// <para>
/// <b>并发</b>：单把锁保护「查—判—摘」的原子性。消费必须原子（否则两个并发批准可能都返回 true
/// ⇒ 同一次写操作被执行两次）；本实现刻意不追求无锁，因为该路径是低频人工操作。
/// </para>
/// </remarks>
public sealed class InMemoryPendingApprovalStore : IFeishuPendingApprovalStore
{
    private readonly Dictionary<(string AppKey, string RequestId), PendingApprovalSnapshot> _items = [];
    private readonly object _gate = new();
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>
    /// 初始化 <see cref="InMemoryPendingApprovalStore"/>。
    /// </summary>
    /// <param name="clock">时钟（可空 → <see cref="DateTimeOffset.UtcNow"/>；测试用确定性时钟）。</param>
    public InMemoryPendingApprovalStore(Func<DateTimeOffset>? clock = null)
        => _clock = clock ?? (static () => DateTimeOffset.UtcNow);

    /// <inheritdoc />
    public Task SaveAsync(PendingApprovalSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _items[(snapshot.AppKey, snapshot.RequestId)] = snapshot;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PendingApprovalSnapshot>> ListPendingAsync(
        string appKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(appKey))
        {
            throw new ArgumentException("必须给出应用唯一标识（多租户隔离维度）", nameof(appKey));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var now = _clock();

        lock (_gate)
        {
            var pending = _items.Values
                .Where(item => string.Equals(item.AppKey, appKey, StringComparison.Ordinal)
                    && !item.IsExpired(now))
                .ToArray();

            return Task.FromResult<IReadOnlyList<PendingApprovalSnapshot>>(pending);
        }
    }

    /// <inheritdoc />
    public Task<PendingApprovalSnapshot?> FindAsync(
        string appKey,
        string requestId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(appKey))
        {
            throw new ArgumentException("必须给出应用唯一标识（多租户隔离维度）", nameof(appKey));
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var found = _items.TryGetValue((appKey, requestId), out var snapshot)
                && !snapshot.IsExpired(_clock())
                ? snapshot
                : null;

            return Task.FromResult(found);
        }
    }

    /// <inheritdoc />
    public Task<bool> TryConsumeAsync(
        string appKey,
        string requestId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(appKey))
        {
            throw new ArgumentException("必须给出应用唯一标识（多租户隔离维度）", nameof(appKey));
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_items.TryGetValue((appKey, requestId), out var snapshot))
            {
                return Task.FromResult(false);
            }

            // 「查—判—摘」在同一把锁内：过期项一并摘除（不再出现在待办里），且消费失败。
            _items.Remove((appKey, requestId));
            return Task.FromResult(!snapshot.IsExpired(_clock()));
        }
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string appKey, string requestId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(appKey))
        {
            throw new ArgumentException("必须给出应用唯一标识（多租户隔离维度）", nameof(appKey));
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(_items.Remove((appKey, requestId)));
        }
    }

    /// <inheritdoc />
    public Task<int> RemoveExpiredAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = _clock();

        lock (_gate)
        {
            var expired = _items
                .Where(kv => kv.Value.IsExpired(now))
                .Select(static kv => kv.Key)
                .ToArray();

            foreach (var key in expired)
            {
                _items.Remove(key);
            }

            return Task.FromResult(expired.Length);
        }
    }
}
