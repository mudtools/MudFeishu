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
using System.Buffers;
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
    // F4 修复：持有 IOptionsMonitor 而非一次性快照；P2-2 修复后通过 Options 属性真正参与运行期读取。
    private readonly IOptionsMonitor<FeishuWebSocketOptions>? _optionsMonitor;
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

    // P2-2 修复（WS2-07）：删除 _connectionState 双真源字段（原 0=未连接/1=已连接/2=连接中）。
    // 该字段只在 ConnectAsync/DisconnectAsync 两处被写、且从不感知"接收循环已退出"，
    // 因此在"接收循环静默终止但 socket 仍 Open"的僵尸态下恒为 1 ⇒ IsConnected 恒为 true。
    // 现在"是否处于连接中"由 _connectionManager.State（Connecting/Open/...）唯一表达（I12），
    // "是否可用"由 连接状态 ∧ 接收循环存活 派生。
    //
    // WS2-02 / I14：接收循环的**原子占位**。0=无在途循环，1=已有循环。
    // 此前公开 StartReceivingAsync 的幂等守卫读取的是 _receiveTask——而该字段只会被 ConnectAsync
    // 赋值，启动方自身从不登记，因此守卫在"循环已结束但 socket 仍 Open"与"DisconnectAsync 正把
    // _receiveTask 置 null"两类窄窗口下失效。现在由本字段承担唯一的"是否已有循环"判定。
    private int _receiveLoopActive = 0;

    /// <summary>
    /// 最近一次收到帧的 UTC 时刻（<see cref="DateTime.Ticks"/>；0 = 尚无样本）。
    /// </summary>
    /// <remarks>
    /// F1 存活探针：以 <see cref="Interlocked"/> 读写，避免为一次心跳级更新引入锁。
    /// 刷新点是 <see cref="HandleReceivedMessageAsync"/> 入口——即"服务端下发的帧已被本端取出"。
    /// </remarks>
    private long _lastReceiveTicks = 0;

    // 处理器引用
    private PingPongMessageHandler? _pingPongHandler;

    /// <summary>
    /// 运行期配置视图（P2-2 修复）。
    /// </summary>
    /// <remarks>
    /// 此前 <c>_optionsMonitor</c> 赋值后从不读取，F4 声称的"热更新"并未落地（属注释与实现不一致）。
    /// 本属性让<b>确实支持热更新</b>的字段（<see cref="FeishuWebSocketOptions.AppKey"/> 指标维度、
    /// <see cref="FeishuWebSocketOptions.AuthGateTimeoutMs"/> 认证闸门）在每次读取时取最新值。
    /// <para>
    /// <b>口径说明</b>：其余配置项（心跳间隔、连接超时、消息大小限制、并发上界、去重配置等）已在构造期
    /// 固化到各子组件（<c>MessageRouter</c>/<c>BinaryMessageProcessor</c>/<c>AuthenticationManager</c>/
    /// <c>HeartbeatManager</c> 等），<b>不支持</b>运行期热更新；需生效请重启进程或重建客户端。
    /// </para>
    /// </remarks>
    private FeishuWebSocketOptions Options => _optionsMonitor?.CurrentValue ?? _options;

    /// <inheritdoc/>
    public WebSocketState State => _connectionManager.State;

    /// <inheritdoc/>
    /// <remarks>
    /// P2-2 修复（WS2-07 / I12）：原实现为 <c>_connectionState == 1 &amp;&amp; _connectionManager.IsConnected</c>，
    /// 其中 <c>_connectionState</c> 从不感知"接收循环已退出"，在僵尸态下恒为 1。
    /// 现在收紧为"**连接仍为 Open 且接收循环仍在运行**"——即"连接可被用来收事件"。
    /// <para>
    /// 副作用（属语义修正）：<see cref="ConnectAsync(WsEndpointResult, CancellationToken)"/> 在握手完成、
    /// 接收循环启动之前的极短窗口内 <see cref="IsConnected"/> 为 <c>false</c>；
    /// 需要"握手是否完成"请读 <see cref="State"/>（<see cref="WebSocketState.Open"/>）。
    /// </para>
    /// </remarks>
    public bool IsConnected => _connectionManager.IsConnected && _receiveTask is { IsCompleted: false };

    /// <inheritdoc/>
    public bool IsAuthenticated => _authManager.IsAuthenticated;

    /// <summary>
    /// 连接存活探针快照（F1）。
    /// </summary>
    /// <remarks>
    /// 供健康检查与运维取证使用：把"是否静默僵死"从"由 <see cref="WebSocketState"/> 推断"变为可观测事实。
    /// </remarks>
    internal ConnectionLiveness Liveness
    {
        get
        {
            var ticks = Interlocked.Read(ref _lastReceiveTicks);
            return new ConnectionLiveness(
                receiveLoopAlive: _receiveTask is { IsCompleted: false },
                lastReceiveUtc: ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : null,
                isConnected: _connectionManager.IsConnected);
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
    /// <param name="eventDeduplicator">事件级去重服务（按 event_id 去重，可选）</param>
    /// <param name="interceptors">事件拦截器集合</param>
    /// <param name="options">WebSocket配置选项</param>
    /// <param name="seqIdDeduplicator">SeqID去重服务（可选）</param>
    /// <param name="sessionManager">会话管理器（可选）</param>
    /// <param name="sequenceValidator">消息序号验证器（可选）</param>
    /// <param name="concurrencyService">并发控制服务（可选，WS-03 修复引入）</param>
    /// <param name="optionsMonitor">WebSocket 配置选项监控器（可选，F4 修复引入，支持热更新）</param>
    /// <param name="unifiedDedupMiddleware">统一去重中间件（可选，F7 修复引入）</param>
    /// <param name="hostEnvironment">宿主环境（可选，R5.2.7/X5 生产加固引入）。由 DI 自动解析；用于在生产环境把证书安全旁路告警升级为 LogError</param>
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
        IUnifiedDeduplicationMiddleware? unifiedDedupMiddleware = null,
        Microsoft.Extensions.Hosting.IHostEnvironment? hostEnvironment = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventHandlerFactory = eventHandlerFactory ?? throw new ArgumentNullException(nameof(eventHandlerFactory));
        _interceptors = interceptors ?? Array.Empty<IFeishuEventInterceptor>();
        _optionsMonitor = optionsMonitor;
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
        _connectionManager = new WebSocketConnectionManager(_loggerFactory.CreateLogger<WebSocketConnectionManager>(), _options, _loggerFactory, hostEnvironment);
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

        // WS2-12：options 参数已删除（原参数只赋值给一个从不读取的私有字段）
        var heartbeatHandler = new HeartbeatMessageHandler(_loggerFactory.CreateLogger<HeartbeatMessageHandler>());

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
    /// <param name="endpoint">WebSocket 端点信息</param>
    /// <param name="cancellationToken">
    /// <b>仅约束建连阶段</b>（TCP/WS 握手 + 后续认证）。该令牌<b>不构成连接生命周期</b>——
    /// 连接建立后再取消它不会中断接收循环或心跳；终止连接请调用
    /// <see cref="DisconnectAsync"/> / <see cref="DisposeAsync"/>（架构不变量 I15）。
    /// </param>
    /// <returns>连接任务</returns>
    /// <remarks>
    /// <b>令牌契约（D2 / I15，行为变更）</b>：接收循环与心跳使用客户端<b>自持</b>的
    /// <see cref="CancellationTokenSource"/>，不再与 <paramref name="cancellationToken"/> 链接。
    /// 此前把两者链接导致"调用方的短命令牌取消 → socket 仍为 Open 但不再读帧、无任何断线通知、
    /// 健康检查仍报 Healthy"的僵尸连接（P0-1 的触发路径之一）。
    /// </remarks>
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
        connectActivity?.SetTag(FeishuActivitySource.Tags.AppKey, Options.AppKey);

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            // 取消并释放旧的 CTS，等待旧的后台任务退出
            await StopBackgroundTasksAsync();

            // P2-2（WS2-07）：不再写 _connectionState="连接中"。
            // "连接中"由 _connectionManager.State（WebSocketState.Connecting）唯一表达。

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

            // WS-10 修复（P1-2）：在启动 _receiveTask 之前 await ResetStateOnReconnectAsync，
            // 确保重连后的首条消息不会被旧连接的残留状态污染。
            // 首次连接时 ResetStateOnReconnectAsync 是幂等的（各组件初始状态即清零）。
            await ResetStateOnReconnectAsync();

            // I15（D2）行为变更：连接生命周期使用客户端**自持**的 CTS，不再链接调用方令牌。
            // 调用方令牌只在此前的握手/（上层）认证阶段生效。
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // I14：循环任务由启动方登记（含原子占位）
            StartReceiveLoop(token);

            // 启动心跳
            _heartbeatTask = Task.Run(() => _heartbeatManager.StartHeartbeatAsync(token), token);

            connectActivity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            connectActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            // P2-2：握手失败时无状态需要复位——_connectionManager.State 自身反映
            // None/Closed/Aborted，"连接中"语义不再由客户端另行维护（原实现会让状态永久停在"连接中"）。
            throw;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    /// <summary>
    /// 启动接收循环并登记到 <c>_receiveTask</c>（I14）。
    /// </summary>
    /// <param name="token">连接生命周期令牌（客户端自持）</param>
    /// <remarks>
    /// 所有"启动接收循环"的入口都必须经过本方法，以保证：
    /// ① <c>_receiveLoopActive</c> 原子占位被置位；② 循环任务被登记（停机等待与
    /// <see cref="IsConnected"/> 的存活判定都依赖它）。
    /// </remarks>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    private void StartReceiveLoop(CancellationToken token)
    {
        Interlocked.Exchange(ref _receiveLoopActive, 1);
        _receiveTask = Task.Run(() => StartReceivingAsyncInternal(token), token);
    }

    /// <summary>
    /// 停止后台任务并等待其退出
    /// </summary>
    private async Task StopBackgroundTasksAsync()
    {
        // ────────────────────────────────────────────────────────────────────
        // I13 顺序约束：**先结束连接（占位 + 关闭握手），再取消后台任务**。
        //
        // 若颠倒（先取消接收循环令牌）会出现两类问题：
        //  ① 归属错误：接收循环因取消退出时 socket 仍为 Open，会按"接收循环被取消"补发
        //     Disconnected（见 WebSocketConnectionManager 的取消分支），把本端**主动**重连/断开
        //     描述成异常断线；
        //  ② 竞态：接收循环异步退出，与后续 CM.DisconnectCoreAsync 的原子占位互相抢先，
        //     事件有可能一次都不派发（双方都认为对方已占位）。
        //
        // 由 CM 完成"占位 + 关闭"还带来一个确定性收益：事件归属与描述固定为
        // "客户端主动断开连接 / IsServerInitiated=false / NormalClosure"。
        // 连接不存在或已关闭时 CM.DisconnectAsync 直接返回（幂等，无事件）。
        // ────────────────────────────────────────────────────────────────────
        try
        {
            await _connectionManager.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // 客户端已 Dispose（关停竞态）：无连接可结束
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "结束连接时发生异常（可忽略，后续仍会取消后台任务）");
        }

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
                        // P1-2 修复：超时后必须确保底层连接已关闭。
                        // 此前仅记录告警就继续，会导致旧 socket 仍处于 Open 状态：
                        // 一是 ConnectAsync 会走进"已 Open → DisconnectAsync"路径造成自死锁（P0-2），
                        // 二是旧接收循环可能与新循环同时读取新 socket 造成"双接收循环"。
                        // 注：方法入口已执行过一次确定性断开；此处为双保险（幂等，连接已关闭时不派发事件）。
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

        // I14 兜底：接收循环的 finally 会自行复位占位，但 `Task.Run(delegate, token)` 在
        // 令牌**已取消**时可能根本不执行委托（任务直接进入 Canceled），此时占位将永久停留在 1
        // ⇒ 后续所有"启动接收循环"的入口都会被幂等守卫拒绝（服务永久不再收帧）。
        // 本方法是唯一的收尾点（ConnectAsync / DisconnectAsync / Dispose 都经过它），在此无条件复位。
        // 注：等待超时（5s）时旧循环可能仍在收敛，但此时已强制 Abort socket，
        // 旧循环很快会因连接失效而退出并再次复位（幂等，无副作用）。
        Volatile.Write(ref _receiveLoopActive, 0);
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
            // P2-2（WS2-07）：不再维护 _connectionState —— IsConnected 由
            // "连接仍为 Open ∧ 接收循环存活" 派生，StopBackgroundTasksAsync 已使后者为假。
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
    /// <b>WS2-02 收口（P1-1 / R1 WS-16 未闭环 + I14）</b>：接收循环由
    /// <see cref="ConnectAsync(WsEndpointResult, CancellationToken)"/> 统一管理，本方法仅作兼容保留。
    /// <list type="bullet">
    /// <item>未连接时抛 <see cref="InvalidOperationException"/>（对齐
    /// <see cref="WebSocketConnectionManager.StartReceivingAsync"/> 的既有语义）；</item>
    /// <item>已有循环在运行时幂等返回并告警（判定依据为<b>原子占位</b> <c>_receiveLoopActive</c>，
    /// 而非"先检查后使用"地读 <c>_receiveTask</c>——后者由 <see cref="ConnectAsync(WsEndpointResult, CancellationToken)"/>
    /// 赋值、启动方本身从不登记，在"循环已结束但 socket 仍 Open"与"DisconnectAsync 正把
    /// <c>_receiveTask</c> 置 null"两类窗口下会失效）。</item>
    /// </list>
    /// </remarks>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    [Obsolete("接收循环由 ConnectAsync 统一管理，无需显式调用。若需重连请调用 IFeishuWebSocketManager.ReconnectAsync。")]
    public async Task StartReceivingAsync(CancellationToken cancellationToken = default)
    {
        // I14：原子占位 —— 抢占失败即表示已有循环在途，直接返回（不再读取 _receiveTask）
        if (Interlocked.CompareExchange(ref _receiveLoopActive, 1, 0) != 0)
        {
            _logger.LogWarning("StartReceivingAsync 已被调用且接收循环仍在运行，跳过重复调用（接收循环由 ConnectAsync 统一管理）");
            return;
        }

        try
        {
            if (!_connectionManager.IsConnected)
            {
                throw new InvalidOperationException(
                    "WebSocket 未连接，无法启动接收循环。接收循环由 ConnectAsync 统一管理，无需显式调用。");
            }

            // I14 的另一半：本路径同样必须把循环任务登记到 _receiveTask（停机等待与 IsConnected 依赖它）。
            var loopTask = StartReceivingAsyncInternal(cancellationToken);
            _receiveTask = loopTask;
            await loopTask;
        }
        catch
        {
            // 启动失败（未连接 / 抛异常）必须释放占位，否则后续启动入口被永久拒绝
            Volatile.Write(ref _receiveLoopActive, 0);
            throw;
        }
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
        finally
        {
            // I14：任何退出路径（含正常取消、异常、socket 关闭）都必须释放原子占位，
            // 否则 IsConnected 会把它当作"循环仍在运行"，后续启动入口也会被幂等守卫永久拒绝。
            Volatile.Write(ref _receiveLoopActive, 0);
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
            // F1 存活探针：刷新"最近收帧时刻"。放在最外层入口，任何帧类型都算一次存活证据。
            Interlocked.Exchange(ref _lastReceiveTicks, DateTime.UtcNow.Ticks);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count);

                _logger.LogDebug("接收到文本消息，长度: {MessageLength}",
                        message.Length);

                // 消息仅由 MessageRouter 处理，不再同时入队 MessageQueueManager 避免双重处理
                // P1-1 修复：并发租约改到<b>接收路径</b>获取。此前租约在 Task.Run 内部获取，
                // 只限制"同时处理数"，不限制"已排队数"——慢消费时 Task.Run 队列与消息副本无上界（单帧上限 10MB）。
                // 现在：租约在接收循环获取 → 接收循环被阻塞 = TCP 级反压，排队量与缓冲量一并受 MaxConcurrentHandlers 约束。
                var (canProcessText, textLease) = await AcquireConcurrencyLeaseAsync(cancellationToken).ConfigureAwait(false);
                if (!canProcessText)
                {
                    _logger.LogDebug("文本消息已丢弃：并发租约获取失败（连接关闭或并发服务已释放）");
                    // F5：受控丢弃必须可计数（此前只写日志，丢弃量不可观测、无法告警）
                    FeishuMetricsHelper.RecordWebSocketFramesDiscarded(
                        Options.AppKey, FeishuMetrics.DiscardReasons.ConcurrencyRejected);
                    return;
                }

                try
                {
                    // M3：Task.Run <b>不得</b>传入 cancellationToken —— 令牌已取消时委托不会执行，
                    // 而租约所有权即将移交 → 租约永久泄漏 → 接收管道最终卡死。取消由委托内部观察。
                    var ownedTextLease = textLease;
                    var frameMessage = message;
                    var frameSize = buffer.Count;
                    var frameEndOfMessage = result.EndOfMessage;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // WS2-08（P1-6）修复：MessageReceived 改为在"取得并发租约之后、任务体内"派发。
                            // 此前在接收循环线程上同步调用订阅者：慢订阅者会**直接阻塞整条接收管道**
                            // （不读帧、不回 ACK、不推进心跳），与《架构与并发模型》§6"接收路径承担背压"
                            // 的语义相矛盾——背压应向**上游 TCP** 施加，而不是被下游回调反噬。
                            //
                            // 行为变更（已登记 CHANGELOG/Readme）：该事件由"接收线程串行、与帧序一致"
                            // 变为"可能并发、可能乱序"，定位为**观测钩子**（Logging/Metrics）。
                            // 有顺序或阻塞需求的订阅者应改用 IMessageHandler（经 MessageRouter，
                            // 受 MessageHandlerTimeoutMs 保护）。
                            var messageReceivedHandler = MessageReceived;
                            messageReceivedHandler?.Invoke(this, new WebSocketMessageEventArgs
                            {
                                Message = frameMessage,
                                MessageType = WebSocketMessageType.Text,
                                EndOfMessage = frameEndOfMessage,
                                MessageSize = frameSize
                            });

                            // P0-2 修复：为 WebSocket 消息处理创建分布式追踪 Span
                            using var wsActivity = FeishuActivitySource.Instance.StartActivity(
                                FeishuActivitySource.ActivityNameWebSocketMessage,
                                ActivityKind.Internal);
                            wsActivity?.SetTag(FeishuActivitySource.Tags.AppKey, Options.AppKey);
                            wsActivity?.SetTag(FeishuActivitySource.Tags.MessageType, "text");

                            using (FeishuMetricsHelper.RecordEventHandling(Options.AppKey, "websocket_message", "text"))
                            using (FeishuMetricsHelper.RecordWebSocketMessageProcessing(Options.AppKey, "text"))
                            {
                                await _messageRouter.RouteMessageAsync(frameMessage, cancellationToken);
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
                            ownedTextLease?.Dispose();
                        }
                    });

                    textLease = null;   // 所有权已随"委托入队"移交
                }
                finally
                {
                    textLease?.Dispose();   // 派发失败（极端调度异常）时兜底，避免租约泄漏
                }
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                // P1-1 修复：租约在接收路径获取（详见文本分支注释）。
                // 注意：await AcquireAsync 会挂起接收循环，因此共享 _receiveBuffer 在等待期间不会被覆写，
                // "先取租约、后拷贝"既能限制排队缓冲数量，又不会与下一次 ReceiveAsync 形成数据竞争。
                var (canProcessBinary, binaryLease) = await AcquireConcurrencyLeaseAsync(cancellationToken).ConfigureAwait(false);
                if (!canProcessBinary)
                {
                    _logger.LogDebug("二进制消息已丢弃：并发租约获取失败（连接关闭或并发服务已释放）");
                    FeishuMetricsHelper.RecordWebSocketFramesDiscarded(
                        Options.AppKey, FeishuMetrics.DiscardReasons.ConcurrencyRejected);
                    return;
                }

                // P2-12：池化副本的"所有权跟踪"变量（须声明在 try 之外，finally 才能兜底归还）
                byte[]? pooledBuffer = null;
                try
                {
                    // P0-1 修复：_receiveBuffer 由接收循环复用，而下面的处理是 fire-and-forget，
                    // 直接把 buffer.Array 传出去会让处理线程与下一次 ReceiveAsync 形成数据竞争，
                    // 造成 protobuf 帧内容被覆写（表现为随机解析失败、SeqID 错乱、字段张冠李戴）。
                    // 必须在返回前持有本次消息的私有副本。
                    // P2-12 修复：副本改由 ArrayPool 提供，避免"每帧一次"的 Gen0/LOH 分配
                    // （大帧上限 10MB，高频投递下会显著放大分配压力）。
                    // 注意：Rent 返回的数组长度可能大于请求数量（池按 2 的幂分桶），
                    // 因此必须以 (buffer, 0, count) 三元组传递，绝不能使用 buffer.Length。
                    if (result.Count == 0)
                    {
                        pooledBuffer = Array.Empty<byte>();   // 空帧：不进池，也无需归还
                    }
                    else
                    {
                        pooledBuffer = ArrayPool<byte>.Shared.Rent(result.Count);
                        Buffer.BlockCopy(buffer.Array!, buffer.Offset, pooledBuffer, 0, result.Count);
                    }

                    // M3：同文本分支，Task.Run 不传 cancellationToken（避免租约泄漏）
                    var ownedBinaryLease = binaryLease;
                    var frameBuffer = pooledBuffer;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // P2-10 修复：认证闸门判断移入任务体。此前在接收循环上轮询等待（最长 AuthGateTimeoutMs）
                            // 会造成队头阻塞——后续帧（含 Pong 控制帧与认证响应）全部被推迟。
                            // 默认 AuthGateTimeoutMs=0 时该分支不执行（保持历史行为）。
                            var authGateTimeoutMs = Options.AuthGateTimeoutMs;
                            if (authGateTimeoutMs > 0 && !_authManager.IsAuthenticated)
                            {
                                if (!await WaitForAuthenticationAsync(authGateTimeoutMs, cancellationToken))
                                {
                                    _logger.LogWarning("连接在认证完成前收到二进制业务帧，已丢弃（等待 {TimeoutMs}ms 仍未认证）",
                                        authGateTimeoutMs);
                                    // F5：闸门丢弃单独计数——与"分片超限丢弃"的处置方式完全不同
                                    FeishuMetricsHelper.RecordWebSocketFramesDiscarded(
                                        Options.AppKey, FeishuMetrics.DiscardReasons.AuthGateTimeout);
                                    return;
                                }
                            }

                            // P0-2 修复：为 WebSocket 二进制消息处理创建分布式追踪 Span
                            using var wsActivity = FeishuActivitySource.Instance.StartActivity(
                                FeishuActivitySource.ActivityNameWebSocketMessage,
                                ActivityKind.Internal);
                            wsActivity?.SetTag(FeishuActivitySource.Tags.AppKey, Options.AppKey);
                            wsActivity?.SetTag(FeishuActivitySource.Tags.MessageType, "binary");

                            using (FeishuMetricsHelper.RecordEventHandling(Options.AppKey, "websocket_message", "binary"))
                            using (FeishuMetricsHelper.RecordWebSocketMessageProcessing(Options.AppKey, "binary"))
                            {
                                await _binaryProcessor.ProcessBinaryDataAsync(frameBuffer, 0, result.Count, result.EndOfMessage, cancellationToken);
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
                            ownedBinaryLease?.Dispose();

                            // P2-12：副本所有权随任务结束归还池（空帧不归还）
                            if (frameBuffer.Length > 0)
                            {
                                ArrayPool<byte>.Shared.Return(frameBuffer);
                            }
                        }
                    });

                    binaryLease = null;    // 租约所有权已随"委托入队"移交
                    pooledBuffer = null;   // 副本所有权已随"委托入队"移交
                }
                finally
                {
                    binaryLease?.Dispose();   // 派发失败兜底

                    // P2-12：派发失败（未移交所有权）时归还池化副本，避免池泄漏
                    if (pooledBuffer != null && pooledBuffer.Length > 0)
                    {
                        ArrayPool<byte>.Shared.Return(pooledBuffer);
                    }
                }
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
    /// 在接收路径获取并发租约（P1-1 修复：背压前移到"排队阶段"）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>
    /// <c>CanProcess</c> 表示是否可以继续处理本帧；<c>Lease</c> 为租约
    /// （未注入并发服务时为 <c>null</c>，表示不做并发记账）。
    /// </returns>
    /// <remarks>
    /// 该方法是"背压闸门"：等待期间接收循环处于挂起状态，从而把反压传递到 TCP 层，使"已排队任务/已排队缓冲"
    /// 同样受 <see cref="FeishuWebSocketOptions.MaxConcurrentHandlers"/> 约束。
    /// 未注入 <see cref="FeishuWebSocketConcurrencyService"/> 时直接返回 <c>(true, null)</c>，行为与改造前一致。
    /// <para>I10：正因为接收循环会在背压时暂停，消息处理器<b>不得</b>反向依赖接收循环的推进
    /// （例如在同一 socket 上做请求-响应式等待），否则构成循环等待。</para>
    /// </remarks>
    private async Task<(bool CanProcess, IDisposable? Lease)> AcquireConcurrencyLeaseAsync(CancellationToken cancellationToken)
    {
        if (_concurrencyService == null)
            return (true, null);

        try
        {
            var lease = await _concurrencyService.AcquireAsync(cancellationToken).ConfigureAwait(false);
            return (lease != null, lease);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return (false, null);   // 连接/应用正在关闭：丢弃本帧
        }
        catch (ObjectDisposedException)
        {
            return (false, null);   // 并发服务已释放（关停竞态）：丢弃本帧，避免异常灌入接收循环
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

            // P1-5b 修复（I9）：不再释放 _connectLock —— 在途 ConnectAsync/DisconnectAsync 仍可能
            // WaitAsync/Release 该信号量，释放会构成 ObjectDisposedException 竞态；不释放无 OS 句柄泄漏。
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
            // I14：同步释放路径不经 StopBackgroundTasksAsync，占位需显式复位
            Volatile.Write(ref _receiveLoopActive, 0);

            UnsubscribeFromComponentEvents();
            UnsubscribeFromHandlerEvents();
            _connectionManager?.Dispose();
            _binaryProcessor?.Dispose();
            // P1-5b 修复（I9）：同 DisposeAsync，不释放 _connectLock（消除释放竞态，无句柄泄漏）
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