// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// P1-6 回归测试：装配路径的背压由"异步槽位"表达，且槽位在"未派发任务"时由本次调用归还。
/// </summary>
/// <remarks>
/// 改造前在<b>同时持有</b> <c>_binaryDataStreamLock</c> 与 <c>_processLock</c> 时执行
/// <c>Task.WaitAny(snapshot, 200)</c> 同步阻塞：既占用线程池又卡住后续帧装配。
/// <para>
/// 改造后：<c>ProcessBinaryDataAsync</c> 以 <c>await</c> 方式获取槽位；若派发了完整消息处理任务，
/// 槽位所有权移交该任务，否则（分片累积/超限丢弃/异常）由本次调用归还。
/// </para>
/// </remarks>
[Trait("Category", "Concurrency")]
public class BinaryMessageProcessorConcurrencyTests
{
    private const int MaxActiveProcessingTasks = 1024;

    private static readonly FieldInfo ProcessingSlotsField =
        typeof(BinaryMessageProcessor).GetField("_processingSlots", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static SemaphoreSlim GetSlots(BinaryMessageProcessor processor)
        => (SemaphoreSlim)ProcessingSlotsField.GetValue(processor)!;

    private static BinaryMessageProcessor CreateProcessor()
    {
        var options = new FeishuWebSocketOptions { EnableLogging = false };
        var manager = new WebSocketConnectionManager(
            NullLogger<WebSocketConnectionManager>.Instance, options, NullLoggerFactory.Instance);
        var router = new MessageRouter(NullLogger<MessageRouter>.Instance, options);

        return new BinaryMessageProcessor(
            NullLogger<BinaryMessageProcessor>.Instance, manager, options, router);
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldReleaseSlot_WhenMessageIsFragment()
    {
        // Arrange
        var processor = CreateProcessor();

        // Act：非结束片（仅累积，不派发任务）
        await processor.ProcessBinaryDataAsync(new byte[] { 1, 2, 3 }, 0, 3, endOfMessage: false);

        // Assert：本次调用必须归还槽位，否则持续分片会耗尽整个闸门
        GetSlots(processor).CurrentCount.Should().Be(MaxActiveProcessingTasks,
            "分片累积路径必须归还槽位（不得泄漏）");
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldReturnSlot_WhenCompleteMessageDispatched()
    {
        // Arrange
        var processor = CreateProcessor();

        // Act：结束片 → 派发后台处理任务，槽位由该任务归还
        await processor.ProcessBinaryDataAsync(new byte[] { 8, 1, 2 }, 0, 3, endOfMessage: true);

        // Assert
        var stopwatch = Stopwatch.StartNew();
        while (GetSlots(processor).CurrentCount < MaxActiveProcessingTasks)
        {
            if (stopwatch.Elapsed > TimeSpan.FromSeconds(5))
                throw new TimeoutException("后台处理任务未归还槽位（疑似槽位泄漏）");

            await Task.Delay(20);
        }
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldNotBlockCaller_WhenSlotsExhausted()
    {
        // Arrange：占满全部槽位（模拟 1024 个在途处理）
        var processor = CreateProcessor();
        var slots = GetSlots(processor);
        var acquired = new List<Task>(MaxActiveProcessingTasks);
        for (var i = 0; i < MaxActiveProcessingTasks; i++)
        {
            acquired.Add(slots.WaitAsync());
        }

        await Task.WhenAll(acquired);

        // Act：必须"立即返回未完成的任务"，而不是同步阻塞调用线程
        Task? processing = null;
        var invokeTask = Task.Run(() =>
        {
            processing = processor.ProcessBinaryDataAsync(new byte[] { 1 }, 0, 1, endOfMessage: false);
        });

        var returned = await Task.WhenAny(invokeTask, Task.Delay(TimeSpan.FromSeconds(3)));

        // Assert
        returned.Should().BeSameAs(invokeTask,
            "P1-6：槽位耗尽时必须以异步方式等待（此前会 Task.WaitAny 同步阻塞线程与装配锁）");
        processing.Should().NotBeNull();
        processing!.IsCompleted.Should().BeFalse("槽位未释放，处理调用应保持等待状态");

        // 释放槽位后必须继续推进
        for (var i = 0; i < MaxActiveProcessingTasks; i++)
        {
            slots.Release();
        }

        await processing.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
