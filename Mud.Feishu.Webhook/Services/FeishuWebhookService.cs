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
using Mud.Feishu.Webhook.Services;

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
    private readonly IFeishuEventDistributedDeduplicator? _distributedDeduplicator;
    private readonly ISecurityAuditService? _securityAuditService;
    private readonly IEncryptKeyProvider _encryptKeyProvider;

    /// <summary>
    /// 当前应用键（多应用场景，使用 AsyncLocal 确保线程安全）
    /// </summary>
    private static readonly AsyncLocal<string?> _currentAppKey = new();

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
        ISecurityAuditService? securityAuditService,
        IFeishuEventDistributedDeduplicator? distributedDeduplicator = null)
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
        _currentAppKey.Value = appKey;
        _validator.SetCurrentAppKey(appKey);
    }

    /// <inheritdoc />
    public async Task<EventVerificationResponse?> VerifyEventSubscriptionAsync(EventVerificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始验证飞书事件订阅请求, AppKey: {AppKey}", _currentAppKey.Value ?? "null");

            // 从应用配置中获取验证 Token
            if (string.IsNullOrEmpty(_currentAppKey.Value))
            {
                _logger.LogError("当前应用键未设置，无法验证事件订阅请求");
                return null;
            }

            var appConfig = Options.GetAppConfig(_currentAppKey.Value!);
            if (appConfig == null)
            {
                _logger.LogError("未找到应用配置, AppKey: {AppKey}", _currentAppKey);
                return null;
            }

            if (!await _validator.ValidateSubscriptionRequestAsync(request, appConfig.VerificationToken ?? string.Empty))
            {
                _logger.LogWarning("事件订阅验证失败, AppKey: {AppKey}", _currentAppKey);
                return null;
            }

            var response = new EventVerificationResponse
            {
                Challenge = request.Challenge
            };

            _logger.LogInformation("事件订阅验证成功，返回挑战码: {Challenge}, AppKey: {AppKey}", request.Challenge, _currentAppKey);
            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证事件订阅请求时发生错误, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorReason)> HandleEventAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        return await HandleEventWithInterceptorsAsync(eventData, null, cancellationToken);
    }

    /// <summary>
    /// 使用拦截器处理事件（已解密的 EventData）
    /// </summary>
    private async Task<(bool Success, string? ErrorReason)> HandleEventWithInterceptorsAsync(EventData eventData, string? appKey, CancellationToken cancellationToken)
    {
        Exception? processingException = null;

        try
        {
            // 记录事件处理开始
            using var eventMetrics = FeishuMetricsHelper.RecordEventHandling(eventData.EventType, "webhook");

            // 前置拦截器
            foreach (var interceptor in _interceptors)
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
                // 分发事件到处理器
                await _handlerFactory.HandleEventParallelAsync(eventData.EventType, eventData, timeoutCts.Token);

                // 处理成功，标记为已完成
                await MarkDeduplicationCompletedAsync(eventData.EventId);

                // 记录事件处理成功
                FeishuMetricsHelper.RecordEventHandlingSuccess(eventData.EventType);

                _logger.LogInformation("事件处理完成: {EventType}, 事件ID: {EventId}, AppKey: {AppKey}",
                    eventData.EventType, eventData.EventId, appKey ?? "null");

                return (true, null);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                await RollbackDeduplicationAsync(eventData.EventId);

                _logger.LogWarning("事件处理超时: {EventType}, 事件ID: {EventId}, 超时时间: {TimeoutMs}ms, AppKey: {AppKey}",
                    eventData.EventType, eventData.EventId, Options.EventHandlingTimeoutMs, appKey ?? "null");
                FeishuMetricsHelper.RecordEventHandlingFailure(eventData.EventType, "timeout");
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            await RollbackDeduplicationAsync(eventData.EventId);
            _logger.LogWarning("事件处理被取消，EventId: {EventId}, AppKey: {AppKey}", eventData.EventId, appKey ?? "null");
            FeishuMetricsHelper.RecordEventHandlingFailure(eventData.EventType, "canceled");
            throw;
        }
        catch (Exception ex)
        {
            processingException = ex;
            await RollbackDeduplicationAsync(eventData.EventId);
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
            foreach (var interceptor in _interceptors)
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
                _logger.LogWarning("请求缺少必要的签名字段, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
                FeishuMetricsHelper.RecordEventHandlingFailure("signature_validation", "missing_fields");
                return false;
            }

            // 使用密钥提供程序获取加密密钥
            string? encryptKey = null;
            if (!string.IsNullOrEmpty(_currentAppKey.Value))
            {
                encryptKey = await _encryptKeyProvider.GetEncryptKeyAsync(_currentAppKey.Value!);
            }

            if (string.IsNullOrEmpty(encryptKey))
            {
                _logger.LogError("缺少加密密钥，无法验证签名, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
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
            _logger.LogError(ex, "验证请求签名时发生错误, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
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
            var encryptKey = await _encryptKeyProvider.GetEncryptKeyAsync(_currentAppKey.Value ?? string.Empty);
            if (string.IsNullOrEmpty(encryptKey))
            {
                _logger.LogError("无法获取加密密钥, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
                return false;
            }

            // 构建签名字符串：timestamp + nonce + encryptKey + body
            var signString = $"{request.Timestamp}{request.Nonce}{encryptKey}{body}";

            // 使用 SHA-256 计算签名
            var computedSignature = SignatureValidator.ComputeSha256Signature(signString);

            // 使用固定时间比较防止计时攻击
            var isValid = SignatureValidator.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedSignature),
                Encoding.UTF8.GetBytes(request.Signature));

            if (!isValid)
            {
                var computedPrefix = computedSignature.Length > 8 ? computedSignature.Substring(0, 8) : computedSignature;
                var signaturePrefix = request.Signature.Length > 8 ? request.Signature.Substring(0, 8) : request.Signature;
                _logger.LogWarning("签名验证失败: 计算 {ComputedSignaturePrefix}..., 期望 {ExpectedSignaturePrefix}..., AppKey: {AppKey}",
                    computedPrefix + "...",
                    signaturePrefix + "...",
                    _currentAppKey.Value ?? "null");
            }
            else
            {
                _logger.LogDebug("签名验证成功, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证请求签名时发生错误, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
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
            if (!string.IsNullOrEmpty(_currentAppKey.Value))
            {
                encryptKey = await _encryptKeyProvider.GetEncryptKeyAsync(_currentAppKey.Value!);
            }

            if (string.IsNullOrEmpty(encryptKey))
            {
                _logger.LogError("缺少加密密钥，无法解密事件数据, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
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
            _logger.LogError(ex, "解密事件数据时发生错误, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
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
            return (_deduplicator.TryMarkAsProcessing(eventId), false);
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
            _deduplicator.MarkAsCompleted(eventId);
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
            _deduplicator.RollbackProcessing(eventId);
        }
    }
}