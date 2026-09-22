// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书 Webhook 并发控制服务
/// 使用全局 SemaphoreSlim 控制事件处理并发数
/// </summary>
public class FeishuWebhookConcurrencyService : IAsyncDisposable, IHostedService
{
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor;
    private readonly ILogger<FeishuWebhookConcurrencyService> _logger;
    private readonly SemaphoreSlim _semaphoreLock = new(1, 1);
    private volatile SemaphoreSlim _semaphore;
    private bool _disposed;
    private volatile int _currentMaxConcurrentEvents;
    private volatile bool _semaphoreUpgraded = false;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly IDisposable? _onChangeSubscription;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FeishuWebhookConcurrencyService(
        IOptionsMonitor<FeishuWebhookOptions> optionsMonitor,
        ILogger<FeishuWebhookConcurrencyService> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;

        var options = _optionsMonitor.CurrentValue;
        // 处理并发限制值：0 或负数视为无限制
        _currentMaxConcurrentEvents = options.MaxConcurrentEvents;
        int actualMaxConcurrent = _currentMaxConcurrentEvents > 0 ? _currentMaxConcurrentEvents : int.MaxValue;
        _semaphore = new SemaphoreSlim(actualMaxConcurrent, actualMaxConcurrent);

        _logger.LogInformation("飞书 Webhook 并发控制服务初始化完成，最大并发数: {MaxConcurrentEvents} (实际: {ActualMaxConcurrent})",
            _currentMaxConcurrentEvents, actualMaxConcurrent);

        // 监听配置变更，支持热更新
        // WHF-13：消除 async-void——OnChange 回调必须是同步 void，异步体显式丢弃到线程池并
        // 全覆盖 try/catch（异常只记日志，不上抛线程池导致进程崩溃）；订阅句柄在 DisposeAsync 释放
        _onChangeSubscription = optionsMonitor.OnChange(newOptions =>
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await UpdateSemaphoreAsync(newOptions.MaxConcurrentEvents);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "并发控制配置热更新失败，最大并发数: {NewMax}", newOptions.MaxConcurrentEvents);
                }
            });
        });
    }

    /// <summary>
    /// HostedService 启动方法
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("飞书 Webhook 并发控制服务已启动");
        return Task.CompletedTask;
    }

    /// <summary>
    /// HostedService 停止方法
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("飞书 Webhook 并发控制服务正在停止...");

        // DI 容器的 DisposeAsync 可能先于 Host.StopAsync 执行（如 WebApplicationFactory
        // 及部分宿主的关闭顺序），此时 _shutdownCts 已被释放，直接 Cancel 会抛
        // ObjectDisposedException；关闭信号已由 DisposeAsync 中的 Cancel 先行发出，此处容错跳过。
        try
        {
            _shutdownCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        // 等待当前正在处理的请求完成（最多等待30秒）
        var timeout = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (_semaphore.CurrentCount < _currentMaxConcurrentEvents)
        {
            if (DateTime.UtcNow - startTime > timeout)
            {
                _logger.LogWarning("等待并发处理完成超时，当前等待的请求数: {WaitingCount}",
                    _currentMaxConcurrentEvents - _semaphore.CurrentCount);
                break;
            }

            await Task.Delay(100, cancellationToken);
        }

        _logger.LogInformation("飞书 Webhook 并发控制服务已停止");
    }

    /// <summary>
    /// 更新信号量配置
    /// </summary>
    private async Task UpdateSemaphoreAsync(int newMaxConcurrent)
    {
        // 使用信号量确保只有一个线程在更新配置
        await _semaphoreLock.WaitAsync();

        try
        {
            if (_disposed || newMaxConcurrent == _currentMaxConcurrentEvents)
                return;

            var oldMax = _currentMaxConcurrentEvents;
            _currentMaxConcurrentEvents = newMaxConcurrent;

            // 处理并发限制值：0 或负数视为无限制
            int actualMaxConcurrent = _currentMaxConcurrentEvents > 0 ? _currentMaxConcurrentEvents : int.MaxValue;

            _logger.LogInformation("并发控制配置已更新，最大并发数: {OldMax} -> {NewMax} (实际: {ActualMaxConcurrent})",
                oldMax, newMaxConcurrent, actualMaxConcurrent);

            // 原子替换信号量并延迟释放旧信号量（修复信号量泄漏）
            var oldSemaphore = Interlocked.Exchange(ref _semaphore,
                new SemaphoreSlim(actualMaxConcurrent, actualMaxConcurrent));
            var logMessage = _semaphoreUpgraded ? "信号量已重新创建" : "信号量首次创建";
            _semaphoreUpgraded = true;
            _logger.LogInformation("{Message}，最大并发数: {NewMax} (实际: {ActualMaxConcurrent})", logMessage, newMaxConcurrent, actualMaxConcurrent);

            // 延迟释放旧信号量，等待可能正在使用的请求完成
            // WHF-R2/C4：固定 60s 改为动态——至少 60s 或有效处理超时的 2 倍
            var effectiveTimeoutMs = _optionsMonitor.CurrentValue.EventHandlingTimeoutMs;
            var delayMs = (int)Math.Max(60_000, effectiveTimeoutMs * 2.0);
            _ = Task.Run(async () =>
            {
                await Task.Delay(delayMs);
                oldSemaphore.Dispose();
            });
        }
        finally
        {
            _semaphoreLock.Release();
        }
    }

    /// <summary>
    /// 获取信号量
    /// </summary>
    public SemaphoreSlim Semaphore => _semaphore;

    /// <summary>
    /// 异步等待获取信号量
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>信号量租约，使用完成后应释放</returns>
    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        // 组合应用关闭的取消令牌
        var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCts.Token);

        try
        {
            // 获取当前信号量的引用（快照）
            var currentSemaphore = GetCurrentSemaphore();
            await currentSemaphore.WaitAsync(combinedCts.Token);

            _logger.LogDebug("获取信号量成功，当前可用: {AvailableSlots}", currentSemaphore.CurrentCount + 1);

            return new SemaphoreLease(currentSemaphore, _logger);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && !_shutdownCts.IsCancellationRequested)
        {
            // WHF-R2/C4：取消与获取的竞态——WaitAsync 可能在已获取后抛 OCE，必须补还槽位
            var currentSemaphore = GetCurrentSemaphore();
            try { currentSemaphore.Release(); } catch (SemaphoreFullException) { /* 已被其他路径补还 */ }
            throw;
        }
        finally
        {
            combinedCts.Dispose();
        }
    }

    /// <summary>
    /// 获取当前信号量的快照
    /// </summary>
    private SemaphoreSlim GetCurrentSemaphore()
    {
        // 使用 Volatile.Read 确保读取最新值
        var semaphore = Interlocked.CompareExchange(ref _semaphore, null!, null!);
        return semaphore;
    }

    /// <summary>
    /// 获取当前可用信号量数量
    /// </summary>
    /// <remarks>此属性内部使用，不对外暴露</remarks>
    internal int AvailableCount => _semaphore.CurrentCount;

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // WHF-13：释放配置变更订阅，避免热更新回调在销毁过程中再次触发
        _onChangeSubscription?.Dispose();

        // 先发出关闭信号再释放：DisposeAsync 可能先于 Host.StopAsync 执行，
        // 此时不 Cancel 会让 AcquireAsync 中等待的处理器错过关闭通知
        _shutdownCts.Cancel();

        _shutdownCts.Dispose();
        _semaphore.Dispose();
        _semaphoreLock.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 信号量租约，用于 using 语句自动释放
    /// </summary>
    private class SemaphoreLease : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger _logger;
        private bool _disposed;

        public SemaphoreLease(SemaphoreSlim semaphore, ILogger logger)
        {
            _semaphore = semaphore;
            _logger = logger;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            // WHF-R2/C4：旧信号量可能在延迟释放后被 Dispose，此处容错不再向上传播
            try
            {
                _semaphore.Release();
                _logger.LogDebug("释放信号量成功，当前可用: {AvailableSlots}", _semaphore.CurrentCount);
            }
            catch (ObjectDisposedException)
            {
                // 旧信号量已释放，槽位无需归还
            }
        }
    }
}
