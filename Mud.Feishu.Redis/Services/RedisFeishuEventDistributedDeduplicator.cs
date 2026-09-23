// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;
using StackExchange.Redis;
using System.Threading;

// ADR-2/ADR-6 修复标记：状态机 v2 三脚本化 + 服务端时钟 + FeishuRedisException


#pragma warning disable CS0618 // R5/X6: Obsolete dual-read fallback base — intentionally references DeduplicationOptions/EventDeduplicationOptions
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
// R2-27：声明 IDisposable（类内已有 Dispose() 方法，但此前**未在基列表声明接口**——
// MS.DI 按**实现类型**判断可释放性，缺少 IDisposable 会让 `ServiceProvider.Dispose()`（同步）
// 抛「type only implements IAsyncDisposable. Use DisposeAsync to dispose the container.」，
// 即 R-14 声称的修复在 DI 路径上并未生效。
public class RedisFeishuEventDistributedDeduplicator : IFeishuEventDeduplicator, IAsyncDisposable, IDisposable
{
    private readonly ILogger<RedisFeishuEventDistributedDeduplicator>? _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly TimeSpan _defaultCacheExpiration;
    private readonly TimeSpan _defaultProcessingTimeout;
    private readonly string _keyPrefix;
    private volatile bool _disposed;

    private const string StatusField = "status";
    private const string TimestampField = "timestamp";
    private const string TimeoutField = "timeout";
    private const string ProcessingStatus = "processing";
    private const string CompletedStatus = "completed";

    // ADR-2：状态机 v2 — 三个独立 Lua 脚本，统一使用服务端 redis.call('TIME') 消除时钟漂移（R-15）
    // 返回值：0=Success, 1=Duplicate(Completed), 2=Duplicate(Processing), 3=TimeoutRecoverable
    private const string TryMarkAsProcessingLuaScript = @"
        local key = KEYS[1]
        local processingTimeoutSeconds = ARGV[1]
        local ttlSeconds = ARGV[2]

        -- R-15：使用服务端时钟，消除实例间时钟漂移
        local now = tonumber(redis.call('TIME')[1])

        -- R-04：TTL 非正 → 显式失败而非静默删键
        if tonumber(ttlSeconds) <= 0 then
            return redis.error_reply('ttl must be positive')
        end

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
                local ts = tonumber(timestamp)
                if ts == nil then
                    -- R2-13：timestamp 缺失或非数字（历史 ISO 存量键 / 外部误写）→ 视为「仍在处理中」，
                    -- 不抢占、不重复（fail-safe）。此前 now - nil 会抛 Lua 运行时错误，
                    -- 被包装为 Server 类不可降级异常，使该 eventId 的请求恒失败。
                    return 2
                end
                if now - ts > tonumber(processingTimeoutSeconds) then
                    -- 超时可恢复：重新标记为 processing（T-M2-2：刷新 timeout 字段）
                    redis.call('HSET', key, 'status', 'processing', 'timestamp', now, 'timeout', processingTimeoutSeconds)
                    redis.call('EXPIRE', key, tonumber(ttlSeconds))
                    return 3
                end
                return 2
            end
        end

        -- 新标记为 processing（ADR-2/T-M2-2：同时写入 timeout 字段，供 GetStatusAsync 判定）
        redis.call('HSET', key, 'status', 'processing', 'timestamp', now, 'timeout', processingTimeoutSeconds)
        redis.call('EXPIRE', key, tonumber(ttlSeconds))
        return 0
        ";

    // ADR-2 / R-05：MarkAsCompleted — 键不存在则不创建，返回 0 由 C# 端记 Warning
    // WHF-11（R-15 完全兑现）：时间戳改用脚本内 redis.call('TIME')（与 TryMark 脚本一致），
    // 消除 C# 端 DateTimeOffset.UtcNow 与 Redis 服务端时钟漂移
    private const string MarkAsCompletedLuaScript = @"
        local key = KEYS[1]
        local ttlSeconds = ARGV[1]

        if redis.call('EXISTS', key) == 0 then
            return 0  -- 键不存在，不创建永久键
        end

        local now = tonumber(redis.call('TIME')[1])
        redis.call('HSET', key, 'status', 'completed', 'timestamp', now)
        redis.call('EXPIRE', key, tonumber(ttlSeconds))
        return 1
        ";

