
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.Handlers;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket客户端实现
/// </summary>
public class FeishuWebSocketClient : IFeishuWebSocketClient
{
    private readonly ILogger<FeishuWebSocketClient> _logger;
    private readonly FeishuWebSocketOptions _options;
    private readonly FeishuEventHandlerFactory _eventHandlerFactory;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly List<Func<string, Task>> _messageProcessors = new();
    private Task? _messageProcessingTask;
    private bool _disposed = false;
    private bool _isAuthenticated = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="eventHandlerFactory">事件处理器工厂</param>
    /// <param name="options">WebSocket配置选项</param>
    public FeishuWebSocketClient(
        ILogger<FeishuWebSocketClient> logger, 
        FeishuEventHandlerFactory eventHandlerFactory,
        FeishuWebSocketOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventHandlerFactory = eventHandlerFactory ?? throw new ArgumentNullException(nameof(eventHandlerFactory));
        _options = options ?? new FeishuWebSocketOptions();
    }

    /// <summary>
    /// WebSocket连接状态
    /// </summary>
    public WebSocketState State => _webSocket?.State ?? WebSocketState.None;

    /// <summary>
    /// 是否已认证
    /// </summary>
    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// 连接建立事件
    /// </summary>
    public event EventHandler<System.EventArgs>? Connected;

    /// <summary>
    /// 连接断开事件
    /// </summary>
    public event EventHandler<WebSocketCloseEventArgs>? Disconnected;

    /// <summary>
    /// 接收到消息事件
    /// </summary>
    public event EventHandler<WebSocketMessageEventArgs>? MessageReceived;

    /// <summary>
    /// 连接错误事件
    /// </summary>
    public event EventHandler<WebSocketErrorEventArgs>? Error;

    /// <summary>
    /// 认证成功事件
    /// </summary>
    public event EventHandler<System.EventArgs>? Authenticated;

    /// <summary>
    /// 接收到Ping事件
    /// </summary>
    public event EventHandler<WebSocketPingEventArgs>? PingReceived;

    /// <summary>
    /// 接收到Pong事件
    /// </summary>
    public event EventHandler<WebSocketPongEventArgs>? PongReceived;

    /// <summary>
    /// 接收到飞书事件
    /// </summary>
    public event EventHandler<WebSocketFeishuEventArgs>? FeishuEventReceived;

    /// <summary>
    /// 建立WebSocket连接
    /// </summary>
    /// <param name="endpoint">WebSocket端点信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接任务</returns>
    public async Task ConnectAsync(WsEndpointResult endpoint, CancellationToken cancellationToken = default)
    {
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            // 如果已经连接，先断开
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await DisconnectAsync(cancellationToken);
            }

            // 创建新的WebSocket连接
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            // 设置连接超时
            using var timeoutCts = new CancellationTokenSource(_options.ConnectionTimeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token,
                _cancellationTokenSource.Token);

            try
            {
                await _webSocket.ConnectAsync(new Uri(endpoint.Url), combinedCts.Token);

                if (_options.EnableLogging)
                    _logger.LogInformation("已连接到飞书WebSocket服务: {Url}", endpoint.Url);

                // 启动心跳和消息接收
                _ = StartHeartbeatAsync(_cancellationTokenSource.Token);
                _ = StartReceivingAsync(_cancellationTokenSource.Token);

                // 如果启用了消息队列，启动消息处理任务
                if (_options.EnableMessageQueue)
                {
                    _messageProcessingTask = ProcessMessageQueueAsync(_cancellationTokenSource.Token);
                }

                // 触发连接建立事件
                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                if (_options.EnableLogging)
                    _logger.LogError("连接飞书WebSocket服务超时");

                throw new TimeoutException("连接飞书WebSocket服务超时");
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 建立WebSocket连接并进行认证
    /// </summary>
    /// <param name="endpoint">WebSocket端点信息</param>
    /// <param name="appAccessToken">应用访问令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接任务</returns>
    public async Task ConnectAsync(WsEndpointResult endpoint, string appAccessToken, CancellationToken cancellationToken = default)
    {
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));

        if (string.IsNullOrEmpty(appAccessToken))
            throw new ArgumentException("应用访问令牌不能为空", nameof(appAccessToken));

        // 先建立基本连接
        await ConnectAsync(endpoint, cancellationToken);

