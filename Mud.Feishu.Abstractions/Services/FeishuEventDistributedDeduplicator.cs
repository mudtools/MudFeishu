// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 基于内存的事件去重服务实现（模拟分布式接口）
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ 重要提示：此类仅适用于单机部署或开发测试环境，不适合真正的分布式场景。
/// </para>
/// <para>
/// 在分布式环境下，多个实例之间的内存不共享，无法实现真正的跨实例去重。
/// 对于生产环境的分布式部署，请使用以下方案：
/// </para>
/// <list type="bullet">
///   <item><description>使用 <see cref="Mud.Feishu.Redis.Services.RedisFeishuEventDistributedDeduplicator"/>（推荐）</description></item>
///   <item><description>实现自定义的 <see cref="IFeishuEventDistributedDeduplicator"/> 接口，使用外部存储（如 Redis、数据库等）</description></item>
/// </list>
/// <para>
/// 此类实现了 <see cref="IFeishuEventDistributedDeduplicator"/> 接口，主要是为了：
/// </para>
/// <list type="number">
///   <item><description>提供开发测试环境下的快速实现</description></item>
///   <item><description>作为接口的默认实现，方便依赖注入</description></item>
///   <item><description>在单机部署场景下提供完整的去重功能</description></item>
/// </list>
/// </remarks>
/// <example>
/// 单机场景使用示例：
/// <code>
/// services.AddSingleton&lt;IFeishuEventDistributedDeduplicator, FeishuEventDistributedDeduplicator&gt;();
/// </code>
/// 
/// 分布式场景使用示例（需要安装 Mud.Feishu.Redis 包）：
/// <code>
/// services.AddSingleton&lt;IFeishuEventDistributedDeduplicator, RedisFeishuEventDistributedDeduplicator&gt;();
/// </code>
/// </example>
public sealed class FeishuEventDistributedDeduplicator : IFeishuEventDistributedDeduplicator
{
    private readonly ILogger<FeishuEventDistributedDeduplicator>? _logger;
    private readonly Dictionary<string, DistributedCacheEntry> _cache;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cacheExpiration;
    private readonly TimeSpan _processingTimeout;
    private readonly TimeSpan _cleanupInterval;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="cacheExpiration">缓存过期时间，默认 24 小时</param>
    /// <param name="cleanupInterval">清理间隔时间，默认 5 分钟</param>
    /// <param name="processingTimeout">处理中超时时间，默认 10 分钟</param>
    public FeishuEventDistributedDeduplicator(
        ILogger<FeishuEventDistributedDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        TimeSpan? cleanupInterval = null,
        TimeSpan? processingTimeout = null)
    {
        _logger = logger;
        _cache = new Dictionary<string, DistributedCacheEntry>();
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5);
        _processingTimeout = processingTimeout ?? TimeSpan.FromMinutes(10);

        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);

        _logger?.LogInformation("飞书分布式事件去重服务初始化完成，缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}, 处理超时: {ProcessingTimeout}",
            _cacheExpiration, _cleanupInterval, _processingTimeout);
    }

    /// <summary>
    /// 使用统一配置构造
    /// </summary>
    /// <param name="options">去重配置选项</param>
    /// <param name="logger">日志记录器</param>
    public FeishuEventDistributedDeduplicator(DeduplicationOptions options, ILogger<FeishuEventDistributedDeduplicator>? logger = null)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _logger = logger;
        _cache = new Dictionary<string, DistributedCacheEntry>();
        _cacheExpiration = options.CacheExpiration;
        _cleanupInterval = options.CleanupInterval;
        _processingTimeout = options.ProcessingTimeout;

        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);

        _logger?.LogInformation("飞书分布式事件去重服务初始化完成（使用统一配置），缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}, 处理超时: {ProcessingTimeout}",
            _cacheExpiration, _cleanupInterval, _processingTimeout);
    }

    /// <inheritdoc />
    public Task<DeduplicationResult> TryMarkAsProcessingAsync(string eventId, string? appKey = null, TimeSpan? ttl = null, TimeSpan? processingTimeout = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            _logger?.LogWarning("事件ID为空，跳过去重检查");
            return Task.FromResult(DeduplicationResult.Success(eventId));
        }

        var cacheKey = GetCacheKey(eventId, appKey);
        var actualProcessingTimeout = processingTimeout ?? _processingTimeout;

        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                if (entry.Status == DeduplicationStatus.Completed)
                {
                    _logger?.LogDebug("事件 {EventId} (AppKey: {AppKey}) 已完成，跳过", eventId, appKey ?? "default");
                    return Task.FromResult(DeduplicationResult.Duplicate(eventId, false, DeduplicationStatus.Completed));
                }

                if (entry.Status == DeduplicationStatus.Processing)
                {
                    var elapsed = DateTimeOffset.UtcNow - entry.Timestamp;
                    if (elapsed > actualProcessingTimeout)
                    {
                        _logger?.LogWarning("事件 {EventId} (AppKey: {AppKey}) 处理中超时 ({Elapsed} > {Timeout})，允许重新处理",
                            eventId, appKey ?? "default", elapsed, actualProcessingTimeout);

                        entry.Status = DeduplicationStatus.Processing;
                        entry.Timestamp = DateTimeOffset.UtcNow;

                        return Task.FromResult(DeduplicationResult.TimeoutRecoverable(eventId));
                    }

                    _logger?.LogDebug("事件 {EventId} (AppKey: {AppKey}) 正在处理中，跳过", eventId, appKey ?? "default");
                    return Task.FromResult(DeduplicationResult.Duplicate(eventId, true, DeduplicationStatus.Processing));
                }
            }

            _cache[cacheKey] = new DistributedCacheEntry
            {
                EventId = eventId,
                AppKey = appKey,
                Status = DeduplicationStatus.Processing,
                Timestamp = DateTimeOffset.UtcNow
            };

            _logger?.LogDebug("事件 {EventId} (AppKey: {AppKey}) 标记为处理中", eventId, appKey ?? "default");
            return Task.FromResult(DeduplicationResult.Success(eventId));
        }
    }

    /// <inheritdoc />
    public Task MarkAsCompletedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return Task.CompletedTask;
        }

        var cacheKey = GetCacheKey(eventId, appKey);

        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                entry.Status = DeduplicationStatus.Completed;
                entry.Timestamp = DateTimeOffset.UtcNow;
                _logger?.LogDebug("事件 {EventId} (AppKey: {AppKey}) 标记为已完成", eventId, appKey ?? "default");
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RollbackProcessingAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return Task.CompletedTask;
        }

        var cacheKey = GetCacheKey(eventId, appKey);

        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                if (entry.Status == DeduplicationStatus.Processing)
                {
                    _cache.Remove(cacheKey);
                    _logger?.LogDebug("事件 {EventId} (AppKey: {AppKey}) 处理回滚，允许重新处理", eventId, appKey ?? "default");
                }
                else
                {
                    _logger?.LogDebug("事件 {EventId} (AppKey: {AppKey}) 状态为 {Status}，无需回滚", eventId, appKey ?? "default", entry.Status);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> IsProcessedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return Task.FromResult(false);
        }

        var cacheKey = GetCacheKey(eventId, appKey);

        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                return Task.FromResult(entry.Status == DeduplicationStatus.Completed);
            }
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<DeduplicationStatus> GetStatusAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return Task.FromResult(DeduplicationStatus.Pending);
        }

        var cacheKey = GetCacheKey(eventId, appKey);

        lock (_lock)
        {
            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                if (entry.Status == DeduplicationStatus.Processing)
                {
                    var elapsed = DateTimeOffset.UtcNow - entry.Timestamp;
                    if (elapsed > _processingTimeout)
                    {
                        return Task.FromResult(DeduplicationStatus.Pending);
                    }
                }
                return Task.FromResult(entry.Status);
            }
            return Task.FromResult(DeduplicationStatus.Pending);
        }
    }

    /// <inheritdoc />
    public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var expiredKeys = _cache
                .Where(kvp => (now - kvp.Value.Timestamp) > _cacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }

            if (expiredKeys.Count > 0)
            {
                _logger?.LogDebug("清理了 {Count} 个过期缓存条目", expiredKeys.Count);
            }

            return Task.FromResult(expiredKeys.Count);
        }
    }

    private void CleanupExpiredEntries(object? state)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var expiredKeys = _cache
                .Where(kvp => (now - kvp.Value.Timestamp) > _cacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
            }

            if (expiredKeys.Count > 0)
            {
                _logger?.LogDebug("清理了 {Count} 个过期缓存条目", expiredKeys.Count);
            }
        }
    }

    private static string GetCacheKey(string eventId, string? appKey)
    {
        return string.IsNullOrEmpty(appKey) ? eventId : $"{appKey}:{eventId}";
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return new ValueTask();

        _disposed = true;
        _cleanupTimer.Dispose();

        lock (_lock)
        {
            _cache.Clear();
        }

        return new ValueTask();
    }

    private class DistributedCacheEntry
    {
        public string EventId { get; set; } = string.Empty;
        public string? AppKey { get; set; }
        public DeduplicationStatus Status { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
