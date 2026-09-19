// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书 Webhook 服务实现
/// </summary>
public class FeishuWebhookService : IFeishuWebhookService
{
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor;
    private readonly IFeishuEventValidator _validator;
    private readonly IFeishuEventDecryptor _decryptor;
    private readonly IFeishuEventHandlerFactory _handlerFactory;
    private readonly ILogger<FeishuWebhookService> _logger;
    private readonly IFeishuEventInterceptor[] _interceptors;
    private readonly FeishuWebhookConcurrencyService _concurrencyService;
    private readonly IFeishuEventDeduplicator _deduplicator;
    private readonly IEncryptKeyProvider _encryptKeyProvider;
    private readonly FeishuWebhookHandlerRegistry _handlerRegistry;
    private readonly FeishuWebhookInterceptorRegistry _interceptorRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebhookAppKeyAccessor _appKeyAccessor;
    private readonly IFailedEventStore? _failedEventStore;

    /// <summary>
    /// 获取当前配置选项（支持热更新）
    /// </summary>
    private FeishuWebhookOptions Options => _optionsMonitor.CurrentValue;

    /// <inheritdoc />
    public FeishuWebhookService(
        IOptionsMonitor<FeishuWebhookOptions> optionsMonitor,
        IFeishuEventValidator validator,
        IFeishuEventDecryptor decryptor,
        IFeishuEventHandlerFactory handlerFactory,
        ILogger<FeishuWebhookService> logger,
        IFeishuEventInterceptor[] interceptors,
        FeishuWebhookConcurrencyService concurrencyService,
        IFeishuEventDeduplicator deduplicator,
        IEncryptKeyProvider encryptKeyProvider,
        FeishuWebhookHandlerRegistry handlerRegistry,
        FeishuWebhookInterceptorRegistry interceptorRegistry,
        IServiceProvider serviceProvider,
        IWebhookAppKeyAccessor appKeyAccessor,
        IFailedEventStore? failedEventStore = null)
    {
        _optionsMonitor = optionsMonitor;
        _validator = validator;
        _decryptor = decryptor;
        _handlerFactory = handlerFactory;
        _logger = logger;
        _interceptors = interceptors;
        _concurrencyService = concurrencyService;
        _deduplicator = deduplicator;
        _encryptKeyProvider = encryptKeyProvider ?? throw new ArgumentNullException(nameof(encryptKeyProvider));
        _handlerRegistry = handlerRegistry ?? throw new ArgumentNullException(nameof(handlerRegistry));
        _interceptorRegistry = interceptorRegistry ?? throw new ArgumentNullException(nameof(interceptorRegistry));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _appKeyAccessor = appKeyAccessor ?? throw new ArgumentNullException(nameof(appKeyAccessor));
        _failedEventStore = failedEventStore;

        // WHF-13：本服务为 Scoped（每个 Webhook 请求创建一个实例），构造函数内的 OnChange 订阅
        // 会随请求数线性泄漏（订阅挂在 Singleton IOptionsMonitor 上，且每次配置变更会对所有
        // 历史 scope 重放回调）。变更日志由 FeishuMultiAppMiddleware 与 FeishuWebhookConcurrencyService
        // 两个 Singleton 持有者承接，此处不再订阅。
    }

    /// <inheritdoc />
    public void SetCurrentAppKey(string appKey)
    {
        _appKeyAccessor.SetAppKey(appKey);
    }

