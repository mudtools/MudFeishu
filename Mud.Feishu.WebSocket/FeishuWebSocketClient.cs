// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.Abstractions.Observability;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.Exceptions;
using Mud.Feishu.WebSocket.Handlers;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket客户端 - 采用组件化设计提高可维护性
/// </summary>
public sealed class FeishuWebSocketClient : IFeishuWebSocketClient, IAsyncDisposable, IDisposable
{
    private readonly ILogger<FeishuWebSocketClient> _logger;
    // F4 修复：持有 IOptionsMonitor 而非一次性快照，保证配置热更新一致性。
    // _options 仍保留为 CurrentValue 快照供构造期组件初始化使用；运行期需热更新的路径
    // （如 AppKey 指标维度、AuthGateTimeoutMs 等）改为读取 _optionsMonitor.CurrentValue。
    private readonly IOptionsMonitor<FeishuWebSocketOptions> _optionsMonitor;
    private readonly FeishuWebSocketOptions _options;
    private readonly IFeishuEventHandlerFactory _eventHandlerFactory;
    private readonly IFeishuEventInterceptor[] _interceptors;
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly AuthenticationManager _authManager;
    private readonly MessageRouter _messageRouter;
    private readonly BinaryMessageProcessor _binaryProcessor;
    private readonly EventSubscriptionManager _subscriptionManager;
    private readonly HeartbeatManager _heartbeatManager;
    private readonly ILoggerFactory _loggerFactory;
    // WS-15 修复（P1-12）：_disposed 改为 int + Interlocked.Exchange 实现原子 check-then-set
    private int _disposed = 0;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiveTask;
    private Task? _heartbeatTask;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly IFeishuSeqIDDeduplicator? _seqIdDeduplicator;
    private readonly IFeishuEventDeduplicator? _eventDeduplicator;
    private readonly MessageSequenceValidator? _sequenceValidator;
    private readonly SessionManager? _sessionManager;
    private readonly FeishuWebSocketConcurrencyService? _concurrencyService;
    // F7 修复：统一去重中间件（可选），提供 EventId + SeqID 双重去重。
    private readonly IUnifiedDeduplicationMiddleware? _unifiedDedupMiddleware;

    // 保存事件处理器委托引用，用于正确的取消订阅，避免内存泄漏
    private readonly EventHandler<EventArgs> _onConnected;
    private readonly EventHandler<WebSocketCloseEventArgs> _onDisconnected;
    private readonly EventHandler<EventArgs> _onAuthenticated;
    private readonly EventHandler<WebSocketErrorEventArgs> _onErrorFromConnectionManager;
    private readonly EventHandler<WebSocketErrorEventArgs> _onErrorFromAuth;
    private readonly EventHandler<WebSocketBinaryMessageEventArgs> _onBinaryMessageReceived;
    private readonly EventHandler<WebSocketErrorEventArgs> _onErrorFromBinary;
    private readonly EventHandler<ClientConfigInfo?> _onPongReceivedBinary;
    private readonly EventHandler _onPongReceivedText;

    // 连接状态线程安全保护 - 使用 Volatile + Interlocked 替代 lock 避免竞态条件
    private int _connectionState = 0; // 0=未连接, 1=已连接, 2=连接中

    // 处理器引用
    private PingPongMessageHandler? _pingPongHandler;
    /// <inheritdoc/>
    public WebSocketState State => _connectionManager.State;

    /// <inheritdoc/>
    public bool IsConnected => Volatile.Read(ref _connectionState) == 1 && _connectionManager.IsConnected;

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
    public event EventHandler<WebSocketBinaryMessageEventArgs>? BinaryMessageReceived;

