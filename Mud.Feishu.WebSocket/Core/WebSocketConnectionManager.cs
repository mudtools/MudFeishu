// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions.Metrics;
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
    // R5.2.7/X5：生产环境判定 — 用于把证书安全旁路（Mode=Dev / ValidateServerCertificate=false）的告警升级为 LogError
    private readonly bool _isProduction;
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
    /// 排空超限消息时的帧数上界（WS2-03）。
    /// </summary>
    /// <remarks>
    /// 排空本身必须有界：恶意/异常对端可以持续投递超限分片让排空永不结束。
    /// 达到上界即判定为协议层异常并转为 D3 方案 B（Abort + 重连）。
    /// </remarks>
    private const int MaxDrainFrames = 1024;

    /// <summary>
    /// 排空超限消息时的字节上界：64MB（<see cref="MaxDrainFrames"/> 之外的独立兜底）。
    /// </summary>
    private const long MaxDrainBytes = 64L * 1024 * 1024;

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
    /// <remarks>
    /// 保留 3 参构造重载（委托到 4 参版本，宿主环境传 null）：
    /// Castle/Moq 代理不支持「可选参数为非简单类型」的构造函数（ProxyGeneration 会以
    /// <c>Can not instantiate proxy</c> 失败），故不能用 <c>= null</c> 默认值形态。
    /// </remarks>
    public WebSocketConnectionManager(
        ILogger<WebSocketConnectionManager> logger,
        FeishuWebSocketOptions options,
        ILoggerFactory loggerFactory)
        : this(logger, options, loggerFactory, null)
    {
    }

    /// <summary>
    /// 初始化WebSocket连接管理器实例（R5.2.7/X5：支持注入宿主环境用于生产加固判定）
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="options">WebSocket配置选项，如果为null则使用默认配置</param>
    /// <param name="loggerFactory">日志工厂，用于创建ErrorRecoveryStrategy的日志记录器</param>
    /// <param name="hostEnvironment">宿主环境（可选）。提供时以其 EnvironmentName 是否为 Production 判定；缺省时回退读取 <c>DOTNET_ENVIRONMENT</c>/<c>ASPNETCORE_ENVIRONMENT</c>（均未设置按 Production 处理，与 Webhook 侧 <c>EnvironmentService</c> 语义一致）</param>
    /// <exception cref="ArgumentNullException">当logger为null时抛出</exception>
    public WebSocketConnectionManager(
        ILogger<WebSocketConnectionManager> logger,
        FeishuWebSocketOptions options,
        ILoggerFactory loggerFactory,
        Microsoft.Extensions.Hosting.IHostEnvironment? hostEnvironment)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FeishuWebSocketOptions();
        _loggerFactory = loggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        _errorRecoveryStrategy = new ErrorRecoveryStrategy(_loggerFactory.CreateLogger<ErrorRecoveryStrategy>());
        _isProduction = ResolveIsProduction(hostEnvironment);
    }

    /// <summary>
    /// R5.2.7/X5：解析「是否生产环境」。
    /// 优先使用宿主注入的 <see cref="Microsoft.Extensions.Hosting.IHostEnvironment"/>；
    /// 否则回退环境变量（DOTNET_ENVIRONMENT > ASPNETCORE_ENVIRONMENT > Production），
    /// 与 Webhook 侧 ADR-4 的 <c>EnvironmentService</c> 保持「缺省即 Production」的安全默认。
    /// </summary>
    private static bool ResolveIsProduction(Microsoft.Extensions.Hosting.IHostEnvironment? hostEnvironment)
    {
        if (hostEnvironment is not null)
            return string.Equals(hostEnvironment.EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase);

        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";
        return string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
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
        if (uri.Scheme == "ws" && !_options.Certificate.AllowInsecureWebSocket)
            throw new ArgumentException("WebSocket URL使用不安全的ws://协议。如需在开发/测试环境使用，请设置 AllowInsecureWebSocket = true", nameof(url));

        // P2-15 修复：主机白名单校验。此前仅校验 scheme——端点 URL 由服务端 API 下发，
        // 一旦被篡改（DNS 劫持 / API 响应被篡改 / 配置错误），客户端会向任意主机发起连接（SSRF 面）。
        ValidateWebSocketHost(uri);

        // P1-5 修复：Dispose 之后确定性拒绝（此前为"随机 ObjectDisposedException / 偶发成功"）
        ThrowIfDisposed();

        await _connectionLock.WaitAsync(cancellationToken);
        WebSocketCloseEventArgs? pendingClose = null;
        // P1-3 修复：连接失败时也必须把"旧连接已断开"告知订阅者，否则事件驱动恢复退化为
        // 最长 HealthCheckIntervalMs 的轮询。此处捕获异常，待事件派发后再原样抛出。
        Exception? connectError = null;
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

                // WS2-05 / F4（D5）：只记录 scheme://host/path，**整体剥离 query**。
                // 端点 URL 由服务端 API 下发，query 中可能携带一次性凭据类参数（ticket/code/nonce 等）；
                // 这类参数一旦入日志即长期留存于集中式日志系统。用"整体剥离"而非"按键名白名单脱敏"，
                // 是为了避免白名单漏键构成新的泄露面（零白名单 = 零漏键）。
                // 排障所需的端点定位信息（主机 + 路径）完整保留；service_id 另有独立日志。
                _logger.LogInformation("已连接到飞书WebSocket服务: {Endpoint}",
                    uri.GetLeftPart(UriPartial.Path));


                // P0-4 修复：只有连接真正成功后才配平计数并允许触发断线事件。
                // 顺序必须为 Increment → 清除断线标志，避免连接失败后 Decrement 未配平导致计数为负。
                Interlocked.Increment(ref _connectionCount);
                Interlocked.Exchange(ref _disconnectedFired, 0);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                _logger.LogError("连接飞书WebSocket服务超时");
                connectError = new TimeoutException("连接飞书WebSocket服务超时");
            }
            catch (Exception ex)
            {
                // P1-3：握手失败不再直接穿透，先让下面的锁外事件派发完成
                connectError = ex;
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

        // P1-3 修复：无论成功/失败，先派发"旧连接断开"，再原样抛出（保留异常类型与原始堆栈）
        if (connectError != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(connectError).Throw();
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
        // P1-5 修复：Dispose 之后确定性拒绝，避免与信号量释放竞态
        ThrowIfDisposed();

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
    /// 校验实例是否已释放（P1-5）。
    /// </summary>
    /// <exception cref="ObjectDisposedException">当实例已释放时抛出</exception>
    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) == 1)
            throw new ObjectDisposedException(nameof(WebSocketConnectionManager));
    }

    /// <summary>
    /// 校验 WebSocket 主机是否在 <see cref="FeishuWebSocketOptions.AllowedHostSuffixes"/> 白名单内（P2-15）。
    /// </summary>
    /// <param name="uri">待校验的连接地址</param>
    /// <exception cref="ArgumentException">当主机不在白名单内时抛出</exception>
    /// <remarks>
    /// 匹配规则（大小写不敏感）：
    /// <list type="bullet">
    /// <item><c>*.feishu.cn</c>：匹配任意层级的 <c>*.feishu.cn</c> 子域（<c>gateway.feishu.cn</c> ✓、<c>a.b.feishu.cn</c> ✓）；</item>
    /// <item><c>gateway.feishu.cn</c>：精确匹配；</item>
    /// <item>列表为空或仅空白：不限制（历史行为）。</item>
    /// </list>
    /// 后缀匹配以前导点（<c>.feishu.cn</c>）为界，因此 <c>evil-feishu.cn</c> 不会误匹配 <c>*.feishu.cn</c>。
    /// </remarks>
    private void ValidateWebSocketHost(Uri uri)
    {
        var allowList = _options.AllowedHostSuffixes;
        if (string.IsNullOrWhiteSpace(allowList))
        {
            return;   // 未配置白名单 = 不限制（历史行为）
        }

        var host = uri.Host;

        foreach (var rawEntry in allowList.Split(';'))
        {
            var entry = rawEntry.Trim();
            if (entry.Length == 0)
            {
                continue;
            }

            if (entry.StartsWith("*.", StringComparison.Ordinal))
            {
                // 通配后缀：以前导点为界匹配任意层级子域（"gateway.feishu.cn".EndsWith(".feishu.cn")）
                if (host.EndsWith(entry.Substring(1), StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            else if (host.Equals(entry, StringComparison.OrdinalIgnoreCase))
            {
                return;   // 精确匹配
            }
        }

        throw new ArgumentException(
            $"WebSocket 主机 \"{host}\" 不在允许的主机白名单内（AllowedHostSuffixes）。" +
            "如需连接自定义网关/本地测试端点，请把该主机加入 AllowedHostSuffixes，或将该项置空表示不限制");
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

        // ────────────────────────────────────────────────────────────────────
        // WS2-01 ①（I13 顺序约束）：占位必须**先于**关闭握手。
        //
        // 改造前顺序是「Cancel → CloseAsync → 占位」：关闭握手会把 socket 推到
        // CloseSent/Aborted，正在阻塞于 ReceiveAsync 的接收循环随即以
        // OperationCanceledException / WebSocketException 退出，并在**占位之前**抢先调用
        // NotifyDisconnected 完成占位 ⇒ 主动断开路径的占位失败、返回 null、不再派发事件。
        // 结果取决于竞态：要么事件由接收循环派发（描述为"接收错误导致连接断开"，
        // 与本端主动断开的事实不符），要么两侧都判定"对方已占位"而**谁都不派发**。
        //
        // 前移后语义确定：本端主动断开的占位优先，接收循环仅在"非本端主动断开"时补发通知。
        // ────────────────────────────────────────────────────────────────────

        // P1-2 修复（I2）：只接受"当前 socket"的断线声明；旧连接的迟到声明一律丢弃（不递减计数、不触发事件）
        if (!ReferenceEquals(Volatile.Read(ref _webSocket), webSocket))
        {
            _logger.LogDebug("忽略过期连接（非当前 socket）的断线声明");
            return null;
        }

        // 原子占位：只有首次声明断线才需要对外触发事件（P0-4）
        if (!TryClaimDisconnected())
        {
            return null;
        }

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

        _logger.LogInformation("已断开飞书WebSocket连接");

        return new WebSocketCloseEventArgs
        {
            CloseStatus = WebSocketCloseStatus.NormalClosure,
            CloseStatusDescription = "客户端主动断开连接",
            IsServerInitiated = false
        };
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
        // P1-5 修复：Dispose 之后确定性拒绝
        ThrowIfDisposed();

        if (data.Count == 0)
            throw new ArgumentException("二进制数据不能为空", nameof(data));

        // P1-4 修复：补齐此前完全缺失的二进制发送上限校验（接收侧一直有该限制）
        if ((long)data.Count > _options.MessageSizeLimits.MaxBinaryMessageSize)
            throw new ArgumentException(
                $"二进制消息大小超过限制 ({data.Count} > {_options.MessageSizeLimits.MaxBinaryMessageSize} 字节)",
                nameof(data));

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

            _logger.LogDebug("已发送二进制消息，大小: {Size} 字节", data.Count);

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
        // P1-5 修复：Dispose 之后确定性拒绝
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("消息不能为空或仅包含空白字符", nameof(message));

        // 字符维度校验（既有契约，保持"提前拦截、避免多余分配"的语义）
        if (message.Length > _options.MessageSizeLimits.MaxTextMessageSize)
            throw new ArgumentException($"消息大小超过限制 ({_options.MessageSizeLimits.MaxTextMessageSize} 字符)", nameof(message));

        // P1-4 修复：补充与接收侧同源的字节维度校验（UTF-8 编码后）。
        // 默认上限 = 3 × MaxTextMessageSize（UTF-8 对 UTF-16 的最坏展开），属"放宽"而非收紧，
        // 因此现有一切合法消息继续通过；但"1MB 中文字符"这类旧实现放行、接收侧会拒的消息现在会被提前拦截。
        var buffer = System.Text.Encoding.UTF8.GetBytes(message);
        var maxTextMessageBytes = _options.MessageSizeLimits.ResolveMaxTextMessageBytes();
        if (buffer.Length > maxTextMessageBytes)
            throw new ArgumentException($"消息大小超过限制 ({maxTextMessageBytes} 字节，实际 {buffer.Length} 字节)", nameof(message));

        // P0-6 修复：统一使用 _sendLock（此前使用 _connectionLock，与二进制发送不互斥且粒度过粗）
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            var webSocket = _webSocket;
            if (webSocket == null || webSocket.State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket未连接，无法发送消息");

            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                cancellationToken);

            _logger.LogDebug("已发送消息: {Message}", MessageSanitizer.Sanitize(message));

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
    /// <para>
    /// <b>缓冲区生命周期契约（P2-7 / I4）</b>：传入 <paramref name="messageHandler"/> 的
    /// <see cref="ArraySegment{T}"/> 指向本实例<b>复用的接收缓冲区</b>（<c>_receiveBuffer</c>）。
    /// 处理器<b>必须在返回前</b>完成读取或拷贝；下一次 <c>ReceiveAsync</c> 会立即覆写该缓冲区，
    /// 因此<b>不允许跨 <c>await</c> 持有</b>，也不允许在 <c>Task.Run</c> 等异步路径中延迟访问。
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

        // I13 穷尽性辅助标记：区分"已由其它路径完成断线声明"与"循环静默退出"
        var disconnectDeclared = false;

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleCloseMessageAsync(result, webSocket);
                    disconnectDeclared = true;
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

            // P0-1 修复（I13）：取消退出**同样是一条终止路径**，必须参与原子占位。
            //
            // 改造前此处只记日志 ⇒ 模块没有任何断线信号（无读循环、无事件、无 Disconnected），
            // 上层只能等 HealthCheckIntervalMs（默认 60 秒）的轮询兜底。
            //
            // 注意这里的**两种取消形态**（实现与用例都必须区分，否则会得出错误的结论）：
            //   ① 取消发生在 `ReceiveAsync` 期间：`ClientWebSocket` 会以 Abort 中止底层连接
            //      ⇒ 退出时 State=Aborted，后续由通用兜底分支补发声明；
            //   ② 取消发生在**派发帧期间**（本循环正处于 await messageHandler / 背压租约等待）：
            //      循环以"条件不成立"自然退出，**socket 仍为 Open** ⇒ 真僵尸态
            //      （上层所有 IsConnected 判定都为 true，健康检查判 Healthy，重连永不触发）。
            //      背压（慢处理器）会显著放大这个窗口。
            if (webSocket.State == WebSocketState.Open)
            {
                NotifyDisconnected(new WebSocketCloseEventArgs
                {
                    CloseStatus = WebSocketCloseStatus.EndpointUnavailable,
                    CloseStatusDescription = "接收循环被取消但连接仍为 Open",
                    IsServerInitiated = false
                }, webSocket);

                disconnectDeclared = true;
            }
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
            }, webSocket);

            disconnectDeclared = true;
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
            }, webSocket);

            disconnectDeclared = true;
        }

        // ────────────────────────────────────────────────────────────────────
        // I13 兜底（R2/WS2-03 实施中发现）：循环还可能**因 socket 状态变化而自然退出**——
        // 既没有读到关闭帧（不走 HandleCloseMessageAsync），也没有抛异常（不进任何 catch）。
        // 典型触发：排空超限消息时达到上界主动 Abort（D3 方案 B）、外部调用 Abort/Dispose socket。
        // 若不在此补发声明，就会出现"循环已退出（不再读任何帧）但没有任何断线信号"的静默状态——
        // 与 P0-1 属同一类缺口（唯一存活检测通道失效且不被覆盖）。
        //
        // NotifyDisconnected 自身是幂等的（owner 身份校验 + 原子占位），
        // 因此对"已由其它路径声明过"的情况调用它是安全且无副作用的。
        // ────────────────────────────────────────────────────────────────────
        if (!disconnectDeclared)
        {
            // 用令牌判定"退出是否由取消引起"（而不是靠某个 catch 分支是否执行）：
            // 取消发生在 ReceiveAsync 期间会以 Abort 中止连接并走 OCE 分支；
            // 取消发生在**派发帧期间**则循环以"条件不成立"自然退出、socket 保持 Open——
            // 后者正是 P0-1 的真僵尸形态，两种形态在此统一收口。
            var exitedByCancellation = cancellationToken.IsCancellationRequested;

            NotifyDisconnected(new WebSocketCloseEventArgs
            {
                // 本地取消触发的退出不属"对端不可达"，语义上按正常关闭上报
                // （与 MapExceptionToCloseStatus(OperationCanceledException) 的口径一致）；
                // 其它状态变化（外部 Abort / 对端异常断开 / 排空上界主动 Abort）按真实状态映射。
                CloseStatus = exitedByCancellation
                    ? WebSocketCloseStatus.NormalClosure
                    : MapSocketStateToCloseStatus(webSocket.State),
                CloseStatusDescription = exitedByCancellation
                    ? $"接收循环被取消（退出时 socket 状态: {webSocket.State}）"
                    : $"接收循环因连接状态变化而退出: {webSocket.State}",
                IsServerInitiated = false
            }, webSocket);
        }
    }

    /// <summary>
    /// 把"循环退出时的 socket 状态"映射为语义准确的关闭状态码。
    /// </summary>
    /// <param name="state">退出时观察到的 socket 状态</param>
    /// <returns>对应的关闭状态码。</returns>
    /// <remarks>
    /// 与 <see cref="MapExceptionToCloseStatus"/> 同一职责（把状态码的语义如实上报给上层，
    /// 避免健康检查/重连策略把异常断线误判为"主动关闭"而放弃重连）。
    /// </remarks>
    private static WebSocketCloseStatus MapSocketStateToCloseStatus(WebSocketState state)
    {
        switch (state)
        {
            case WebSocketState.Closed:
            case WebSocketState.CloseSent:
            case WebSocketState.CloseReceived:
                return WebSocketCloseStatus.NormalClosure;
            case WebSocketState.Aborted:
                return WebSocketCloseStatus.EndpointUnavailable;
            case WebSocketState.Open:
                // 仍为 Open 却退出循环：只可能是取消令牌已触发（socket 未受影响），
                // 属人为终止而非协议错误。
                return WebSocketCloseStatus.NormalClosure;
            default:
                return WebSocketCloseStatus.EndpointUnavailable;
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
        // P1-4 修复：文本分片按"UTF-8 字节"计量（与此前按字符误算的同一配置项语义对齐），
        // 二进制分片沿用字节上限。
        var maxMessageSize = firstResult.MessageType == WebSocketMessageType.Binary
            ? _options.MessageSizeLimits.MaxBinaryMessageSize
            : _options.MessageSizeLimits.ResolveMaxTextMessageBytes();

        // P2-1 修复：首帧同样先校验后写入
        if (firstResult.Count > maxMessageSize)
        {
            _logger.LogError("分片消息首帧大小 {Size} 已超过最大限制 {MaxSize}，将排空至消息边界后丢弃",
                firstResult.Count, maxMessageSize);
            OnError(new InvalidOperationException($"分片消息大小超过最大限制 {maxMessageSize}"),
                "分片消息大小超限", errorTypeOverride: FragmentSizeExceededErrorType);

            // F5：受控丢弃可计数（消息数 +1）
            FeishuMetricsHelper.RecordWebSocketFramesDiscarded(
                _options.AppKey, FeishuMetrics.DiscardReasons.FragmentSizeExceeded);

            // WS2-03（D3 方案 A）：丢弃必须"丢干净"——排空至 EndOfMessage 后再返回。
            // 此前直接 return 会留下半个消息：相邻消息的字节被重新组帧，
            // 并被当作独立消息送入 protobuf 解析与序号链路（SequenceGapThreshold 默认 0 = 无跳跃上界）。
            await DrainUntilEndOfMessageAsync(webSocket, buffer, cancellationToken).ConfigureAwait(false);

            return;
        }

        // WS-13 修复：使用调用方传入的局部 buffer 引用，而非 _receiveBuffer 字段
        messageStream.Write(buffer, 0, firstResult.Count);

        while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                // 关闭帧是唯一**允许**不排空即返回的分支：连接即将终止，WS 消息边界已无意义
                // （后续不再有任何消息会被重新组帧）。
                await HandleCloseMessageAsync(result, webSocket);
                return;
            }

            // P2-1 修复：写入前拦截，避免超限数据先进入内存
            if (messageStream.Length + result.Count > maxMessageSize)
            {
                _logger.LogError("分片消息大小将超过最大限制 {MaxSize}（当前 {Size}，本帧 {FrameSize}），将排空至消息边界后丢弃",
                    maxMessageSize, messageStream.Length, result.Count);
                OnError(new InvalidOperationException($"分片消息大小超过最大限制 {maxMessageSize}"),
                    "分片消息大小超限", errorTypeOverride: FragmentSizeExceededErrorType);

                FeishuMetricsHelper.RecordWebSocketFramesDiscarded(
                    _options.AppKey, FeishuMetrics.DiscardReasons.FragmentSizeExceeded);

                // WS2-03（D3 方案 A）：同上，先把本条消息的剩余分片读空再返回。
                await DrainUntilEndOfMessageAsync(webSocket, buffer, cancellationToken).ConfigureAwait(false);

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
    /// 分片超限时使用的错误类型标识（供告警规则按类型区分"超限丢弃"与其它接收错误）。
    /// </summary>
    private const string FragmentSizeExceededErrorType = "FragmentSizeExceeded";

    /// <summary>
    /// 排空剩余分片直到当前 WS 消息的 <c>EndOfMessage</c>（WS2-03 / D3 方案 A）。
    /// </summary>
    /// <param name="webSocket">接收所使用的 WebSocket 实例</param>
    /// <param name="buffer">接收缓冲区</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示排空操作的任务</returns>
    /// <remarks>
    /// <b>为什么必须排空</b>：WebSocket 是**消息边界化**的帧协议，分片（continuation）只有在
    /// <c>EndOfMessage</c> 处才结束一条消息。超限时直接 <c>return</c> 会让本条消息的剩余分片
    /// 在下一轮 <c>ReceiveAsync</c> 中被当作**新消息**消费——这些字节既不构成合法 protobuf/JSON，
    /// 又会污染 <c>MessageSequenceValidator</c> 游标与去重状态（默认 <c>SequenceGapThreshold = 0</c>，
    /// 没有任何跳跃检测能发现它）。
    /// <para>
    /// <b>上界保护</b>：排空本身也必须有界——恶意/异常对端可以持续投递超限分片让排空永不结束。
    /// 超过 <see cref="MaxDrainFrames"/> 帧或 <see cref="MaxDrainBytes"/> 字节即判定为协议层异常，
    /// 转为 D3 方案 B（快速失败）：<c>Abort</c> 连接，让接收循环以异常退出并触发重连
    /// （重连会连带重置序号验证器与半包状态）。
    /// </para>
    /// </remarks>
    private async Task DrainUntilEndOfMessageAsync(ClientWebSocket webSocket, byte[] buffer, CancellationToken cancellationToken)
    {
        long drainedBytes = 0;
        var drainedFrames = 0;

        while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            WebSocketReceiveResult result;

            try
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 关停/重连：放弃排空（连接即将终止，边界已无意义）
                return;
            }
            catch (Exception ex)
            {
                // 对端断开或 socket 已失效：排空不可能完成，交给接收循环的异常路径处理
                _logger.LogDebug(ex, "排空超限分片时连接已不可用，中止排空");
                return;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                // 排空途中收到关闭帧：完成关闭握手（与正常接收路径一致），不再继续排空
                await HandleCloseMessageAsync(result, webSocket).ConfigureAwait(false);
                return;
            }

            drainedBytes += result.Count;
            drainedFrames++;

            if (result.EndOfMessage)
            {
                _logger.LogWarning("已排空超限消息的剩余分片（{Frames} 帧 / {Bytes} 字节）并丢弃，连接保持可用",
                    drainedFrames, drainedBytes);
                return;
            }

            if (drainedFrames >= MaxDrainFrames || drainedBytes >= MaxDrainBytes)
            {
                _logger.LogError("排空超限消息时达到上界（{Frames} 帧 / {Bytes} 字节）仍未收到 EndOfMessage，" +
                    "判定为协议层异常，强制中止连接以触发重连",
                    drainedFrames, drainedBytes);

                // F5：排空上界触发的丢弃与"单条消息超限"是两个不同量级的事件，单独计数
                FeishuMetricsHelper.RecordWebSocketFramesDiscarded(
                    _options.AppKey, FeishuMetrics.DiscardReasons.DrainBoundExceeded, drainedFrames);

                TryAbort(webSocket);
                return;
            }
        }
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
        _logger.LogInformation("服务器请求关闭连接: {Status} - {Description}",
    result.CloseStatus, result.CloseStatusDescription);


        // 通过 NotifyDisconnected 统一处理连接计数递减和 Disconnected 事件触发，
        // _disconnectedFired 标志（Interlocked）确保不会与 StartReceivingAsync 异常路径或 DisconnectAsync 重复触发。
        if (webSocket.State == WebSocketState.Open)
        {
            try
            {
                // P1-4 修复：关闭握手限时，避免服务端不应答时接收循环永久阻塞
                using var closeCts = new CancellationTokenSource(CloseHandshakeTimeout);
                // P2-8 修复：回显服务端下发的关闭码与描述，而非固定 NormalClosure（RFC 6455 §5.5.1）
                await webSocket.CloseAsync(
                    result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                    result.CloseStatusDescription ?? "客户端确认关闭连接",
                    closeCts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "确认关闭连接时发生异常（可忽略）");
                TryAbort(webSocket);
            }
        }

        // 递减连接计数并触发断开事件（仅一次）；P1-2：绑定 socket 身份
        NotifyDisconnected(new WebSocketCloseEventArgs
        {
            CloseStatus = result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
            CloseStatusDescription = result.CloseStatusDescription,
            IsServerInitiated = true
        }, webSocket);
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
            var cert = _options.Certificate;

            // ────────────────────────────────────────────────────────────────
            // R5/X5：Certificate.Mode 驱动运行时（改造前 Mode 只被 Validate/ToString 读取，
            // 而 Validate 的报错文案却引导用户「请改 Mode=Dev」——用户改完仍被拒绝，陷入无效循环）。
            //
            // 优先级链（唯一权威表述，勿在别处另立一套）：
            //   CustomCallback（Mode=Custom，或 Mode≠Custom 但回调非 null 的兼容分支）
            //     > ValidateServerCertificate=false（完全关闭校验）
            //     > Mode=Dev（仍校验，仅放宽「自签名根」与「名称不匹配」）
            //     > Mode=Strict（默认：拒绝自签名 / 名称不匹配 / 其他链错误）
            // ────────────────────────────────────────────────────────────────

            // 兼容分支（G-05）：Mode 未显式设为 Custom 却提供了回调 —— 回调**最高优先级**，立即安装并返回。
            // 改造前 CustomCallback 的优先级最高且**完全不看 Mode**，若改为「只在 Mode=Custom 时使用，
            // 会让「只配回调、不配 Mode」的存量部署静默改用严格回调 —— 属安全面行为突变，必须显式告警而非静默。
            // 注意：此分支必须在此处 return —— 若仅告警后继续下落，Mode=Dev 会安装 Dev 回调把用户回调
            // 覆盖掉（警告与行为互相矛盾，且违反上方优先级链）。
            if (cert.CustomCallback is not null && cert.Mode != CertificateValidationMode.Custom)
            {
                _logger.LogWarning(
                    "Certificate.CustomCallback 已配置但 Certificate.Mode={Mode}；为兼容既有行为仍使用该回调。" +
                    "请显式设置 Certificate.Mode=Custom 以消除歧义。",
                    cert.Mode);
                webSocket.Options.RemoteCertificateValidationCallback = cert.CustomCallback;
                return;
            }

            if (cert.Mode == CertificateValidationMode.Custom && cert.CustomCallback is null)
            {
                // 启动期 ValidateCertificateOptions 应已拦截该组合；走到这里说明运行期被代码改写。
                // 此处**不抛异常**（避免把可用的连接路径变成硬失败），而是回落到既有布尔驱动的行为：
                // ValidateServerCertificate=true → 严格校验（更安全的一侧）；=false → 完全关闭（既有能力）。
                _logger.LogError(
                    "Certificate.Mode=Custom 但未提供 Certificate.CustomCallback——" +
                    "启动期校验应已拦截该组合；运行期回落为布尔驱动行为" +
                    "（ValidateServerCertificate={Validate}：true=严格校验，false=完全关闭校验）。",
                    cert.ValidateServerCertificate);
            }

            if (cert.Mode == CertificateValidationMode.Dev)
            {
                if (!cert.ValidateServerCertificate)
                {
                    // 完全关闭校验的能力优先于 Mode=Dev（保持与改造前一致的安全能力可见性）
                    webSocket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                    // R5.2.7/X5 生产加固：生产环境将安全旁路升级为 LogError（不阻断启动；major 再评估是否 Validate 失败）
                    _logger.Log(
                        _isProduction ? LogLevel.Error : LogLevel.Warning,
                        "已禁用 SSL 证书验证（ValidateServerCertificate=false 优先于 Mode=Dev），此配置仅应在开发/测试环境使用{ProductionSuffix}",
                        _isProduction ? "；检测到当前为生产环境，请立即移除该配置" : string.Empty);
                    return;
                }

                webSocket.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors == SslPolicyErrors.None)
                        return true;

                    if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                    {
                        _logger.LogWarning("Dev 模式：放行证书名称不匹配: {Errors}", sslPolicyErrors);
                        // 名称不匹配已放行，继续检查其余错误
                        sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;
                        if (sslPolicyErrors == SslPolicyErrors.None)
                            return true;
                    }

                    if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 && IsSelfSignedRoot(chain))
                    {
                        _logger.LogWarning("Dev 模式：放行自签名根证书（仅 UntrustedRoot）: {Errors}", sslPolicyErrors);
                        return true;
                    }

                    // 过期/已撤销等其他链错误一律拒绝——即使处于 Dev 模式也不放宽（沿用 WS-12 的收紧语义）
                    _logger.LogError("SSL证书验证失败（Dev 模式：链错误非自签名根或含其他错误）: {Errors}", sslPolicyErrors);
                    return false;
                };

                // R5.2.7/X5 生产加固：生产环境将安全旁路升级为 LogError（不阻断启动；major 再评估是否 Validate 失败）
                _logger.Log(
                    _isProduction ? LogLevel.Error : LogLevel.Warning,
                    "WebSocket 证书校验处于 Dev 模式（允许自签名/名称不匹配），仅应用于开发与测试环境{ProductionSuffix}",
                    _isProduction ? "；检测到当前为生产环境，请改用 Mode=Strict 或自定义回调" : string.Empty);
                return;
            }

            // ↓↓↓ 以下为 Mode=Strict 与「Mode=Custom 且回调存在」的既有逻辑，保持改造前行为不变 ↓↓↓
            // （Mode≠Custom 的回调已由上方兼容分支接管并提前返回；此处 CustomCallback 分支
            //   实际只会服务 Mode=Custom 且回调非 null 的组合。）
            // 使用自定义证书验证回调（Mode=Custom 的主路径）
            if (_options.Certificate.CustomCallback != null)
            {
                webSocket.Options.RemoteCertificateValidationCallback = _options.Certificate.CustomCallback;
                                _logger.LogDebug("已配置自定义证书验证回调");
            
                return;
            }

            // 根据配置决定是否验证证书
            if (!_options.Certificate.ValidateServerCertificate)
            {
                // 禁用证书验证（仅用于开发/测试环境）
                webSocket.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                // R5.2.7/X5 生产加固：生产环境将安全旁路升级为 LogError（不阻断启动；major 再评估是否 Validate 失败）
                _logger.Log(
                    _isProduction ? LogLevel.Error : LogLevel.Warning,
                    "已禁用SSL证书验证，此配置仅应在开发/测试环境使用{ProductionSuffix}",
                    _isProduction ? "；检测到当前为生产环境，请立即移除该配置" : string.Empty);
            
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
                    if (_options.Certificate.AllowCertificateNameMismatch)
                    {
                                                _logger.LogWarning("允许证书名称不匹配: {Errors}", sslPolicyErrors);
                    
                        // 清除名称不匹配标志，继续检查其他错误
                        sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;
                    }
                    else
                    {
                                                _logger.LogError("SSL证书验证失败（名称不匹配）: {Errors}", sslPolicyErrors);
                    
                        return false;
                    }
                }

                // 如果清除名称不匹配后已无错误，直接通过
                if (sslPolicyErrors == SslPolicyErrors.None)
                    return true;

                // WS-12 修复：处理链错误 — 仅在「自签名根证书」场景放行
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
                {
                    if (_options.Certificate.AllowSelfSignedCertificates && IsSelfSignedRoot(chain))
                    {
                                                _logger.LogWarning("允许自签名根证书（仅 UntrustedRoot）: {Errors}", sslPolicyErrors);
                    
                        return true;
                    }

                                        _logger.LogError("SSL证书验证失败（链错误，非自签名根或含其他链状态）: {Errors}", sslPolicyErrors);
                
                    return false;
                }

                // 其他错误严格拒绝
                                _logger.LogError("SSL证书验证失败: {Errors}", sslPolicyErrors);
            
                return false;
            };

                        _logger.LogDebug("已配置SSL证书验证 (允许自签名: {AllowSelfSigned})", _options.Certificate.AllowSelfSignedCertificates);
        
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置证书验证时发生错误");
        }
