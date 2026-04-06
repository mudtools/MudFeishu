// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 降级告警服务实现
/// 在 Redis 失败时发送告警通知
/// </summary>
public class FallbackAlertService : IFallbackAlertService
{
    private readonly ILogger<FallbackAlertService>? _logger;
    private readonly List<IFallbackAlertHandler> _handlers;
    private readonly object _handlersLock = new();

    /// <summary>
    /// 告警事件
    /// </summary>
    public event EventHandler<FallbackAlertEventArgs>? AlertRaised;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="handlers">告警处理器列表（可选）</param>
    public FallbackAlertService(
        ILogger<FallbackAlertService>? logger = null,
        IEnumerable<IFallbackAlertHandler>? handlers = null)
    {
        _logger = logger;
        _handlers = handlers?.ToList() ?? new List<IFallbackAlertHandler>();
    }

    /// <inheritdoc />
    public async Task RaiseAlertAsync(
        FallbackAlertType alertType,
        string message,
        Exception? exception = null,
        Dictionary<string, object?>? additionalData = null)
    {
        var eventArgs = new FallbackAlertEventArgs
        {
            AlertType = alertType,
            Message = message,
            Exception = exception,
            AdditionalData = additionalData ?? new Dictionary<string, object?>()
        };

        _logger?.LogWarning("降级告警: {AlertType} - {Message}", alertType, message);

        AlertRaised?.Invoke(this, eventArgs);

        List<IFallbackAlertHandler> handlersCopy;
        lock (_handlersLock)
        {
            handlersCopy = new List<IFallbackAlertHandler>(_handlers);
        }

        foreach (var handler in handlersCopy)
        {
            try
            {
                await handler.HandleAlertAsync(eventArgs);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "告警处理器 {HandlerType} 执行失败", handler.GetType().Name);
            }
        }
    }

    /// <summary>
    /// 注册告警处理器
    /// </summary>
    /// <param name="handler">告警处理器</param>
    public void RegisterHandler(IFallbackAlertHandler handler)
    {
        lock (_handlersLock)
        {
            _handlers.Add(handler);
        }
    }

    /// <summary>
    /// 移除告警处理器
    /// </summary>
    /// <param name="handler">告警处理器</param>
    public void RemoveHandler(IFallbackAlertHandler handler)
    {
        lock (_handlersLock)
        {
            _handlers.Remove(handler);
        }
    }
}

/// <summary>
/// 告警处理器接口
/// </summary>
public interface IFallbackAlertHandler
{
    /// <summary>
    /// 处理告警
    /// </summary>
    /// <param name="eventArgs">告警事件参数</param>
    Task HandleAlertAsync(FallbackAlertEventArgs eventArgs);
}

/// <summary>
/// 日志告警处理器
/// 将告警输出到日志
/// </summary>
public class LoggingAlertHandler : IFallbackAlertHandler
{
    private readonly ILogger<LoggingAlertHandler>? _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器（可选）</param>
    public LoggingAlertHandler(ILogger<LoggingAlertHandler>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task HandleAlertAsync(FallbackAlertEventArgs eventArgs)
    {
        var logLevel = eventArgs.AlertType switch
        {
            FallbackAlertType.RedisRecovered => LogLevel.Information,
            FallbackAlertType.ProcessingTimeoutRecovery => LogLevel.Warning,
            _ => LogLevel.Error
        };

        _logger?.Log(logLevel, 
            "[降级告警] {AlertType}: {Message}, 连续失败: {Failures}, 已降级: {IsFallback}, 异常: {Exception}",
            eventArgs.AlertType,
            eventArgs.Message,
            eventArgs.ConsecutiveFailures,
            eventArgs.IsFallbackActive,
            eventArgs.Exception?.Message ?? "无");

        return Task.CompletedTask;
    }
}

/// <summary>
/// 阈值告警处理器
/// 当连续失败次数超过阈值时触发
/// </summary>
public class ThresholdAlertHandler : IFallbackAlertHandler
{
    private readonly int _threshold;
    private readonly Func<FallbackAlertEventArgs, Task> _onThresholdReached;
    private readonly ILogger<ThresholdAlertHandler>? _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="threshold">连续失败阈值</param>
    /// <param name="onThresholdReached">阈值达到时的回调</param>
    /// <param name="logger">日志记录器（可选）</param>
    public ThresholdAlertHandler(
        int threshold,
        Func<FallbackAlertEventArgs, Task> onThresholdReached,
        ILogger<ThresholdAlertHandler>? logger = null)
    {
        _threshold = threshold;
        _onThresholdReached = onThresholdReached ?? throw new ArgumentNullException(nameof(onThresholdReached));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAlertAsync(FallbackAlertEventArgs eventArgs)
    {
        if (eventArgs.ConsecutiveFailures >= _threshold)
        {
            _logger?.LogWarning("连续失败次数 {Failures} 达到阈值 {Threshold}", eventArgs.ConsecutiveFailures, _threshold);
            await _onThresholdReached(eventArgs);
        }
    }
}