    /// <summary>
    /// 初始化飞书WebSocket客户端
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="eventHandlerFactory">事件处理器工厂</param>
    /// <param name="loggerFactory">日志记录器工厂</param>
    /// <param name="eventDeduplicator">事件级去重服务（按 event_id 去重，可选）</param>
    /// <param name="interceptors">事件拦截器集合</param>
    /// <param name="options">WebSocket配置选项</param>
    /// <param name="seqIdDeduplicator">SeqID去重服务（可选）</param>
    /// <param name="sessionManager">会话管理器（可选）</param>
    /// <param name="sequenceValidator">消息序号验证器（可选）</param>
    /// <param name="concurrencyService">并发控制服务（可选，WS-03 修复引入）</param>
    /// <param name="optionsMonitor">WebSocket 配置选项监控器（可选，F4 修复引入，支持热更新）</param>
    /// <param name="unifiedDedupMiddleware">统一去重中间件（可选，F7 修复引入）</param>
    /// <remarks>
    /// F4 修复：优先使用 <paramref name="optionsMonitor"/>；为兼容存量调用方，
    /// 当其为 null 时回退到 <paramref name="options"/> 快照。
    /// </remarks>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public FeishuWebSocketClient(
        ILogger<FeishuWebSocketClient> logger,
        IFeishuEventHandlerFactory eventHandlerFactory,
        ILoggerFactory loggerFactory,
        IFeishuEventDeduplicator? eventDeduplicator = null,
        IFeishuEventInterceptor[]? interceptors = null,
        FeishuWebSocketOptions? options = null,
        IFeishuSeqIDDeduplicator? seqIdDeduplicator = null,
        SessionManager? sessionManager = null,
        MessageSequenceValidator? sequenceValidator = null,
        FeishuWebSocketConcurrencyService? concurrencyService = null,
        IOptionsMonitor<FeishuWebSocketOptions>? optionsMonitor = null,
        IUnifiedDeduplicationMiddleware? unifiedDedupMiddleware = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventHandlerFactory = eventHandlerFactory ?? throw new ArgumentNullException(nameof(eventHandlerFactory));
        _interceptors = interceptors ?? Array.Empty<IFeishuEventInterceptor>();
        _optionsMonitor = optionsMonitor!;
        _options = optionsMonitor?.CurrentValue ?? options ?? new FeishuWebSocketOptions();
        _loggerFactory = loggerFactory;
        _seqIdDeduplicator = seqIdDeduplicator;
        _sessionManager = sessionManager;
        _sequenceValidator = sequenceValidator;
        _eventDeduplicator = eventDeduplicator;
        _concurrencyService = concurrencyService;
        _unifiedDedupMiddleware = unifiedDedupMiddleware;

        // 初始化事件处理器委托，保存引用以便正确取消订阅
        // WS-10 修复（P1-7/P1-2）：此前 _onConnected 使用 Task.Run fire-and-forget
        // 调用 ResetStateOnReconnectAsync，与首帧处理存在竞态（重置可能晚于首条消息处理）。
        // 现在改为纯事件转发，状态重置移到 ConnectAsync 中在启动 _receiveTask 之前 await。
        _onConnected = (s, e) =>
        {
            var handler = Connected;
            if (handler != null)
            {
                try
                {
                    handler.Invoke(this, e);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Connected 事件处理器抛出异常");
                }
            }
        };
        _onDisconnected = (s, e) =>
        {
            var handler = Disconnected;
            handler?.Invoke(this, e);
        };
        _onAuthenticated = (s, e) =>
        {
            var handler = Authenticated;
            handler?.Invoke(this, e);
        };
        _onErrorFromConnectionManager = (s, e) =>
        {
            var handler = Error;
            handler?.Invoke(this, e);
        };
        _onErrorFromAuth = (s, e) =>
        {
            var handler = Error;
            handler?.Invoke(this, e);
        };
        _onBinaryMessageReceived = (s, e) =>
        {
            var handler = BinaryMessageReceived;
            handler?.Invoke(this, e);
        };
        _onErrorFromBinary = (s, e) =>
        {
            var handler = Error;
            handler?.Invoke(this, e);
        };
        // 初始化组件
        _connectionManager = new WebSocketConnectionManager(_loggerFactory.CreateLogger<WebSocketConnectionManager>(), _options, _loggerFactory);
        _authManager = new AuthenticationManager(_loggerFactory.CreateLogger<AuthenticationManager>(), _options, (message) => SendMessageAsync(message), _sessionManager);
        _messageRouter = new MessageRouter(_loggerFactory.CreateLogger<MessageRouter>(), _options);
        _binaryProcessor = new BinaryMessageProcessor(_loggerFactory.CreateLogger<BinaryMessageProcessor>(), _connectionManager, _options, _messageRouter, _seqIdDeduplicator, _sequenceValidator);
        _subscriptionManager = new EventSubscriptionManager(_loggerFactory.CreateLogger<EventSubscriptionManager>(), _options, (message) => SendMessageAsync(message));
        _heartbeatManager = new HeartbeatManager(
            _loggerFactory.CreateLogger<HeartbeatManager>(),
            _options,
            (data, token) => _connectionManager.SendBinaryMessageAsync(data, token));

