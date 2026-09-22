// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Diagnostics.CodeAnalysis;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket管理器实现，用于管理WebSocket连接的生命周期
/// </summary>
public class FeishuWebSocketManager : IFeishuWebSocketManager, IAsyncDisposable, IDisposable
{
    private readonly ILogger<FeishuWebSocketManager> _logger;
    // TMR-P0-2（F2）：默认应用身份运行时可变（热更新 / SetDefaultApp），主路径经
    // IFeishuAppManager 现取当前默认上下文；_appContextFallback 仅为旧构造签名兼容保留。
    private readonly IFeishuAppManager? _appManager;
    private readonly IFeishuAppContext _appContextFallback;
    private readonly IOptionsMonitor<FeishuWebSocketOptions> _webSocketOptionsMonitor;
    private readonly IFeishuWebSocketClient _webSocketClient;
    private readonly SemaphoreSlim _startStopLock = new(1, 1);
    // WS-23 修复（P2-13）：_isRunning 改为 volatile bool，确保跨线程可见性
    private volatile bool _isRunning = false;
    private volatile bool _isReconnecting = false;
    // WS-15 修复（P1-12）：_disposed 改为 int + Interlocked.Exchange 实现原子 check-then-set
    private int _disposed = 0;
    // TMR-P0-2（F2）：回退告警只发一次，避免每次连接重复刷日志
    private int _fallbackWarned = 0;

