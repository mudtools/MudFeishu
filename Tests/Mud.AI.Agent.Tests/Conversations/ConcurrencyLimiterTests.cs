// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Conversations;

/// <summary>
/// 削峰引擎的四条路径（正常获取 / FailFast 满额 / 队列超时 / 外部取消）及其边界：
/// 超时与取消的<b>来源分界</b>、许可不泄漏、释放幂等、参数边界。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么"来源分界"必须单独立案</b>：超时与外部取消在旧实现里都表现为
/// <see cref="OperationCanceledException"/>，靠"取消标志是否为真"反推来源——两者并发触发时
/// 超时诉求会被静默吞掉。本组用例用同一组配置分别触发两条路径，锁住"超时 → <see langword="null"/>、
/// 取消 → 异常"这一分界（<c>ConcurrencyLimiter_ShouldModelTimeoutExplicitly</c> 是同批的源码侧哨兵）。
/// </para>
/// <para>
/// <b>时序纪律</b>：等待类断言依赖"另一个请求确实已进入等待"，用信号量/闸门而非固定睡眠对齐；
/// 仅"超时是否如期发生"用宽松下界（超时值本身就是契约）。
/// </para>
/// </remarks>
public class ConcurrencyLimiterTests
{
    /// <summary>正常路径：未满额时取得许可，在途计数随获取/释放对称变化。</summary>
    [Fact]
    public async Task TryAcquireAsync_ShouldGrantLease_WhenBelowLimit()
    {
        var limiter = new ConcurrencyLimiter(maxConcurrency: 2);

        limiter.MaxConcurrency.Should().Be(2);
        limiter.InFlight.Should().Be(0, "初始在途为 0");

        var lease = await limiter.TryAcquireAsync();
        lease.Should().NotBeNull("未满额必须取得许可");
        limiter.InFlight.Should().Be(1, "取得后在途 +1");

        lease!.Dispose();
        limiter.InFlight.Should().Be(0, "释放后在途归零");
    }