        // Pong 回调委托必须在 _heartbeatManager 赋值之后创建：
        // 此前定义在构造函数前段，委托体解引用尚未赋值的 _heartbeatManager 触发 CS8602。
        _onPongReceivedBinary = (object? s, ClientConfigInfo? config) =>
        {
            _heartbeatManager.OnPongReceived(config);
        };
        _onPongReceivedText = (object? s, EventArgs e) =>
        {
            _heartbeatManager.OnPongReceived(null);
        };

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
        _connectionManager.Connected += _onConnected;
        _connectionManager.Disconnected += _onDisconnected;
        _connectionManager.Error += _onErrorFromConnectionManager;

        _authManager.Authenticated += _onAuthenticated;
        _authManager.AuthenticationFailed += _onErrorFromAuth;

        _binaryProcessor.BinaryMessageReceived += _onBinaryMessageReceived;
        _binaryProcessor.Error += _onErrorFromBinary;
        _binaryProcessor.PongReceived += _onPongReceivedBinary;

    }

    /// <summary>
    /// 注册消息处理器
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    private void RegisterMessageHandlers()
    {
        var pingPongHandler = new PingPongMessageHandler(
            _loggerFactory.CreateLogger<PingPongMessageHandler>(),
            _options,
            (message) => SendMessageAsync(message));

        pingPongHandler.PongReceived += _onPongReceivedText;

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
            },
            _options.AppKey);

        var heartbeatHandler = new HeartbeatMessageHandler(_loggerFactory.CreateLogger<HeartbeatMessageHandler>(), _options);

        // P1-5 修复：此前第三个参数（事件级去重器）恒为 null，
        // 导致 EventDeduplication 配置（默认 InMemory）在 WebSocket 路径上完全失效，
        // 服务端重发事件必然重复消费。
        // WS-21 修复（P2-10）：删除未使用的 seqIdDeduplicator 参数。
        // F7 修复：传入统一去重中间件，由 FeishuEventMessageHandler 优先使用双重去重路径。
        var eventHandler = new FeishuEventMessageHandler(
            _loggerFactory.CreateLogger<FeishuEventMessageHandler>(),
            _eventHandlerFactory,
            _eventDeduplicator,
            _interceptors,
            _options,
            _unifiedDedupMiddleware);

        _messageRouter.RegisterHandler(pingPongHandler);
        _messageRouter.RegisterHandler(authHandler);
        _messageRouter.RegisterHandler(heartbeatHandler);
        _messageRouter.RegisterHandler(eventHandler);

        // 保存 PingPongHandler 引用以便在 Dispose 时取消订阅
        _pingPongHandler = pingPongHandler;
    }

    /// <summary>
    /// 重连时重置状态
    /// </summary>
    /// <remarks>
    /// 在 WebSocket 重连成功后调用，重置消息序号验证器和去重器的状态，
    /// 避免旧状态影响新连接的消息处理。
    /// </remarks>
    private async Task ResetStateOnReconnectAsync()
    {
        _logger.LogDebug("重连成功，重置消息序号验证器和去重器状态");

        _sequenceValidator?.Reset();

        // P1-6 修复：清理二进制处理器中残留的半包，
        // 否则重连后首条消息会被拼上旧连接的残片，且该状态无法自愈。
        _binaryProcessor?.Reset();

        if (_seqIdDeduplicator != null)
        {
            await _seqIdDeduplicator.ClearCacheAsync();
        }

        _logger.LogInformation("重连状态重置完成");
    }

    /// <summary>
    /// 建立WebSocket连接
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public async Task ConnectAsync(WsEndpointResult endpoint, CancellationToken cancellationToken = default)
    {
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));

        // P0-2 修复：为 WebSocket 连接建立创建分布式追踪 Span
        using var connectActivity = FeishuActivitySource.Instance.StartActivity(
            FeishuActivitySource.ActivityNameWebSocketConnect,
            ActivityKind.Client);
        connectActivity?.SetTag(FeishuActivitySource.Tags.AppKey, _options.AppKey);

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            // 取消并释放旧的 CTS，等待旧的后台任务退出
            await StopBackgroundTasksAsync();

            Volatile.Write(ref _connectionState, 2);

            await _connectionManager.ConnectAsync(endpoint.Url, cancellationToken);

            // 从 WebSocket URL 中提取 service_id 并注入心跳管理器（对照 Java SDK）
            var serviceId = FrameBuilder.ExtractServiceId(endpoint.Url);
            if (serviceId.HasValue)
            {
                _heartbeatManager.SetServiceId(serviceId.Value);
            }
            else
            {
                _logger.LogWarning("无法从 WebSocket URL 提取 service_id，心跳将使用默认值 0");
            }

            Volatile.Write(ref _connectionState, 1);

            // WS-10 修复（P1-2）：在启动 _receiveTask 之前 await ResetStateOnReconnectAsync，
            // 确保重连后的首条消息不会被旧连接的残留状态污染。
            // 首次连接时 ResetStateOnReconnectAsync 是幂等的（各组件初始状态即清零）。
            await ResetStateOnReconnectAsync();

            // 创建与调用方 Token 链接的新 CTS
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cancellationTokenSource.Token;

            // 启动消息接收
            _receiveTask = Task.Run(() => StartReceivingAsyncInternal(token), token);

            // 启动心跳
            _heartbeatTask = Task.Run(() => _heartbeatManager.StartHeartbeatAsync(token), token);

            connectActivity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            connectActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            // 修复：连接失败时必须把状态从"连接中(2)"复位为"未连接(0)"。
            // 此前失败后状态永久停留在 2，对外表现为一直处于"连接中"。
            Volatile.Write(ref _connectionState, 0);
            throw;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    /// <summary>
    /// 停止后台任务并等待其退出
    /// </summary>
    private async Task StopBackgroundTasksAsync()
    {
        var oldCts = _cancellationTokenSource;
        if (oldCts != null)
        {
            try { oldCts.Cancel(); }
            catch (ObjectDisposedException) { }

            // 等待旧的后台任务退出（带超时避免死锁）
            var tasks = new List<Task>();
            if (_receiveTask != null) tasks.Add(_receiveTask);
            if (_heartbeatTask != null) tasks.Add(_heartbeatTask);

            if (tasks.Count > 0)
            {
                try
                {
                    var allTask = Task.WhenAll(tasks);
                    var completed = await Task.WhenAny(allTask, Task.Delay(TimeSpan.FromSeconds(5)));
                    if (completed != allTask)
                    {
                        // P1-2 修复：超时后必须强制关闭底层连接。
                        // 此前仅记录告警就继续，会导致旧 socket 仍处于 Open 状态：
                        // 一是 ConnectAsync 会走进"已 Open → DisconnectAsync"路径造成自死锁（P0-2），
                        // 二是旧接收循环可能与新循环同时读取新 socket 造成"双接收循环"。
                        _logger.LogWarning("等待后台任务退出超时（5秒），强制关闭底层连接");
                        try
                        {
                            await _connectionManager.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (Exception disconnectEx)
                        {
                            _logger.LogDebug(disconnectEx, "强制关闭底层连接时发生异常（可忽略）");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "后台任务退出时发生异常（可忽略）");
                }
            }

            oldCts.Dispose();
            _cancellationTokenSource = null;
            _receiveTask = null;
            _heartbeatTask = null;
        }
    }

    /// <summary>
    /// 建立WebSocket连接并进行认证
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public async Task ConnectAsync(WsEndpointResult endpoint, string appAccessToken, CancellationToken cancellationToken = default)
    {
        await ConnectAsync(endpoint, cancellationToken);

        // 重置认证状态：确保每次新连接（包括重连）都重新进行认证，
        // 避免旧连接的 _isAuthenticated=true 导致新连接跳过认证。
        _authManager.ResetAuthentication();

        await _authManager.AuthenticateAsync(appAccessToken, cancellationToken);

        // 认证成功后，自动订阅事件
        if (_subscriptionManager.HasSubscribed)
        {
            _logger.LogInformation("自动重新订阅事件类型...");
            await _subscriptionManager.SendSubscriptionRequestAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 断开WebSocket连接
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            await StopBackgroundTasksAsync();
            await _connectionManager.DisconnectAsync(cancellationToken);
            Volatile.Write(ref _connectionState, 0);
        }
        finally
        {
            _connectLock.Release();
        }
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        await _connectionManager.SendMessageAsync(message, cancellationToken);
    }


    /// <summary>
    /// 开始接收消息（公共接口实现）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步接收操作的任务</returns>
    /// <remarks>P2-8 修复：补齐默认参数，与 <see cref="IFeishuWebSocketClient"/> 契约保持一致。</remarks>
    /// <remarks>
    /// WS-16 修复（P1-14）：补齐幂等保护，防止重复调用创建双接收循环。
    /// </remarks>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public async Task StartReceivingAsync(CancellationToken cancellationToken = default)
    {
        // WS-16：幂等保护 - 如果已有接收循环在运行，直接返回
        if (_receiveTask is { IsCompleted: false })
        {
            _logger.LogWarning("StartReceivingAsync 已被调用且接收循环仍在运行，跳过重复调用");
            return;
        }

        await StartReceivingAsyncInternal(cancellationToken);
    }

    /// <summary>
    /// 开始接收消息（内部实现）
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
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
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
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
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
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
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
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
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
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
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
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
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
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
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
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
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
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
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
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
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    private async Task HandleReceivedMessageAsync(ArraySegment<byte> buffer, WebSocketReceiveResult result, CancellationToken cancellationToken)
    {
        try
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count);

                _logger.LogDebug("接收到文本消息，长度: {MessageLength}",
                        message.Length);

                var messageReceivedHandler = MessageReceived;
                messageReceivedHandler?.Invoke(this, new WebSocketMessageEventArgs
                {
                    Message = message,
                    MessageType = result.MessageType,
                    EndOfMessage = result.EndOfMessage,
                    MessageSize = buffer.Count
                });

                // 消息仅由 MessageRouter 处理，不再同时入队 MessageQueueManager 避免双重处理
                // WS-03 修复：并发租约在 Task.Run 内部获取，保持 fire-and-forget 语义的同时引入并发上界
                _ = Task.Run(async () =>
                {
                    // WS-03：租约在 Task.Run 内部获取，避免阻塞接收循环
                    IDisposable? lease = null;
                    if (_concurrencyService != null)
                    {
                        try
                        {
                            lease = await _concurrencyService.AcquireAsync(cancellationToken);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                    try
                    {
                        // P0-2 修复：为 WebSocket 消息处理创建分布式追踪 Span
                        using var wsActivity = FeishuActivitySource.Instance.StartActivity(
                            FeishuActivitySource.ActivityNameWebSocketMessage,
                            ActivityKind.Internal);
                        wsActivity?.SetTag(FeishuActivitySource.Tags.AppKey, _options.AppKey);
                        wsActivity?.SetTag(FeishuActivitySource.Tags.MessageType, "text");

                        using (FeishuMetricsHelper.RecordEventHandling(_options.AppKey, "websocket_message", "text"))
                        using (FeishuMetricsHelper.RecordWebSocketMessageProcessing(_options.AppKey, "text"))
                        {
                            await _messageRouter.RouteMessageAsync(message, cancellationToken);
                        }

                        wsActivity?.SetStatus(ActivityStatusCode.Ok);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "消息处理任务执行失败，不影响接收管道");
                        var handler = Error;
                        handler?.Invoke(this, new WebSocketErrorEventArgs
                        {
                            Exception = ex,
                            ErrorMessage = $"消息处理错误: {ex.Message}",
                            ErrorType = "MessageProcessingError",
                            IsRecoverable = true
                        });
                    }
                    finally
                    {
                        lease?.Dispose();
                    }
                }, cancellationToken);
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                // P0-1 修复：_receiveBuffer 由接收循环复用，而下面的处理是 fire-and-forget，
                // 直接把 buffer.Array 传出去会让处理线程与下一次 ReceiveAsync 形成数据竞争，
                // 造成 protobuf 帧内容被覆写（表现为随机解析失败、SeqID 错乱、字段张冠李戴）。
                // 必须在返回前拷贝到本次消息私有的数组。
                var ownedBuffer = new byte[result.Count];
                Buffer.BlockCopy(buffer.Array!, buffer.Offset, ownedBuffer, 0, result.Count);

                // P1-14 修复：认证完成前的业务帧闸门（默认 AuthGateTimeoutMs=0，即保持历史行为）
                if (_options.AuthGateTimeoutMs > 0 && !_authManager.IsAuthenticated)
                {
                    if (!await WaitForAuthenticationAsync(_options.AuthGateTimeoutMs, cancellationToken))
                    {
                        _logger.LogWarning("连接在认证完成前收到二进制业务帧，已丢弃（等待 {TimeoutMs}ms 仍未认证）",
                            _options.AuthGateTimeoutMs);
                        return;
                    }
                }

                // WS-03 修复：并发租约在 Task.Run 内部获取，保持 fire-and-forget 语义的同时引入并发上界
                _ = Task.Run(async () =>
                {
                    // WS-03：租约在 Task.Run 内部获取，避免阻塞接收循环
                    IDisposable? lease = null;
                    if (_concurrencyService != null)
                    {
                        try
                        {
                            lease = await _concurrencyService.AcquireAsync(cancellationToken);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                    try
                    {
                        // P0-2 修复：为 WebSocket 二进制消息处理创建分布式追踪 Span
                        using var wsActivity = FeishuActivitySource.Instance.StartActivity(
                            FeishuActivitySource.ActivityNameWebSocketMessage,
                            ActivityKind.Internal);
                        wsActivity?.SetTag(FeishuActivitySource.Tags.AppKey, _options.AppKey);
                        wsActivity?.SetTag(FeishuActivitySource.Tags.MessageType, "binary");

                        using (FeishuMetricsHelper.RecordEventHandling(_options.AppKey, "websocket_message", "binary"))
                        using (FeishuMetricsHelper.RecordWebSocketMessageProcessing(_options.AppKey, "binary"))
                        {
                            await _binaryProcessor.ProcessBinaryDataAsync(ownedBuffer, 0, ownedBuffer.Length, result.EndOfMessage, cancellationToken);
                        }

                        wsActivity?.SetStatus(ActivityStatusCode.Ok);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "二进制消息处理任务执行失败，不影响接收管道");
                        var handler = Error;
                        handler?.Invoke(this, new WebSocketErrorEventArgs
                        {
                            Exception = ex,
                            ErrorMessage = $"二进制消息处理错误: {ex.Message}",
                            ErrorType = "BinaryMessageProcessingError",
                            IsRecoverable = true
                        });
                    }
                    finally
                    {
                        lease?.Dispose();
                    }
                }, cancellationToken);
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "解析 JSON 消息失败，消息大小: {MessageSize}",
                buffer.Count);
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
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
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
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
            var handler = Error;
            handler?.Invoke(this, new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = $"消息处理错误: {ex.GetType().Name}",
                ErrorType = "MessageProcessingError",
                IsRecoverable = true
            });
        }
    }

    /// <summary>
    /// 等待认证完成（P1-14 认证闸门）。
    /// </summary>
    /// <param name="timeoutMs">最长等待时间（毫秒）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证在超时前完成返回 true，否则返回 false</returns>
    /// <remarks>
    /// 接收循环在认证完成之前就已启动，服务端若在认证完成前下发业务帧会被当作合法事件分发。
    /// 该方法为可选防护（由 <see cref="FeishuWebSocketOptions.AuthGateTimeoutMs"/> 控制，默认关闭）。
    /// </remarks>
    private async Task<bool> WaitForAuthenticationAsync(int timeoutMs, CancellationToken cancellationToken)
    {
        // WS-27 修复（P2-18）：Environment.TickCount 是 int，运行 ~24.8 天后溢出变负数，
        // 导致 deadline 计算错误。net6+ 使用 Environment.TickCount64（long），
        // ns2.0 退化为 Stopwatch（兼容性最佳）。
#if NET6_0_OR_GREATER
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!_authManager.IsAuthenticated)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            var remaining = deadline - Environment.TickCount64;
            if (remaining <= 0)
                return false;

            await Task.Delay((int)Math.Min(remaining, 50), cancellationToken);
        }
