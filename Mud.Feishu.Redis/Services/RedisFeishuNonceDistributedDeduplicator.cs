// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 基于 Redis 的分布式 Nonce 去重服务实现
/// 使用 Redis SETNX + EXPIRE 实现原子性去重，用于防止重放攻击
/// </summary>
/// <remarks>
/// 此实现适用于分布式部署场景，使用 Redis 作为共享存储。
/// 通过 SETNX (SET if Not Exists) 和 EXPIRE 命令确保原子性操作。
/// </remarks>
public class RedisFeishuNonceDistributedDeduplicator : IFeishuNonceDistributedDeduplicator, IAsyncDisposable
{
    private readonly ILogger<RedisFeishuNonceDistributedDeduplicator>? _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly TimeSpan _defaultNonceTtl;
    private readonly string _keyPrefix;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="redis">Redis 连接多路复用器</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="nonceTtl">默认 Nonce 有效期</param>
    /// <param name="keyPrefix">Redis 键前缀，默认为 "feishu:nonce:"</param>
    public RedisFeishuNonceDistributedDeduplicator(
        IConnectionMultiplexer redis,
        ILogger<RedisFeishuNonceDistributedDeduplicator>? logger = null,
        TimeSpan? nonceTtl = null,
        string? keyPrefix = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger;
        _database = _redis.GetDatabase();
        _defaultNonceTtl = nonceTtl ?? TimeSpan.FromMinutes(5);
        _keyPrefix = keyPrefix ?? "feishu:nonce:";

        _logger?.LogInformation("飞书 Redis 分布式 Nonce 去重服务初始化完成，Nonce TTL: {Ttl}, 键前缀: {KeyPrefix}",
            _defaultNonceTtl, _keyPrefix);
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkAsUsedAsync(string nonce, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(nonce))
        {
            _logger?.LogWarning("Nonce 为空，跳过去重检查");
            return false;
        }

        try
        {
            var actualTtl = ttl ?? _defaultNonceTtl;
            var redisKey = GetRedisKey(nonce);

            // 使用 SETNX + EXPIRE 实现原子性去重（仅当键不存在时设置，并设置过期时间）
            var setResult = await _database.StringSetAsync(
                redisKey,
                "1",
                actualTtl,
                When.NotExists);

            if (!setResult)
            {
                _logger?.LogWarning("Nonce {Nonce} 已使用过，拒绝重放攻击", nonce);
                return true; // 已使用且未过期
            }

            _logger?.LogDebug("Nonce {Nonce} 标记为已使用，TTL: {Ttl}", nonce, actualTtl);
            return false; // 未使用
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "尝试标记 Nonce {Nonce} 为已使用时发生错误", nonce);
            // Redis 异常时，为防止阻塞处理，返回 false（允许处理，但记录错误）
            // 生产环境应根据业务需求调整此行为
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsUsedAsync(string nonce, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(nonce))
        {
            return false;
        }

        try
        {
            var redisKey = GetRedisKey(nonce);
            var exists = await _database.KeyExistsAsync(redisKey);

            _logger?.LogDebug("Nonce {Nonce} 使用状态: {Status}", nonce, exists ? "已使用" : "未使用");
            return exists;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "检查 Nonce {Nonce} 使用状态时发生错误", nonce);
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
    /// 手动移除指定 Nonce 的去重标记
    /// </summary>
    /// <param name="nonce">Nonce 值</param>
    /// <returns>是否成功移除</returns>
    public async Task<bool> RemoveAsync(string nonce)
    {
        if (string.IsNullOrEmpty(nonce))
        {
            return false;
        }

        try
        {
            var redisKey = GetRedisKey(nonce);
            var result = await _database.KeyDeleteAsync(redisKey);

            if (result)
            {
                _logger?.LogDebug("已移除 Nonce {Nonce} 的去重标记", nonce);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "移除 Nonce {Nonce} 的去重标记时发生错误", nonce);
            return false;
        }
    }

    /// <summary>
    /// 批量移除多个 Nonce 的去重标记
    /// </summary>
    /// <param name="nonces">Nonce 集合</param>
    /// <returns>成功移除的数量</returns>
    public async Task<long> RemoveRangeAsync(IEnumerable<string> nonces)
    {
        if (nonces == null)
        {
            return 0;
        }

        try
        {
            var keys = nonces
                .Where(n => !string.IsNullOrEmpty(n))
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

            _logger?.LogDebug("批量移除了 {Count} 个 Nonce 的去重标记", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "批量移除 Nonce 去重标记时发生错误");
            return 0;
        }
    }

    /// <summary>
    /// 获取当前缓存中的 Nonce 数量
    /// </summary>
    /// <returns>Nonce 数量</returns>
    public async Task<long> GetCachedCountAsync()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{_keyPrefix}*");
            var count = keys.Count();

            _logger?.LogDebug("当前缓存中的 Nonce 数量: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取缓存 Nonce 数量时发生错误");
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
    /// <param name="nonce">Nonce 值</param>
    /// <returns>Redis 键</returns>
    private string GetRedisKey(string nonce)
    {
        return $"{_keyPrefix}{nonce}";
    }
}