    // ADR-2 / R-06：RollbackProcessing — 读-改-删原子化，竞态下不会误删 completed
    private const string RollbackProcessingLuaScript = @"
        local key = KEYS[1]

        if redis.call('HGET', key, 'status') == 'processing' then
            redis.call('DEL', key)
            return 1
        end
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
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(eventId))
        {
            _logger?.LogWarning("事件ID为空，跳过去重检查");
            return DeduplicationResult.Success(eventId);
        }

        // R-04：TTL 非正直接抛异常，不走 Lua（双保险）
        var actualTtl = ttl ?? _defaultCacheExpiration;
        if (actualTtl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ttl), "TTL 必须为正值");

        // R2-21：耗时计时（无分配），成功/失败两条路径共用同一时间戳
        var startTimestamp = RedisMetricsHelper.Begin();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var actualProcessingTimeout = processingTimeout ?? _defaultProcessingTimeout;
            var redisKey = GetRedisKey(eventId, appKey);
            var processingTimeoutSeconds = Math.Max(1, (long)actualProcessingTimeout.TotalSeconds);
            var ttlSeconds = Math.Max(1, (long)actualTtl.TotalSeconds);

            var result = (long)await _database.ScriptEvaluateAsync(
                TryMarkAsProcessingLuaScript,
                new RedisKey[] { redisKey },
                new RedisValue[] { processingTimeoutSeconds, ttlSeconds }
            );

            // R2-21：去重决策可观测（成功/重复/超时可恢复）
            RedisMetricsHelper.Record(
                FeishuMetrics.RedisCommands.TryMarkProcessing,
                FeishuMetrics.DedupTypes.Event,
                result switch
                {
                    0 => FeishuMetrics.RedisOutcomes.Success,
                    1 or 2 => FeishuMetrics.RedisOutcomes.Duplicate,
                    _ => FeishuMetrics.RedisOutcomes.TimeoutRecoverable
                },
                startTimestamp);

            // R-17：未知返回值 fail-closed
            return result switch
            {
                0 => LogAndReturnSuccess(eventId, appKey, actualTtl),
                1 => LogAndReturnDuplicate(eventId, appKey, false, DeduplicationStatus.Completed),
                2 => LogAndReturnDuplicate(eventId, appKey, true, DeduplicationStatus.Processing),
                3 => LogAndReturnTimeoutRecoverable(eventId, appKey, actualProcessingTimeout),
                _ => throw new InvalidOperationException($"Lua 脚本返回未知值 {result}，无法判定去重状态（fail-closed）")
            };
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "Redis 连接异常，事件 {EventId} 去重失败", eventId);
            RecordFailure(FeishuMetrics.RedisCommands.TryMarkProcessing, FeishuRedisFailureKind.Connection, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Connection, "Redis 连接失败，无法完成去重", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "Redis 超时，事件 {EventId} 去重失败", eventId);
            RecordFailure(FeishuMetrics.RedisCommands.TryMarkProcessing, FeishuRedisFailureKind.Timeout, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Timeout, "Redis 操作超时", ex);
        }
        catch (RedisServerException ex)
        {
            _logger?.LogError(ex, "Redis 服务端异常，事件 {EventId} 去重失败", eventId);
            RecordFailure(FeishuMetrics.RedisCommands.TryMarkProcessing, FeishuRedisFailureKind.Server, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 服务端错误", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "Redis 操作异常，事件 {EventId} 去重失败", eventId);
            RecordFailure(FeishuMetrics.RedisCommands.TryMarkProcessing, FeishuRedisFailureKind.Server, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 操作失败", ex);
        }
    }

    /// <summary>
    /// 上报一次事件去重失败（R2-21）。
    /// </summary>
    private static void RecordFailure(string command, FeishuRedisFailureKind kind, long startTimestamp)
        => RedisMetricsHelper.Record(command, FeishuMetrics.DedupTypes.Event, RedisMetricsHelper.FromFailureKind(kind), startTimestamp);

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
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(eventId))
            return;

        var startTimestamp = RedisMetricsHelper.Begin();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var redisKey = GetRedisKey(eventId, appKey);
            var ttlSeconds = Math.Max(1, (long)_defaultCacheExpiration.TotalSeconds);

            // ADR-2 / R-05 / WHF-11：使用 Lua 脚本（键不存在则不创建），时间戳由服务端时钟写入
            var result = (long)await _database.ScriptEvaluateAsync(
                MarkAsCompletedLuaScript,
                new RedisKey[] { redisKey },
                new RedisValue[] { ttlSeconds }
            );

            if (result == 0)
            {
                _logger?.LogWarning("事件 {EventId} 的去重键不存在，MarkAsCompleted 未创建新键（R-05 修复行为）(AppKey: {AppKey})",
                    eventId, appKey ?? "default");
            }
            else
            {
                _logger?.LogDebug("事件 {EventId} 标记为已完成 (AppKey: {AppKey})", eventId, appKey ?? "default");
            }

            // R2-21：key_missing 与 success 分列——可量化 R-05 行为（对缺失键不再创建永久记录）
            RecordFailureOutcome(
                FeishuMetrics.RedisCommands.MarkCompleted,
                result == 0 ? FeishuMetrics.RedisOutcomes.KeyMissing : FeishuMetrics.RedisOutcomes.Success,
                startTimestamp);
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "Redis 连接异常，标记事件 {EventId} 为已完成失败", eventId);
            RecordFailure(FeishuMetrics.RedisCommands.MarkCompleted, FeishuRedisFailureKind.Connection, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Connection, "Redis 连接失败，无法标记事件为已完成", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "Redis 超时，标记事件 {EventId} 为已完成失败", eventId);
            RecordFailure(FeishuMetrics.RedisCommands.MarkCompleted, FeishuRedisFailureKind.Timeout, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Timeout, "Redis 操作超时", ex);
        }
        catch (RedisServerException ex)
        {
            _logger?.LogError(ex, "Redis 服务端异常，标记事件 {EventId} 为已完成失败", eventId);
            RecordFailure(FeishuMetrics.RedisCommands.MarkCompleted, FeishuRedisFailureKind.Server, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 服务端错误", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "标记事件 {EventId} 为已完成时发生 Redis 错误", eventId);
            RecordFailure(FeishuMetrics.RedisCommands.MarkCompleted, FeishuRedisFailureKind.Server, startTimestamp);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 操作失败", ex);
        }
    }

    /// <summary>
    /// 上报一次非失败结果（success / duplicate / key_missing 等）（R2-21）。
    /// </summary>
    private static void RecordFailureOutcome(string command, string outcome, long startTimestamp)
        => RedisMetricsHelper.Record(command, FeishuMetrics.DedupTypes.Event, outcome, startTimestamp);

    /// <inheritdoc />
    public async Task RollbackProcessingAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(eventId))
            return;

        // ADR-6.3：best-effort API — 失败记日志，不抛
        var startTimestamp = RedisMetricsHelper.Begin();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var redisKey = GetRedisKey(eventId, appKey);

            // ADR-2 / R-06：使用 Lua 脚本原子化，竞态下不会误删 completed
            var result = (long)await _database.ScriptEvaluateAsync(
                RollbackProcessingLuaScript,
                new RedisKey[] { redisKey },
                Array.Empty<RedisValue>()
            );

            if (result == 1)
            {
                _logger?.LogDebug("事件 {EventId} 处理回滚，允许重新处理 (AppKey: {AppKey})", eventId, appKey ?? "default");
            }
            else
            {
                _logger?.LogDebug("事件 {EventId} 状态非 processing，未回滚 (AppKey: {AppKey})", eventId, appKey ?? "default");
            }

            // R2-21：回滚效果可观测（rolled_back=1 / not_processing=0）
            RecordFailureOutcome(
                FeishuMetrics.RedisCommands.RollbackProcessing,
                result == 1 ? FeishuMetrics.RedisOutcomes.Success : FeishuMetrics.RedisOutcomes.KeyMissing,
                startTimestamp);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "回滚事件 {EventId} 处理状态时发生错误（best-effort，不抛出）", eventId);
            RecordFailure(FeishuMetrics.RedisCommands.RollbackProcessing, FeishuRedisFailureKind.Server, startTimestamp);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsProcessedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(eventId))
            return false;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var redisKey = GetRedisKey(eventId, appKey);
            var status = await _database.HashGetAsync(redisKey, StatusField);

            var isProcessed = status == CompletedStatus;
            _logger?.LogDebug("事件 {EventId} 处理状态: {Status} (AppKey: {AppKey})", eventId, isProcessed ? "已处理" : "未处理", appKey ?? "default");
            return isProcessed;
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "Redis 连接异常，检查事件 {EventId} 处理状态失败", eventId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Connection, "Redis 连接失败", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "Redis 超时，检查事件 {EventId} 处理状态失败", eventId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Timeout, "Redis 操作超时", ex);
        }
        catch (RedisServerException ex)
        {
            _logger?.LogError(ex, "Redis 服务端异常，检查事件 {EventId} 处理状态失败", eventId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 服务端错误", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "Redis 操作异常，检查事件 {EventId} 处理状态失败", eventId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 操作失败", ex);
        }
    }

    /// <inheritdoc />
    public async Task<DeduplicationStatus> GetStatusAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(eventId))
            return DeduplicationStatus.Pending;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var redisKey = GetRedisKey(eventId, appKey);
            var entries = await _database.HashGetAllAsync(redisKey, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);

            if (entries.Length == 0)
                return DeduplicationStatus.Pending;

            var statusEntry = entries.FirstOrDefault(x => x.Name == StatusField);
            var timestampEntry = entries.FirstOrDefault(x => x.Name == TimestampField);
            var timeoutEntry = entries.FirstOrDefault(x => x.Name == TimeoutField);
            var status = statusEntry.Value.ToString();

            if (status == CompletedStatus)
                return DeduplicationStatus.Completed;

            if (status == ProcessingStatus)
            {
                var timestampStr = timestampEntry.Value.ToString();
                // R-15：时间戳由 Lua 脚本写入服务端 Unix 秒，此处读取也用服务端对齐的 UTC
                var timestamp = TryParseTimestamp(timestampStr);
                if (timestamp.HasValue)
                {
                    // T-M2-2：优先读取 Hash 中的 timeout 字段（per-call），缺省回落默认值
                    var effectiveTimeout = _defaultProcessingTimeout;
                    if (timeoutEntry.Value.HasValue)
                    {
                        var timeoutStr = timeoutEntry.Value.ToString();
                        if (long.TryParse(timeoutStr, System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out var timeoutSeconds))
                        {
                            effectiveTimeout = TimeSpan.FromSeconds(timeoutSeconds);
                        }
                    }

                    // R2-10：读侧同样以 Redis 服务端时间求差——写侧时间戳由 Lua 的 redis.call('TIME')
                    // 写入，若此处用本进程 DateTimeOffset.UtcNow 比较，客户端时钟偏移会让本方法
                    // 与 Lua 的权威判定不一致（ADR-16）。
                    var serverNowSeconds = await RedisStoreHelper
                        .GetServerTimeSecondsAsync(_database, cancellationToken)
                        .ConfigureAwait(false);
                    var elapsed = TimeSpan.FromSeconds(serverNowSeconds - timestamp.Value.ToUnixTimeSeconds());
                    if (elapsed > effectiveTimeout)
                        return DeduplicationStatus.Pending;
                }
                return DeduplicationStatus.Processing;
            }

            return DeduplicationStatus.Pending;
        }
        catch (RedisConnectionException ex)
        {
            _logger?.LogError(ex, "获取事件 {EventId} 状态时发生连接错误", eventId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Connection, "Redis 连接失败", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger?.LogWarning(ex, "获取事件 {EventId} 状态时超时", eventId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Timeout, "Redis 操作超时", ex);
        }
        catch (RedisServerException ex)
        {
            _logger?.LogError(ex, "获取事件 {EventId} 状态时服务端异常", eventId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 服务端错误", ex);
        }
        catch (RedisException ex)
        {
            _logger?.LogError(ex, "获取事件 {EventId} 状态时发生错误", eventId);
            throw new FeishuRedisException(FeishuRedisFailureKind.Server, "Redis 操作失败", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        // Redis 路径恒返回 0（自动过期），接口为兼容内存实现保留
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

        // 兼容存量数据：ISO 8601 "O" 格式（1.x 版本经 HashSetAsync 写入的历史键）。
        // R2-10：当前所有写入均为 Lua 服务端 Unix 秒，本分支仅用于存量键（TTL ≤ EventCacheExpiration 后自然消失）。
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
    /// <remarks>best-effort：失败记日志并返回 false，不抛出。</remarks>
    public async Task<bool> RemoveAsync(string eventId, string? appKey = null)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(eventId))
            return false;

        try
        {
            var redisKey = GetRedisKey(eventId, appKey);
            var result = await _database.KeyDeleteAsync(redisKey);

            if (result)
                _logger?.LogDebug("已移除事件 {EventId} 的去重标记 (AppKey: {AppKey})", eventId, appKey ?? "default");

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "移除事件 {EventId} 的去重标记时发生错误（best-effort，不抛出）", eventId);
            return false;
        }
    }

    /// <summary>
    /// 批量移除多个事件ID的去重标记
    /// </summary>
    /// <param name="eventIds">事件ID集合</param>
    /// <param name="appKey">应用键，用于多应用隔离（可选）</param>
    /// <returns>成功移除的数量</returns>
    /// <remarks>best-effort：失败记日志并返回 0，不抛出。</remarks>
    public async Task<long> RemoveRangeAsync(IEnumerable<string> eventIds, string? appKey = null)
    {
        ThrowIfDisposed();
        if (eventIds == null)
            return 0;

        try
        {
            var keys = eventIds
                .Where(eid => !string.IsNullOrEmpty(eid))
                .Select(eid => GetRedisKey(eid!, appKey))
                .ToArray();

            if (keys.Length == 0)
                return 0;

            // T-M3-6：分片批量删除（500/批）
            var count = 0L;
            const int batchSize = 500;
            for (int i = 0; i < keys.Length; i += batchSize)
            {
                var batch = keys.Skip(i).Take(batchSize).Select(k => (RedisKey)k).ToArray();
                count += await _database.KeyDeleteAsync(batch);
            }

            _logger?.LogDebug("批量移除了 {Count} 个事件的去重标记 (AppKey: {AppKey})", count, appKey ?? "default");
            return count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "批量移除去重标记时发生错误（best-effort，不抛出）");
            return 0;
        }
    }

    /// <summary>
    /// 获取当前缓存中的事件数量
    /// </summary>
    /// <returns>事件数量</returns>
    /// <remarks>best-effort：失败记日志并返回 0，不抛出。</remarks>
    public async Task<long> GetCachedCountAsync()
    {
        ThrowIfDisposed();
        try
        {
            var totalCount = 0L;
            // R-01 护栏：空前缀在此抛出；R2-02：模式统一经 RedisKeyBuilder.Pattern 产出（与键同源）
            var pattern = RedisKeyBuilder.Pattern(_keyPrefix);

            // R2-12：端点策略统一为 RedisStoreHelper.GetServers（跳过不可达/副本节点，全不可用时回退首节点），
            // 避免"某端点异常 → 外层 catch 吞掉 → 计数整体归 0"的失真。
            foreach (var redisServer in RedisStoreHelper.GetServers(_redis))
            {
                await foreach (var key in redisServer.KeysAsync(pattern: pattern, pageSize: 1000))
                {
                    totalCount++;
                }
            }

            _logger?.LogDebug("当前缓存中的事件数量: {Count}", totalCount);
            // R2-21：SCAN 计数可观测（键空间有界性验证）
            RedisMetricsHelper.RecordScan(FeishuMetrics.RedisCommands.GetCachedCount, totalCount, deleted: false);
            return totalCount;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取缓存事件数量时发生错误（best-effort，不抛出）");
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
        await Task.CompletedTask;
    }

    /// <summary>
    /// 释放检查
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RedisFeishuEventDistributedDeduplicator));
    }

    /// <summary>
    /// 生成 Redis 键（使用 RedisKeyBuilder 统一构造，含转义和护栏）
    /// </summary>
    private string GetRedisKey(string eventId, string? appKey = null)
    {
        if (!string.IsNullOrEmpty(appKey))
        {
            return RedisKeyBuilder.Combine(_keyPrefix, appKey, eventId);
        }
        return RedisKeyBuilder.Combine(_keyPrefix, eventId);
    }
}