#else
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!_authManager.IsAuthenticated)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            var remaining = timeoutMs - (int)stopwatch.ElapsedMilliseconds;
            if (remaining <= 0)
                return false;

            await Task.Delay(Math.Min(remaining, 50), cancellationToken);
        }
#endif

        return true;
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
    /// 异步释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        try
        {
            _cancellationTokenSource?.Cancel();
            await StopBackgroundTasksAsync();
            UnsubscribeFromComponentEvents();
            UnsubscribeFromHandlerEvents();

            if (_connectionManager is IAsyncDisposable asyncDisposableConn)
                await asyncDisposableConn.DisposeAsync();
            else
                _connectionManager?.Dispose();

            // P1-3 修复：BinaryMessageProcessor.Dispose 为同步阻塞实现，
            // 在异步释放路径上应改用 DisposeAsync，避免阻塞线程。
            if (_binaryProcessor != null)
                await _binaryProcessor.DisposeAsync();

            _connectLock?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步释放资源时发生错误");
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <remarks>
    /// WS-08 修复（P1-3）：同步释放路径补齐对在途后台任务的尽力等待（2s 超时）
    /// 和链接 CTS 的释放。需确定性停止请调用 <see cref="DisposeAsync"/>。
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        try
        {
            _cancellationTokenSource?.Cancel();

            // WS-08：同步路径尽力等待在途后台任务退出（2s 超时）
            var tasks = new List<Task>();
            if (_receiveTask != null) tasks.Add(_receiveTask);
            if (_heartbeatTask != null) tasks.Add(_heartbeatTask);
            if (tasks.Count > 0)
            {
                try
                {
                    Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "同步释放等待后台任务退出时发生异常（可忽略）");
                }
            }

            // WS-08：释放链接 CTS 并置 null
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _receiveTask = null;
            _heartbeatTask = null;

            UnsubscribeFromComponentEvents();
            UnsubscribeFromHandlerEvents();
            _connectionManager?.Dispose();
            _binaryProcessor?.Dispose();
            _connectLock?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放资源时发生错误");
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 取消处理器事件订阅
    /// </summary>
    private void UnsubscribeFromHandlerEvents()
    {
        if (_pingPongHandler != null)
        {
            _pingPongHandler.PongReceived -= _onPongReceivedText;
            _pingPongHandler = null;
        }

        // 清理订阅管理器
        _subscriptionManager?.ClearSubscriptions();
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
            _binaryProcessor.PongReceived -= _onPongReceivedBinary;
        }

    }
}