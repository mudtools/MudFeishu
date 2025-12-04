
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.SocketEventArgs;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket管理器实现，用于管理WebSocket连接的生命周期
/// </summary>
public class FeishuWebSocketManager : IFeishuWebSocketManager
{
    private readonly ILogger<FeishuWebSocketManager> _logger;
    private readonly IFeishuV3AuthenticationApi _authenticationApi;
    private readonly ITenantTokenManager _appTokenManager;
    private readonly FeishuOptions _feishuOptions;
    private readonly FeishuWebSocketOptions _webSocketOptions;
    private readonly IFeishuWebSocketClient _webSocketClient;
    private readonly SemaphoreSlim _startStopLock = new(1, 1);
    private bool _isRunning = false;
    private bool _disposed = false;

    // 令牌缓存
    private string? _cachedAccessToken;
    private DateTime _tokenExpiryTime = DateTime.MinValue;
    private readonly object _tokenLock = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="authenticationApi">认证API</param>
    /// <param name="appTokenManager">应用令牌管理器</param>
    /// <param name="feishuOptions">飞书配置选项</param>
    /// <param name="webSocketOptions">WebSocket配置选项</param>
    /// <param name="webSocketClient">WebSocket客户端</param>
    public FeishuWebSocketManager(
        ILogger<FeishuWebSocketManager> logger,
        IFeishuV3AuthenticationApi authenticationApi,
        ITenantTokenManager appTokenManager,
        IOptions<FeishuOptions> feishuOptions,
        IOptions<FeishuWebSocketOptions> webSocketOptions,
        IFeishuWebSocketClient webSocketClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _appTokenManager = appTokenManager ?? throw new ArgumentNullException(nameof(appTokenManager));
        _feishuOptions = feishuOptions?.Value ?? throw new ArgumentNullException(nameof(feishuOptions));
        _webSocketOptions = webSocketOptions?.Value ?? throw new ArgumentNullException(nameof(webSocketOptions));
        _webSocketClient = webSocketClient ?? throw new ArgumentNullException(nameof(webSocketClient));

        // 订阅客户端事件
        _webSocketClient.Connected += OnClientConnected;
        _webSocketClient.Disconnected += OnClientDisconnected;
        _webSocketClient.MessageReceived += OnClientMessageReceived;
        _webSocketClient.Error += OnClientError;
        _webSocketClient.HeartbeatReceived += OnClientHeartbeatReceived;
    }

    /// <summary>
    /// 获取有效的访问令牌（带缓存）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>有效的访问令牌</returns>
    private async Task<string> GetValidAccessTokenAsync(CancellationToken cancellationToken)
    {
        lock (_tokenLock)
        {
            if (_cachedAccessToken != null && DateTime.UtcNow < _tokenExpiryTime)
            {
                return _cachedAccessToken;
            }
        }

        // 需要刷新令牌
        var newToken = await _appTokenManager.GetTokenAsync(cancellationToken);

        if (string.IsNullOrEmpty(newToken))
        {
            throw new InvalidOperationException("获取的应用访问令牌为空");
        }

        lock (_tokenLock)
        {
            _cachedAccessToken = newToken;
            // 假设令牌有效期为2小时，提前5分钟刷新
            _tokenExpiryTime = DateTime.UtcNow.AddHours(2).AddMinutes(-5);
        }

        return newToken;
    }

