// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 飞书 WebSocket 并发控制服务（背压闸门）。
/// </summary>
/// <remarks>
/// WS-03 修复引入：为 WebSocket 事件分发提供并发上界，与 Webhook 的
/// <c>FeishuWebhookConcurrencyService</c> 语义一致。
/// <para>
/// 使用 <see cref="SemaphoreSlim"/> 控制并发处理数；支持 <c>IOptionsMonitor</c> 热更新，
/// 原子替换信号量并延迟 60 秒释放旧信号量。实现 <see cref="IHostedService"/>，
/// 关停时最多等待 30 秒在途事件完成。
/// </para>
/// </remarks>
public class FeishuWebSocketConcurrencyService : IAsyncDisposable, IHostedService
{
    private readonly IOptionsMonitor<FeishuWebSocketOptions> _optionsMonitor;
    private readonly ILogger<FeishuWebSocketConcurrencyService> _logger;
    private readonly SemaphoreSlim _semaphoreLock = new(1, 1);
    // WS-03：本字段仅经 Interlocked.Exchange（原子替换）与 Volatile.Read（快照读）访问，自带完整栅栏语义，
    // 等效 volatile 的发布/获取保证；volatile 与 Interlocked/Volatile API 混用会产生 CS0420，故不标记 volatile。
    // 所有裸读一律通过 Volatile.Read 获取快照，不得直接读取本字段。
    private SemaphoreSlim _semaphore;
    // P1-5b 修复：_disposed 改为 int + Interlocked.Exchange 实现原子 check-then-set（与模块内既有范式统一）
    private int _disposed = 0;
    private volatile int _currentMaxConcurrentHandlers;
    private readonly CancellationTokenSource _shutdownCts = new();
    /// <summary>
    /// 配置变更订阅句柄（P2-5 修复：此前未保存，Dispose 后回调仍会进入）。
    /// </summary>
    private readonly IDisposable? _optionsChangeSubscription;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="optionsMonitor">配置选项监控器（支持热更新）</param>
    /// <param name="logger">日志记录器</param>
    public FeishuWebSocketConcurrencyService(
        IOptionsMonitor<FeishuWebSocketOptions> optionsMonitor,
        ILogger<FeishuWebSocketConcurrencyService> logger)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var options = _optionsMonitor.CurrentValue;
        _currentMaxConcurrentHandlers = options.MaxConcurrentHandlers;
        int actualMaxConcurrent = _currentMaxConcurrentHandlers > 0
            ? _currentMaxConcurrentHandlers
            : int.MaxValue;
        _semaphore = new SemaphoreSlim(actualMaxConcurrent, actualMaxConcurrent);

        _logger.LogInformation("飞书 WebSocket 并发控制服务初始化完成，最大并发数: {MaxConcurrentHandlers} (实际: {ActualMaxConcurrent})",
            _currentMaxConcurrentHandlers, actualMaxConcurrent);

