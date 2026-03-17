// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.Exceptions;
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
    private readonly IFeishuEventInterceptor[] _interceptors;
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly AuthenticationManager _authManager;
    private readonly MessageRouter _messageRouter;
    private readonly BinaryMessageProcessor _binaryProcessor;
    private readonly EventSubscriptionManager _subscriptionManager;
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly List<Func<string, Task>> _messageProcessors = new();
    private readonly ILoggerFactory _loggerFactory;
    private bool _disposed = false;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly IFeishuSeqIDDeduplicator? _seqIdDeduplicator;
    private readonly MessageSequenceValidator? _sequenceValidator;
    private readonly SessionManager? _sessionManager;
    private readonly SemaphoreSlim _messageProcessingSemaphore;

    // 保存事件处理器委托引用，用于正确的取消订阅，避免内存泄漏
    private readonly EventHandler<EventArgs> _onConnected;
    private readonly EventHandler<WebSocketCloseEventArgs> _onDisconnected;
    private readonly EventHandler<EventArgs> _onAuthenticated;
    private readonly EventHandler<WebSocketErrorEventArgs> _onErrorFromConnectionManager;
    private readonly EventHandler<WebSocketErrorEventArgs> _onErrorFromAuth;
    private readonly EventHandler<WebSocketBinaryMessageEventArgs> _onBinaryMessageReceived;
    private readonly EventHandler<WebSocketErrorEventArgs> _onErrorFromBinary;

    // 心跳相关状态
    private DateTime _lastPongTime = DateTime.MinValue;
    private int _heartbeatMissedCount = 0;

    // 连接状态线程安全保护
    private int _connectionState = 0; // 0=未连接, 1=已连接, 2=连接中, 3=重连中
    private readonly object _connectionStateLock = new();

    // 重连总次数和时间限制 (防止无限重连)
    private int _totalReconnectAttempts = 0;
    private DateTime _firstReconnectAttempt = DateTime.MinValue;
    private const int MaxTotalReconnectAttempts = 20; // 总最大重连次数
    private static readonly TimeSpan MaxReconnectTotalTime = TimeSpan.FromMinutes(30); // 最大重连总时间

    // 处理器引用
    private PingPongMessageHandler? _pingPongHandler;
    /// <inheritdoc/>
    public WebSocketState State
    {
        get
        {
            lock (_connectionStateLock)
            {
                return _connectionManager.State;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsConnected
    {
        get
        {
            lock (_connectionStateLock)
            {
                return _connectionState == 1 && _connectionManager.IsConnected;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsAuthenticated => _authManager.IsAuthenticated;

    /// <summary>
    /// 检查是否超过最大重连次数限制
    /// </summary>
    private bool IsMaxReconnectAttemptsReached()
    {
        lock (_connectionStateLock)
        {
            // 检查总重连次数
            if (_totalReconnectAttempts >= MaxTotalReconnectAttempts)
            {
                _logger.LogError("已达到最大重连总次数 {MaxAttempts}，停止重连", MaxTotalReconnectAttempts);
                return true;
            }

            // 检查重连总时间
            if (_firstReconnectAttempt != DateTime.MinValue)
            {
                var totalReconnectTime = DateTime.UtcNow - _firstReconnectAttempt;
                if (totalReconnectTime >= MaxReconnectTotalTime)
                {
                    _logger.LogError("已达到最大重连总时间 {MaxTime} 分钟，停止重连", MaxReconnectTotalTime.TotalMinutes);
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 重置重连计数器
    /// </summary>
    private void ResetReconnectCounter()
    {
        lock (_connectionStateLock)
        {
            _totalReconnectAttempts = 0;
            _firstReconnectAttempt = DateTime.MinValue;
        }
    }

    /// <summary>
    /// 增加重连计数
    /// </summary>
    private void IncrementReconnectAttempt()
    {
        lock (_connectionStateLock)
        {
            if (_firstReconnectAttempt == DateTime.MinValue)
            {
                _firstReconnectAttempt = DateTime.UtcNow;
            }
            _totalReconnectAttempts++;
        }
    }

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
    public event EventHandler<WebSocketBinaryMessageEventArgs>? BinaryMessageReceived;

    /// <summary>
    /// 初始化飞书WebSocket客户端
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="eventHandlerFactory">事件处理器工厂</param>
    /// <param name="loggerFactory">日志记录器工厂</param>
    /// <param name="interceptors">事件拦截器集合</param>
    /// <param name="options">WebSocket配置选项</param>
    /// <param name="seqIdDeduplicator">SeqID去重服务（可选）</param>
    /// <param name="sessionManager">会话管理器（可选）</param>
    /// <param name="sequenceValidator">消息序号验证器（可选）</param>
    public FeishuWebSocketClient(
        ILogger<FeishuWebSocketClient> logger,
        IFeishuEventHandlerFactory eventHandlerFactory,
        ILoggerFactory loggerFactory,
        IFeishuEventInterceptor[]? interceptors = null,
        FeishuWebSocketOptions? options = null,
        IFeishuSeqIDDeduplicator? seqIdDeduplicator = null,
        SessionManager? sessionManager = null,
        MessageSequenceValidator? sequenceValidator = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventHandlerFactory = eventHandlerFactory ?? throw new ArgumentNullException(nameof(eventHandlerFactory));
        _interceptors = interceptors ?? Array.Empty<IFeishuEventInterceptor>();
        _options = options ?? new FeishuWebSocketOptions();
        _loggerFactory = loggerFactory;
        _seqIdDeduplicator = seqIdDeduplicator;
        _sessionManager = sessionManager;
        _sequenceValidator = sequenceValidator;

        // 初始化并发控制信号量
        _messageProcessingSemaphore = new SemaphoreSlim(_options.MaxConcurrentMessageProcessing, _options.MaxConcurrentMessageProcessing);

        // 初始化事件处理器委托，保存引用以便正确取消订阅
        _onConnected = (s, e) => Connected?.Invoke(this, e);
        _onDisconnected = (s, e) => Disconnected?.Invoke(this, e);
        _onAuthenticated = (s, e) => Authenticated?.Invoke(this, e);
        _onErrorFromConnectionManager = (s, e) => Error?.Invoke(this, e);
        _onErrorFromAuth = (s, e) => Error?.Invoke(this, e);
        _onBinaryMessageReceived = (s, e) => BinaryMessageReceived?.Invoke(this, e);
        _onErrorFromBinary = (s, e) => Error?.Invoke(this, e);

        // 初始化组件
        _connectionManager = new WebSocketConnectionManager(_loggerFactory.CreateLogger<WebSocketConnectionManager>(), _options, _loggerFactory);
        _authManager = new AuthenticationManager(_loggerFactory.CreateLogger<AuthenticationManager>(), _options, (message) => SendMessageAsync(message), _sessionManager);
        _messageRouter = new MessageRouter(_loggerFactory.CreateLogger<MessageRouter>(), _options);
        _binaryProcessor = new BinaryMessageProcessor(_loggerFactory.CreateLogger<BinaryMessageProcessor>(), _connectionManager, _options, _messageRouter, _seqIdDeduplicator, _sequenceValidator);
        _subscriptionManager = new EventSubscriptionManager(_loggerFactory.CreateLogger<EventSubscriptionManager>(), _options, (message) => SendMessageAsync(message));

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
        // 连接管理器事件 - 使用保存的委托引用
        _connectionManager.Connected += _onConnected;
        _connectionManager.Disconnected += _onDisconnected;
        _connectionManager.Error += _onErrorFromConnectionManager;

        // 认证管理器事件 - 使用保存的委托引用
        _authManager.Authenticated += _onAuthenticated;
        _authManager.AuthenticationFailed += _onErrorFromAuth;

        // 二进制处理器事件 - 使用保存的委托引用
        _binaryProcessor.BinaryMessageReceived += _onBinaryMessageReceived;
        _binaryProcessor.Error += _onErrorFromBinary;
    }

    /// <summary>
    /// 注册消息处理器
    /// </summary>
    private void RegisterMessageHandlers()
    {
        var pingPongHandler = new PingPongMessageHandler(
            _loggerFactory.CreateLogger<PingPongMessageHandler>(),
            _options,
            (message) => SendMessageAsync(message));

        // 订阅 PongReceived 事件以更新最后一次 Pong 时间
        if (pingPongHandler is IPongHandler pongHandler)
        {
            pongHandler.PongReceived += OnPongReceived;
        }

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

        var heartbeatHandler = new HeartbeatMessageHandler(_loggerFactory.CreateLogger<HeartbeatMessageHandler>(), _options);

        var eventHandler = new FeishuEventMessageHandler(
            _loggerFactory.CreateLogger<FeishuEventMessageHandler>(),
            _eventHandlerFactory,
            null,
            null,
            _seqIdDeduplicator,
            _interceptors,
            _options);

        _messageRouter.RegisterHandler(pingPongHandler);
        _messageRouter.RegisterHandler(authHandler);
        _messageRouter.RegisterHandler(heartbeatHandler);
        _messageRouter.RegisterHandler(eventHandler);

        // 保存 PingPongHandler 引用以便在 Dispose 时取消订阅
        _pingPongHandler = pingPongHandler;
    }

    /// <summary>
    /// 处理 Pong 消息接收事件
    /// </summary>
    private void OnPongReceived(object? sender, EventArgs e)
    {
        _lastPongTime = DateTime.UtcNow;
        _heartbeatMissedCount = 0;

        if (_options.EnableLogging)
            _logger.LogDebug("已更新最后一次Pong时间");
    }

    /// <summary>
    /// 建立WebSocket连接
    /// </summary>
    public async Task ConnectAsync(WsEndpointResult endpoint, CancellationToken cancellationToken = default)
    {
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));

        // 设置连接状态为连接中
        lock (_connectionStateLock)
        {
            _connectionState = 2; // 连接中
        }

        // 重置重连计数器（连接建立时重置）
        ResetReconnectCounter();

        using (FeishuMetricsHelper.RecordHttpRequest("GET", endpoint.Url))
        {
            await _connectionManager.ConnectAsync(endpoint.Url, cancellationToken);
        }

        // 设置连接状态为已连接
        lock (_connectionStateLock)
        {
            _connectionState = 1; // 已连接
        }

        // 启动消息接收
        _cancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(() => StartReceivingAsyncInternal(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        // 启动心跳
        _ = Task.Run(() => StartHeartbeatAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        // 启动消息队列处理
        if (_options.EnableMessageQueue)
        {
            _ = Task.Run(() => ProcessMessageQueueAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }
    }

    /// <summary>
    /// 建立WebSocket连接并进行认证
    /// </summary>
    public async Task ConnectAsync(WsEndpointResult endpoint, string appAccessToken, CancellationToken cancellationToken = default)
    {
        await ConnectAsync(endpoint, cancellationToken);
        await _authManager.AuthenticateAsync(appAccessToken, cancellationToken);

        // 认证成功后，自动订阅事件
        if (_subscriptionManager.HasSubscribed)
        {
            if (_options.EnableLogging)
                _logger.LogInformation("自动重新订阅事件类型...");
            await _subscriptionManager.SendSubscriptionRequestAsync(cancellationToken);
        }
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
        catch (WebSocketException wsEx) when (wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            _logger.LogError(wsEx, "WebSocket 连接过早关闭，可能是网络问题或服务端主动断开");
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = new FeishuConnectionException("连接过早关闭", _connectionManager.State.ToString()),
                ErrorMessage = "连接过早关闭",
                ErrorType = "ConnectionClosedPrematurely",
                IsRecoverable = true
            });
        }
        catch (WebSocketException wsEx) when (wsEx.WebSocketErrorCode == WebSocketError.NotAWebSocket)
        {
            _logger.LogError(wsEx, "WebSocket 协议错误，端点可能不是WebSocket服务");
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = new FeishuConnectionException("WebSocket协议错误", wsEx),
                ErrorMessage = "WebSocket协议错误",
                ErrorType = "ProtocolError",
                IsRecoverable = false
            });
        }
        catch (WebSocketException wsEx) when (wsEx.WebSocketErrorCode == WebSocketError.Success)
        {
            _logger.LogWarning(wsEx, "WebSocket 连接已关闭");
            // 不触发Error事件，因为这是正常的关闭
        }
        catch (WebSocketException wsEx)
        {
            _logger.LogError(wsEx, "WebSocket 发生错误，错误代码: {ErrorCode}, 原因: {NativeErrorCode}",
                wsEx.WebSocketErrorCode, wsEx.NativeErrorCode);
            var isRecoverable = IsWebSocketErrorRecoverable(wsEx);
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = new FeishuConnectionException($"WebSocket错误: {wsEx.WebSocketErrorCode}", wsEx),
                ErrorMessage = $"WebSocket错误: {wsEx.WebSocketErrorCode}",
                ErrorType = "WebSocketError",
                IsRecoverable = isRecoverable
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("消息接收循环被正常取消");
        }
        catch (IOException ioEx)
        {
            _logger.LogError(ioEx, "发生 IO 错误，可能是网络中断: {HResult}", ioEx.HResult);
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = new FeishuNetworkException("网络错误 - 可能是网络中断", ioEx),
                ErrorMessage = "网络错误 - 可能是网络中断",
                ErrorType = "NetworkError",
                IsRecoverable = true
            });
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON 解析错误，消息格式可能不正确");
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = new FeishuMessageException("消息格式错误", jsonEx),
                ErrorMessage = "消息格式错误",
                ErrorType = "MessageFormatError",
                IsRecoverable = true
            });
        }
        catch (ArgumentException argEx)
        {
            _logger.LogError(argEx, "参数验证错误: {Message}", argEx.Message);
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = argEx,
                ErrorMessage = "参数验证错误",
                ErrorType = "ArgumentError",
                IsRecoverable = false
            });
        }
        catch (TimeoutException timeoutEx)
        {
            _logger.LogWarning(timeoutEx, "操作超时");
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = timeoutEx,
                ErrorMessage = "操作超时",
                ErrorType = "TimeoutError",
                IsRecoverable = true
            });
        }
        catch (TaskCanceledException taskCanceledEx) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(taskCanceledEx, "任务被取消，可能是超时导致的");
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = taskCanceledEx,
                ErrorMessage = "任务超时",
                ErrorType = "TaskTimeoutError",
                IsRecoverable = true
            });
        }
        catch (ObjectDisposedException disposedEx)
        {
            _logger.LogWarning(disposedEx, "对象已释放: {ObjectName}", disposedEx.ObjectName);
            // 不触发Error事件，因为这是正常的关闭流程
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "消息接收循环发生未预期的错误: {ExceptionType}, HResult: {HResult}, Message: {Message}",
                ex.GetType().Name, ex.HResult, ex.Message);
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = $"未预期错误: {ex.GetType().Name} - {ex.Message}",
                ErrorType = "UnexpectedError",
                IsRecoverable = false
            });
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
                    _logger.LogDebug("接收到文本消息，长度: {MessageLength}, 队列大小: {QueueCount}",
                        message.Length, _messageQueue.Count);

                // 触发消息接收事件
                MessageReceived?.Invoke(this, new WebSocketMessageEventArgs
                {
                    Message = message,
                    MessageType = result.MessageType,
                    EndOfMessage = result.EndOfMessage,
                    MessageSize = buffer.Count,
                    QueueCount = _messageQueue.Count
                });

                // 使用 Task.Run 将消息处理隔离到独立线程池，避免阻塞接收管道
                // 捕获异常确保不会影响消息接收循环
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 路由消息到处理器并记录指标
                        using (FeishuMetricsHelper.RecordEventHandling("websocket_message", "text"))
                        using (FeishuMetricsHelper.RecordWebSocketMessageProcessing())
                        {
                            await _messageRouter.RouteMessageAsync(message, cancellationToken);
                        }

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
                    catch (Exception ex)
                    {
                        // 隔离处理消息处理异常，避免影响接收管道
                        _logger.LogError(ex, "消息处理任务执行失败，不影响接收管道");
                        Error?.Invoke(this, new WebSocketErrorEventArgs
                        {
                            Exception = ex,
                            ErrorMessage = $"消息处理错误: {ex.Message}",
                            ErrorType = "MessageProcessingError",
                            IsRecoverable = true
                        });
                    }
                }, cancellationToken);
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                // 二进制消息处理同样隔离到独立任务
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using (FeishuMetricsHelper.RecordEventHandling("websocket_message", "binary"))
                        using (FeishuMetricsHelper.RecordWebSocketMessageProcessing())
                        {
                            await _binaryProcessor.ProcessBinaryDataAsync(buffer.Array!, buffer.Offset, buffer.Count, result.EndOfMessage, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "二进制消息处理任务执行失败，不影响接收管道");
                        Error?.Invoke(this, new WebSocketErrorEventArgs
                        {
                            Exception = ex,
                            ErrorMessage = $"二进制消息处理错误: {ex.Message}",
                            ErrorType = "BinaryMessageProcessingError",
                            IsRecoverable = true
                        });
                    }
                }, cancellationToken);
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "解析 JSON 消息失败，消息大小: {MessageSize}",
                buffer.Count);
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = jsonEx,
                ErrorMessage = "JSON 解析失败",
                ErrorType = "JsonParseError",
                IsRecoverable = true
            });
        }
        catch (InvalidOperationException invEx)
        {
            _logger.LogError(invEx, "无效操作错误，可能是连接状态异常: {Message}",
                invEx.Message);
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = invEx,
                ErrorMessage = "无效操作 - 连接状态可能异常",
                ErrorType = "InvalidStateError",
                IsRecoverable = false
            });
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("消息处理被取消");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理接收到的消息时发生未预期的错误: {ExceptionType}, 消息类型: {MessageType}",
                ex.GetType().Name, result.MessageType);
            Error?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = $"消息处理错误: {ex.GetType().Name}",
                ErrorType = "MessageProcessingError",
                IsRecoverable = true
            });
        }
    }

    /// <summary>
    /// 启动心跳
    /// </summary>
    private async Task StartHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            _lastPongTime = DateTime.UtcNow;
            _heartbeatMissedCount = 0;

            // 保持心跳运行，即使连接断开也要持续检测以便触发重连
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_options.HeartbeatIntervalMs, cancellationToken);

                // 如果连接已断开且启用自动重连，尝试触发重连
                if (!_connectionManager.IsConnected)
                {
                    if (_options.AutoReconnect)
                    {
                        // 检查是否超过最大重连次数，避免无限重连
                        if (IsMaxReconnectAttemptsReached())
                        {
                            _logger.LogError("已达到最大重连限制，停止重连。连接状态: {State}", _connectionManager.State);
                            break;
                        }

                        IncrementReconnectAttempt();
                        _logger.LogDebug("连接已断开，触发重连事件... (第 {Attempt} 次)", _totalReconnectAttempts);
                        // 触发断开事件，让 HostedService 处理重连逻辑
                        Disconnected?.Invoke(this, new SocketEventArgs.WebSocketCloseEventArgs
                        {
                            CloseStatus = System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                            CloseStatusDescription = "心跳检测到连接断开，准备重连",
                            IsServerInitiated = false,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    // 连接断开后重置心跳状态
                    _lastPongTime = DateTime.UtcNow;
                    _heartbeatMissedCount = 0;
                    continue;
                }

                try
                {
                    var pingMessage = new PingMessage
                    {
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    var heartbeatMessage = JsonSerializer.Serialize(pingMessage, JsonOptions.Default);
                    await SendMessageAsync(heartbeatMessage, cancellationToken);

                    if (_options.EnableLogging)
                        _logger.LogDebug("已发送心跳");

                    // 检查心跳超时
                    await CheckHeartbeatTimeoutAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送心跳时发生错误");

                    // 心跳发送失败，尝试触发重连
                    if (_options.AutoReconnect)
                    {
                        _logger.LogWarning("心跳发送失败，准备重连...");
                        await TriggerReconnectAsync(cancellationToken);
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
    /// 检查心跳超时
    /// </summary>
    private async Task CheckHeartbeatTimeoutAsync(CancellationToken cancellationToken)
    {
        var timeSinceLastPong = DateTime.UtcNow - _lastPongTime;
        var heartbeatTimeoutMs = _options.HeartbeatIntervalMs * 2; // 超时时间为2倍心跳间隔

        if (timeSinceLastPong.TotalMilliseconds > heartbeatTimeoutMs)
        {
            _heartbeatMissedCount++;

            _logger.LogWarning("心跳超时：{TimeSinceLastPong}ms 未收到响应，超时次数：{MissedCount}",
                timeSinceLastPong.TotalMilliseconds, _heartbeatMissedCount);

            // 如果连续多次超时，触发重连
            if (_heartbeatMissedCount >= 3 && _options.AutoReconnect)
            {
                // 检查是否超过最大重连次数，避免无限重连
                if (IsMaxReconnectAttemptsReached())
                {
                    _logger.LogError("已达到最大重连限制，停止重连");
                    return;
                }

                IncrementReconnectAttempt();
                _logger.LogError("连续 {MissedCount} 次心跳超时，触发重连 (第 {Attempt} 次)", _heartbeatMissedCount, _totalReconnectAttempts);
                await TriggerReconnectAsync(cancellationToken);
            }
        }
        else
        {
            // 重置超时计数器
            _heartbeatMissedCount = 0;
        }
    }

    /// <summary>
    /// 触发重连
    /// </summary>
    private async Task TriggerReconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_connectionManager.IsConnected)
            {
                await _connectionManager.DisconnectAsync(cancellationToken);
            }

            // 触发断开事件，让 HostedService 处理重连逻辑
            Disconnected?.Invoke(this, new SocketEventArgs.WebSocketCloseEventArgs
            {
                CloseStatus = System.Net.WebSockets.WebSocketCloseStatus.EndpointUnavailable,
                CloseStatusDescription = "心跳超时，触发重连",
                IsServerInitiated = false,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发重连时发生错误");
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
                        // 等待信号量，控制并发数
                        await _messageProcessingSemaphore.WaitAsync(cancellationToken);

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
                        finally
                        {
                            // 释放信号量
                            _messageProcessingSemaphore.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理队列中的消息时发生错误: {Message}", message);
                    }
                }
                else
                {
                    await Task.Delay(_options.EmptyQueueCheckIntervalMs, cancellationToken);
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
    private async Task ProcessMessageSafely(Func<string, Task> processor, string message, CancellationToken _)
    {
        try
        {
            await processor(message);
        }
        catch (Exception ex)
        {
            if (_options.EnableLogging)
                _logger.LogWarning(ex, "消息处理器执行失败: {Message}", message);
        }
    }

    /// <summary>
    /// 判断 WebSocket 错误是否可恢复
    /// </summary>
    /// <param name="wsEx">WebSocket 异常</param>
    /// <returns>如果错误可恢复返回 true，否则返回 false</returns>
    private bool IsWebSocketErrorRecoverable(WebSocketException wsEx)
    {
        return wsEx.WebSocketErrorCode switch
        {
            WebSocketError.ConnectionClosedPrematurely => true,
            WebSocketError.NotAWebSocket => false,
            WebSocketError.UnsupportedVersion => false,
            WebSocketError.UnsupportedProtocol => false,
            WebSocketError.HeaderError => false,
            WebSocketError.InvalidMessageType => false,
            WebSocketError.Faulted => true,
            _ => true
        };
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
            UnsubscribeFromComponentEvents();
            UnsubscribeFromHandlerEvents();
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

    /// <summary>
    /// 取消处理器事件订阅
    /// </summary>
    private void UnsubscribeFromHandlerEvents()
    {
        if (_pingPongHandler != null)
        {
            _pingPongHandler.PongReceived -= OnPongReceived;
            _pingPongHandler = null;
        }

        // 清理订阅管理器
        _subscriptionManager?.ClearSubscriptions();

        // 释放并发控制信号量
        _messageProcessingSemaphore?.Dispose();
    }

    /// <summary>
    /// 取消组件事件订阅
    /// </summary>
    private void UnsubscribeFromComponentEvents()
    {
        // 取消连接管理器事件订阅 - 使用保存的委托引用
        if (_connectionManager != null)
        {
            _connectionManager.Connected -= _onConnected;
            _connectionManager.Disconnected -= _onDisconnected;
            _connectionManager.Error -= _onErrorFromConnectionManager;
        }

        // 取消认证管理器事件订阅 - 使用保存的委托引用
        if (_authManager != null)
        {
            _authManager.Authenticated -= _onAuthenticated;
            _authManager.AuthenticationFailed -= _onErrorFromAuth;
        }

        // 取消二进制处理器事件订阅 - 使用保存的委托引用
        if (_binaryProcessor != null)
        {
            _binaryProcessor.BinaryMessageReceived -= _onBinaryMessageReceived;
            _binaryProcessor.Error -= _onErrorFromBinary;
        }
    }
}