// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Interceptors;

namespace TaskManageDemo.Backend.Interceptors;

/// <summary>
/// 性能监控拦截器 - 记录事件处理耗时
/// </summary>
public class PerformanceMonitoringInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<PerformanceMonitoringInterceptor> _logger;
    private readonly ConcurrentDictionary<string, Stopwatch> _stopwatches = new();

    public PerformanceMonitoringInterceptor(ILogger<PerformanceMonitoringInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var eventId = eventData.EventId ?? Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        _stopwatches.TryAdd(eventId, stopwatch);

        _logger.LogDebug("[性能监控] 开始处理事件: EventType={EventType}, EventId={EventId}",
            eventType, eventData.EventId);

        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        var eventId = eventData.EventId ?? Guid.NewGuid().ToString();

        if (_stopwatches.TryRemove(eventId, out var stopwatch))
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (exception != null)
            {
                _logger.LogWarning(exception,
                    "[性能监控] 事件处理失败: EventType={EventType}, EventId={EventId}, ElapsedMs={ElapsedMs}",
                    eventType, eventData.EventId, elapsedMs);
            }
            else
            {
                var logLevel = elapsedMs > 1000 ? LogLevel.Warning : LogLevel.Information;
                _logger.Log(logLevel,
                    "[性能监控] 事件处理完成: EventType={EventType}, EventId={EventId}, ElapsedMs={ElapsedMs}",
                    eventType, eventData.EventId, elapsedMs);
            }
        }

        return Task.CompletedTask;
    }
}
