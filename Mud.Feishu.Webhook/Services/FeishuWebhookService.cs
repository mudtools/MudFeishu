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
    /// <summary>
    /// 拦截器中断且 <see cref="InterceptionAckMode"/> 为 <see cref="InterceptionAckMode.Retryable"/>
    /// 时，<c>EventHandlingResult.ErrorReason</c> 的取值。
    /// </summary>
    /// <remarks>
    /// 中间件据此显式映射 HTTP 503（要求飞书重推），而<b>不</b>复用 500 语义——
    /// 500 会触发失败事件存储写入，与“拦截不是业务失败”的语义不符。
    /// 以常量共享，避免宿主与中间件之间的字符串散落。
    /// </remarks>
    public const string InterceptedRetryableReason = "Event intercepted (retryable)";

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
    [Obsolete("明文验证协议无重放防护，中间件已强制加密验证。请使用加密 url_verification 链路。将在下个 major 移除。")]
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

        // R5.2/X10：已删除的性能监控开关（迁移说明必须引用其键名）。
        // audit-allow: X10 - migration note must name the removed config key
        // 耗时改为**无条件**采集、并以 Debug 级别输出：
        // ① 保留可诊断性——需要时把 `Logging:LogLevel:Mud.Feishu.Webhook` 调到 `Debug` 即可拿到耗时；
        // ② 与 R4 起「日志级别只由 Logging:LogLevel 控制，不设模块私有开关」的口径一致（AGENTS.md 配置面治理）。
        var appConfig = !string.IsNullOrEmpty(appKey) ? Options.GetAppConfig(appKey!) : null;

        var performanceStopwatch = System.Diagnostics.Stopwatch.StartNew();

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

                    // R3-P0-2/D2：拦截的默认语义是“已消费”——此前返回 (false, …) 被中间件一律映射 500，
                    // 飞书重推 → 再次被拦截 → 再 500，事件永不 ack，且不落任何去重/失败存储记录。
                    // 宿主可通过 InterceptionAckMode=Retryable 表达“暂时不能处理，请稍后重推”（→ 503）。
                    if (Options.InterceptionAckMode == InterceptionAckMode.Retryable)
                    {
                        FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: false, "intercepted_retryable");
                        processingException = new Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException(
                            "intercepted_retryable", $"事件被 {interceptor.GetType().Name} 拦截，要求对端重推");
                        // 不落去重标记：本事件尚未被消费，重推后必须能再次进入处理流程。
                        return (false, InterceptedRetryableReason);
                    }

                    // 已消费口径：拦截是**有意消费**，指标按 success=true 记（便于与业务失败区分）。
                    FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: true, "intercepted");

                    // P1-7：AfterHandle 仍可判别拦截终态（接口签名不变）
                    processingException = new Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException(
                        "intercepted", $"事件被 {interceptor.GetType().Name} 拦截");

                    // 关键：拦截发生在去重之前，必须补落去重标记——否则飞书重推会再次进入本分支。
                    // 注意：MemoryDeduplicator.MarkAsCompleted 对不存在的键是静默 no-op，
                    // 因此必须**先占位**（TryMarkAsProcessing）再置完成。
                    if (!string.IsNullOrEmpty(eventData.EventId))
                    {
                        try
                        {
                            await _deduplicator.TryMarkAsProcessingAsync(eventData.EventId, appKey, cancellationToken: CancellationToken.None);
                            await MarkDeduplicationCompletedAsync(eventData.EventId, appKey);
                        }
                        catch (Exception markEx)
                        {
                            // 与 WHF-07 同口径：标记失败不影响“已消费”结论（此处尚未执行业务），仅告警供对账。
                            _logger.LogWarning(markEx, "拦截事件 {EventId} 的去重标记写入失败，AppKey: {AppKey}", eventData.EventId, appKey ?? "null");
                        }
                    }

                    return (true, null);   // 200 → 停止飞书重推
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

                // R3-P1-3：EventHandlingTimeoutMs 是"软超时"——await Task.WhenAll 已确保处理器收尾
                // （满足 D4：不制造孤儿任务），但不响应 CancellationToken 的处理器会把实际耗时
                // 顶到远超 timeoutMs。此处只做可观测（指标 + Warning），**不**引入硬超时：
                // 硬超时会制造 D4 明令禁止的"状态已释放而任务仍在跑"。
                performanceStopwatch.Stop();
                var elapsedMs = performanceStopwatch.ElapsedMilliseconds;
                if (elapsedMs > timeoutMs * 1.5)
                {
                    _logger.LogWarning(
                        "事件处理实际耗时 {ElapsedMs}ms 显著超过软超时 {TimeoutMs}ms（处理器未响应 CancellationToken），" +
                        "期间并发闸槽位与去重 processing 态被一并占用。请让处理器协作式响应取消令牌。EventId: {EventId}, AppKey: {AppKey}",
                        elapsedMs, timeoutMs, eventData.EventId, appKey ?? "null");
                    FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: false, "timeout_overshoot");
                }

                _logger.LogWarning("事件处理超时: {EventType}, 事件ID: {EventId}, 超时时间: {TimeoutMs}ms, AppKey: {AppKey}",
                    eventData.EventType, eventData.EventId, timeoutMs, appKey ?? "null");
                FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: false, "timeout");

                processingException = oce;
                return (false, "Event handling timeout");
            }
        }
        catch (OperationCanceledException oce)
        {
            await RollbackDeduplicationAsync(eventData.EventId, appKey);
            _logger.LogWarning("事件处理被取消，EventId: {EventId}, AppKey: {AppKey}", eventData.EventId, appKey ?? "null");
            FeishuMetricsHelper.RecordEventOutcome(appKey ?? "unknown", eventData.EventType, success: false, "canceled");
            // P1-7/P2-1：AfterHandleAsync 需要可判别的终态（接口契约 null=成功，原始 OCE 无法表达"取消"这一类别）；
            // 但对外仍按 OCE 传播——取消语义不得被替换为普通异常。
            processingException = new Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException(
                "canceled", "事件处理被外部取消");
            throw;
        }
        catch (FeishuDeduplicationFatalException ex)
        {
            // WHF-02：语义不变——去重体系致命故障不是业务失败：不回滚、不写失败存储、上抛转 503
            _logger.LogError(ex, "去重服务致命故障，EventId: {EventId}, AppKey: {AppKey}", eventData.EventId, appKey ?? "null");
            processingException = new Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException("dedup_fatal", ex.Message);
            throw;
        }
        // 注意：此处<b>不得</b>再放一个 catch (FeishuRedisException when Server) 的"兼容"分支。
        // 该分支会把业务处理器内部因自用 Redis 而抛出的 Server 类异常也判为 WHF-02
        // （不回滚、不写失败存储、上抛 503）——正是决策 C 要消除的过宽过滤。
        // WHF-02 的对外 503 转换仍成立：FeishuDeduplicationFatalException 继承 FeishuRedisException，
        // FeishuMultiAppMiddleware 的 catch (FeishuRedisException { FailureKind: Server }) 继续命中。
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

            performanceStopwatch.Stop();
            _logger.LogDebug(
                "事件处理耗时: {EventType} {ElapsedMs}ms, EventId: {EventId}, AppKey: {AppKey}",
                eventData.EventType, performanceStopwatch.ElapsedMilliseconds, eventData.EventId, appKey ?? "null");
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
            // WHF-R2/B3：贯穿 CancellationToken，客户端断开时尽早取消验签链路
            return await _validator.ValidateHeaderSignatureAsync(
                request.Timestamp,
                request.Nonce,
                body,
                request.Signature,
                encryptKey!,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // R3-P1-6/WHF-16：客户端断开 / 宿主关停——交由中间件的 OCE 分支处理，
            // 禁止伪装成“验签失败 403”写向已中止连接。
            throw;
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
        catch (OperationCanceledException)
        {
            // R3-P2-2：解密超时（中间件 DecryptionTimeoutMs）此前被下方 catch(Exception) 吞成
            // null → 中间件映射 400（终态，飞书不重推）。解密超时是**可恢复**的服务端问题，
            // 语义应为 503 让飞书重投，而非"请求体非法 400"。
            // 同时：客户端断开的 OCE 也在此重抛——禁止写向已中止连接（WHF-16）。
            _logger.LogWarning("解密事件数据超时/被取消, AppKey: {AppKey}", _appKeyAccessor.CurrentAppKey ?? "null");
            FeishuMetricsHelper.RecordEventOutcome(_appKeyAccessor.CurrentAppKey ?? "unknown", "event_decryption", success: false, "decryption_timeout");
            throw;
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
        try
        {
            var result = await _deduplicator.TryMarkAsProcessingAsync(eventId, appKey, cancellationToken: cancellationToken);
            return (result.IsDuplicate, result.WasProcessing);
        }
        catch (FeishuRedisException ex) when (ex.FailureKind == FeishuRedisFailureKind.Server)
        {
            // P1-3/WHF-02：仅去重调用点包装为标记异常，业务侧 Redis 故障不被误吞
            throw new FeishuDeduplicationFatalException("事件去重检查遭遇 Redis 服务端致命故障", ex);
        }
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

            var tasks = handlerTypes
                .Select(t => ProcessAppHandlerSafelyAsync(t, eventData, appKey!, eventType, cancellationToken))
                .ToList();
            try
            {
                var all = Task.WhenAll(tasks);
                try
                {
                    await all;
                }
                catch
                {
                    // R3-P2-9：await Task.WhenAll 只重抛**首个**异常，故 `ex is AggregateException`
                    // 恒为 false（await 解包成首个 inner）→ 原"补记全部 InnerExceptions"分支永不命中，
                    // 注释与行为自相矛盾。改为从 `all.Exception` 取完整聚合。
                    // 仅在**多个**处理器同时失败时补记——单处理器失败已由 ProcessAppHandlerSafelyAsync
                    // 逐条记录，避免重复日志。
                    var inners = all.Exception?.InnerExceptions;
                    if (inners is { Count: > 1 })
                    {
                        foreach (var inner in inners)
                            _logger.LogError(inner, "应用 {AppKey} 专属处理器分发事件 {EventType} 失败（聚合明细）", appKey, eventType);
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用 {AppKey} 专属处理器分发事件 {EventType} 失败", appKey, eventType);
                throw;
            }

            // R3-P1-1：全部处理器都因 SupportedEventType 不匹配而跳过 → 事件被静默丢弃。
            // 此前该路径只有 LogDebug（生产 Information 级不可见），用户把 SupportedEventType 拼错时
            // 事件消失且无任何告警与指标。此处补 Warning + unhandled 指标，与全局分支口径一致
            // （unhandled 表示“未处理”，不是失败，故 success:true）。
            if (tasks.Count > 0 && tasks.All(t => !t.Result))
            {
                _logger.LogWarning(
                    "应用 {AppKey} 的 {Count} 个专属处理器均不匹配事件类型 {EventType}，事件已被忽略" +
                    "（请核对各处理器的 SupportedEventType 拼写；声明为空串表示处理该应用全部事件）",
                    appKey, handlerTypes.Count, eventType);
                FeishuMetricsHelper.RecordEventOutcome(appKey!, eventType, success: true, "unhandled");
            }
        }
        else
        {
            // WHF-09：未注册 eventType 门控——静默忽略（unhandled），不回退默认处理器兜底
            if (Options.IgnoreUnknownEventTypes && !_handlerFactory.IsHandlerRegistered(eventType))
            {
                // R3-P1-1：由 Debug 提升为 Warning——生产默认 Information 级看不到 Debug，
                // 未注册/拼错的事件类型会静默消失（全局分支此前连指标都无，只有日志）。
                _logger.LogWarning("事件类型 {EventType} 未注册处理器，已忽略（IgnoreUnknownEventTypes=true），请核对 SupportedEventType 拼写, AppKey: {AppKey}",
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
    /// 应用专属处理器安全分发：逐处理器日志，保留聚合重抛（at-least-once 不变）。
    /// </summary>
    /// <returns>
    /// 是否**实际处理**了该事件：<c>true</c> = 已调用 <c>HandleAsync</c>；
    /// <c>false</c> = 因 <see cref="IFeishuEventHandler.SupportedEventType"/> 不匹配而跳过。
    /// 调用方据此判定“全部处理器均跳过”（R3-P1-1：此时记 Warning + <c>unhandled</c> 指标）。
    /// </returns>
    /// <remarks>
    /// P1-2（R2）：按 <see cref="IFeishuEventHandler.SupportedEventType"/> 过滤——非空声明与当前
    /// 事件类型不符的处理器跳过（与全局工厂按 SupportedEventType 路由的语义对齐）；
    /// 显式声明空串表示处理该应用全部事件（与
    /// <see cref="Mud.Feishu.Abstractions.EventHandlers.DefaultFeishuEventHandler{T}"/>
    /// 默认约定一致）。拦截器分支不做该过滤（横切组件，与 WS 通道一致）。
    /// <para>
    /// R3-P1-3：处理器应**协作式响应** <paramref name="ct"/>。<c>EventHandlingTimeoutMs</c> 是
    /// **软超时**——只取消令牌，不中断处理器；不响应取消的处理器会持续占用本次请求的
    /// 并发闸槽位与去重 <c>processing</c> 态（等效于把该事件静默限流一段时间）。
    /// </para>
    /// </remarks>
    private async Task<bool> ProcessAppHandlerSafelyAsync(Type handlerType, EventData eventData, string appKey, string eventType, CancellationToken ct)
    {
        try
        {
            var handler = (IFeishuEventHandler)_serviceProvider.GetRequiredService(handlerType);

            if (handler.SupportedEventType is { Length: > 0 } supported &&
                !string.Equals(supported, eventType, StringComparison.Ordinal))
            {
                _logger.LogDebug(
                    "应用 {AppKey} 处理器 {HandlerType} 声明处理 {Supported}，跳过事件 {EventType}",
                    appKey, handlerType.Name, supported, eventType);
                return false;   // R3-P1-1：未匹配——调用方据此判定“全部跳过”
            }

            await handler.HandleAsync(eventData, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用专属处理器 {HandlerType} 处理事件 {EventId} 失败 (AppKey: {AppKey})",
                handlerType.Name, eventData.EventId, appKey);
            throw;
        }
    }

    /// <summary>
    /// 获取拦截器列表（支持按 AppKey 隔离）
    /// </summary>
    /// <remarks>
    /// R3-P2-3：组合策略由 <see cref="FeishuWebhookOptions.InterceptorFallbackMode"/> 决定
    /// （默认 <see cref="InterceptorFallbackMode.Merge"/>：全局先行，再应用专属）。
    /// 旧行为等价于 <see cref="InterceptorFallbackMode.AppOnly"/>——全局拦截器被完全丢弃。
    /// </remarks>
    private IEnumerable<IFeishuEventInterceptor> GetInterceptors(string? appKey)
    {
        var hasAppScoped = !string.IsNullOrEmpty(appKey) && _interceptorRegistry.HasInterceptors(appKey!);

        // 无应用专属拦截器 → 直接使用全局拦截器（唯一路径）
        if (!hasAppScoped)
        {
            _logger.LogDebug("使用全局拦截器（{Count} 个）, AppKey: {AppKey}",
                _interceptors.Length, appKey ?? "null");

            foreach (var interceptor in _interceptors)
                yield return interceptor;

            yield break;
        }

        var appScopedTypes = _interceptorRegistry.GetInterceptors(appKey!);
        _logger.LogDebug("应用 {AppKey} 的专属拦截器（{Count} 个）", appKey, appScopedTypes.Count);

        if (Options.InterceptorFallbackMode == InterceptorFallbackMode.AppOnly)
        {
            foreach (var interceptorType in appScopedTypes)
                yield return (IFeishuEventInterceptor)_serviceProvider.GetRequiredService(interceptorType);

            yield break;
        }

        var globalFirst = Options.InterceptorFallbackMode == InterceptorFallbackMode.Merge;

        // 按类型去重：同一类型既全局注册又 app 专属注册时只执行一次
        var emitted = new HashSet<Type>();
        var ordered = globalFirst
            ? _interceptors.Select(i => (IFeishuEventInterceptor?)i)
                .Concat(appScopedTypes.Select(t => (IFeishuEventInterceptor?)_serviceProvider.GetRequiredService(t)))
            : appScopedTypes.Select(t => (IFeishuEventInterceptor?)_serviceProvider.GetRequiredService(t))
                .Concat(_interceptors.Select(i => (IFeishuEventInterceptor?)i));

        foreach (var interceptor in ordered)
        {
            if (interceptor is null)
                continue;

            if (emitted.Add(interceptor.GetType()))
                yield return interceptor;
        }
    }
}