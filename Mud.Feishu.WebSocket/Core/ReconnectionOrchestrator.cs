// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Mud.Feishu.WebSocket.Core;

/// <summary>
/// 统一重连协调器实现
/// </summary>
public class ReconnectionOrchestrator : IReconnectionOrchestrator, IDisposable
{
    private readonly ILogger<ReconnectionOrchestrator> _logger;
    private readonly IReconnectStrategy _strategy;
    private readonly IFeishuWebSocketManager _webSocketManager;
    private readonly FeishuWebSocketOptions _options;

    private readonly SemaphoreSlim _reconnectLock = new(1, 1);
    private bool _isReconnecting;
    private int _currentAttempt;
    private int _totalReconnectCount;
    private DateTime _lastReconnectAttempt = DateTime.MinValue;
    private DateTime? _reconnectStartTime;
    private string? _lastReconnectReason;
    private Exception? _lastError;

    private bool _disposed;

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
                        _totalReconnectCount++;
                        _logger.LogInformation("重连成功 (尝试次数: {Attempt}, 总次数: {Total})",
                            _currentAttempt, _totalReconnectCount);

                        OnReconnectSucceeded(_currentAttempt);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _lastError = ex;
                    _logger.LogWarning(ex, "第 {Attempt} 次重连尝试失败", _currentAttempt);
                }
            }

            if (!reconnected && !cancellationToken.IsCancellationRequested)
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
    public void ResetReconnectCounter()
    {
        _currentAttempt = 0;
        _reconnectStartTime = null;
        _lastError = null;
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
            TotalReconnectCount = _totalReconnectCount,
            LastReconnectAttempt = _lastReconnectAttempt,
            ReconnectStartTime = _reconnectStartTime,
            LastReconnectReason = _lastReconnectReason,
            LastError = _lastError
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
}
