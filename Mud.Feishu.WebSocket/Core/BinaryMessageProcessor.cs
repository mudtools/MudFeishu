// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 二进制消息处理器 - 负责处理二进制数据的增量接收和解析
/// </summary>
public class BinaryMessageProcessor : IDisposable
{
    private readonly ILogger<BinaryMessageProcessor> _logger;
    private readonly FeishuWebSocketOptions _options;
    private MemoryStream? _binaryDataStream;
    private readonly object _binaryDataStreamLock = new object();
    private DateTime _binaryDataReceiveStartTime = DateTime.MinValue;
    private bool _disposed = false;
    private readonly MessageRouter? _messageRouter;
    private readonly WebSocketConnectionManager? _connectionManager;
    private readonly List<Task> _activeProcessingTasks = new();
    private readonly IFeishuSeqIDDeduplicator? _seqIdDeduplicator;
    private readonly MessageSequenceValidator? _sequenceValidator;
    private readonly IUnifiedDeduplicationMiddleware? _unifiedDeduplicationMiddleware;

    /// <summary>
    /// 大对象阈值（字节），超过此阈值使用 ToArray() 避免 GetBuffer() 的额外数据
    /// </summary>
    private const int LargeObjectThreshold = 85_000;

    /// <summary>
    /// 二进制消息接收事件
    /// </summary>
    public event EventHandler<WebSocketBinaryMessageEventArgs>? BinaryMessageReceived;

    /// <summary>
    /// 错误事件
    /// </summary>
    public event EventHandler<WebSocketErrorEventArgs>? Error;

    /// <summary>
    /// 收到 Pong 控制帧事件，携带服务端下发的 ClientConfig（如果存在）
    /// <para>handleControlFrame → PONG → configure(ClientConfig)</para>
    /// </summary>
    public event EventHandler<ClientConfigInfo?>? PongReceived;

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public BinaryMessageProcessor(
        ILogger<BinaryMessageProcessor> logger,
        WebSocketConnectionManager? webSocketConnectionManager,
        FeishuWebSocketOptions options,
        MessageRouter messageRouter,
        IFeishuSeqIDDeduplicator? seqIdDeduplicator = null,
        MessageSequenceValidator? sequenceValidator = null,
        IUnifiedDeduplicationMiddleware? unifiedDeduplicationMiddleware = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FeishuWebSocketOptions();
        _connectionManager = webSocketConnectionManager ?? throw new ArgumentNullException(nameof(_connectionManager));
        _messageRouter = messageRouter ?? throw new ArgumentNullException(nameof(messageRouter));
        _seqIdDeduplicator = seqIdDeduplicator;
        _sequenceValidator = sequenceValidator;
        _unifiedDeduplicationMiddleware = unifiedDeduplicationMiddleware;
    }