    /// <inheritdoc />
    public async Task<EventVerificationResponse?> VerifyEventSubscriptionAsync(EventVerificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始验证飞书事件订阅请求, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");

            // 从应用配置中获取验证 Token
            if (string.IsNullOrEmpty(_appKeyAccessor.CurrentAppKey))
            {
                _logger.LogError("当前应用键未设置，无法验证事件订阅请求");
                return null;
            }

            var appConfig = Options.GetAppConfig(_appKeyAccessor.CurrentAppKey!);
            if (appConfig == null)
            {
                _logger.LogError("未找到应用配置, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey);
                return null;
            }

            if (!await _validator.ValidateSubscriptionRequestAsync(request, appConfig.VerificationToken ?? string.Empty))
            {
                _logger.LogWarning("事件订阅验证失败, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey);
                return null;
            }

            var response = new EventVerificationResponse
            {
                Challenge = request.Challenge
            };

            _logger.LogInformation("事件订阅验证成功，返回挑战码: {Challenge}, AppKey: {AppKey}", request.Challenge, _appKeyAccessor.CurrentAppKey);
            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证事件订阅请求时发生错误, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorReason)> HandleEventAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        return await HandleEventWithInterceptorsAsync(eventData, _appKeyAccessor.CurrentAppKey, cancellationToken);
    }

    /// <summary>
    /// 使用拦截器处理事件（已解密的 EventData）
    /// </summary>
    private async Task<(bool Success, string? ErrorReason)> HandleEventWithInterceptorsAsync(EventData eventData, string? appKey, CancellationToken cancellationToken)
    {
        Exception? processingException = null;

        var enablePerformanceMonitoring = Options.EnablePerformanceMonitoring;
        var appConfig = !string.IsNullOrEmpty(appKey) ? Options.GetAppConfig(appKey!) : null;
        if (appConfig != null)
            enablePerformanceMonitoring = appConfig.GetEffectiveEnablePerformanceMonitoring(Options.EnablePerformanceMonitoring);

        var performanceStopwatch = enablePerformanceMonitoring ? System.Diagnostics.Stopwatch.StartNew() : null;

        // 获取拦截器列表（优先使用应用专属拦截器，回退到全局拦截器）
        var interceptors = GetInterceptors(appKey).ToList();

        try
        {
            // 记录事件处理开始
            using var eventMetrics = FeishuMetricsHelper.RecordEventHandling(appKey ?? "unknown", eventData.EventType, "webhook");

            // 前置拦截器
            foreach (var interceptor in interceptors)
            {
                var shouldContinue = await interceptor.BeforeHandleAsync(eventData.EventType, eventData, cancellationToken);
                if (!shouldContinue)
                {
                    _logger.LogWarning("事件被拦截器中断: {EventType}, EventId: {EventId}, Interceptor: {InterceptorType}, AppKey: {AppKey}",
                        eventData.EventType, eventData.EventId, interceptor.GetType().Name, appKey ?? "null");
                    FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: false, "intercepted");
                    return (false, "Event intercepted");
                }
            }

            // 去重检查
            var deduplicationResult = await CheckDeduplicationAsync(eventData.EventId, appKey, cancellationToken);
            if (deduplicationResult.ShouldSkip)
            {
                _logger.LogWarning("检测到重复事件 {EventId}（AppKey: {AppKey}），跳过处理（幂等性）", eventData.EventId, appKey ?? "null");
                FeishuMetricsHelper.RecordEventDeduplication(appKey ?? "unknown", "event_id", hit: true);
                return (true, null); // 幂等性：返回成功避免飞书重试
            }

            // T2-2: 显式处理 WasProcessing 语义——命中 TimeoutRecoverable 时记录并计数
            if (deduplicationResult.WasProcessing)
            {
                _logger.LogInformation("事件 {EventId} 原处理中超时，本次重新处理，AppKey: {AppKey}", eventData.EventId, appKey ?? "null");
                FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: true, "timeout_recovered");
            }

            // 使用全局并发控制服务
            using var concurrencyLock = await _concurrencyService.AcquireAsync(cancellationToken);

            // 添加超时控制（B1：消费应用级 EventHandlingTimeoutMs；LegacyGlobalTimeoutOnly=true 时保持全局-only）
            var timeoutMs = Options.ResolveEventHandlingTimeoutMs(appConfig);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeoutMs);

