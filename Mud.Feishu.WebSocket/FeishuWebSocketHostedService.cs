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
using System.Diagnostics.CodeAnalysis;
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
    private readonly FeishuWebSocketConcurrencyService? _concurrencyService;
    // NEW-WS-01 修复：保存 host stoppingToken 用于链接重连任务的取消令牌，支持优雅关闭
    private CancellationToken _stoppingToken;
    private bool _disposed;
    private DateTime _lastReconnectTriggerTime = DateTime.MinValue;
    private readonly object _reconnectDebounceLock = new();
    private static readonly TimeSpan ReconnectDebounceInterval = TimeSpan.FromSeconds(3);
    /// <summary>
    /// WebSocket 指标源注销令牌（P2-3 修复）。
    /// </summary>
    private IDisposable? _metricsRegistration;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="webSocketManager">WebSocket管理器</param>
    /// <param name="reconnectionOrchestrator">重连协调器</param>
    /// <param name="options">WebSocket配置选项监控器（支持热更新）</param>
    /// <param name="concurrencyService">并发控制服务（可选背压闸门；为 null 时不启用并发上界）</param>
    public FeishuWebSocketHostedService(
        ILogger<FeishuWebSocketHostedService> logger,
        IFeishuWebSocketManager webSocketManager,
        IReconnectionOrchestrator reconnectionOrchestrator,
        IOptionsMonitor<FeishuWebSocketOptions> options,
        FeishuWebSocketConcurrencyService? concurrencyService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
        _reconnectionOrchestrator = reconnectionOrchestrator ?? throw new ArgumentNullException(nameof(reconnectionOrchestrator));
        _optionsMonitor = options ?? throw new ArgumentNullException(nameof(options));
        _concurrencyService = concurrencyService;

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
    /// 注册 WebSocket 指标源（P2-3 修复：按注册实例登记，多应用互不覆盖，Dispose 时注销）。
    /// </summary>
    /// <remarks>
    /// 此前直接给 <c>FeishuMetrics.WebSocketConnectionObserver</c> / <c>WebSocketBacklogObserver</c>
    /// 这两个<b>静态可写属性</b>赋值：同进程多应用互相覆盖，且静态属性长期持有服务实例（不可回收）。
    /// <para>
    /// WS-17（P1-13）：连接数取本实例的 <c>IsConnected</c>（1 或 0）；
    /// F1：积压数取并发闸门的在途处理数。
    /// AppKey 由提供器在<b>每次采集时</b>读取，支持配置热更新。
    /// </para>
    /// </remarks>
    private void InitializeMetricsObservers()
    {
        var appKey = _optionsMonitor.CurrentValue.AppKey;

        _metricsRegistration = FeishuMetrics.RegisterWebSocketMetricsSource(
            appKeyProvider: () => _optionsMonitor.CurrentValue.AppKey,
            activeConnectionsProvider: () => _webSocketManager.IsConnected ? 1 : 0,
            pendingMessagesProvider: () => _concurrencyService?.PendingCount ?? 0);

        _logger.LogDebug("WebSocket 指标源已注册，AppKey: {AppKey}", appKey);
    }

    /// <summary>
    /// 执行后台服务
    /// </summary>
    /// <param name="stoppingToken">停止令牌</param>
    /// <returns>执行任务</returns>
    // 说明：BackgroundService.ExecuteAsync 基方法未携带 Requires 标注，override 无法添加
    // RequiresUnreferencedCode/RequiresDynamicCode（否则触发 IL2046/IL3051）。
    // 其内部调用的 StartAsync（带标注）的 IL 警告在此处统一用 UnconditionalSuppressMessage 屏蔽。
#if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "BackgroundService 基方法不支持 Requires 标注，反射式调用已由 WebSocket 客户端内部处理")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "BackgroundService 基方法不支持 Requires 标注，反射式调用已由 WebSocket 客户端内部处理")]
#endif
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // NEW-WS-01 修复：保存 stoppingToken 供 TryTriggerReconnect 中的 fire-and-forget 重连任务使用
        _stoppingToken = stoppingToken;
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
            try
            {
                // P1-11 修复：stoppingToken 在 finally 中必然已取消，
                // 直接传给 StopAsync 会让 _startStopLock.WaitAsync(已取消token) 立即抛
                // OperationCanceledException，导致关停流程异常收尾且 _isRunning 无法复位。
                // 这里使用独立的宽限令牌。
                using var graceCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _webSocketManager.StopAsync(graceCts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "停止WebSocket服务时发生异常，已强制结束");
            }
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

        // P2-16 修复：基类停止流程沿用调用方令牌（用于取消 ExecuteAsync 的等待），
        // 但底层连接的关闭必须使用<b>独立宽限令牌</b>——宿主常以"已取消的令牌"调用本方法，
        // 直接透传会让 FeishuWebSocketManager.StopAsync 内的信号量等待立即抛 OperationCanceledException，
        // 连接无法完成关闭握手（与服务端记录为异常断线）。
        try
        {
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("基类停止流程被取消，继续执行连接关闭（可忽略）");
        }

        using var graceCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await _webSocketManager.StopAsync(graceCts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "停止WebSocket连接时发生异常，已强制结束");
        }

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
                var stats = _webSocketManager.GetConnectionStats();
        _logger.LogInformation("飞书WebSocket连接已断开: {Status} - {Description} (持续时间: {Duration})",
            e.CloseStatus, e.CloseStatusDescription, stats.Uptime);
    

        // OnDisconnected 作为事件处理器无法添加 Requires 标注，其内部调用带标注的
        // TryTriggerReconnect 时用 pragma 屏蔽 IL 警告。
#pragma warning disable IL2026, IL3050
        TryTriggerReconnect("连接断开事件触发");
#pragma warning restore IL2026, IL3050
    }

    /// <summary>
    /// 尝试触发重连（带防抖机制）
    /// </summary>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
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
                // NEW-WS-01 修复：链接 host stoppingToken，支持优雅关闭
                // P1-12 修复：重连窗口此前被硬编码为 5 分钟，会让
                // FeishuWebSocketOptions.Reconnect.TotalBudget（默认 30 分钟）永远无法生效。
                // 现在以配置值为准（+1 分钟余量用于收尾）。
                var window = _optionsMonitor.CurrentValue.Reconnect.TotalBudget;
                if (window <= TimeSpan.Zero)
                {
                    window = TimeSpan.FromMinutes(30);
                }
                using var timeoutCts = new CancellationTokenSource(window + TimeSpan.FromMinutes(1));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken, timeoutCts.Token);
                await _reconnectionOrchestrator.TryReconnectAsync(reason, linkedCts.Token);
            }
            catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
            {
                // 应用关闭导致的取消，正常退出
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "断线重连时发生错误");
            }
        }, _stoppingToken);  // 也传入 stoppingToken 作为任务取消令牌
    }

    /// <summary>
    /// WebSocket错误事件处理
    /// </summary>
    private void OnError(object? sender, WebSocketErrorEventArgs e)
    {
        // 可恢复错误已在下层组件以 Warning 级别记录，此处仅在 Debug 级别记录避免重复刷屏。
        // 不可恢复错误仍以 Error 级别记录完整异常。

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
    /// 获取并发控制服务实例（供健康检查读取并发指标）。
    /// </summary>
    /// <returns>并发控制服务，未注入时返回 null</returns>
    internal FeishuWebSocketConcurrencyService? GetConcurrencyService() => _concurrencyService;

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

                // P2-3 修复：注销指标源，避免观测结果残留已释放的实例（此前静态属性无法回收服务实例）
                _metricsRegistration?.Dispose();
                _metricsRegistration = null;

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
