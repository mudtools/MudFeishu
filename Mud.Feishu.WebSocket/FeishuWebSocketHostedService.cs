// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Diagnostics.Metrics;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书WebSocket后台服务，用于自动启动和管理WebSocket连接
/// </summary>
public sealed class FeishuWebSocketHostedService : BackgroundService, IDisposable
{
    private readonly ILogger<FeishuWebSocketHostedService> _logger;
    private readonly IFeishuWebSocketManager _webSocketManager;
    private readonly IReconnectionOrchestrator _reconnectionOrchestrator;
    private readonly IOptionsMonitor<FeishuWebSocketOptions> _optionsMonitor;
    private bool _disposed;
    private DateTime _lastReconnectTriggerTime = DateTime.MinValue;
    private readonly object _reconnectDebounceLock = new();
    private static readonly TimeSpan ReconnectDebounceInterval = TimeSpan.FromSeconds(3);

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="webSocketManager">WebSocket管理器</param>
    /// <param name="reconnectionOrchestrator">重连协调器</param>
    /// <param name="options">WebSocket配置选项监控器（支持热更新）</param>
    public FeishuWebSocketHostedService(
        ILogger<FeishuWebSocketHostedService> logger,
        IFeishuWebSocketManager webSocketManager,
        IReconnectionOrchestrator reconnectionOrchestrator,
        IOptionsMonitor<FeishuWebSocketOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
        _reconnectionOrchestrator = reconnectionOrchestrator ?? throw new ArgumentNullException(nameof(reconnectionOrchestrator));
        _optionsMonitor = options ?? throw new ArgumentNullException(nameof(options));

        _webSocketManager.Connected += OnConnected;
        _webSocketManager.Disconnected += OnDisconnected;
        _webSocketManager.Error += OnError;

        _reconnectionOrchestrator.ReconnectSucceeded += OnReconnectSucceeded;
        _reconnectionOrchestrator.ReconnectFailed += OnReconnectFailed;
        _reconnectionOrchestrator.ReconnectLimitReached += OnReconnectLimitReached;

        // P1-5/P1-6 修复：在此处初始化 WebSocket 指标观察器，使用实际 AppKey 而非硬编码 "websocket"。
        // 此时 DI 容器已就绪，可从 IOptionsMonitor 解析当前 FeishuWebSocketOptions.AppKey。
        // 观察器在每次指标采集时动态读取 _optionsMonitor.CurrentValue.AppKey，支持配置热更新。
        InitializeMetricsObservers();
    }

    /// <summary>
    /// 初始化 WebSocket 指标观察器，使用实际 AppKey 作为维度标签。
    /// </summary>
    private void InitializeMetricsObservers()
    {
        var appKey = _optionsMonitor.CurrentValue.AppKey;

        // WebSocket 活跃连接数观察器（按 app_key 分组）
        FeishuMetrics.WebSocketConnectionObserver = () =>
        {
            var currentAppKey = _optionsMonitor.CurrentValue.AppKey;
            return new[]
            {
                new Measurement<int>(
                    WebSocketConnectionManager.ConnectionCount,
                    new KeyValuePair<string, object?>(FeishuMetrics.Tags.AppKey, currentAppKey))
            };
        };

        // WebSocket 消息积压数观察器（当前架构同步处理消息无队列，积压始终为 0）
        // 预留接口供未来引入消息队列时填充实际积压数
        FeishuMetrics.WebSocketBacklogObserver = () =>
        {
            var currentAppKey = _optionsMonitor.CurrentValue.AppKey;
            return new[]
            {
                new Measurement<int>(
                    0,
                    new KeyValuePair<string, object?>(FeishuMetrics.Tags.AppKey, currentAppKey))
            };
        };

        _logger.LogDebug("WebSocket 指标观察器已初始化，AppKey: {AppKey}", appKey);
    }

    /// <summary>
    /// 执行后台服务
    /// </summary>
    /// <param name="stoppingToken">停止令牌</param>
    /// <returns>执行任务</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("飞书WebSocket后台服务正在启动...");

        // 初始连接重试：首次启动失败时进行有限次重试
        int initialRetryCount = 0;
        const int maxInitialRetries = 3;
        const int initialRetryDelayMs = 5000;

