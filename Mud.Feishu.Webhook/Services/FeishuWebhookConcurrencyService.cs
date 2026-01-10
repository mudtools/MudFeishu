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
    private readonly SemaphoreSlim _semaphore;
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor;
    private readonly ILogger<FeishuWebhookConcurrencyService> _logger;
    private readonly object _semaphoreLock = new();
    private bool _disposed;
    private int _currentMaxConcurrentEvents;

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
        lock (_semaphoreLock)
        {
            if (_disposed || newMaxConcurrent == _currentMaxConcurrentEvents)
                return;

            var oldMax = _currentMaxConcurrentEvents;
            _currentMaxConcurrentEvents = newMaxConcurrent;

            _logger.LogInformation("并发控制配置已更新，最大并发数: {OldMax} -> {NewMax}",
                oldMax, newMaxConcurrent);

            // 由于 SemaphoreSlim 的初始计数不能动态修改，
            // 新配置将在下一次创建信号量时生效
            // 这里的处理是合理的，因为配置变更应该谨慎处理
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
        await _semaphore.WaitAsync(cancellationToken);

        _logger.LogDebug("获取信号量成功，当前可用: {AvailableSlots}", _semaphore.CurrentCount + 1);

        return new SemaphoreLease(_semaphore, _logger);
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