            try
            {
                // 分发事件到处理器（优先使用应用专属处理器，回退到全局工厂）
                await DispatchEventAsync(eventData.EventType, eventData, appKey, timeoutCts.Token);

                try
                {
                    await MarkDeduplicationCompletedAsync(eventData.EventId, appKey);
                }
                catch (Exception markEx)
                {
                    // WHF-07：业务分发已成功——Mark 失败禁止回滚（回滚将导致飞书重推后重复消费）。
                    // 保留 processing 态，由 ProcessingTimeout/TTL 兜底；记录 Warning 供对账。
                    // 本分支捕获 Exception（含 Server 类 FeishuRedisException——内层 catch 先于外层
                    // 过滤器命中）：任何 Mark 失败的语义都相同（业务已成功），按成功口径返回，
                    // 不抛、不写失败存储。
                    _logger.LogWarning(markEx,
                        "事件 {EventId} 处理成功但完成标记失败，保留 processing 态等待超时恢复, AppKey: {AppKey}",
                        eventData.EventId, appKey ?? "null");
                    FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: true, "mark_completed_failed");

                                        _logger.LogInformation("事件处理完成（完成标记失败，按成功口径）: {EventType}, 事件ID: {EventId}, AppKey: {AppKey}",
                        eventData.EventType, eventData.EventId, appKey ?? "null");
                

                    return (true, null);
                }