    /// <summary>
    /// 处理二进制数据
    /// </summary>
    public async Task ProcessBinaryDataAsync(byte[] data, int offset, int count, bool endOfMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            lock (_binaryDataStreamLock)
            {
                // 如果是新消息的开始，初始化内存流
                if (_binaryDataStream == null)
                {
                    _binaryDataStream = new MemoryStream();
                    _binaryDataReceiveStartTime = DateTime.UtcNow;

                    if (_options.EnableLogging)
                        _logger.LogDebug("开始接收新的二进制消息");
                }

                // 预先检查数据大小限制（写入前检查，防止内存溢出）
                var currentLength = _binaryDataStream.Length;
                var newLength = currentLength + count;
                if (newLength > _options.MessageSizeLimits.MaxBinaryMessageSize)
                {
                    var errorMessage = $"二进制消息大小超过限制 ({newLength} > {_options.MessageSizeLimits.MaxBinaryMessageSize})";
                    _logger.LogError(errorMessage);

                    // 清理当前数据流
                    _binaryDataStream.Dispose();
                    _binaryDataStream = null;

                    // 触发错误事件
                    OnError(errorMessage, "MessageSizeExceeded");
                    return;
                }

                // 写入数据片段
                _binaryDataStream.Write(data, offset, count);

                // 如果消息接收完成
                if (endOfMessage)
                {
                    // 使用 GetBuffer() 获取内部缓冲区引用，避免 ToArray() 的复制
                    // 注意：缓冲区长度可能大于实际数据长度，需要使用 Length 属性
                    var buffer = _binaryDataStream.GetBuffer();
                    var actualLength = (int)_binaryDataStream.Length;
                    var receiveDuration = DateTime.UtcNow - _binaryDataReceiveStartTime;

                    if (_options.EnableLogging)
                        _logger.LogInformation("二进制消息接收完成，大小: {Size} 字节，耗时: {Duration}ms",
                            actualLength, receiveDuration.TotalMilliseconds);

                    byte[] completeData;
                    if (actualLength > LargeObjectThreshold)
                    {
                        completeData = _binaryDataStream.ToArray();
                    }
                    else
                    {
                        completeData = new byte[actualLength];
                        Buffer.BlockCopy(buffer, 0, completeData, 0, actualLength);
                    }

                    // 异步处理完整的二进制消息并跟踪任务
                    var processingTask = Task.Run(async () =>
                    {
                        await ProcessCompleteBinaryMessageAsync(completeData, cancellationToken);
                    }, cancellationToken);

                    lock (_activeProcessingTasks)
                    {
                        _activeProcessingTasks.Add(processingTask);
                    }

                    // 清理完成后从列表中移除
                    _ = processingTask.ContinueWith(t =>
                    {
                        lock (_activeProcessingTasks)
                        {
                            _activeProcessingTasks.Remove(t);
                        }
                    }, CancellationToken.None);

                    // 清理资源
                    _binaryDataStream.Dispose();
                    _binaryDataStream = null;
                }
                else
                {
                    if (_options.EnableLogging)
                        _logger.LogDebug("已接收二进制消息片段，当前总大小: {Size} 字节", _binaryDataStream.Length);
                }
            }
        }
        catch (Exception ex)
        {
            // 发生异常时清理资源
            lock (_binaryDataStreamLock)
            {
                _binaryDataStream?.Dispose();
                _binaryDataStream = null;
            }

            if (_options.EnableLogging)
                _logger.LogError(ex, "处理二进制消息时发生错误");
            OnError($"处理二进制消息时发生错误: {ex.Message}", ex.GetType().Name);
        }
    }

    /// <summary>
    /// 处理完整的二进制消息
    /// </summary>
    private async Task ProcessCompleteBinaryMessageAsync(byte[] completeData, CancellationToken cancellationToken)
    {
        try
        {
            var eventArgs = new WebSocketBinaryMessageEventArgs
            {
                Data = completeData ?? Array.Empty<byte>(),
                ReceiveEndTime = DateTime.UtcNow
            };

            if (completeData == null || completeData.Length == 0)
            {
                if (_options.EnableLogging)
                    _logger.LogWarning("接收到空的二进制消息");
                eventArgs.ParseError = "接收到空的二进制消息";
                BinaryMessageReceived?.Invoke(this, eventArgs);
                return;
            }

            // 尝试解析为 Frame 对象
            ulong? markedSeqId = null; // 跟踪已标记的 SeqID，用于失败时回滚
            try
            {
                if (_options.EnableLogging)
                    _logger.LogDebug("尝试使用 ProtoBuf 反序列化二进制消息");

                // 使用 Memory<byte> 的 Pin 方法或创建 MemoryStream
                // 对于 netstandard2.0
#if NETSTANDARD2_0
                // 使用 Buffer.BlockCopy 替代 MemoryMarshal
                var dataArray = new byte[completeData.Length];
                Buffer.BlockCopy(completeData, 0, dataArray, 0, completeData.Length);
                var frame = ProtoBuf.Serializer.Deserialize<EventProtoData>(new MemoryStream(dataArray));
#else
                // 对于 .NET Core 2.1+
                var span = new ReadOnlySpan<byte>(completeData);
                var frame = ProtoBuf.Serializer.Deserialize<EventProtoData>(span);
#endif

                if (_options.EnableLogging)
                    _logger.LogDebug("成功反序列化为 Frame 对象: Service={Service}, Method={Method}, PayloadType={PayloadType}, SeqID={SeqID}",
                        frame.Service, frame.Method, frame.PayloadType, frame.SeqID);

                // 区分 CONTROL 帧和 DATA 帧进行不同处理（
                if (FrameBuilder.IsControlFrame(frame))
                {
                    HandleControlFrame(frame, eventArgs);
                    return;
                }

                // 消息序号验证
                if (_sequenceValidator != null)
                {
                    var validationResult = _sequenceValidator.ValidateSequence(frame.SeqID, eventArgs.MessageType);
                    if (validationResult == SequenceValidationResult.Duplicate ||
                        validationResult == SequenceValidationResult.Rollback)
                    {
                        if (_options.EnableLogging)
                            _logger.LogWarning("消息序号验证失败: {ValidationResult}, SeqID={SeqID}", validationResult, frame.SeqID);
                        eventArgs.SkipReason = $"消息序号验证失败: {validationResult}";
                        BinaryMessageReceived?.Invoke(this, eventArgs);
                        await SendAckMessageAsync(frame, true, cancellationToken);
                        return;
                    }
                }

                string? extractedEventId = null;
                if (frame?.Payload != null)
                {
                    var jsonPayload = Encoding.UTF8.GetString(frame.Payload);
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(jsonPayload);
                        if (jsonDoc.RootElement.TryGetProperty("event_id", out var eventIdElement))
                        {
                            extractedEventId = eventIdElement.GetString();
                        }
                    }
                    catch
                    {
                    }
                }

                if (_unifiedDeduplicationMiddleware != null && (!string.IsNullOrEmpty(extractedEventId) || frame.SeqID > 0))
                {
                    var dedupResult = await _unifiedDeduplicationMiddleware.CheckAsync(extractedEventId, frame.SeqID, cancellationToken);
                    if (dedupResult.ShouldSkip)
                    {
                        if (_options.EnableLogging)
                            _logger.LogDebug("统一去重检查跳过: {Reason}, EventId={EventId}, SeqId={SeqId}",
                                dedupResult.Reason, extractedEventId, frame.SeqID);
                        eventArgs.SkipReason = dedupResult.Reason;
                        BinaryMessageReceived?.Invoke(this, eventArgs);
                        await SendAckMessageAsync(frame, true, cancellationToken);
                        return;
                    }
                }
                else
                {
                    if (_seqIdDeduplicator != null && await _seqIdDeduplicator.TryMarkAsProcessedAsync(frame.SeqID))
                    {
                        if (_options.EnableLogging)
                            _logger.LogDebug("SeqID {SeqID} 已处理过，跳过", frame.SeqID);
                        eventArgs.SkipReason = $"SeqID {frame.SeqID} 已处理过";
                        BinaryMessageReceived?.Invoke(this, eventArgs);
                        await SendAckMessageAsync(frame, true, cancellationToken);
                        return;
                    }
                    // 记录已标记的 SeqID，用于处理失败时回滚
                    if (_seqIdDeduplicator != null)
                    {
                        markedSeqId = frame.SeqID;
                    }
                }

                if (frame?.Payload != null)
                {
                    var jsonPayload = System.Text.Encoding.UTF8.GetString(frame.Payload);
                    eventArgs.JsonContent = jsonPayload;
                    eventArgs.MessageType = "Frame";

                    if (_options.EnableLogging)
                        _logger.LogDebug("成功解析 Frame Payload 为 JSON 内容（长度: {PayloadLength}）", jsonPayload.Length);

                    BinaryMessageReceived?.Invoke(this, eventArgs);

                    if (_messageRouter != null)
                    {
                        if (_options.EnableLogging)
                            _logger.LogDebug("路由二进制转换的JSON消息到MessageRouter");
                        await _messageRouter.RouteBinaryMessageAsync(jsonPayload, "Frame", cancellationToken);
                    }

                    if (eventArgs.ProcessingTask != null)
                    {
                        try
                        {
                            await eventArgs.ProcessingTask;
                        }
                        catch (Exception ex)
                        {
                            eventArgs.ProcessingSuccess = false;
                            eventArgs.ProcessingException = ex;
                            _logger.LogError(ex, "事件处理器处理失败: EventId={EventId}, SeqId={SeqId}", extractedEventId, frame.SeqID);
                        }
                    }

                    if (_unifiedDeduplicationMiddleware != null && (!string.IsNullOrEmpty(extractedEventId) || frame.SeqID > 0))
                    {
                        if (eventArgs.ProcessingSuccess)
                        {
                            await _unifiedDeduplicationMiddleware.MarkCompletedAsync(extractedEventId, frame.SeqID, cancellationToken);
                        }
                        else
                        {
                            await _unifiedDeduplicationMiddleware.RollbackAsync(extractedEventId, frame.SeqID, cancellationToken);
                        }
                    }

                    await SendAckMessageAsync(frame, eventArgs.ProcessingSuccess, cancellationToken);
                }
                else
                {
                    if (_options.EnableLogging)
                        _logger.LogWarning("Frame 解析成功但 Payload 为空");
                    eventArgs.ParseError = "Frame 解析成功但 Payload 为空";
                    BinaryMessageReceived?.Invoke(this, eventArgs);

                    if (_unifiedDeduplicationMiddleware != null && (!string.IsNullOrEmpty(extractedEventId) || frame.SeqID > 0))
                    {
                        await _unifiedDeduplicationMiddleware.RollbackAsync(extractedEventId, frame.SeqID, cancellationToken);
                    }

                    await SendAckMessageAsync(frame, false, cancellationToken);
                }
            }
            catch (ProtoBuf.ProtoException ex)
            {
                _logger.LogError(ex, "ProtoBuf 反序列化失败，尝试直接解析为 JSON");

                eventArgs.ParseError = $"ProtoBuf 反序列化失败: {ex.Message}";

                var jsonString = Encoding.UTF8.GetString(completeData);
                if (!string.IsNullOrWhiteSpace(jsonString))
                {
                    eventArgs.JsonContent = jsonString;
                    eventArgs.MessageType = "JSON_Fallback";
                    BinaryMessageReceived?.Invoke(this, eventArgs);

                    if (_messageRouter != null)
                    {
                        if (_options.EnableLogging)
                            _logger.LogDebug("路由二进制转换的JSON消息到MessageRouter (Fallback模式)");
                        await _messageRouter.RouteBinaryMessageAsync(jsonString, "JSON_Fallback", cancellationToken);
                    }
                }
                else
                {
                    BinaryMessageReceived?.Invoke(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理完整二进制消息时发生错误");
                eventArgs.ParseError = $"处理完整二进制消息时发生错误: {ex.Message}";
                BinaryMessageReceived?.Invoke(this, eventArgs);

                // 回滚 SeqID 去重状态，允许服务端重发时重新处理
                if (markedSeqId.HasValue && _seqIdDeduplicator != null)
                {
                    try
                    {
                        await _seqIdDeduplicator.RollbackAsync(markedSeqId.Value);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "回滚 SeqID {SeqId} 时发生错误", markedSeqId.Value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理完整二进制消息时发生未知错误");
            OnError($"处理完整二进制消息时发生未知错误: {ex.Message}", ex.GetType().Name);
        }
    }

    /// <summary>
    /// 处理控制帧（CONTROL, Method=0）
    /// <para>Ping: 忽略（服务端不应发送 Ping 到客户端）</para>
    /// <para>Pong: 解析 Payload 中的 ClientConfig，触发 PongReceived 事件</para>
    /// </summary>
    /// <param name="frame">ProtoBuf 控制帧</param>
    /// <param name="eventArgs">二进制消息事件参数</param>
    private void HandleControlFrame(EventProtoData frame, WebSocketBinaryMessageEventArgs eventArgs)
    {
        var messageType = frame.MessageType;

        eventArgs.MessageType = $"Control_{messageType}";

        switch (messageType)
        {
            case MessageType.Ping:
                // 服务端发送的 Ping，忽略（对照 Java SDK: case PING: return;）
                if (_options.EnableLogging)
                    _logger.LogDebug("收到服务端 Ping 控制帧，已忽略");
                eventArgs.SkipReason = "服务端 Ping 控制帧，无需处理";
                BinaryMessageReceived?.Invoke(this, eventArgs);
                break;

            case MessageType.Pong:
                // 解析 Pong 中的 ClientConfig 并触发事件（对照 Java SDK: case PONG: configure(conf);）
                // Pong 接收日志不受 EnableLogging 限制，便于诊断心跳问题
                _logger.LogDebug("收到 Pong 控制帧，解析 ClientConfig...");

                var config = FrameBuilder.ExtractClientConfig(frame, _logger);
                eventArgs.JsonContent = frame.Payload != null ? Encoding.UTF8.GetString(frame.Payload) : null;
                BinaryMessageReceived?.Invoke(this, eventArgs);

                // 通知 HeartbeatManager 重置超时并应用动态配置
                PongReceived?.Invoke(this, config);
                break;

            default:
                if (_options.EnableLogging)
                    _logger.LogDebug("收到未知控制帧类型: {MessageType}", messageType);
                eventArgs.SkipReason = $"未知控制帧类型: {messageType}";
                BinaryMessageReceived?.Invoke(this, eventArgs);
                break;
        }
    }

    private async Task SendAckMessageAsync(EventProtoData? eventProtoData, bool success, CancellationToken cancellationToken)
    {
        if (eventProtoData == null)
            return;

        // 按照飞书 WebSocket 协议（Java SDK 对照）构造 ACK 响应
        // Response 格式: {"code": 200/500, "headers": {}, "data": "base64-encoded"}
        // 同时在 Frame headers 中添加 biz_rt（业务处理耗时）
        var stopwatch = Stopwatch.StartNew();

        var responseData = System.Array.Empty<byte>();

        var responseObj = new
        {
            code = success ? 200 : 500,
            headers = (Dictionary<string, string>?)null,
            data = Convert.ToBase64String(responseData)
        };

        var ackJson = JsonSerializer.Serialize(responseObj, JsonOptions.Default);
        var ackPayload = Encoding.UTF8.GetBytes(ackJson);

        stopwatch.Stop();
        var elapsedMs = (long)stopwatch.Elapsed.TotalMilliseconds;

        try
        {
            // 克隆 Frame 对象避免修改原始数据
            var headerCount = eventProtoData.Headers?.Length ?? 0;
            var newHeaders = new ProtoHeader[headerCount + 1];
            if (eventProtoData.Headers != null)
            {
                for (int i = 0; i < eventProtoData.Headers.Length; i++)
                {
                    newHeaders[i] = new ProtoHeader
                    {
                        Key = eventProtoData.Headers[i].Key,
                        Value = eventProtoData.Headers[i].Value
                    };
                }
            }
            newHeaders[headerCount] = new ProtoHeader { Key = "biz_rt", Value = elapsedMs.ToString() };

            var ackFrame = new EventProtoData
            {
                Service = eventProtoData.Service,
                Method = eventProtoData.Method,
                SeqID = eventProtoData.SeqID,
                LogID = eventProtoData.LogID,
                LogIDNew = eventProtoData.LogIDNew,
                Payload = ackPayload,
                PayloadEncoding = "json",
                PayloadType = "ack",
                Headers = newHeaders
            };

            using var messageStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(messageStream, ackFrame);

            if (messageStream.TryGetBuffer(out var arraySegment) && _connectionManager != null)
            {
                await _connectionManager.SendBinaryMessageAsync(arraySegment, cancellationToken);
                if (_options.EnableLogging)
                    _logger.LogDebug("已发送ACK消息: code={Code}, biz_rt={BizRt}ms", responseObj.code, elapsedMs);
            }
        }
        catch (Exception x)
        {
            _logger.LogError(x, "发送ACK消息时发生错误");
            OnError($"发送ACK消息时发生错误: {x.Message}", x.GetType().Name);
        }
    }

    /// <summary>
    /// 触发错误事件
    /// </summary>
    private void OnError(string errorMessage, string errorType)
    {
        Error?.Invoke(this, new WebSocketErrorEventArgs
        {
            ErrorMessage = errorMessage,
            ErrorType = errorType,
            IsNetworkError = false
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            Task[] tasksToWait;
            lock (_activeProcessingTasks)
            {
                tasksToWait = _activeProcessingTasks.ToArray();
            }

            // 使用异步等待而不是同步等待，避免死锁风险
            // 注意：在同步Dispose方法中无法使用await，这里使用Wait但设置较短超时
            // 如果超时，只记录警告而不阻塞
            try
            {
                if (!Task.WaitAll(tasksToWait, TimeSpan.FromSeconds(5)))
                {
                    _logger.LogWarning("等待所有处理任务完成超时（5秒），部分任务可能仍在运行");
                }
            }
            catch (AggregateException ex)
            {
                _logger.LogError(ex, "等待处理任务时发生错误");
            }

            lock (_binaryDataStreamLock)
            {
                _binaryDataStream?.Dispose();
                _binaryDataStream = null;
            }

            lock (_activeProcessingTasks)
            {
                _activeProcessingTasks.Clear();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放二进制处理器资源时发生错误");
        }
        finally
        {
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}