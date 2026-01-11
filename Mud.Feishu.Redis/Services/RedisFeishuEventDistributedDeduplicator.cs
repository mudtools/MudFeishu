// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 基于 Redis 的分布式事件去重服务实现
/// 使用 Redis SETNX + EXPIRE 实现原子性去重
/// </summary>
/// <remarks>
/// 此实现适用于分布式部署场景，使用 Redis 作为共享存储。
/// 通过 SETNX (SET if Not Exists) 和 EXPIRE 命令确保原子性操作。
/// </remarks>
public class RedisFeishuEventDistributedDeduplicator : IFeishuEventDistributedDeduplicator, IAsyncDisposable
{
    private readonly ILogger<RedisFeishuEventDistributedDeduplicator>? _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly TimeSpan _defaultCacheExpiration;
    private readonly string _keyPrefix;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="redis">Redis 连接多路复用器</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cacheExpiration">默认缓存过期时间</param>
    /// <param name="keyPrefix">Redis 键前缀，默认为 "feishu:event:"</param>
    public RedisFeishuEventDistributedDeduplicator(
        IConnectionMultiplexer redis,
        ILogger<RedisFeishuEventDistributedDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        string? keyPrefix = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger;
        _database = _redis.GetDatabase();
        _defaultCacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
        _keyPrefix = keyPrefix ?? "feishu:event:";

        _logger?.LogInformation("飞书 Redis 分布式事件去重服务初始化完成，缓存过期时间: {Expiration}, 键前缀: {KeyPrefix}",
            _defaultCacheExpiration, _keyPrefix);
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkAsProcessedAsync(string eventId, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            _logger?.LogWarning("事件ID为空，跳过去重检查");
            return false;
        }

        try
        {
            var actualTtl = ttl ?? _defaultCacheExpiration;
            var redisKey = GetRedisKey(eventId);

            // 使用 SETNX + EXPIRE 实现原子性去重（仅当键不存在时设置，并设置过期时间）
            var setResult = await _database.StringSetAsync(
                redisKey,
                "1",
                actualTtl,
                When.NotExists);

            if (!setResult)
            {
                _logger?.LogDebug("事件 {EventId} 已处理过，跳过", eventId);
                return true; // 已处理
            }

            _logger?.LogDebug("事件 {EventId} 标记为已处理，TTL: {Ttl}", eventId, actualTtl);
            return false; // 未处理，新事件
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "尝试标记事件 {EventId} 为已处理时发生错误", eventId);
            // Redis 异常时，为防止阻塞处理，返回 false（允许处理，但记录错误）
            // 生产环境应根据业务需求调整此行为
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsProcessedAsync(string eventId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return false;
        }

        try
        {
            var redisKey = GetRedisKey(eventId);
            var exists = await _database.KeyExistsAsync(redisKey);

            _logger?.LogDebug("事件 {EventId} 处理状态: {Status}", eventId, exists ? "已处理" : "未处理");
            return exists;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "检查事件 {EventId} 处理状态时发生错误", eventId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        // Redis 使用 EXPIRE 自动清理过期键，无需手动清理
        // 此方法为兼容接口而保留，返回 0 表示无需清理
        _logger?.LogDebug("Redis 自动清理过期键，无需手动清理");
        await Task.CompletedTask;
        return 0;
    }

    /// <summary>
    /// 手动移除指定事件ID的去重标记
    /// </summary>
    /// <param name="eventId">事件ID</param>
    /// <returns>是否成功移除</returns>
    public async Task<bool> RemoveAsync(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return false;
        }

        try
        {
            var redisKey = GetRedisKey(eventId);
            var result = await _database.KeyDeleteAsync(redisKey);

            if (result)
            {
                _logger?.LogDebug("已移除事件 {EventId} 的去重标记", eventId);
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
    /// <returns>成功移除的数量</returns>
    public async Task<long> RemoveRangeAsync(IEnumerable<string> eventIds)
    {
        if (eventIds == null)
        {
            return 0;
        }

        try
        {
            var keys = eventIds
                .Where(eid => !string.IsNullOrEmpty(eid))
                .Select(GetRedisKey)
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

            _logger?.LogDebug("批量移除了 {Count} 个事件的去重标记", count);
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
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{_keyPrefix}*");
            var count = keys.Count();

            _logger?.LogDebug("当前缓存中的事件数量: {Count}", count);
            return count;
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

        // ConnectionMultiplexer 应由调用者管理，此处不释放
        await Task.CompletedTask;
    }

    /// <summary>
    /// 生成 Redis 键
    /// </summary>
    /// <param name="eventId">事件ID</param>
    /// <returns>Redis 键</returns>
    private string GetRedisKey(string eventId)
    {
        return $"{_keyPrefix}{eventId}";
    }
}
