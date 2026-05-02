// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Services;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 带 Redis 降级策略的分布式事件去重服务
/// 当 Redis 连接失败时自动降级到内存去重，并支持指数退避重试
/// </summary>
/// <remarks>
/// 此实现提供高可用性保障：
/// 1. 正常情况使用 Redis 分布式去重
/// 2. Redis 失败时自动降级到内存去重
/// 3. 支持指数退避重试机制
/// 4. 记录降级和恢复事件
/// 5. 支持完整的状态机（Processing -> Completed / Rollback）
/// </remarks>
public class RedisFeishuEventDistributedDeduplicatorWithFallback : IFeishuEventDistributedDeduplicator, IAsyncDisposable
{
    private readonly ILogger<RedisFeishuEventDistributedDeduplicatorWithFallback>? _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly IFeishuEventDistributedDeduplicator _fallbackDeduplicator;
    private readonly IFallbackAlertService? _alertService;
    private readonly TimeSpan _defaultCacheExpiration;
    private readonly TimeSpan _defaultProcessingTimeout;
    private readonly string _keyPrefix;
    private readonly int _maxRetryCount;
    private readonly TimeSpan _initialRetryDelay;
    private readonly TimeSpan _maxRetryDelay;

    private bool _redisAvailable = true;
    private int _consecutiveFailures = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private readonly SemaphoreSlim _retrySemaphore = new(1, 1);
    private bool _disposed;

    private const string StatusField = "status";
    private const string TimestampField = "timestamp";
    private const string ProcessingStatus = "processing";
    private const string CompletedStatus = "completed";

    /// <summary>
    /// 获取当前是否使用 Redis
    /// </summary>
    public bool IsUsingRedis => _redisAvailable;

    /// <summary>
    /// 获取连续失败次数
    /// </summary>
    public int ConsecutiveFailures => _consecutiveFailures;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="redis">Redis 连接多路复用器</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cacheExpiration">默认缓存过期时间</param>
    /// <param name="processingTimeout">默认处理中超时时间</param>
    /// <param name="keyPrefix">Redis 键前缀，默认为 "feishu:event:"</param>
    /// <param name="maxRetryCount">最大重试次数，默认为 3</param>
    /// <param name="initialRetryDelay">初始重试延迟，默认为 1 秒</param>
    /// <param name="maxRetryDelay">最大重试延迟，默认为 30 秒</param>
    /// <param name="alertService">降级告警服务（可选）</param>
    public RedisFeishuEventDistributedDeduplicatorWithFallback(
        IConnectionMultiplexer redis,
        ILogger<RedisFeishuEventDistributedDeduplicatorWithFallback>? logger = null,
        TimeSpan? cacheExpiration = null,
        TimeSpan? processingTimeout = null,
        string? keyPrefix = null,
        int maxRetryCount = 3,
        TimeSpan? initialRetryDelay = null,
        TimeSpan? maxRetryDelay = null,
        IFallbackAlertService? alertService = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger;
        _database = _redis.GetDatabase();
        _defaultCacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
        _defaultProcessingTimeout = processingTimeout ?? TimeSpan.FromMinutes(10);
        _keyPrefix = keyPrefix ?? "feishu:event:";
        _maxRetryCount = maxRetryCount;
        _initialRetryDelay = initialRetryDelay ?? TimeSpan.FromSeconds(1);
        _maxRetryDelay = maxRetryDelay ?? TimeSpan.FromSeconds(30);
        _alertService = alertService;

#pragma warning disable CS0618
        _fallbackDeduplicator = new FeishuEventDistributedDeduplicator(
            logger as ILogger<FeishuEventDistributedDeduplicator>,
            _defaultCacheExpiration,
            TimeSpan.FromMinutes(5),
            _defaultProcessingTimeout);
#pragma warning restore CS0618

        _logger?.LogInformation("飞书 Redis 分布式事件去重服务（带降级）初始化完成，缓存过期时间: {Expiration}, 处理超时: {ProcessingTimeout}, 键前缀: {KeyPrefix}, 最大重试: {MaxRetry}",
            _defaultCacheExpiration, _defaultProcessingTimeout, _keyPrefix, _maxRetryCount);
    }

