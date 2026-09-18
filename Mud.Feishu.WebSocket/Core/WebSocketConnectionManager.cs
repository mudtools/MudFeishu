// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.SocketEventArgs;
using Mud.HttpUtils;
using System.Net.Security;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// WebSocket连接管理器，提供飞书WebSocket连接的完整生命周期管理
/// </summary>
/// <remarks>
/// 该类负责WebSocket连接的建立、维护、断开和消息收发功能。
/// 支持自动重连、连接超时、错误处理和资源清理等企业级特性。
/// 锁策略（P0-2/P0-6 修复）：
/// <list type="bullet">
/// <item><c>_connectionLock</c>：仅保护连接生命周期（建立/断开/替换 socket），<b>不可重入</b>；
/// 内部方法一律使用无锁版本 <c>DisconnectCoreAsync</c>。</item>
/// <item><c>_sendLock</c>：仅保护发送，与生命周期解耦，避免慢发送阻塞连接/重连。</item>
/// </list>
/// 事件策略（P0-5 修复）：所有用户事件均在锁外触发，避免在持锁期间回调用户代码造成死锁。
/// </remarks>
public class WebSocketConnectionManager : IAsyncDisposable, IDisposable
{
    // WS-17 修复（P1-13）：将 _connectionCount 从 static 改为实例字段。
    // 此前所有 manager 实例共享一个静态计数器，多应用场景下各实例的连接数互相干扰。
    // 现在每个 manager 实例独立维护自己的连接计数。
    private int _connectionCount = 0;
    private readonly ILogger<WebSocketConnectionManager> _logger;
    private readonly FeishuWebSocketOptions _options;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    // WS-15 修复（P1-12）：_disposed 改为 int + Interlocked.Exchange 实现原子 check-then-set。
    // 此前 volatile bool 的 check-then-set 非原子，并发调用 Dispose/DisposeAsync 可能双进入释放逻辑。
    private int _disposed = 0;
    private byte[]? _receiveBuffer;
    private readonly ErrorRecoveryStrategy _errorRecoveryStrategy;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// 断线事件触发标志（原子）：0=已连接且尚未触发断线事件，1=已触发/未连接。
    /// <para>使用 <see cref="Interlocked"/> 保证 check-then-set 的原子性（P0-4 修复）。</para>
    /// </summary>
    private int _disconnectedFired = 1;

    /// <summary>
    /// 关闭握手超时时间，避免服务端不应答时无限等待（P1-4 修复）。
    /// </summary>
    private static readonly TimeSpan CloseHandshakeTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 获取当前WebSocket连接数（实例级别）
    /// </summary>
    /// <remarks>
    /// WS-17 修复（P1-13）：从 static 改为实例属性，每个 manager 独立计数，
    /// 避免多应用场景下各实例连接数互相干扰。
    /// </remarks>
    public int ConnectionCount => Volatile.Read(ref _connectionCount);

    /// <summary>
    /// WebSocket连接成功建立时触发的事件
    /// </summary>
    /// <remarks>该事件保证在连接锁之外触发，回调中可安全调用发送等需要加锁的方法。</remarks>
    public event EventHandler<EventArgs>? Connected;

    /// <summary>
    /// WebSocket连接断开时触发的事件
    /// </summary>
    /// <remarks>该事件保证在连接锁之外触发；对同一次连接最多触发一次。</remarks>
    public event EventHandler<WebSocketCloseEventArgs>? Disconnected;

    /// <summary>
    /// WebSocket连接发生错误时触发的事件
    /// </summary>
    public event EventHandler<WebSocketErrorEventArgs>? Error;

    /// <summary>
    /// 获取当前WebSocket连接的状态
    /// </summary>
    /// <returns>WebSocket连接状态，如果未初始化则返回None</returns>
    public WebSocketState State => _webSocket?.State ?? WebSocketState.None;

    /// <summary>
    /// 获取WebSocket是否已连接并处于活动状态
    /// </summary>
    /// <returns>如果WebSocket处于Open状态返回true，否则返回false</returns>
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    /// <summary>
    /// 初始化WebSocket连接管理器实例
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="options">WebSocket配置选项，如果为null则使用默认配置</param>
    /// <param name="loggerFactory">日志工厂，用于创建ErrorRecoveryStrategy的日志记录器</param>
    /// <exception cref="ArgumentNullException">当logger为null时抛出</exception>
    public WebSocketConnectionManager(
        ILogger<WebSocketConnectionManager> logger,
        FeishuWebSocketOptions options,
        ILoggerFactory loggerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FeishuWebSocketOptions();
        _loggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        _errorRecoveryStrategy = new ErrorRecoveryStrategy(_loggerFactory.CreateLogger<ErrorRecoveryStrategy>());
    }

