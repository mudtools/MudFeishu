// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// P1-1 / M3 回归测试：背压前移到接收路径，且<b>租约绝不泄漏</b>。
/// </summary>
/// <remarks>
/// 改造前：<c>HandleReceivedMessageAsync</c> 先分配消息副本、后在 <c>Task.Run</c> 内部取租约，
/// 导致"已排队任务 + 已排队缓冲"无上界（单帧上限 10MB）。
/// <para>
/// 改造后：租约在接收路径获取 → 槽位耗尽时接收循环被挂起（TCP 级反压）。
/// </para>
/// <para>
/// M3 守卫：<c>Task.Run</c> <b>不得</b>传入已取消的令牌——否则委托不会执行、租约永久泄漏，
/// 最终使接收管道彻底卡死。本类用"令牌预先取消"场景固化该约束。
/// </para>
/// </remarks>
[Trait("Category", "Concurrency")]
public class BackpressureTests
{
    private static readonly MethodInfo HandleReceivedMessageAsyncMethod =
        typeof(FeishuWebSocketClient).GetMethod("HandleReceivedMessageAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly PropertyInfo AvailableCountProperty =
        typeof(FeishuWebSocketConcurrencyService).GetProperty("AvailableCount",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static int GetAvailableCount(FeishuWebSocketConcurrencyService service)
        => (int)AvailableCountProperty.GetValue(service)!;

    private static FeishuWebSocketConcurrencyService CreateConcurrencyService(int maxConcurrent)
    {
        // 使用真实 OptionsMonitor（OnChange 是扩展方法，无法用 Moq 设置）
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(o => o.MaxConcurrentHandlers = maxConcurrent);
        var optionsMonitor = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        return new FeishuWebSocketConcurrencyService(
            optionsMonitor, NullLogger<FeishuWebSocketConcurrencyService>.Instance);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - startTime > timeout)
                throw new TimeoutException("等待条件超时");

            await Task.Delay(20);
        }
    }

    private static FeishuWebSocketClient CreateClient(FeishuWebSocketConcurrencyService? concurrencyService)
    {
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock.Setup(x => x.GetHandler(It.IsAny<string>())).Returns(Mock.Of<IFeishuEventHandler>());
        factoryMock.Setup(x => x.GetHandlers(It.IsAny<string>()))
            .Returns(new List<IFeishuEventHandler>());

        return new FeishuWebSocketClient(
            NullLogger<FeishuWebSocketClient>.Instance,
            factoryMock.Object,
            NullLoggerFactory.Instance,
            options: new FeishuWebSocketOptions(),
            concurrencyService: concurrencyService);
    }

    private static Task InvokeHandleReceivedMessageAsync(
        FeishuWebSocketClient client, string json, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(payload);
        var result = new WebSocketReceiveResult(payload.Length, WebSocketMessageType.Text, true);

        return (Task)HandleReceivedMessageAsyncMethod
            .Invoke(client, new object[] { buffer, result, cancellationToken })!;
    }

    [Fact]
    public async Task HandleReceivedMessageAsync_ShouldBeBlocked_WhenAllSlotsExhausted()
    {
        // Arrange：并发上界 = 1，并先占满唯一槽位
        var service = CreateConcurrencyService(maxConcurrent: 1);
        var client = CreateClient(service);
        var holdingLease = await service.AcquireAsync();

        try
        {
            // Act
            var blocked = InvokeHandleReceivedMessageAsync(client, "{\"type\":\"event_callback\"}", CancellationToken.None);
            var completed = await Task.WhenAny(blocked, Task.Delay(500));

            // Assert：槽位耗尽时接收路径必须被阻塞（背压已前移到接收路径）
            completed.Should().NotBeSameAs(blocked,
                "P1-1：接收路径必须等待并发租约，而不是把无界数量的任务与副本排入队列");

            holdingLease.Dispose();

            var afterRelease = await Task.WhenAny(blocked, Task.Delay(5000));
            afterRelease.Should().BeSameAs(blocked, "槽位释放后接收路径必须恢复");

            // 租约不得泄漏：处理任务在后台完成并归还槽位（异步，需等待）
            await WaitUntilAsync(() => GetAvailableCount(service) == 1, TimeSpan.FromSeconds(5));
        }
        finally
        {
            holdingLease.Dispose();
        }
    }

    [Fact]
    public async Task HandleReceivedMessageAsync_ShouldNotLeakLease_WhenTokenAlreadyCancelled()
    {
        // Arrange
        var service = CreateConcurrencyService(maxConcurrent: 2);
        var client = CreateClient(service);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        // Act：令牌已取消
        var act = () => InvokeHandleReceivedMessageAsync(client, "{\"type\":\"event_callback\"}", cancelled.Token);

        // Assert：必须"静默丢弃本帧"且不抛异常
        await act.Should().NotThrowAsync();

        // M3 守卫：取消路径不得留下未归还的租约（否则后续帧会被永久阻塞）
        GetAvailableCount(service).Should().Be(2, "被取消的帧不得占用/泄漏槽位");
    }

    [Fact]
    public async Task HandleReceivedMessageAsync_ShouldNotBlock_WhenConcurrencyServiceNotInjected()
    {
        // Arrange：未注入并发服务（增量 DI / Build() 缺省路径）→ 行为必须与改造前一致（不做并发记账）
        var client = CreateClient(concurrencyService: null);

        // Act
        var task = InvokeHandleReceivedMessageAsync(client, "{\"type\":\"event_callback\"}", CancellationToken.None);
        var completed = await Task.WhenAny(task, Task.Delay(1000));

        // Assert
        completed.Should().BeSameAs(task, "未注入并发服务时不得引入任何阻塞");
        await task;
    }

    [Fact]
    public async Task HandleReceivedMessageAsync_ShouldNotBlock_WhenMaxConcurrentHandlersIsZero()
    {
        // Arrange：MaxConcurrentHandlers = 0 表示"不限制并发"（信号量上界退化为 int.MaxValue），
        // 接收路径不得被阻塞——与未注入并发服务的行为保持一致
        var service = CreateConcurrencyService(maxConcurrent: 0);
        var client = CreateClient(service);

        // Act
        var task = InvokeHandleReceivedMessageAsync(client, "{\"type\":\"event_callback\"}", CancellationToken.None);
        var completed = await Task.WhenAny(task, Task.Delay(1000));

        // Assert
        completed.Should().BeSameAs(task, "MaxConcurrentHandlers=0 表示不限制并发，接收路径不得被阻塞");
        await task;
    }
}
