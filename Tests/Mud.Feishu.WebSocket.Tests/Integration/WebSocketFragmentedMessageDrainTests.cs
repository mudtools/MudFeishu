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
/// 分片超限的丢弃语义集成测试（R2 / WS2-03 / WS2-17）。
/// </summary>
/// <remarks>
/// 覆盖"只能靠真实协议交互验证"的语义：WebSocket 是消息边界化的帧协议，
/// 超限时**直接 return** 会让本条消息的剩余分片在下一轮 <c>ReceiveAsync</c> 中被当作**新消息**消费，
/// 污染消息边界与序号游标（<c>SequenceGapThreshold</c> 默认 0，没有任何跳跃检测能发现）。
/// <para>
/// 三类断言缺一不可：
/// <list type="number">
/// <item>超限消息本身**不得**被派发（避免把半个消息交给解析链路）；</item>
/// <item>排空之后**后续合法消息必须被完整派发**（证明边界未污染——这是本用例的核心）；</item>
/// <item>排空有上界：对端持续灌数据时必须转为主动断连（否则排空会无穷进行）。</item>
/// </list>
/// </para>
/// <para>需要用真实回环端口，标记 <c>Category=Integration</c>；仅 <c>net8.0+</c> 编译。</para>
/// </remarks>
[Trait("Category", "Integration")]
public class WebSocketFragmentedMessageDrainTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    /// <summary>客户端二进制上限（测试用极小值，使几 KB 的分片即可构造超限）。</summary>
    private const long TestMaxBinaryMessageSize = 4096;

    /// <summary>超过上限的分片大小（8192 &gt; 4096）。</summary>
    private const int OversizeFragmentSize = 8192;

    private static WebSocketConnectionManager CreateManager()
        => new(NullLogger<WebSocketConnectionManager>.Instance,
            new FeishuWebSocketOptions
            {
                Certificate = new WebSocketCertificateOptions { AllowInsecureWebSocket = true },
                ConnectionTimeoutMs = 3000,
                AllowedHostSuffixes = "127.0.0.1",
                MessageSizeLimits = new MessageSizeLimits
                {
                    MaxBinaryMessageSize = TestMaxBinaryMessageSize,
                    MaxTextMessageSize = 1024 * 1024
                }
            },
            NullLoggerFactory.Instance);

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

    /// <summary>
    /// 累积超限：客户端必须在写入前拦截并**排空至消息边界**，随后合法消息仍被完整派发。
    /// </summary>
    [Fact]
    public async Task HandleFragmentedMessageAsync_ShouldDrainUntilEndOfMessage_WhenSizeExceeded()
    {
        // Arrange：4 片，第 1 片 1KB（通过）、其后 8KB（累积超限）
        await using var server = LoopbackWebSocketServer.Start(
            LoopbackWebSocketServer.ServerMode.SendOversizeAccumulatedFragments,
            fragmentFirst: string.Empty,
            fragmentSecond: string.Empty,
            fragmentSize: OversizeFragmentSize,
            fragmentCount: 4);

        var manager = CreateManager();
        var dispatched = new List<string>();
        var errors = new List<WebSocketErrorEventArgs>();
        manager.Error += (_, e) => { lock (dispatched) { errors.Add(e); } };

        try
        {
            await manager.ConnectAsync(server.Url);

            // Act
            var receiveTask = manager.StartReceivingAsync((buffer, result) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    lock (dispatched)
                    {
                        dispatched.Add(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
                    }
                }

                return Task.CompletedTask;
            });

            await WaitUntilAsync(
                () => { lock (dispatched) { return dispatched.Count > 0; } },
                "排空超限消息后，后续合法消息必须被完整派发（证明消息边界未被污染）");

            // Assert
            lock (dispatched)
            {
                dispatched.Should().ContainSingle()
                    .Which.Should().Be(LoopbackWebSocketServer.PostDrainValidMessage,
                        "排空后到达的首条消息必须是服务端发出的合法消息——" +
                        "若超限消息的尾部被当作新消息消费，这里会变成乱码或被吞掉");
            }

            errors.Should().Contain(e => e.ErrorType == "FragmentSizeExceeded",
                "超限必须上报可区分于其它接收错误的类型（便于告警规则按类型区分'超限丢弃'）");

            manager.IsConnected.Should().BeTrue("排空后连接必须保持可用（优于直接断连）");

            await manager.DisconnectAsync();
            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    /// <summary>
    /// 首帧即超限：命中"首帧校验"分支，同样必须排空而非直接返回。
    /// </summary>
    /// <remarks>
    /// 该分支在默认配置下不可达（单次 <c>ReceiveAsync</c> 返回长度 ≤ 接收缓冲区 4096 &lt; 上限 1MB），
    /// 故用例显式收紧 <c>MaxBinaryMessageSize</c> 使其可达——这是 R1 承诺但未落地的用例。
    /// </remarks>
    [Fact]
    public async Task HandleFragmentedMessageAsync_ShouldRejectFirstFrame_WhenFirstFragmentExceedsLimit()
    {
        // Arrange：首片 8KB > 上限 4KB
        await using var server = LoopbackWebSocketServer.Start(
            LoopbackWebSocketServer.ServerMode.SendOversizeFirstFragment,
            fragmentFirst: string.Empty,
            fragmentSecond: string.Empty,
            fragmentSize: OversizeFragmentSize,
            fragmentCount: 3);

        var manager = CreateManager();
        var dispatched = new List<string>();
        var errors = new List<WebSocketErrorEventArgs>();
        manager.Error += (_, e) => { lock (dispatched) { errors.Add(e); } };

        try
        {
            await manager.ConnectAsync(server.Url);

            var receiveTask = manager.StartReceivingAsync((buffer, result) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    lock (dispatched)
                    {
                        dispatched.Add(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
                    }
                }

                return Task.CompletedTask;
            });

            await WaitUntilAsync(
                () => { lock (dispatched) { return dispatched.Count > 0; } },
                "首帧超限同样必须排空，后续合法消息必须被派发");

            lock (dispatched)
            {
                dispatched.Should().ContainSingle()
                    .Which.Should().Be(LoopbackWebSocketServer.PostDrainValidMessage);
            }

            errors.Should().Contain(e => e.ErrorType == "FragmentSizeExceeded");
            manager.IsConnected.Should().BeTrue();

            await manager.DisconnectAsync();
            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    /// <summary>
    /// 排空上界：对端持续投递永不结束的分片时，必须转为主动断连（D3 方案 B）。
    /// </summary>
    /// <remarks>
    /// 若排空无上界，一条"永不结束"的消息即可让客户端永久停留在排空循环中——既不再派发任何事件，
    /// 也不会触发断线重连（接收循环 Task 仍在运行 ⇒ 存活探针也看不出异常）。
    /// </remarks>
    [Fact]
    public async Task HandleFragmentedMessageAsync_ShouldAbortConnection_WhenDrainExceedsFrameBound()
    {
        // Arrange：1100 帧 × 1KB，永不设置 endOfMessage → 超过排空帧数上界（1024）
        await using var server = LoopbackWebSocketServer.Start(
            LoopbackWebSocketServer.ServerMode.SendNeverEndingFragments,
            fragmentFirst: string.Empty,
            fragmentSecond: string.Empty,
            fragmentSize: 1024,
            fragmentCount: 1100);

        var manager = CreateManager();
        var disconnected = new TaskCompletionSource<WebSocketCloseEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        manager.Disconnected += (_, e) => disconnected.TrySetResult(e);

        try
        {
            await manager.ConnectAsync(server.Url);

            var receiveTask = manager.StartReceivingAsync((_, _) => Task.CompletedTask);

            // Act
            var completed = await Task.WhenAny(disconnected.Task, Task.Delay(WaitTimeout));

            // Assert
            completed.Should().BeSameAs(disconnected.Task,
                "排空达到上界后必须主动 Abort，使接收循环以异常退出并触发断线/重连");

            manager.ConnectionCount.Should().Be(0, "断线声明必须完成占位（连接计数归零）");

            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }
}

#endif