    /// <summary>
    /// 连接到WebSocket服务器
    /// </summary>
    /// <param name="url">WebSocket服务器URL，必须使用ws://或wss://协议</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步连接操作的任务</returns>
    /// <exception cref="ArgumentException">当URL为空、格式无效或协议不正确时抛出</exception>
    /// <exception cref="TimeoutException">当连接超时时抛出</exception>
    /// <exception cref="WebSocketException">当WebSocket连接失败时抛出</exception>
    /// <remarks>
    /// 如果当前已有连接，会先断开现有连接再建立新连接。
    /// 连接超时时间由<see cref="FeishuWebSocketOptions.ConnectionTimeoutMs"/>配置决定。
    /// </remarks>
    public async Task ConnectAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("WebSocket URL不能为空", nameof(url));

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException("无效的WebSocket URL格式", nameof(url));

        if (uri.Scheme != "ws" && uri.Scheme != "wss")
            throw new ArgumentException("WebSocket URL必须使用ws://或wss://协议", nameof(url));

        // 安全校验：默认禁止不安全的 ws:// 连接
        if (uri.Scheme == "ws" && !_options.AllowInsecureWebSocket)
            throw new ArgumentException("WebSocket URL使用不安全的ws://协议。如需在开发/测试环境使用，请设置 AllowInsecureWebSocket = true", nameof(url));

        await _connectionLock.WaitAsync(cancellationToken);
        WebSocketCloseEventArgs? pendingClose = null;
        try
        {
            // P0-2 修复：此处已持有 _connectionLock，SemaphoreSlim 不可重入，
            // 必须调用无锁版本 DisconnectCoreAsync，否则将永久挂起。
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                pendingClose = await DisconnectCoreAsync(cancellationToken);
            }

            // P1-2 修复：创建连接前彻底释放上一次连接的 socket 与 CTS
            DisposeCurrentSocket();

            // 创建新的WebSocket连接
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            // 启用协议级 WebSocket Ping/Pong 保活（对齐 Python websockets 库默认行为）。
            // Python SDK 的 websockets 库默认每 20 秒发送协议级 Ping 帧，
            // .NET ClientWebSocket 默认 KeepAliveInterval=Zero（禁用）。
            // 启用后，.NET 运行时会自动发送 WebSocket Ping (opcode 0x9)，
            // 服务端回复 Pong (opcode 0xA)，保持中间网络设备（NAT/负载均衡器）的连接表项不超时。
            // 注意：该值与 FeishuWebSocketOptions.HeartbeatIntervalMs（应用层 ProtoBuf Ping）职责不同：
            // 前者用于链路存活检测，后者用于维持飞书应用层会话，二者不应互相替代。
            // F5 修复：从 FeishuWebSocketOptions.ProtocolKeepAliveInterval 读取配置（默认 20s），
            // 替代硬编码值。设为 Zero 时禁用协议级保活。
            _webSocket.Options.KeepAliveInterval = _options.ProtocolKeepAliveInterval;

            // 配置SSL/TLS证书验证
            ConfigureCertificateValidation(_webSocket, uri);

            // 设置连接超时
            using var timeoutCts = new CancellationTokenSource(_options.ConnectionTimeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token,
                _cancellationTokenSource.Token);