        // 发送认证消息
        await AuthenticateAsync(appAccessToken, cancellationToken);
    }

    /// <summary>
    /// 断开WebSocket连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>断开连接任务</returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_webSocket == null || _webSocket.State == WebSocketState.Closed)
                return;

            _cancellationTokenSource?.Cancel();

            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "客户端主动断开连接",
                    cancellationToken);
            }

            if (_options.EnableLogging)
                _logger.LogInformation("已断开飞书WebSocket连接");

            // 触发连接断开事件
            Disconnected?.Invoke(this, new WebSocketCloseEventArgs
            {
                CloseStatus = WebSocketCloseStatus.NormalClosure,
                CloseStatusDescription = "客户端主动断开连接",
                IsServerInitiated = false
            });
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogError(ex, "断开飞书WebSocket连接时发生错误");

            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = ex.Message,
                ErrorType = ex.GetType().Name,
                ConnectionState = _webSocket?.State ?? WebSocketState.None,
                IsNetworkError = ex is WebSocketException || ex is IOException,
                IsAuthError = ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                              ex.Message.Contains("认证", StringComparison.OrdinalIgnoreCase)
            });
        }
        finally
        {
            _connectionLock.Release();
        }
    }


    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="message">要发送的消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送任务</returns>
    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket未连接，无法发送消息");
        }

        try
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                cancellationToken);

            if (_options.EnableLogging)
                _logger.LogDebug("已发送消息: {Message}", message);
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogError(ex, "发送消息时发生错误");

            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = ex.Message,
                ErrorType = ex.GetType().Name,
                ConnectionState = _webSocket?.State ?? WebSocketState.None,
                IsNetworkError = ex is WebSocketException || ex is IOException,
                IsAuthError = ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                              ex.Message.Contains("认证", StringComparison.OrdinalIgnoreCase)
            });
            throw;
        }
    }

    /// <summary>
    /// 注册消息处理器
    /// </summary>
    /// <param name="processor">消息处理器</param>
    public void RegisterMessageProcessor(Func<string, Task> processor)
    {
        if (processor == null)
            throw new ArgumentNullException(nameof(processor));

        _messageProcessors.Add(processor);
    }

    /// <summary>
    /// 移除消息处理器
    /// </summary>
    /// <param name="processor">要移除的消息处理器</param>
    /// <returns>是否成功移除</returns>
    public bool UnregisterMessageProcessor(Func<string, Task> processor)
    {
        return _messageProcessors.Remove(processor);
    }

    /// <summary>
    /// 开始接收消息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接收任务</returns>
    public async Task StartReceivingAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket == null)
            throw new InvalidOperationException("WebSocket未初始化");

        var buffer = new byte[_options.ReceiveBufferSize];
        var messageBuilder = new StringBuilder();

        try
        {
            while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (_options.EnableLogging)
                            _logger.LogInformation("服务器请求关闭连接: {Status} - {Description}",
                                result.CloseStatus, result.CloseStatusDescription);

                        // 触发断开连接事件
                        Disconnected?.Invoke(this, new WebSocketCloseEventArgs
                        {
                            CloseStatus = result.CloseStatus,
                            CloseStatusDescription = result.CloseStatusDescription,
                            IsServerInitiated = true
                        });

                        // 如果启用自动重连，则尝试重连
                        if (_options.AutoReconnect)
                        {
                            _ = Task.Run(async () => await AttemptReconnectAsync(cancellationToken), cancellationToken);
                        }

                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(chunk);

                        if (result.EndOfMessage)
                        {
                            var message = messageBuilder.ToString();
                            messageBuilder.Clear();

                            if (_options.EnableLogging)
                                _logger.LogDebug("接收到消息: {Message}", message);

                            // 处理消息类型
                            await ProcessMessageByTypeAsync(message, cancellationToken);

                            // 触发消息接收事件
                            MessageReceived?.Invoke(this, new WebSocketMessageEventArgs
                            {
                                Message = message,
                                MessageType = result.MessageType,
                                EndOfMessage = result.EndOfMessage,
                                MessageSize = Encoding.UTF8.GetByteCount(message),
                                QueueCount = _messageQueue.Count
                            });

                            // 如果启用了消息队列，将消息加入队列
                            if (_options.EnableMessageQueue)
                            {
                                // 如果队列已满，移除最旧的消息
                                while (_messageQueue.Count >= _options.MessageQueueCapacity)
                                {
                                    _messageQueue.TryDequeue(out _);
                                }

                                _messageQueue.Enqueue(message);
                            }
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        // 处理二进制消息，如果需要的话
                        if (_options.EnableLogging)
                            _logger.LogDebug("接收到二进制消息，长度: {Length}", result.Count);
                    }
                }
                catch (WebSocketException ex)
                {
                    if (_options.EnableLogging)
                        _logger.LogError(ex, "接收消息时发生WebSocket错误");

                    Error?.Invoke(this, new WebSocketErrorEventArgs
                    {
                        Exception = ex,
                        ErrorMessage = ex.Message,
                        ErrorType = ex.GetType().Name,
                        ConnectionState = _webSocket?.State ?? WebSocketState.None,
                        IsNetworkError = ex is WebSocketException || ex is IOException,
                        IsAuthError = ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                                      ex.Message.Contains("认证", StringComparison.OrdinalIgnoreCase)
                    });

                    // 如果启用自动重连，则尝试重连
                    if (_options.AutoReconnect)
                    {
                        _ = Task.Run(async () => await AttemptReconnectAsync(cancellationToken), cancellationToken);
                    }
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不需要处理
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogError(ex, "接收消息时发生错误");

            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = ex.Message,
                ErrorType = ex.GetType().Name,
                ConnectionState = _webSocket?.State ?? WebSocketState.None,
                IsNetworkError = ex is WebSocketException || ex is IOException,
                IsAuthError = ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                              ex.Message.Contains("认证", StringComparison.OrdinalIgnoreCase)
            });
        }
        finally
        {
            messageBuilder.Clear();
        }
    }

    /// <summary>
    /// 启动心跳
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>心跳任务</returns>
    private async Task StartHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_options.HeartbeatIntervalMs, cancellationToken);

                if (_webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        // 发送心跳消息
                        var pingMessage = new PingMessage
                        {
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };
                        var heartbeatMessage = JsonSerializer.Serialize(pingMessage);
                        await SendMessageAsync(heartbeatMessage, cancellationToken);

                        if (_options.EnableLogging)
                            _logger.LogDebug("已发送心跳");
                    }
                    catch (Exception ex)
                    {
                        if (_options.EnableLogging)
                            _logger.LogError(ex, "发送心跳时发生错误");

                        Error?.Invoke(this, new WebSocketErrorEventArgs
                        {
                            Exception = ex,
                            ErrorMessage = ex.Message,
                            ErrorType = ex.GetType().Name,
                            ConnectionState = _webSocket?.State ?? WebSocketState.None,
                            IsNetworkError = ex is WebSocketException || ex is IOException
                        });
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不需要处理
        }
    }

    /// <summary>
    /// 根据消息类型处理消息
    /// </summary>
    /// <param name="message">接收到的消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    private async Task ProcessMessageByTypeAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            // 解析消息基础信息
            using var jsonDoc = JsonDocument.Parse(message);
            if (!jsonDoc.RootElement.TryGetProperty("type", out var typeElement))
            {
                if (_options.EnableLogging)
                    _logger.LogWarning("收到无类型消息: {Message}", message);
                return;
            }

            var messageType = typeElement.GetString()?.ToLowerInvariant();

            switch (messageType)
            {
                case "ping":
                    await HandlePingMessageAsync(message);
                    break;

                case "pong":
                    await HandlePongMessageAsync(message);
                    break;

                case "event":
                    await HandleEventMessageAsync(message, cancellationToken);
                    break;

                case "auth":
                    await HandleAuthResponseMessageAsync(message);
                    break;

                default:
                    if (_options.EnableLogging)
                        _logger.LogWarning("收到未知类型消息: {Type}, 内容: {Message}", messageType, message);
                    break;
            }
        }
        catch (JsonException ex)
        {
            if (_options.EnableLogging)
                _logger.LogError(ex, "解析消息JSON时发生错误: {Message}", message);
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogError(ex, "处理消息时发生错误: {Message}", message);
        }
    }

    /// <summary>
    /// 处理Ping消息
    /// </summary>
    /// <param name="message">Ping消息</param>
    /// <returns>处理任务</returns>
    private async Task HandlePingMessageAsync(string message)
    {
        try
        {
            var pingMessage = JsonSerializer.Deserialize<PingMessage>(message);

            if (_options.EnableLogging)
                _logger.LogDebug("收到Ping消息，时间戳: {Timestamp}", pingMessage?.Timestamp);

            // 触发Ping接收事件
            PingReceived?.Invoke(this, new WebSocketPingEventArgs
            {
                PingMessage = pingMessage
            });

            // 发送Pong响应
            var pongMessage = new PongMessage
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            var pongJson = JsonSerializer.Serialize(pongMessage);
            await SendMessageAsync(pongJson);

            if (_options.EnableLogging)
                _logger.LogDebug("已发送Pong响应");
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogError(ex, "处理Ping消息时发生错误");
        }
    }

    /// <summary>
    /// 处理Pong消息
    /// </summary>
    /// <param name="message">Pong消息</param>
    /// <returns>处理任务</returns>
    private async Task HandlePongMessageAsync(string message)
    {
        try
        {
            var pongMessage = JsonSerializer.Deserialize<PongMessage>(message);

            if (_options.EnableLogging)
                _logger.LogDebug("收到Pong消息，时间戳: {Timestamp}", pongMessage?.Timestamp);

            // 计算延迟（如果有原始Ping时间戳）
            long? latencyMs = null;
            if (pongMessage?.Timestamp > 0)
            {
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                latencyMs = (currentTime - pongMessage.Timestamp) * 1000; // 转换为毫秒
            }

            // 触发Pong接收事件
            PongReceived?.Invoke(this, new WebSocketPongEventArgs
            {
                PongMessage = pongMessage,
                LatencyMs = latencyMs
            });
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogError(ex, "处理Pong消息时发生错误");
        }
    }

    /// <summary>
    /// 处理事件消息
    /// </summary>
    /// <param name="message">事件消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    private async Task HandleEventMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventMessage = JsonSerializer.Deserialize<EventMessage>(message);

            if (_options.EnableLogging)
                _logger.LogInformation("收到事件消息: {EventType}, 应用ID: {AppId}, 租户: {TenantKey}",
                    eventMessage?.Data?.EventType, eventMessage?.Data?.AppId, eventMessage?.Data?.TenantKey);

            // 触发飞书事件接收事件
            FeishuEventReceived?.Invoke(this, new WebSocketFeishuEventArgs
            {
                EventMessage = eventMessage,
                RawEvent = message
            });

            // 使用策略模式处理不同的事件类型
            if (eventMessage?.Data != null)
            {
                var handler = _eventHandlerFactory.GetHandler(eventMessage.Data.EventType);
                await handler.HandleAsync(eventMessage.Data, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogError(ex, "处理事件消息时发生错误");
        }
    }

    /// <summary>
    /// 处理认证响应消息
    /// </summary>
    /// <param name="message">认证响应消息</param>
    /// <returns>处理任务</returns>
    private async Task HandleAuthResponseMessageAsync(string message)
    {
        try
        {
            var authResponse = JsonSerializer.Deserialize<AuthResponseMessage>(message);

            if (authResponse?.Code == 0)
            {
                _isAuthenticated = true;

                if (_options.EnableLogging)
                    _logger.LogInformation("WebSocket认证成功: {Message}", authResponse.Message);

                // 触发认证成功事件
                Authenticated?.Invoke(this, System.EventArgs.Empty);
            }
            else
            {
                _isAuthenticated = false;

                if (_options.EnableLogging)
                    _logger.LogError("WebSocket认证失败: {Code} - {Message}", authResponse?.Code, authResponse?.Message);

                // 触发认证错误事件
                Error?.Invoke(this, new WebSocketErrorEventArgs
                {
                    ErrorMessage = $"WebSocket认证失败: {authResponse?.Code} - {authResponse?.Message}",
                    IsAuthError = true,
                    ConnectionState = State
                });
            }
        }
        catch (Exception ex)
        {
            _isAuthenticated = false;

            if (_options.EnableLogging)
                _logger.LogError(ex, "处理认证响应消息时发生错误");
        }
    }



    /// <summary>
    /// 进行WebSocket认证
    /// </summary>
    /// <param name="appAccessToken">应用访问令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证任务</returns>
    private async Task AuthenticateAsync(string appAccessToken, CancellationToken cancellationToken)
    {
        try
        {
            if (_options.EnableLogging)
                _logger.LogInformation("正在进行WebSocket认证...");

            _isAuthenticated = false; // 重置认证状态

            // 创建认证消息
            var authMessage = new AuthMessage
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Data = new AuthData
                {
                    AppAccessToken = appAccessToken
                }
            };

            var authJson = JsonSerializer.Serialize(authMessage);
            await SendMessageAsync(authJson, cancellationToken);

            if (_options.EnableLogging)
                _logger.LogInformation("已发送认证消息，等待响应...");

            // 注意：认证响应现在由 HandleAuthResponseMessageAsync 异步处理
            // 这里不直接等待，而是通过事件机制通知认证结果
        }
        catch (Exception ex)
        {
            _isAuthenticated = false;

            if (_options.EnableLogging)
                _logger.LogError(ex, "WebSocket认证失败");

            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = $"WebSocket认证失败: {ex.Message}",
                ErrorType = ex.GetType().Name,
                ConnectionState = _webSocket?.State ?? WebSocketState.None,
                IsAuthError = true
            });

            throw;
        }
    }

    /// <summary>
    /// 尝试重连
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重连任务</returns>
    private async Task AttemptReconnectAsync(CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (attempt < _options.MaxReconnectAttempts && !cancellationToken.IsCancellationRequested)
        {
            attempt++;

            if (_options.EnableLogging)
                _logger.LogInformation("尝试重连 ({Attempt}/{MaxAttempts})...", attempt, _options.MaxReconnectAttempts);

            try
            {
                // 等待一段时间再重连
                await Task.Delay(_options.ReconnectDelayMs, cancellationToken);

                // 这里需要重新获取端点，但当前接口设计不支持，需要外部重新调用ConnectAsync
                // 因此我们只是触发断开事件，让外部处理重连
                Disconnected?.Invoke(this, new WebSocketCloseEventArgs
                {
                    CloseStatus = WebSocketCloseStatus.Empty,
                    CloseStatusDescription = "需要重新连接"
                });

                break;
            }
            catch (TaskCanceledException)
            {
                // 取消操作，退出重连循环
                break;
            }
            catch (Exception ex)
            {
                if (_options.EnableLogging)
                    _logger.LogError(ex, "重连尝试 {Attempt} 失败", attempt);

                Error?.Invoke(this, new WebSocketErrorEventArgs
                {
                    Exception = ex,
                    ErrorMessage = $"重连尝试 {attempt} 失败: {ex.Message}"
                });
            }
        }

        if (attempt >= _options.MaxReconnectAttempts)
        {
            if (_options.EnableLogging)
                _logger.LogError("已达到最大重连次数，停止重连");

            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                ErrorMessage = "已达到最大重连次数，停止重连"
            });
        }
    }

    /// <summary>
    /// 处理消息队列
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    private async Task ProcessMessageQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_messageQueue.TryDequeue(out var message))
                {
                    try
                    {
                        // 记录日志
                        if (_options.EnableLogging)
                            _logger.LogDebug("处理队列中的消息: {Message}", message);

                        // 调用所有注册的消息处理器
                        var processingTasks = _messageProcessors.Select(processor =>
                            ProcessMessageSafely(processor, message, cancellationToken));

                        await Task.WhenAll(processingTasks);
                    }
                    catch (Exception ex)
                    {
                        if (_options.EnableLogging)
                            _logger.LogError(ex, "处理消息时发生错误: {Message}", message);

                        Error?.Invoke(this, new WebSocketErrorEventArgs
                        {
                            Exception = ex,
                            ErrorMessage = $"处理消息时发生错误: {ex.Message}",
                            ErrorType = ex.GetType().Name,
                            ConnectionState = _webSocket?.State ?? WebSocketState.None
                        });
                    }
                }
                else
                {
                    // 队列为空，等待一段时间再检查
                    await Task.Delay(100, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不需要处理
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogError(ex, "处理消息队列时发生错误");

            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = ex.Message,
                ErrorType = ex.GetType().Name,
                ConnectionState = _webSocket?.State ?? WebSocketState.None,
                IsNetworkError = ex is WebSocketException || ex is IOException,
                IsAuthError = ex.Message.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
                              ex.Message.Contains("认证", StringComparison.OrdinalIgnoreCase)
            });
        }
    }

    /// <summary>
    /// 安全地处理消息（捕获单个处理器的异常）
    /// </summary>
    /// <param name="processor">消息处理器</param>
    /// <param name="message">消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理任务</returns>
    private async Task ProcessMessageSafely(Func<string, Task> processor, string message, CancellationToken cancellationToken)
    {
        try
        {
            await processor(message);
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogWarning(ex, "消息处理器执行失败: {Message}", message);

            // 不重新抛出异常，避免影响其他处理器
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _cancellationTokenSource?.Cancel();
            _messageProcessingTask?.Wait(TimeSpan.FromSeconds(5));
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
            _connectionLock.Dispose();
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogError(ex, "释放资源时发生错误");
        }
        finally
        {
            _disposed = true;
        }
    }
}
