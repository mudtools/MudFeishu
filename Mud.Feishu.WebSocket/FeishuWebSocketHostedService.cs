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
    private bool _disposed = false;

    // 重连状态管理
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);
    private bool _isReconnecting = false;
    private DateTime _lastReconnectAttempt = DateTime.MinValue;
    private int _currentReconnectAttempt = 0;

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
    /// 统一的重连处理方法
    /// </summary>
    /// <param name="reason">重连原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否重连成功</returns>
    private async Task<bool> TryReconnectAsync(string reason, CancellationToken cancellationToken)
    {
        if (!_options.AutoReconnect)
        {
            _logger.LogInformation("自动重连已禁用，跳过重连");
            return false;
        }

        await _reconnectLock.WaitAsync(cancellationToken);
        try
        {
            // 检查是否已经在重连中
            if (_isReconnecting)
            {
                _logger.LogDebug("重连已在进行中，跳过重复重连请求");
                return false;
            }

            // 检查重连冷却期（防止过于频繁的重连尝试）
            var timeSinceLastAttempt = DateTime.UtcNow - _lastReconnectAttempt;
            if (timeSinceLastAttempt < TimeSpan.FromSeconds(5))
            {
                _logger.LogDebug("重连冷却期内，跳过重连尝试");
                return false;
            }

            _isReconnecting = true;
            _lastReconnectAttempt = DateTime.UtcNow;
            _currentReconnectAttempt = 0;

            _logger.LogInformation("开始重连流程，原因: {Reason}", reason);

            var maxReconnectAttempts = _options.MaxReconnectAttempts;
            var reconnected = false;

            for (int attempt = 0; attempt < maxReconnectAttempts && !reconnected && !cancellationToken.IsCancellationRequested; attempt++)
            {
                _currentReconnectAttempt = attempt + 1;

                if (_options.EnableLogging)
                    _logger.LogInformation("重连尝试 {Attempt}/{MaxAttempts}...", _currentReconnectAttempt, maxReconnectAttempts);

                reconnected = await TryReconnectWithBackoffAsync(attempt, cancellationToken);

                if (reconnected)
                {
                    _logger.LogInformation("重连成功 (尝试次数: {Attempt})", _currentReconnectAttempt);
                    break;
                }
            }

            if (!reconnected && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogError("重连失败，已达到最大重连次数 {MaxAttempts}", maxReconnectAttempts);
            }

            return reconnected;
        }
        finally
        {
            _isReconnecting = false;
            _reconnectLock.Release();
        }
    }

    /// <summary>
    /// 使用指数退避策略尝试重连
    /// </summary>
    /// <param name="attempt">当前尝试次数（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否重连成功</returns>
    private async Task<bool> TryReconnectWithBackoffAsync(int attempt, CancellationToken cancellationToken)
    {
        // 指数退避：delay = baseDelay * (2^attempt)，最大不超过配置的MaxReconnectDelayMs
        var baseDelay = TimeSpan.FromMilliseconds(_options.ReconnectDelayMs);
        var exponentialDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
        var maxDelay = TimeSpan.FromMilliseconds(_options.MaxReconnectDelayMs);
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
                    // 使用配置的健康检查间隔检查连接状态
                    await Task.Delay(TimeSpan.FromMilliseconds(_options.HealthCheckIntervalMs), stoppingToken);

                    // 如果连接断开且启用自动重连，使用统一重连方法
                    if (!_webSocketManager.IsConnected)
                    {
                        await TryReconnectAsync("健康检查发现连接断开", stoppingToken);
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
        
        // 心跳管理已由 FeishuWebSocketClient 内部统一处理，无需在此重复启动
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

        // 心跳管理已由 FeishuWebSocketClient 内部统一处理，无需在此停止
        
        // 使用统一重连方法，避免重复重连逻辑
        _ = Task.Run(async () =>
        {
            try
            {
                await TryReconnectAsync("连接断开事件触发", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "断线重连时发生错误");
            }
        });
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
    private void Dispose(bool disposing)
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

                // 释放重连锁
                _reconnectLock?.Dispose();

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