        // 监听配置变更，支持热更新（P2-5：保存句柄以便 Dispose 时注销）
        _optionsChangeSubscription = _optionsMonitor.OnChange(OnOptionsChanged);
    }

    /// <summary>
    /// 配置变更回调（P2-5 修复：由 <c>async void</c> 改为"返回 Task 的显式 fire-and-forget"）。
    /// </summary>
    /// <param name="newOptions">新的配置快照</param>
    /// <remarks>
    /// <c>async void</c> 无法被观察、无法被等待，异常虽被内部 catch 兜住但语义上不属于回调契约；
    /// 现改为同步入口 + 独立异步方法，异常处理与生命周期显式化。
    /// </remarks>
    private void OnOptionsChanged(FeishuWebSocketOptions newOptions)
    {
        _ = UpdateSemaphoreSafeAsync(newOptions.MaxConcurrentHandlers);
    }

    /// <summary>
    /// 安全地更新并发配置（吞掉关停竞态与异常，不影响配置监听回调线程）。
    /// </summary>
    private async Task UpdateSemaphoreSafeAsync(int newMaxConcurrent)
    {
        try
        {
            await UpdateSemaphoreAsync(newMaxConcurrent).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // Dispose 竞态：忽略
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新并发控制配置时发生错误");
        }
    }

    /// <summary>
    /// HostedService 启动方法
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("飞书 WebSocket 并发控制服务已启动");
        return Task.CompletedTask;
    }

    /// <summary>
    /// HostedService 停止方法：等待在途事件处理完成（最多 30 秒）
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("飞书 WebSocket 并发控制服务正在停止...");

        _shutdownCts.Cancel();

        var timeout = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (Volatile.Read(ref _semaphore).CurrentCount < _currentMaxConcurrentHandlers)
        {
            if (DateTime.UtcNow - startTime > timeout)
            {
                _logger.LogWarning("等待并发处理完成超时，当前等待的处理数: {WaitingCount}",
                    _currentMaxConcurrentHandlers - Volatile.Read(ref _semaphore).CurrentCount);
                break;
            }

            await Task.Delay(100, cancellationToken);
        }

        _logger.LogInformation("飞书 WebSocket 并发控制服务已停止");
    }

    /// <summary>
    /// 更新信号量配置（原子替换 + 延迟释放旧信号量）
    /// </summary>
    private async Task UpdateSemaphoreAsync(int newMaxConcurrent)
    {
        await _semaphoreLock.WaitAsync();
        try
        {
            if (Volatile.Read(ref _disposed) == 1 || newMaxConcurrent == _currentMaxConcurrentHandlers)
                return;

            var oldMax = _currentMaxConcurrentHandlers;
            _currentMaxConcurrentHandlers = newMaxConcurrent;

            int actualMaxConcurrent = _currentMaxConcurrentHandlers > 0
                ? _currentMaxConcurrentHandlers
                : int.MaxValue;

            _logger.LogInformation("并发控制配置已更新，最大并发数: {OldMax} -> {NewMax} (实际: {ActualMaxConcurrent})",
                oldMax, newMaxConcurrent, actualMaxConcurrent);

            // 原子替换信号量并延迟释放旧信号量
            var oldSemaphore = Interlocked.Exchange(ref _semaphore,
                new SemaphoreSlim(actualMaxConcurrent, actualMaxConcurrent));

            // 延迟释放旧信号量，等待可能正在使用的请求完成
            _ = Task.Run(async () =>
            {
                await Task.Delay(60000);
                oldSemaphore.Dispose();
            });
        }
        finally
        {
            _semaphoreLock.Release();
        }
    }

    /// <summary>
    /// 异步获取并发租约。使用 <c>await using</c> 自动释放。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租约对象，释放时归还信号量</returns>
    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        // 组合应用关闭的取消令牌
        var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _shutdownCts.Token);

        try
        {
            var currentSemaphore = GetCurrentSemaphore();
            await currentSemaphore.WaitAsync(combinedCts.Token);

            return new SemaphoreLease(currentSemaphore);
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
        return Volatile.Read(ref _semaphore);
    }

    /// <summary>
    /// 当前可用并发槽位数量
    /// </summary>
    internal int AvailableCount => Volatile.Read(ref _semaphore).CurrentCount;

    /// <summary>
    /// 当前配置的最大并发数（0 或负数表示无限制）
    /// </summary>
    internal int MaxConcurrentHandlers => _currentMaxConcurrentHandlers;

    /// <summary>
    /// 当前积压（正在处理中的）事件数
    /// </summary>
    internal int PendingCount =>
        _currentMaxConcurrentHandlers > 0
            ? _currentMaxConcurrentHandlers - Volatile.Read(ref _semaphore).CurrentCount
            : 0;

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <remarks>
    /// P1-5b（I9）：<b>不释放</b> <c>_semaphore</c> / <c>_semaphoreLock</c>。
    /// 二者均未访问 <c>SemaphoreSlim.AvailableWaitHandle</c>，不释放不产生任何 OS 句柄泄漏；
    /// 而释放会与"在途租约归还"（<c>SemaphoreLease.Dispose → Release()</c>）以及配置变更回调
    /// 构成 <see cref="ObjectDisposedException"/> 竞态。
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        // P1-5b：原子 check-then-set
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return default;

        // P2-5 修复：注销配置变更订阅，避免 Dispose 之后回调仍进入
        _optionsChangeSubscription?.Dispose();
        _shutdownCts.Dispose();
        GC.SuppressFinalize(this);
        return default;
    }

    /// <summary>
    /// 信号量租约，用于 using 语句自动释放
    /// </summary>
    private class SemaphoreLease : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public SemaphoreLease(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _semaphore.Release();
        }
    }
}
