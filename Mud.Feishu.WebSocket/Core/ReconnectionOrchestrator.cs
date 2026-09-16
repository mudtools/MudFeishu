// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 统一重连协调器实现
/// </summary>
public class ReconnectionOrchestrator : IReconnectionOrchestrator, IAsyncDisposable, IDisposable
{
    private readonly ILogger<ReconnectionOrchestrator> _logger;
    private readonly IReconnectStrategy _strategy;
    private readonly IFeishuWebSocketManager _webSocketManager;
    private readonly FeishuWebSocketOptions _options;

    private readonly SemaphoreSlim _reconnectLock = new(1, 1);
    // P2-6 修复：该字段被重连线程写、被监控/查询线程读，必须保证可见性
    private volatile bool _isReconnecting;
    private int _currentAttempt;
    private int _totalReconnectCount;
    private DateTime _lastReconnectAttempt = DateTime.MinValue;
    private DateTime? _reconnectStartTime;
    private string? _lastReconnectReason;
    private Exception? _lastError;

    private bool _disposed;

    // F3 修复：重连熔断标志。达到重连上限后打开，阻止后续健康检查触发无效重连。
    // 仅在连接成功后由 ResetReconnectCounter 清除。
    private volatile bool _circuitOpen;

    /// <summary>
    /// 重连成功事件
    /// </summary>
    public event EventHandler<ReconnectSuccessEventArgs>? ReconnectSucceeded;

    /// <summary>
    /// 重连失败事件
    /// </summary>
    public event EventHandler<ReconnectFailedEventArgs>? ReconnectFailed;

    /// <summary>
    /// 达到重连限制事件
    /// </summary>
    public event EventHandler<ReconnectLimitReachedEventArgs>? ReconnectLimitReached;

