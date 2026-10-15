// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Mud.AI.Agent.Tests.Conversations;

/// <summary>
/// 键控串行闸门引擎的机械语义：同键串行、跨键并行、Dispose 放行、空闲回收（信号量不泄漏）、
/// 空键拒绝。
/// </summary>
/// <remarks>
/// 取消竞态（许可不丢失/不超发的运行时契约）由
/// <see cref="KeyedAsyncGateCancellationStressTests"/> 实证守护——两者分工：本组管"常态语义"，
/// 压测管"取消 × 释放并发交错"。
/// </remarks>
public class KeyedAsyncGateTests
{
    [Fact]
    public async Task AcquireAsync_ShouldSerializeSameKey_AndReleaseOnDispose()
    {
        var gate = new KeyedAsyncGate();
        var timeline = new ConcurrentQueue<string>();

        using var first = await gate.AcquireAsync("key-1");
        var secondTask = Task.Run(async () =>
        {
            using var handle = await gate.AcquireAsync("key-1");
            timeline.Enqueue("second-entered");
        });

        // 让第二个任务先等待，验证其被闸门挡住。
        await Task.Delay(50);
        timeline.Should().NotContain("second-entered", "同键第二个 Acquire 必须等待（串行化）");

        first.Dispose();
        await Task.WhenAny(secondTask, Task.Delay(2000));
        timeline.Should().Contain("second-entered", "首个句柄 Dispose 后放行");
    }

    [Fact]
    public async Task AcquireAsync_ShouldRunDifferentKeysInParallel()
    {
        var gate = new KeyedAsyncGate();
        using var firstEntered = new ManualResetEventSlim(false);
        using var secondEntered = new ManualResetEventSlim(false);

        var firstTask = Task.Run(async () =>
        {
            using var _ = await gate.AcquireAsync("key-1");
            firstEntered.Set();
            secondEntered.Wait(2000);
        });
        var secondTask = Task.Run(async () =>
        {
            using var _ = await gate.AcquireAsync("key-2");
            secondEntered.Set();
            firstEntered.Wait(2000);
        });

        var allDone = Task.WhenAll(firstTask, secondTask);
        var winner = await Task.WhenAny(allDone, Task.Delay(3000));
        winner.Should().Be(allDone, "跨键并行：双方都应拿到处理权并互相等待后正常退出");
    }

    [Fact]
    public async Task AcquireAsync_ShouldAllowReacquire_AfterRelease()
    {
        var gate = new KeyedAsyncGate();

        for (var round = 0; round < 3; round++)
        {
            using (var handle = await gate.AcquireAsync("key-recycle"))
            {
                handle.Should().NotBeNull();
            }
        }

        // 空闲回收后再次获取应正常（新条目）。
        using var reacquired = await gate.AcquireAsync("key-recycle");
        reacquired.Should().NotBeNull("回收后重新获取不失效（信号量按需重建）");
    }

    [Fact]
    public async Task AcquireAsync_ShouldPropagateCancellation()
    {
        var gate = new KeyedAsyncGate();
        using var first = await gate.AcquireAsync("key-cancel");
        using var cts = new CancellationTokenSource(50);

        var act = async () => await gate.AcquireAsync("key-cancel", cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>("等待中的取消即时传播");
    }

    [Fact]
    public async Task AcquireAsync_ShouldRejectEmptyKey()
    {
        var gate = new KeyedAsyncGate();
        var act = async () => await gate.AcquireAsync(" ");
        await act.Should().ThrowAsync<ArgumentException>("键必须非空（引擎兜底；键的构造归调用方）");
    }

    /// <summary>句柄 Dispose 幂等：双重放行不得让计数失衡（其后同键仍可正常取得）。</summary>
    [Fact]
    public async Task Handle_ShouldBeIdempotentOnDoubleDispose()
    {
        var gate = new KeyedAsyncGate();

        var handle = await gate.AcquireAsync("key-double");
        handle.Dispose();
        handle.Dispose();

        var second = gate.AcquireAsync("key-double");
        (await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(5)))).Should().BeSameAs(second,
            "重复 Dispose 不得把许可超发/超放（否则后续同键 Acquire 与条目回收错乱）");
        (await second).Dispose();
    }
}
