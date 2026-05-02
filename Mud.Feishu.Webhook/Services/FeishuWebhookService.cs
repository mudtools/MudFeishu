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
#pragma warning disable CS0618 // IFeishuEventDistributedDeduplicator 已废弃，但需保持向后兼容直到正式移除
    private readonly IFeishuEventDistributedDeduplicator? _distributedDeduplicator;
#pragma warning restore CS0618 // IFeishuEventDistributedDeduplicator 已废弃，但需保持向后兼容直到正式移除
    private readonly ISecurityAuditService? _securityAuditService;
    private readonly IEncryptKeyProvider _encryptKeyProvider;
    private readonly FeishuWebhookHandlerRegistry _handlerRegistry;
    private readonly FeishuWebhookInterceptorRegistry _interceptorRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebhookAppKeyAccessor _appKeyAccessor;

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
        ISecurityAuditService? securityAuditService,
#pragma warning disable CS0618 // IFeishuEventDistributedDeduplicator 已废弃，但需保持向后兼容直到正式移除
        IFeishuEventDistributedDeduplicator? distributedDeduplicator = null)
#pragma warning restore CS0618 // IFeishuEventDistributedDeduplicator 已废弃，但需保持向后兼容直到正式移除
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
        _distributedDeduplicator = distributedDeduplicator;
        _securityAuditService = securityAuditService;

        // 监听配置变更
        _optionsMonitor.OnChange((newOptions, name) =>
        {
            var oldOptions = Options;
            var changes = new List<string>();

            // 检测关键配置项的变更
            if (oldOptions.EventHandlingTimeoutMs != newOptions.EventHandlingTimeoutMs)
            {
                changes.Add($"EventHandlingTimeoutMs: {oldOptions.EventHandlingTimeoutMs}ms → {newOptions.EventHandlingTimeoutMs}ms");
            }

            if (oldOptions.MaxConcurrentEvents != newOptions.MaxConcurrentEvents)
            {
                changes.Add($"MaxConcurrentEvents: {oldOptions.MaxConcurrentEvents} → {newOptions.MaxConcurrentEvents}");
            }

            if (oldOptions.EnableExceptionHandling != newOptions.EnableExceptionHandling)
            {
                changes.Add($"EnableExceptionHandling: {oldOptions.EnableExceptionHandling} → {newOptions.EnableExceptionHandling}");
            }

            if (oldOptions.EnableBackgroundProcessing != newOptions.EnableBackgroundProcessing)
            {
                changes.Add($"EnableBackgroundProcessing: {oldOptions.EnableBackgroundProcessing} → {newOptions.EnableBackgroundProcessing}");
            }

            // 注意：CircuitBreaker 配置变更已被移除


            if (oldOptions.EnablePerformanceMonitoring != newOptions.EnablePerformanceMonitoring)
            {
                changes.Add($"EnablePerformanceMonitoring: {oldOptions.EnablePerformanceMonitoring} → {newOptions.EnablePerformanceMonitoring}");
            }

            // 检测应用配置的变更
            var oldAppKeys = oldOptions.Apps.Keys.OrderBy(k => k).ToList();
            var newAppKeys = newOptions.Apps.Keys.OrderBy(k => k).ToList();

            if (!oldAppKeys.SequenceEqual(newAppKeys))
            {
                var addedApps = newAppKeys.Except(oldAppKeys).ToList();
                var removedApps = oldAppKeys.Except(newAppKeys).ToList();

                if (addedApps.Count > 0)
                {
                    changes.Add($"新增应用: {string.Join(", ", addedApps)}");
                }

                if (removedApps.Count > 0)
                {
                    changes.Add($"移除应用: {string.Join(", ", removedApps)}");
                }
            }

            if (changes.Count > 0)
            {
                _logger.LogInformation("飞书 Webhook 配置已更新，来源: {ChangeSource}，变更内容:\n{Changes}", name, string.Join("\n  - ", changes));
            }
            else
            {
                _logger.LogDebug("飞书 Webhook 配置已更新，来源: {ChangeSource}（无关键配置变更）", name);
            }
        });
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

        // 获取拦截器列表（优先使用应用专属拦截器，回退到全局拦截器）
        var interceptors = GetInterceptors(appKey).ToList();

        try
        {
            // 记录事件处理开始
            using var eventMetrics = FeishuMetricsHelper.RecordEventHandling(eventData.EventType, "webhook");

            // 前置拦截器
            foreach (var interceptor in interceptors)
            {
                var shouldContinue = await interceptor.BeforeHandleAsync(eventData.EventType, eventData, cancellationToken);
                if (!shouldContinue)
                {
                    _logger.LogWarning("事件被拦截器中断: {EventType}, EventId: {EventId}, Interceptor: {InterceptorType}, AppKey: {AppKey}",
                        eventData.EventType, eventData.EventId, interceptor.GetType().Name, appKey ?? "null");
                    FeishuMetricsHelper.RecordEventHandlingFailure(eventData.EventType, "intercepted");
                    return (false, "Event intercepted");
                }
            }

            // 去重检查
            var deduplicationResult = await CheckDeduplicationAsync(eventData.EventId, appKey, cancellationToken);
            if (deduplicationResult.shouldSkip)
            {
                _logger.LogWarning("检测到重复事件 {EventId}（AppKey: {AppKey}），跳过处理（幂等性）", eventData.EventId, appKey ?? "null");
                FeishuMetricsHelper.RecordEventDeduplicationHit("event_id");
                return (true, null); // 幂等性：返回成功避免飞书重试
            }

            // 使用全局并发控制服务
            using var concurrencyLock = await _concurrencyService.AcquireAsync(cancellationToken);

            // 添加超时控制
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Options.EventHandlingTimeoutMs);

            try
            {
                // 分发事件到处理器（优先使用应用专属处理器，回退到全局工厂）
                await DispatchEventAsync(eventData.EventType, eventData, appKey, timeoutCts.Token);

                // 处理成功，标记为已完成
                await MarkDeduplicationCompletedAsync(eventData.EventId, appKey);

                // 记录事件处理成功
                FeishuMetricsHelper.RecordEventHandlingSuccess(eventData.EventType);

                _logger.LogInformation("事件处理完成: {EventType}, 事件ID: {EventId}, AppKey: {AppKey}",
                    eventData.EventType, eventData.EventId, appKey ?? "null");

                return (true, null);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                await RollbackDeduplicationAsync(eventData.EventId, appKey);

                _logger.LogWarning("事件处理超时: {EventType}, 事件ID: {EventId}, 超时时间: {TimeoutMs}ms, AppKey: {AppKey}",
                    eventData.EventType, eventData.EventId, Options.EventHandlingTimeoutMs, appKey ?? "null");
                FeishuMetricsHelper.RecordEventHandlingFailure(eventData.EventType, "timeout");
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            await RollbackDeduplicationAsync(eventData.EventId, appKey);
            _logger.LogWarning("事件处理被取消，EventId: {EventId}, AppKey: {AppKey}", eventData.EventId, appKey ?? "null");
            FeishuMetricsHelper.RecordEventHandlingFailure(eventData.EventType, "canceled");
            throw;
        }
        catch (Exception ex)
        {
            processingException = ex;
            await RollbackDeduplicationAsync(eventData.EventId, appKey);
            _logger.LogError(ex, "处理飞书事件时发生错误，EventId: {EventId}, AppKey: {AppKey}", eventData.EventId, appKey ?? "null");

            // 记录事件处理失败
            FeishuMetricsHelper.RecordEventHandlingFailure(eventData.EventType, ex.GetType().Name);

            if (Options.EnableExceptionHandling)
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
        }
    }

    /// <summary>
    /// 使用拦截器处理事件（加密的 FeishuWebhookRequest）
    /// </summary>
    private async Task<(bool Success, string? ErrorReason)> HandleEventWithInterceptorsAsync(FeishuWebhookRequest request, string? appKey, CancellationToken cancellationToken)
    {
        // 验证请求签名
        if (Options.EnableBodySignatureValidation && !await ValidateRequestSignature(request))
        {
            _logger.LogWarning("请求体签名验证失败，AppKey: {AppKey}", appKey ?? "null");

            // 记录安全审计日志
            _ = _securityAuditService?.LogSecurityFailureAsync(
                SecurityEventType.SignatureValidation,
                "unknown", // 在服务层无法获取客户端IP
                "FeishuWebhookService",
                "请求体签名验证失败",
                "",
                appKey);

            return (false, "Signature validation failed");
        }

        // 解密事件数据
        if (string.IsNullOrEmpty(request.Encrypt))
        {
            _logger.LogError("请求中缺少加密数据，AppKey: {AppKey}", appKey ?? "null");
            return (false, "Missing encrypted data");
        }

        var eventData = await DecryptEventAsync(request.Encrypt!, cancellationToken);
        if (eventData == null)
        {
            _logger.LogError("事件数据解密失败，AppKey: {AppKey}", appKey ?? "null");
            return (false, "Decryption failed");
        }

        return await HandleEventWithInterceptorsAsync(eventData, appKey, cancellationToken);
    }

    /// <summary />
    public async Task<bool> ValidateRequestSignature(FeishuWebhookRequest request)
    {
        // 记录签名验证开始
        using var signatureMetrics = FeishuMetricsHelper.RecordEventHandling("signature_validation", "webhook");

        try
        {
            if (string.IsNullOrEmpty(request.Encrypt) ||
                string.IsNullOrEmpty(request.Signature) ||
                string.IsNullOrEmpty(request.Nonce))
            {
                _logger.LogWarning("请求缺少必要的签名字段, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");
                FeishuMetricsHelper.RecordEventHandlingFailure("signature_validation", "missing_fields");
                return false;
            }

            // 使用密钥提供程序获取加密密钥
            string? encryptKey = null;
            if (!string.IsNullOrEmpty(_appKeyAccessor.CurrentAppKey))
            {
                encryptKey = await _encryptKeyProvider.GetEncryptKeyAsync(_appKeyAccessor.CurrentAppKey!);
            }

            if (string.IsNullOrEmpty(encryptKey))
            {
                _logger.LogError("缺少加密密钥，无法验证签名, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");
                FeishuMetricsHelper.RecordEventHandlingFailure("signature_validation", "missing_encrypt_key");
                return false;
            }

            var isValid = await _validator.ValidateSignatureAsync(
                request.Timestamp,
                request.Nonce,
                request.Encrypt!,
                request.Signature,
                encryptKey!);

            if (isValid)
            {
                FeishuMetricsHelper.RecordEventHandlingSuccess("signature_validation");
            }
            else
            {
                FeishuMetricsHelper.RecordEventHandlingFailure("signature_validation", "invalid_signature");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证请求签名时发生错误, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");
            FeishuMetricsHelper.RecordEventHandlingFailure("signature_validation", ex.GetType().Name);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> HandleEventAsync(FeishuWebhookRequest request, string body)
    {
        try
        {
            // 使用密钥提供程序获取加密密钥
            var encryptKey = await _encryptKeyProvider.GetEncryptKeyAsync(_appKeyAccessor.CurrentAppKey ?? string.Empty);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证请求签名时发生错误, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");
            return false;
        }
    }



    /// <inheritdoc />
    public async Task<EventData?> DecryptEventAsync(string encryptedData, CancellationToken cancellationToken = default)
    {
        // 记录事件解密开始
        using var decryptMetrics = FeishuMetricsHelper.RecordEventHandling("event_decryption", "webhook");

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
                FeishuMetricsHelper.RecordEventHandlingFailure("event_decryption", "missing_encrypt_key");
                return null;
            }

            var eventData = await _decryptor.DecryptAsync(encryptedData, encryptKey!, cancellationToken);
            if (eventData != null)
            {
                FeishuMetricsHelper.RecordEventHandlingSuccess("event_decryption");
            }
            else
            {
                FeishuMetricsHelper.RecordEventHandlingFailure("event_decryption", "decryption_failed");
            }

            return eventData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密事件数据时发生错误, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");
            FeishuMetricsHelper.RecordEventHandlingFailure("event_decryption", ex.GetType().Name);
            return null;
        }
    }

    /// <summary>
    /// 检查去重状态
    /// </summary>
    private async Task<(bool shouldSkip, bool isProcessing)> CheckDeduplicationAsync(string eventId, string? appKey, CancellationToken cancellationToken)
    {
        if (_distributedDeduplicator != null)
        {
            var result = await _distributedDeduplicator.TryMarkAsProcessingAsync(eventId, appKey, cancellationToken: cancellationToken);
            return (result.IsDuplicate, result.WasProcessing);
        }
        else
        {
            return (_deduplicator.TryMarkAsProcessing(eventId, appKey), false);
        }
    }

    /// <summary>
    /// 标记去重为已完成
    /// </summary>
    private async Task MarkDeduplicationCompletedAsync(string eventId, string? appKey = null)
    {
        if (_distributedDeduplicator != null)
        {
            await _distributedDeduplicator.MarkAsCompletedAsync(eventId, appKey);
        }
        else
        {
            _deduplicator.MarkAsCompleted(eventId, appKey);
        }
    }

    /// <summary>
    /// 回滚去重状态
    /// </summary>
    private async Task RollbackDeduplicationAsync(string eventId, string? appKey = null)
    {
        if (_distributedDeduplicator != null)
        {
            await _distributedDeduplicator.RollbackProcessingAsync(eventId, appKey);
        }
        else
        {
            _deduplicator.RollbackProcessing(eventId, appKey);
        }
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