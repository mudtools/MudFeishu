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
/// <para><b>ADR-3（T-M2-4）</b>：<c>scopeKey</c> 构造参数用于多实例/多应用隔离。
/// 键格式由 <c>RedisKeyBuilder.Combine</c> 产出：String 键 <c>{prefix}:{scopeKey}:{seqId}</c>，
/// Sorted Set 键 <c>{prefix}:{scopeKey}:set</c>。</para>
/// <para><b>ADR-10（R2-01，取代 ADR-4 的裁剪口径）</b>：Sorted Set 在写入时执行
/// <c>ZREMRANGEBYRANK</c> 裁剪，仅保留分数最大的 <c>windowCapacity</c> 个成员，并刷新 TTL，
/// 使集合大小具有**确定性上界**。<see cref="GetMaxProcessedSeqId"/> 返回窗口内真实最大值
/// （<c>score=SeqID</c>，故按分数降序取 1 即最大值），<see cref="GetCacheCount"/> 返回当前成员数
/// （≤ <c>windowCapacity</c>，**不再等于** String 键 TTL 窗口内的去重规模，不可用于推断剩余去重空间）。</para>
/// <para>历史缺陷说明：ADR-4 曾要求 <c>ZREMRANGEBYSCORE -inf (now - ttl)</c>，但成员分数是 SeqID
/// （自增计数器）而非时间戳，阈值 ≈1.79×10⁹ 恒大于任何真实 SeqID，导致成员写入即被清除、
/// 计数恒为 0；此项在本轮（R2）修正为按容量裁剪。</para>
/// </remarks>
public class RedisFeishuSeqIDDeduplicator : IFeishuSeqIDDeduplicator, IAsyncDisposable, IDisposable
{
    private readonly ILogger<RedisFeishuSeqIDDeduplicator>? _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly TimeSpan _defaultCacheExpiration;
    private readonly string _keyPrefix;
    private readonly string _scopeKey;
    private readonly int _windowCapacity;
    private readonly string _sortedSetKey;
    private volatile bool _disposed;

    // ADR-10：String 键 + ZADD + ZREMRANGEBYRANK 单脚本原子化，消除半写状态；
    // 写入时按容量裁剪 + 刷新 TTL。
    private const string TryMarkAsProcessedLuaScript = @"
local stringKey = KEYS[1]
local sortedSetKey = KEYS[2]
local seqId = ARGV[1]
local ttlSeconds = ARGV[2]
local windowCapacity = ARGV[3]

if tonumber(ttlSeconds) <= 0 then
    return redis.error_reply('ttl must be positive')
end
if tonumber(windowCapacity) <= 0 then
    return redis.error_reply('windowCapacity must be positive')
end

-- 原子化：SETNX + ZADD + 按容量裁剪 + EXPIRE
local setResult = redis.call('SET', stringKey, '1', 'EX', tonumber(ttlSeconds), 'NX')
if setResult == false or setResult == nil then
    -- 键已存在 → 已处理
    return 1
end

-- 写入 Sorted Set（score=seqId, member=seqId）：score 与 member 语义一致，
-- 使按分数降序取首元素即「窗口内最大 SeqID」
redis.call('ZADD', sortedSetKey, tonumber(seqId), seqId)

-- 写入时裁剪：仅保留分数最大的 windowCapacity 个成员（R2-01：不再用时间阈值比较 SeqID 分数）
redis.call('ZREMRANGEBYRANK', sortedSetKey, 0, -(tonumber(windowCapacity) + 1))

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
    /// <param name="windowCapacity">
    /// Sorted Set 容量窗口上界（ADR-10，R2-01）。<c>null</c> 或非正值回落到
    /// <see cref="Mud.Feishu.Abstractions.Consts.DefaultSeqIdWindowCapacity"/>。
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="scopeKey"/> 为 null 或空白</exception>
    public RedisFeishuSeqIDDeduplicator(
        IConnectionMultiplexer redis,
        ILogger<RedisFeishuSeqIDDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        string? keyPrefix = null,
        string? scopeKey = null,
        int? windowCapacity = null)
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