    /// <summary>
    /// 初始化重连协调器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="strategy">重连策略</param>
    /// <param name="webSocketManager">WebSocket管理器</param>
    /// <param name="options">WebSocket配置选项</param>
    public ReconnectionOrchestrator(
        ILogger<ReconnectionOrchestrator> logger,
        IReconnectStrategy strategy,
        IFeishuWebSocketManager webSocketManager,
        FeishuWebSocketOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _webSocketManager = webSocketManager ?? throw new ArgumentNullException(nameof(webSocketManager));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// 尝试重连（核心方法）
    /// </summary>
    /// <param name="reason">重连原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重连是否成功</returns>
    public async Task<bool> TryReconnectAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (!_options.AutoReconnect)
        {
            _logger.LogInformation("自动重连已禁用，跳过重连");
            return false;
        }

        // F3：熔断器打开时拒绝重连，避免对飞书侧造成持续连接压力
        if (_circuitOpen)
        {
            _logger.LogDebug("重连熔断器已打开，跳过重连尝试（需等待连接成功后清除）");
            return false;
        }

        await _reconnectLock.WaitAsync(cancellationToken);
        try
        {
            if (_isReconnecting)
            {
                _logger.LogDebug("重连已在进行中，跳过重复重连请求");
                return false;
            }

            var timeSinceLastAttempt = DateTime.UtcNow - _lastReconnectAttempt;
            if (timeSinceLastAttempt < _options.ReconnectCooldownTime)
            {
                _logger.LogDebug("重连冷却期内，跳过重连尝试");
                return false;
            }

            _isReconnecting = true;
            _reconnectStartTime = DateTime.UtcNow;
            _lastReconnectAttempt = DateTime.UtcNow;
            _lastReconnectReason = reason;
            _currentAttempt = 0;
            // P2-12 修复：标记是否已达重连上限，避免同一轮重连同时触发
            // ReconnectLimitReached 与 ReconnectFailed，导致上层重复记录失败指标。
            var limitReached = false;

            _logger.LogInformation("开始重连流程，原因: {Reason}", reason);

            var reconnected = false;
            while (!reconnected && !cancellationToken.IsCancellationRequested)
            {
                _currentAttempt++;

                var elapsedTime = DateTime.UtcNow - _reconnectStartTime.Value;
                if (!_strategy.ShouldContinueReconnect(_currentAttempt, elapsedTime))
                {
                    _logger.LogError("已达到重连限制 (次数: {Attempt}, 时间: {ElapsedTime})",
                        _currentAttempt, elapsedTime);

                    limitReached = true;
                    // F3：达到重连上限后打开熔断器
                    _circuitOpen = true;
                    _logger.LogWarning("重连熔断器已打开：已达到重连上限（次数: {Attempt}, 时间: {ElapsedTime}），" +
                        "熔断期间健康检查不会触发重连，直到连接成功后自动清除", _currentAttempt, elapsedTime);
                    OnReconnectLimitReached(_currentAttempt, elapsedTime);
                    break;
                }

                var delay = _strategy.CalculateDelay(_currentAttempt);
                _logger.LogInformation("等待 {Delay}毫秒后进行第 {Attempt} 次重连尝试",
                    delay.TotalMilliseconds, _currentAttempt);
                await Task.Delay(delay, cancellationToken);

                try
                {
                    await _webSocketManager.ReconnectAsync(cancellationToken);
                    reconnected = _webSocketManager.IsConnected;

                    if (reconnected)
                    {
                        if (_options.EnableReconnectMetrics)
                            _totalReconnectCount++;
                        var attemptCount = _currentAttempt;
                        _currentAttempt = 0;
                        _reconnectStartTime = null;
                        _lastError = null;

                        // 仅通过事件通知上层（FeishuWebSocketHostedService 记录日志），避免重复打印
                        OnReconnectSucceeded(attemptCount);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _lastError = ex;
                    _logger.LogWarning(ex, "第 {Attempt} 次重连尝试失败", _currentAttempt);
                }
            }

            if (!reconnected && !limitReached && !cancellationToken.IsCancellationRequested)
            {
                OnReconnectFailed(_currentAttempt, _lastError);
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
    /// 重置重连计数器（在连接成功建立时调用）
    /// </summary>
    /// <remarks>
    /// F3 修复：同时清除熔断标志，恢复正常重连能力。
    /// </remarks>
    public void ResetReconnectCounter()
    {
        _currentAttempt = 0;
        _reconnectStartTime = null;
        _lastError = null;

        // F3：连接成功，清除熔断标志
        if (_circuitOpen)
        {
            _circuitOpen = false;
            _logger.LogInformation("重连熔断器已关闭（连接成功建立）");
        }

        _logger.LogDebug("重连计数器已重置");
    }

    /// <summary>
    /// 获取当前重连状态
    /// </summary>
    /// <returns>重连状态信息</returns>
    public ReconnectState GetReconnectState()
    {
        return new ReconnectState
        {
            IsReconnecting = _isReconnecting,
            CurrentAttempt = _currentAttempt,
            TotalReconnectCount = _options.EnableReconnectMetrics ? _totalReconnectCount : 0,
            LastReconnectAttempt = _lastReconnectAttempt,
            ReconnectStartTime = _reconnectStartTime,
            LastReconnectReason = _lastReconnectReason,
            LastError = _lastError,
            IsCircuitOpen = _circuitOpen
        };
    }

    private void OnReconnectSucceeded(int attemptCount)
    {
        ReconnectSucceeded?.Invoke(this, new ReconnectSuccessEventArgs
        {
            AttemptCount = attemptCount,
            TotalReconnectCount = _totalReconnectCount,
            Timestamp = DateTime.UtcNow
        });
    }

    private void OnReconnectFailed(int attemptCount, Exception? error)
    {
        ReconnectFailed?.Invoke(this, new ReconnectFailedEventArgs
        {
            AttemptCount = attemptCount,
            Error = error,
            Timestamp = DateTime.UtcNow
        });
    }

    private void OnReconnectLimitReached(int totalAttempts, TimeSpan elapsedTime)
    {
        ReconnectLimitReached?.Invoke(this, new ReconnectLimitReachedEventArgs
        {
            TotalAttempts = totalAttempts,
            TotalElapsedTime = elapsedTime,
            LastError = _lastError,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _reconnectLock.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// WS-29 修复（P2-19）：异步释放资源，等待在途重连任务完成后再释放锁。
    /// </summary>
    /// <remarks>
    /// 此前同步 <see cref="Dispose()"/> 直接释放 <c>_reconnectLock</c>，
    /// 若重连任务正在持锁执行，<c>SemaphoreSlim.Dispose</c> 会抛异常或死锁。
    /// 异步路径等待最多 5 秒后释放，确保在途重连安全退出。
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        // 等待在途重连完成（最多 5 秒）
        try
        {
            await _reconnectLock.WaitAsync(TimeSpan.FromSeconds(5));
            _reconnectLock.Release();
        }
        catch
        {
            // 超时或已释放，忽略
        }

        _reconnectLock.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
