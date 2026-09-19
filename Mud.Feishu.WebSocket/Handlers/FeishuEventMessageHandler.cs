// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Interceptors;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.WebSocket.DataModels;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Handlers;

/// <summary>
/// 飞书事件消息处理器 - 处理飞书WebSocket推送的事件消息
/// </summary>
public class FeishuEventMessageHandler : JsonMessageHandler
{
    private readonly IFeishuEventHandlerFactory _eventHandlerFactory;
    private readonly IFeishuEventDeduplicator? _deduplicator;
    private readonly IFeishuEventInterceptor[] _interceptors;
    private readonly FeishuWebSocketOptions _options;
    // F7 修复：统一去重中间件（可选），优先于分离的 _deduplicator 使用，提供 EventId + SeqID 双重去重。
    private readonly IUnifiedDeduplicationMiddleware? _unifiedDedupMiddleware;

    /// <summary>
    /// 初始化飞书事件消息处理器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="eventHandlerFactory">事件处理器工厂</param>
    /// <param name="deduplicator">事件去重服务（可选）</param>
    /// <param name="interceptors">事件拦截器集合</param>
    /// <param name="options">WebSocket 配置选项</param>
    /// <param name="unifiedDedupMiddleware">统一去重中间件（可选，F7 修复引入）</param>
    public FeishuEventMessageHandler(
        ILogger<FeishuEventMessageHandler> logger,
        IFeishuEventHandlerFactory eventHandlerFactory,
        IFeishuEventDeduplicator? deduplicator,
        IFeishuEventInterceptor[]? interceptors,
        FeishuWebSocketOptions options,
        IUnifiedDeduplicationMiddleware? unifiedDedupMiddleware = null)
        : base(logger)
    {
        _eventHandlerFactory = eventHandlerFactory ?? throw new ArgumentNullException(nameof(eventHandlerFactory));
        _deduplicator = deduplicator;
        _interceptors = interceptors ?? Array.Empty<IFeishuEventInterceptor>();
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _unifiedDedupMiddleware = unifiedDedupMiddleware;
    }

