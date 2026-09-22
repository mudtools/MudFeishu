// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

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
    /// <summary>
    /// 重连闸门（P0-1 / I11 / I12）：0 = 空闲，1 = 重连中。
    /// </summary>
    /// <remarks>
    /// 该字段是"是否正在重连"的<b>唯一真源</b>（替代此前的 <c>_isReconnecting</c> 布尔字段），
    /// 同时承担"重复请求快速失败"的职责：<b>不允许</b>先获取 <see cref="_reconnectLock"/> 再判断，
    /// 否则订阅者在事件回调内重入 <see cref="TryReconnectAsync"/> 时会永久挂起（锁不可重入）。
    /// <para>所有路径（含提前 return、事件回调抛异常）都必须在最外层 finally 复位闸门，见 I11。</para>
    /// </remarks>
    private int _reconnectGate;

    // ────────────────────────────────────────────────────────────────────────
    // WS2-12：状态字段改为无锁原子访问（Interlocked / Volatile）。
    //
    // 必要性：这些字段的**写点分布在两处不同同步域**——
    //   ① 重连主循环：持有 _reconnectLock；
    //   ② ResetReconnectCounter()：由 Connected 事件回调触发，**不能**取 _reconnectLock
    //      （该回调在 ReconnectAsync → StartAsync → ConnectAsync 的调用链内触发，而调用链的
    //        外层正持有 _reconnectLock ⇒ 取锁必然自死锁）。
    // 因此"用一个锁保护所有字段"不可行，只能保证**每个字段自身的读写原子性**。
    //
    // 说明：64 位值（DateTime/DateTime?）统一以 Ticks（long）存储并用 Interlocked.Read 读取，
    // 避免 32 位平台上的撕裂读；引用类型用 Volatile。读侧允许看到略陈旧的值——
    // GetReconnectState() 是观测接口，不做跨字段一致性保证（跨字段快照需要锁，代价不值）。
    // ────────────────────────────────────────────────────────────────────────
    private int _currentAttempt;
    private int _totalReconnectCount;

    /// <summary>最近一次重连尝试时刻（<see cref="DateTime.Ticks"/>；0 = <see cref="DateTime.MinValue"/>）。</summary>
    private long _lastReconnectAttemptTicks;

    /// <summary>本轮重连起始时刻（<see cref="DateTime.Ticks"/>；0 = null = 当前无在途轮次）。</summary>
    private long _reconnectStartTimeTicks;

    private string? _lastReconnectReason;
    private Exception? _lastError;

    // WS-15 范式统一：_disposed 改为 int + Interlocked.Exchange 实现原子 check-then-set
    private int _disposed = 0;

    // F3 修复：重连熔断标志。达到重连上限后打开，阻止后续健康检查触发无效重连。
    // 仅在连接成功后由 ResetReconnectCounter 清除。
    private volatile bool _circuitOpen;

    /// <summary>
    /// 重连成功事件
    /// </summary>
    /// <remarks>
    /// P0-1 修复：事件在<b>锁外</b>派发，但在重连闸门持有期内派发。
    /// <para>
    /// 订阅者约定：<b>严禁在回调内同步阻塞等待</b>（如需耗时操作请自行 <c>Task.Run</c>，
    /// 或改由 <see cref="ReconnectFailed"/> / <see cref="ReconnectLimitReached"/> 触发异步补偿）。
    /// 在回调内重入 <see cref="TryReconnectAsync"/> 会立即返回 <c>false</c>（不阻塞、不开启嵌套轮次）。
    /// </para>
    /// </remarks>
    public event EventHandler<ReconnectSuccessEventArgs>? ReconnectSucceeded;

    /// <summary>
    /// 重连失败事件
    /// </summary>
    /// <inheritdoc cref="ReconnectSucceeded" path="/remarks"/>
    public event EventHandler<ReconnectFailedEventArgs>? ReconnectFailed;

    /// <summary>
    /// 达到重连限制事件
    /// </summary>
    /// <inheritdoc cref="ReconnectSucceeded" path="/remarks"/>
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
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("反射式System.Text.Json序列化在裁剪下无法静态分析目标类型成员")]
#endif
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("反射式System.Text.Json序列化在 AOT/动态代码生成环境下不可用")]
#endif
    public async Task<bool> TryReconnectAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _disposed) == 1)
        {
            _logger.LogDebug("重连协调器已释放，跳过重连");
            return false;
        }

        if (!_options.Reconnect.Auto)
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

        // P0-1 / I1 / I3 / I11：闸门在锁外，重复请求立即返回，不排队等锁。
        // 此前用 `await _reconnectLock.WaitAsync()` 后再判断 _isReconnecting，
        // 导致订阅者在事件回调内重入时永久挂起（SemaphoreSlim 不可重入）且 finally 永不执行。
        if (Interlocked.CompareExchange(ref _reconnectGate, 1, 0) != 0)
        {
            _logger.LogDebug("重连已在进行中，跳过重复重连请求");
            return false;
        }

        // P0-1：所有"结果"只写局部变量，事件统一在锁外派发（原 :144/:168/:181 在锁内触发）
        var reconnected = false;
        var limitReached = false;
        var attemptCount = 0;
        var limitElapsed = TimeSpan.Zero;
        Exception? lastError = null;

        try
        {
            await _reconnectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var timeSinceLastAttempt = DateTime.UtcNow - ReadLastReconnectAttempt();
                if (timeSinceLastAttempt < _options.Reconnect.Cooldown)
                {
                    _logger.LogDebug("重连冷却期内，跳过重连尝试");
                    return false;
                }

                var reconnectStart = DateTime.UtcNow;
                Interlocked.Exchange(ref _reconnectStartTimeTicks, reconnectStart.Ticks);
                Interlocked.Exchange(ref _lastReconnectAttemptTicks, reconnectStart.Ticks);
                Volatile.Write(ref _lastReconnectReason, reason);
                Interlocked.Exchange(ref _currentAttempt, 0);

                _logger.LogInformation("开始重连流程，原因: {Reason}", reason);

                while (!reconnected && !cancellationToken.IsCancellationRequested)
                {
                    var currentAttempt = Interlocked.Increment(ref _currentAttempt);
                    attemptCount = currentAttempt;

                    var elapsedTime = DateTime.UtcNow - reconnectStart;
                    if (!_strategy.ShouldContinueReconnect(currentAttempt, elapsedTime))
                    {
                        _logger.LogError("已达到重连限制 (次数: {Attempt}, 时间: {ElapsedTime})",
                            currentAttempt, elapsedTime);

                        // P2-12 修复：标记已达上限，避免同一轮同时触发
                        // ReconnectLimitReached 与 ReconnectFailed，导致上层重复记录失败指标。
                        limitReached = true;
                        limitElapsed = elapsedTime;
                        // F3：达到重连上限后打开熔断器
                        _circuitOpen = true;
                        _logger.LogWarning("重连熔断器已打开：已达到重连上限（次数: {Attempt}, 时间: {ElapsedTime}），" +
                            "熔断期间健康检查不会触发重连，直到连接成功后自动清除", currentAttempt, elapsedTime);
                        break;
                    }

                    try
                    {
                        // W4-P2-6：延迟计算与等待一并纳入 try，避免策略实现抛异常时异常穿透整轮重连。
                        var delay = _strategy.CalculateDelay(currentAttempt);
                        _logger.LogInformation("等待 {Delay}毫秒后进行第 {Attempt} 次重连尝试",
                            delay.TotalMilliseconds, currentAttempt);
                        await Task.Delay(delay, cancellationToken);

                        await _webSocketManager.ReconnectAsync(cancellationToken);
                        reconnected = _webSocketManager.IsConnected;

                        if (reconnected)
                        {
                            if (_options.EnableReconnectMetrics)
                                Interlocked.Increment(ref _totalReconnectCount);
                            attemptCount = currentAttempt;
                            Interlocked.Exchange(ref _currentAttempt, 0);
                            Interlocked.Exchange(ref _reconnectStartTimeTicks, 0);
                            Volatile.Write(ref _lastError, null);
                            lastError = null;
                            break;
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        // 外部取消（关停/超时窗口）：保持与改造前一致的语义——向上抛出而不吞掉
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Volatile.Write(ref _lastError, ex);
                        lastError = ex;
                        _logger.LogWarning(ex, "第 {Attempt} 次重连尝试失败", currentAttempt);
                    }
                }
            }
            finally
            {
                _reconnectLock.Release();
            }

            // P0-1 / I1：锁外派发事件；闸门仍被持有（保证"任意时刻至多一轮重连在途"，
            // 订阅者重入将快速返回 false，而不是开启嵌套重连轮次）。
            if (limitReached)
            {
                OnReconnectLimitReached(attemptCount, limitElapsed);
            }
            else if (reconnected)
            {
                OnReconnectSucceeded(attemptCount);
            }
            else if (!cancellationToken.IsCancellationRequested)
            {
                OnReconnectFailed(attemptCount, lastError);
            }

            return reconnected;
        }
        finally
        {
            // I11：任何路径（含提前 return 与事件回调抛异常）都必须复位闸门，防止闸门泄漏后重连被永久拒绝
            Volatile.Write(ref _reconnectGate, 0);
        }
    }

    /// <summary>
    /// 重置重连计数器（在连接成功建立时调用）
    /// </summary>
    /// <remarks>
    /// F3 修复：同时清除熔断标志，恢复正常重连能力。
    /// <para>
    /// WS2-12：本方法由 <c>Connected</c> 事件回调触发，且在
    /// <c>ReconnectAsync → StartAsync → ConnectAsync</c> 调用链内——此时外层正持有
    /// <c>_reconnectLock</c>，因此**禁止**改为"取锁后统一更新字段"（必然自死锁）。
    /// 字段更新一律使用 <see cref="Interlocked"/>/<see cref="Volatile"/> 保证单字段原子性。
    /// </para>
    /// </remarks>
    public void ResetReconnectCounter()
    {
        Interlocked.Exchange(ref _currentAttempt, 0);
        Interlocked.Exchange(ref _reconnectStartTimeTicks, 0);
        Volatile.Write(ref _lastError, null);

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
    /// <remarks>
    /// WS2-12：各字段以原子读取值。本方法为**观测接口**，不保证跨字段一致性快照
    /// （跨字段一致性需要取锁，而写侧存在无法取锁的路径，故该保证不可达）。
    /// </remarks>
    public ReconnectState GetReconnectState()
    {
        return new ReconnectState
        {
            // I12：单一真源——"是否正在重连"直接由闸门派生（不再维护第二个布尔字段）
            IsReconnecting = Volatile.Read(ref _reconnectGate) == 1,
            CurrentAttempt = Volatile.Read(ref _currentAttempt),
            TotalReconnectCount = _options.EnableReconnectMetrics ? Volatile.Read(ref _totalReconnectCount) : 0,
            LastReconnectAttempt = ReadLastReconnectAttempt(),
            ReconnectStartTime = ReadReconnectStartTime(),
            LastReconnectReason = Volatile.Read(ref _lastReconnectReason),
            LastError = Volatile.Read(ref _lastError),
            IsCircuitOpen = _circuitOpen
        };
    }

    /// <summary>
    /// 读取"最近一次重连尝试时刻"（Ticks=0 映射为 <see cref="DateTime.MinValue"/>）。
    /// </summary>
    private DateTime ReadLastReconnectAttempt()
    {
        var ticks = Interlocked.Read(ref _lastReconnectAttemptTicks);
        return ticks == 0 ? DateTime.MinValue : new DateTime(ticks, DateTimeKind.Utc);
    }

    /// <summary>
    /// 读取"本轮重连起始时刻"（Ticks=0 表示当前无在途轮次）。
    /// </summary>
    private DateTime? ReadReconnectStartTime()
    {
        var ticks = Interlocked.Read(ref _reconnectStartTimeTicks);
        return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
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
    /// <remarks>
    /// P1-5（I9）：<b>不再释放</b> <c>_reconnectLock</c>。本类型从不访问
    /// <see cref="SemaphoreSlim.AvailableWaitHandle"/>，不释放不会产生任何 OS 句柄泄漏；
    /// 而释放会与在途 <c>WaitAsync</c>/<c>Release</c> 构成 <see cref="ObjectDisposedException"/> 竞态。
    /// </remarks>
    public void Dispose()
    {
        // WS-15 范式统一：原子 check-then-set
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// WS-29 修复（P2-19）+ P1-5b：异步释放资源，尽力等待在途重连任务退出。
    /// </summary>
    /// <remarks>
    /// 此前同步 <see cref="Dispose()"/> 直接释放 <c>_reconnectLock</c>，
    /// 若重连任务正在持锁执行，<c>SemaphoreSlim.Dispose</c> 会抛异常或死锁。
    /// 现按 I9 不再释放该信号量：等待仅用于让调用方获得"在途重连已收尾"的可观测性。
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        // 等待在途重连完成（最多 5 秒）；无论成功与否都不释放信号量
        try
        {
            if (await _reconnectLock.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false))
            {
                _reconnectLock.Release();
            }
        }
        catch
        {
            // 超时或已释放，忽略
        }

        GC.SuppressFinalize(this);
    }
}
