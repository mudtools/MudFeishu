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

    /// <summary>
    /// 连接生命周期事件的"双锁纪律"（§7.2 #23/#24）：回调内回调加锁 API 不得死锁。
    /// </summary>
    /// <remarks>
    /// 与 <c>P0P1FixRegressionTests</c> 中"原子占位"用例的区别：本组守护的是**锁纪律对外契约**
    /// （P0-5：所有用户事件在锁外触发），而不是事件次数。
    /// <para>
    /// <b>为什么必须同步阻塞在回调内</b>：若事件在 <c>_connectionLock</c> 持有期内触发，
    /// 回调内发起的 <c>DisconnectAsync</c> 会在同一把不可重入信号量上无限等待——
    /// 只要回调<b>不阻塞</b>（例如 <c>Task.Run</c> 后立即返回），锁就会被释放，缺陷便无法被观测。
    /// 用 <c>.GetAwaiter().GetResult()</c> 同步等待，加上外层 <c>WaitAsync(超时)</c>，
    /// 使"死锁"表现为**超时失败**而不是永久挂起。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Connected_Handler_ShouldNotDeadlock_WhenCallingDisconnectAsync()
    {
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var manager = CreateManager();

        var callbackCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.Connected += (_, _) =>
        {
            try
            {
                manager.DisconnectAsync().GetAwaiter().GetResult();
                callbackCompleted.TrySetResult(true);
            }
            catch (Exception ex)
            {
                callbackCompleted.TrySetException(ex);
            }
        };

        try
        {
            // Act：若事件在持锁期内触发，ConnectAsync 会因回调死锁而永不返回
            var connect = manager.ConnectAsync(server.Url);
            await connect.WaitAsync(WaitTimeout);

            await WaitAsync(callbackCompleted, "Connected 回调内的 DisconnectAsync 必须能完成（事件在锁外触发）");

            manager.ConnectionCount.Should().Be(0);
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    /// <summary>
    /// <c>Disconnected</c> 回调内调用 <c>SendMessageAsync</c> 不得死锁（发送锁与生命周期锁解耦）。
    /// </summary>
    [Fact]
    public async Task Disconnected_Handler_ShouldNotDeadlock_WhenCallingSendMessageAsync()
    {
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var manager = CreateManager();

        var callbackCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.Disconnected += (_, _) =>
        {
            try
            {
                manager.SendMessageAsync("{\"type\":\"probe\"}").GetAwaiter().GetResult();
                callbackCompleted.TrySetResult(true);
            }
            catch (InvalidOperationException)
            {
                // 连接已关闭 ⇒ 发送被**确定性拒绝**（"WebSocket未连接，无法发送消息"），
                // 这正是期望的"发送锁与生命周期锁解耦"表现：拒绝而不是在锁上无限等待。
                callbackCompleted.TrySetResult(true);
            }
            catch (Exception ex)
            {
                callbackCompleted.TrySetException(ex);
            }
        };

        try
        {
            await manager.ConnectAsync(server.Url);
            await manager.DisconnectAsync().WaitAsync(WaitTimeout);

            await WaitAsync(callbackCompleted,
                "Disconnected 回调内的 SendMessageAsync 必须能返回（_sendLock 与 _connectionLock 解耦）");
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    /// <summary>
    /// <c>Disconnected</c> 回调内再次调用 <c>DisconnectAsync</c> 不得死锁（幂等重入）。
    /// </summary>
    [Fact]
    public async Task Disconnected_Handler_ShouldNotDeadlock_WhenCallingDisconnectAsync()
    {
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var manager = CreateManager();

        var callbackCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var disconnectCount = 0;
        manager.Disconnected += (_, _) =>
        {
            if (Interlocked.Increment(ref disconnectCount) > 1)
            {
                return;   // 防止重入自身无限递归
            }

            try
            {
                manager.DisconnectAsync().GetAwaiter().GetResult();
                callbackCompleted.TrySetResult(true);
            }
            catch (Exception ex)
            {
                callbackCompleted.TrySetException(ex);
            }
        };

        try
        {
            await manager.ConnectAsync(server.Url);
            await manager.DisconnectAsync().WaitAsync(WaitTimeout);

            await WaitAsync(callbackCompleted, "Disconnected 回调内的 DisconnectAsync 必须幂等返回而不死锁");
            manager.ConnectionCount.Should().Be(0, "重复断开不得把连接计数减为负数");
        }
        finally
        {
            await manager.DisposeAsync();
        }
    }

    /// <summary>
    /// 断线竞态下连接计数不得为负（§7.2 #26）。
    /// </summary>
    /// <remarks>
    /// 构造：服务端<b>不应答关闭握手</b>（<see cref="LoopbackWebSocketServer.ServerMode.IgnoreCloseHandshake"/>），
    /// 客户端主动断开会在关闭握手超时后走 <c>Abort</c>；同时接收循环因 socket 中止抛出
    /// <c>WebSocketException</c> ⇒ **"主动断开"与"接收异常"同时尝试占位**。
    /// 必须恰好一次声明、计数归零（不得因两条路径各减一次而变成 <c>-1</c>）。
    /// </remarks>
    [Fact]
    public async Task ConnectionCount_ShouldNotGoNegative_WhenDisconnectRacesWithReceiveError()
    {
        await using var server = LoopbackWebSocketServer.Start(
            LoopbackWebSocketServer.ServerMode.IgnoreCloseHandshake);

        var manager = CreateManager();
        var closeArgs = new List<WebSocketCloseEventArgs>();
        manager.Disconnected += (_, e) => { lock (closeArgs) { closeArgs.Add(e); } };

        var minObserved = int.MaxValue;
        var sampling = true;

        try
        {
            await manager.ConnectAsync(server.Url);
            var receiveTask = manager.StartReceivingAsync((_, _) => Task.CompletedTask);

            var sampler = Task.Run(async () =>
            {
                while (Volatile.Read(ref sampling))
                {
                    var count = manager.ConnectionCount;
                    if (count < Volatile.Read(ref minObserved))
                    {
                        Volatile.Write(ref minObserved, count);
                    }

                    await Task.Delay(5);
                }
            });

            // Act：主动断开（握手超时后 Abort）与接收异常竞态
            await manager.DisconnectAsync().WaitAsync(WaitTimeout);
            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(2)));

            Volatile.Write(ref sampling, false);
            await sampler;

            // Assert
            manager.ConnectionCount.Should().Be(0);
            Volatile.Read(ref minObserved).Should().BeGreaterThanOrEqualTo(0,
                "连接计数是【是否已声明断线】的对外可观测事实：任何路径重复递减都会让它变成 -1，" +
                "进而使后续的 IsConnected/统计口径全部失真");
            lock (closeArgs)
            {
                closeArgs.Should().ContainSingle("断线声明的原子占位必须让竞态下的两条路径只生效一次");
            }
        }
        finally
        {
            Volatile.Write(ref sampling, false);
            await manager.DisposeAsync();
        }
    }

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

    /// <summary>
    /// P0-1 的真僵尸形态：取消发生在**派发帧期间**（接收循环正 await 消息处理器），
    /// 循环以"条件不成立"自然退出，socket **仍为 Open** ⇒ 必须补发断线声明（I13）。
    /// </summary>
    /// <remarks>
    /// 与"取消发生在 ReceiveAsync 期间"的区别（实测结论）：
    /// <list type="bullet">
    /// <item>取消在 <c>ReceiveAsync</c> 期间 → <c>ClientWebSocket</c> 以 Abort 中止底层连接
    /// ⇒ <c>State = Aborted</c> ⇒ <c>IsConnected</c> 为 false，上层健康检查（默认 60 秒轮询）能兜底；</item>
    /// <item>取消在**派发帧期间** → 循环自然退出而 <c>State = Open</c> ⇒ <c>IsConnected</c> 恒为 true、
    /// 健康检查判 Healthy、重连永不触发 ⇒ **彻底静默的僵尸连接**。</item>
    /// </list>
    /// 背压（慢处理器）会显著放大后者的窗口，因此这条契约必须在"派发帧卡住"时成立。
    /// </remarks>
    [Fact]
    public async Task ReceiveLoop_ShouldRaiseDisconnected_WhenCancelledWhileDispatchingFrame()
    {
        // Arrange
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var manager = CreateManager();
        var closeArgs = new List<WebSocketCloseEventArgs>();
        manager.Disconnected += (_, e) => { lock (closeArgs) { closeArgs.Add(e); } };

        using var loopCts = new CancellationTokenSource();
        using var handlerEntered = new ManualResetEventSlim(false);
        using var releaseHandler = new ManualResetEventSlim(false);

        try
        {
            await manager.ConnectAsync(server.Url);

            var receiveTask = manager.StartReceivingAsync((_, _) =>
            {
                handlerEntered.Set();
                // 让接收循环停留在"派发帧"阶段（而非阻塞在 ReceiveAsync 上）
                releaseHandler.Wait(TimeSpan.FromSeconds(5));
                return Task.CompletedTask;
            }, loopCts.Token);

            await server.SendTextAsync("{\"type\":\"probe\"}");
            await WaitUntilAsync(() => handlerEntered.IsSet,
                "前置条件：消息处理器必须已进入（接收循环正停留在派发帧阶段）");

            // Act：在派发阶段取消 → 循环条件不再成立，socket 未被 Abort
            loopCts.Cancel();
            releaseHandler.Set();

            // Assert
            await WaitUntilAsync(
                () => { lock (closeArgs) { return closeArgs.Count == 1; } },
                "P0-1/I13：取消导致的自然退出必须补发断线声明（改造前此处完全静默）");

            lock (closeArgs)
            {
                closeArgs[0].CloseStatusDescription.Should().Contain("接收循环被取消");
                closeArgs[0].CloseStatus.Should().Be(WebSocketCloseStatus.NormalClosure);
            }

            manager.IsConnected.Should().BeTrue(
                "socket 未被 Abort —— 这正是真僵尸态的特征：所有基于 State 的判定都会认为连接正常");
            manager.ConnectionCount.Should().Be(0, "断线声明必须完成原子占位");

            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(2)));
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
