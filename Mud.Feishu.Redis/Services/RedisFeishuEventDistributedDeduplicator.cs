// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 基于 Redis 的分布式事件去重服务实现
/// 使用 Redis Hash 存储完整状态，支持状态机和异常恢复
/// </summary>
/// <remarks>
/// 此实现适用于分布式部署场景，使用 Redis 作为共享存储。
/// 通过 Redis Hash 存储事件状态（processing/completed）和时间戳，支持：
/// 1. 处理中状态追踪
/// 2. 处理中超时恢复
/// 3. 异常后回滚
/// </remarks>
public class RedisFeishuEventDistributedDeduplicator : IFeishuEventDeduplicator, IAsyncDisposable
{
    private readonly ILogger<RedisFeishuEventDistributedDeduplicator>? _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly TimeSpan _defaultCacheExpiration;
    private readonly TimeSpan _defaultProcessingTimeout;
    private readonly string _keyPrefix;
    private bool _disposed;

    private const string StatusField = "status";
    private const string TimestampField = "timestamp";
    private const string ProcessingStatus = "processing";
    private const string CompletedStatus = "completed";

    private const string TryMarkAsProcessingLuaScript = @"
        local key = KEYS[1]
        local processingTimeout = ARGV[1]
        local currentTimestamp = ARGV[2]
        local ttlSeconds = ARGV[3]

        local existing = redis.call('HGETALL', key)
        if #existing > 0 then
            local status = nil
            local timestamp = nil
            for i = 1, #existing, 2 do
                if existing[i] == 'status' then
                    status = existing[i+1]
                elseif existing[i] == 'timestamp' then
                    timestamp = existing[i+1]
                end
            end

            if status == 'completed' then
                return 1
            end

            if status == 'processing' then
                if timestamp then
                    local elapsed = tonumber(currentTimestamp) - tonumber(timestamp)
                    if elapsed > tonumber(processingTimeout) then
                        redis.call('HMSET', key, 'status', 'processing', 'timestamp', currentTimestamp)
                        redis.call('EXPIRE', key, tonumber(ttlSeconds))
                        return 3
                    end
                end
                return 2
            end
        end

        redis.call('HMSET', key, 'status', 'processing', 'timestamp', currentTimestamp)
        redis.call('EXPIRE', key, tonumber(ttlSeconds))
        return 0
        ";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="redis">Redis 连接多路复用器</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cacheExpiration">默认缓存过期时间</param>
    /// <param name="processingTimeout">默认处理中超时时间</param>
    /// <param name="keyPrefix">Redis 键前缀，默认为 "feishu:event:"</param>
    public RedisFeishuEventDistributedDeduplicator(
        IConnectionMultiplexer redis,
        ILogger<RedisFeishuEventDistributedDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        TimeSpan? processingTimeout = null,
        string? keyPrefix = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger;
        _database = _redis.GetDatabase();
        _defaultCacheExpiration = cacheExpiration ?? TimeSpan.FromMilliseconds(Mud.Feishu.Abstractions.Consts.DefaultCacheExpirationMs);
        _defaultProcessingTimeout = processingTimeout ?? TimeSpan.FromMilliseconds(Mud.Feishu.Abstractions.Consts.DefaultProcessingTimeoutMs);
        _keyPrefix = keyPrefix ?? Mud.Feishu.Abstractions.Consts.DefaultEventKeyPrefix;

        _logger?.LogInformation("飞书 Redis 分布式事件去重服务初始化完成，缓存过期时间: {Expiration}, 处理超时: {ProcessingTimeout}, 键前缀: {KeyPrefix}",
            _defaultCacheExpiration, _defaultProcessingTimeout, _keyPrefix);
    }

