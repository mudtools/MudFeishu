// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 基于内存的分布式事件去重服务实现
/// 适用于单机部署或开发测试环境
/// 对于分布式部署，建议使用 Redis 等外部存储实现
/// </summary>
public class FeishuWebhookDistributedDeduplicator : IFeishuWebhookDistributedDeduplicator, IAsyncDisposable
{
    private readonly ILogger<FeishuWebhookDistributedDeduplicator> _logger;
    private readonly Dictionary<string, EventCacheEntry> _eventCache;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cacheExpiration;
    private readonly TimeSpan _cleanupInterval;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FeishuWebhookDistributedDeduplicator(
        ILogger<FeishuWebhookDistributedDeduplicator> logger,
        TimeSpan? cacheExpiration = null,
        TimeSpan? cleanupInterval = null)
    {
        _logger = logger;
        _eventCache = new Dictionary<string, EventCacheEntry>();
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromMinutes(30); // 默认缓存30分钟
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5); // 默认每5分钟清理一次

        // 启动定期清理任务
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);

        _logger.LogInformation("飞书 Webhook 分布式事件去重服务初始化完成，缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}",
            _cacheExpiration, _cleanupInterval);
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkAsProcessedAsync(string eventId, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (string.IsNullOrEmpty(eventId))
        {
            _logger.LogWarning("事件ID为空，跳过去重检查");
            return false;
        }

        lock (_lock)
        {
            // 先清理已过期的条目
            CleanupExpiredEntriesLocked();

            // 检查是否已存在且未过期
            if (_eventCache.TryGetValue(eventId, out var entry))
            {
                if (DateTimeOffset.UtcNow - entry.ProcessedAt <= _cacheExpiration)
                {
                    _logger.LogDebug("事件 {EventId} 已处理过，跳过", eventId);
                    return true; // 已处理
                }
                // 已过期，删除并继续处理
                _eventCache.Remove(eventId);
            }

            // 记录新事件
            _eventCache[eventId] = new EventCacheEntry
            {
                ProcessedAt = DateTimeOffset.UtcNow,
                EventId = eventId
            };

            _logger.LogDebug("事件 {EventId} 标记为已处理", eventId);
            return false; // 未处理，新事件
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsProcessedAsync(string eventId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (string.IsNullOrEmpty(eventId))
        {
            return false;
        }

        lock (_lock)
        {
            if (!_eventCache.TryGetValue(eventId, out var entry))
                return false;

            // 检查是否已过期
            return (DateTimeOffset.UtcNow - entry.ProcessedAt) <= _cacheExpiration;
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            return CleanupExpiredEntriesLocked();
        }
    }

    /// <summary>
    /// 清理过期条目（内部方法，需要在锁内调用）
    /// </summary>
    private int CleanupExpiredEntriesLocked()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _eventCache
            .Where(kvp => (now - kvp.Value.ProcessedAt) > _cacheExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _eventCache.Remove(key);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("清理了 {Count} 个过期的事件缓存条目", expiredKeys.Count);
        }

        return expiredKeys.Count;
    }

    /// <summary>
    /// 定期清理过期条目
    /// </summary>
    private void CleanupExpiredEntries(object? state)
    {
        lock (_lock)
        {
            CleanupExpiredEntriesLocked();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
#if NETSTANDARD2_0
            return default;
#else
            return ValueTask.CompletedTask;
#endif

        _disposed = true;
        _cleanupTimer.Dispose();

        lock (_lock)
        {
            _eventCache.Clear();
        }

#if NETSTANDARD2_0
            return default;
#else
        return ValueTask.CompletedTask;
#endif
    }

    /// <summary>
    /// 事件缓存条目
    /// </summary>
    private class EventCacheEntry
    {
        public string EventId { get; set; } = string.Empty;
        public DateTimeOffset ProcessedAt { get; set; }
    }
}

/// <summary>
/// 基于内存的分布式 Nonce 去重服务实现
/// 适用于单机部署或开发测试环境
/// 对于分布式部署，建议使用 Redis 等外部存储实现
/// </summary>
public class FeishuWebhookDistributedNonceDeduplicator : IFeishuWebhookDistributedNonceDeduplicator, IAsyncDisposable
{
    private readonly ILogger<FeishuWebhookDistributedNonceDeduplicator> _logger;
    private readonly Dictionary<string, DateTimeOffset> _nonceCache;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _nonceTtl;
    private readonly TimeSpan _cleanupInterval;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FeishuWebhookDistributedNonceDeduplicator(
        ILogger<FeishuWebhookDistributedNonceDeduplicator> logger,
        TimeSpan? nonceTtl = null,
        TimeSpan? cleanupInterval = null)
    {
        _logger = logger;
        _nonceCache = new Dictionary<string, DateTimeOffset>();
        _nonceTtl = nonceTtl ?? TimeSpan.FromMinutes(5); // 默认 nonce 有效期为 5 分钟
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(1); // 默认每 1 分钟清理一次

        // 启动定期清理任务
        _cleanupTimer = new Timer(CleanupExpiredNonces, null, _cleanupInterval, _cleanupInterval);

        _logger.LogInformation("飞书 Webhook 分布式 Nonce 去重服务初始化完成，Nonce TTL: {Ttl}, 清理间隔: {CleanupInterval}",
            _nonceTtl, _cleanupInterval);
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkAsUsedAsync(string nonce, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (string.IsNullOrEmpty(nonce))
        {
            _logger.LogWarning("Nonce 为空，跳过去重检查");
            return false;
        }

        lock (_lock)
        {
            // 先清理已过期的 nonce
            CleanupExpiredNoncesLocked();

            // 检查 nonce 是否已存在且未过期
            if (_nonceCache.TryGetValue(nonce, out var createdAt))
            {
                var actualTtl = ttl ?? _nonceTtl;
                if (DateTimeOffset.UtcNow - createdAt <= actualTtl)
                {
                    _logger.LogWarning("Nonce {Nonce} 已使用过，拒绝重放攻击", nonce);
                    return true; // 已使用且未过期
                }
                // 已过期，删除并继续处理
                _nonceCache.Remove(nonce);
            }

            // 标记新 nonce（使用当前时间）
            _nonceCache[nonce] = DateTimeOffset.UtcNow;

            _logger.LogDebug("Nonce {Nonce} 标记为已使用", nonce);
            return false; // 未使用
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsUsedAsync(string nonce, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (string.IsNullOrEmpty(nonce))
        {
            return false;
        }

        lock (_lock)
        {
            if (!_nonceCache.TryGetValue(nonce, out var createdAt))
                return false;

            // 检查是否已过期
            return (DateTimeOffset.UtcNow - createdAt) <= _nonceTtl;
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            return CleanupExpiredNoncesLocked();
        }
    }

    /// <summary>
    /// 清理过期的 nonce（内部方法，需要在锁内调用）
    /// </summary>
    private int CleanupExpiredNoncesLocked()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _nonceCache
            .Where(kvp => (now - kvp.Value) > _nonceTtl)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _nonceCache.Remove(key);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("清理了 {Count} 个过期的 Nonce 缓存条目", expiredKeys.Count);
        }

        return expiredKeys.Count;
    }

    /// <summary>
    /// 定期清理过期 nonce
    /// </summary>
    private void CleanupExpiredNonces(object? state)
    {
        lock (_lock)
        {
            CleanupExpiredNoncesLocked();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
#if NETSTANDARD2_0
            return default;
#else
            return ValueTask.CompletedTask;
#endif

        _disposed = true;
        _cleanupTimer.Dispose();

        lock (_lock)
        {
            _nonceCache.Clear();
        }

#if NETSTANDARD2_0
            return default;
#else
        return ValueTask.CompletedTask;
#endif
    }
}
