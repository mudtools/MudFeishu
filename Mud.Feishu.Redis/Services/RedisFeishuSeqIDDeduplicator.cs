// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 基于 Redis 的分布式 SeqID 去重服务实现
/// <para>使用 Redis Sorted Set 存储已处理的 SeqID，支持范围查询和自动过期</para>
/// <para>适用于多实例部署场景</para>
/// </summary>
public class RedisFeishuSeqIDDeduplicator : IFeishuSeqIDDeduplicator, IAsyncDisposable
{
    private readonly ILogger<RedisFeishuSeqIDDeduplicator>? _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly TimeSpan _defaultCacheExpiration;
    private readonly string _keyPrefix;
    private readonly string _maxSeqIdKey;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="redis">Redis 连接多路复用器</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cacheExpiration">默认缓存过期时间</param>
    /// <param name="keyPrefix">Redis 键前缀，默认为 "feishu:seqid:"</param>
    public RedisFeishuSeqIDDeduplicator(
        IConnectionMultiplexer redis,
        ILogger<RedisFeishuSeqIDDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        string? keyPrefix = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger;
        _database = _redis.GetDatabase();
        _defaultCacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
        _keyPrefix = keyPrefix ?? "feishu:seqid:";
        _maxSeqIdKey = $"{_keyPrefix}max";

        _logger?.LogInformation("飞书 Redis SeqID 去重服务初始化完成，缓存过期时间: {Expiration}, 键前缀: {KeyPrefix}",
            _defaultCacheExpiration, _keyPrefix);
    }

    /// <inheritdoc />
    public bool TryMarkAsProcessed(ulong seqId)
    {
        try
        {
            var redisKey = GetRedisKey(seqId);

            // 使用 SETNX + EXPIRE 实现原子性去重
            var setResult = _database.StringSet(
                redisKey,
                "1",
                _defaultCacheExpiration,
                When.NotExists);

            if (!setResult)
            {
                _logger?.LogDebug("SeqID {SeqId} 已处理过，跳过", seqId);
                return true; // 已处理
            }

            // 更新最大 SeqID（使用 ZINCRBY 记录在 Sorted Set 中）
            var sortedSetKey = $"{_keyPrefix}set";
            _database.SortedSetAdd(sortedSetKey, seqId.ToString(), seqId);

            return false; // 未处理，新消息
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "尝试标记 SeqID {SeqId} 为已处理时发生错误", seqId);
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsProcessed(ulong seqId)
    {
        try
        {
            var redisKey = GetRedisKey(seqId);
            var exists = _database.KeyExists(redisKey);
            return exists;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "检查 SeqID {SeqId} 处理状态时发生错误", seqId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsProcessedAsync(ulong seqId)
    {
        try
        {
            var redisKey = GetRedisKey(seqId);
            var exists = await _database.KeyExistsAsync(redisKey);
            return exists;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "检查 SeqID {SeqId} 处理状态时发生错误", seqId);
            return false;
        }
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{_keyPrefix}*");
            var count = _database.KeyDelete(keys.ToArray());

            _logger?.LogInformation("清空了 {Count} 个 SeqID 缓存条目", count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "清空 SeqID 缓存时发生错误");
        }
    }

    /// <inheritdoc />
    public int GetCacheCount()
    {
        try
        {
            var sortedSetKey = $"{_keyPrefix}set";
            var count = (long)_database.SortedSetLength(sortedSetKey);
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取 SeqID 缓存数量时发生错误");
            return 0;
        }
    }

    /// <inheritdoc />
    public ulong GetMaxProcessedSeqId()
    {
        try
        {
            var sortedSetKey = $"{_keyPrefix}set";
            var maxSeqId = _database.SortedSetRangeByScore(sortedSetKey, order: Order.Descending, take: 1);

            if (maxSeqId.Length > 0 && ulong.TryParse(maxSeqId[0], out var seqId))
            {
                return seqId;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取最大 SeqID 时发生错误");
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
    /// <param name="seqId">SeqID 值</param>
    /// <returns>Redis 键</returns>
    private string GetRedisKey(ulong seqId)
    {
        return $"{_keyPrefix}{seqId}";
    }
}