    /// <summary>
    /// 使用统一配置构造
    /// </summary>
    /// <param name="redis">Redis 连接多路复用器</param>
    /// <param name="options">去重配置选项</param>
    /// <param name="logger">日志记录器（可选）</param>
    public RedisFeishuEventDistributedDeduplicator(
        IConnectionMultiplexer redis,
        DeduplicationOptions options,
        ILogger<RedisFeishuEventDistributedDeduplicator>? logger = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _logger = logger;
        _database = _redis.GetDatabase();
        _defaultCacheExpiration = options.CacheExpiration;
        _defaultProcessingTimeout = options.ProcessingTimeout;
        _keyPrefix = options.KeyPrefix;

        _logger?.LogInformation("飞书 Redis 分布式事件去重服务初始化完成（使用统一配置），缓存过期时间: {Expiration}, 处理超时: {ProcessingTimeout}, 键前缀: {KeyPrefix}",
            _defaultCacheExpiration, _defaultProcessingTimeout, _keyPrefix);
    }

    /// <inheritdoc />
    public async Task<DeduplicationResult> TryMarkAsProcessingAsync(string eventId, string? appKey = null, TimeSpan? ttl = null, TimeSpan? processingTimeout = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            _logger?.LogWarning("事件ID为空，跳过去重检查");
            return DeduplicationResult.Success(eventId);
        }

