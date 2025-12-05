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
/// 飞书WebSocket客户端 - 采用组件化设计提高可维护性
/// </summary>
public sealed class FeishuWebSocketClient : IFeishuWebSocketClient, IDisposable
{
    private readonly ILogger<FeishuWebSocketClient> _logger;
    private readonly FeishuWebSocketOptions _options;
    private readonly IFeishuEventHandlerFactory _eventHandlerFactory;
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly AuthenticationManager _authManager;
    private readonly MessageRouter _messageRouter;
    private readonly BinaryMessageProcessor _binaryProcessor;
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly List<Func<string, Task>> _messageProcessors = new();
    private Task? _messageProcessingTask;
    private ILoggerFactory _loggerFactory;
    private bool _disposed = false;
    private CancellationTokenSource? _cancellationTokenSource;
    /// <inheritdoc/>
    public WebSocketState State => _connectionManager.State;
    /// <inheritdoc/>
    public bool IsAuthenticated => _authManager.IsAuthenticated;

    /// <inheritdoc/>
    public event EventHandler<EventArgs>? Connected;
    /// <inheritdoc/>
    public event EventHandler<WebSocketCloseEventArgs>? Disconnected;
    /// <inheritdoc/>
    public event EventHandler<WebSocketMessageEventArgs>? MessageReceived;
    /// <inheritdoc/>
    public event EventHandler<WebSocketErrorEventArgs>? Error;
    /// <inheritdoc/>
    public event EventHandler<EventArgs>? Authenticated;
    /// <inheritdoc/>
    public event EventHandler<WebSocketPingEventArgs>? PingReceived;
    /// <inheritdoc/>
    public event EventHandler<WebSocketPongEventArgs>? PongReceived;
    /// <inheritdoc/>
    public event EventHandler<WebSocketHeartbeatEventArgs>? HeartbeatReceived;
    /// <inheritdoc/>
    public event EventHandler<WebSocketFeishuEventArgs>? FeishuEventReceived;
    /// <inheritdoc/>
    public event EventHandler<WebSocketBinaryMessageEventArgs>? BinaryMessageReceived;

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public FeishuWebSocketClient(
        ILogger<FeishuWebSocketClient> logger,
        IFeishuEventHandlerFactory eventHandlerFactory,
        ILoggerFactory loggerFactory,
        FeishuWebSocketOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventHandlerFactory = eventHandlerFactory ?? throw new ArgumentNullException(nameof(eventHandlerFactory));
        _options = options ?? new FeishuWebSocketOptions();
        _loggerFactory = loggerFactory;
        // 初始化组件
        _connectionManager = new WebSocketConnectionManager(_loggerFactory.CreateLogger<WebSocketConnectionManager>(), _options);
        _authManager = new AuthenticationManager(_loggerFactory.CreateLogger<AuthenticationManager>(), (message) => SendMessageAsync(message));
        _messageRouter = new MessageRouter(_loggerFactory.CreateLogger<MessageRouter>());
        _binaryProcessor = new BinaryMessageProcessor(_loggerFactory.CreateLogger<BinaryMessageProcessor>(), _connectionManager, _options, _messageRouter);

        // 订阅组件事件
        SubscribeToComponentEvents();

        // 注册消息处理器
        RegisterMessageHandlers();
    }

    /// <summary>
    /// 订阅组件事件
    /// </summary>
    private void SubscribeToComponentEvents()
    {
        // 连接管理器事件
        _connectionManager.Connected += (s, e) => Connected?.Invoke(this, e);
        _connectionManager.Disconnected += (s, e) => Disconnected?.Invoke(this, e);
        _connectionManager.Error += (s, e) => Error?.Invoke(this, e);

        // 认证管理器事件
        _authManager.Authenticated += (s, e) => Authenticated?.Invoke(this, e);
        _authManager.AuthenticationFailed += (s, e) => Error?.Invoke(this, e);

        // 二进制处理器事件
        _binaryProcessor.BinaryMessageReceived += (s, e) => BinaryMessageReceived?.Invoke(this, e);
        _binaryProcessor.Error += (s, e) => Error?.Invoke(this, e);
    }

