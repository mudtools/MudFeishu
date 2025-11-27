
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.TokenManager;
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
    }

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
                var accessToken = await _appTokenManager.GetTokenAsync(combinedCts.Token);
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new InvalidOperationException("无法获取应用访问令牌");
                }
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                _logger.LogError("获取应用访问令牌超时，超时时间: {TimeoutSeconds}秒", timeoutSeconds);
                throw new TimeoutException($"获取应用访问令牌超时，超时时间: {timeoutSeconds}秒");
            }

            // 获取WebSocket端点
            var credentials = new AppCredentials
            {
                AppId = _feishuOptions.AppId,
                AppSecret = _feishuOptions.AppSecret
            };

            // 使用重试策略获取WebSocket端点
            var maxRetries = _feishuOptions.RetryCount ?? 3;
            WsEndpointResult? wsEndpointData = null;
            var retryCount = 0;
            var success = false;

            while (!success && retryCount <= maxRetries)
            {
                try
                {
                    var wsEndpointResult = await _authenticationApi.GetWebSocketEndpointAsync(credentials, cancellationToken);
                    if (wsEndpointResult?.Data != null)
                    {
                        wsEndpointData = wsEndpointResult.Data;
                        success = true;
                    }
                    else
                    {
                        throw new InvalidOperationException("无法获取WebSocket端点信息");
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "获取WebSocket端点失败，尝试次数: {RetryCount}/{MaxRetries}", retryCount, maxRetries);

                    if (retryCount > maxRetries)
                    {
                        _logger.LogError(ex, "获取WebSocket端点失败，已达到最大重试次数");
                        throw new InvalidOperationException($"获取WebSocket端点失败，已重试{maxRetries}次", ex);
                    }

                    // 等待一段时间再重试
                    await Task.Delay(1000 * retryCount, cancellationToken);
                }
            }

            if (wsEndpointData == null)
            {
                throw new InvalidOperationException("无法获取WebSocket端点信息");
            }

            // 建立WebSocket连接并认证
            var appAccessToken = await _appTokenManager.GetTokenAsync(combinedCts.Token);
            await _webSocketClient.ConnectAsync(wsEndpointData, appAccessToken, cancellationToken);

            _isRunning = true;
            _logger.LogInformation("飞书WebSocket服务启动成功");
        }
        catch (Exception ex)
        {
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
            _logger.LogInformation("飞书WebSocket服务已停止");
        }
        catch (Exception ex)
        {
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

        try
        {
            // 先断开现有连接
            if (IsConnected)
            {
                await _webSocketClient.DisconnectAsync(cancellationToken);
            }

            // 重新启动连接
            await StartAsync(cancellationToken);

            _logger.LogInformation("飞书WebSocket服务重连成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "飞书WebSocket服务重连失败");
            throw;
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
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _startStopLock.Wait(TimeSpan.FromSeconds(5));
            if (_isRunning)
            {
                _ = StopAsync().ConfigureAwait(false);
            }

            _webSocketClient.Dispose();
            _startStopLock.Dispose();
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
