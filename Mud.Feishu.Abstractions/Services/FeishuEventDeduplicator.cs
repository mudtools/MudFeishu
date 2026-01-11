// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 飞书事件去重服务实现
/// 使用内存缓存 + 滑动窗口实现事件幂等性
/// </summary>
/// <remarks>
/// 此实现基于内存缓存，适用于单实例场景。
/// 对于分布式场景，建议使用基于 Redis 等外部存储的分布式去重实现。
/// </remarks>
public class FeishuEventDeduplicator : IFeishuEventDeduplicator
{
    private readonly ILogger<FeishuEventDeduplicator>? _logger;
    private readonly Dictionary<string, EventCacheEntry> _eventCache;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cacheExpiration;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _processingTimeout; // 处理中超时时间
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cacheExpiration">缓存过期时间</param>
    /// <param name="cleanupInterval">清理间隔时间</param>
    public FeishuEventDeduplicator(
        ILogger<FeishuEventDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        TimeSpan? cleanupInterval = null,
        TimeSpan? processingTimeout = null)
    {
        _logger = logger;
        _eventCache = new Dictionary<string, EventCacheEntry>();
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24); // 默认缓存24小时
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5); // 默认每5分钟清理一次
        _processingTimeout = processingTimeout ?? TimeSpan.FromMinutes(5); // 默认处理中超时5分钟

        // 启动定期清理任务
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);

        _logger?.LogInformation("飞书事件去重服务初始化完成，缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}, 处理中超时: {ProcessingTimeout}",
            _cacheExpiration, _cleanupInterval, _processingTimeout);
    }

    /// <inheritdoc/>
    public bool TryMarkAsProcessed(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            _logger?.LogWarning("事件ID为空，跳过去重检查");
            return false;
        }

        lock (_lock)
        {
            // 检查是否已存在
            if (_eventCache.ContainsKey(eventId))
            {
                _logger?.LogDebug("事件 {EventId} 已处理过，跳过", eventId);
                return true; // 已处理
            }

            // 记录新事件
            _eventCache[eventId] = new EventCacheEntry
            {
                ProcessedAt = DateTimeOffset.UtcNow,
                EventId = eventId,
                Status = DeduplicationStatus.Completed
            };

            _logger?.LogDebug("事件 {EventId} 标记为已处理", eventId);
            return false; // 未处理，新事件
        }
    }

    /// <inheritdoc/>
    public bool TryMarkAsProcessing(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            _logger?.LogWarning("事件ID为空，跳过去重检查");
            return false;
        }

        lock (_lock)
        {
            // 检查是否已存在
            if (_eventCache.TryGetValue(eventId, out var entry))
            {
                // 如果已处理，返回 true
                if (entry.Status == DeduplicationStatus.Completed)
                {
                    _logger?.LogDebug("事件 {EventId} 已处理过，跳过", eventId);
                    return true;
                }

                // 如果已在处理中，检查是否超时
                if (entry.Status == DeduplicationStatus.Processing)
                {
                    if (DateTimeOffset.UtcNow - entry.ProcessedAt > _processingTimeout)
                    {
                        // 处理中超时，允许重新处理
                        _logger?.LogWarning("事件 {EventId} 处理中超时，允许重新处理", eventId);
                        _eventCache.Remove(eventId);
                        // 继续处理
                    }
                    else
                    {
                        _logger?.LogDebug("事件 {EventId} 正在处理中，跳过", eventId);
                        return true;
                    }
                }
            }

            // 标记为处理中
            _eventCache[eventId] = new EventCacheEntry
            {
                ProcessedAt = DateTimeOffset.UtcNow,
                EventId = eventId,
                Status = DeduplicationStatus.Processing
            };

            _logger?.LogDebug("事件 {EventId} 标记为处理中", eventId);
            return false; // 未处理，新事件
        }
    }

    /// <inheritdoc/>
    public void MarkAsCompleted(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }

        lock (_lock)
        {
            if (_eventCache.TryGetValue(eventId, out var entry))
            {
                entry.Status = DeduplicationStatus.Completed;
                entry.ProcessedAt = DateTimeOffset.UtcNow; // 更新完成时间
                _logger?.LogDebug("事件 {EventId} 标记为已完成", eventId);
            }
        }
    }

    /// <inheritdoc/>
    public void RollbackProcessing(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }

        lock (_lock)
        {
            if (_eventCache.TryGetValue(eventId, out var entry))
            {
                if (entry.Status == DeduplicationStatus.Processing)
                {
                    _eventCache.Remove(eventId);
                    _logger?.LogDebug("事件 {EventId} 处理回滚，允许重新处理", eventId);
                }
                else
                {
                    _logger?.LogDebug("事件 {EventId} 状态为 {Status}，无需回滚", eventId, entry.Status);
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool IsProcessed(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return false;
        }

        lock (_lock)
        {
            return _eventCache.TryGetValue(eventId, out var entry) && entry.Status == DeduplicationStatus.Completed;
        }
    }

    /// <inheritdoc/>
    public DeduplicationStatus GetStatus(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return DeduplicationStatus.Pending;
        }

        lock (_lock)
        {
            if (_eventCache.TryGetValue(eventId, out var entry))
            {
                // 检查处理中超时
                if (entry.Status == DeduplicationStatus.Processing &&
                    DateTimeOffset.UtcNow - entry.ProcessedAt > _processingTimeout)
                {
                    return DeduplicationStatus.Pending; // 视为可重新处理
                }

                return entry.Status;
            }

            return DeduplicationStatus.Pending;
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>总缓存数量和过期数量</returns>
    public (int TotalCached, int ExpiredCount) GetCacheStats()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var expiredCount = _eventCache.Values.Count(e =>
                (now - e.ProcessedAt) > _cacheExpiration);

            return (_eventCache.Count, expiredCount);
        }
    }

    /// <summary>
    /// 清理过期条目
    /// </summary>
    private void CleanupExpiredEntries(object? state)
    {
        lock (_lock)
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
                _logger?.LogDebug("清理了 {Count} 个过期的事件缓存条目", expiredKeys.Count);
            }
        }
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            var count = _eventCache.Count;
            _eventCache.Clear();
            _logger?.LogInformation("清空了 {Count} 个事件缓存条目", count);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cleanupTimer.Dispose();

        lock (_lock)
        {
            _eventCache.Clear();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 事件缓存条目
    /// </summary>
    private class EventCacheEntry
    {
        public string EventId { get; set; } = string.Empty;
        public DateTimeOffset ProcessedAt { get; set; }
        public DeduplicationStatus Status { get; set; } = DeduplicationStatus.Pending;
    }
}
