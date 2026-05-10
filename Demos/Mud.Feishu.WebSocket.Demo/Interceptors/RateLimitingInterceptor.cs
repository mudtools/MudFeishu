// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Interceptors;

namespace Mud.Feishu.WebSocket.Demo.Interceptors;

/// <summary>
/// 限流拦截器 - 对同一类型的事件进行频率限制
/// </summary>
public class RateLimitingInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<RateLimitingInterceptor> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _lastProcessedTimes = new();
    private readonly TimeSpan _minInterval;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="minIntervalMs">最小处理间隔（毫秒），默认 100ms</param>
    public RateLimitingInterceptor(ILogger<RateLimitingInterceptor> logger, int minIntervalMs = 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _minInterval = TimeSpan.FromMilliseconds(minIntervalMs);
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var key = $"{eventData.TenantKey}:{eventType}";

        if (_lastProcessedTimes.TryGetValue(key, out var lastProcessed))
        {
            var elapsed = DateTime.UtcNow - lastProcessed;
            if (elapsed < _minInterval)
            {
                _logger.LogWarning(
                    "[限流] 事件处理过快，已限流: EventType={EventType}, EventId={EventId}, TenantKey={TenantKey}, ElapsedMs={ElapsedMs}, MinIntervalMs={MinIntervalMs}",
                    eventType, eventData.EventId, eventData.TenantKey,
                    elapsed.TotalMilliseconds, _minInterval.TotalMilliseconds);

                return Task.FromResult(false); // 返回 false 中断处理
            }
        }

        _lastProcessedTimes.AddOrUpdate(key, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