    /// <summary>
    /// 使用指数退避策略重试操作
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要重试的操作</param>
    /// <param name="maxRetries">最大重试次数</param>
    /// <param name="operationName">操作名称（用于日志）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    private async Task<T> RetryWithExponentialBackoffAsync<T>(
        Func<Task<T>> operation,
        int maxRetries,
        string operationName,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i <= maxRetries; i++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (i < maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, i) * 1000); // 1s, 2s, 4s, 8s...
                _logger.LogWarning(ex, "{OperationName}失败，将在{Delay}毫秒后重试 (尝试 {RetryCount}/{MaxRetries})",
                    operationName, delay.TotalMilliseconds, i + 1, maxRetries + 1);

                await Task.Delay(delay, cancellationToken);
            }
        }

        // 最后一次尝试，不捕获异常
        _logger.LogError("{OperationName}失败，已达到最大重试次数 {MaxRetries}", operationName, maxRetries + 1);

        // 执行最后一次尝试
        return await operation();
    }

    // 监控字段
    private DateTime _connectedTime = DateTime.MinValue;
    private int _reconnectCount = 0;
    private Exception? _lastError;
    private bool _isReconnecting = false;
    private readonly object _stateLock = new();

    /// <summary>
    /// WebSocket客户端实例
    /// </summary>
    public IFeishuWebSocketClient Client => _webSocketClient;

    /// <summary>
    /// 连接状态
    /// </summary>
    public bool IsConnected => _webSocketClient.State == System.Net.WebSockets.WebSocketState.Open;

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
    /// 接收到心跳事件
    /// </summary>
    public event EventHandler<WebSocketHeartbeatEventArgs>? HeartbeatReceived;

    /// <summary>
    /// 启动WebSocket连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动任务</returns>
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

            // 获取应用访问令牌，使用配置的超时时间
            int timeoutSeconds = 30; // 默认超时时间
            if (!string.IsNullOrEmpty(_feishuOptions.TimeOut) && int.TryParse(_feishuOptions.TimeOut, out int configuredTimeout))
            {
                timeoutSeconds = configuredTimeout;
            }

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                var accessToken = await GetValidAccessTokenAsync(combinedCts.Token);
                if (string.IsNullOrEmpty(accessToken))
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

            // 获取WebSocket端点
            var credentials = new WsAppCredentials
            {
                AppId = _feishuOptions.AppId,
                AppSecret = _feishuOptions.AppSecret
            };

            // 使用重试策略获取WebSocket端点
            var maxRetries = _feishuOptions.RetryCount ?? 3;
            WsEndpointResult? wsEndpointData = null;

            wsEndpointData = await RetryWithExponentialBackoffAsync(
                async () =>
                {
                    var wsEndpointResult = await _authenticationApi.GetWebSocketEndpointAsync(credentials, cancellationToken);
                    if (wsEndpointResult?.Data == null)
                    {
                        throw new InvalidOperationException("获取的WebSocket端点信息为空");
                    }
                    return wsEndpointResult.Data;
                },
                maxRetries,
                "获取WebSocket端点",
                cancellationToken);

            if (wsEndpointData == null)
            {
                throw new InvalidOperationException("无法获取WebSocket端点信息");
            }

            // 建立WebSocket连接并认证
            var appAccessToken = await GetValidAccessTokenAsync(combinedCts.Token);
            await _webSocketClient.ConnectAsync(wsEndpointData, appAccessToken, cancellationToken);

            _isRunning = true;

            lock (_stateLock)
            {
                _connectedTime = DateTime.UtcNow;
                _lastError = null;
                _isReconnecting = false;
            }

            _logger.LogInformation("飞书WebSocket服务启动成功");
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                _lastError = ex;
            }
            _logger.LogError(ex, "启动飞书WebSocket服务失败");
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

            _logger.LogInformation("正在停止飞书WebSocket服务...");

            await _webSocketClient.DisconnectAsync(cancellationToken);

            _isRunning = false;

            lock (_stateLock)
            {
                _connectedTime = DateTime.MinValue;
                _isReconnecting = false;
            }

            _logger.LogInformation("飞书WebSocket服务已停止");
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                _lastError = ex;
            }
            _logger.LogError(ex, "停止飞书WebSocket服务失败");
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
    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("正在重新连接飞书WebSocket服务...");

        lock (_stateLock)
        {
            _isReconnecting = true;
        }

        try
        {
            // 先断开现有连接
            if (IsConnected)
            {
                await _webSocketClient.DisconnectAsync(cancellationToken);
            }

            // 重新启动连接
            await StartAsync(cancellationToken);

            lock (_stateLock)
            {
                _reconnectCount++;
                _isReconnecting = false;
            }

            _logger.LogInformation("飞书WebSocket服务重连成功");
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                _lastError = ex;
                _isReconnecting = false;
            }
            _logger.LogError(ex, "飞书WebSocket服务重连失败");
            throw;
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
            if (_isReconnecting)
            {
                return WebSocketConnectionState.Reconnecting with
                {
                    ReconnectCount = _reconnectCount,
                    LastError = _lastError
                };
            }

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
        _logger.LogInformation("飞书WebSocket连接已建立");
        Connected?.Invoke(this, e);
    }

    /// <summary>
    /// 客户端连接断开事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnClientDisconnected(object? sender, WebSocketCloseEventArgs e)
    {
        _logger.LogInformation("飞书WebSocket连接已断开: {Status} - {Description} (服务器端: {IsServerInitiated}, 时间: {Timestamp})",
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
    private void OnClientMessageReceived(object? sender, WebSocketMessageEventArgs e)
    {
        _logger.LogDebug("接收到飞书WebSocket消息: {Message} (大小: {Size}字节, 队列: {Queue}条, 时间: {Timestamp})",
            e.Message, e.MessageSize, e.QueueCount, e.Timestamp);
        MessageReceived?.Invoke(this, e);
    }

    /// <summary>
    /// 客户端错误事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnClientError(object? sender, WebSocketErrorEventArgs e)
    {
        _logger.LogError(e.Exception, "飞书WebSocket发生错误: {Message} (类型: {ErrorType}, 状态: {State}, 网络: {IsNetwork}, 认证: {IsAuth}, 时间: {Timestamp})",
            e.ErrorMessage, e.ErrorType, e.ConnectionState, e.IsNetworkError, e.IsAuthError, e.Timestamp);
        Error?.Invoke(this, e);
    }

    /// <summary>
    /// 客户端心跳事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnClientHeartbeatReceived(object? sender, WebSocketHeartbeatEventArgs e)
    {
        _logger.LogDebug("飞书WebSocket心跳消息 - 时间戳: {Timestamp}, 间隔: {Interval}s, 状态: {Status}",
            e.Timestamp, e.Interval, e.Status);
        HeartbeatReceived?.Invoke(this, e);
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
            // 同步等待锁，设置超时避免死锁
            if (_startStopLock.Wait(TimeSpan.FromSeconds(5)))
            {
                try
                {
                    if (_isRunning)
                    {
                        // 同步等待停止操作完成，但不无限等待
                        var stopTask = StopAsync();
                        if (!stopTask.Wait(TimeSpan.FromSeconds(3)))
                        {
                            _logger.LogWarning("停止WebSocket服务超时，强制释放资源");
                        }
                    }
                }
                finally
                {
                    _startStopLock.Release();
                }
            }
            else
            {
                _logger.LogWarning("获取启动停止锁超时，强制释放资源");
            }

            // 清理令牌缓存
            lock (_tokenLock)
            {
                _cachedAccessToken = null;
                _tokenExpiryTime = DateTime.MinValue;
            }

            _webSocketClient?.Dispose();
            _startStopLock?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放资源时发生错误");
        }
        finally
        {
            _disposed = true;
        }

        // 调用 GC.SuppressFinalize 以防止终结器被调用
        GC.SuppressFinalize(this);
    }
}