    /// <summary>
    /// 构造函数（推荐，DI 最长构造优先选择）。
    /// </summary>
    /// <remarks>
    /// TMR-P0-2（F2）：经 <see cref="IFeishuAppManager"/> 在每次启动/重连时现取当前默认应用上下文，
    /// 使配置热更新与 <c>SetDefaultApp</c> 对 WebSocket 长连接立即生效。
    /// 修复前经构造期捕获的 <c>IFeishuAppContext</c> Singleton 取令牌与凭据，
    /// 热更新后指向已退休上下文（ODE / 旧凭据）。
    /// 注意：与 4 参旧构造保持<b>不同元数</b>——避免同元数重载导致宿主传 <c>null</c> 字面量时
    /// 编译二义，以及 MS.DI 多构造选择歧义。
    /// </remarks>
    /// <param name="logger">日志记录器</param>
    /// <param name="appManager">飞书应用管理器（现取默认应用上下文）</param>
    /// <param name="appContext">飞书应用上下文（回退通道，当 <paramref name="appManager"/> 不可用时使用）</param>
    /// <param name="webSocketOptions">WebSocket配置选项监控器（支持热更新）</param>
    /// <param name="webSocketClient">WebSocket客户端</param>
    public FeishuWebSocketManager(
        ILogger<FeishuWebSocketManager> logger,
        IFeishuAppManager appManager,
        IFeishuAppContext? appContext = null,
        IOptionsMonitor<FeishuWebSocketOptions>? webSocketOptions = null,
        IFeishuWebSocketClient? webSocketClient = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appManager = appManager ?? throw new ArgumentNullException(nameof(appManager));
        _appContextFallback = appContext!; // 可为 null：仅当 appManager 现取路径不可用时的运行时告警
        _webSocketOptionsMonitor = webSocketOptions ?? throw new ArgumentNullException(nameof(webSocketOptions));
        _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));

        // 订阅客户端事件
        _webSocketClient.Connected += OnClientConnected;
        _webSocketClient.Disconnected += OnClientDisconnected;
        _webSocketClient.MessageReceived += OnClientMessageReceived;
        _webSocketClient.Error += OnClientError;
    }

    /// <summary>
    /// 构造函数（旧签名，向后兼容宿主手工 new）。
    /// </summary>
    /// <remarks>
    /// 无 <see cref="IFeishuAppManager"/> 时无现取通道：取令牌/凭据将固定使用构造期传入的
    /// <paramref name="appContext"/>，热更新 / <c>SetDefaultApp</c> 后可能失效（运行时告警提示）。
    /// DI 宿主推荐使用包含 <see cref="IFeishuAppManager"/> 的 5 参构造（最长构造优先）。
    /// </remarks>
    /// <param name="logger">日志记录器</param>
    /// <param name="appContext">飞书应用上下文（构造期快照）</param>
    /// <param name="webSocketOptions">WebSocket配置选项监控器（支持热更新）</param>
    /// <param name="webSocketClient">WebSocket客户端</param>
    public FeishuWebSocketManager(
        ILogger<FeishuWebSocketManager> logger,
        IFeishuAppContext appContext,
        IOptionsMonitor<FeishuWebSocketOptions> webSocketOptions,
        IFeishuWebSocketClient webSocketClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appManager = null;
        _appContextFallback = appContext ?? throw new ArgumentNullException(nameof(appContext));
        _webSocketOptionsMonitor = webSocketOptions ?? throw new ArgumentNullException(nameof(webSocketOptions));
        _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));
        _logger.LogWarning(
            "FeishuWebSocketManager 未注入 IFeishuAppManager：取令牌/凭据将固定使用构造期 IFeishuAppContext，" +
            "配置热更新 / SetDefaultApp 后不会跟随默认应用。请改用 DI 注册（多应用模式自动注入 IFeishuAppManager）。");

        // 订阅客户端事件
        _webSocketClient.Connected += OnClientConnected;
        _webSocketClient.Disconnected += OnClientDisconnected;
        _webSocketClient.MessageReceived += OnClientMessageReceived;
        _webSocketClient.Error += OnClientError;
    }

    /// <summary>
    /// TMR-P0-2（F2）：现取当前默认应用上下文。
    /// </summary>
    /// <remarks>
    /// 默认应用身份运行时可变（热更新 / <c>SetDefaultApp</c> / <c>RemoveApp</c> 提升），
    /// 构造期捕获的 <c>IFeishuAppContext</c> Singleton 在热更新后指向已退休上下文。
    /// 调用方须在同一次启动/重连内对返回值保持单引用，保证令牌与凭据（AppId/AppSecret）
    /// 取自同一上下文——混用新旧上下文会导致凭据错配。
    /// </remarks>
    private IFeishuAppContext ResolveCurrentContext()
    {
        if (_appManager != null)
        {
            return _appManager.GetDefaultApp();
        }

        if (Interlocked.Exchange(ref _fallbackWarned, 1) == 0)
        {
            _logger.LogWarning(
                "取令牌/凭据回退到构造期 IFeishuAppContext（未注入 IFeishuAppManager），" +
                "默认应用热更新后此处将不会跟随。");
        }
        return _appContextFallback;
    }

    /// <summary>
    /// 获取有效的访问令牌
    /// </summary>
    /// <param name="context">现取的应用上下文（TMR-P0-2：与调用方的凭据/端点取自同一上下文）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>有效的访问令牌</returns>
    private async Task<string> GetValidAccessTokenAsync(IFeishuAppContext context, CancellationToken cancellationToken)
    {
        var tokenManager = context.GetTokenManager("TenantAccessToken");
        var token = await tokenManager.GetTokenAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("获取的应用访问令牌为空");
        }

        return token;
    }

    /// <summary>
    /// 使用指数退避策略重试操作
    /// </summary>
    // 监控字段
    private DateTime _connectedTime = DateTime.MinValue;
    private int _reconnectCount = 0;
    private Exception? _lastError;
    private readonly object _stateLock = new();

    /// <summary>
    /// WebSocket客户端实例
    /// </summary>
    public IFeishuWebSocketClient Client => _webSocketClient;

    /// <summary>
    /// 连接状态
    /// </summary>
    /// <remarks>
    /// 注意：本属性只看 <see cref="System.Net.WebSockets.WebSocketState"/>，因此在
    /// "接收循环已死但 socket 仍 Open"的僵尸态下为 <c>true</c>。需要"连接是否可用来收事件"
    /// 请结合 <see cref="Liveness"/>（其 <c>IsZombie</c> 为此组合提供确定性判定）。
    /// </remarks>
    public bool IsConnected => _webSocketClient.State == System.Net.WebSockets.WebSocketState.Open;

    /// <summary>
    /// 连接存活探针快照（F1；供健康检查与周期性自愈判定使用）。
    /// </summary>
    /// <remarks>
    /// 客户端未实现具体类型（自定义/替身实现）时返回 <c>default</c>（各项为 false/0），
    /// 健康检查会因此回落到原有的"仅按连接状态"判定，不产生误报。
    /// </remarks>
    internal ConnectionLiveness Liveness
        => _webSocketClient is FeishuWebSocketClient client ? client.Liveness : default;

    /// <summary>
    /// 连接建立事件
    /// </summary>
    public event EventHandler<EventArgs>? Connected;

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
    /// 启动WebSocket连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动任务</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _startStopLock.WaitAsync(cancellationToken);
        try
        {
            if (_isRunning)
            {
                _logger.LogWarning("WebSocket服务已在运行中");
                return;
            }
            _logger.LogInformation("正在启动飞书WebSocket服务...");

            // TMR-P0-2（F2）：本次启动/重连的整条链路（令牌、凭据、超时、重试参数、WS 端点）
            // 统一使用同一次现取的默认应用上下文，保证凭据一致性（禁止令牌与凭据混用新旧上下文）。
            var context = ResolveCurrentContext();

            // 获取应用访问令牌，使用配置的超时时间
            int timeoutSeconds = context.Config.TimeoutSeconds;

            // WS2-04 / I16（评审新增点）：TimeoutSeconds 来自 AppConfig，属**配置派生值**。
            // TimeSpan.FromSeconds(int) 本身不溢出，但 CancellationTokenSource(TimeSpan) 会校验上界
            // 并在超长时抛 ArgumentOutOfRangeException（.NET Core ≈ 49.7 天 / .NET Framework ≈ 24.8 天）；
            // 异常会以晦涩的启动失败形式暴露，在生产上极难定位。此处统一走钳制。
            var startupTimeout = TimeSpanGuards.ClampToCancellationTokenRange(TimeSpan.FromSeconds(timeoutSeconds));
            using var timeoutCts = new CancellationTokenSource(startupTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var combinedToken = combinedCts.Token;

            // 获取一次 AccessToken 并复用
            string appAccessToken;
            try
            {
                appAccessToken = await GetValidAccessTokenAsync(context, combinedToken);
                if (string.IsNullOrEmpty(appAccessToken))
                {
                    _logger.LogError("获取的应用访问令牌为空");
                    throw new InvalidOperationException("无法获取有效的应用访问令牌");
                }
                _logger.LogDebug("成功获取应用访问令牌");
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                _logger.LogError("获取应用访问令牌超时，超时时间: {TimeoutSeconds}秒", timeoutSeconds);
                throw new TimeoutException($"获取应用访问令牌超时，超时时间: {timeoutSeconds}秒");
            }

            // 获取WebSocket端点（凭据取自同一现取上下文，TMR-P0-2）
            var credentials = new WsAppCredentials
            {
                AppId = context.Config.AppId,
                AppSecret = context.Config.AppSecret
            };

            // 使用重试策略获取WebSocket端点（使用 combinedToken 传递超时控制）
            var maxRetries = context.Config.HttpRetry.MaxAttempts;
            WsEndpointResult? wsEndpointData = null;

            wsEndpointData = await RetryHelper.RetryWithExponentialBackoffAsync(
                _logger,
                async () =>
                {
                    var wsEndpointResult = await context.Authentication.GetWebSocketEndpointAsync(credentials, combinedToken);
                    if (wsEndpointResult?.Data == null)
                    {
                        throw new InvalidOperationException("获取的WebSocket端点信息为空");
                    }
                    return wsEndpointResult.Data;
                },
                maxRetries,
                context.Config.HttpRetry.DelayMs,
                "获取WebSocket端点",
                combinedToken);

            if (wsEndpointData == null)
            {
                throw new InvalidOperationException("无法获取WebSocket端点信息");
            }

            // 建立WebSocket连接并认证（复用已获取的 AccessToken，不再重复获取）
            await _webSocketClient.ConnectAsync(wsEndpointData, appAccessToken, combinedToken);

            _isRunning = true;

            lock (_stateLock)
            {
                _connectedTime = DateTime.UtcNow;
                _lastError = null;
            }

            _logger.LogInformation("--------------Mud飞书WebSocket服务启动成功--------------");
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                _lastError = ex;
            }
            _logger.LogError(ex, "--------------!!!启动Mud飞书WebSocket服务失败!!!--------------");
            throw;
        }
        finally
        {
            _startStopLock.Release();
        }
    }

    /// <summary>
    /// 停止WebSocket连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停止任务</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _startStopLock.WaitAsync(cancellationToken);
        try
        {
            if (!_isRunning)
            {

                _logger.LogWarning("WebSocket服务未在运行");
                return;
            }

            _logger.LogWarning("正在停止Mud飞书WebSocket服务...");

            await _webSocketClient.DisconnectAsync(cancellationToken);

            _isRunning = false;

            lock (_stateLock)
            {
                _connectedTime = DateTime.MinValue;
            }

            _logger.LogWarning("Mud飞书WebSocket服务已停止");
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                _lastError = ex;
            }
            _logger.LogError(ex, "停止Mud飞书WebSocket服务失败");
            throw;
        }
        finally
        {
            _startStopLock.Release();
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
        if (!IsConnected)
        {
            throw new InvalidOperationException("WebSocket未连接，无法发送消息");
        }

        await _webSocketClient.SendMessageAsync(message, cancellationToken);
    }

    /// <summary>
    /// 重新连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重连任务</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("正在重新连接Mud飞书WebSocket服务...");

        // 设置重连标志位，抑制重连过程中的 Disconnected 事件转发，
        // 防止 ReconnectAsync → DisconnectAsync → Disconnected 事件 → 触发新重连 的级联风暴。
        _isReconnecting = true;
        try
        {
            // 断开旧连接（如果仍然连接）
            if (IsConnected)
            {
                await _webSocketClient.DisconnectAsync(cancellationToken);
            }

            // 关键：强制重置 _isRunning 状态，确保 StartAsync 能建立新连接
            // 心跳超时或连接断开事件不会重置 _isRunning，只有 StopAsync 会重置。
            // 如果不在此处重置，StartAsync 会检测到 _isRunning == true 而直接返回，
            // 导致重连逻辑误认为成功但实际未建立新连接（死循环问题）。
            _isRunning = false;

            await StartAsync(cancellationToken);

            lock (_stateLock)
            {
                _reconnectCount++;
            }
            _logger.LogInformation("Mud飞书WebSocket服务重连成功");
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                _lastError = ex;
            }
            _logger.LogError(ex, "Mud飞书WebSocket服务重连失败");
            throw;
        }
        finally
        {
            _isReconnecting = false;
        }
    }

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    /// <returns>连接统计信息</returns>
    public (TimeSpan Uptime, int ReconnectCount, Exception? LastError) GetConnectionStats()
    {
        lock (_stateLock)
        {
            var uptime = _connectedTime == DateTime.MinValue
                ? TimeSpan.Zero
                : DateTime.UtcNow - _connectedTime;

            return (uptime, _reconnectCount, _lastError);
        }
    }

    /// <summary>
    /// 获取连接状态详情
    /// </summary>
    /// <returns>连接状态详情</returns>
    public WebSocketConnectionState GetConnectionState()
    {
        lock (_stateLock)
        {
            if (IsConnected)
            {
                return WebSocketConnectionState.Connected(_connectedTime, _reconnectCount);
            }

            return WebSocketConnectionState.Disconnected(_lastError);
        }
    }

    /// <summary>
    /// 客户端连接建立事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnClientConnected(object? sender, EventArgs e)
    {
        _logger.LogInformation("Mud 飞书WebSocket连接已建立");
        Connected?.Invoke(this, e);
    }

    /// <summary>
    /// 客户端连接断开事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnClientDisconnected(object? sender, WebSocketCloseEventArgs e)
    {
        // 重连过程中的主动断开不转发 Disconnected 事件，避免触发级联重连
        // （ReconnectAsync → DisconnectAsync → Disconnected 事件 → TryTriggerReconnect → 新重连）
        if (_isReconnecting)
        {
            _logger.LogDebug("重连过程中的断开事件已被抑制，不转发 Disconnected 事件");
            return;
        }

        _logger.LogInformation("Mud飞书WebSocket连接已断开: {Status} - {Description} (服务器端: {IsServerInitiated}, 时间: {Timestamp})",
            e.CloseStatus, e.CloseStatusDescription, e.IsServerInitiated, e.Timestamp);

        if (e.ConnectionDuration.HasValue)
        {
            _logger.LogInformation("连接持续时间: {Duration}", e.ConnectionDuration);
        }

        Disconnected?.Invoke(this, e);
    }

    /// <summary>
    /// 客户端消息接收事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    /// <remarks>
    /// WS2-05 修复（D5 / P1-4）：此前 <c>{Message}</c> 直传 <c>e.Message</c> —— 入站报文**全文**（未脱敏、
    /// 未截断）写入 Debug 日志。飞书事件报文内含 <c>header.token</c>、<c>event.*</c> 业务载荷与用户 PII，
    /// 全文入日志即长期留存于集中式日志系统。
    /// <para>
    /// 现与模块内既有正确范式对齐（<c>MessageRouter</c> / <c>FeishuEventMessageHandler</c> 均走
    /// <c>LogSanitizer.CleanMessage</c>）：只输出**结构化字段 + 脱敏截断预览**。
    /// 需要完整报文取证请在订阅者侧落库。
    /// </para>
    /// </remarks>
    private void OnClientMessageReceived(object? sender, WebSocketMessageEventArgs e)
    {
        _logger.LogDebug("接收到Mud 飞书WebSocket消息: 长度={Length} 预览={Preview} (大小: {Size}字节, 队列: {Queue}条, 时间: {Timestamp})",
                e.Message?.Length ?? 0,
                LogSanitizer.CleanMessage(e.Message, 200),
                e.MessageSize, e.QueueCount, e.Timestamp);
        MessageReceived?.Invoke(this, e);
    }

    /// <summary>
    /// 客户端错误事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnClientError(object? sender, WebSocketErrorEventArgs e)
    {
        // 可恢复错误（如服务端关闭连接）仅以 Warning 级别记录摘要，不输出完整堆栈，
        // 避免同一异常在事件冒泡链中被多次以 ERR 级别重复打印。
        // 不可恢复错误仍以 Error 级别记录完整异常信息。
        if (e.IsRecoverable)
        {
            _logger.LogWarning("Mud 飞书WebSocket发生可恢复错误: {Message} (类型: {ErrorType}, 状态: {State}, 网络: {IsNetwork}, 时间: {Timestamp})",
                e.ErrorMessage, e.ErrorType, e.ConnectionState, e.IsNetworkError, e.Timestamp);
        }
        else
        {
            _logger.LogError(e.Exception, "Mud 飞书WebSocket发生错误: {Message} (类型: {ErrorType}, 状态: {State}, 网络: {IsNetwork}, 认证: {IsAuth}, 时间: {Timestamp})",
                e.ErrorMessage, e.ErrorType, e.ConnectionState, e.IsNetworkError, e.IsAuthError, e.Timestamp);
        }
        Error?.Invoke(this, e);
    }

    /// <summary>
    /// 异步释放资源
    /// </summary>
    /// <returns>表示异步释放操作的任务</returns>
    /// <remarks>
    /// WS2-06 修复（I9）：<b>不释放</b> <c>_startStopLock</c>。本类型从不访问
    /// <see cref="SemaphoreSlim.AvailableWaitHandle"/>，不释放不产生任何 OS 句柄泄漏；
    /// 而释放会与在途 <c>StartAsync</c>/<c>StopAsync</c>/<c>ReconnectAsync</c> 的
    /// <c>WaitAsync</c>/<c>Release</c> 构成 <see cref="ObjectDisposedException"/> 竞态。
    /// 与模块内另外四处既有实现（<c>WebSocketConnectionManager</c>、<c>BinaryMessageProcessor</c>、
    /// <c>FeishuWebSocketConcurrencyService</c>、<c>ReconnectionOrchestrator</c>）保持一致。
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        try
        {
            // 使用异步方式停止服务
            if (_isRunning)
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try
                {
                    await StopAsync(timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("停止WebSocket服务超时，强制释放资源");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "停止WebSocket服务时发生错误");
                }
            }

            UnsubscribeClientEvents();

            // 异步释放客户端资源（如果客户端实现了IAsyncDisposable）
            if (_webSocketClient is IAsyncDisposable asyncDisposableClient)
            {
                await asyncDisposableClient.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _webSocketClient?.Dispose();
            }

            // WS2-06（I9）：不释放 _startStopLock（见 DisposeAsync 的 remarks）
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步释放资源时发生错误");
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 同步释放资源（尽力释放语义）
    /// </summary>
    /// <remarks>
    /// P0-2 修复（WS-02）：此前同步 <see cref="Dispose()"/> 会获取 <c>_startStopLock</c> 后调用
    /// <see cref="StopAsync"/>，而后者同样需要该锁 → 自死锁（约 3~5 秒超时后服务仍未停止）。
    /// <b>同步路径不再尝试停止服务</b>，仅做尽力释放：取消订阅事件 + Dispose 客户端 + 释放锁。
    /// 需确定性停止请调用 <see cref="StopAsync"/> 或 <see cref="DisposeAsync"/>。
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        try
        {
            // P0-2 修复：同步路径不再获取 _startStopLock，不调用 StopAsync。
            // StopAsync 内部需要获取同一把锁，构成不可重入的自死锁。
            // 异步路径 DisposeAsync 是唯一保证完成关闭握手与等待后台任务的路径。
            if (_isRunning)
            {
                _logger.LogWarning("同步 Dispose() 不保证停止服务，请改用 DisposeAsync() 或 StopAsync() 获取确定性停止");
            }

            UnsubscribeClientEvents();

            _webSocketClient?.Dispose();
            // WS2-06（I9）：同 DisposeAsync，不释放 _startStopLock
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放资源时发生错误");
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 取消订阅客户端事件，防止内存泄漏
    /// </summary>
    private void UnsubscribeClientEvents()
    {
        _webSocketClient.Connected -= OnClientConnected;
        _webSocketClient.Disconnected -= OnClientDisconnected;
        _webSocketClient.MessageReceived -= OnClientMessageReceived;
        _webSocketClient.Error -= OnClientError;
    }
}