    /// <summary>
    /// 注册消息处理器
    /// </summary>
    private void RegisterMessageHandlers()
    {
        var eventHandler = new EventMessageHandler(
            _loggerFactory.CreateLogger<EventMessageHandler>(),
            _eventHandlerFactory,
            _options,
            SendAckMessageAsync);

        var pingPongHandler = new PingPongMessageHandler(
            _loggerFactory.CreateLogger<PingPongMessageHandler>(),
            (message) => SendMessageAsync(message));

        var authHandler = new AuthMessageHandler(
            _loggerFactory.CreateLogger<AuthMessageHandler>(),
            (success) =>
            {
                if (success)
                {
                    // 通知认证管理器认证成功
                    _authManager.HandleAuthResponse("{\"code\":0,\"msg\":\"Authentication successful\"}");
                }
                else
                {
                    _authManager.HandleAuthResponse("{\"code\":-1,\"msg\":\"Authentication failed\"}");
                }
            });

        var heartbeatHandler = new HeartbeatMessageHandler(
            _loggerFactory.CreateLogger<HeartbeatMessageHandler>());

        _messageRouter.RegisterHandler(eventHandler);
        _messageRouter.RegisterHandler(pingPongHandler);
        _messageRouter.RegisterHandler(authHandler);
        _messageRouter.RegisterHandler(heartbeatHandler);
    }