    /// <summary>FailFast：满额时立即返回 <see langword="null"/>，不排队等待。</summary>
    [Fact]
    public async Task TryAcquireAsync_ShouldReturnNull_WhenFailFastAndFull()
    {
        var limiter = new ConcurrencyLimiter(maxConcurrency: 1, queueTimeout: null, failFast: true);
        var held = await limiter.TryAcquireAsync();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var refused = await limiter.TryAcquireAsync();
        stopwatch.Stop();

        refused.Should().BeNull("FailFast 模式满额时不排队，直接返回 null（由外壳翻译成业务异常）");
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5), "FailFast 不得进入等待");

        held!.Dispose();
    }

    /// <summary>队列超时：等待超时返回 <see langword="null"/>（不是异常）；超时后许可未被吞掉。</summary>
    [Fact]
    public async Task TryAcquireAsync_ShouldReturnNull_WhenQueueTimeoutElapses()
    {
        var timeout = TimeSpan.FromMilliseconds(150);
        var limiter = new ConcurrencyLimiter(maxConcurrency: 1, queueTimeout: timeout);
        var held = await limiter.TryAcquireAsync();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var timedOut = await limiter.TryAcquireAsync();
        stopwatch.Stop();

        timedOut.Should().BeNull("队列超时的表达是 null，不是异常——异常语义留给外壳");
        stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(timeout, "必须真的等到超时点才放弃");

        held!.Dispose();
        (await limiter.TryAcquireAsync()).Should().NotBeNull("超时放弃不得吞掉许可（放弃的是登记，不是许可）");
    }

    /// <summary>
    /// 超时路径在<b>有可取消令牌但未取消</b>时仍返回 <see langword="null"/>——
    /// 反推出的"取消 ⇒ 超时"实现在此会错报成取消。
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_ShouldReturnNull_WhenTimeoutWithLiveToken()
    {
        var limiter = new ConcurrencyLimiter(maxConcurrency: 1, queueTimeout: TimeSpan.FromMilliseconds(120));
        var held = await limiter.TryAcquireAsync();
        using var liveCts = new CancellationTokenSource();

        var timedOut = await limiter.TryAcquireAsync(liveCts.Token);

        timedOut.Should().BeNull("令牌存在但未取消 ⇒ 超时路径，不得误判为外部取消");
        held!.Dispose();
    }

    /// <summary>外部取消：抛 <see cref="OperationCanceledException"/>（.NET 通用语义，原样穿透）。</summary>
    [Fact]
    public async Task TryAcquireAsync_ShouldThrowOperationCanceled_WhenExternallyCancelled()
    {
        var limiter = new ConcurrencyLimiter(maxConcurrency: 1, queueTimeout: null);
        var held = await limiter.TryAcquireAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(80));
        var act = async () => await limiter.TryAcquireAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>("外部取消不是业务忙：必须原样抛出");
        held!.Dispose();
    }

    /// <summary>
    /// 取消<b>先于</b>超时触发：即使配置了队列超时，也按取消路径抛异常（不是返回 <see langword="null"/>）。
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_ShouldPreferCancellation_WhenCancelledBeforeTimeout()
    {
        var limiter = new ConcurrencyLimiter(maxConcurrency: 1, queueTimeout: TimeSpan.FromSeconds(30));
        var held = await limiter.TryAcquireAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(60));
        var act = async () => await limiter.TryAcquireAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>(
            "取消是调用方的意图，必须优先于队列超时被观察者看到");

        held!.Dispose();
    }

    /// <summary>
    /// 取消不泄漏许可：取消后释放原持有者，新请求必须能立即取得（否则削峰会随取消次数逐步"自锁死"）。
    /// </summary>
    [Fact]
    public async Task TryAcquireAsync_ShouldNotLeakPermit_WhenCancelled()
    {
        var limiter = new ConcurrencyLimiter(maxConcurrency: 1, queueTimeout: null);
        var held = await limiter.TryAcquireAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(80));
        var act = async () => await limiter.TryAcquireAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        held!.Dispose();

        using var reclaim = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var again = await limiter.TryAcquireAsync(reclaim.Token);
        again.Should().NotBeNull("取消（放弃登记）不得吞掉许可");
        again!.Dispose();
    }

    /// <summary>不限等待（超时为 null）：第二个请求一直等，直到第一个释放才取得——不得提前返回 null。</summary>
    [Fact]
    public async Task TryAcquireAsync_ShouldWaitIndefinitely_WhenQueueTimeoutIsNull()
    {
        var limiter = new ConcurrencyLimiter(maxConcurrency: 1, queueTimeout: null);
        var held = await limiter.TryAcquireAsync();

        var pending = limiter.TryAcquireAsync();
        await Task.Delay(120);
        pending.IsCompleted.Should().BeFalse("不限等待时未满额解除前必须保持等待，不得返回 null");

        held!.Dispose();
        var acquired = await pending;
        acquired.Should().NotBeNull("原持有者释放后等待方应取得许可");
        acquired!.Dispose();
    }

    /// <summary>释放幂等：重复 Dispose 不二次归还许可（否则满额时会直接抛 <c>SemaphoreFullException</c>）。</summary>
    [Fact]
    public async Task Lease_ShouldBeIdempotentOnDoubleDispose()
    {
        var limiter = new ConcurrencyLimiter(maxConcurrency: 1, queueTimeout: TimeSpan.FromMilliseconds(60));
        var lease = await limiter.TryAcquireAsync();
        limiter.InFlight.Should().Be(1);

        lease!.Dispose();
        var act = () => lease.Dispose();

        act.Should().NotThrow("重复释放必须幂等——二次归还会超发许可（引擎满额时直接抛 SemaphoreFullException）");
        limiter.InFlight.Should().Be(0, "在途计数不得被重复释放推成负数");

        (await limiter.TryAcquireAsync()).Should().NotBeNull("一次释放恰好归还一个许可");
    }

    /// <summary>构造边界：并发上限与队列超时必须为正（0 秒超时 ≠ 不等待，误用只表现为"总是忙"）。</summary>
    [Theory]
    [InlineData(0, null, false)]
    [InlineData(-1, null, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, -5, false)]
    public void Constructor_ShouldRejectNonPositiveArguments(int maxConcurrency, int? timeoutMs, bool failFast)
    {
        var timeout = timeoutMs is { } ms ? TimeSpan.FromMilliseconds(ms) : (TimeSpan?)null;
        var act = () => new ConcurrencyLimiter(maxConcurrency, timeout, failFast);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
