// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Interceptors;

namespace Mud.Feishu.WebSocket.Demo.Interceptors;

/// <summary>
/// WebSocket 遥测拦截器 - 使用 OpenTelemetry 收集事件处理指标
/// </summary>
public class WebSocketTelemetryInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<WebSocketTelemetryInterceptor> _logger;
    private readonly ActivitySource _activitySource;
    private readonly ConcurrentDictionary<string, Activity> _activities = new();
    private int _totalEvents;
    private int _failedEvents;

    public WebSocketTelemetryInterceptor(ILogger<WebSocketTelemetryInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = new ActivitySource("Mud.Feishu.WebSocket.Demo");
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var activity = _activitySource.StartActivity($"WebSocketEvent_{eventType}");
        if (activity != null)
        {
            activity.SetTag("component", "Mud.Feishu.WebSocket.Demo");
            activity.SetTag("event.type", eventType);
            activity.SetTag("event.id", eventData.EventId);
            activity.SetTag("event.tenant_key", eventData.TenantKey);
        }

        var eventId = eventData.EventId ?? Guid.NewGuid().ToString();
        _activities.TryAdd(eventId, activity);

        Interlocked.Increment(ref _totalEvents);
        _logger.LogDebug("[遥测] 开始处理 WebSocket 事件: EventType={EventType}, EventId={EventId}",
            eventType, eventData.EventId);

        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        var eventId = eventData.EventId ?? string.Empty;

        if (_activities.TryRemove(eventId, out var activity))
        {
            if (exception != null)
            {
                Interlocked.Increment(ref _failedEvents);
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", exception.Message);
                activity?.SetTag("error.type", exception.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }

            activity?.Dispose();
        }

        _logger.LogDebug("[遥测] 完成 WebSocket 事件处理: EventType={EventType}, EventId={EventId}, Success={Success}",
            eventType, eventData.EventId, exception == null);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public (int TotalEvents, int FailedEvents, double SuccessRate) GetStatistics()
    {
        var total = Interlocked.CompareExchange(ref _totalEvents, 0, 0);
        var failed = Interlocked.CompareExchange(ref _failedEvents, 0, 0);
        var successRate = total > 0 ? (double)(total - failed) / total * 100 : 100;
        return (total, failed, successRate);
    }
}
