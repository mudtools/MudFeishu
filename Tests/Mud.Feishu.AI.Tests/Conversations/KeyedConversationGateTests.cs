// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.AI.Conversations;

namespace Mud.Feishu.AI.Tests.Conversations;

/// <summary>
/// 进程内会话闸门测试（AI-FD-D12 P2D-1）：同键串行、跨键并行、Dispose 放行、
/// 空闲回收（信号量不泄漏）、取消传播。
/// </summary>
public class KeyedConversationGateTests
{
    [Fact]
    public async Task AcquireAsync_ShouldSerializeSameKey_AndReleaseOnDispose()
    {
        var gate = new KeyedConversationGate();
        var timeline = new ConcurrentQueue<string>();

        var first = await gate.AcquireAsync("feishu:app-a:conversation:chat:oc_1");
        var secondTask = Task.Run(async () =>
        {
            using var handle = await gate.AcquireAsync("feishu:app-a:conversation:chat:oc_1");
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
        var gate = new KeyedConversationGate();
        using var firstEntered = new ManualResetEventSlim(false);
        using var secondEntered = new ManualResetEventSlim(false);

        var firstTask = Task.Run(async () =>
        {
            using var _ = await gate.AcquireAsync("feishu:app-a:conversation:chat:oc_1");
            firstEntered.Set();
            secondEntered.Wait(2000);
        });
        var secondTask = Task.Run(async () =>
        {
            using var _ = await gate.AcquireAsync("feishu:app-a:conversation:chat:oc_2");
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
        var gate = new KeyedConversationGate();

        for (var round = 0; round < 3; round++)
        {
            using (var handle = await gate.AcquireAsync("feishu:app-a:conversation:chat:oc_recycle"))
            {
                handle.Should().NotBeNull();
            }
        }

        // 空闲回收后再次获取应正常（新条目）。
        using var reacquired = await gate.AcquireAsync("feishu:app-a:conversation:chat:oc_recycle");
        reacquired.Should().NotBeNull("回收后重新获取不失效（信号量按需重建）");
    }

    [Fact]
    public async Task AcquireAsync_ShouldPropagateCancellation()
    {
        var gate = new KeyedConversationGate();
        using var first = await gate.AcquireAsync("feishu:app-a:conversation:chat:oc_cancel");
        using var cts = new CancellationTokenSource(50);

        var act = async () => await gate.AcquireAsync("feishu:app-a:conversation:chat:oc_cancel", cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>("等待中的取消即时传播");
    }

    [Fact]
    public async Task AcquireAsync_ShouldRejectEmptyKey()
    {
        var gate = new KeyedConversationGate();
        var act = async () => await gate.AcquireAsync(" ");
        await act.Should().ThrowAsync<ArgumentException>("键必须由 ConversationKeyBuilder 构造（非空校验兜底）");
    }
}
