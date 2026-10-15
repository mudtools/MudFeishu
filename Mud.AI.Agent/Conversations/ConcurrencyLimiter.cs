// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Conversations;

/// <summary>
/// 全局削峰引擎（第八式「中立引擎返回中立结果，外壳决定异常语义」）：限制进程内在途并发数，满额时按调用方选择等待空位或立即返回，队列等待另有超时。
/// </summary>
/// <remarks>
/// <para>
/// <b>零业务异常 + 超时显式建模</b>：以 <see langword="null"/> 表达"这次没拿到"（满额或队列超时），业务异常由宿主外壳翻译；取消按 .NET 通用语义原样穿透（它不是业务异常）。
/// 超时用 <see cref="SemaphoreSlim"/> 的超时等待重载（返回 <see langword="false"/> 即超时）——<b>不用</b> <c>CancelAfter</c> ＋ 取消标志反推：那种写法无法区分"外部取消"与"队列超时"，两者并发触发时超时诉求会被静默吞掉。
/// </para>
/// <para>
/// <b>不做许可预检</b>：<c>FailFast</c> 的满额检查与取许可之间存在竞态，但该竞态只在满额时改变结果（先到者拿许可、后到者得 <see langword="null"/>），不产生超发；引擎正确性不依赖该检查的原子性。
/// </para>
/// </remarks>
public sealed class ConcurrencyLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private readonly TimeSpan? _queueTimeout;
    private readonly bool _failFast;
    private readonly int _maxConcurrency;

    /// <summary>
    /// 初始化 <see cref="ConcurrencyLimiter"/>。
    /// </summary>
    /// <param name="maxConcurrency">进程内最大在途数（必须为正）。</param>
    /// <param name="queueTimeout">队列等待超时；<see langword="null"/> = 不限等待（等到取得为止）。</param>
    /// <param name="failFast">满额时立即返回 <see langword="null"/>（不排队、不等待）。</param>
    /// <exception cref="ArgumentOutOfRangeException">并发上限非正，或队列超时非正。</exception>
    public ConcurrencyLimiter(int maxConcurrency, TimeSpan? queueTimeout = null, bool failFast = false)
    {
        if (maxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxConcurrency), maxConcurrency, "进程内最大在途数必须为正数");
        }

        if (queueTimeout is { } timeout && timeout <= TimeSpan.Zero)
        {
            // 不限等待的表示是 null；传 <= 0 的时长几乎总是把"0 秒"误当成"不等待"，
            // 而这种误用在运行期只表现为"总是忙"，极难定位（无异常、无日志）。
            throw new ArgumentOutOfRangeException(
                nameof(queueTimeout), queueTimeout, "队列超时必须为正数；不限等待请传 null");
        }

        _maxConcurrency = maxConcurrency;
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        _queueTimeout = queueTimeout;
        _failFast = failFast;
    }

    /// <summary>配置的进程内最大在途数。</summary>
    public int MaxConcurrency => _maxConcurrency;

    /// <summary>当前在途数（<see cref="MaxConcurrency"/> 减去可用许可）。</summary>
    public int InFlight => _maxConcurrency - _semaphore.CurrentCount;

    /// <summary>
    /// 尝试取得一个在途许可；成功返回释放器（Dispose 即归还，幂等于重复 Dispose）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>许可释放器；<see langword="null"/> = 未取得（满额 <c>FailFast</c> 或队列超时）。</returns>
    /// <exception cref="OperationCanceledException">等待期间被取消（.NET 通用语义，原样穿透）。</exception>
    public async Task<IDisposable?> TryAcquireAsync(CancellationToken cancellationToken = default)
    {
        if (_failFast && _semaphore.CurrentCount == 0)
        {
            return null;
        }

        if (_queueTimeout is { } timeout)
        {
            // 超时等待重载的返回值为 false 即"超时"，取消仍抛 OperationCanceledException：
            // 两条路径的来源由 API 形状直接给出，无需任何标志反推。
            if (!await _semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }
        }
        else
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return new Lease(this);
    }

    /// <summary>在途许可的释放器：幂等，且只归还本类型取得的那一个许可。</summary>
    private sealed class Lease(ConcurrencyLimiter owner) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            owner._semaphore.Release();
        }
    }
}
