// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.SocketEventArgs;
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

    public event EventHandler<WebSocketBinaryMessageEventArgs>? BinaryMessageReceived;
    public event EventHandler<WebSocketErrorEventArgs>? Error;

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public BinaryMessageProcessor(
        ILogger<BinaryMessageProcessor> logger,
        WebSocketConnectionManager? webSocketConnectionManager,
        FeishuWebSocketOptions options,
        MessageRouter messageRouter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FeishuWebSocketOptions();
        _connectionManager = webSocketConnectionManager ?? throw new ArgumentNullException(nameof(_connectionManager));
        _messageRouter = messageRouter ?? throw new ArgumentNullException(nameof(messageRouter));
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

                // 写入数据片段
                _binaryDataStream.Write(data, offset, count);

                // 检查数据大小限制
                if (_binaryDataStream.Length > _options.MaxBinaryMessageSize)
                {
                    var errorMessage = $"二进制消息大小超过限制 ({_binaryDataStream.Length} > {_options.MaxBinaryMessageSize})";
                    _logger.LogError(errorMessage);

                    // 清理当前数据流
                    _binaryDataStream.Dispose();
                    _binaryDataStream = null;

                    // 触发错误事件
                    OnError(errorMessage, "MessageSizeExceeded");
                    return;
                }

                // 如果消息接收完成
                if (endOfMessage)
                {
                    var completeData = _binaryDataStream.ToArray();
                    var receiveDuration = DateTime.UtcNow - _binaryDataReceiveStartTime;

                    if (_options.EnableLogging)
                        _logger.LogInformation("二进制消息接收完成，大小: {Size} 字节，耗时: {Duration}ms",
                            completeData.Length, receiveDuration.TotalMilliseconds);

                    // 异步处理完整的二进制消息
                    _ = Task.Run(async () =>
                    {
                        await ProcessCompleteBinaryMessageAsync(completeData, cancellationToken);
                    }, cancellationToken);

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
            try
            {
                if (_options.EnableLogging)
                    _logger.LogDebug("尝试使用 ProtoBuf 反序列化二进制消息");
                var frame = ProtoBuf.Serializer.Deserialize<EventProtoData>(new MemoryStream(completeData));

                if (_options.EnableLogging)
                    _logger.LogDebug("成功反序列化为 Frame 对象: Service={Service}, Method={Method}, PayloadType={PayloadType}",
                        frame.Service, frame.Method, frame.PayloadType);

                if (frame?.Payload != null)
                {
                    // 解析 JSON payload
                    var jsonPayload = System.Text.Encoding.UTF8.GetString(frame.Payload);
                    eventArgs.JsonContent = jsonPayload;
                    eventArgs.MessageType = "Frame";

                    if (_options.EnableLogging)
                        _logger.LogDebug("成功解析 Frame Payload 为 JSON 内容:{jsonPayload}", jsonPayload);

                    // 触发事件，让外部处理JSON payload
                    BinaryMessageReceived?.Invoke(this, eventArgs);

                    // 如果设置了MessageRouter，则路由JSON内容
                    if (_messageRouter != null)
                    {
                        if (_options.EnableLogging)
                            _logger.LogDebug("路由二进制转换的JSON消息到MessageRouter");
                        await _messageRouter.RouteBinaryMessageAsync(jsonPayload, "Frame", cancellationToken);
                    }

                    await SendAckMessageAsync(frame, true, cancellationToken);
                }
                else
                {
                    if (_options.EnableLogging)
                        _logger.LogWarning("Frame 解析成功但 Payload 为空");
                    eventArgs.ParseError = "Frame 解析成功但 Payload 为空";
                    BinaryMessageReceived?.Invoke(this, eventArgs);
                    await SendAckMessageAsync(frame, false, cancellationToken);
                }
            }
            catch (ProtoBuf.ProtoException ex)
            {
                _logger.LogError(ex, "ProtoBuf 反序列化失败，尝试直接解析为 JSON");

                eventArgs.ParseError = $"ProtoBuf 反序列化失败: {ex.Message}";

                // 如果 ProtoBuf 解析失败，尝试直接解析为 JSON
                var jsonString = Encoding.UTF8.GetString(completeData);
                if (!string.IsNullOrWhiteSpace(jsonString))
                {
                    eventArgs.JsonContent = jsonString;
                    eventArgs.MessageType = "JSON_Fallback";
                    BinaryMessageReceived?.Invoke(this, eventArgs);

                    // 如果设置了MessageRouter，则路由JSON内容
                    if (_messageRouter != null)
                    {
                        if (_options.EnableLogging)
                            _logger.LogDebug("路由二进制转换的JSON消息到MessageRouter (Fallback模式)");
                        await _messageRouter.RouteBinaryMessageAsync(jsonString, "JSON_Fallback", cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理完整二进制消息时发生错误");
                eventArgs.ParseError = $"处理完整二进制消息时发生错误: {ex.Message}";
            }

            // 触发二进制消息接收事件
            BinaryMessageReceived?.Invoke(this, eventArgs);

            await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理完整二进制消息时发生未知错误");
            OnError($"处理完整二进制消息时发生未知错误: {ex.Message}", ex.GetType().Name);
        }
    }

    private async Task SendAckMessageAsync(EventProtoData? eventProtoData, bool sucess, CancellationToken cancellationToken)
    {
        if (eventProtoData == null)
            return;
        var jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        var data = new
        {
            status = sucess ? "success" : "failure",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var dto_string = JsonSerializer.Serialize(data, jsonOptions);
        var ackMessage = new
        {
            code = 200,
            data = Encoding.UTF8.GetBytes(dto_string)
        };

        try
        {
            var ackJson = JsonSerializer.Serialize(ackMessage, jsonOptions);
            using var messageStream = new MemoryStream();
            eventProtoData.Payload = Encoding.UTF8.GetBytes(ackJson);
            ProtoBuf.Serializer.Serialize(messageStream, eventProtoData);

            if (messageStream.TryGetBuffer(out var arraySegment) && _connectionManager != null)
            {
                await _connectionManager.SendBinaryMessageAsync(arraySegment, cancellationToken);
                if (_options.EnableLogging)
                    _logger.LogDebug("已发送ACK消息: {AckJson}", ackJson);
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
            lock (_binaryDataStreamLock)
            {
                _binaryDataStream?.Dispose();
                _binaryDataStream = null;
            }
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogError(ex, "释放二进制处理器资源时发生错误");
        }
        finally
        {
            _disposed = true;
        }
    }
}