// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Mud.Feishu.AI.Tests.Conversations;

/// <summary>
/// <see cref="KeyedConversationGate"/> 取消竞态压测（R2-10）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是压测而不是断言式单测</b>：取消路径「不 Release」依赖 .NET
/// <see cref="SemaphoreSlim"/> 在「取消与获取竞态」时自行把许可交给下一个等待者的运行时契约。
/// 该契约<b>不可从代码内部观测</b>（<c>WaitAsync</c> 抛 <see cref="OperationCanceledException"/> 时，
/// 调用方无法分辨许可是否已被消费）——唯一可信的判据就是本压测：
/// 「许可泄漏」表现为风暴结束后同键再也 Acquire 不到；「许可超发」表现为
/// <see cref="SemaphoreFullException"/>。
/// </para>
/// <para>
/// 本用例在<b>每个 TFM</b> 上运行；一旦失败即启用 R2 方案 §0.5.2 备选方案
/// （<c>WaitAsync(CancellationToken.None)</c> + <c>Task.WhenAny</c> 显式所有权），不再争论语义。
/// </para>
/// </remarks>
public class KeyedConversationGateCancellationStressTests
{
    private const string Key = "feishu:app-a:conversation:chat:oc_stress";
    private const string OtherKey = "feishu:app-a:conversation:chat:oc_other";

    /// <summary>风暴规模（取消 + 释放并发交错，足以撞出竞态但保持用例秒级完成）。</summary>
    private const int StormSize = 200;

    /// <summary>
    /// 同键「取消风暴 × 并发释放」：不得出现 SemaphoreFullException / ObjectDisposedException /
    /// 除 <see cref="OperationCanceledException"/> 之外的任何异常。
    /// </summary>
    [Fact]
    public async Task CancelStorm_ShouldNotThrowSemaphoreFullException()
    {
        var gate = new KeyedConversationGate();
        var unexpected = new ConcurrentQueue<Exception>();

        // 持有者循环：反复「取得 → 极短持有 → 释放」，制造「释放与取消撞在一起」的窗口。
        var holderLoop = Task.Run(async () =>
        {
            for (var i = 0; i < StormSize; i++)
            {
                var handle = await gate.AcquireAsync(Key);
                await Task.Delay(1);
                handle.Dispose();
            }
        });

        var waiters = Enumerable.Range(0, StormSize).Select(i => Task.Run(async () =>
        {
            using var cts = new CancellationTokenSource(Random.Shared.Next(0, 3));
            try
            {
                var handle = await gate.AcquireAsync(Key, cts.Token);
                handle.Dispose();
            }
            catch (OperationCanceledException)
            {
                // 预期路径。
            }
            catch (Exception ex)
            {
                unexpected.Enqueue(ex);
            }
        })).ToArray();

        await Task.WhenAll(waiters);
        await holderLoop;

        unexpected.Should().BeEmpty(
            "取消路径不得产生 SemaphoreFullException / ObjectDisposedException——"
            + "前者意味着许可超发（其后 Release 会抛），后者意味着闸门被提前回收");
    }

    /// <summary>
    /// 许可不泄漏的可执行判据：风暴结束后同键仍能在超时前完整 Acquire/Release 一个回合。
    /// </summary>
    [Fact]
    public async Task CancelStorm_ShouldNotLeakPermit_SameKeyStillAcquirable()
    {
        var gate = new KeyedConversationGate();

        // 全部取消（无一取得许可）：若实现泄漏许可，此处就会把许可吃光。
        var cancellations = Enumerable.Range(0, StormSize).Select(_ => Task.Run(async () =>
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            try
            {
                var handle = await gate.AcquireAsync(Key, cts.Token);
                handle.Dispose();
            }
            catch (OperationCanceledException)
            {
            }
        })).ToArray();

        await Task.WhenAll(cancellations);

        // 关键断言：风暴后同键必须仍能取得许可（否则会话永久卡死）。
        var acquire = gate.AcquireAsync(Key);
        var completed = await Task.WhenAny(acquire, Task.Delay(TimeSpan.FromSeconds(5)));

        completed.Should().BeSameAs(acquire, "取消风暴不得泄漏许可——否则同键后续 Acquire 永久阻塞（会话卡死）");

        var handle = await acquire;
        handle.Dispose();

        // 再来一个回合：确认 Dispose 后计数归零、可再次取得。
        var second = gate.AcquireAsync(Key);
        (await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(5)))).Should().BeSameAs(second);
        (await second).Dispose();
    }

    /// <summary>取消风暴不得污染其它键（跨键仍并行、可立即取得）。</summary>
    [Fact]
    public async Task CancelStorm_CrossKey_ShouldNotCrossContaminate()
    {
        var gate = new KeyedConversationGate();

        var storms = Enumerable.Range(0, 100).Select(_ => Task.Run(async () =>
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            try
            {
                var handle = await gate.AcquireAsync(Key, cts.Token);
                handle.Dispose();
            }
            catch (OperationCanceledException)
            {
            }
        })).ToArray();

        await Task.WhenAll(storms);

        var other = gate.AcquireAsync(OtherKey);
        (await Task.WhenAny(other, Task.Delay(TimeSpan.FromSeconds(5)))).Should().BeSameAs(other,
            "同键的取消风暴不得影响其它会话键（键空闲即回收，跨键并行）");
        (await other).Dispose();
    }

    /// <summary>取消语义不回归：已取消的令牌必须抛 <see cref="OperationCanceledException"/>（不静默成功）。</summary>
    [Fact]
    public async Task AcquireAsync_ShouldPropagateOperationCanceledException()
    {
        var gate = new KeyedConversationGate();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await gate.AcquireAsync(Key, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
