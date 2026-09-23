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
    private readonly IOptionsMonitor<FeishuWebhookOptions> _webhookOptions;
    private readonly ILogger<FailedEventRetryService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFailedEventStore? _failedEventStore;
    private readonly IWebhookAppKeyAccessor? _appKeyAccessor;

    /// <summary>
    /// 失败事件重试配置（唯一真相源）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>R5/X3 修复</b>：此前本服务注入 <c>IOptions&lt;FailedEventRetryOptions&gt;</c>，而
    /// <c>FeishuWebhookServiceBuilder</c> **从未** <c>Configure/AddOptions&lt;FailedEventRetryOptions&gt;</c>
    /// —— <c>IOptions&lt;T&gt;</c> 的开放泛型注册使其解析为一个全默认实例而不报错。结果是
    /// <c>FeishuWebhook:Retry</c> 整节（经 Builder 绑定到 <see cref="FeishuWebhookOptions.Retry"/>）
    /// 的 6 个字段（<c>MaxRetryCount</c> / <c>InitialRetryDelaySeconds</c> / <c>RetryDelayMultiplier</c> /
    /// <c>MaxRetryDelaySeconds</c> / <c>RetryPollIntervalSeconds</c> / <c>MaxRetryPerPoll</c>）
    /// <b>恒为默认</b>，只有 <c>EnableRetry</c> 因旧代码的兜底分支偶然生效。
    /// </para>
    /// <para>
    /// 改为直接读 <see cref="FeishuWebhookOptions.Retry"/> 后，写入侧
    /// （<c>FeishuWebhookService.HandleEventAsync</c> 写 <c>NextRetryAt</c>）与轮询侧**同源**，
    /// 且保留 <c>IOptionsMonitor</c> 的热更语义（WHF-14）。
    /// </para>
    /// </remarks>
    private FailedEventRetryOptions Retry => _webhookOptions.CurrentValue.Retry ?? new FailedEventRetryOptions();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="webhookOptions">
    /// Webhook 配置（含 <see cref="FeishuWebhookOptions.Retry"/>）。由
    /// <c>FeishuWebhookServiceBuilder.RegisterOptions()</c> 注册，与本服务同一 DI 容器，必然可解析。
    /// </param>
    /// <param name="logger">日志。</param>
    /// <param name="scopeFactory">作用域工厂（每个待重试事件创建一个作用域）。</param>
    /// <param name="failedEventStore">失败事件存储；未配置时重试服务不启动。</param>
    /// <param name="appKeyAccessor">应用键上下文访问器（可选，用于每轮结束时显式清除 AppKey）。</param>
    public FailedEventRetryService(
        IOptionsMonitor<FeishuWebhookOptions> webhookOptions,
        ILogger<FailedEventRetryService> logger,
        IServiceScopeFactory scopeFactory,
        IFailedEventStore? failedEventStore = null,
        IWebhookAppKeyAccessor? appKeyAccessor = null)
    {
        _webhookOptions = webhookOptions ?? throw new ArgumentNullException(nameof(webhookOptions));
        _logger = logger;
        _scopeFactory = scopeFactory;
        _failedEventStore = failedEventStore;
        _appKeyAccessor = appKeyAccessor;

        if (Retry.EnableRetry && _failedEventStore == null)
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

        _logger.LogInformation("失败事件重试服务已启动，轮询间隔: {Interval} 秒", Retry.RetryPollIntervalSeconds);

        // WHF-14：每轮循环重新读取 EnableRetry（支持配置热更新——禁用态进入轻量轮询，启用即恢复工作），
        // 不再在启动期一次性判定后永久退出
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (Retry.EnableRetry)
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
                await Task.Delay(TimeSpan.FromSeconds(Retry.RetryPollIntervalSeconds), stoppingToken);
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
        // R5/X3：先取快照再计算，避免同一次计算中途发生配置热更导致三项参数来自不同版本。
        var retry = Retry;
        var raw = retry.InitialRetryDelaySeconds * Math.Pow(retry.RetryDelayMultiplier, retryCount);
        var capped = Math.Min(raw, retry.MaxRetryDelaySeconds);
        if (double.IsNaN(capped) || capped < 0) capped = retry.MaxRetryDelaySeconds;
        return TimeSpan.FromSeconds(capped);
    }

    /// <summary>
    /// 重试失败的事件
    /// </summary>
    private async Task RetryFailedEventsAsync(CancellationToken cancellationToken)
    {
        var failedEvents = await _failedEventStore!.GetPendingRetryEventsAsync(
            DateTimeOffset.UtcNow,
            Retry.MaxRetryPerPoll,
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
                    _logger.LogInformation("事件 {EventId} 已达到最大重试次数 {MaxRetry}，放弃重试", failedEvent.EventId, Retry.MaxRetryCount);
                    await _failedEventStore.RemoveFailedEventAsync(failedEvent.StoreKey ?? failedEvent.EventId, cancellationToken);
                    continue;
                }

                // 关键：恢复该事件所属应用上下文，保证应用专属处理器 / 去重键 / Nonce 隔离一致
                // WHF-R2/B5：无条件设置（空 AppKey 显式清空），否则 AsyncLocal 继承上一迭代
                webhookService.SetCurrentAppKey(failedEvent.AppKey ?? string.Empty);

                // 反序列化事件数据
                var eventData = FeishuJsonAot.Deserialize<EventData>(
                    failedEvent.SerializedEventData, FeishuJsonDefaults.DeserializerOptions);
                if (eventData == null)
                {
                    _logger.LogError("无法反序列化事件 {EventId} 的数据，放弃重试", failedEvent.EventId);
                    await _failedEventStore.RemoveFailedEventAsync(failedEvent.StoreKey ?? failedEvent.EventId, cancellationToken);
                    continue;
                }

                // P2-8：补回失败时序列化的 Header（v2.0 schema/app_id 等），反序列化失败仅告警不阻断
                if (!string.IsNullOrEmpty(failedEvent.SerializedHeader) && eventData.Header == null)
                {
                    try
                    {
                        eventData.Header = FeishuJsonAot.Deserialize<Mud.Feishu.Abstractions.FeishuEventHeader>(
                            failedEvent.SerializedHeader!, FeishuJsonDefaults.DeserializerOptions);
                    }
                    catch (Exception headerEx)
                    {
                        _logger.LogWarning(headerEx, "恢复事件 {EventId} 的 Header 失败，继续重试（Header 为空）", failedEvent.EventId);
                    }
                }

                _logger.LogInformation("开始重试事件 {EventId}，当前重试次数: {RetryCount}/{MaxRetry}，AppKey: {AppKey}",
                    failedEvent.EventId, failedEvent.RetryCount, Retry.MaxRetryCount, failedEvent.AppKey ?? "null");

                // 尝试重新处理
                var result = await webhookService.HandleEventAsync(eventData, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("事件 {EventId} 重试成功", failedEvent.EventId);
                    await _failedEventStore.RemoveFailedEventAsync(failedEvent.StoreKey ?? failedEvent.EventId, cancellationToken);
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

        // R3-P2-14：本轮结束后清除 AppKey 上下文（纵深防御）。
        // 迭代内已通过 SetCurrentAppKey(AppKey ?? "") 无条件覆盖（WHF-R2/B5），跨迭代不会串味；
        // 但本服务运行在**长生命周期 ExecutionContext** 中，若不清除，最后一批重试事件的 AppKey
        // 会在进程余下时间残留在 AsyncLocal 里，而 IAppKeyAccessor 与业务去重键、应用上下文
        // 共用同一实例——显式 Clear 成本近零，可消除该残留面。
        _appKeyAccessor?.Clear();
    }

    /// <summary>
    /// 判断是否应该重试
    /// </summary>
    private bool ShouldRetry(FailedEventInfo failedEvent)
    {
        return failedEvent.RetryCount < Retry.MaxRetryCount;
    }
}
