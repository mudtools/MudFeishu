// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 飞书 Webhook 并发控制服务
/// 使用全局 SemaphoreSlim 控制事件处理并发数
/// </summary>
public class FeishuWebhookConcurrencyService : IAsyncDisposable
{
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor;
    private readonly ILogger<FeishuWebhookConcurrencyService> _logger;
    private readonly SemaphoreSlim _semaphoreLock = new(1, 1);
    private SemaphoreSlim _semaphore;
    private bool _disposed;
    private volatile int _currentMaxConcurrentEvents;

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
        _currentMaxConcurrentEvents = options.MaxConcurrentEvents;
        _semaphore = new SemaphoreSlim(_currentMaxConcurrentEvents, _currentMaxConcurrentEvents);

        _logger.LogInformation("飞书 Webhook 并发控制服务初始化完成，最大并发数: {MaxConcurrentEvents}",
            _currentMaxConcurrentEvents);

        // 监听配置变更，支持热更新
        _optionsMonitor.OnChange(newOptions =>
        {
            UpdateSemaphore(newOptions.MaxConcurrentEvents);
        });
    }

    /// <summary>
    /// 更新信号量配置
    /// </summary>
    private void UpdateSemaphore(int newMaxConcurrent)
    {
        // 使用信号量确保只有一个线程在更新配置
        _semaphoreLock.Wait();

        try
        {
            if (_disposed || newMaxConcurrent == _currentMaxConcurrentEvents)
                return;

            var oldMax = _currentMaxConcurrentEvents;
            _currentMaxConcurrentEvents = newMaxConcurrent;

            _logger.LogInformation("并发控制配置已更新，最大并发数: {OldMax} -> {NewMax}",
                oldMax, newMaxConcurrent);

            // 创建新的信号量
            var oldSemaphore = _semaphore;
            _semaphore = new SemaphoreSlim(_currentMaxConcurrentEvents, _currentMaxConcurrentEvents);

            _logger.LogInformation("信号量已重新创建，新的最大并发数: {NewMax}", newMaxConcurrent);

            // 注意：旧的信号量不会被立即释放，因为可能还有等待者
            // 旧信号量的 Dispose 会延迟到对象被 GC 回收
            // 这是设计上的权衡：配置变更即时生效，但旧请求仍使用旧配置
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
        // 获取当前信号量的引用（快照）
        var currentSemaphore = GetCurrentSemaphore();
        await currentSemaphore.WaitAsync(cancellationToken);

        _logger.LogDebug("获取信号量成功，当前可用: {AvailableSlots}", currentSemaphore.CurrentCount + 1);

        return new SemaphoreLease(currentSemaphore, _logger);
    }

    /// <summary>
    /// 获取当前信号量的快照
    /// </summary>
    private SemaphoreSlim GetCurrentSemaphore()
    {
        return Volatile.Read(ref _semaphore);
    }

    /// <summary>
    /// 获取当前可用信号量数量
    /// </summary>
    /// <remarks>此属性内部使用，不对外暴露</remarks>
    internal int AvailableCount => _semaphore.CurrentCount;

    /// <summary>
    /// 释放资源
    /// </summary>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
#if NETSTANDARD2_0
            return default;
#else
            return ValueTask.CompletedTask;
#endif

        _disposed = true;
        _semaphore.Dispose();
#if NETSTANDARD2_0
        return default;
#else
        return ValueTask.CompletedTask;
#endif
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