    /// <inheritdoc/>
    public override bool CanHandle(string messageType)
    {
        return messageType.Equals("event", StringComparison.OrdinalIgnoreCase) ||
               messageType.Equals("event_callback", StringComparison.OrdinalIgnoreCase) ||
               messageType.Equals("binary_event", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public override async Task HandleAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("收到空事件消息");
                return;
            }

            // 尝试解析JSON以判断版本
            // 无效 JSON 时 JsonDocument.Parse 抛出 JsonException，在此阶段捕获并记录警告后返回，
            // 避免无效消息导致整个处理流程抛异常（P0-1: 外层 catch 会重抛，导致 ACK 500 + 服务端重发死循环）。
            System.Text.Json.JsonDocument? jsonDoc;
            try
            {
                jsonDoc = System.Text.Json.JsonDocument.Parse(message);
            }
            catch (System.Text.Json.JsonException jex)
            {
                var truncatedMsg = Mud.Feishu.Abstractions.Utilities.LogSanitizer.CleanMessage(message, 200);
                _logger.LogWarning(jex, "收到无效JSON事件消息 (长度: {Length}, 消息前200字符: {Message})",
                    message.Length, truncatedMsg);
                return;
            }
            using (jsonDoc)
            {
                var root = jsonDoc.RootElement;

                EventData eventData;

                // 检查是否为v2.0版本
                if (root.TryGetProperty("schema", out var schemaElement) &&
                    schemaElement.GetString() == "2.0")
                {
                    // v2.0版本解析
                    eventData = ParseV2Event(root);
                }
                else
                {
                    // v1.0版本解析
                    var eventMessage = SafeDeserialize<EventMessage>(message);
                    if (eventMessage?.Data == null)
                    {
                        var truncatedV1Msg = message.Length > 200 ? message.Substring(0, 200) + "..." : message;
                        _logger.LogWarning("无法解析v1.0事件消息 (长度: {Length}): {Message}", message.Length, truncatedV1Msg);
                        return;
                    }
                    eventData = eventMessage.Data;
                }

                if (string.IsNullOrEmpty(eventData.EventType))
                {
                    _logger.LogWarning("事件类型为空: {EventId}", eventData.EventId);
                    return;
                }

                _logger.LogDebug("收到飞书事件: {EventType}, EventId: {EventId}",
                    eventData.EventType, eventData.EventId);

                // P1-2：空 EventId 无法有效去重（null 跳过 / 空串同键碰撞）——fail-closed，与 Webhook WHF-05 对齐。
                // 丢弃而非 ACK 500：空 ID 属确定性畸形，重发不能自愈。
                if (string.IsNullOrEmpty(eventData.EventId))
                {
                    if (_options.RejectEmptyEventIds)
                    {
                        _logger.LogWarning("事件 EventId 为空，RejectEmptyEventIds=true 拒绝处理: {EventType}", eventData.EventType);
                        FeishuMetricsHelper.RecordEventOutcome(_options.AppKey, eventData.EventType, success: false, "empty_event_id");
                        return;
                    }
                    _logger.LogWarning("事件 EventId 为空，已配置放行——本次处理不具备去重保护: {EventType}", eventData.EventType);
                }

                // 去重检查
                // F7 修复：优先使用统一去重中间件（EventId + SeqID 双重去重），
                // 未注入时回退到分离的 _deduplicator 路径，保持向后兼容。
                bool shouldSkip = false;

                if (_unifiedDedupMiddleware != null)
                {
                    // WebSocket 文本事件路径无 SeqID 上下文，统一中间件内部会跳过 SeqID 检查。
                    var unifiedResult = await _unifiedDedupMiddleware.CheckAsync(eventData.EventId, seqId: null, cancellationToken);
                    if (unifiedResult.ShouldSkip)
                    {
                        _logger.LogDebug("事件 {EventId} 被统一去重中间件跳过 (类型: {IdentifierType}, 原因: {Reason})",
                            eventData.EventId, unifiedResult.IdentifierType, unifiedResult.Reason);
                        shouldSkip = true;
                        FeishuMetricsHelper.RecordEventDeduplication(_options.AppKey,
                            unifiedResult.IdentifierType.ToString().ToLowerInvariant(), hit: true);
                    }
                }
                else if (_deduplicator != null)
                {
                    // P2-12：WS 去重键带 AppKey，与 MemoryDeduplicator.GetCacheKey 多应用隔离口径一致
                    var dedupResult = await _deduplicator.TryMarkAsProcessingAsync(eventData.EventId, _options.AppKey, cancellationToken: cancellationToken);
                    if (dedupResult.IsDuplicate)
                    {
                        _logger.LogDebug("事件 {EventId} 已在处理中或已处理，跳过 (WasProcessing: {WasProcessing}, Status: {Status})",
                            eventData.EventId, dedupResult.WasProcessing, dedupResult.Status);
                        shouldSkip = true;
                        FeishuMetricsHelper.RecordEventDeduplication(_options.AppKey, "event_id", hit: true);
                    }
                }

                if (!shouldSkip)
                {
                    Exception? processingException = null;
                    bool isInterrupted = false;

                    using (FeishuMetricsHelper.RecordEventHandling(_options.AppKey, eventData.EventType))
                    {
                        try
                        {
                            // 前置拦截器
                            foreach (var interceptor in _interceptors)
                            {
                                var shouldContinue = await interceptor.BeforeHandleAsync(eventData.EventType, eventData, cancellationToken);
                                if (!shouldContinue)
                                {
                                    _logger.LogWarning("事件被拦截器中断: {EventType}, EventId: {EventId}, Interceptor: {InterceptorType}",
                                        eventData.EventType, eventData.EventId, interceptor.GetType().Name);
                                    isInterrupted = true;

                                    // P1-7：被拦截 = 干净回滚（与 Webhook"不进去重即可重试"对齐），
                                    // 服务端重发后拦截器重新决策（retry-until-accept）
                                    if (_unifiedDedupMiddleware != null)
                                        await _unifiedDedupMiddleware.RollbackAsync(eventData.EventId, seqId: null, cancellationToken);
                                    else if (_deduplicator != null)
                                        await _deduplicator.RollbackProcessingAsync(eventData.EventId, _options.AppKey, cancellationToken);

                                    FeishuMetricsHelper.RecordEventOutcome(_options.AppKey, eventData.EventType, success: false, "intercepted");
                                    processingException = new EventHandlingOutcomeException(
                                        "intercepted", $"事件被 {interceptor.GetType().Name} 拦截");
                                    throw processingException; // → ACK 500 → 服务端重发 → 与 Webhook 语义一致
                                }
                            }

                            if (!isInterrupted)
                            {
                                // 使用事件处理器工厂并行处理事件
                                await _eventHandlerFactory.HandleEventParallelAsync(eventData.EventType, eventData, cancellationToken);

                                // WHF-07（WS 对齐）：业务分发已成功——Mark 失败禁止回滚
                                //（回滚将导致服务端重发后重复消费）。保留 processing 态，由 ProcessingTimeout/TTL 兜底。
                                try
                                {
                                    if (_unifiedDedupMiddleware != null)
                                        await _unifiedDedupMiddleware.MarkCompletedAsync(eventData.EventId, seqId: null, cancellationToken);
                                    else if (_deduplicator != null)
                                        await _deduplicator.MarkAsCompletedAsync(eventData.EventId, _options.AppKey, cancellationToken);
                                }
                                catch (Exception markEx)
                                {
                                    _logger.LogWarning(markEx,
                                        "事件 {EventId} 处理成功但完成标记失败，保留 processing 态等待超时恢复",
                                        eventData.EventId);
                                    FeishuMetricsHelper.RecordEventOutcome(_options.AppKey, eventData.EventType, success: true, "mark_completed_failed");
                                }

                                // 记录事件处理成功
                                FeishuMetricsHelper.RecordEventOutcome(_options.AppKey, eventData.EventType, success: true);
                            }
                        }
                        catch (EventHandlingOutcomeException outcomeEx)
                        {
                            // P1-7：拦截等终态已在拦截点完成回滚与指标，此处不得二次回滚/二次记 failure
                            processingException = outcomeEx;
                            _logger.LogWarning(outcomeEx, "事件处理终态: {OutcomeKind}", outcomeEx.OutcomeKind);
                            throw;
                        }
                        catch (Exception ex)
                        {
                            processingException = ex;

                            // 处理失败，回滚处理中状态
                            if (_unifiedDedupMiddleware != null)
                            {
                                await _unifiedDedupMiddleware.RollbackAsync(eventData.EventId, seqId: null, cancellationToken);
                            }
                            else if (_deduplicator != null)
                            {
                                await _deduplicator.RollbackProcessingAsync(eventData.EventId, _options.AppKey, cancellationToken);
                            }

                            // 记录事件处理失败
                            FeishuMetricsHelper.RecordEventOutcome(_options.AppKey, eventData.EventType, success: false, ex.GetType().Name);
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
                }
            } // end using (jsonDoc)
        }
        catch (OperationCanceledException)
        {
            // P2-1：取消必须传播以触发 ACK 500 / 停机语义
            FeishuMetricsHelper.RecordEventOutcome(_options.AppKey, "unknown", success: false, "canceled");
            throw;
        }
        catch (EventHandlingOutcomeException outcomeEx)
        {
            // P1-7：拦截/取消等终态已在拦截点记录指标，此处仅补充日志，避免通用 catch 二次记 failure
            _logger.LogWarning(outcomeEx, "事件处理终态: {OutcomeKind}", outcomeEx.OutcomeKind);
            throw;
        }
        catch (Exception ex)
        {
            // P0-1 修复（WS-01）：此前外层 catch 吞掉异常后正常返回，
            // 导致 MessageRouter.RouteBinaryMessageWithResultAsync 恒返回 true、
            // BinaryMessageProcessor 回 ACK code=200，飞书服务端不再重发，事件永久丢失。
            // 现重抛异常，让 MessageRouter 感知失败并回 ACK code=500，服务端将重发。
            // 同时解决 P2-11：日志仅记录结构化字段 + 消息前 200 字符，避免全文入日志。
            var truncatedMsg = Mud.Feishu.Abstractions.Utilities.LogSanitizer.CleanMessage(message, 200);
            _logger.LogError(ex, "处理飞书事件消息时发生错误 (消息长度: {Length}, 消息前200字符: {Message})",
                message.Length, truncatedMsg);
            throw;
        }
    }

    /// <summary>
    /// 解析v2.0版本的事件
    /// </summary>
    private EventData ParseV2Event(JsonElement root)
    {
        var eventData = new EventData();

        // 解析header
        if (root.TryGetProperty("header", out var headerElement))
        {
            // 构建 Header 对象，保留完整的 v2.0 header 数据
            var header = new FeishuEventHeader { Schema = "2.0" };

            if (headerElement.TryGetProperty("event_id", out var eventIdElement))
            {
                var eventId = eventIdElement.GetString() ?? string.Empty;
                header.EventId = eventId;
                eventData.EventId = eventId;
            }

            if (headerElement.TryGetProperty("event_type", out var eventTypeElement))
            {
                var eventType = eventTypeElement.GetString() ?? string.Empty;
                header.EventType = eventType;
                eventData.EventType = eventType;
            }

            if (headerElement.TryGetProperty("create_time", out var createTimeElement))
            {
                header.CreateTime = createTimeElement.ValueKind == JsonValueKind.String
                    ? createTimeElement.GetString()
                    : createTimeElement.TryGetInt64(out var ct) ? ct.ToString() : null;

                if (createTimeElement.ValueKind == JsonValueKind.String &&
                    long.TryParse(createTimeElement.GetString(), out var createTimeLong))
                {
                    eventData.CreateTime = createTimeLong / 1000; // 转换为秒
                }
                else if (createTimeElement.TryGetInt64(out var createTimeInt))
                {
                    eventData.CreateTime = createTimeInt / 1000;
                }
            }

            if (headerElement.TryGetProperty("token", out var tokenElement))
                header.Token = tokenElement.GetString();

            if (headerElement.TryGetProperty("tenant_key", out var tenantKeyElement))
            {
                header.TenantKey = tenantKeyElement.GetString() ?? string.Empty;
                eventData.TenantKey = header.TenantKey;
            }

            if (headerElement.TryGetProperty("app_id", out var appIdElement))
            {
                header.AppId = appIdElement.GetString() ?? string.Empty;
                eventData.AppId = header.AppId;
            }

            // 设置完整 Header
            eventData.Header = header;
        }

        // 解析 schema（在 header 之外）
        if (root.TryGetProperty("schema", out var schemaElement))
        {
            eventData.Header ??= new FeishuEventHeader();
            eventData.Header.Schema = schemaElement.GetString();
        }

        // 解析event
        if (root.TryGetProperty("event", out var eventElement))
        {
            // P1-10 修复：eventElement 隶属于 using var jsonDoc 所租用的 ArrayPool 缓冲，
            // 直接赋值会让 JsonElement 逃逸出 JsonDocument 生命周期：文档 Dispose 后缓冲被归还池中，
            // 任何在 HandleAsync 返回之后读取 eventData.Event 的代码（例如把事件排入后台队列延迟处理）
            // 都会读到已被其它 JSON 解析覆写的内存，造成静默数据损坏或抛 ObjectDisposedException。
            // Clone() 会分配独立的文档副本，脱离原生命周期。
            eventData.Event = eventElement.Clone();
        }

        return eventData;
    }
}
