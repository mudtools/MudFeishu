
// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.WebSocket.SocketEventArgs;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket后台服务，用于自动启动和管理WebSocket连接
/// </summary>
public sealed class FeishuWebSocketHostedService : BackgroundService, IDisposable
{
    private readonly ILogger<FeishuWebSocketHostedService> _logger;
    private readonly IFeishuWebSocketManager _webSocketManager;
    private readonly FeishuWebSocketOptions _options;

    // 心跳和健康检查
    private Timer? _heartbeatTimer;
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="webSocketManager">WebSocket管理器</param>
    /// <param name="options">WebSocket配置选项</param>
    public FeishuWebSocketHostedService(
        ILogger<FeishuWebSocketHostedService> logger,
        IFeishuWebSocketManager webSocketManager,
        IOptions<FeishuWebSocketOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // 订阅WebSocket事件
        _webSocketManager.Connected += OnConnected;
        _webSocketManager.Disconnected += OnDisconnected;
        _webSocketManager.Error += OnError;
    }

    /// <summary>
    /// 使用指数退避策略尝试重连
    /// </summary>
    /// <param name="attempt">当前尝试次数（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否重连成功</returns>
    private async Task<bool> TryReconnectWithBackoffAsync(int attempt, CancellationToken cancellationToken)
    {
        // 指数退避：delay = baseDelay * (2^attempt)，最大不超过30秒
        var baseDelay = TimeSpan.FromMilliseconds(_options.ReconnectDelayMs);
        var exponentialDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
        var maxDelay = TimeSpan.FromSeconds(30);
        var delay = exponentialDelay > maxDelay ? maxDelay : exponentialDelay;

        _logger.LogInformation("等待 {Delay}毫秒后进行第 {Attempt} 次重连尝试", delay.TotalMilliseconds, attempt + 1);
        await Task.Delay(delay, cancellationToken);

        try
        {
            await _webSocketManager.ReconnectAsync(cancellationToken);
            return _webSocketManager.IsConnected;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "第 {Attempt} 次重连尝试失败", attempt + 1);
            return false;
        }
    }

    /// <summary>
    /// 执行后台服务
    /// </summary>
    /// <param name="stoppingToken">停止令牌</param>
    /// <returns>执行任务</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("飞书WebSocket后台服务正在启动...");

        try
        {
            // 启动WebSocket连接
            await _webSocketManager.StartAsync(stoppingToken);

            // 保持服务运行，直到收到停止信号
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 每隔一段时间检查连接状态
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                    // 如果连接断开且启用自动重连，则尝试重连
                    if (!_webSocketManager.IsConnected && _options.AutoReconnect)
                    {
                        _logger.LogInformation("检测到连接断开，尝试重新连接...");

                        var maxReconnectAttempts = _options.MaxReconnectAttempts;
                        var reconnected = false;

                        for (int attempt = 0; attempt < maxReconnectAttempts && !reconnected; attempt++)
                        {
                            if (_options.EnableLogging)
                                _logger.LogInformation("重连尝试 ({Attempt}/{MaxAttempts})...", attempt + 1, maxReconnectAttempts);

                            reconnected = await TryReconnectWithBackoffAsync(attempt, stoppingToken);

                            if (reconnected)
                            {
                                _logger.LogInformation("重连成功");
                                break;
                            }
                        }

                        if (!reconnected)
                        {
                            if (_options.EnableLogging)
                                _logger.LogError("已达到最大重连次数 ({MaxAttempts})，停止重连", maxReconnectAttempts);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // 正常取消，不需要处理
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "检查连接状态时发生错误");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "飞书WebSocket后台服务运行时发生错误");
        }
        finally
        {
            _logger.LogInformation("飞书WebSocket后台服务正在停止...");
            await _webSocketManager.StopAsync(stoppingToken);
            _logger.LogInformation("飞书WebSocket后台服务已停止");
        }
    }

    /// <summary>
    /// 停止后台服务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停止任务</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止飞书WebSocket后台服务...");
        await base.StopAsync(cancellationToken);
        await _webSocketManager.StopAsync(cancellationToken);
        _logger.LogInformation("飞书WebSocket后台服务已停止");
    }

    /// <summary>
    /// WebSocket连接建立事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnConnected(object? sender, System.EventArgs e)
    {
        var state = _webSocketManager.GetConnectionState();
        _logger.LogInformation("飞书WebSocket连接已建立 (时间: {Time}, 重连次数: {ReconnectCount})",
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), state.ReconnectCount);

        // 启动心跳检测
        StartHeartbeat();
    }

    /// <summary>
    /// WebSocket连接断开事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnDisconnected(object? sender, WebSocketCloseEventArgs e)
    {
        if (_options.EnableLogging)
        {
            var stats = _webSocketManager.GetConnectionStats();
            _logger.LogInformation("飞书WebSocket连接已断开: {Status} - {Description} (持续时间: {Duration})",
                e.CloseStatus, e.CloseStatusDescription, stats.Uptime);
        }

        // 停止心跳检测
        StopHeartbeat();
    }

    /// <summary>
    /// WebSocket错误事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnError(object? sender, WebSocketErrorEventArgs e)
    {
        if (_options.EnableLogging)
            _logger.LogError(e.Exception, "飞书WebSocket发生错误: {Message} (类型: {Type})", e.ErrorMessage, e.ErrorType);
    }

    /// <summary>
    /// 启动心跳检测
    /// </summary>
    private void StartHeartbeat()
    {
        StopHeartbeat(); // 确保没有重复的心跳定时器

        if (_options.HeartbeatIntervalMs > 0)
        {
            _heartbeatTimer = new Timer(HeartbeatCallback, null,
                TimeSpan.FromMilliseconds(_options.HeartbeatIntervalMs),
                TimeSpan.FromMilliseconds(_options.HeartbeatIntervalMs));
        }
    }

    /// <summary>
    /// 停止心跳检测
    /// </summary>
    private void StopHeartbeat()
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
    }

    /// <summary>
    /// 心跳回调
    /// </summary>
    /// <param name="state">状态对象</param>
    private async void HeartbeatCallback(object? state)
    {
        if (_disposed || !_webSocketManager.IsConnected)
            return;

        try
        {
            // 发送心跳消息（这里可以自定义心跳消息格式）
            var heartbeatMessage = "{\"type\":\"heartbeat\",\"timestamp\":" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "}";
            await _webSocketManager.SendMessageAsync(heartbeatMessage);

            if (_options.EnableLogging)
                _logger.LogDebug("心跳检测成功");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "心跳检测失败");
        }
    }

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    /// <returns>连接统计信息</returns>
    public (TimeSpan Uptime, int ReconnectCount, Exception? LastError) GetConnectionStats()
    {
        return _webSocketManager.GetConnectionStats();
    }

    /// <summary>
    /// 获取详细连接状态
    /// </summary>
    /// <returns>连接状态详情</returns>
    public WebSocketConnectionState GetConnectionState()
    {
        return _webSocketManager.GetConnectionState();
    }

    /// <summary>
    /// 重写Dispose方法，确保资源正确释放
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 重写Dispose方法，确保资源正确释放
    /// </summary>
    /// <param name="disposing">是否正在释放托管资源</param>
    protected void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            try
            {
                // 取消事件订阅，防止内存泄漏
                _webSocketManager.Connected -= OnConnected;
                _webSocketManager.Disconnected -= OnDisconnected;
                _webSocketManager.Error -= OnError;

                // 停止心跳检测
                StopHeartbeat();

                _logger.LogInformation("飞书WebSocket后台服务资源已清理");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理资源时发生异常");
            }
        }

        _disposed = true;
    }
}
