// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using System.Collections.Concurrent;

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
    public Task StoreFailedEventAsync(EventData eventData, Exception exception, CancellationToken cancellationToken = default)
    {
        var failedEvent = new FailedEventInfo
        {
            EventId = eventData.EventId,
            EventType = eventData.EventType,
            SerializedEventData = JsonSerializer.Serialize(eventData),
            ExceptionMessage = exception.Message,
            ExceptionStackTrace = exception.StackTrace ?? string.Empty,
            FailedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        // 如果已存在则更新，否则添加新记录
        _failedEvents.AddOrUpdate(eventData.EventId, failedEvent, (_, _) => failedEvent);

        _logger.LogWarning("后台事件处理失败，事件ID: {EventId}, 事件类型: {EventType}, 错误: {Error}",
            eventData.EventId, eventData.EventType, exception.Message);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<FailedEventInfo>> GetFailedEventsForRetryAsync(int maxRetryCount, CancellationToken cancellationToken = default)
    {
        var failedEvents = _failedEvents.Values
            .Where(e => e.RetryCount < maxRetryCount)
            .OrderBy(e => e.FailedAt)
            .ToList();

        return Task.FromResult<IEnumerable<FailedEventInfo>>(failedEvents);
    }

    /// <inheritdoc />
    public Task<List<FailedEventInfo>> GetPendingRetryEventsAsync(DateTimeOffset beforeTime, int maxCount, CancellationToken cancellationToken = default)
    {
        var failedEvents = _failedEvents.Values
            .Where(e => e.RetryCount < maxCount)
            .OrderBy(e => e.FailedAt)
            .Take(maxCount)
            .ToList();

        return Task.FromResult(failedEvents);
    }

    /// <inheritdoc />
    public Task UpdateRetryCountAsync(string eventId, int retryCount, CancellationToken cancellationToken = default)
    {
        if (_failedEvents.TryGetValue(eventId, out var failedEvent))
        {
            failedEvent.RetryCount = retryCount;
            _logger.LogDebug("更新失败事件重试次数: {EventId}, 重试次数: {RetryCount}", eventId, retryCount);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateFailedEventAsync(FailedEventInfo eventInfo, CancellationToken cancellationToken = default)
    {
        if (_failedEvents.TryGetValue(eventInfo.EventId, out var failedEvent))
        {
            failedEvent.RetryCount = eventInfo.RetryCount;
            failedEvent.ExceptionMessage = eventInfo.ExceptionMessage;
            failedEvent.FailedAt = eventInfo.FailedAt;
            _logger.LogDebug("更新失败事件: {EventId}, 重试次数: {RetryCount}", eventInfo.EventId, eventInfo.RetryCount);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveFailedEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        _failedEvents.TryRemove(eventId, out _);
        _logger.LogDebug("删除成功重试的失败事件记录: {EventId}", eventId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 清理过期的失败事件记录
    /// </summary>
    private void CleanupExpiredEvents(object? state)
    {
        // 使用锁保护清理操作，确保线程安全
        lock (_failedEvents)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-RetentionHours);
            var expiredKeys = _failedEvents.Values
                .Where(e => e.FailedAt < cutoffTime)
                .Select(e => e.EventId)
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
                var oldestEvents = _failedEvents.Values
                    .OrderBy(e => e.FailedAt)
                    .Take(toRemoveCount)
                    .Select(e => e.EventId)
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
