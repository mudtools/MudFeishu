// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Utilities;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 内存实现的失败事件存储
/// 适用于开发环境和单实例部署
/// </summary>
/// <remarks>
/// 生产环境建议使用基于 Redis、数据库等持久化存储的实现
/// </remarks>
public class InMemoryFailedEventStore : IFailedEventStore, IDisposable
{
    private readonly ConcurrentDictionary<string, FailedEventInfo> _failedEvents = new();
    private readonly ILogger<InMemoryFailedEventStore> _logger;
    private readonly Timer _cleanupTimer;
    /// <summary>容量淘汰与定时清理共用的锁；日常读写依赖 ConcurrentDictionary 自身原子性。</summary>
    private readonly object _evictionLock = new();

    /// <summary>
    /// 最大存储的失败事件数量
    /// </summary>
    private const int MaxStoredEvents = 1000;

    /// <summary>
    /// 失败事件保留时间（小时）
    /// </summary>
    private const int RetentionHours = 24;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException">当 logger 为 null 时抛出</exception>
    public InMemoryFailedEventStore(ILogger<InMemoryFailedEventStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 每小时清理一次过期记录
        _cleanupTimer = new Timer(
            CleanupExpiredEvents,
            null,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));
    }

    /// <inheritdoc />
#if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode")]
#endif
    public Task StoreFailedEventAsync(EventData eventData, Exception exception, CancellationToken cancellationToken = default)
    {
        return StoreFailedEventAsync(eventData, exception, null, DateTimeOffset.UtcNow, cancellationToken);
    }

    /// <inheritdoc />
    public Task StoreFailedEventAsync(EventData eventData, Exception exception, string? appKey, DateTimeOffset nextRetryAt, CancellationToken cancellationToken = default)
    {
        // P2-9：空 EventId 用生成键兜底，避免多条空 ID 事件相互覆盖
        var storeKey = string.IsNullOrEmpty(eventData.EventId)
            ? $"no-event-id:{Guid.NewGuid():N}"
            : eventData.EventId;

        string? serializedHeader = null;
        if (eventData.Header != null)
        {
            try
            {
                serializedHeader = FeishuJsonAot.Serialize(eventData.Header, FeishuJsonDefaults.SerializerOptions);
            }
            catch (Exception serEx)
            {
                _logger.LogWarning(serEx, "序列化事件 Header 失败，EventId: {EventId}", eventData.EventId);
            }
        }

        var failedEvent = new FailedEventInfo
        {
            EventId = eventData.EventId,
            EventType = eventData.EventType,
            SerializedEventData = FeishuJsonAot.Serialize(eventData, FeishuJsonDefaults.SerializerOptions),
            SerializedHeader = serializedHeader,
            StoreKey = storeKey,
            ExceptionMessage = exception.Message,
            ExceptionStackTrace = exception.StackTrace ?? string.Empty,
            FailedAt = DateTime.UtcNow,
            RetryCount = 0,
            AppKey = appKey,
            NextRetryAt = nextRetryAt
        };

        // WHF-R2/B4：update 工厂保留存量 RetryCount——覆盖写会丢失已累积的重试计数，
        // 多实例/并发场景下重试次数可能超 MaxRetryCount
        _failedEvents.AddOrUpdate(storeKey, failedEvent,
            (_, existing) => { failedEvent.RetryCount = existing.RetryCount; return failedEvent; });

        // 容量上限检查
        if (_failedEvents.Count > MaxStoredEvents)
        {
            lock (_evictionLock)
            {
                if (_failedEvents.Count > MaxStoredEvents)
                {
                    var toRemoveCount = _failedEvents.Count - MaxStoredEvents;
                    var oldestEvents = _failedEvents.Values
                        .OrderBy(e => e.FailedAt)
                        .Take(toRemoveCount)
                        .Select(e => e.StoreKey ?? e.EventId)
                        .ToList();

                    foreach (var key in oldestEvents)
                    {
                        _failedEvents.TryRemove(key, out _);
                    }

                    _logger.LogWarning("失败事件存储已满，淘汰了 {Count} 个最旧事件", toRemoveCount);
                }
            }
        }

        _logger.LogWarning("后台事件处理失败，事件ID: {EventId}, 事件类型: {EventType}, AppKey: {AppKey}, 错误: {Error}",
            eventData.EventId, eventData.EventType, appKey ?? "null", exception.Message);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// WHF-15：返回 <see cref="FailedEventInfo"/> 的<b>深拷贝快照</b>——调用方对返回对象的修改
    /// 不会影响存储内的条目（须通过 <see cref="UpdateFailedEventAsync"/> 按 EventId 写回）；
    /// 多次 Get 之间互不可见。
    /// </remarks>
    public Task<IEnumerable<FailedEventInfo>> GetFailedEventsForRetryAsync(int maxRetryCount, CancellationToken cancellationToken = default)
    {
        var failedEvents = _failedEvents.Values
            .Where(e => e.RetryCount < maxRetryCount)
            .OrderBy(e => e.FailedAt)
            .Select(CloneSnapshot)
            .ToList();

        return Task.FromResult<IEnumerable<FailedEventInfo>>(failedEvents);
    }

    /// <inheritdoc />
    /// <remarks>WHF-15：返回深拷贝快照，语义见 <see cref="GetFailedEventsForRetryAsync"/>。</remarks>
    public Task<List<FailedEventInfo>> GetPendingRetryEventsAsync(DateTimeOffset beforeTime, int maxCount, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var failedEvents = _failedEvents.Values
            .Where(e => e.NextRetryAt <= now)   // 仅取退避到期的
            .OrderBy(e => e.NextRetryAt)
            .Take(maxCount)                     // maxCount 语义 = 本次最多取几条
            .Select(CloneSnapshot)
            .ToList();

        return Task.FromResult(failedEvents);
    }

    /// <inheritdoc />
    public Task UpdateRetryCountAsync(string eventId, int retryCount, CancellationToken cancellationToken = default)
    {
        if (TryGetEntry(eventId, out var failedEvent))
        {
            failedEvent!.RetryCount = retryCount;
            _logger.LogDebug("更新失败事件重试次数: {EventId}, 重试次数: {RetryCount}", eventId, retryCount);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateFailedEventAsync(FailedEventInfo eventInfo, CancellationToken cancellationToken = default)
    {
        var lookup = eventInfo.StoreKey ?? eventInfo.EventId;
        if (TryGetEntry(lookup, out var failedEvent))
        {
            failedEvent!.RetryCount = eventInfo.RetryCount;
            failedEvent.ExceptionMessage = eventInfo.ExceptionMessage;
            failedEvent.FailedAt = eventInfo.FailedAt;
            failedEvent.NextRetryAt = eventInfo.NextRetryAt;
            if (!string.IsNullOrEmpty(eventInfo.AppKey))
                failedEvent.AppKey = eventInfo.AppKey;
            _logger.LogDebug("更新失败事件: {EventId}, 重试次数: {RetryCount}, 下次重试: {NextRetryAt}", eventInfo.EventId, eventInfo.RetryCount, eventInfo.NextRetryAt);
        }
        else
        {
            // WHF-R2/B4：条目已被淘汰（容量上限清理或过期清理），重试状态更新丢失
            _logger.LogWarning("失败事件 {EventId} 已被淘汰，重试状态更新丢失", eventInfo.EventId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveFailedEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        _failedEvents.TryRemove(eventId, out _);
        // 空 EventId 场景：StoreKey 与 EventId 不一致时，按 EventId 再扫一次
        if (!string.IsNullOrEmpty(eventId))
        {
            var orphan = _failedEvents.FirstOrDefault(kv => kv.Value.EventId == eventId && kv.Key != eventId);
            if (orphan.Key != null)
                _failedEvents.TryRemove(orphan.Key, out _);
        }
        _logger.LogDebug("删除成功重试的失败事件记录: {EventId}", eventId);
        return Task.CompletedTask;
    }

    private bool TryGetEntry(string? key, out FailedEventInfo? info)
    {
        info = null;
        if (string.IsNullOrEmpty(key))
            return false;
        if (_failedEvents.TryGetValue(key!, out var direct))
        {
            info = direct;
            return true;
        }
        var match = _failedEvents.Values.FirstOrDefault(e => e.EventId == key || e.StoreKey == key);
        if (match != null)
        {
            info = match;
            return true;
        }
        return false;
    }

    /// <summary>
    /// WHF-15：构造 <see cref="FailedEventInfo"/> 深拷贝快照（消除共享可变引用）
    /// </summary>
    private static FailedEventInfo CloneSnapshot(FailedEventInfo source) => new()
    {
        EventId = source.EventId,
        EventType = source.EventType,
        SerializedEventData = source.SerializedEventData,
        SerializedHeader = source.SerializedHeader,
        StoreKey = source.StoreKey,
        ExceptionMessage = source.ExceptionMessage,
        ExceptionStackTrace = source.ExceptionStackTrace,
        AppKey = source.AppKey,
        NextRetryAt = source.NextRetryAt,
        FailedAt = source.FailedAt,
        RetryCount = source.RetryCount
    };

    /// <summary>
    /// 清理过期的失败事件记录
    /// </summary>
    private void CleanupExpiredEvents(object? state)
    {
        lock (_evictionLock)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-RetentionHours);
            var expiredKeys = _failedEvents
                .Where(kv => kv.Value.FailedAt < cutoffTime)
                .Select(kv => kv.Key)
                .ToList();

            var removedCount = 0;
            foreach (var key in expiredKeys)
            {
                if (_failedEvents.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }

            // 限制存储数量
            if (_failedEvents.Count > MaxStoredEvents)
            {
                var toRemoveCount = _failedEvents.Count - MaxStoredEvents;
                var oldestEvents = _failedEvents
                    .OrderBy(kv => kv.Value.FailedAt)
                    .Take(toRemoveCount)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var key in oldestEvents)
                {
                    if (_failedEvents.TryRemove(key, out _))
                    {
                        removedCount++;
                    }
                }
            }

            if (removedCount > 0)
            {
                _logger.LogInformation("清理了 {Count} 个过期的失败事件记录", removedCount);
            }
        }
    }

    /// <summary>
    /// 获取当前存储的失败事件数量
    /// </summary>
    public int GetFailedEventCount() => _failedEvents.Count;

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}
