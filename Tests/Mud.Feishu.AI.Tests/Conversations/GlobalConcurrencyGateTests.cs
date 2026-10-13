// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.AI.Conversations;

namespace Mud.Feishu.AI.Tests.Conversations;

/// <summary>
/// T5 / D5 全局在途背压闸门行为用例。
/// </summary>
public class GlobalConcurrencyGateTests
{
    /// <summary>
    /// 上限生效：超过 maxConcurrentTurns 的请求排队等待。
    /// </summary>
    [Fact]
    public async Task AcquireAsync_ExceedsMax_ShouldQueue()
    {
        var inner = new KeyedConversationGate();
        var gate = new GlobalConcurrencyGate(inner, maxConcurrentTurns: 2, turnQueueTimeoutSeconds: 5);

        var handle1 = await gate.AcquireAsync("key1");
        var handle2 = await gate.AcquireAsync("key2");

        // 第三次应排队（因全局上限 2）
        var acquireTask = gate.AcquireAsync("key3");

        // 短暂等待确认排队
        await Task.Delay(100);
        acquireTask.IsCompleted.Should().BeFalse("第三次请求应排队等待空位");

        // 释放一个空位
        handle1.Dispose();

        // 排队请求应获得空位
        var handle3 = await acquireTask;
        handle3.Should().NotBeNull();

        handle2.Dispose();
        handle3.Dispose();
    }

    /// <summary>
    /// 队列超时抛 ConversationBusyException。
    /// </summary>
    [Fact]
    public async Task AcquireAsync_QueueTimeout_ShouldThrowBusyException()
    {
        var inner = new KeyedConversationGate();
        var gate = new GlobalConcurrencyGate(inner, maxConcurrentTurns: 1, turnQueueTimeoutSeconds: 1);

        var handle1 = await gate.AcquireAsync("key1");

        // 第二次应排队，1 秒后超时
        var act = async () => await gate.AcquireAsync("key2");

        await act.Should().ThrowAsync<ConversationBusyException>()
            .WithMessage("*超时*");

        handle1.Dispose();
    }

    /// <summary>
    /// FailFast 模式：满额时立即失败。
    /// </summary>
    /// <remarks>
    /// 计时敏感（Category=Timing，测试体系治理 §三.3）：断言的是硬性墙钟阈值（500ms），
    /// 在全量门禁高负载下实测可达 2.6s 而抖动出假红；Standard/Fast 档与 CI 排除，
    /// 仅 verify-build.ps1 -Full 档执行。
    /// </remarks>
    [Fact]
    [Trait("Category", "Timing")]
    public async Task AcquireAsync_FailFast_ShouldThrowImmediately()
    {
        var inner = new KeyedConversationGate();
        var gate = new GlobalConcurrencyGate(
            inner, maxConcurrentTurns: 1, turnQueueTimeoutSeconds: 30, busyMode: BusyModes.FailFast);

        var handle1 = await gate.AcquireAsync("key1");

        // 第二次应立即失败（不等待）
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var act = async () => await gate.AcquireAsync("key2");
        await act.Should().ThrowAsync<ConversationBusyException>()
            .WithMessage("*FailFast*");

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500),
            "FailFast 模式应立即失败，不排队等待");

        handle1.Dispose();
    }

    /// <summary>
    /// 取消传播：等待被外部取消时不泄漏许可。
    /// </summary>
    [Fact]
    public async Task AcquireAsync_Cancelled_ShouldNotLeakPermit()
    {
        var inner = new KeyedConversationGate();
        var gate = new GlobalConcurrencyGate(inner, maxConcurrentTurns: 1, turnQueueTimeoutSeconds: 0);

        var handle1 = await gate.AcquireAsync("key1");

        // 第二次用 CTS 取消
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var act = async () => await gate.AcquireAsync("key2", cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        // 释放 handle1 后应能立即获得空位（许可未泄漏）
        handle1.Dispose();
        var handle3 = await gate.AcquireAsync("key3");
        handle3.Should().NotBeNull("取消后许可未泄漏——释放后可立即获取");
        handle3.Dispose();
    }

    /// <summary>
    /// 内层闸门先于全局信号量释放（逆序释放）。
    /// </summary>
    [Fact]
    public async Task Dispose_ShouldReleaseInnerFirstThenGlobal()
    {
        var inner = new KeyedConversationGate();
        var gate = new GlobalConcurrencyGate(inner, maxConcurrentTurns: 1, turnQueueTimeoutSeconds: 5);

        var handle = await gate.AcquireAsync("key1");
        handle.Dispose();

        // 释放后全局信号量应恢复
        // 再次获取应立即成功（证明全局信号量已释放）
        var handle2 = await gate.AcquireAsync("key1");
        handle2.Should().NotBeNull("全局信号量释放后应立即可获取");
        handle2.Dispose();
    }

    /// <summary>
    /// 守卫：GlobalConcurrencyGate 实现 IConversationGate 接口（装饰器模式）。
    /// </summary>
    [Fact]
    public void GlobalConcurrencyGate_ShouldImplementIConversationGate()
    {
        typeof(GlobalConcurrencyGate)
            .Should().Implement<IConversationGate>(
                "GlobalConcurrencyGate 实现 IConversationGate 接口作为装饰器，事件入口零改动");
    }

    /// <summary>
    /// 守卫：BusyModes 枚举有且仅有两个值。
    /// </summary>
    [Fact]
    public void BusyModes_ShouldHaveExactlyTwoValues()
    {
        Enum.GetValues<BusyModes>().Should().HaveCount(2,
            "BusyModes 必须为 2 值闭集（WaitForSlot / FailFast）");
    }
}
