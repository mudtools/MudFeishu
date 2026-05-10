// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 失败事件记录
/// </summary>
public class FailedEventRecord
{
    /// <summary>
    /// 事件 ID
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// 事件类型
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 事件数据（JSON）
    /// </summary>
    public string EventDataJson { get; set; } = string.Empty;

    /// <summary>
    /// 失败原因
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// 首次失败时间
    /// </summary>
    public DateTimeOffset FirstFailureTime { get; set; }

    /// <summary>
    /// 最后失败时间
    /// </summary>
    public DateTimeOffset LastFailureTime { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 下次重试时间
    /// </summary>
    public DateTimeOffset? NextRetryTime { get; set; }
}

/// <summary>
/// 失败事件重试服务
/// 后台服务，定期重试失败的事件
/// </summary>
public class FailedEventRetryService : BackgroundService
{
    private readonly FailedEventRetryOptions _options;
    private readonly ILogger<FailedEventRetryService> _logger;
    private readonly IFeishuWebhookService _webhookService;
    private readonly IFailedEventStore? _failedEventStore;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FailedEventRetryService(
        IOptions<FailedEventRetryOptions> options,
        ILogger<FailedEventRetryService> logger,
        IFeishuWebhookService webhookService,
        IFailedEventStore? failedEventStore = null)
    {
        _options = options.Value;
        _logger = logger;
        _webhookService = webhookService;
        _failedEventStore = failedEventStore;

        if (_options.EnableRetry && _failedEventStore == null)
        {
            _logger.LogWarning("已启用失败事件重试，但未配置失败事件存储(IFailedEventStore)，重试服务将无法工作");
        }
    }

    /// <summary>
    /// 后台服务执行方法
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableRetry)
        {
            _logger.LogInformation("失败事件重试服务未启用");
            return;
        }

        if (_failedEventStore == null)
        {
            _logger.LogWarning("未配置失败事件存储(IFailedEventStore)，失败事件重试服务无法启动");
            return;
        }

        _logger.LogInformation("失败事件重试服务已启动，轮询间隔: {Interval} 秒", _options.RetryPollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RetryFailedEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重试失败事件时发生错误");
            }

            // 等待下次轮询
            await Task.Delay(TimeSpan.FromSeconds(_options.RetryPollIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("失败事件重试服务已停止");
    }

    /// <summary>
    /// 重试失败的事件
    /// </summary>
    private async Task RetryFailedEventsAsync(CancellationToken cancellationToken)
    {
        var failedEvents = await _failedEventStore!.GetPendingRetryEventsAsync(
            DateTimeOffset.UtcNow,
            _options.MaxRetryPerPoll,
            cancellationToken);

        if (failedEvents.Count == 0)
        {
            return;
        }

        _logger.LogInformation("找到 {Count} 个待重试的失败事件", failedEvents.Count);

        foreach (var failedEvent in failedEvents)
        {
            try
            {
                // 检查是否应该重试
                if (!ShouldRetry(failedEvent))
                {
                    _logger.LogInformation("事件 {EventId} 已达到最大重试次数，放弃重试", failedEvent.EventId);
                    await _failedEventStore.RemoveFailedEventAsync(failedEvent.EventId, cancellationToken);
                    continue;
                }

                // 反序列化事件数据
                var eventData = JsonSerializer.Deserialize<EventData>(failedEvent.SerializedEventData);
                if (eventData == null)
                {
                    _logger.LogError("无法反序列化事件 {EventId} 的数据，放弃重试", failedEvent.EventId);
                    await _failedEventStore.RemoveFailedEventAsync(failedEvent.EventId, cancellationToken);
                    continue;
                }

                _logger.LogInformation("开始重试事件 {EventId}，当前重试次数: {RetryCount}/{MaxRetry}",
                    failedEvent.EventId, failedEvent.RetryCount, _options.MaxRetryCount);

                // 尝试重新处理
                var result = await _webhookService.HandleEventAsync(eventData, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("事件 {EventId} 重试成功", failedEvent.EventId);
                    await _failedEventStore.RemoveFailedEventAsync(failedEvent.EventId, cancellationToken);
                }
                else
                {
                    // 重试失败，更新重试次数
                    failedEvent.RetryCount++;
                    failedEvent.FailedAt = DateTime.UtcNow;
                    failedEvent.ExceptionMessage = result.ErrorReason ?? "重试失败";

                    await _failedEventStore.UpdateFailedEventAsync(failedEvent, cancellationToken);

                    _logger.LogWarning("事件 {EventId} 重试失败，原因: {Reason}",
                        failedEvent.EventId, result.ErrorReason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重试事件 {EventId} 时发生异常", failedEvent.EventId);

                // 异常情况，更新重试次数
                failedEvent.RetryCount++;
                failedEvent.FailedAt = DateTime.UtcNow;
                failedEvent.ExceptionMessage = ex.Message;

                await _failedEventStore!.UpdateFailedEventAsync(failedEvent, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 判断是否应该重试
    /// </summary>
    private bool ShouldRetry(Mud.Feishu.Abstractions.FailedEventInfo failedEvent)
    {
        return failedEvent.RetryCount < _options.MaxRetryCount;
    }
}
