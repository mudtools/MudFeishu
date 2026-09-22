// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.DataModels.WsEndpoint;

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
        // 边界取闸门时长（800ms）的 3/4：闸门等待若被错误地放在调用方线程上，
        // 本次调用至少要阻塞 800ms ⇒ 必然超过 600ms；
        // 而正确实现是微秒级返回。CI 负载波动远小于该间隔（ubuntu-latest 上曾出现
        // 计时断言 3000ms 被测成 2999ms 的偏差，量级完全不同）。
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(600),
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
        // 闸门关闭时正确路径是微秒级返回；这里只需排除"被闸门等待阻塞"（秒级），
        // 不做紧边界（纯计时上界在 CI 负载下天然抖动）。强断言见下方回环用例 #18/#19。
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    // ────────────────────────────────────────────────────────────────────────
    // R2/WS2-15：以上三个用例只断言"耗时 < 300ms"与"无 Error"，属**弱断言**——
    // 它们无法区分"帧被按时丢弃"与"帧被静默吞掉/走了别的分支"。
    // 下面的集成用例用回环服务端的**可观测面**（是否回 ACK）做正/负对照。
    //
    // 说明（为什么不能用 Mock<MessageRouter> 断言"未被调用"）：
    //   · MessageRouter.RouteMessageAsync 不是 virtual ⇒ Moq 无法拦截（Verify 会抛 NotSupportedException）；
    //   · 且 FeishuWebSocketClient 在**构造函数内** new MessageRouter(...) ⇒ 没有注入点。
    // 因此改用"服务端是否收到 ACK 帧"作为"帧是否进入处理链路"的端到端证据。
    // ────────────────────────────────────────────────────────────────────────
#if NET8_0_OR_GREATER

    private static async Task<byte[]?> TryObserveAckAsync(Mud.Feishu.WebSocket.Tests.Integration.LoopbackWebSocketServer server)
        => await server.TryWaitBinaryFrameAsync(TimeSpan.FromSeconds(1));

    private static byte[] BuildProbeFrame()
    {
        var frame = new EventProtoData
        {
            Service = 1001,
            Method = 1,
            SeqID = 7,
            PayloadType = "JSON",
            Payload = Encoding.UTF8.GetBytes("{\"type\":\"probe\"}")
        };

        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, frame);
        return stream.ToArray();
    }

    private static FeishuWebSocketClient CreateLoopbackClient(int authGateTimeoutMs)
    {
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock.Setup(x => x.GetHandler(It.IsAny<string>())).Returns(Mock.Of<IFeishuEventHandler>());
        factoryMock.Setup(x => x.GetHandlers(It.IsAny<string>())).Returns(new List<IFeishuEventHandler>());

        return new FeishuWebSocketClient(
            NullLogger<FeishuWebSocketClient>.Instance,
            factoryMock.Object,
            NullLoggerFactory.Instance,
            options: new FeishuWebSocketOptions
            {
                AuthGateTimeoutMs = authGateTimeoutMs,
                Certificate = new WebSocketCertificateOptions { AllowInsecureWebSocket = true },
                AllowedHostSuffixes = "127.0.0.1",
                ConnectionTimeoutMs = 3000
            });
    }

    /// <summary>
    /// 闸门超时 ⇒ 二进制帧**不得进入处理链路**（端到端证据：服务端收不到 ACK）。
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleReceivedMessageAsync_ShouldDropFrame_WhenAuthGateTimesOut()
    {
        // Arrange：未认证 + 闸门 300ms
        await using var server = Mud.Feishu.WebSocket.Tests.Integration.LoopbackWebSocketServer.Start(
            Mud.Feishu.WebSocket.Tests.Integration.LoopbackWebSocketServer.ServerMode.Idle);

        var client = CreateLoopbackClient(authGateTimeoutMs: 300);
        var binaryMessageReceived = 0;
        client.BinaryMessageReceived += (_, _) => Interlocked.Increment(ref binaryMessageReceived);

        try
        {
            await client.ConnectAsync(new WsEndpointResult { Url = server.Url });
            client.IsAuthenticated.Should().BeFalse("前置条件：本用例不执行认证");

            // Act：由服务端投递一条合法二进制帧
            await server.SendBinaryAsync(BuildProbeFrame());

            // Assert：闸门超时后丢弃 ⇒ 无 ACK、无事件
            var ack = await TryObserveAckAsync(server);
            ack.Should().BeNull("闸门超时属受控丢弃：帧不得进入处理链路（处理链路成功处理会回 ACK）");
            Volatile.Read(ref binaryMessageReceived).Should().Be(0, "被丢弃的帧不得产生 BinaryMessageReceived 事件");
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    /// <summary>
    /// 闸门关闭（默认 0）⇒ 二进制帧**正常进入处理链路**（端到端证据：服务端收到 ACK）。
    /// </summary>
    /// <remarks>正对照：证明上一条用例的"无 ACK"确实来自闸门丢弃，而不是环境导致 ACK 永远收不到。</remarks>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleReceivedMessageAsync_ShouldRouteFrame_WhenAuthGateDisabled()
    {
        await using var server = Mud.Feishu.WebSocket.Tests.Integration.LoopbackWebSocketServer.Start(
            Mud.Feishu.WebSocket.Tests.Integration.LoopbackWebSocketServer.ServerMode.Idle);

        var client = CreateLoopbackClient(authGateTimeoutMs: 0);

        try
        {
            await client.ConnectAsync(new WsEndpointResult { Url = server.Url });

            // Act
            await server.SendBinaryAsync(BuildProbeFrame());

            // Assert
            var ack = await TryObserveAckAsync(server);
            ack.Should().NotBeNull("闸门关闭时帧必须进入处理链路并回 ACK（否则服务端只能等超时重投）");

            using var ackStream = new MemoryStream(ack!);
            var ackFrame = ProtoBuf.Serializer.Deserialize<EventProtoData>(ackStream);
            ackFrame.PayloadType.Should().Be("ack");
            ackFrame.SeqID.Should().Be(7, "ACK 必须回带原帧 SeqID");
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

#endif
}
