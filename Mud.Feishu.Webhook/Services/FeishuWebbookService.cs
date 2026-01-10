// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;

namespace Mud.Feishu.Webhook.Services;

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
    private readonly MetricsCollector _metrics;
    private readonly FeishuWebhookConcurrencyService _concurrencyService;
    private readonly FeishuWebhookEventDeduplicator _deduplicator;

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
        MetricsCollector metrics,
        FeishuWebhookConcurrencyService concurrencyService,
        FeishuWebhookEventDeduplicator deduplicator)
    {
        _optionsMonitor = optionsMonitor;
        _validator = validator;
        _decryptor = decryptor;
        _handlerFactory = handlerFactory;
        _logger = logger;
        _metrics = metrics;
        _concurrencyService = concurrencyService;
        _deduplicator = deduplicator;

        // 监听配置变更
        _optionsMonitor.OnChange((newOptions, name) =>
        {
            _logger.LogInformation("飞书 Webhook 配置已更新，来源: {ChangeSource}", name);
        });
    }

    /// <inheritdoc />
    public async Task<EventVerificationResponse?> VerifyEventSubscriptionAsync(EventVerificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始验证飞书事件订阅请求");

            if (!_validator.ValidateSubscriptionRequest(request, Options.VerificationToken))
            {
                _logger.LogWarning("事件订阅验证失败");
                return null;
            }

            _metrics.IncrementVerificationRequests();

            var response = new EventVerificationResponse
            {
                Challenge = request.Challenge
            };

            _logger.LogInformation("事件订阅验证成功，返回挑战码: {Challenge}", request.Challenge);
            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证事件订阅请求时发生错误");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorReason)> HandleEventAsync(FeishuWebhookRequest request, CancellationToken cancellationToken = default)
    {
        if (Options.EnablePerformanceMonitoring)
        {
            using var activity = _metrics.StartEventHandlingActivity();
            return await HandleEventInternalAsync(request, cancellationToken);
        }
        else
        {
            return await HandleEventInternalAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// 内部事件处理方法
    /// </summary>
    private async Task<(bool Success, string? ErrorReason)> HandleEventInternalAsync(FeishuWebhookRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("开始处理飞书事件");

            // 验证请求签名（可选，因为 Middleware 中已验证 X-Lark-Signature 请求头）
            if (Options.EnableBodySignatureValidation && !ValidateRequestSignature(request))
            {
                _logger.LogWarning("请求体签名验证失败");
                _metrics.IncrementSignatureValidationFailures();
                return (false, "Signature validation failed");
            }

            // 解密事件数据
            if (string.IsNullOrEmpty(request.Encrypt))
            {
                _logger.LogError("请求中缺少加密数据");
                return (false, "Missing encrypted data");
            }

            var eventData = await DecryptEventAsync(request.Encrypt!, cancellationToken);
            if (eventData == null)
            {
                _logger.LogError("事件数据解密失败");
                _metrics.IncrementDecryptionFailures();
                return (false, "Decryption failed");
            }

            // 检查事件是否已处理（幂等性）
            if (_deduplicator.TryMarkAsProcessed(eventData.EventId))
            {
                _logger.LogInformation("事件 {EventId} 已处理过，跳过（幂等性）", eventData.EventId);
                return (true, null); // 已处理，返回成功避免飞书重试
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

                _metrics.IncrementSuccessfulEvents();
                _logger.LogInformation("事件处理完成: {EventType}, 事件ID: {EventId}",
                    eventData.EventType, eventData.EventId);

                return (true, null);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("事件处理超时: {EventType}, 事件ID: {EventId}, 超时时间: {TimeoutMs}ms",
                    eventData.EventType, eventData.EventId, Options.EventHandlingTimeoutMs);
                _metrics.IncrementFailedEvents();
                return (false, "Event handling timeout");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("事件处理被取消");
            _metrics.IncrementCancelledEvents();
            return (false, "Operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书事件时发生错误");
            _metrics.IncrementFailedEvents();

            if (Options.EnableExceptionHandling)
            {
                // 记录异常但不抛出，避免影响整体服务
                return (false, "Internal server error");
            }

            throw;
        }
    }

    /// <inheritdoc />
    public bool ValidateRequestSignature(FeishuWebhookRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Encrypt) ||
                string.IsNullOrEmpty(request.Signature) ||
                string.IsNullOrEmpty(request.Nonce))
            {
                _logger.LogWarning("请求缺少必要的签名字段");
                return false;
            }

            return _validator.ValidateSignature(
                request.Timestamp,
                request.Nonce,
                request.Encrypt!,
                request.Signature,
                Options.EncryptKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证请求签名时发生错误");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<EventData?> DecryptEventAsync(string encryptedData, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _decryptor.DecryptAsync(encryptedData, Options.EncryptKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密事件数据时发生错误");
            return null;
        }
    }
}