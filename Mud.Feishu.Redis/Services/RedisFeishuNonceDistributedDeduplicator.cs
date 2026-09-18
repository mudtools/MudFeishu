// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  本项目基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 基于 Redis 的分布式 Nonce 去重服务实现
/// 使用 SET NX EX 实现原子性去重，用于防止重放攻击
/// </summary>
/// <remarks>
/// 此实现适用于分布式部署场景，使用 Redis 作为共享存储。
/// 通过 SET NX EX (SET if Not Exists + Expire) 命令确保原子性操作。
/// </remarks>
public class RedisFeishuNonceDistributedDeduplicator : IFeishuNonceDistributedDeduplicator, IAsyncDisposable, IDisposable
{
    private readonly ILogger<RedisFeishuNonceDistributedDeduplicator>? _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly TimeSpan _defaultNonceTtl;
    private readonly string _keyPrefix;
    private volatile bool _disposed;

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
        _keyPrefix = keyPrefix ?? Mud.Feishu.Abstractions.Consts.DefaultNonceKeyPrefix;

        _logger?.LogInformation("飞书 Redis 分布式 Nonce 去重服务初始化完成，Nonce TTL: {Ttl}, 键前缀: {KeyPrefix}",
            _defaultNonceTtl, _keyPrefix);
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkAsUsedAsync(string nonce, string? appKey = null, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(nonce))
        {
            _logger?.LogWarning("Nonce 为空，跳过去重检查");
            return false;
        }

