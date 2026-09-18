// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
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
/// <remarks>
/// <para><b>ADR-3（T-M2-4）</b>：新增 <c>scopeKey</c> 构造参数，用于多实例/多应用隔离。
/// 键格式为 <c>{prefix}{scopeKey}{seqId}</c>，Sorted Set 为 <c>{prefix}{scopeKey}set</c>。</para>
/// <para><b>ADR-4（T-M2-3）</b>：Sorted Set 在写入时刷新 TTL 并执行 <c>ZREMRANGEBYSCORE</c> 裁剪，
/// 使集合大小 ≈ TTL 窗口内的消息量（有界）。<see cref="GetMaxProcessedSeqId"/> 语义收窄为
/// "TTL 窗口内已处理的最大 SeqID"。</para>
/// </remarks>
public class RedisFeishuSeqIDDeduplicator : IFeishuSeqIDDeduplicator, IAsyncDisposable, IDisposable
{
    private readonly ILogger<RedisFeishuSeqIDDeduplicator>? _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly TimeSpan _defaultCacheExpiration;
    private readonly string _keyPrefix;
    private readonly string _scopeKey;
    private readonly string _sortedSetKey;
    private volatile bool _disposed;

    // ADR-4：String 键 + ZADD 单脚本原子化，消除半写状态；写入时裁剪 + 刷新 TTL
    private const string TryMarkAsProcessedLuaScript = @"
local stringKey = KEYS[1]
local sortedSetKey = KEYS[2]
local seqId = ARGV[1]
local ttlSeconds = ARGV[2]
local now = tonumber(redis.call('TIME')[1])
local expireBefore = now - tonumber(ttlSeconds)

-- TTL 非正 → 显式失败（R-04 护栏，与事件去重一致）
if tonumber(ttlSeconds) <= 0 then
    return redis.error_reply('ttl must be positive')
end

-- 原子化：SETNX + ZADD + 裁剪 + EXPIRE
local setResult = redis.call('SET', stringKey, '1', 'EX', tonumber(ttlSeconds), 'NX')
if setResult == false or setResult == nil then
    -- 键已存在 → 已处理
    return 1
end

-- 写入 Sorted Set（score=seqId, member=seqId）
redis.call('ZADD', sortedSetKey, tonumber(seqId), seqId)

-- 写入时裁剪：移除 TTL 窗口外的旧成员（ADR-4）
-- 注意：Redis 排他区间语法仅有前导 '('，没有闭括号——'(1789691238)' 会导致
-- ERR min or max is not a float（strtod 在尾部 ')' 处解析失败）
redis.call('ZREMRANGEBYSCORE', sortedSetKey, '-inf', '(' .. tostring(expireBefore))

-- 刷新 Sorted Set TTL（与 String 键同生命周期）
redis.call('EXPIRE', sortedSetKey, tonumber(ttlSeconds))

return 0
";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="redis">Redis 连接多路复用器</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cacheExpiration">默认缓存过期时间</param>
    /// <param name="keyPrefix">Redis 键前缀，默认为 "feishu:seqid:"</param>
    /// <param name="scopeKey">隔离维度键（ADR-3）。必须非空——空值会导致多实例互相判重（R-07）</param>
    /// <exception cref="ArgumentException"><paramref name="scopeKey"/> 为 null 或空白</exception>
    public RedisFeishuSeqIDDeduplicator(
        IConnectionMultiplexer redis,
        ILogger<RedisFeishuSeqIDDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        string? keyPrefix = null,
        string? scopeKey = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger;
        _database = _redis.GetDatabase();
        _defaultCacheExpiration = cacheExpiration ?? TimeSpan.FromMilliseconds(Mud.Feishu.Abstractions.Consts.DefaultCacheExpirationMs);
        _keyPrefix = keyPrefix ?? Mud.Feishu.Abstractions.Consts.DefaultSeqIdKeyPrefix;

        // ADR-3：scopeKey 必须非空（fail-fast，防止退化为全局共享键导致多实例丢帧）
        // is null 显式判空，保证后续 _scopeKey = scopeKey 的非空流分析在所有 TFM 下成立
        if (scopeKey is null || string.IsNullOrWhiteSpace(scopeKey))
            throw new ArgumentException(
                "scopeKey 不能为空——空 scopeKey 会导致多实例互相判重（R-07 修复）。" +
                "多实例共享 Redis 时，scopeKey 必须包含实例维度（如 AppKey+MachineName）。",
                nameof(scopeKey));
        _scopeKey = scopeKey;

        // 预计算 Sorted Set 键
        _sortedSetKey = RedisKeyBuilder.Combine(_keyPrefix, _scopeKey, "set");

        _logger?.LogInformation("飞书 Redis SeqID 去重服务初始化完成，缓存过期时间: {Expiration}, 键前缀: {KeyPrefix}, ScopeKey: {ScopeKey}, SortedSetKey: {SortedSetKey}",
            _defaultCacheExpiration, _keyPrefix, _scopeKey, _sortedSetKey);
    }

