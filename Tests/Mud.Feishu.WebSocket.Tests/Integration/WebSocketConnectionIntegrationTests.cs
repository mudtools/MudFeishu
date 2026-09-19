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
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.WebSocket.SocketEventArgs;

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
                EnableLogging = false,
                AllowInsecureWebSocket = true,
                ConnectionTimeoutMs = 3000
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
}

#endif
