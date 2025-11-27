
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
public class FeishuWebSocketHostedService : BackgroundService
{
    private readonly ILogger<FeishuWebSocketHostedService> _logger;
    private readonly IFeishuWebSocketManager _webSocketManager;
    private readonly FeishuWebSocketOptions _options;

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

                        var reconnectAttempts = 0;
                        var maxReconnectAttempts = _options.MaxReconnectAttempts;

                        while (reconnectAttempts < maxReconnectAttempts && !_webSocketManager.IsConnected)
                        {
                            reconnectAttempts++;
                            if (_options.EnableLogging)
                                _logger.LogInformation("重连尝试 ({Attempt}/{MaxAttempts})...", reconnectAttempts, maxReconnectAttempts);

                            try
                            {
                                await _webSocketManager.ReconnectAsync(stoppingToken);
                                if (_webSocketManager.IsConnected)
                                {
                                    _logger.LogInformation("重连成功");
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (_options.EnableLogging)
                                    _logger.LogError(ex, "重连尝试 {Attempt} 失败", reconnectAttempts);
                                if (reconnectAttempts < maxReconnectAttempts)
                                {
                                    // 等待一段时间再进行下一次重连
                                    await Task.Delay(TimeSpan.FromMilliseconds(_options.ReconnectDelayMs), stoppingToken);
                                }
                            }
                        }

                        if (!_webSocketManager.IsConnected)
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
        _logger.LogInformation("飞书WebSocket连接已建立");
    }

    /// <summary>
    /// WebSocket连接断开事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnDisconnected(object? sender, WebSocketCloseEventArgs e)
    {
        if (_options.EnableLogging)
            _logger.LogInformation("飞书WebSocket连接已断开: {Status} - {Description}",
                e.CloseStatus, e.CloseStatusDescription);
    }

    /// <summary>
    /// WebSocket错误事件处理
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数</param>
    private void OnError(object? sender, WebSocketErrorEventArgs e)
    {
        if (_options.EnableLogging)
            _logger.LogError(e.Exception, "飞书WebSocket发生错误: {Message}", e.ErrorMessage);
    }
}