                // 记录事件处理成功
                FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: true);

                                _logger.LogInformation("事件处理完成: {EventType}, 事件ID: {EventId}, AppKey: {AppKey}",
                    eventData.EventType, eventData.EventId, appKey ?? "null");
            

                return (true, null);
            }
            catch (OperationCanceledException oce) when (!cancellationToken.IsCancellationRequested)
            {
                // WHF-08：超时路径就地收尾，不再 rethrow——消除外层 OCE catch 的二次回滚/metrics；
                // processingException 赋值保证 AfterHandleAsync 收到非空异常
                await RollbackDeduplicationAsync(eventData.EventId, appKey);

                _logger.LogWarning("事件处理超时: {EventType}, 事件ID: {EventId}, 超时时间: {TimeoutMs}ms, AppKey: {AppKey}",
                    eventData.EventType, eventData.EventId, timeoutMs, appKey ?? "null");
                FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: false, "timeout");

                processingException = oce;
                return (false, "Event handling timeout");
            }
        }
        catch (OperationCanceledException)
        {
            await RollbackDeduplicationAsync(eventData.EventId, appKey);
            _logger.LogWarning("事件处理被取消，EventId: {EventId}, AppKey: {AppKey}", eventData.EventId, appKey ?? "null");
            FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: false, "canceled");
            throw;
        }
        catch (FeishuRedisException ex) when (ex.FailureKind == FeishuRedisFailureKind.Server)
        {
            // WHF-02：去重体系致命故障不是业务失败——不回滚（TryMark 失败时无状态可回滚，
            // Rollback 对不存在键本就安全）、不写失败存储（避免重试服务后续重复消费），上抛由中间件转 503
            _logger.LogError(ex, "去重服务致命故障（Server），EventId: {EventId}, AppKey: {AppKey}", eventData.EventId, appKey ?? "null");
            throw;
        }
        catch (Exception ex)
        {
            processingException = ex;
            await RollbackDeduplicationAsync(eventData.EventId, appKey);
            _logger.LogError(ex, "处理飞书事件时发生错误，EventId: {EventId}, AppKey: {AppKey}", eventData.EventId, appKey ?? "null");

            // 记录事件处理失败
            FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: false, ex.GetType().Name);

            // ADR-2: 失败路径在去重回滚之后写入失败事件存储（仅当启用重试时）
            if (Options.Retry.EnableRetry && _failedEventStore != null)
            {
                try
                {
                    var initialRetryDelay = Options.Retry.InitialRetryDelaySeconds;
                    var nextRetryAt = DateTimeOffset.UtcNow.AddSeconds(initialRetryDelay);
                    await _failedEventStore.StoreFailedEventAsync(eventData, ex, appKey, nextRetryAt, cancellationToken);
                }
                catch (Exception storeEx)
                {
                    _logger.LogError(storeEx, "写入失败事件存储失败，EventId: {EventId}", eventData.EventId);
                }
            }

            var effectiveEnableExceptionHandling = Options.EnableExceptionHandling;
            if (appConfig != null)
                effectiveEnableExceptionHandling = appConfig.GetEffectiveEnableExceptionHandling(Options.EnableExceptionHandling);

            if (effectiveEnableExceptionHandling)
            {
                return (false, "Internal server error");
            }
            throw;
        }
        finally
        {
            // 后置拦截器（无论成功或失败都执行）
            foreach (var interceptor in interceptors)
            {
                await interceptor.AfterHandleAsync(eventData.EventType, eventData, processingException, cancellationToken);
            }

            if (performanceStopwatch != null)
            {
                performanceStopwatch.Stop();
                _logger.LogInformation(
                    "性能监控: 事件 {EventType} 处理耗时 {ElapsedMs}ms, EventId: {EventId}, AppKey: {AppKey}",
                    eventData.EventType, performanceStopwatch.ElapsedMilliseconds, eventData.EventId, appKey ?? "null");
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> HandleEventAsync(FeishuWebhookRequest request, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            // 使用密钥提供程序获取加密密钥
            var encryptKey = await _encryptKeyProvider.GetEncryptKeyAsync(_appKeyAccessor.CurrentAppKey ?? string.Empty, cancellationToken);
            if (string.IsNullOrEmpty(encryptKey))
            {
                _logger.LogError("无法获取加密密钥, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");
                return false;
            }

            // 委托给验证器进行签名验证，消除内联重复代码
            return await _validator.ValidateHeaderSignatureAsync(
                request.Timestamp,
                request.Nonce,
                body,
                request.Signature,
                encryptKey!);
        }
        catch (Exception ex) when (ex is not FeishuRedisException { FailureKind: FeishuRedisFailureKind.Server })
        {
            _logger.LogError(ex, "验证请求签名时发生错误, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");
            return false;
        }
        // WHF-02：Server 类 FeishuRedisException 直接上抛（T-M2-10 契约），
        // 由中间件转为 503，不得在此层伪装成“验签失败”返回 false（→ 403）
    }



    /// <inheritdoc />
    public async Task<EventData?> DecryptEventAsync(string encryptedData, CancellationToken cancellationToken = default)
    {
        // 记录事件解密开始
        using var decryptMetrics = FeishuMetricsHelper.RecordEventHandling(_appKeyAccessor.CurrentAppKey ?? "unknown", "event_decryption", "webhook");

        try
        {
            // 使用密钥提供程序获取加密密钥
            string? encryptKey = null;
            if (!string.IsNullOrEmpty(_appKeyAccessor.CurrentAppKey))
            {
                encryptKey = await _encryptKeyProvider.GetEncryptKeyAsync(_appKeyAccessor.CurrentAppKey!);
            }

            if (string.IsNullOrEmpty(encryptKey))
            {
                _logger.LogError("缺少加密密钥，无法解密事件数据, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");
                FeishuMetricsHelper.RecordEventOutcome(_appKeyAccessor.CurrentAppKey ?? "unknown", "event_decryption", success: false, "missing_encrypt_key");
                return null;
            }

            var eventData = await _decryptor.DecryptAsync(encryptedData, encryptKey!, cancellationToken);
            if (eventData != null)
            {
                FeishuMetricsHelper.RecordEventOutcome(_appKeyAccessor.CurrentAppKey ?? "unknown", "event_decryption", success: true);
            }
            else
            {
                FeishuMetricsHelper.RecordEventOutcome(_appKeyAccessor.CurrentAppKey ?? "unknown", "event_decryption", success: false, "decryption_failed");
            }

            return eventData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密事件数据时发生错误, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");
            FeishuMetricsHelper.RecordEventOutcome(_appKeyAccessor.CurrentAppKey ?? "unknown", "event_decryption", success: false, ex.GetType().Name);
            return null;
        }
    }

    private async Task<(bool ShouldSkip, bool WasProcessing)> CheckDeduplicationAsync(string eventId, string? appKey, CancellationToken cancellationToken)
    {
        var result = await _deduplicator.TryMarkAsProcessingAsync(eventId, appKey, cancellationToken: cancellationToken);
        return (result.IsDuplicate, result.WasProcessing);
    }

    private async Task MarkDeduplicationCompletedAsync(string eventId, string? appKey = null)
    {
        await _deduplicator.MarkAsCompletedAsync(eventId, appKey);
    }

    private async Task RollbackDeduplicationAsync(string eventId, string? appKey = null)
    {
        await _deduplicator.RollbackProcessingAsync(eventId, appKey);
    }

    /// <summary>
    /// 分发事件到处理器（支持按 AppKey 隔离）
    /// 优先使用应用专属处理器，无专属处理器时回退到全局工厂
    /// </summary>
    private async Task DispatchEventAsync(string eventType, EventData eventData, string? appKey, CancellationToken cancellationToken)
    {
        // 如果有应用专属处理器，优先使用
        if (!string.IsNullOrEmpty(appKey) && _handlerRegistry.HasHandlers(appKey!))
        {
            var handlerTypes = _handlerRegistry.GetHandlers(appKey!);
            _logger.LogDebug("使用应用 {AppKey} 的专属处理器（{Count} 个）分发事件: {EventType}",
                appKey, handlerTypes.Count, eventType);

            var tasks = new List<Task>();
            foreach (var handlerType in handlerTypes)
            {
                var handler = (IFeishuEventHandler)_serviceProvider.GetRequiredService(handlerType);

                tasks.Add(handler.HandleAsync(eventData, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
        else
        {
            // WHF-09：未注册 eventType 门控——静默忽略（unhandled），不回退默认处理器兜底
            if (Options.IgnoreUnknownEventTypes && !_handlerFactory.IsHandlerRegistered(eventType))
            {
                _logger.LogDebug("事件类型 {EventType} 未注册处理器，已忽略（IgnoreUnknownEventTypes=true）, AppKey: {AppKey}",
                    eventType, appKey ?? "null");
                FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventType, success: true, "unhandled");
                return;
            }

            // 回退到全局处理器工厂
            _logger.LogDebug("使用全局处理器工厂分发事件: {EventType}, AppKey: {AppKey}",
                eventType, appKey ?? "null");
            await _handlerFactory.HandleEventParallelAsync(eventType, eventData, cancellationToken);
        }
    }

    /// <summary>
    /// 获取拦截器列表（支持按 AppKey 隔离）
    /// 优先使用应用专属拦截器，无专属拦截器时回退到全局拦截器
    /// </summary>
    private IEnumerable<IFeishuEventInterceptor> GetInterceptors(string? appKey)
    {
        // 如果有应用专属拦截器，优先使用
        if (!string.IsNullOrEmpty(appKey) && _interceptorRegistry.HasInterceptors(appKey!))
        {
            var interceptorTypes = _interceptorRegistry.GetInterceptors(appKey!);
            _logger.LogDebug("使用应用 {AppKey} 的专属拦截器（{Count} 个）", appKey, interceptorTypes.Count);

            foreach (var interceptorType in interceptorTypes)
            {
                var interceptor = (IFeishuEventInterceptor)_serviceProvider.GetRequiredService(interceptorType);
                yield return interceptor;
            }
        }
        else
        {
            // 回退到全局拦截器
            _logger.LogDebug("使用全局拦截器（{Count} 个）, AppKey: {AppKey}",
                _interceptors.Length, appKey ?? "null");

            foreach (var interceptor in _interceptors)
            {
                yield return interceptor;
            }
        }
    }
}