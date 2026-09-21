// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 应用上下文退休队列（TMA-07 / P1-6 修复，D5 契约）。
/// </summary>
/// <remarks>
/// <para>
/// 当 <see cref="FeishuAppManager"/> 执行配置热更新（<c>ApplyConfigurationChanges</c>，
/// TMF-04：原 <c>RebuildAppContext</c> 已并入热更新路径）或 <c>RemoveApp</c> 时，
/// 旧的 <see cref="FeishuAppContext"/> 不再被引用，但其内部的 <c>Timer</c>（令牌定时刷新）
/// 会 root 整个对象图，GC 不会自动回收。此队列在宽限期后显式 <c>Dispose</c> 旧上下文，
/// 停止其 Timer，释放资源。
/// </para>
/// <para>
/// <b>宽限期</b>的设计目的是允许在途请求安全完成。在途请求持有旧上下文的 <c>HttpClient</c> 引用，
/// 立即 <c>Dispose</c> 会导致在途请求抛 <c>ObjectDisposedException</c>。
/// </para>
/// <para>
/// <b>自清闭环</b>（C6）：退休 <c>Dispose</c> 后，后台刷新链在下一轮捕获
/// <c>ObjectDisposedException</c> 并自动摘除字典项，形成闭环，
/// 不需要组件提供注销 API。
/// </para>
/// <para>
/// 此类使用 <see cref="ConcurrentQueue{T}"/> + 单个 <see cref="Timer"/> 实现，
/// 兼容全部目标框架（netstandard2.0 / net6.0 / net8.0 / net10.0）。
/// </para>
/// <para>
/// <b>维护约束（TMA-24）</b>：若 <see cref="FeishuAppContext"/>（或其持有的任何成员）
/// 新增了实现 <see cref="IDisposable"/> 的资源（如 <c>Timer</c>、<c>HttpClient</c> 等），
/// 必须确保该资源在 <see cref="FeishuAppContext.Dispose"/> 中被释放，
/// 否则退休队列的 <c>Dispose</c> 调用无法停止该资源，会导致泄漏。
/// 禁止以"GC 会回收"为由省略 <c>Dispose</c>——<c>Timer</c> 会 root 整个对象图，GC 不会代劳。
/// </para>
/// </remarks>
internal sealed class FeishuAppContextRetirement : IDisposable
{
    /// <summary>
    /// 退休条目：记录待释放的上下文及其退休时间。
    /// </summary>
    private readonly ConcurrentQueue<RetirementEntry> _entries = new();

    /// <summary>
    /// 扫描定时器，周期性检查到期条目。
    /// </summary>
    private readonly Timer _sweepTimer;

    /// <summary>
    /// 退休宽限期（秒）。
    /// </summary>
    private readonly TimeSpan _retireDelay;

    /// <summary>
    /// 日志记录器。
    /// </summary>
    private readonly ILogger? _logger;

    /// <summary>
    /// 释放标记。
    /// </summary>
    /// <remarks>
    /// TMR-P2-13（F13）：由 volatile bool 收敛为 int + <see cref="Interlocked"/> 原子 check-then-set——
    /// 并发 Dispose（容器关闭 Flush 与 Sweep Timer 重叠）下保证释放序列恰好执行一次。
    /// </remarks>
    private int _disposed;

    /// <summary>
    /// 初始化退休队列。
    /// </summary>
    /// <param name="retireDelaySeconds">退休宽限期（秒），到达后 Dispose 旧上下文。</param>
    /// <param name="logger">日志记录器（可选）。</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// 当 <paramref name="retireDelaySeconds"/> 不在 [1, 3600] 范围内时抛出。
    /// </exception>
    public FeishuAppContextRetirement(int retireDelaySeconds, ILogger? logger = null)
    {
        if (retireDelaySeconds < 1 || retireDelaySeconds > 3600)
            throw new ArgumentOutOfRangeException(nameof(retireDelaySeconds), retireDelaySeconds,
                "ContextRetireDelaySeconds 必须在 1–3600 秒之间。");

        _retireDelay = TimeSpan.FromSeconds(retireDelaySeconds);
        _logger = logger;

        // Timer 回调周期取 min(30s, retireDelay/2)，确保宽限期到期后能及时扫描。
        var sweepPeriod = TimeSpan.FromMilliseconds(Math.Min(30_000, _retireDelay.TotalMilliseconds / 2));
        _sweepTimer = new Timer(
            _ => Sweep(DateTimeOffset.UtcNow),
            null,
            sweepPeriod,
            sweepPeriod);
    }

    /// <summary>
    /// 将一个旧上下文加入退休队列，在宽限期后 Dispose。
    /// </summary>
    /// <param name="appKey">应用键（用于日志）。</param>
    /// <param name="context">待退休的应用上下文。</param>
    public void Enqueue(string appKey, FeishuAppContext context)
    {
        if (context == null)
            return;

        var retireAt = DateTimeOffset.UtcNow.Add(_retireDelay);
        _entries.Enqueue(new RetirementEntry(appKey, context, retireAt));

        _logger?.LogInformation(
            "应用 {AppKey} 的旧上下文已进入退休队列，将在 {RetireDelay} 秒后释放（宽限期保护在途请求）。",
            appKey, (int)_retireDelay.TotalSeconds);
    }

    /// <summary>
    /// 扫描并释放已到期的退休条目。
    /// </summary>
    /// <param name="now">当前时间（由调用方传入，便于测试驱动）。</param>
    /// <returns>本次扫描释放的条目数。</returns>
    internal int Sweep(DateTimeOffset now)
    {
        var disposed = 0;

        while (_entries.TryDequeue(out var entry))
        {
            if (entry.RetireAt > now)
            {
                // 尚未到期，重新入队等待下次扫描。
                _entries.Enqueue(entry);
                break; // ConcurrentQueue 不保证严格 FIFO，但到期时间接近的条目大致有序；
                       // break 避免遍历整个队列（绝大多数场景下队首即最早到期）。
            }

            try
            {
                entry.Context.Dispose();
                disposed++;

                _logger?.LogDebug(
                    "应用 {AppKey} 的旧上下文已释放（退休队列扫描），Timer 已停止。",
                    entry.AppKey);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogWarning(ex,
                    "释放应用 {AppKey} 的旧上下文时发生异常，已忽略（Timer 可能仍在运行）。",
                    entry.AppKey);
            }
        }

        return disposed;
    }

    /// <summary>
    /// 获取当前待退休的条目数（用于诊断与健康检查）。
    /// </summary>
    internal int PendingCount => _entries.Count;

    /// <summary>
    /// 强制释放全部待退休上下文（供 <see cref="FeishuAppManager.Dispose"/> 调用）。
    /// </summary>
    public void Flush()
    {
        while (_entries.TryDequeue(out var entry))
        {
            try
            {
                entry.Context.Dispose();

                _logger?.LogDebug(
                    "应用 {AppKey} 的旧上下文已强制释放（Flush）。",
                    entry.AppKey);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogWarning(ex,
                    "强制释放应用 {AppKey} 的旧上下文时发生异常，已忽略。",
                    entry.AppKey);
            }
        }
    }

    /// <summary>
    /// 释放退休队列资源：停止定时器并强制释放全部待退休上下文。
    /// </summary>
    public void Dispose()
    {
        // TMR-P2-13（F13）：原子 check-then-set 收敛。
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _sweepTimer.Dispose();
        Flush();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 退休条目。
    /// </summary>
    private sealed record RetirementEntry(string AppKey, FeishuAppContext Context, DateTimeOffset RetireAt);
}