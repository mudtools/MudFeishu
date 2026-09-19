// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER

using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.SocketEventArgs;
using ProtoBuf;

namespace Mud.Feishu.WebSocket.Tests.Integration;

/// <summary>
/// W5 回环集成测试：以真实 WebSocket 握手覆盖只能在协议交互中验证的路径。
/// </summary>
/// <remarks>
/// 覆盖面：连接建立 → 服务端主动关闭（关闭码透传）→ 客户端主动关闭（旧连接已 Open 时新连接失败 → P1-3）
/// → 分片消息重组（P2-7 相关缓冲区语义）。
/// <para>
/// 需真实回环端口，故标记 <c>Category=Integration</c>；仅 <c>net8.0+</c> 编译。
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
public class WebSocketConnectionIntegrationTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private static WebSocketConnectionManager CreateManager() =>
        new(NullLogger<WebSocketConnectionManager>.Instance,
            new FeishuWebSocketOptions
            {
                Certificate = new WebSocketCertificateOptions { AllowInsecureWebSocket = true },
                ConnectionTimeoutMs = 3000,
                // P2-15：回环服务端主机需显式列入白名单（同时验证精确主机匹配路径）
                AllowedHostSuffixes = "127.0.0.1"
            },
            NullLoggerFactory.Instance);

    private static async Task<T> WaitAsync<T>(TaskCompletionSource<T> source, string because)
    {
        var completed = await Task.WhenAny(source.Task, Task.Delay(WaitTimeout));
        completed.Should().BeSameAs(source.Task, because);
        return await source.Task;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string because)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > WaitTimeout)
            {
                throw new TimeoutException($"等待条件超时：{because}");
            }

            await Task.Delay(20);
        }
    }

    [Fact]
    public async Task ConnectAsync_ShouldRaiseConnected_WhenHandshakeSucceeds()
    {
        // Arrange
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var manager = CreateManager();
        var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.Connected += (_, _) => connected.TrySetResult(true);

        try
        {
            // Act
            await manager.ConnectAsync(server.Url);

            // Assert
            await WaitAsync(connected, "真实握手成功后必须触发 Connected");
            manager.IsConnected.Should().BeTrue();
            manager.ConnectionCount.Should().Be(1);
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task StartReceivingAsync_ShouldReassembleFragmentedText_WhenServerSendsFragments()
    {
        // Arrange
        const string first = "{\"part\":\"1\",";
        const string second = "\"index\":2}";
        await using var server = LoopbackWebSocketServer.Start(
            LoopbackWebSocketServer.ServerMode.SendFragmentedText, first, second);

        var manager = CreateManager();
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await manager.ConnectAsync(server.Url);

            // Act：真实分片（endOfMessage=false + true）
            var receiveTask = manager.StartReceivingAsync(
                (buffer, result) =>
                {
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        received.TrySetResult(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
                    }

                    return Task.CompletedTask;
                });

            var payload = await WaitAsync(received, "分片消息必须被完整重组后交给回调");

            // Assert
            payload.Should().Be(first + second, "分片重组不得丢字节或错序");
            await manager.DisconnectAsync();
            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task HandleCloseMessageAsync_ShouldEchoServerCloseStatus_WhenServerCloses()
    {
        // Arrange
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.CloseImmediately);
        var manager = CreateManager();
        var disconnected = new TaskCompletionSource<WebSocketCloseEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.Disconnected += (_, e) => disconnected.TrySetResult(e);

        try
        {
            await manager.ConnectAsync(server.Url);
            var receiveTask = manager.StartReceivingAsync((_, _) => Task.CompletedTask);

            // Act
            var args = await WaitAsync(disconnected, "服务端主动关闭必须触发 Disconnected");

            // Assert
            args.IsServerInitiated.Should().BeTrue();
            args.CloseStatus.Should().Be(WebSocketCloseStatus.NormalClosure, "关闭状态码应来自服务端下发的关闭帧（P2-8）");
            await WaitUntilAsync(() => manager.ConnectionCount == 0, "断线后连接计数必须归零");
            manager.IsConnected.Should().BeFalse();

            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task ConnectAsync_ShouldRaiseDisconnectedAndThrow_WhenNewConnectionFailsAfterOldSocketWasOpen()
    {
        // Arrange：先与回环服务端建立真实连接（旧 socket 处于 Open）
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var manager = CreateManager();

        var disconnectedArgs = new List<WebSocketCloseEventArgs>();
        manager.Disconnected += (_, e) => disconnectedArgs.Add(e);

        var closedPort = LoopbackWebSocketServer.GetFreePort(); // 无监听者 → 连接必然被拒

        try
        {
            await manager.ConnectAsync(server.Url);
            manager.IsConnected.Should().BeTrue();

            // Act：连接到必然失败的端点（旧连接已 Open）
            var act = () => manager.ConnectAsync($"ws://127.0.0.1:{closedPort}/ws/");

            // Assert：异常必须照常抛出，且"旧连接已断开"必须送达订阅者（P1-3）
            await act.Should().ThrowAsync<Exception>();

            await WaitUntilAsync(() => disconnectedArgs.Count == 1,
                "P1-3：新连接握手失败时，旧连接的 Disconnected 事件不得丢失");

            disconnectedArgs[0].IsServerInitiated.Should().BeFalse("旧连接是客户端主动断开以重连");
            manager.ConnectionCount.Should().Be(0);
            manager.IsConnected.Should().BeFalse();
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    [Fact]
    public async Task Dispose_ShouldCompleteWithinBound_WhenServerNeverAcksCloseHandshake()
    {
        // Arrange（P2-11）：服务端读到关闭帧后不应答（保持 TCP 半开），
        // 客户端的关闭握手只能依靠超时 + Abort 收尾，不得留下悬挂的关闭任务。
        await using var server = LoopbackWebSocketServer.Start(
            LoopbackWebSocketServer.ServerMode.IgnoreCloseHandshake);
        var manager = CreateManager();
        await manager.ConnectAsync(server.Url);
        manager.IsConnected.Should().BeTrue();

        // Act：同步 Dispose（关闭握手将一直等不到服务端应答）
        var stopwatch = Stopwatch.StartNew();
        manager.Dispose();
        stopwatch.Stop();

        // Assert：CloseHandshakeTimeout=5s + Abort 兜底 → 必须在 10s 内确定性返回
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10),
            "关闭握手超时后必须 Abort 收尾，不得因服务端不应答而永久悬挂（P2-11）");
        // 说明：ConnectionCount 的递减由接收循环驱动的 NotifyDisconnected 负责，
        // 本用例未启动接收循环（P2-11 的契约是"超时+Abort 收尾、不留下未观察的关闭任务"），
        // 断言以 IsConnected 收口。
        manager.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldAckFailureOverWire_WhenSubscriberThrows()
    {
        // Arrange（P2-14）：BinaryMessageReceived 订阅者抛异常时仍必须回 ACK(500)——
        // 订阅者首次抛出会把处理流程带入 catch 分支，错误通知路径若再次抛出（同一订阅者
        // 必然再次抛出），异常会穿透并吞掉失败 ACK，退化为"服务端等超时后重投"。
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var manager = CreateManager();
        var options = new FeishuWebSocketOptions();
        var router = new MessageRouter(NullLogger<MessageRouter>.Instance, options);
        var processor = new BinaryMessageProcessor(
            NullLogger<BinaryMessageProcessor>.Instance, manager, options, router);

        try
        {
            await manager.ConnectAsync(server.Url);

            processor.BinaryMessageReceived += (_, _) =>
                throw new InvalidOperationException("订阅者处理失败");

            var frame = new EventProtoData
            {
                Service = 1001,
                Method = 1,
                SeqID = 42,
                PayloadType = "JSON",
                Payload = Encoding.UTF8.GetBytes("{\"type\":\"test\"}")
            };
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, frame);
            var data = stream.ToArray();

            // Act：投递一条有效 DATA 帧（与真实接收循环相同的入口）
            await processor.ProcessBinaryDataAsync(data, 0, data.Length, true, CancellationToken.None);

            var ackWait = await Task.WhenAny(server.BinaryFrameReceived, Task.Delay(WaitTimeout));
            ackWait.Should().BeSameAs(server.BinaryFrameReceived,
                "订阅者抛异常时仍必须发出失败 ACK，否则服务端只能等超时后重投（P2-14）");
            var ackBytes = await server.BinaryFrameReceived;

            // Assert：反序列化 ACK 帧，code 必须为 500（触发服务端即时重投）
            using var ackStream = new MemoryStream(ackBytes);
            var ackFrame = Serializer.Deserialize<EventProtoData>(ackStream);
            ackFrame.PayloadType.Should().Be("ack");
            ackFrame.SeqID.Should().Be(42, "ACK 帧必须回带原帧的 SeqID");

            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(ackFrame.Payload ?? Array.Empty<byte>()));
            doc.RootElement.GetProperty("code").GetInt32().Should().Be(500,
                "失败路径必须回 code=500 触发服务端重投，而不是静默丢弃");
        }
        finally
        {
            processor.Dispose();
            await manager.DisposeAsync();
        }
    }
}

#endif