        while (!stoppingToken.IsCancellationRequested && initialRetryCount <= maxInitialRetries)
        {
            try
            {
                await _webSocketManager.StartAsync(stoppingToken);
                break; // 启动成功，跳出重试循环
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                initialRetryCount++;
                if (initialRetryCount > maxInitialRetries)
                {
                    _logger.LogError(ex, "飞书WebSocket服务初始连接失败，已达最大重试次数 ({MaxRetries})，进入健康检查模式", maxInitialRetries);
                    break;
                }

                _logger.LogWarning(ex, "飞书WebSocket服务初始连接失败 (第 {Attempt}/{MaxRetries} 次)，{DelayMs}ms 后重试...",
                    initialRetryCount, maxInitialRetries, initialRetryDelayMs);
                try
                {
                    await Task.Delay(initialRetryDelayMs, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(_optionsMonitor.CurrentValue.HealthCheckIntervalMs), stoppingToken);

                    if (!_webSocketManager.IsConnected)
                    {
                        TryTriggerReconnect("健康检查发现连接断开");
                    }
                }
                catch (TaskCanceledException)
                {
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
    private void OnConnected(object? sender, EventArgs e)
    {
        var state = _webSocketManager.GetConnectionState();
        _logger.LogInformation("飞书WebSocket连接已建立 (时间: {Time}, 重连次数: {ReconnectCount})",
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), state.ReconnectCount);

        _reconnectionOrchestrator.ResetReconnectCounter();
    }

    /// <summary>
    /// WebSocket连接断开事件处理
    /// </summary>
    private void OnDisconnected(object? sender, WebSocketCloseEventArgs e)
    {
        if (_optionsMonitor.CurrentValue.EnableLogging)
        {
            var stats = _webSocketManager.GetConnectionStats();
            _logger.LogInformation("飞书WebSocket连接已断开: {Status} - {Description} (持续时间: {Duration})",
                e.CloseStatus, e.CloseStatusDescription, stats.Uptime);
        }

        TryTriggerReconnect("连接断开事件触发");
    }

    /// <summary>
    /// 尝试触发重连（带防抖机制）
    /// </summary>
    private void TryTriggerReconnect(string reason)
    {
        lock (_reconnectDebounceLock)
        {
            var timeSinceLastTrigger = DateTime.UtcNow - _lastReconnectTriggerTime;
            if (timeSinceLastTrigger < ReconnectDebounceInterval)
            {
                _logger.LogDebug("重连防抖：距上次触发仅 {Elapsed}ms，跳过本次重连触发（原因: {Reason}）",
                    timeSinceLastTrigger.TotalMilliseconds, reason);
                return;
            }
            _lastReconnectTriggerTime = DateTime.UtcNow;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                await _reconnectionOrchestrator.TryReconnectAsync(reason, cts.Token);
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
    private void OnError(object? sender, WebSocketErrorEventArgs e)
    {
        // 可恢复错误已在下层组件以 Warning 级别记录，此处仅在 Debug 级别记录避免重复刷屏。
        // 不可恢复错误仍以 Error 级别记录完整异常。
        if (!_optionsMonitor.CurrentValue.EnableLogging)
            return;

        if (e.IsRecoverable)
            _logger.LogDebug("飞书WebSocket发生可恢复错误: {Message} (类型: {Type})", e.ErrorMessage, e.ErrorType);
        else
            _logger.LogError(e.Exception, "飞书WebSocket发生错误: {Message} (类型: {Type})", e.ErrorMessage, e.ErrorType);
    }

    /// <summary>
    /// 重连成功事件处理
    /// </summary>
    private void OnReconnectSucceeded(object? sender, ReconnectSuccessEventArgs e)
    {
        _logger.LogInformation("重连成功 (尝试次数: {Attempt}, 总次数: {Total})",
            e.AttemptCount, e.TotalReconnectCount);

        // P1-5 修复：记录 WebSocket 重连成功指标
        FeishuMetricsHelper.RecordWebSocketReconnect(_optionsMonitor.CurrentValue.AppKey, success: true);
    }

    /// <summary>
    /// 重连失败事件处理
    /// </summary>
    private void OnReconnectFailed(object? sender, ReconnectFailedEventArgs e)
    {
        _logger.LogError(e.Error, "重连失败 (尝试次数: {Attempt})", e.AttemptCount);

        // P1-5 修复：记录 WebSocket 重连失败指标
        FeishuMetricsHelper.RecordWebSocketReconnect(_optionsMonitor.CurrentValue.AppKey, success: false);
    }

    /// <summary>
    /// 达到重连限制事件处理
    /// </summary>
    private void OnReconnectLimitReached(object? sender, ReconnectLimitReachedEventArgs e)
    {
        _logger.LogError("已达到重连限制 (总尝试次数: {TotalAttempts}, 总时间: {ElapsedTime})",
            e.TotalAttempts, e.TotalElapsedTime);

        // P1-5 修复：达到重连上限视为最终失败，记录重连失败指标
        FeishuMetricsHelper.RecordWebSocketReconnect(_optionsMonitor.CurrentValue.AppKey, success: false);
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
                _webSocketManager.Connected -= OnConnected;
                _webSocketManager.Disconnected -= OnDisconnected;
                _webSocketManager.Error -= OnError;

                _reconnectionOrchestrator.ReconnectSucceeded -= OnReconnectSucceeded;
                _reconnectionOrchestrator.ReconnectFailed -= OnReconnectFailed;
                _reconnectionOrchestrator.ReconnectLimitReached -= OnReconnectLimitReached;

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