    /// <summary>
    /// 尝试标记 SeqID 为已处理
    /// </summary>
    public async Task<bool> TryMarkAsProcessedAsync(ulong seqId)
    {
        ThrowIfDisposed();
        try
        {
            var redisKey = GetRedisKey(seqId);
            var ttlSeconds = Math.Max(1, (long)_defaultCacheExpiration.TotalSeconds);

            // ADR-4：使用 Lua 脚本原子化 SETNX + ZADD + 裁剪 + EXPIRE
            var result = (long)await _database.ScriptEvaluateAsync(
                TryMarkAsProcessedLuaScript,
                new RedisKey[] { redisKey, _sortedSetKey },
                new RedisValue[] { seqId.ToString(), ttlSeconds }
            ).ConfigureAwait(false);

            if (result == 1)
            {
                _logger?.LogDebug("SeqID {SeqId} 已处理过，跳过 (ScopeKey: {ScopeKey})", seqId, _scopeKey);
                return true; // 已处理
            }

            return false; // 未处理，新消息
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "Redis 连接异常，SeqID {SeqId} 去重失败", seqId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Connection, "Redis 连接失败，无法完成 SeqID 去重", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "Redis 超时，SeqID {SeqId} 去重失败", seqId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Timeout, "Redis 操作超时", ex);
        }
        catch (RedisServerException ex)
        {
            _logger?.LogError(ex, "Redis 服务端异常，SeqID {SeqId} 去重失败", seqId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 服务端错误", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "Redis 操作异常，SeqID {SeqId} 去重失败", seqId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 操作失败", ex);
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync(ulong seqId)
    {
        ThrowIfDisposed();
        // ADR-6.3：best-effort API — 失败记日志，不抛
        try
        {
            var redisKey = GetRedisKey(seqId);

            // 原子化删除 String 键和 Sorted Set 条目
            await _database.KeyDeleteAsync(redisKey).ConfigureAwait(false);
            await _database.SortedSetRemoveAsync(_sortedSetKey, seqId.ToString()).ConfigureAwait(false);

            _logger?.LogDebug("SeqID {SeqId} 已回滚，允许重新处理 (ScopeKey: {ScopeKey})", seqId, _scopeKey);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "回滚 SeqID {SeqId} 时发生错误（best-effort，不抛出）", seqId);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsProcessedAsync(ulong seqId)
    {
        ThrowIfDisposed();
        try
        {
            var redisKey = GetRedisKey(seqId);
            var exists = await _database.KeyExistsAsync(redisKey).ConfigureAwait(false);
            return exists;
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "Redis 连接异常，检查 SeqID {SeqId} 处理状态失败", seqId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Connection, "Redis 连接失败", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "Redis 超时，检查 SeqID {SeqId} 处理状态失败", seqId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Timeout, "Redis 操作超时", ex);
        }
        catch (RedisServerException ex)
        {
            _logger?.LogError(ex, "Redis 服务端异常，检查 SeqID {SeqId} 处理状态失败", seqId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 服务端错误", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "Redis 操作异常，检查 SeqID {SeqId} 处理状态失败", seqId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 操作失败", ex);
        }
    }

    /// <summary>
    /// 异步清空缓存。
    /// <para><b>⚠️ 破坏性操作</b>：此方法会删除所有匹配 <c>{keyPrefix}{scopeKey}*</c> 的键。
    /// 跨实例共享 Redis 时会清空<b>所有匹配该 scopeKey 的实例</b>的 SeqID 状态，仅用于运维场景，
    /// 勿在常规重连路径调用。</para>
    /// </summary>
    /// <remarks>best-effort：失败记日志，不抛出。</remarks>
    public async Task ClearCacheAsync()
    {
        ThrowIfDisposed();
        // R-01 护栏：空前缀或通配符前缀在 RedisKeyBuilder.Combine 中已拦截
        RedisKeyBuilder.Combine(_keyPrefix);

        try
        {
            var count = 0;
            var pattern = $"{_keyPrefix}{RedisKeyBuilder.Escape(_scopeKey)}*";

            foreach (var endPoint in _redis.GetEndPoints())
            {
                var redisServer = _redis.GetServer(endPoint);
                if (redisServer.IsReplica)
                    continue;

                var keysToDelete = new List<RedisKey>();
                await foreach (var key in redisServer.KeysAsync(pattern: pattern, pageSize: 1000).ConfigureAwait(false))
                {
                    keysToDelete.Add(key);

                    if (keysToDelete.Count >= 1000)
                    {
                        var deleted = await _database.KeyDeleteAsync(keysToDelete.ToArray()).ConfigureAwait(false);
                        count += (int)deleted;
                        keysToDelete.Clear();
                    }
                }

                if (keysToDelete.Count > 0)
                {
                    var deleted = await _database.KeyDeleteAsync(keysToDelete.ToArray()).ConfigureAwait(false);
                    count += (int)deleted;
                }
            }

            _logger?.LogInformation("清空了 {Count} 个 SeqID 缓存条目 (ScopeKey: {ScopeKey})", count, _scopeKey);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "清空 SeqID 缓存时发生错误（best-effort，不抛出）");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// ADR-4：语义收窄为"TTL 窗口内已处理的最大 SeqID"（与真实缓存状态一致）。
    /// Sorted Set 在写入时裁剪过期成员，此计数反映当前窗口内的消息量。
    /// </remarks>
    public int GetCacheCount()
    {
        ThrowIfDisposed();
        try
        {
            var sortedSetCount = (long)_database.SortedSetLength(_sortedSetKey);

            _logger?.LogDebug("SeqID 缓存数量 (ScopeKey: {ScopeKey}): {SortedSetCount}",
                _scopeKey, sortedSetCount);

            return (int)sortedSetCount;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取 SeqID 缓存数量时发生错误（best-effort，返回 0）");
            return 0;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// ADR-4：语义收窄为"TTL 窗口内已处理的最大 SeqID"。
    /// Sorted Set 在写入时裁剪过期成员，此值反映当前窗口内的最大值。
    /// </remarks>
    public ulong GetMaxProcessedSeqId()
    {
        ThrowIfDisposed();
        try
        {
            var maxSeqId = _database.SortedSetRangeByScore(_sortedSetKey, order: Order.Descending, take: 1);

            if (maxSeqId.Length > 0 && ulong.TryParse(maxSeqId[0], out var seqId))
            {
                return seqId;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取最大 SeqID 时发生错误（best-effort，返回 0）");
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
            throw new ObjectDisposedException(nameof(RedisFeishuSeqIDDeduplicator));
    }

    /// <summary>
    /// 生成 Redis 键（使用 RedisKeyBuilder 统一构造，含转义和 scopeKey 隔离）
    /// </summary>
    private string GetRedisKey(ulong seqId)
    {
        return RedisKeyBuilder.Combine(_keyPrefix, _scopeKey, seqId.ToString());
    }
}
