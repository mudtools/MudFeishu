// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.Webhook.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 失败事件重试服务
/// 后台服务，定期重试失败的事件
/// </summary>
public class FailedEventRetryService : BackgroundService
{
    private readonly FailedEventRetryOptions _options;
    private readonly IOptionsMonitor<FeishuWebhookOptions>? _webhookOptions;
    private readonly ILogger<FailedEventRetryService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFailedEventStore? _failedEventStore;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FailedEventRetryService(
        IOptions<FailedEventRetryOptions> options,
        ILogger<FailedEventRetryService> logger,
        IServiceScopeFactory scopeFactory,
        IFailedEventStore? failedEventStore = null,
        IOptionsMonitor<FeishuWebhookOptions>? webhookOptions = null)
    {
        _options = options.Value;
        _webhookOptions = webhookOptions;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _failedEventStore = failedEventStore;

        if (_options.EnableRetry && _failedEventStore == null)
        {
            _logger.LogWarning("已启用失败事件重试，但未配置失败事件存储(IFailedEventStore)，重试服务将无法工作");
        }
    }

    /// <summary>
    /// 后台服务执行方法
    /// </summary>
#if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode")]
#endif
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_failedEventStore == null)
        {
            _logger.LogWarning("未配置失败事件存储(IFailedEventStore)，失败事件重试服务无法启动");
            return;
        }

        // 内存存储警告
        if (_failedEventStore is InMemoryFailedEventStore)
        {
            _logger.LogWarning("失败事件重试使用内存存储(InMemoryFailedEventStore)，进程崩溃将导致待重试事件丢失。生产环境建议使用 Redis 实现。");
        }

        _logger.LogInformation("失败事件重试服务已启动，轮询间隔: {Interval} 秒", _options.RetryPollIntervalSeconds);

        // WHF-14：每轮循环重新读取 EnableRetry（支持配置热更新——禁用态进入轻量轮询，启用即恢复工作），
        // 不再在启动期一次性判定后永久退出
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var enableRetry = _webhookOptions?.CurrentValue.Retry.EnableRetry ?? _options.EnableRetry;
                if (enableRetry)
                {
                    await RetryFailedEventsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // WHF-14：RetryFailedEventsAsync 内部转发外部 token 时，关停引发的 OCE 不应视为处理失败
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重试失败事件时发生错误");
            }

            // WHF-14：Task.Delay 在已取消 token 上会以 Canceled 态完成；await 会在调度时机抛 OCE。
            // 直接 await 在 net8.0 的 BackgroundService 调度下存在 OCE 经 Status 而非 catch 路径逃逸的窗口
            // （net10 的 StartAsync 改为 Task.Run，行为不同）。改为：
            // ① 若 token 已取消，不进入 Delay，直接退出循环；
            // ② 若未取消，Delay 正常 await；其 OCE 仅来自关停取消，无条件捕获后退出。
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.RetryPollIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("失败事件重试服务已停止");
    }

    /// <summary>
    /// 计算第 retryCount 次重试的退避延迟（已封顶，杜绝溢出）
    /// </summary>
    /// <param name="retryCount">当前重试次数</param>
    /// <returns>退避延迟</returns>
    internal TimeSpan CalculateBackoff(int retryCount)
    {
        var raw = _options.InitialRetryDelaySeconds * Math.Pow(_options.RetryDelayMultiplier, retryCount);
        var capped = Math.Min(raw, _options.MaxRetryDelaySeconds);
        if (double.IsNaN(capped) || capped < 0) capped = _options.MaxRetryDelaySeconds;
        return TimeSpan.FromSeconds(capped);
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
            using var scope = _scopeFactory.CreateScope();
            var webhookService = scope.ServiceProvider.GetRequiredService<IFeishuWebhookService>();

            try
            {
                // 检查是否应该重试
                if (!ShouldRetry(failedEvent))
                {
                    _logger.LogInformation("事件 {EventId} 已达到最大重试次数 {MaxRetry}，放弃重试", failedEvent.EventId, _options.MaxRetryCount);
                    await _failedEventStore.RemoveFailedEventAsync(failedEvent.EventId, cancellationToken);
                    continue;
                }

                // 关键：恢复该事件所属应用上下文，保证应用专属处理器 / 去重键 / Nonce 隔离一致
                if (!string.IsNullOrEmpty(failedEvent.AppKey))
                    webhookService.SetCurrentAppKey(failedEvent.AppKey!);

                // 反序列化事件数据
                var eventData = FeishuJsonAot.Deserialize<EventData>(
                    failedEvent.SerializedEventData, FeishuJsonDefaults.DeserializerOptions);
                if (eventData == null)
                {
                    _logger.LogError("无法反序列化事件 {EventId} 的数据，放弃重试", failedEvent.EventId);
                    await _failedEventStore.RemoveFailedEventAsync(failedEvent.EventId, cancellationToken);
                    continue;
                }

                _logger.LogInformation("开始重试事件 {EventId}，当前重试次数: {RetryCount}/{MaxRetry}，AppKey: {AppKey}",
                    failedEvent.EventId, failedEvent.RetryCount, _options.MaxRetryCount, failedEvent.AppKey ?? "null");

                // 尝试重新处理
                var result = await webhookService.HandleEventAsync(eventData, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("事件 {EventId} 重试成功", failedEvent.EventId);
                    await _failedEventStore.RemoveFailedEventAsync(failedEvent.EventId, cancellationToken);
                }
                else
                {
                    failedEvent.RetryCount++;
                    failedEvent.FailedAt = DateTime.UtcNow;
                    failedEvent.NextRetryAt = DateTimeOffset.UtcNow + CalculateBackoff(failedEvent.RetryCount);
                    failedEvent.ExceptionMessage = result.ErrorReason ?? "重试失败";

                    await _failedEventStore.UpdateFailedEventAsync(failedEvent, cancellationToken);

                    _logger.LogWarning("事件 {EventId} 重试失败，原因: {Reason}，下次重试: {NextRetryAt}",
                        failedEvent.EventId, result.ErrorReason, failedEvent.NextRetryAt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重试事件 {EventId} 时发生异常", failedEvent.EventId);

                failedEvent.RetryCount++;
                failedEvent.FailedAt = DateTime.UtcNow;
                failedEvent.NextRetryAt = DateTimeOffset.UtcNow + CalculateBackoff(failedEvent.RetryCount);
                failedEvent.ExceptionMessage = ex.Message;

                await _failedEventStore!.UpdateFailedEventAsync(failedEvent, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 判断是否应该重试
    /// </summary>
    private bool ShouldRetry(FailedEventInfo failedEvent)
    {
        return failedEvent.RetryCount < _options.MaxRetryCount;
    }
}