            try
            {
                await _webSocket.ConnectAsync(uri, combinedCts.Token);

                if (_options.EnableLogging)
                {
                    _logger.LogInformation("已连接到飞书WebSocket服务: {Url}", url);
                }

                // P0-4 修复：只有连接真正成功后才配平计数并允许触发断线事件。
                // 顺序必须为 Increment → 清除断线标志，避免连接失败后 Decrement 未配平导致计数为负。
                Interlocked.Increment(ref _connectionCount);
                Interlocked.Exchange(ref _disconnectedFired, 0);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                _logger.LogError("连接飞书WebSocket服务超时");
                throw new TimeoutException("连接飞书WebSocket服务超时");
            }
        }
        finally
        {
            _connectionLock.Release();
        }

        // P0-5 修复：事件在锁外触发，允许回调安全地调用 SendMessageAsync / DisconnectAsync 等加锁方法。
        if (pendingClose != null)
        {
            SafeInvokeDisconnected(pendingClose);
        }

        SafeInvokeConnected();
    }

    /// <summary>
    /// 断开WebSocket连接
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步断开操作的任务</returns>
    /// <remarks>
    /// 如果连接已经关闭，此方法会直接返回而不执行任何操作。
    /// 断开连接时会触发<see cref="Disconnected"/>事件（锁外触发，最多一次）。
    /// </remarks>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        WebSocketCloseEventArgs? pendingClose = null;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            pendingClose = await DisconnectCoreAsync(cancellationToken);
        }
        finally
        {
            _connectionLock.Release();
        }

        if (pendingClose != null)
        {
            SafeInvokeDisconnected(pendingClose);
        }
    }

    /// <summary>
    /// 断开连接的无锁核心实现。
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>需要在锁外触发的断开事件参数；若无需触发则为 <c>null</c></returns>
    /// <remarks>
    /// P0-2 修复的关键：本方法<b>不得</b>获取 <c>_connectionLock</c>，
    /// 供 <see cref="ConnectAsync"/> 与 <see cref="DisconnectAsync"/> 复用。
    /// <para>
    /// P0-5 修复：本方法内部只做"原子占位"，不触发事件；事件由调用方在锁外触发，
    /// 否则用户回调中调用 SendMessageAsync 等加锁方法会立即死锁。
    /// </para>
    /// </remarks>
    private async Task<WebSocketCloseEventArgs?> DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        if (_webSocket == null || _webSocket.State == WebSocketState.Closed)
            return null;

        var webSocket = _webSocket;

        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch (ObjectDisposedException) { }

        if (webSocket.State == WebSocketState.Open)
        {
            try
            {
                // P1-4 修复：关闭握手必须限时，服务端不应答时强制 Abort
                using var closeCts = new CancellationTokenSource(CloseHandshakeTimeout);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, closeCts.Token);
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "客户端主动断开连接",
                    linked.Token);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "关闭握手未正常完成，强制中止连接");
                TryAbort(webSocket);
            }
        }

        if (_options.EnableLogging)
        {
            _logger.LogInformation("已断开飞书WebSocket连接");
        }

        var args = new WebSocketCloseEventArgs
        {
            CloseStatus = WebSocketCloseStatus.NormalClosure,
            CloseStatusDescription = "客户端主动断开连接",
            IsServerInitiated = false
        };

        // 原子占位：只有首次声明断线才需要对外触发事件（P0-4）
        return TryClaimDisconnected() ? args : null;
    }

    /// <summary>
    /// 原子地声明"本次连接已断开"，成功占位返回 <c>true</c>。
    /// </summary>
    /// <returns>是否成功占位（即调用方需要触发事件）</returns>
    private bool TryClaimDisconnected()
    {
        if (Interlocked.CompareExchange(ref _disconnectedFired, 1, 0) == 1)
            return false;

        Interlocked.Decrement(ref _connectionCount);
        return true;
    }

    /// <summary>
    /// 释放当前 socket 与关联的取消令牌源（无锁，调用方需自行保证同步）。
    /// </summary>
    /// <remarks>P1-2 修复：重连前必须释放旧连接，否则每次重连泄漏一个 ClientWebSocket。</remarks>
    private void DisposeCurrentSocket()
    {
        var webSocket = _webSocket;
        var cts = _cancellationTokenSource;

        if (webSocket != null)
        {
            TryAbort(webSocket);
            try { webSocket.Dispose(); }
            catch (Exception ex) { _logger.LogDebug(ex, "释放旧 WebSocket 实例时发生异常（可忽略）"); }
        }

        if (cts != null)
        {
            try { cts.Dispose(); }
            catch (Exception ex) { _logger.LogDebug(ex, "释放旧 CancellationTokenSource 时发生异常（可忽略）"); }
        }

        _webSocket = null;
        _cancellationTokenSource = null;
    }

    /// <summary>
    /// 尝试强制中止WebSocket连接
    /// </summary>
    /// <param name="webSocket">WebSocket实例</param>
    private void TryAbort(ClientWebSocket webSocket)
    {
        try { webSocket.Abort(); }
        catch (Exception ex) { _logger.LogDebug(ex, "中止 WebSocket 时发生异常（可忽略）"); }
    }

    /// <summary>
    /// 发送二进制消息
    /// </summary>
    /// <param name="data">要发送的二进制数据</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步发送操作的任务</returns>
    /// <exception cref="ArgumentNullException">当data为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当WebSocket未连接时抛出</exception>
    /// <remarks>
    /// 内部调用<see cref="SendBinaryMessageAsync(ArraySegment{byte}, CancellationToken)"/>方法。
    /// </remarks>
    public async Task SendBinaryMessageAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        await SendBinaryMessageAsync(new ArraySegment<byte>(data), cancellationToken);
    }

    /// <summary>
    /// 发送二进制消息
    /// </summary>
    /// <param name="data">要发送的二进制数据段</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步发送操作的任务</returns>
    /// <exception cref="ArgumentException">当data为空（长度为0）时抛出</exception>
    /// <exception cref="InvalidOperationException">当WebSocket未连接时抛出</exception>
    /// <remarks>
    /// 使用WebSocketMessageType.Binary消息类型发送数据。
    /// <para>
    /// P0-6 修复：本方法与 <see cref="SendMessageAsync"/> 统一使用 <c>_sendLock</c>。
    /// ClientWebSocket 同一时刻只允许一个未完成的 SendAsync，
    /// 此前二进制发送完全无锁，心跳帧与事件 ACK 并发时会抛 InvalidOperationException 并静默丢失 ACK，
    /// 导致服务端重复投递事件。
    /// </para>
    /// 发送成功后会记录调试日志（如果启用日志记录）。
    /// </remarks>
    public async Task SendBinaryMessageAsync(ArraySegment<byte> data, CancellationToken cancellationToken = default)
    {
        if (data.Count == 0)
            throw new ArgumentException("二进制数据不能为空", nameof(data));

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            var webSocket = _webSocket;
            if (webSocket == null || webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket未连接，无法发送消息");

            await webSocket.SendAsync(
                data,
                WebSocketMessageType.Binary,
                true,
                cancellationToken);

            if (_options.EnableLogging)
            {
                _logger.LogDebug("已发送二进制消息，大小: {Size} 字节", data.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送二进制消息时发生错误");
            OnError(ex, "发送二进制消息错误");
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 发送文本消息
    /// </summary>
    /// <param name="message">要发送的文本消息内容</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步发送操作的任务</returns>
    /// <exception cref="ArgumentException">当message为空或仅包含空白字符时抛出</exception>
    /// <exception cref="ArgumentException">当消息长度超过配置的最大限制时抛出</exception>
    /// <exception cref="InvalidOperationException">当WebSocket未连接时抛出</exception>
    /// <remarks>
    /// 消息会使用UTF-8编码发送。
    /// 最大消息长度由<see cref="MessageSizeLimits.MaxTextMessageSize"/>配置决定。
    /// </remarks>
    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("消息不能为空或仅包含空白字符", nameof(message));

        if (message.Length > _options.MessageSizeLimits.MaxTextMessageSize)
            throw new ArgumentException($"消息大小超过限制 ({_options.MessageSizeLimits.MaxTextMessageSize} 字符)", nameof(message));

        // P0-6 修复：统一使用 _sendLock（此前使用 _connectionLock，与二进制发送不互斥且粒度过粗）
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            var webSocket = _webSocket;
            if (webSocket == null || webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket未连接，无法发送消息");

            var buffer = System.Text.Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                cancellationToken);

            if (_options.EnableLogging)
            {
                _logger.LogDebug("已发送消息: {Message}", MessageSanitizer.Sanitize(message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息时发生错误");
            OnError(ex, "发送消息错误");
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 开始接收WebSocket消息
    /// </summary>
    /// <param name="messageHandler">处理接收到的消息的回调函数</param>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步接收操作的任务</returns>
    /// <exception cref="InvalidOperationException">当WebSocket未初始化时抛出</exception>
    /// <remarks>
    /// 该方法会持续监听WebSocket消息，直到连接关闭或取消令牌被触发。
    /// 接收到的消息会通过提供的messageHandler回调函数处理。
    /// 如果接收到关闭消息，会自动调用<see cref="HandleCloseMessageAsync"/>处理关闭逻辑。
    /// 初始缓冲区大小由<see cref="FeishuWebSocketOptions.InitialReceiveBufferSize"/>配置决定。
    /// <para>
    /// P1-2 修复：循环内固定使用进入循环时的 socket 局部引用；
    /// 此前每轮重新读取 <c>_webSocket</c> 字段，重连后旧循环会与新循环同时读取同一新 socket，造成"双接收循环"。
    /// </para>
    /// </remarks>
    public async Task StartReceivingAsync(Func<ArraySegment<byte>, WebSocketReceiveResult, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        var webSocket = _webSocket ?? throw new InvalidOperationException("WebSocket未初始化");

        _receiveBuffer ??= new byte[_options.InitialReceiveBufferSize];

        // WS-13 修复（P1-10）：将 _receiveBuffer 固化为局部变量，避免 Dispose/DisposeAsync
        // 将 _receiveBuffer 置为 null 后，接收循环或分片重组仍尝试访问该字段导致 NullReferenceException。
        // 局部引用在循环期间不会被外部置 null 操作影响。
        var buffer = _receiveBuffer;

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleCloseMessageAsync(result, webSocket);
                    break;
                }

                if (result.EndOfMessage)
                {
                    await messageHandler(new ArraySegment<byte>(buffer, 0, result.Count), result);
                }
                else
                {
                    await HandleFragmentedMessageAsync(result, messageHandler, webSocket, buffer, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 正常的取消操作（如断开连接或重连时取消接收循环），不应触发错误事件
            _logger.LogInformation("消息接收循环已取消（正常关闭或重连）");
        }
        catch (WebSocketException ex)
        {
            // 仅在 Debug 级别记录完整异常（含堆栈），避免可恢复的网络断连以 ERR 级别刷屏。
            // OnError 方法会根据错误恢复策略以适当的级别（WRN/ERR）记录面向用户的摘要日志。
            _logger.LogDebug(ex, "接收消息时发生WebSocket错误");
            OnError(ex, "WebSocket接收错误");
            // 远程方未完成关闭握手就断开连接时，WebSocket 状态为 Aborted 而非 Closed，
            // 不会走 HandleCloseMessageAsync 路径，因此需要在此处主动触发 Disconnected 事件，
            // 否则重连只能等待心跳管理器检测到连接断开后才触发（延迟可达数十秒）。
            // P1-1 修复：不再硬编码 NormalClosure，按异常类型映射真实关闭状态码。
            NotifyDisconnected(new WebSocketCloseEventArgs
            {
                CloseStatus = MapExceptionToCloseStatus(ex),
                CloseStatusDescription = $"WebSocket接收错误导致连接断开: {ex.WebSocketErrorCode}",
                IsServerInitiated = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "接收消息时发生错误");
            OnError(ex, "接收消息错误");
            NotifyDisconnected(new WebSocketCloseEventArgs
            {
                CloseStatus = MapExceptionToCloseStatus(ex),
                CloseStatusDescription = $"接收消息错误导致连接断开: {ex.GetType().Name}",
                IsServerInitiated = false
            });
        }
    }

    /// <summary>
    /// 处理分片消息的重组
    /// </summary>
    /// <param name="firstResult">第一帧的接收结果</param>
    /// <param name="messageHandler">消息处理器回调</param>
    /// <param name="webSocket">接收所使用的WebSocket实例</param>
    /// <param name="buffer">接收缓冲区（WS-13 修复：由调用方传入局部引用，避免 Dispose 竞态置 null）</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task HandleFragmentedMessageAsync(
        WebSocketReceiveResult firstResult,
        Func<ArraySegment<byte>, WebSocketReceiveResult, Task> messageHandler,
        ClientWebSocket webSocket,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        using var messageStream = new MemoryStream();
        var maxMessageSize = firstResult.MessageType == WebSocketMessageType.Binary
            ? _options.MessageSizeLimits.MaxBinaryMessageSize
            : _options.MessageSizeLimits.MaxTextMessageSize;

        // P2-1 修复：首帧同样先校验后写入
        if (firstResult.Count > maxMessageSize)
        {
            _logger.LogError("分片消息首帧大小 {Size} 已超过最大限制 {MaxSize}，丢弃消息",
                firstResult.Count, maxMessageSize);
            OnError(new InvalidOperationException($"分片消息大小超过最大限制 {maxMessageSize}"), "分片消息大小超限");
            return;
        }

        // WS-13 修复：使用调用方传入的局部 buffer 引用，而非 _receiveBuffer 字段
        messageStream.Write(buffer, 0, firstResult.Count);

        while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await HandleCloseMessageAsync(result, webSocket);
                return;
            }

            // P2-1 修复：写入前拦截，避免超限数据先进入内存
            if (messageStream.Length + result.Count > maxMessageSize)
            {
                _logger.LogError("分片消息大小将超过最大限制 {MaxSize}（当前 {Size}，本帧 {FrameSize}），丢弃消息",
                    maxMessageSize, messageStream.Length, result.Count);
                OnError(new InvalidOperationException($"分片消息大小超过最大限制 {maxMessageSize}"), "分片消息大小超限");
                return;
            }

            // WS-13 修复：使用局部 buffer 引用
            messageStream.Write(buffer, 0, result.Count);

            if (result.EndOfMessage)
            {
                var completeData = messageStream.ToArray();
                var combinedResult = new WebSocketReceiveResult(
                    completeData.Length,
                    firstResult.MessageType,
                    true);
                await messageHandler(new ArraySegment<byte>(completeData), combinedResult);
                return;
            }
        }

        var receivedBytes = messageStream.Length;
        _logger.LogWarning("分片消息重组中断，已接收 {ReceivedBytes} 字节但未收到 EndOfMessage 信号。连接状态: {State}, 取消请求: {Cancelled}",
            receivedBytes, webSocket.State, cancellationToken.IsCancellationRequested);

        OnError(new InvalidOperationException($"分片消息重组中断，已接收 {receivedBytes} 字节但未完成"), "分片消息重组中断");
    }

    /// <summary>
    /// 处理WebSocket关闭消息
    /// </summary>
    /// <param name="result">WebSocket接收结果，包含关闭状态和描述</param>
    /// <param name="webSocket">接收所使用的WebSocket实例</param>
    /// <returns>表示异步处理操作的任务</returns>
    /// <remarks>
    /// 该方法会触发<see cref="Disconnected"/>事件，通知订阅者连接已关闭。
    /// 重连逻辑由<see cref="ReconnectionOrchestrator"/>统一处理。
    /// </remarks>
    private async Task HandleCloseMessageAsync(WebSocketReceiveResult result, ClientWebSocket webSocket)
    {
        if (_options.EnableLogging)
        {
            _logger.LogInformation("服务器请求关闭连接: {Status} - {Description}",
                result.CloseStatus, result.CloseStatusDescription);
        }

        // 通过 NotifyDisconnected 统一处理连接计数递减和 Disconnected 事件触发，
        // _disconnectedFired 标志（Interlocked）确保不会与 StartReceivingAsync 异常路径或 DisconnectAsync 重复触发。
        if (webSocket.State == WebSocketState.Open)
        {
            try
            {
                // P1-4 修复：关闭握手限时，避免服务端不应答时接收循环永久阻塞
                using var closeCts = new CancellationTokenSource(CloseHandshakeTimeout);
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "客户端确认关闭连接",
                    closeCts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "确认关闭连接时发生异常（可忽略）");
                TryAbort(webSocket);
            }
        }

        // 递减连接计数并触发断开事件（仅一次）
        NotifyDisconnected(new WebSocketCloseEventArgs
        {
            CloseStatus = result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
            CloseStatusDescription = result.CloseStatusDescription,
            IsServerInitiated = true
        });
    }

    /// <summary>
    /// 将异常映射为语义准确的WebSocket关闭状态码
    /// </summary>
    /// <param name="exception">导致连接断开的异常</param>
    /// <returns>对应的关闭状态码</returns>
    /// <remarks>
    /// P1-1 修复：此前所有异常断线一律上报 <see cref="WebSocketCloseStatus.NormalClosure"/>，
    /// 掩盖了真实断线原因，会误导上层（健康检查、重连策略）判定为"主动关闭"而放弃重连。
    /// </remarks>
    private static WebSocketCloseStatus MapExceptionToCloseStatus(Exception exception)
    {
        switch (exception)
        {
            case WebSocketException wsEx when wsEx.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely:
            case WebSocketException wsEx2 when wsEx2.WebSocketErrorCode == WebSocketError.Faulted:
                return WebSocketCloseStatus.EndpointUnavailable;
            case WebSocketException:
                return WebSocketCloseStatus.ProtocolError;
            case System.IO.IOException:
            case System.Net.Sockets.SocketException:
                return WebSocketCloseStatus.EndpointUnavailable;
            case TimeoutException:
                return WebSocketCloseStatus.EndpointUnavailable;
            case OperationCanceledException:
                return WebSocketCloseStatus.NormalClosure;
            default:
                return WebSocketCloseStatus.InternalServerError;
        }
    }

    /// <summary>
    /// 配置SSL/TLS证书验证
    /// </summary>
    /// <param name="webSocket">WebSocket客户端实例</param>
    /// <param name="uri">连接的URI</param>
    /// <remarks>
    /// 根据配置选项设置证书验证策略。生产环境建议启用严格的证书验证。
    /// 支持自定义证书验证回调、自签名证书处理等配置。
    /// 注意：证书验证回调仅在 .NET Core 2.1+ / .NET 5+ 中可用。
    /// </remarks>
    private void ConfigureCertificateValidation(ClientWebSocket webSocket, Uri uri)
    {
        // 仅对wss协议配置证书验证
        if (uri.Scheme != "wss")
            return;

#if NETCOREAPP2_1_OR_GREATER || NET5_0_OR_GREATER
        try
        {
            // 使用自定义证书验证回调（优先级最高）
            if (_options.CustomCertificateValidationCallback != null)
            {
                webSocket.Options.RemoteCertificateValidationCallback = _options.CustomCertificateValidationCallback;
                if (_options.EnableLogging)
                {
                    _logger.LogDebug("已配置自定义证书验证回调");
                }
                return;
            }

            // 根据配置决定是否验证证书
            if (!_options.ValidateServerCertificate)
            {
                // 禁用证书验证（仅用于开发/测试环境）
                webSocket.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                if (_options.EnableLogging)
                {
                    _logger.LogWarning("已禁用SSL证书验证，此配置仅应在开发/测试环境使用");
                }
                return;
            }

            // 配置标准证书验证，支持自签名证书选项
            // WS-12 修复（P1-8）：收紧自签名判定逻辑。
            // 此前放行所有 RemoteCertificateChainErrors，含过期/已撤销证书。
            // 现在仅在「链中仅 1 个元素且 ChainStatus 仅 UntrustedRoot」时才放行，
            // 显式拒绝 NotTimeValid/Revoked 等链错误。
            // RemoteCertificateNameMismatch 改为由独立的 AllowCertificateNameMismatch 选项控制。
            webSocket.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                // 如果没有错误，直接通过
                if (sslPolicyErrors == SslPolicyErrors.None)
                    return true;

                // WS-12 修复：处理名称不匹配 — 仅在显式允许时放行
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                {
                    if (_options.AllowCertificateNameMismatch)
                    {
                        if (_options.EnableLogging)
                        {
                            _logger.LogWarning("允许证书名称不匹配: {Errors}", sslPolicyErrors);
                        }
                        // 清除名称不匹配标志，继续检查其他错误
                        sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;
                    }
                    else
                    {
                        if (_options.EnableLogging)
                        {
                            _logger.LogError("SSL证书验证失败（名称不匹配）: {Errors}", sslPolicyErrors);
                        }
                        return false;
                    }
                }

                // 如果清除名称不匹配后已无错误，直接通过
                if (sslPolicyErrors == SslPolicyErrors.None)
                    return true;

                // WS-12 修复：处理链错误 — 仅在「自签名根证书」场景放行
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
                {
                    if (_options.AllowSelfSignedCertificates && IsSelfSignedRoot(chain))
                    {
                        if (_options.EnableLogging)
                        {
                            _logger.LogWarning("允许自签名根证书（仅 UntrustedRoot）: {Errors}", sslPolicyErrors);
                        }
                        return true;
                    }

                    if (_options.EnableLogging)
                    {
                        _logger.LogError("SSL证书验证失败（链错误，非自签名根或含其他链状态）: {Errors}", sslPolicyErrors);
                    }
                    return false;
                }

                // 其他错误严格拒绝
                if (_options.EnableLogging)
                {
                    _logger.LogError("SSL证书验证失败: {Errors}", sslPolicyErrors);
                }
                return false;
            };

            if (_options.EnableLogging)
            {
                _logger.LogDebug("已配置SSL证书验证 (允许自签名: {AllowSelfSigned})", _options.AllowSelfSignedCertificates);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置证书验证时发生错误");
        }
#else
        // .NET Standard 2.0 不支持 RemoteCertificateValidationCallback
        if (_options.EnableLogging)
        {
            if (!_options.ValidateServerCertificate)
            {
                _logger.LogWarning(".NET Standard 2.0 不支持自定义证书验证回调，ValidateServerCertificate 配置无效");
            }
            if (_options.AllowSelfSignedCertificates)
            {
                _logger.LogWarning(".NET Standard 2.0 不支持自定义证书验证回调，AllowSelfSignedCertificates 配置无效");
            }
        }
#endif
    }

    /// <summary>
    /// 判断证书链是否为「自签名根证书」场景。
    /// </summary>
    /// <param name="chain">SSL 证书链，可能为 null（服务端未提供链时）。</param>
    /// <returns>
    /// 仅当链中恰好 1 个元素且该元素的 <c>ChainStatus</c> 仅含 <c>UntrustedRoot</c> 时返回 <c>true</c>；
    /// 显式拒绝 <c>NotTimeValid</c>（过期）、<c>Revoked</c>（已撤销）等链状态。
    /// </returns>
    /// <remarks>
    /// WS-12 修复（P1-8）：此前放行所有 <c>RemoteCertificateChainErrors</c>，
    /// 含过期/已撤销证书。现在收紧为仅接受「自签名根」——即链中仅有 1 个证书，
    /// 且唯一错误是「不受信任的根」（自签名证书的典型特征）。
    /// </remarks>
    private static bool IsSelfSignedRoot(System.Security.Cryptography.X509Certificates.X509Chain? chain)
    {
        if (chain is null)
            return false;

        // 链中必须恰好 1 个元素（自签名证书：自身即根）
        if (chain.ChainElements.Count != 1)
            return false;

        // ChainStatus 为空数组表示无错误（不会走到这里），非空时逐条检查
        // 仅允许 UntrustedRoot，其他状态（NotTimeValid/Revoked 等）一律拒绝
        foreach (var status in chain.ChainStatus)
        {
            if (status.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.UntrustedRoot)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 通知连接已断开（线程安全，仅触发一次）
    /// </summary>
    /// <param name="args">断开事件参数</param>
    /// <remarks>
    /// P0-4 修复：使用 <see cref="System.Threading.Interlocked.CompareExchange(ref int, int, int)"/> 实现原子的 check-then-set，
    /// 确保对同一次连接只递减一次连接计数并触发一次 <see cref="Disconnected"/> 事件。
    /// </remarks>
    private void NotifyDisconnected(WebSocketCloseEventArgs args)
    {
        if (!TryClaimDisconnected())
            return;

        SafeInvokeDisconnected(args);
    }

    /// <summary>
    /// 在锁外安全地触发 <see cref="Connected"/> 事件
    /// </summary>
    /// <remarks>P0-5 修复：捕获用户回调异常，避免异常沿接收/连接线程冒泡导致进程崩溃。</remarks>
    private void SafeInvokeConnected()
    {
        var handler = Connected;
        if (handler == null)
            return;

        try
        {
            handler.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connected 事件处理器抛出异常");
        }
    }

    /// <summary>
    /// 在锁外安全地触发 <see cref="Disconnected"/> 事件
    /// </summary>
    /// <param name="args">断开事件参数</param>
    private void SafeInvokeDisconnected(WebSocketCloseEventArgs args)
    {
        var handler = Disconnected;
        if (handler == null)
            return;

        try
        {
            handler.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disconnected 事件处理器抛出异常");
        }
    }

    /// <summary>
    /// 触发错误事件
    /// </summary>
    /// <param name="ex">发生的异常</param>
    /// <param name="context">错误发生的上下文描述</param>
    /// <remarks>
    /// 该方法会创建<see cref="WebSocketErrorEventArgs"/>并触发<see cref="Error"/>事件。
    /// 会自动检测异常类型，设置网络错误和认证错误的标志。
    /// 用户回调抛出的异常会被捕获并记录，不会影响调用方。
    /// </remarks>
    private void OnError(Exception ex, string context)
    {
        // 使用错误恢复策略分析异常
        var recoveryResult = _errorRecoveryStrategy.AnalyzeError(ex, context);

        var errorArgs = new WebSocketErrorEventArgs
        {
            Exception = ex,
            ErrorMessage = $"{context}: {ex.Message}",
            ErrorType = recoveryResult.ErrorType,
            ConnectionState = _webSocket?.State ?? WebSocketState.None,
            IsNetworkError = ex is WebSocketException || ex is IOException,
            IsAuthError = ex.Message.Contains("auth") || ex.Message.Contains("认证"),
            IsRecoverable = recoveryResult.IsRecoverable,
            RecoveryRecommendation = recoveryResult.RecoveryRecommendation,
            SuggestedDelay = recoveryResult.SuggestedDelay
        };

        // 记录错误恢复分析结果
        if (recoveryResult.IsRecoverable)
        {
            _logger.LogWarning("可恢复错误: {ErrorType} - {Recommendation}, 建议延迟: {Delay}",
                recoveryResult.ErrorType, recoveryResult.RecoveryRecommendation, recoveryResult.SuggestedDelay);
        }
        else
        {
            _logger.LogError("不可恢复错误: {ErrorType} - {Recommendation}",
                recoveryResult.ErrorType, recoveryResult.RecoveryRecommendation);
        }

        var handler = Error;
        if (handler == null)
            return;

        try
        {
            handler.Invoke(this, errorArgs);
        }
        catch (Exception callbackEx)
        {
            _logger.LogError(callbackEx, "Error 事件处理器抛出异常");
        }
    }

    /// <summary>
    /// 异步释放资源
    /// </summary>
    /// <remarks>
    /// 会执行限时关闭握手（P1-4）、释放 socket、CTS 与所有信号量。重复调用安全。
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        // WS-15：原子 check-then-set，确保并发调用安全
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        try
        {
            var webSocket = _webSocket;
            var cts = _cancellationTokenSource;

            try { cts?.Cancel(); }
            catch (ObjectDisposedException) { }

            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    // P1-4 修复：关闭握手限时，超时强制 Abort，避免 Dispose 永久阻塞
                    using var closeCts = new CancellationTokenSource(CloseHandshakeTimeout);
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "客户端释放资源",
                        closeCts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "关闭 WebSocket 时发生异常（可忽略）");
                    TryAbort(webSocket);
                }
            }

            if (webSocket != null)
            {
                try { webSocket.Dispose(); }
                catch (Exception ex) { _logger.LogDebug(ex, "释放 WebSocket 时发生异常（可忽略）"); }
            }

            try { cts?.Dispose(); }
            catch (Exception ex) { _logger.LogDebug(ex, "释放 CancellationTokenSource 时发生异常（可忽略）"); }

            _webSocket = null;
            _cancellationTokenSource = null;
            _receiveBuffer = null;

            _connectionLock.Dispose();
            _sendLock.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步释放连接管理器资源时发生错误");
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放WebSocket连接管理器占用的资源
    /// </summary>
    /// <remarks>
    /// 该方法会取消所有正在进行的操作，执行限时关闭握手（P1-3 修复：此前直接 Dispose 等价 Abort，
    /// 与 DisposeAsync 行为不一致，服务端记录为异常断线），并释放相关资源。
    /// 实现IDisposable模式，确保资源正确清理。
    /// 如果已经释放过，重复调用不会产生副作用。
    /// </remarks>
    public void Dispose()
    {
        // WS-15：原子 check-then-set，确保并发调用安全
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        try
        {
            var webSocket = _webSocket;
            var cts = _cancellationTokenSource;

            try { cts?.Cancel(); }
            catch (ObjectDisposedException) { }

            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    // WS-09 修复（P1-4）：同步路径下使用独立 closeCts（5s）而非 CancellationToken.None，
                    // 确保服务端不应答时 CloseAsync 能被取消而非悬挂。
                    // 通过 Task.Run 脱离调用方同步上下文，降低死锁风险（P1-3）。
                    using var closeCts = new CancellationTokenSource(CloseHandshakeTimeout);
                    Task.Run(() => webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "客户端释放资源",
                            closeCts.Token))
                        .Wait(CloseHandshakeTimeout);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "同步释放时关闭握手未正常完成（可忽略）");
                }
                finally
                {
                    TryAbort(webSocket);
                }
            }

            if (webSocket != null)
            {
                try { webSocket.Dispose(); }
                catch (Exception ex) { _logger.LogDebug(ex, "释放 WebSocket 时发生异常（可忽略）"); }
            }

            try { cts?.Dispose(); }
            catch (Exception ex) { _logger.LogDebug(ex, "释放 CancellationTokenSource 时发生异常（可忽略）"); }

            _webSocket = null;
            _cancellationTokenSource = null;
            _receiveBuffer = null;

            _connectionLock.Dispose();
            _sendLock.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放连接管理器资源时发生错误");
        }

        GC.SuppressFinalize(this);
    }
}
