// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Net.WebSockets;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// P2-10 回归测试：认证闸门不得阻塞接收循环。
/// </summary>
/// <remarks>
/// 改造前认证闸门（<see cref="FeishuWebSocketOptions.AuthGateTimeoutMs"/> &gt; 0 时）在<b>接收循环线程</b>上
/// 轮询等待认证完成，最长阻塞闸门时长；期间后续帧（含 Pong 控制帧与认证响应）全部被推迟，形成队头阻塞。
/// <para>
/// 改造后闸门判断位于 <c>Task.Run</c> 任务体内：接收循环恒速抽帧，等待只发生在处理任务上。
/// </para>
/// </remarks>
[Trait("Category", "Concurrency")]
public class FeishuWebSocketClientAuthGateTests
{
    private static readonly MethodInfo HandleReceivedMessageAsyncMethod =
        typeof(FeishuWebSocketClient).GetMethod("HandleReceivedMessageAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static FeishuWebSocketClient CreateClient(int authGateTimeoutMs)
    {
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock.Setup(x => x.GetHandler(It.IsAny<string>())).Returns(Mock.Of<IFeishuEventHandler>());
        factoryMock.Setup(x => x.GetHandlers(It.IsAny<string>()))
            .Returns(new List<IFeishuEventHandler>());

        return new FeishuWebSocketClient(
            NullLogger<FeishuWebSocketClient>.Instance,
            factoryMock.Object,
            NullLoggerFactory.Instance,
            options: new FeishuWebSocketOptions
            {
                AuthGateTimeoutMs = authGateTimeoutMs
            });
    }

    private static Task InvokeHandleBinaryFrameAsync(FeishuWebSocketClient client)
    {
        var payload = new byte[] { 8, 1, 2 };
        var buffer = new ArraySegment<byte>(payload);
        var result = new WebSocketReceiveResult(payload.Length, WebSocketMessageType.Binary, true);

        return (Task)HandleReceivedMessageAsyncMethod
            .Invoke(client, new object[] { buffer, result, CancellationToken.None })!;
    }

    [Fact]
    public async Task HandleReceivedMessageAsync_ShouldNotBlockReceiveLoop_WhenAuthGateEnabled()
    {
        // Arrange：闸门 800ms —— 若闸门仍在接收循环上等待，本次调用会被阻塞 800ms
        var client = CreateClient(authGateTimeoutMs: 800);

        // Act
        var stopwatch = Stopwatch.StartNew();
        await InvokeHandleBinaryFrameAsync(client);
        stopwatch.Stop();

        // Assert
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(300),
            "P2-10：闸门等待必须在任务体内进行，接收循环不得被阻塞（队头阻塞）");
    }

    [Fact]
    public async Task HandleReceivedMessageAsync_ShouldNotRaiseError_WhenAuthGateTimesOut()
    {
        // Arrange
        var client = CreateClient(authGateTimeoutMs: 150);
        var errors = 0;
        client.Error += (_, _) => Interlocked.Increment(ref errors);

        // Act：未认证 → 闸门超时后丢弃该帧（不得进入处理链路）
        await InvokeHandleBinaryFrameAsync(client);
        await Task.Delay(500);

        // Assert
        errors.Should().Be(0, "闸门丢弃属受控行为，不得触发 Error 事件");
    }

    [Fact]
    public async Task HandleReceivedMessageAsync_ShouldNotBlock_WhenAuthGateDisabled()
    {
        // Arrange：默认 0 = 关闭闸门（保持历史行为）
        var client = CreateClient(authGateTimeoutMs: 0);

        // Act
        var stopwatch = Stopwatch.StartNew();
        await InvokeHandleBinaryFrameAsync(client);
        stopwatch.Stop();

        // Assert
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(300));
    }
}