        // R-12：TTL 非正直接抛异常（双保险，Validate 已在启动期拦截）
        var actualTtl = ttl ?? _defaultNonceTtl;
        if (actualTtl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ttl), "Nonce TTL 必须为正值");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var redisKey = GetRedisKey(nonce, appKey);

            // 使用 SET NX EX 实现原子性去重（仅当键不存在时设置，并设置过期时间）
            var setResult = await _database.StringSetAsync(
                redisKey,
                "1",
                actualTtl,
                When.NotExists,
                flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);

            if (!setResult)
            {
                _logger?.LogWarning("Nonce {Nonce} 已使用过，拒绝重放攻击 (AppKey: {AppKey})", LogSanitizer.Clean(nonce), appKey ?? "default");
                return true; // 已使用且未过期
            }

            _logger?.LogDebug("Nonce {Nonce} 标记为已使用，TTL: {Ttl} (AppKey: {AppKey})", LogSanitizer.Clean(nonce), actualTtl, appKey ?? "default");
            return false; // 未使用
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "Redis 连接异常，Nonce {Nonce} 去重失败", LogSanitizer.Clean(nonce));
            throw new FeishuRedisException(FeishuRedisFailureKind.Connection, "Redis 连接失败，无法完成 Nonce 去重", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "Redis 超时，Nonce {Nonce} 去重失败", LogSanitizer.Clean(nonce));
            throw new FeishuRedisException(FeishuRedisFailureKind.Timeout, "Redis 操作超时", ex);
        }
        catch (RedisServerException ex)
        {
            _logger?.LogError(ex, "Redis 服务端异常，Nonce {Nonce} 去重失败", LogSanitizer.Clean(nonce));
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 服务端错误", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "Redis 操作异常，Nonce {Nonce} 去重失败", LogSanitizer.Clean(nonce));
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 操作失败", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsUsedAsync(string nonce, string? appKey = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(nonce))
        {
            return false;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var redisKey = GetRedisKey(nonce, appKey);
            var exists = await _database.KeyExistsAsync(redisKey, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);

            _logger?.LogDebug("Nonce {Nonce} 使用状态: {Status} (AppKey: {AppKey})", LogSanitizer.Clean(nonce), exists ? "已使用" : "未使用", appKey ?? "default");
            return exists;
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "Redis 连接异常，检查 Nonce {Nonce} 使用状态失败", nonce);
            throw new FeishuRedisException(FeishuRedisFailureKind.Connection, "Redis 连接失败，无法检查 Nonce 状态", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "Redis 超时，检查 Nonce {Nonce} 使用状态失败", nonce);
            throw new FeishuRedisException(FeishuRedisFailureKind.Timeout, "Redis 操作超时", ex);
        }
        catch (RedisServerException ex)
        {
            _logger?.LogError(ex, "Redis 服务端异常，检查 Nonce {Nonce} 使用状态失败", nonce);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 服务端错误", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "Redis 操作异常，检查 Nonce {Nonce} 使用状态失败", nonce);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 操作失败", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        // Redis 使用 EXPIRE 自动清理过期键，无需手动清理
        // 此方法为兼容接口而保留，返回 0 表示无需清理
        _logger?.LogDebug("Redis 自动清理过期键，无需手动清理");
        await Task.CompletedTask.ConfigureAwait(false);
        return 0;
    }

    /// <summary>
    /// 手动移除指定 Nonce 的去重标记
    /// </summary>
    /// <param name="nonce">Nonce 值</param>
    /// <param name="appKey">应用键，用于多应用隔离（可选）</param>
    /// <returns>是否成功移除</returns>
    /// <remarks>best-effort：失败记日志并返回 false，不抛出。</remarks>
    public async Task<bool> RemoveAsync(string nonce, string? appKey = null)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(nonce))
        {
            return false;
        }

        try
        {
            var redisKey = GetRedisKey(nonce, appKey);
            var result = await _database.KeyDeleteAsync(redisKey).ConfigureAwait(false);

            if (result)
            {
                _logger?.LogDebug("已移除 Nonce {Nonce} 的去重标记 (AppKey: {AppKey})", nonce, appKey ?? "default");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "移除 Nonce {Nonce} 的去重标记时发生错误（best-effort，不抛出）", nonce);
            return false;
        }
    }

    /// <summary>
    /// 批量移除多个 Nonce 的去重标记
    /// </summary>
    /// <param name="nonces">Nonce 集合</param>
    /// <param name="appKey">应用键，用于多应用隔离（可选）</param>
    /// <returns>成功移除的数量</returns>
    /// <remarks>best-effort：失败记日志并返回 0，不抛出。</remarks>
    public async Task<long> RemoveRangeAsync(IEnumerable<string> nonces, string? appKey = null)
    {
        ThrowIfDisposed();
        if (nonces == null)
        {
            return 0;
        }

        try
        {
            var keys = nonces
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => GetRedisKey(n, appKey))
                .ToArray();

            if (keys.Length == 0)
            {
                return 0;
            }

            // 分片批量删除（500/批）
            var count = 0L;
            const int batchSize = 500;
            for (int i = 0; i < keys.Length; i += batchSize)
            {
                var batch = keys.Skip(i).Take(batchSize).Select(k => (RedisKey)k).ToArray();
                count += await _database.KeyDeleteAsync(batch).ConfigureAwait(false);
            }

            _logger?.LogDebug("批量移除了 {Count} 个 Nonce 的去重标记 (AppKey: {AppKey})", count, appKey ?? "default");
            return count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "批量移除 Nonce 去重标记时发生错误（best-effort，不抛出）");
            return 0;
        }
    }

    /// <summary>
    /// 获取当前缓存中的 Nonce 数量
    /// </summary>
    /// <returns>Nonce 数量</returns>
    /// <remarks>best-effort：失败记日志并返回 0，不抛出。全库 SCAN，禁止热路径调用。</remarks>
    public async Task<long> GetCachedCountAsync()
    {
        ThrowIfDisposed();
        try
        {
            var totalCount = 0;
            var pattern = RedisKeyBuilder.Combine(_keyPrefix) + "*";

            foreach (var endPoint in _redis.GetEndPoints())
            {
                var redisServer = _redis.GetServer(endPoint);
                if (redisServer.IsReplica)
                    continue;

                await foreach (var key in redisServer.KeysAsync(pattern: pattern, pageSize: 1000).ConfigureAwait(false))
                {
                    totalCount++;
                }
            }

            _logger?.LogDebug("当前缓存中的 Nonce 数量: {Count}", totalCount);
            return totalCount;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取缓存 Nonce 数量时发生错误（best-effort，不抛出）");
            return 0;
        }
    }

    /// <summary>
    /// 释放资源（R-14：补 IDisposable，同步释放容器不抛 InvalidOperationException）
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        // ConnectionMultiplexer 应由调用者管理，此处不释放
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// 释放检查
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RedisFeishuNonceDistributedDeduplicator));
    }

    /// <summary>
    /// 生成 Redis 键（使用 RedisKeyBuilder 统一构造，含转义和护栏）
    /// </summary>
    /// <param name="nonce">Nonce 值</param>
    /// <param name="appKey">应用键，用于多应用隔离（可选）</param>
    /// <returns>Redis 键</returns>
    private string GetRedisKey(string nonce, string? appKey = null)
    {
        if (!string.IsNullOrEmpty(appKey))
        {
            return RedisKeyBuilder.Combine(_keyPrefix, appKey, nonce);
        }
        return RedisKeyBuilder.Combine(_keyPrefix, nonce);
    }
}