    /// <summary>
    /// 使用统一配置构造
    /// </summary>
    /// <param name="redis">Redis 连接多路复用器</param>
    /// <param name="options">去重配置选项</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="alertService">降级告警服务（可选）</param>
    public RedisFeishuEventDistributedDeduplicatorWithFallback(
        IConnectionMultiplexer redis,
        DeduplicationOptions options,
        ILogger<RedisFeishuEventDistributedDeduplicatorWithFallback>? logger = null,
        IFallbackAlertService? alertService = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _logger = logger;
        _database = _redis.GetDatabase();
        _defaultCacheExpiration = options.CacheExpiration;
        _defaultProcessingTimeout = options.ProcessingTimeout;
        _keyPrefix = options.KeyPrefix;
        _maxRetryCount = options.MaxRetryCount;
        _initialRetryDelay = options.InitialRetryDelay;
        _maxRetryDelay = options.MaxRetryDelay;
        _alertService = alertService;

#pragma warning disable CS0618
        _fallbackDeduplicator = new FeishuEventDistributedDeduplicator(
            options,
            logger as ILogger<FeishuEventDistributedDeduplicator>);
#pragma warning restore CS0618

        _logger?.LogInformation("飞书 Redis 分布式事件去重服务（带降级）初始化完成（使用统一配置），缓存过期时间: {Expiration}, 处理超时: {ProcessingTimeout}, 键前缀: {KeyPrefix}, 最大重试: {MaxRetry}",
            _defaultCacheExpiration, _defaultProcessingTimeout, _keyPrefix, _maxRetryCount);
    }

    /// <inheritdoc />
    public async Task<DeduplicationResult> TryMarkAsProcessingAsync(string eventId, string? appKey = null, TimeSpan? ttl = null, TimeSpan? processingTimeout = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            _logger?.LogWarning("事件ID为空，跳过去重检查");
            return DeduplicationResult.Success(eventId);
        }

        if (!_redisAvailable)
        {
            return await UseFallbackForProcessingAsync(eventId, appKey, ttl, processingTimeout, cancellationToken, "Redis 不可用");
        }

        try
        {
            return await TryMarkProcessingWithRetryAsync(eventId, appKey, ttl, processingTimeout, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return await HandleRedisFailureForProcessingAsync(eventId, appKey, ttl, processingTimeout, cancellationToken, ex);
        }
    }