        try
        {
            var actualTtl = ttl ?? _defaultCacheExpiration;
            var actualProcessingTimeout = processingTimeout ?? _defaultProcessingTimeout;
            var redisKey = GetRedisKey(eventId, appKey);
            var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var processingTimeoutSeconds = (long)actualProcessingTimeout.TotalSeconds;
            var ttlSeconds = (long)actualTtl.TotalSeconds;

            var result = (long)await _database.ScriptEvaluateAsync(
                TryMarkAsProcessingLuaScript,
                new RedisKey[] { redisKey },
                new RedisValue[] { processingTimeoutSeconds, currentTimestamp, ttlSeconds }
            );

            return result switch
            {
                0 => LogAndReturnSuccess(eventId, appKey, actualTtl),
                1 => LogAndReturnDuplicate(eventId, appKey, false, DeduplicationStatus.Completed),
                2 => LogAndReturnDuplicate(eventId, appKey, true, DeduplicationStatus.Processing),
                3 => LogAndReturnTimeoutRecoverable(eventId, appKey, actualProcessingTimeout),
                _ => LogAndReturnSuccess(eventId, appKey, actualTtl)
            };
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "Redis 连接异常，事件 {EventId} 去重失败", eventId);
            throw new InvalidOperationException("Redis 连接失败，无法完成去重", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "Redis 超时，事件 {EventId} 去重失败", eventId);
            throw new InvalidOperationException("Redis 操作超时", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "Redis 操作异常，事件 {EventId} 去重失败", eventId);
            throw new InvalidOperationException("Redis 操作失败", ex);
        }
    }

    private DeduplicationResult LogAndReturnSuccess(string eventId, string? appKey, TimeSpan ttl)
    {
        _logger?.LogDebug("事件 {EventId} 标记为处理中，TTL: {Ttl} (AppKey: {AppKey})", eventId, ttl, appKey ?? "default");
        return DeduplicationResult.Success(eventId);
    }

    private DeduplicationResult LogAndReturnDuplicate(string eventId, string? appKey, bool isProcessing, DeduplicationStatus status)
    {
        if (status == DeduplicationStatus.Completed)
            _logger?.LogDebug("事件 {EventId} 已完成，跳过 (AppKey: {AppKey})", eventId, appKey ?? "default");
        else
            _logger?.LogDebug("事件 {EventId} 正在处理中，跳过 (AppKey: {AppKey})", eventId, appKey ?? "default");
        return DeduplicationResult.Duplicate(eventId, isProcessing, status);
    }

    private DeduplicationResult LogAndReturnTimeoutRecoverable(string eventId, string? appKey, TimeSpan processingTimeout)
    {
        _logger?.LogWarning("事件 {EventId} 处理中超时，允许重新处理 (AppKey: {AppKey})", eventId, appKey ?? "default");
        return DeduplicationResult.TimeoutRecoverable(eventId);
    }

    /// <inheritdoc />
    public async Task MarkAsCompletedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }

        try
        {
            var redisKey = GetRedisKey(eventId, appKey);

            await _database.HashSetAsync(redisKey, new[]
            {
                new HashEntry(StatusField, CompletedStatus),
                // P-2 修复：统一使用 Unix 秒时间戳，与 TryMarkAsProcessingAsync 的 Lua 脚本保持一致。
                new HashEntry(TimestampField, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            });

            _logger?.LogDebug("事件 {EventId} 标记为已完成 (AppKey: {AppKey})", eventId, appKey ?? "default");
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "Redis 连接异常，标记事件 {EventId} 为已完成失败", eventId);
            throw new InvalidOperationException("Redis 连接失败，无法标记事件为已完成", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "Redis 超时，标记事件 {EventId} 为已完成失败", eventId);
            throw new InvalidOperationException("Redis 操作超时，无法标记事件为已完成", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "标记事件 {EventId} 为已完成时发生 Redis 错误", eventId);
            throw new InvalidOperationException("Redis 操作失败，无法标记事件为已完成", ex);
        }
    }

    /// <inheritdoc />
    public async Task RollbackProcessingAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }

        try
        {
            var redisKey = GetRedisKey(eventId, appKey);

            var status = await _database.HashGetAsync(redisKey, StatusField);
            if (status == ProcessingStatus)
            {
                await _database.KeyDeleteAsync(redisKey);
                _logger?.LogDebug("事件 {EventId} 处理回滚，允许重新处理 (AppKey: {AppKey})", eventId, appKey ?? "default");
            }
            else
            {
                _logger?.LogDebug("事件 {EventId} 状态为 {Status}，无需回滚 (AppKey: {AppKey})", eventId, status, appKey ?? "default");
            }
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "回滚事件 {EventId} 处理状态时发生错误", eventId);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsProcessedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return false;
        }

        try
        {
            var redisKey = GetRedisKey(eventId, appKey);
            var status = await _database.HashGetAsync(redisKey, StatusField);

            var isProcessed = status == CompletedStatus;
            _logger?.LogDebug("事件 {EventId} 处理状态: {Status} (AppKey: {AppKey})", eventId, isProcessed ? "已处理" : "未处理", appKey ?? "default");
            return isProcessed;
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "Redis 连接异常，检查事件 {EventId} 处理状态失败", eventId);
            throw new InvalidOperationException("Redis 连接失败，无法检查处理状态", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "Redis 超时，检查事件 {EventId} 处理状态失败", eventId);
            throw new InvalidOperationException("Redis 操作超时", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "Redis 操作异常，检查事件 {EventId} 处理状态失败", eventId);
            throw new InvalidOperationException("Redis 操作失败", ex);
        }
    }

    /// <inheritdoc />
    public async Task<DeduplicationStatus> GetStatusAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return DeduplicationStatus.Pending;
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
                // P-2 修复：兼容两种时间戳格式。
                // TryMarkAsProcessingAsync 的 Lua 脚本写入 Unix 秒（数字字符串）；
                // 历史 MarkAsCompletedAsync 写入 ISO 8601 字符串（已改为 Unix 秒，但需兼容存量数据）。
                var timestamp = TryParseTimestamp(timestampStr);
                if (timestamp.HasValue)
                {
                    var elapsed = DateTimeOffset.UtcNow - timestamp.Value;
                    if (elapsed > _defaultProcessingTimeout)
                    {
                        return DeduplicationStatus.Pending;
                    }
                }
                return DeduplicationStatus.Processing;
            }

            return DeduplicationStatus.Pending;
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "获取事件 {EventId} 状态时发生错误", eventId);
            return DeduplicationStatus.Pending;
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Redis 自动清理过期键，无需手动清理");
        await Task.CompletedTask;
        return 0;
    }

    /// <summary>
    /// P-2 修复：尝试解析时间戳，兼容 Unix 秒（数字字符串）和 ISO 8601 字符串两种格式。
    /// </summary>
    /// <param name="timestampStr">时间戳字符串。</param>
    /// <returns>解析成功返回 <see cref="DateTimeOffset"/>，否则返回 null。</returns>
    private static DateTimeOffset? TryParseTimestamp(string timestampStr)
    {
        if (string.IsNullOrEmpty(timestampStr))
        {
            return null;
        }

        // 优先尝试 Unix 秒（与 TryMarkAsProcessingAsync 的 Lua 脚本一致）
        if (long.TryParse(timestampStr, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var unixSeconds))
        {
            // 合理范围校验：Unix 秒应在 2000-01-01 至 2100-01-01 之间
            if (unixSeconds > 946684800 && unixSeconds < 4102444800)
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            }
        }

        // 兼容存量数据：ISO 8601 "O" 格式（历史 MarkAsCompletedAsync 写入）
        if (DateTimeOffset.TryParse(timestampStr, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out var isoTimestamp))
        {
            return isoTimestamp;
        }

        return null;
    }

    /// <summary>
    /// 手动移除指定事件ID的去重标记
    /// </summary>
    /// <param name="eventId">事件ID</param>
    /// <param name="appKey">应用键（用于多应用场景，避免跨应用冲突）</param>
    /// <returns>是否成功移除</returns>
    public async Task<bool> RemoveAsync(string eventId, string? appKey = null)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return false;
        }

        try
        {
            var redisKey = GetRedisKey(eventId, appKey);
            var result = await _database.KeyDeleteAsync(redisKey);

            if (result)
            {
                _logger?.LogDebug("已移除事件 {EventId} 的去重标记 (AppKey: {AppKey})", eventId, appKey ?? "default");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "移除事件 {EventId} 的去重标记时发生错误", eventId);
            return false;
        }
    }

    /// <summary>
    /// 批量移除多个事件ID的去重标记
    /// </summary>
    /// <param name="eventIds">事件ID集合</param>
    /// <param name="appKey">应用键，用于多应用隔离（可选）</param>
    /// <returns>成功移除的数量</returns>
    public async Task<long> RemoveRangeAsync(IEnumerable<string> eventIds, string? appKey = null)
    {
        if (eventIds == null)
        {
            return 0;
        }

        try
        {
            var keys = eventIds
                .Where(eid => !string.IsNullOrEmpty(eid))
                .Select(eid => GetRedisKey(eid!, appKey))
                .ToArray();

            if (keys.Length == 0)
            {
                return 0;
            }
            var count = 0;
            foreach (var key in keys)
            {
                var result = await _database.KeyDeleteAsync(key);
                if (result)
                {
                    count++;
                }
            }

            _logger?.LogDebug("批量移除了 {Count} 个事件的去重标记 (AppKey: {AppKey})", count, appKey ?? "default");
            return count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "批量移除去重标记时发生错误");
            return 0;
        }
    }

    /// <summary>
    /// 获取当前缓存中的事件数量
    /// </summary>
    /// <returns>事件数量</returns>
    public async Task<long> GetCachedCountAsync()
    {
        try
        {
            var totalCount = 0L;
            var pattern = $"{_keyPrefix}*";

            foreach (var endPoint in _redis.GetEndPoints())
            {
                var redisServer = _redis.GetServer(endPoint);

                await foreach (var key in redisServer.KeysAsync(pattern: pattern, pageSize: 1000))
                {
                    totalCount++;
                }
            }

            _logger?.LogDebug("当前缓存中的事件数量: {Count}", totalCount);
            return totalCount;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取缓存事件数量时发生错误");
            return 0;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await Task.CompletedTask;
    }

    /// <summary>
    /// 生成 Redis 键
    /// </summary>
    private string GetRedisKey(string eventId, string? appKey = null)
    {
        if (!string.IsNullOrEmpty(appKey))
        {
            return $"{_keyPrefix}{appKey}:{eventId}";
        }
        return $"{_keyPrefix}{eventId}";
    }
}