        // ADR-10：容量窗口非正时回落到默认值（fail-safe；Startup 期由 RedisOptions.Validate 拦截误配）
        _windowCapacity = windowCapacity is > 0
            ? windowCapacity.Value
            : Mud.Feishu.Abstractions.Consts.DefaultSeqIdWindowCapacity;

        // 预计算 Sorted Set 键
        _sortedSetKey = RedisKeyBuilder.Combine(_keyPrefix, _scopeKey, "set");

        _logger?.LogInformation("飞书 Redis SeqID 去重服务初始化完成，缓存过期时间: {Expiration}, 键前缀: {KeyPrefix}, ScopeKey: {ScopeKey}, 容量窗口: {WindowCapacity}, SortedSetKey: {SortedSetKey}",
            _defaultCacheExpiration, _keyPrefix, _scopeKey, _windowCapacity, _sortedSetKey);
    }

    /// <summary>
    /// 当前实例的隔离维度键（供诊断门面与非公开装配使用）。
    /// </summary>
    internal string ScopeKey => _scopeKey;

    /// <summary>
    /// 尝试标记 SeqID 为已处理
    /// </summary>
    public async Task<bool> TryMarkAsProcessedAsync(ulong seqId)
    {
        ThrowIfDisposed();
        // R2-21：耗时计时（无分配；本方法在 WS 逐帧热路径上被调用）
        var startTimestamp = RedisMetricsHelper.Begin();
        try
        {
            var redisKey = GetRedisKey(seqId);
            var ttlSeconds = Math.Max(1, (long)_defaultCacheExpiration.TotalSeconds);

            // ADR-10：使用 Lua 脚本原子化 SETNX + ZADD + 按容量裁剪 + EXPIRE
            var result = (long)await _database.ScriptEvaluateAsync(
                TryMarkAsProcessedLuaScript,
                new RedisKey[] { redisKey, _sortedSetKey },
                new RedisValue[] { seqId.ToString(), ttlSeconds, _windowCapacity }
            ).ConfigureAwait(false);

            if (result == 1)
            {
                _logger?.LogDebug("SeqID {SeqId} 已处理过，跳过 (ScopeKey: {ScopeKey})", seqId, _scopeKey);
                RecordOutcome(FeishuMetrics.RedisOutcomes.Duplicate, startTimestamp);
                return true; // 已处理
            }

            RecordOutcome(FeishuMetrics.RedisOutcomes.Success, startTimestamp);
            return false; // 未处理，新消息
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "Redis 连接异常，SeqID {SeqId} 去重失败", seqId);
            RecordFailure(FeishuRedisFailureKind.Connection, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Connection, "Redis 连接失败，无法完成 SeqID 去重", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "Redis 超时，SeqID {SeqId} 去重失败", seqId);
            RecordFailure(FeishuRedisFailureKind.Timeout, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Timeout, "Redis 操作超时", ex);
        }
        catch (RedisServerException ex)
        {
            _logger?.LogError(ex, "Redis 服务端异常，SeqID {SeqId} 去重失败", seqId);
            RecordFailure(FeishuRedisFailureKind.Server, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 服务端错误", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "Redis 操作异常，SeqID {SeqId} 去重失败", seqId);
            RecordFailure(FeishuRedisFailureKind.Server, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 操作失败", ex);
        }
    }

    /// <summary>上报一次 SeqID 去重结果（R2-21）。</summary>
    private static void RecordOutcome(string outcome, long startTimestamp)
        => RedisMetricsHelper.Record(FeishuMetrics.RedisCommands.TryMarkProcessing, FeishuMetrics.DedupTypes.SeqId, outcome, startTimestamp);

    /// <summary>上报一次 SeqID 去重失败（R2-21）。</summary>
    private static void RecordFailure(FeishuRedisFailureKind kind, long startTimestamp)
        => RedisMetricsHelper.Record(
            FeishuMetrics.RedisCommands.TryMarkProcessing,
            FeishuMetrics.DedupTypes.SeqId,
            RedisMetricsHelper.FromFailureKind(kind),
            startTimestamp);

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
            RedisMetricsHelper.Record(FeishuMetrics.RedisCommands.RollbackProcessing, FeishuMetrics.DedupTypes.SeqId, FeishuMetrics.RedisOutcomes.Success);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "回滚 SeqID {SeqId} 时发生错误（best-effort，不抛出）", seqId);
            RedisMetricsHelper.Record(
                FeishuMetrics.RedisCommands.RollbackProcessing,
                FeishuMetrics.DedupTypes.SeqId,
                FeishuMetrics.RedisOutcomes.Server);
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
    /// <para><b>⚠️ 破坏性操作</b>：此方法会删除所有匹配
    /// <c>{keyPrefix}:{scopeKey}:*</c> 的键（String 键与 Sorted Set 键）。
    /// 跨实例共享 Redis 时会清空<b>所有匹配该 scopeKey 的实例</b>的 SeqID 状态。</para>
    /// <para>调用方：<c>FeishuWebSocketClient.ResetStateOnReconnectAsync</c> 在每次 WS 重连时调用
    /// （目的：避免上一连接的 SeqID 残留抑制新连接的帧）；运维修正亦可手工调用。</para>
    /// </summary>
    /// <remarks>best-effort：失败记日志，不抛出。</remarks>
    public async Task ClearCacheAsync()
    {
        ThrowIfDisposed();
        // R-01 护栏：空前缀或通配符前缀在 RedisKeyBuilder.Combine 中已拦截
        RedisKeyBuilder.Combine(_keyPrefix);

        try
        {
            var count = 0L;
            // R2-02：模式必须与键同源（RedisKeyBuilder.Pattern）——历史实现用裸拼接
            // $"{_keyPrefix}{Escape(_scopeKey)}*" 缺少 Combine 插入的分隔符，恒不匹配实际键，
            // 导致本方法一个键都删不掉（清理静默失效）。
            var pattern = RedisKeyBuilder.Pattern(_keyPrefix, _scopeKey);

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
                        count += await _database.KeyDeleteAsync(keysToDelete.ToArray()).ConfigureAwait(false);
                        keysToDelete.Clear();
                    }
                }

                if (keysToDelete.Count > 0)
                {
                    count += await _database.KeyDeleteAsync(keysToDelete.ToArray()).ConfigureAwait(false);
                }
            }

            _logger?.LogInformation("清空了 {Count} 个 SeqID 缓存条目 (ScopeKey: {ScopeKey}, Pattern: {Pattern})", count, _scopeKey, pattern);
            // R2-21：清理效果可观测——历史缺陷（R2-02）下该值恒为 0，一眼可辨
            RedisMetricsHelper.RecordScan(FeishuMetrics.RedisCommands.ClearCache, count, deleted: true);
            RedisMetricsHelper.Record(FeishuMetrics.RedisCommands.ClearCache, FeishuMetrics.DedupTypes.SeqId, FeishuMetrics.RedisOutcomes.Success);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "清空 SeqID 缓存时发生错误（best-effort，不抛出）");
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// ADR-10（R2-01）：返回 Sorted Set 当前成员数，上界为构造时传入的 <c>windowCapacity</c>。
    /// <para>
    /// <b>语义边界</b>：本计数反映"最近 <c>windowCapacity</c> 条已处理 SeqID"，与 String 键的
    /// TTL 窗口<b>不等价</b>，不可用于推断剩余去重空间或去重规模。
    /// </para>
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
    public async Task<int> GetCacheCountAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            var sortedSetCount = (long)await _database.SortedSetLengthAsync(_sortedSetKey).ConfigureAwait(false);

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
    /// ADR-10（R2-01）：成员分数即 SeqID，故按分数降序取首元素即"当前窗口内已处理的最大 SeqID"。
    /// 历史缺陷（时间阈值裁剪 + SeqID 分数混用）曾使集合恒空、本方法恒返回 0。
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

    /// <inheritdoc />
    public async Task<ulong> GetMaxProcessedSeqIdAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            var maxSeqId = await _database.SortedSetRangeByScoreAsync(_sortedSetKey,
                order: Order.Descending, take: 1).ConfigureAwait(false);

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