    /// <inheritdoc />
    public async Task MarkAsCompletedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }

        if (!_redisAvailable)
        {
            await _fallbackDeduplicator.MarkAsCompletedAsync(eventId, appKey, cancellationToken);
            return;
        }

        try
        {
            var redisKey = GetRedisKey(eventId, appKey);

            for (int attempt = 0; attempt < _maxRetryCount; attempt++)
            {
                try
                {
                    await _database.HashSetAsync(redisKey, new[]
                    {
                        new HashEntry(StatusField, CompletedStatus),
                        new HashEntry(TimestampField, DateTime.UtcNow.ToString("O"))
                    });
                    ResetFailureCount();
                    _logger?.LogDebug("事件 {EventId} 标记为已完成 (AppKey: {AppKey})", eventId, appKey ?? "default");
                    return;
                }
                catch (RedisConnectionException ex)
                {
                    _logger?.LogWarning(ex, "Redis 连接失败 (尝试 {Attempt}/{MaxRetry})", attempt + 1, _maxRetryCount);
                    if (attempt == _maxRetryCount - 1)
                        throw;
                    await Task.Delay(CalculateRetryDelay(attempt), cancellationToken);
                }
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger?.LogError(ex, "Redis 失败，使用内存降级标记完成");
            await _fallbackDeduplicator.MarkAsCompletedAsync(eventId, appKey, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RollbackProcessingAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }

        if (!_redisAvailable)
        {
            await _fallbackDeduplicator.RollbackProcessingAsync(eventId, appKey, cancellationToken);
            return;
        }

        try
        {
            var redisKey = GetRedisKey(eventId, appKey);

            for (int attempt = 0; attempt < _maxRetryCount; attempt++)
            {
                try
                {
                    var status = await _database.HashGetAsync(redisKey, StatusField);
                    if (status == ProcessingStatus)
                    {
                        await _database.KeyDeleteAsync(redisKey);
                        _logger?.LogDebug("事件 {EventId} 处理回滚，允许重新处理 (AppKey: {AppKey})", eventId, appKey ?? "default");
                    }
                    ResetFailureCount();
                    return;
                }
                catch (RedisConnectionException ex)
                {
                    _logger?.LogWarning(ex, "Redis 连接失败 (尝试 {Attempt}/{MaxRetry})", attempt + 1, _maxRetryCount);
                    if (attempt == _maxRetryCount - 1)
                        throw;
                    await Task.Delay(CalculateRetryDelay(attempt), cancellationToken);
                }
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger?.LogError(ex, "Redis 失败，使用内存降级回滚");
            await _fallbackDeduplicator.RollbackProcessingAsync(eventId, appKey, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsProcessedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return false;
        }

        if (!_redisAvailable)
        {
            return await _fallbackDeduplicator.IsProcessedAsync(eventId, appKey, cancellationToken);
        }

        try
        {
            return await CheckWithRetryAsync(eventId, appKey, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return await HandleRedisFailureForCheckAsync(eventId, appKey, cancellationToken, ex);
        }
    }

    /// <inheritdoc />
    public async Task<DeduplicationStatus> GetStatusAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return DeduplicationStatus.Pending;
        }

        if (!_redisAvailable)
        {
            return await _fallbackDeduplicator.GetStatusAsync(eventId, appKey, cancellationToken);
        }

        try
        {
            var redisKey = GetRedisKey(eventId, appKey);
            var entries = await _database.HashGetAllAsync(redisKey);

            if (entries.Length == 0)
            {
                return DeduplicationStatus.Pending;
            }

            var statusEntry = entries.FirstOrDefault(x => x.Name == StatusField);
            var timestampEntry = entries.FirstOrDefault(x => x.Name == TimestampField);

            var status = statusEntry.Value.ToString();

            if (status == CompletedStatus)
            {
                return DeduplicationStatus.Completed;
            }

            if (status == ProcessingStatus)
            {
                var timestampStr = timestampEntry.Value.ToString();
                if (DateTime.TryParse(timestampStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var timestamp))
                {
                    var elapsed = DateTime.UtcNow - timestamp;
                    if (elapsed > _defaultProcessingTimeout)
                    {
                        return DeduplicationStatus.Pending;
                    }
                }
                return DeduplicationStatus.Processing;
            }

            return DeduplicationStatus.Pending;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger?.LogError(ex, "Redis 失败，使用内存降级获取状态");
            return await _fallbackDeduplicator.GetStatusAsync(eventId, appKey, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _fallbackDeduplicator.CleanupExpiredAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "清理过期条目时发生错误");
            return 0;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await _fallbackDeduplicator.DisposeAsync();
        _retrySemaphore.Dispose();
    }

    private async Task<DeduplicationResult> TryMarkProcessingWithRetryAsync(string eventId, string? appKey, TimeSpan? ttl, TimeSpan? processingTimeout, CancellationToken cancellationToken)
    {
        var actualTtl = ttl ?? _defaultCacheExpiration;
        var actualProcessingTimeout = processingTimeout ?? _defaultProcessingTimeout;
        var redisKey = GetRedisKey(eventId, appKey);

        for (int attempt = 0; attempt < _maxRetryCount; attempt++)
        {
            try
            {
                var existing = await _database.HashGetAllAsync(redisKey);
                if (existing.Length > 0)
                {
                    var statusEntry = existing.FirstOrDefault(x => x.Name == StatusField);
                    var timestampEntry = existing.FirstOrDefault(x => x.Name == TimestampField);

                    var status = statusEntry.Value.ToString();
                    var timestampStr = timestampEntry.Value.ToString();

                    if (status == CompletedStatus)
                    {
                        _logger?.LogDebug("事件 {EventId} 已完成，跳过 (AppKey: {AppKey})", eventId, appKey ?? "default");
                        return DeduplicationResult.Duplicate(eventId, false, DeduplicationStatus.Completed);
                    }

                    if (status == ProcessingStatus)
                    {
                        if (DateTime.TryParse(timestampStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var timestamp))
                        {
                            var elapsed = DateTime.UtcNow - timestamp;
                            if (elapsed > actualProcessingTimeout)
                            {
                                _logger?.LogWarning("事件 {EventId} 处理中超时，允许重新处理 (AppKey: {AppKey})", eventId, appKey ?? "default");

                                await _database.HashSetAsync(redisKey, new[]
                                {
                                    new HashEntry(StatusField, ProcessingStatus),
                                    new HashEntry(TimestampField, DateTime.UtcNow.ToString("O"))
                                });
                                await _database.KeyExpireAsync(redisKey, actualTtl);

                                ResetFailureCount();
                                return DeduplicationResult.TimeoutRecoverable(eventId);
                            }
                        }

                        _logger?.LogDebug("事件 {EventId} 正在处理中，跳过 (AppKey: {AppKey})", eventId, appKey ?? "default");
                        return DeduplicationResult.Duplicate(eventId, true, DeduplicationStatus.Processing);
                    }
                }

                await _database.HashSetAsync(redisKey, new[]
                {
                    new HashEntry(StatusField, ProcessingStatus),
                    new HashEntry(TimestampField, DateTime.UtcNow.ToString("O"))
                });
                await _database.KeyExpireAsync(redisKey, actualTtl);

                ResetFailureCount();
                _logger?.LogDebug("事件 {EventId} 标记为处理中，TTL: {Ttl} (AppKey: {AppKey})", eventId, actualTtl, appKey ?? "default");
                return DeduplicationResult.Success(eventId);
            }
            catch (RedisConnectionException ex)
            {
                _logger?.LogWarning(ex, "Redis 连接失败 (尝试 {Attempt}/{MaxRetry})", attempt + 1, _maxRetryCount);
                if (attempt == _maxRetryCount - 1)
                    throw;
                await Task.Delay(CalculateRetryDelay(attempt), cancellationToken);
            }
        }

        return DeduplicationResult.Success(eventId);
    }

    private async Task<bool> CheckWithRetryAsync(string eventId, string? appKey, CancellationToken cancellationToken)
    {
        var redisKey = GetRedisKey(eventId, appKey);

        for (int attempt = 0; attempt < _maxRetryCount; attempt++)
        {
            try
            {
                var status = await _database.HashGetAsync(redisKey, StatusField);
                ResetFailureCount();
                return status == CompletedStatus;
            }
            catch (RedisConnectionException ex)
            {
                _logger?.LogWarning(ex, "Redis 连接失败 (尝试 {Attempt}/{MaxRetry})", attempt + 1, _maxRetryCount);
                if (attempt == _maxRetryCount - 1)
                    throw;
                await Task.Delay(CalculateRetryDelay(attempt), cancellationToken);
            }
        }

        return false;
    }

    private async Task<DeduplicationResult> HandleRedisFailureForProcessingAsync(string eventId, string? appKey, TimeSpan? ttl, TimeSpan? processingTimeout, CancellationToken cancellationToken, Exception ex)
    {
        await _retrySemaphore.WaitAsync(cancellationToken);
        try
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTime.UtcNow;

            _logger?.LogError(ex, "Redis 失败，连续失败次数: {FailCount}, 降级到内存去重", _consecutiveFailures);

            if (_consecutiveFailures >= 3)
            {
                _redisAvailable = false;
                _logger?.LogWarning("连续失败 {FailCount} 次，标记 Redis 为不可用，使用内存去重", _consecutiveFailures);
            }

            return await UseFallbackForProcessingAsync(eventId, appKey, ttl, processingTimeout, cancellationToken, "Redis 失败");
        }
        finally
        {
            _retrySemaphore.Release();
        }
    }

    private async Task<bool> HandleRedisFailureForCheckAsync(string eventId, string? appKey, CancellationToken cancellationToken, Exception ex)
    {
        await _retrySemaphore.WaitAsync(cancellationToken);
        try
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTime.UtcNow;

            _logger?.LogError(ex, "Redis 失败（检查操作），连续失败次数: {FailCount}, 使用内存去重", _consecutiveFailures);

            if (_consecutiveFailures >= 3 && _redisAvailable)
            {
                _redisAvailable = false;
                _logger?.LogWarning("连续失败 {FailCount} 次，标记 Redis 为不可用", _consecutiveFailures);

                if (_alertService != null)
                {
                    await _alertService.RaiseAlertAsync(
                        FallbackAlertType.RedisFallbackActivated,
                        $"Redis 连续失败 {_consecutiveFailures} 次，已降级到内存去重",
                        ex,
                        new Dictionary<string, object?>
                        {
                            ["ConsecutiveFailures"] = _consecutiveFailures,
                            ["IsFallbackActive"] = true,
                            ["EventId"] = eventId
                        });
                }
            }

            return await _fallbackDeduplicator.IsProcessedAsync(eventId, appKey, cancellationToken);
        }
        finally
        {
            _retrySemaphore.Release();
        }
    }

    private async Task<DeduplicationResult> UseFallbackForProcessingAsync(string eventId, string? appKey, TimeSpan? ttl, TimeSpan? processingTimeout, CancellationToken cancellationToken, string reason)
    {
        _logger?.LogDebug("使用内存降级去重器，原因: {Reason}, 事件ID: {EventId}, AppKey: {AppKey}", reason, eventId, appKey ?? "default");
        return await _fallbackDeduplicator.TryMarkAsProcessingAsync(eventId, appKey, ttl, processingTimeout, cancellationToken);
    }

    private TimeSpan CalculateRetryDelay(int attempt)
    {
        var delay = TimeSpan.FromMilliseconds(
            Math.Min(
                _initialRetryDelay.TotalMilliseconds * Math.Pow(2, attempt),
                _maxRetryDelay.TotalMilliseconds));

        _logger?.LogDebug("重试延迟: {Delay}ms (尝试 {Attempt})", delay.TotalMilliseconds, attempt + 1);
        return delay;
    }

    private void ResetFailureCount()
    {
        if (_consecutiveFailures > 0)
        {
            var wasUnavailable = !_redisAvailable;
            _consecutiveFailures = 0;
            if (wasUnavailable)
            {
                _redisAvailable = true;
                _logger?.LogInformation("Redis 连接恢复，重新启用 Redis 去重");

                if (_alertService != null)
                {
                    _ = _alertService.RaiseAlertAsync(
                        FallbackAlertType.RedisRecovered,
                        "Redis 连接已恢复，重新启用 Redis 分布式去重",
                        null,
                        new Dictionary<string, object?>
                        {
                            ["WasFallbackActive"] = true,
                            ["RecoveredAt"] = DateTime.UtcNow
                        });
                }
            }
        }
    }

    private string GetRedisKey(string eventId, string? appKey = null)
    {
        if (!string.IsNullOrEmpty(appKey))
        {
            return $"{_keyPrefix}{appKey}:{eventId}";
        }
        return $"{_keyPrefix}{eventId}";
    }
}