#else
        // .NET Standard 2.0 不支持 ClientWebSocketOptions.RemoteCertificateValidationCallback，
        // 因此下列 **5 项** 配置在本目标框架上被**静默忽略**（WS2-11 补全）。
        // 改造前只告警其中 2 项：Mode=Dev / Mode=Custom / AllowCertificateNameMismatch 三项无任何提示，
        // 运维按 Readme 配好证书旁路却发现仍被严格校验证书时，日志里找不到任何线索。
        // 本清单必须与上方 #if 分支实际读取的配置一一对应（含"回调被忽略"的 Custom 组合）。
        if (!_options.Certificate.ValidateServerCertificate)
        {
            _logger.LogWarning(".NET Standard 2.0 不支持自定义证书验证回调，ValidateServerCertificate 配置无效");
        }
        if (_options.Certificate.AllowSelfSignedCertificates)
        {
            _logger.LogWarning(".NET Standard 2.0 不支持自定义证书验证回调，AllowSelfSignedCertificates 配置无效");
        }
        if (_options.Certificate.AllowCertificateNameMismatch)
        {
            _logger.LogWarning(".NET Standard 2.0 不支持自定义证书验证回调，AllowCertificateNameMismatch 配置无效");
        }
        if (_options.Certificate.Mode == CertificateValidationMode.Dev)
        {
            _logger.LogWarning(".NET Standard 2.0 不支持自定义证书验证回调，Certificate.Mode=Dev 的放宽行为无效（仍按严格校验处理）");
        }
        if (_options.Certificate.Mode == CertificateValidationMode.Custom || _options.Certificate.CustomCallback is not null)
        {
            _logger.LogWarning(".NET Standard 2.0 不支持自定义证书验证回调，Certificate.CustomCallback 被忽略（Mode=Custom 无效）");
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
    /// <param name="owner">
    /// 声明本次断线的 socket 实例。传入非 <c>null</c> 时，若该实例已不是当前 socket
    /// （旧接收循环的迟到通知 / 已被替换的连接），则整条通知被丢弃。
    /// </param>
    /// <remarks>
    /// P0-4 修复：使用 <see cref="System.Threading.Interlocked.CompareExchange(ref int, int, int)"/> 实现原子的 check-then-set，
    /// 确保对同一次连接只递减一次连接计数并触发一次 <see cref="Disconnected"/> 事件。
    /// <para>
    /// P1-2 修复（I2）：<c>_disconnectedFired</c> 是实例级标志，不区分连接代次。旧接收循环在新连接建立后
    /// 抛出的非取消异常会命中 <c>TryClaimDisconnected</c> 成功 → 误报"新连接断开"、连接计数漂移，
    /// 并吞掉后续真实的断线声明。故这里增加 socket 身份校验：<b>只接受"当前 socket"的断线声明</b>。
    /// </para>
    /// </remarks>
    private void NotifyDisconnected(WebSocketCloseEventArgs args, ClientWebSocket? owner = null)
    {
        // I2：非当前 socket 的迟到通知一律丢弃（旧接收循环 / 已替换的连接）
        if (owner != null && !ReferenceEquals(Volatile.Read(ref _webSocket), owner))
        {
            _logger.LogDebug("忽略过期连接（非当前 socket）的断线声明，避免误报新连接断开");
            return;
        }

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
    /// <param name="errorTypeOverride">
    /// 可选的错误类型覆盖值。用于把"语义明确但异常类型通用"的场景（如分片超限用
    /// <see cref="InvalidOperationException"/>）映射为可直接用于告警规则的错误类型；
    /// 为 <c>null</c> 时沿用 <see cref="ErrorRecoveryStrategy"/> 的分析结果。
    /// </param>
    /// <remarks>
    /// 该方法会创建<see cref="WebSocketErrorEventArgs"/>并触发<see cref="Error"/>事件。
    /// 会自动检测异常类型，设置网络错误和认证错误的标志。
    /// 用户回调抛出的异常会被捕获并记录，不会影响调用方。
    /// </remarks>
    private void OnError(Exception ex, string context, string? errorTypeOverride = null)
    {
        // 使用错误恢复策略分析异常
        var recoveryResult = _errorRecoveryStrategy.AnalyzeError(ex, context);

        var errorArgs = new WebSocketErrorEventArgs
        {
            Exception = ex,
            ErrorMessage = $"{context}: {ex.Message}",
            ErrorType = errorTypeOverride ?? recoveryResult.ErrorType,
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
    /// 会执行限时关闭握手（P1-4）、释放 socket 与 CTS。重复调用安全。
    /// <para>P1-5（I9）：<b>不释放</b> <c>_connectionLock</c>/<c>_sendLock</c>（未访问 <c>AvailableWaitHandle</c>，无句柄泄漏）。</para>
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

            // WS2-01 ③（I13）：释放同样是"使 socket 不再被读取"的终止路径，必须参与原子占位。
            // 此前 Dispose 只关 socket、不占位：若释放时连接仍为 Open（例如宿主直接释放一个
            // "接收循环已死但 socket 还 Open"的僵尸态客户端），_disconnectedFired 永远停在 0、
            // _connectionCount 永远不归零，且任何后续触发的重连/健康检查看到的状态都是矛盾的。
            // 这里只做**占位**、不派发事件（Dispose 语义下用户已明确要求终止，无需再通知；
            // 也避免在释放路径上执行用户回调带来新的异常面）。
            if (webSocket != null && TryClaimDisconnected())
            {
                _logger.LogDebug("释放连接管理器时已占位断线声明（连接计数已归零）");
            }

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

            // P1-5 修复（I9）：不再释放 _connectionLock / _sendLock。
            // 本类型从不访问 SemaphoreSlim.AvailableWaitHandle，不释放不会产生任何 OS 句柄泄漏；
            // 而释放会与在途 WaitAsync/Release 构成 ObjectDisposedException 竞态
            // （此前 Dispose 与并发 SendMessageAsync 会随机抛出 ObjectDisposedException）。
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
    /// <para>
    /// P1-5 行为变更：本方法返回后，<see cref="ConnectAsync"/> / <see cref="DisconnectAsync"/> /
    /// <see cref="SendMessageAsync"/> / <see cref="SendBinaryMessageAsync(ArraySegment{byte}, CancellationToken)"/>
    /// 将<b>确定性抛出</b> <see cref="ObjectDisposedException"/>（此前为"随机抛出或偶发成功"）。
    /// </para>
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

            // WS2-01 ③（I13）：同 DisposeAsync — 释放路径必须占位（只占位、不派发事件）
            if (webSocket != null && TryClaimDisconnected())
            {
                _logger.LogDebug("同步释放连接管理器时已占位断线声明（连接计数已归零）");
            }

            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    // WS-09 修复（P1-4）：同步路径下使用独立 closeCts（5s）而非 CancellationToken.None，
                    // 确保服务端不应答时 CloseAsync 能被取消而非悬挂。
                    // 通过 Task.Run 脱离调用方同步上下文，降低死锁风险（P1-3）。
                    // P2-11 修复：
                    //  ① 超时后强制 Abort，使在途 CloseAsync 立即以异常结束（此前会留下"游离任务"）；
                    //  ② 无条件观察一次该任务的异常，避免 UnobservedTaskException；
                    //  ③ closeCts 的释放延后到任务真正结束之后（此前 using 作用域先于任务结束即释放，
                    //     在途 CloseAsync 会拿到"已释放令牌"而抛出与关闭无关的异常）。
                    var closeCts = new CancellationTokenSource(CloseHandshakeTimeout);
                    var closeTask = Task.Run(() => webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "客户端释放资源",
                        closeCts.Token));

                    _ = closeTask.ContinueWith(
                        t =>
                        {
                            _ = t.Exception;   // 观察异常（避免 UnobservedTaskException）
                            closeCts.Dispose();
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);

                    if (!closeTask.Wait(CloseHandshakeTimeout))
                    {
                        _logger.LogDebug("同步释放时关闭握手超时，强制中止连接以回收在途任务");
                        TryAbort(webSocket);
                        _ = closeTask.Wait(TimeSpan.FromSeconds(1));
                    }
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

            // P1-5 修复（I9）：同 DisposeAsync，不再释放 _connectionLock / _sendLock（无句柄泄漏，且消除释放竞态）
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放连接管理器资源时发生错误");
        }

        GC.SuppressFinalize(this);
    }
}