    /// <summary>
    /// 建立WebSocket连接
    /// </summary>
    public async Task ConnectAsync(WsEndpointResult endpoint, CancellationToken cancellationToken = default)
    {
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));

        await _connectionManager.ConnectAsync(endpoint.Url, cancellationToken);

        // 启动消息接收
        _cancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(() => StartReceivingAsyncInternal(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        // 启动心跳
        _ = Task.Run(() => StartHeartbeatAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        // 启动消息队列处理
        if (_options.EnableMessageQueue)
        {
            _messageProcessingTask = ProcessMessageQueueAsync(_cancellationTokenSource.Token);
        }
    }

    /// <summary>
    /// 建立WebSocket连接并进行认证
    /// </summary>
    public async Task ConnectAsync(WsEndpointResult endpoint, string appAccessToken, CancellationToken cancellationToken = default)
    {
        await ConnectAsync(endpoint, cancellationToken);
        await _authManager.AuthenticateAsync(appAccessToken, cancellationToken);
    }

    /// <summary>
    /// 断开WebSocket连接
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource?.Cancel();
        await _connectionManager.DisconnectAsync(cancellationToken);
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        await _connectionManager.SendMessageAsync(message, cancellationToken);
    }

    /// <summary>
    /// 注册消息处理器
    /// </summary>
    public void RegisterMessageProcessor(Func<string, Task> processor)
    {
        if (processor == null)
            throw new ArgumentNullException(nameof(processor));

        _messageProcessors.Add(processor);
    }

    /// <summary>
    /// 移除消息处理器
    /// </summary>
    public bool UnregisterMessageProcessor(Func<string, Task> processor)
    {
        return _messageProcessors.Remove(processor);
    }

    /// <summary>
    /// 开始接收消息（公共接口实现）
    /// </summary>
    public async Task StartReceivingAsync(CancellationToken cancellationToken)
    {
        await StartReceivingAsyncInternal(cancellationToken);
    }

    /// <summary>
    /// 开始接收消息（内部实现）
    /// </summary>
    private async Task StartReceivingAsyncInternal(CancellationToken cancellationToken)
    {
        try
        {
            await _connectionManager.StartReceivingAsync(async (buffer, result) =>
            {
                await HandleReceivedMessageAsync(buffer, result, cancellationToken);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消息接收循环发生错误");
        }
    }

    /// <summary>
    /// 处理接收到的消息
    /// </summary>
    private async Task HandleReceivedMessageAsync(ArraySegment<byte> buffer, WebSocketReceiveResult result, CancellationToken cancellationToken)
    {
        try
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count);

                if (_options.EnableLogging)
                    _logger.LogDebug("接收到文本消息: {Message}", message);

                // 触发消息接收事件
                MessageReceived?.Invoke(this, new WebSocketMessageEventArgs
                {
                    Message = message,
                    MessageType = result.MessageType,
                    EndOfMessage = result.EndOfMessage,
                    MessageSize = buffer.Count,
                    QueueCount = _messageQueue.Count
                });

                // 路由消息到处理器
                await _messageRouter.RouteMessageAsync(message, cancellationToken);

                // 加入消息队列
                if (_options.EnableMessageQueue)
                {
                    while (_messageQueue.Count >= _options.MessageQueueCapacity)
                    {
                        _messageQueue.TryDequeue(out _);
                    }
                    _messageQueue.Enqueue(message);
                }
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                await _binaryProcessor.ProcessBinaryDataAsync(buffer.Array!, buffer.Offset, buffer.Count, result.EndOfMessage, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理接收到的消息时发生错误");
        }
    }

    /// <summary>
    /// 启动心跳
    /// </summary>
    private async Task StartHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_connectionManager.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_options.HeartbeatIntervalMs, cancellationToken);

                if (_connectionManager.IsConnected)
                {
                    try
                    {
                        var pingMessage = new PingMessage
                        {
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };

                        var jsonOptions = new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            WriteIndented = false
                        };

                        var heartbeatMessage = JsonSerializer.Serialize(pingMessage, jsonOptions);
                        await SendMessageAsync(heartbeatMessage, cancellationToken);

                        _logger.LogDebug("已发送心跳");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "发送心跳时发生错误");
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
    /// 处理消息队列
    /// </summary>
    private async Task ProcessMessageQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            var processedMessages = 0;
            const int maxMessagesBeforeYield = 100;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_messageQueue.TryDequeue(out var message))
                {
                    try
                    {
                        var processingTasks = _messageProcessors.Select(processor =>
                            ProcessMessageSafely(processor, message, cancellationToken));

                        await Task.WhenAll(processingTasks);
                        processedMessages++;

                        if (processedMessages % maxMessagesBeforeYield == 0)
                        {
                            await Task.Yield();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理队列中的消息时发生错误: {Message}", message);
                    }
                }
                else
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不需要处理
        }
    }

    /// <summary>
    /// 安全地处理消息
    /// </summary>
    private async Task ProcessMessageSafely(Func<string, Task> processor, string message, CancellationToken cancellationToken)
    {
        try
        {
            await processor(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "消息处理器执行失败: {Message}", message);
        }
    }

    /// <summary>
    /// 发送ACK确认消息
    /// </summary>
    private async Task SendAckMessageAsync(string? eventType, string? eventId, CancellationToken cancellationToken)
    {
        if (!_options.EnableAutoAck || string.IsNullOrEmpty(eventType))
            return;

        return;

        try
        {
            var ackMessage = new
            {
                code = 200,
                type = "ack",
                data = new
                {
                    event_type = eventType,
                    event_id = eventId,
                    status = "success",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };

            var ackJson = JsonSerializer.Serialize(ackMessage, jsonOptions);

            await SendMessageAsync(ackJson, cancellationToken);

            _logger.LogInformation("已发送ACK确认消息: EventType={EventType}, EventId={EventId}", eventType, eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送ACK确认消息时发生错误: EventType={EventType}, EventId={EventId}", eventType, eventId);
        }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _cancellationTokenSource?.Cancel();
            _connectionManager?.Dispose();
            _binaryProcessor?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放资源时发生错误");
        }
        finally
        {
            _disposed = true;
        }
    }
}