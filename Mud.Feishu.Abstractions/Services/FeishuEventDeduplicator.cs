// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 飞书事件去重服务实现
/// 使用内存缓存 + 滑动窗口实现事件幂等性
/// </summary>
/// <remarks>
/// 此实现基于内存缓存，适用于单实例场景。
/// 对于分布式场景，建议使用基于 Redis 等外部存储的分布式去重实现。
/// </remarks>
public class FeishuEventDeduplicator : IFeishuEventDeduplicator, IDisposable, IAsyncDisposable
{
    private readonly ILogger<FeishuEventDeduplicator>? _logger;
    private readonly Dictionary<string, EventCacheEntry> _eventCache;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cacheExpiration;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _processingTimeout;
    private readonly int _maxCacheSize;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cacheExpiration">缓存过期时间</param>
    /// <param name="cleanupInterval">清理间隔时间</param>
    /// <param name="processingTimeout">处理中超时时间</param>
    public FeishuEventDeduplicator(
        ILogger<FeishuEventDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        TimeSpan? cleanupInterval = null,
        TimeSpan? processingTimeout = null,
        int maxCacheSize = 100000)
    {
        _logger = logger;
        _eventCache = new Dictionary<string, EventCacheEntry>();
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5);
        _processingTimeout = processingTimeout ?? TimeSpan.FromMinutes(5);
        _maxCacheSize = Math.Max(0, maxCacheSize);

        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);

        if (_logger != null && _logger.IsEnabled(LogLevel.Information))
            _logger?.LogInformation("飞书事件去重服务初始化完成，缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}, 处理中超时: {ProcessingTimeout}",
            _cacheExpiration, _cleanupInterval, _processingTimeout);
    }

    /// <summary>
    /// 使用统一配置构造
    /// </summary>
    /// <param name="options">去重配置选项</param>
    /// <param name="logger">日志记录器（可选）</param>
    public FeishuEventDeduplicator(DeduplicationOptions options, ILogger<FeishuEventDeduplicator>? logger = null)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _logger = logger;
        _eventCache = new Dictionary<string, EventCacheEntry>();
        _cacheExpiration = options.CacheExpiration;
        _cleanupInterval = options.CleanupInterval;
        _processingTimeout = options.ProcessingTimeout;
        _maxCacheSize = options.MaxCacheSize;

        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);

        if (_logger != null && _logger.IsEnabled(LogLevel.Information))
            _logger?.LogInformation("飞书事件去重服务初始化完成（使用统一配置），缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}, 处理中超时: {ProcessingTimeout}",
            _cacheExpiration, _cleanupInterval, _processingTimeout);
    }

    /// <inheritdoc/>
    public bool TryMarkAsProcessed(string eventId, string? appKey = null)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(eventId))
        {
            if (_logger != null && _logger.IsEnabled(LogLevel.Warning))
                _logger?.LogWarning("事件ID为空，跳过去重检查");
            return false;
        }

        var cacheKey = BuildCacheKey(eventId, appKey);

        lock (_lock)
        {
            // 检查是否已存在
            if (_eventCache.ContainsKey(cacheKey))
            {
                if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
                    _logger?.LogDebug("事件 {EventId}（AppKey: {AppKey}）已处理过，跳过", eventId, appKey ?? "null");
                return true; // 已处理
            }

            // 记录新事件
            EnsureCapacityLocked();
            _eventCache[cacheKey] = new EventCacheEntry
            {
                ProcessedAt = DateTimeOffset.UtcNow,
                EventId = eventId,
                AppKey = appKey,
                Status = DeduplicationStatus.Completed
            };
            if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
                _logger?.LogDebug("事件 {EventId}（AppKey: {AppKey}）标记为已处理", eventId, appKey ?? "null");
            return false; // 未处理，新事件
        }
    }

    /// <inheritdoc/>
    public bool TryMarkAsProcessing(string eventId, string? appKey = null)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(eventId))
        {
            if (_logger != null && _logger.IsEnabled(LogLevel.Warning))
                _logger?.LogWarning("事件ID为空，跳过去重检查");
            return false;
        }

        var cacheKey = BuildCacheKey(eventId, appKey);

        lock (_lock)
        {
            // 检查是否已存在
            if (_eventCache.TryGetValue(cacheKey, out var entry))
            {
                // 如果已处理，返回 true
                if (entry.Status == DeduplicationStatus.Completed && _logger != null && _logger.IsEnabled(LogLevel.Debug))
                {
                    _logger?.LogDebug("事件 {EventId}（AppKey: {AppKey}）已处理过，跳过", eventId, appKey ?? "null");
                    return true;
                }

                // 如果已在处理中，检查是否超时
                if (entry.Status == DeduplicationStatus.Processing)
                {
                    if (DateTimeOffset.UtcNow - entry.ProcessedAt > _processingTimeout)
                    {
                        // 处理中超时，允许重新处理
                        if (_logger != null && _logger.IsEnabled(LogLevel.Warning))
                            _logger?.LogWarning("事件 {EventId}（AppKey: {AppKey}）处理中超时，允许重新处理", eventId, appKey ?? "null");
                        _eventCache.Remove(cacheKey);
                        // 继续处理
                    }
                    else
                    {
                        if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
                            _logger?.LogDebug("事件 {EventId}（AppKey: {AppKey}）正在处理中，跳过", eventId, appKey ?? "null");
                        return true;
                    }
                }
            }

            // 标记为处理中
            EnsureCapacityLocked();
            _eventCache[cacheKey] = new EventCacheEntry
            {
                ProcessedAt = DateTimeOffset.UtcNow,
                EventId = eventId,
                AppKey = appKey,
                Status = DeduplicationStatus.Processing
            };
            if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
                _logger?.LogDebug("事件 {EventId}（AppKey: {AppKey}）标记为处理中", eventId, appKey ?? "null");
            return false; // 未处理，新事件
        }
    }

    /// <inheritdoc/>
    public void MarkAsCompleted(string eventId, string? appKey = null)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }

        var cacheKey = BuildCacheKey(eventId, appKey);

        lock (_lock)
        {
            if (_eventCache.TryGetValue(cacheKey, out var entry))
            {
                entry.Status = DeduplicationStatus.Completed;
                entry.ProcessedAt = DateTimeOffset.UtcNow; // 更新完成时间

                if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
                    _logger?.LogDebug("事件 {EventId}（AppKey: {AppKey}）标记为已完成", eventId, appKey ?? "null");
            }
        }
    }

    /// <inheritdoc/>
    public void RollbackProcessing(string eventId, string? appKey = null)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }

        var cacheKey = BuildCacheKey(eventId, appKey);

        lock (_lock)
        {
            if (_eventCache.TryGetValue(cacheKey, out var entry))
            {
                if (entry.Status == DeduplicationStatus.Processing)
                {
                    _eventCache.Remove(cacheKey);
                    if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
                        _logger?.LogDebug("事件 {EventId}（AppKey: {AppKey}）处理回滚，允许重新处理", eventId, appKey ?? "null");
                }
                else
                {
                    if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
                        _logger?.LogDebug("事件 {EventId}（AppKey: {AppKey}）状态为 {Status}，无需回滚", eventId, appKey ?? "null", entry.Status);
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool IsProcessed(string eventId, string? appKey = null)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return false;
        }

        var cacheKey = BuildCacheKey(eventId, appKey);

        lock (_lock)
        {
            return _eventCache.TryGetValue(cacheKey, out var entry) && entry.Status == DeduplicationStatus.Completed;
        }
    }

    /// <inheritdoc/>
    public DeduplicationStatus GetStatus(string eventId, string? appKey = null)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return DeduplicationStatus.Pending;
        }

        var cacheKey = BuildCacheKey(eventId, appKey);

        lock (_lock)
        {
            if (_eventCache.TryGetValue(cacheKey, out var entry))
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
    /// 构建缓存键，包含 AppKey 以实现多应用隔离
    /// </summary>
    /// <param name="eventId">事件唯一标识符</param>
    /// <param name="appKey">应用键</param>
    /// <returns>包含 AppKey 前缀的缓存键</returns>
    private static string BuildCacheKey(string eventId, string? appKey)
    {
        return string.IsNullOrEmpty(appKey) ? eventId : $"{appKey}:{eventId}";
    }

    private void ThrowIfDisposed()
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
#endif
    }

    private void EnsureCapacityLocked()
    {
        if (_maxCacheSize <= 0)
            return;

        if (_eventCache.Count >= _maxCacheSize)
        {
            CleanupExpiredEntriesLocked();

            if (_eventCache.Count >= _maxCacheSize)
            {
                if (_logger != null && _logger.IsEnabled(LogLevel.Warning))
                    _logger?.LogWarning("缓存容量已达上限 {MaxCacheSize}，移除最旧条目", _maxCacheSize);

                var oldestKey = _eventCache
                    .OrderBy(x => x.Value.ProcessedAt)
                    .Select(x => x.Key)
                    .FirstOrDefault();

                if (oldestKey != null)
                    _eventCache.Remove(oldestKey);
            }
        }
    }

    private void CleanupExpiredEntriesLocked()
    {
        var removedCount = RemoveExpiredEntriesLocked();

        if (removedCount > 0)
        {
            if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
                _logger?.LogDebug("清理了 {Count} 个过期的事件缓存条目", removedCount);
        }
    }

    private int RemoveExpiredEntriesLocked()
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

        return expiredKeys.Count;
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
            CleanupExpiredEntriesLocked();
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

            if (_logger != null && _logger.IsEnabled(LogLevel.Information))
                _logger?.LogInformation("清空了 {Count} 个事件缓存条目", count);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        DisposeCore();
        GC.SuppressFinalize(this);
        return new ValueTask();
    }

    private void DisposeCore()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cleanupTimer.Dispose();

        lock (_lock)
        {
            _eventCache.Clear();
        }
    }

    /// <summary>
    /// 事件缓存条目
    /// </summary>
    private class EventCacheEntry
    {
        public string EventId { get; set; } = string.Empty;
        public string? AppKey { get; set; }
        public DateTimeOffset ProcessedAt { get; set; }
        public DeduplicationStatus Status { get; set; } = DeduplicationStatus.Pending;
    }
}
