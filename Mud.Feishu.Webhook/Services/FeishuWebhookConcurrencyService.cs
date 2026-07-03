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
        _optionsMonitor.OnChange(async newOptions =>
        {
            await UpdateSemaphoreAsync(newOptions.MaxConcurrentEvents);
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

        _shutdownCts.Cancel();

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

            if (_semaphoreUpgraded)
            {
                // 已经升级过，原子替换并延迟释放旧信号量
                var oldSemaphore = Interlocked.Exchange(ref _semaphore,
                    new SemaphoreSlim(actualMaxConcurrent, actualMaxConcurrent));

                _logger.LogInformation("信号量已重新创建，新的最大并发数: {NewMax} (实际: {ActualMaxConcurrent})", newMaxConcurrent, actualMaxConcurrent);

                // 延迟释放旧信号量，等待可能正在使用的请求完成
                _ = Task.Run(async () =>
                   {
                       await Task.Delay(60000); // 等待 60 秒
                       oldSemaphore.Dispose();
                   });
            }
            else
            {
                // 首次升级，原子替换并延迟释放旧信号量（修复信号量泄漏）
                var oldSemaphore = Interlocked.Exchange(ref _semaphore,
                    new SemaphoreSlim(actualMaxConcurrent, actualMaxConcurrent));
                _semaphoreUpgraded = true;
                _logger.LogInformation("信号量首次创建，最大并发数: {NewMax} (实际: {ActualMaxConcurrent})", newMaxConcurrent, actualMaxConcurrent);

                // 延迟释放旧信号量，等待可能正在使用的请求完成
                _ = Task.Run(async () =>
                {
                    await Task.Delay(60000); // 等待 60 秒
                    oldSemaphore.Dispose();
                });
            }
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
            _semaphore.Release();
            _logger.LogDebug("释放信号量成功，当前可用: {AvailableSlots}", _semaphore.CurrentCount);
        }
    }
}
