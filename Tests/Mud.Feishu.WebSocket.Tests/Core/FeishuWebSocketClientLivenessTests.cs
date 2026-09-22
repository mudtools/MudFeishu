// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER

using System.Diagnostics;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.SocketEventArgs;
using Mud.Feishu.WebSocket.Tests.Integration;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// 连接生命周期与存活契约的集成测试（R2 / P0-1 / P1-1 / P2-2 / WS2-07 / WS2-08）。
/// </summary>
/// <remarks>
/// 用真实回环握手覆盖"只能在协议交互中验证"的四条契约：
/// <list type="number">
/// <item><b>I15 / D2</b>：调用方令牌<b>不构成连接生命周期</b>——连接成功后再取消它，连接仍可用且能收帧；</item>
/// <item><b>I13 / P0-1</b>：接收循环因取消退出而 socket 仍为 <c>Open</c> 时，必须产生且仅产生一次断线声明；</item>
/// <item><b>I12 / P2-2</b>：<c>IsConnected</c> 必须反映"连接可用来收事件"，僵尸态下为 <c>false</c>（且存活探针报僵尸）；</item>
/// <item><b>I14 / P1-1</b>：重复调用 <c>StartReceivingAsync</c> 不得创建第二条接收循环。</item>
/// </list>
/// <para>需真实回环端口，标记 <c>Category=Integration</c>；仅 <c>net8.0+</c> 编译。</para>
/// </remarks>
[Trait("Category", "Integration")]
public class FeishuWebSocketClientLivenessTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private static readonly FieldInfo CancellationTokenSourceField =
        typeof(FeishuWebSocketClient).GetField("_cancellationTokenSource", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo ReceiveTaskField =
        typeof(FeishuWebSocketClient).GetField("_receiveTask", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo ConnectionManagerField =
        typeof(FeishuWebSocketClient).GetField("_connectionManager", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo LastReceiveTicksField =
        typeof(FeishuWebSocketClient).GetField("_lastReceiveTicks", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static FeishuWebSocketClient CreateClient(
        int maxConcurrentHandlers = 0,
        FeishuWebSocketConcurrencyService? concurrencyService = null)
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
                Certificate = new WebSocketCertificateOptions { AllowInsecureWebSocket = true },
                AllowedHostSuffixes = "127.0.0.1",
                ConnectionTimeoutMs = 3000,
                MaxConcurrentHandlers = maxConcurrentHandlers
            },
            concurrencyService: concurrencyService);
    }

    /// <summary>
    /// 构造"仅允许 1 个并发租约"的并发服务（用于制造确定性背压，见存活态用例）。
    /// </summary>
    private static FeishuWebSocketConcurrencyService CreateSingleSlotConcurrencyService()
    {
        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(o => o.MaxConcurrentHandlers = 1);
        var optionsMonitor = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();

        return new FeishuWebSocketConcurrencyService(
            optionsMonitor, NullLogger<FeishuWebSocketConcurrencyService>.Instance);
    }

    /// <summary>
    /// 读取私有 <c>_lastReceiveTicks</c>（仅用于"本帧是否已被接收循环取走"的时序判定，不做精确同步）。
    /// </summary>
    private static long ReadLastReceiveTicks(FeishuWebSocketClient client)
        => (long)LastReceiveTicksField.GetValue(client)!;


    private static WsEndpointResult Endpoint(LoopbackWebSocketServer server)
        => new() { Url = server.Url };

    private static WebSocketConnectionManager GetConnectionManager(FeishuWebSocketClient client)
        => (WebSocketConnectionManager)ConnectionManagerField.GetValue(client)!;

    private static Task? GetReceiveTask(FeishuWebSocketClient client)
        => (Task?)ReceiveTaskField.GetValue(client);

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

    #region I15 / D2：调用方令牌只约束建连

    /// <summary>
    /// 连接成功后取消调用方令牌：连接必须保持可用，且仍能收到文本帧。
    /// </summary>
    /// <remarks>
    /// 改造前令牌被链接为接收循环与心跳的生命周期令牌 ⇒ 取消即"socket 仍 Open 但不再读帧、
    /// 无任何断线通知、健康检查仍报 Healthy"的僵尸连接（P0-1 的触发路径之一）。
    /// <para>
    /// 断言不只"IsConnected 仍为 true"（那可能只是字段没更新），而是**真的再收一帧**——
    /// 这是"接收管道仍活着"的端到端证据。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ConnectAsync_ShouldNotBindConnectionLifetime_ToCallerToken()
    {
        // Arrange
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var client = CreateClient();

        var disconnectedCount = 0;
        client.Disconnected += (_, _) => Interlocked.Increment(ref disconnectedCount);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.MessageReceived += (_, e) => received.TrySetResult(e.Message);

        using var callerCts = new CancellationTokenSource();

        try
        {
            await client.ConnectAsync(Endpoint(server), callerCts.Token);
            client.IsConnected.Should().BeTrue("前置条件：握手成功且接收循环已启动");

            // Act：取消调用方令牌（旧实现会静默停掉接收循环）
            callerCts.Cancel();
            await Task.Delay(200);

            // Assert ①：连接生命周期不受调用方令牌影响
            client.IsConnected.Should().BeTrue(
                "I15/D2：调用方令牌只约束建连阶段，不得构成连接生命周期");
            disconnectedCount.Should().Be(0, "调用方令牌取消不得产生断线声明");

            // Assert ②：接收管道必须仍然活着（端到端证据）
            await server.SendTextAsync("{\"type\":\"probe\",\"after\":\"caller-token-cancelled\"}");

            var completed = await Task.WhenAny(received.Task, Task.Delay(WaitTimeout));
            completed.Should().BeSameAs(received.Task,
                "取消调用方令牌后接收循环必须仍在读帧（否则即为 P0-1 的僵尸态）");
            (await received.Task).Should().Contain("caller-token-cancelled");
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    #endregion

    #region I13 / P0-1：接收循环取消时的断线声明

    /// <summary>
    /// 取消接收循环令牌：必须恰好产生一次 <c>Disconnected</c>，且连接计数归零、<c>IsConnected</c> 转为 <c>false</c>。
    /// </summary>
    /// <remarks>
    /// 这是 P0-1 的核心验收：改造前该路径**只记一条 Information 日志**，
    /// 于是"无读循环 / 无事件 / 只能等 HealthCheckIntervalMs 轮询兜底"同时发生。
    /// <para>
    /// <b>实现细节（实测结论，影响用例的正确写法）</b>：取消发生在
    /// <c>ReceiveAsync</c> 期间时，<c>ClientWebSocket</c> 会以 <c>Abort</c> 中止底层连接
    /// ⇒ 退出时 <c>State = Aborted</c>。因此本用例**不得**断言"socket 仍为 Open"；
    /// 真僵尸态（socket 保持 Open）由
    /// <c>WebSocketConnectionIntegrationTests.ReceiveLoop_ShouldRaiseDisconnected_WhenCancelledWhileDispatchingFrame</c>
    /// 覆盖。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ReceiveLoop_ShouldRaiseDisconnected_WhenCancelledWhileSocketStillOpen()
    {
        // Arrange
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var client = CreateClient();
        var closeArgs = new List<WebSocketCloseEventArgs>();
        client.Disconnected += (_, e) => { lock (closeArgs) { closeArgs.Add(e); } };

        try
        {
            await client.ConnectAsync(Endpoint(server));
            client.Liveness.ReceiveLoopAlive.Should().BeTrue();

            var manager = GetConnectionManager(client);
            manager.ConnectionCount.Should().Be(1);

            // Act：直接取消接收循环令牌（模拟"某处的取消"）
            var loopCts = (CancellationTokenSource?)CancellationTokenSourceField.GetValue(client);
            loopCts.Should().NotBeNull("前置条件：连接成功后客户端必须持有自持的循环令牌");
            loopCts!.Cancel();

            await WaitUntilAsync(
                () => { lock (closeArgs) { return closeArgs.Count >= 1; } },
                "P0-1/I13：接收循环因取消退出时必须补发 Disconnected（改造前此处完全静默）");

            // Assert ①：恰好一次
            await Task.Delay(200);
            lock (closeArgs)
            {
                closeArgs.Should().ContainSingle("同一次连接最多产生一次断线声明（I2/I13）");
                closeArgs[0].IsServerInitiated.Should().BeFalse();
                closeArgs[0].CloseStatusDescription.Should().Contain("接收循环被取消",
                    "断线描述必须如实反映终止原因，便于运维定位");
            }

            // Assert ②：计数与状态一致
            await WaitUntilAsync(() => manager.ConnectionCount == 0, "断线声明必须递减连接计数（原子占位）");
            client.IsConnected.Should().BeFalse(
                "I12：连接不可再收事件时 IsConnected 必须为 false（旧实现的 _connectionState 恒为 1）");
            client.Liveness.ReceiveLoopAlive.Should().BeFalse();
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    /// <summary>
    /// 主动断开（<c>DisconnectAsync</c>）仍必须恰好一次 <c>Disconnected</c>，且归属为"客户端主动断开"。
    /// </summary>
    /// <remarks>
    /// 守护 WS2-01 ① 的"占位顺序前移"：前移后接收循环不再抢先在占位上胜出，
    /// 事件归属与描述必须固定为"客户端主动断开连接"，而不是"接收循环被取消"。
    /// </remarks>
    [Fact]
    public async Task DisconnectAsync_ShouldFireExactlyOneDisconnected_AfterClaimReorder()
    {
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var client = CreateClient();
        var closeArgs = new List<WebSocketCloseEventArgs>();
        client.Disconnected += (_, e) => { lock (closeArgs) { closeArgs.Add(e); } };

        try
        {
            await client.ConnectAsync(Endpoint(server));

            // Act
            await client.DisconnectAsync();
            await Task.Delay(300);

            // Assert
            lock (closeArgs)
            {
                closeArgs.Should().ContainSingle("主动断开不得重复派发，也不得被接收循环的迟到通知吞掉");
                closeArgs[0].CloseStatusDescription.Should().Contain("客户端主动断开连接",
                    "占位顺序前移后，事件归属必须确定地指向本端主动断开（而非接收循环取消）");
                closeArgs[0].IsServerInitiated.Should().BeFalse();
                closeArgs[0].CloseStatus.Should().Be(WebSocketCloseStatus.NormalClosure);
            }

            client.IsConnected.Should().BeFalse();
            GetConnectionManager(client).ConnectionCount.Should().Be(0);
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    /// <summary>
    /// 释放路径同样必须参与占位（I13）：socket 仍 Open 时 <c>DisposeAsync</c> 必须把连接计数清零。
    /// </summary>
    [Fact]
    public async Task Dispose_ShouldClaimDisconnected_WhenSocketStillOpen()
    {
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var client = CreateClient();

        await client.ConnectAsync(Endpoint(server));
        var manager = GetConnectionManager(client);
        manager.ConnectionCount.Should().Be(1);

        // Act
        await client.DisposeAsync();

        // Assert
        manager.ConnectionCount.Should().Be(0,
            "I13：释放是'使 socket 不再被读取'的终止路径，必须完成原子占位（否则连接计数永久漂移）");
    }

    #endregion

    #region I14 / P1-1：接收循环唯一性

    /// <summary>
    /// 已连接时重复调用 <c>StartReceivingAsync</c>：必须为幂等 no-op，且 <c>_receiveTask</c> 引用不变。
    /// </summary>
    /// <remarks>
    /// 断言"循环任务引用未变"比"未抛异常"强得多：它直接证明**没有第二条循环被创建**。
    /// 改造前的守卫读取的是由 <c>ConnectAsync</c> 赋值的 <c>_receiveTask</c>，
    /// 在"循环已结束但 socket 仍 Open"与"DisconnectAsync 正把该字段置 null"两类窗口下失效。
    /// </remarks>
    [Fact]
    public async Task StartReceivingAsync_ShouldRejectSecondCall_WhenAlreadyReceiving()
    {
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var client = CreateClient();

        try
        {
            await client.ConnectAsync(Endpoint(server));
            var before = GetReceiveTask(client);
            before.Should().NotBeNull();

#pragma warning disable CS0618 // WS2-02：本用例刻意调用已弃用 API 验证其收口语义
            var act = async () => await client.StartReceivingAsync();
#pragma warning restore CS0618

            await act.Should().NotThrowAsync("已有循环在途时必须幂等返回（不得抛异常，避免破坏宿主关停流程）");

            GetReceiveTask(client).Should().BeSameAs(before,
                "I14：幂等守卫必须阻止创建第二条接收循环——循环任务引用不得被替换");
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    /// <summary>
    /// 未连接时调用 <c>StartReceivingAsync</c>：必须抛 <see cref="InvalidOperationException"/>。
    /// </summary>
    [Fact]
    public async Task StartReceivingAsync_ShouldThrow_WhenNotConnected()
    {
        var client = CreateClient();

        try
        {
#pragma warning disable CS0618 // WS2-02：验证收口后的对外语义
            var act = async () => await client.StartReceivingAsync();
#pragma warning restore CS0618

            await act.Should().ThrowAsync<InvalidOperationException>(
                "未连接时接收循环无 socket 可读，必须确定性失败（对齐 WebSocketConnectionManager 的既有语义）");
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    #endregion

    #region WS2-08 / P1-6：MessageReceived 不得阻塞接收管道

    /// <summary>
    /// 慢 <c>MessageReceived</c> 订阅者不得串行化整条接收管道（5 个订阅者必须能同时进入）。
    /// </summary>
    /// <remarks>
    /// 改造前该事件在**接收循环线程上同步派发**：第 1 个订阅者阻塞即堵死整条管道
    /// （不读帧、不回 ACK、不推进心跳）。
    /// <para>
    /// <b>为什么用"最大并发进入数"而不是耗时</b>：耗时断言在并行/受限 CI 上必然抖动，
    /// 且"耗时 &lt; X"无法区分"没有阻塞"与"碰巧很快"。这里让每个订阅者阻塞在一个
    /// <see cref="CountdownEvent"/> 上直到全部进入：旧实现下第 1 个回调就会卡住接收循环，
    /// 计数器**永远到不了 2**；新实现下 5 个回调并发进入，计数器达到 5。
    /// 两者相差一个数量级，判定确定性高。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task MessageReceived_ShouldNotBlockReceiveLoop_WhenSubscriberIsSlow()
    {
        // Arrange
        const int frameCount = 5;
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var client = CreateClient();

        using var allEntered = new CountdownEvent(frameCount);
        var inFlight = 0;
        var maxConcurrent = 0;
        var delivered = 0;

        client.MessageReceived += (_, _) =>
        {
            Interlocked.Increment(ref delivered);

            var current = Interlocked.Increment(ref inFlight);
            InterlockedMax(ref maxConcurrent, current);

            allEntered.Signal();
            // 阻塞至"所有订阅者都已进入"——模拟慢订阅者（旧实现下这里会卡住接收循环本身）
            allEntered.Wait(TimeSpan.FromSeconds(5));

            Interlocked.Decrement(ref inFlight);
        };

        try
        {
            await client.ConnectAsync(Endpoint(server));

            // Act：连投 5 帧
            for (var i = 0; i < frameCount; i++)
            {
                await server.SendTextAsync($"{{\"type\":\"probe\",\"seq\":{i}}}");
            }

            await WaitUntilAsync(() => Volatile.Read(ref delivered) >= frameCount,
                "全部帧都必须被派发（不得因慢订阅者而丢帧）");

            // Assert
            Volatile.Read(ref maxConcurrent).Should().BeGreaterThan(1,
                "WS2-08/P1-6：MessageReceived 必须脱离接收循环线程派发（可并发），" +
                "否则第 1 个慢订阅者会串行化整条接收管道（与《架构与并发模型》§6 的背压模型矛盾）");
            client.IsConnected.Should().BeTrue("慢订阅者不得导致连接失效");
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while ((current = Volatile.Read(ref target)) < value)
        {
            if (Interlocked.CompareExchange(ref target, value, current) == current)
            {
                return;
            }
        }
    }

    #endregion

    #region I12 / F1：真僵尸态在客户端层的可观测性（#15 / #29）

    /// <summary>
    /// 真僵尸态（socket 保持 <c>Open</c> 而接收循环已结束）必须在客户端层被判为
    /// "不可收事件"且"僵尸"，同时产生断线声明。
    /// </summary>
    /// <remarks>
    /// <b>如何确定性制造"取消落在派发阶段"</b>（这是 P0-1 的真僵尸形态，见方案 §0.5.5 REV-19）：
    /// <list type="number">
    /// <item>把并发上界设为 <c>1</c>，订阅者阻塞在信号量上以长期占用唯一租约；</item>
    /// <item>投第 2 帧——接收循环会先进入 <c>HandleReceivedMessageAsync</c>（刷新收帧时间戳），
    /// 然后**阻塞在租约等待**上；</item>
    /// <item>用私有 <c>_lastReceiveTicks</c> 的变化确认"第 2 帧已被接收循环取走"，
    /// 此时取消令牌必然落在**租约等待**（而非 <c>ReceiveAsync</c>）——
    /// 后者会触发 <c>ClientWebSocket</c> 的 Abort 而使 socket 变为 Aborted（另一种形态，不会形成僵尸）。</item>
    /// </list>
    /// <para>
    /// 不使用 <c>Task.Delay</c> 赌时序：第 2 步的等待条件是"时间戳已变化"这一**可观测事实**。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Liveness_ShouldReportZombie_WhenReceiveLoopStopsWhileSocketStaysOpen()
    {
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        await using var concurrencyService = CreateSingleSlotConcurrencyService();
        var client = CreateClient(maxConcurrentHandlers: 1, concurrencyService: concurrencyService);

        using var subscriberEntered = new ManualResetEventSlim(false);
        using var releaseSubscriber = new ManualResetEventSlim(false);

        var closeArgs = new List<WebSocketCloseEventArgs>();
        client.Disconnected += (_, e) => { lock (closeArgs) { closeArgs.Add(e); } };

        // 订阅者占用唯一租约并阻塞（模拟慢处理器造成的背压）
        client.MessageReceived += (_, _) =>
        {
            subscriberEntered.Set();
            releaseSubscriber.Wait(TimeSpan.FromSeconds(10));
        };

        try
        {
            await client.ConnectAsync(Endpoint(server));

            // 第 1 帧：被派发并占住唯一租约
            await server.SendTextAsync("{\"type\":\"probe\",\"seq\":1}");
            await WaitUntilAsync(() => subscriberEntered.IsSet, "订阅者必须已进入（租约被占住）");

            // 第 2 帧：接收循环取走后会阻塞在租约等待上
            var ticksBeforeSecondFrame = ReadLastReceiveTicks(client);
            await server.SendTextAsync("{\"type\":\"probe\",\"seq\":2}");
            await WaitUntilAsync(
                () => ReadLastReceiveTicks(client) != ticksBeforeSecondFrame,
                "第 2 帧必须已被接收循环取走（此后取消必然落在租约等待阶段）");

            // Act：在"派发阶段"取消 —— 接收循环以条件不成立自然退出，socket 未被 Abort
            var loopCts = (CancellationTokenSource?)CancellationTokenSourceField.GetValue(client);
            loopCts.Should().NotBeNull();
            loopCts!.Cancel();

            releaseSubscriber.Set();

            await WaitUntilAsync(
                () => !client.Liveness.ReceiveLoopAlive,
                "取消落在租约等待时必须让接收循环结束");

            // Assert
            client.Liveness.IsConnected.Should().BeTrue(
                "socket 未被 Abort（取消发生在租约等待而非 ReceiveAsync）——这正是【看似正常】的来源");
            client.Liveness.IsZombie.Should().BeTrue(
                "F1：必须把【Socket Open 但接收循环已死】这一确定性矛盾暴露为可观测事实");
            client.IsConnected.Should().BeFalse(
                "I12：连接不可再收事件时 IsConnected 必须为 false，否则健康检查/重连判定永远拒绝恢复");

            await WaitUntilAsync(
                () => { lock (closeArgs) { return closeArgs.Count == 1; } },
                "I13：该终止路径必须产生且仅产生一次断线声明");
        }
        finally
        {
            releaseSubscriber.Set();
            await client.DisposeAsync();
        }
    }

    #endregion

    #region FU-3（R2 遗留项）：连接生命周期事件必须"出锁派发"

    /// <summary>
    /// <c>Connected</c> 回调期间发起的 <c>DisconnectAsync</c> 必须能完成（事件不得在 <c>_connectLock</c> 内派发）。
    /// </summary>
    /// <remarks>
    /// 客户端层的观测手法（与 CM 层用例同构，但**用独立线程发起断开**以避免同步上下文干扰）：
    /// <list type="bullet">
    /// <item>回调内阻塞等待"断开已完成"的信号；</item>
    /// <item>后台线程在回调已进入后调用 <c>DisconnectAsync()</c>；</item>
    /// <item>若事件仍在 <c>_connectLock</c> 持有期内派发 ⇒ 只能拿到锁，信号永不到达 ⇒ 用例红。</item>
    /// </list>
    /// 这与"回调内直接同步调用"具有同等检测力（不可重入信号量被连接线程持有时，任何线程都拿不到），
    /// 但不依赖测试框架的 SynchronizationContext 行为。
    /// </remarks>
    [Fact]
    public async Task Connected_Handler_ShouldNotDeadlock_WhenDisconnectingFromAnotherThread()
    {
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var client = CreateClient();

        using var handlerEntered = new ManualResetEventSlim(false);
        using var releaseHandler = new ManualResetEventSlim(false);
        var disconnectCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        client.Connected += (_, _) =>
        {
            handlerEntered.Set();
            releaseHandler.Wait(TimeSpan.FromSeconds(10));
        };

        try
        {
            var connectTask = Task.Run(() => client.ConnectAsync(Endpoint(server)));

            await WaitUntilAsync(() => handlerEntered.IsSet, "前置条件：Connected 回调必须已进入");

            // 只要事件是在锁外派发，这把锁此刻就是空闲的，后台线程可以完成整轮断开
            _ = Task.Run(async () =>
            {
                try
                {
                    await client.DisconnectAsync();
                    disconnectCompleted.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    disconnectCompleted.TrySetException(ex);
                }
            });

            var act = async () => await disconnectCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await act.Should().NotThrowAsync(
                "FU-3：Connected 事件必须在 _connectLock 之外派发——否则回调期间发起的任何断开/重连都会自锁死锁");

            releaseHandler.Set();
            await connectTask.WaitAsync(TimeSpan.FromSeconds(5));

            client.IsConnected.Should().BeFalse();
        }
        finally
        {
            releaseHandler.Set();
            await client.DisposeAsync();
        }
    }

    /// <summary>
    /// <c>Disconnected</c> 回调期间发起的 <c>DisconnectAsync</c> 必须幂等完成（同上）。
    /// </summary>
    [Fact]
    public async Task Disconnected_Handler_ShouldNotDeadlock_WhenDisconnectingFromAnotherThread()
    {
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var client = CreateClient();

        using var handlerEntered = new ManualResetEventSlim(false);
        using var releaseHandler = new ManualResetEventSlim(false);
        var secondDisconnectCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var entered = 0;
        client.Disconnected += (_, _) =>
        {
            if (Interlocked.Increment(ref entered) > 1)
            {
                return;   // 只阻塞首次派发，避免用例自身造出递归
            }

            handlerEntered.Set();
            releaseHandler.Wait(TimeSpan.FromSeconds(10));
        };

        try
        {
            await client.ConnectAsync(Endpoint(server));

            var disconnectTask = Task.Run(() => client.DisconnectAsync());

            await WaitUntilAsync(() => handlerEntered.IsSet, "前置条件：Disconnected 回调必须已进入");

            _ = Task.Run(async () =>
            {
                try
                {
                    await client.DisconnectAsync();
                    secondDisconnectCompleted.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    secondDisconnectCompleted.TrySetException(ex);
                }
            });

            var act = async () => await secondDisconnectCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await act.Should().NotThrowAsync(
                "FU-3：Disconnected 事件同样必须在 _connectLock 之外派发");

            releaseHandler.Set();
            await disconnectTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            releaseHandler.Set();
            await client.DisposeAsync();
        }
    }

    /// <summary>
    /// 出锁缓冲必须**保持事件原始顺序**（替换旧连接时先断开、后连接）。
    /// </summary>
    /// <remarks>
    /// 用有序队列而非"Connected/Disconnected 两个标志位"的原因：
    /// 标志位会把"旧连接断开"与"新连接建立"的次序丢掉，使重连状态机与日志时间线出现倒序噪音。
    /// </remarks>
    [Fact]
    public async Task DeferredConnectionEvents_ShouldPreserveOrder_WhenReplacingConnection()
    {
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var client = CreateClient();

        var sequence = new List<string>();
        client.Connected += (_, _) => { lock (sequence) { sequence.Add("connected"); } };
        client.Disconnected += (_, _) => { lock (sequence) { sequence.Add("disconnected"); } };

        try
        {
            await client.ConnectAsync(Endpoint(server));

            // Act：再次连接同一端点 → 连接管理器先断开旧连接（Disconnected）再建立（Connected）
            await client.ConnectAsync(Endpoint(server));

            await WaitUntilAsync(
                () => { lock (sequence) { return sequence.Count >= 3; } },
                "第二次连接必须派发【旧连接断开 + 新连接建立】两条事件");

            // Assert
            lock (sequence)
            {
                // 注意：StringCollectionAssertions.Equal(params string[]) 会把"理由文本"当成元素，
                // 故此处显式传数组、理由写在注释里
                sequence.Should().Equal(new[] { "connected", "disconnected", "connected" },
                    "FU-3：出锁缓冲必须保持原始顺序（先断开后连接）");
            }
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    #endregion

    #region 池化副本所有权（#25 的等价替代）

    /// <summary>
    /// 高频二进制帧下，每一帧的 payload 都必须被正确解析（守护 <c>ArrayPool</c> 副本的所有权）。
    /// </summary>
    /// <remarks>
    /// <b>为什么替代了"延迟读取的处理器"写法</b>：<c>FeishuWebSocketClient</c> 的池化副本
    /// （P2-12）在**派发任务内部**即被 protobuf 反序列化消费，用户可见的处理器拿到的是已经
    /// 解析完成的 <c>JsonContent</c>（<c>string</c>，不可变）——**不存在"用户延迟读取副本"这一可注入面**，
    /// 原本的用例形态无法观测到池复用缺陷。
    /// <para>
    /// 真正会破坏该不变量的回归是"在解析完成前归还池"（<c>finally</c> 顺序被打乱）。
    /// 该回归在高频投递下会表现为：同一数组被下一帧 <c>Rent</c> 到并覆写，解析结果串帧/失败。
    /// 因此用"200 帧唯一载荷全部正确解析"作为等价守护：它在回归时会稳定失败（池按 2 的幂分桶，
    /// 同一数组会被反复复用），而在正确实现下是确定性的。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task BinaryFrames_ShouldAllBeParsedCorrectly_WhenReceivedAtHighRate()
    {
        const int frameCount = 200;
        await using var server = LoopbackWebSocketServer.Start(LoopbackWebSocketServer.ServerMode.Idle);
        var client = CreateClient();

        var payloads = new List<string>();
        var parseErrors = new List<string>();
        client.BinaryMessageReceived += (_, e) =>
        {
            lock (payloads)
            {
                if (e.ParseError != null)
                {
                    parseErrors.Add(e.ParseError);
                }
                else if (e.JsonContent != null)
                {
                    payloads.Add(e.JsonContent);
                }
            }
        };

        try
        {
            await client.ConnectAsync(Endpoint(server));

            // Act：连续投递 200 帧，每帧带唯一标记
            for (var i = 0; i < frameCount; i++)
            {
                await server.SendBinaryAsync(BuildFrameWithMarker($"m{i}"));
            }

            await WaitUntilAsync(
                () => { lock (payloads) { return payloads.Count >= frameCount; } },
                $"{frameCount} 帧必须全部被解析并派发");

            // Assert
            lock (payloads)
            {
                parseErrors.Should().BeEmpty("池化副本所有权若被打乱，会出现解析失败");
                payloads.Should().OnlyHaveUniqueItems("每一帧的载荷必须各自独立（不得串帧）");
                payloads.Should().HaveCount(frameCount);
                for (var i = 0; i < frameCount; i++)
                {
                    payloads.Should().Contain(p => p.Contains($"m{i}"),
                        $"第 {i} 帧的载荷必须原样送达（池复用覆写会造成载荷错配）");
                }
            }
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    /// <summary>
    /// 构造带唯一标记的合法 <c>EventProtoData</c> 帧（JSON payload）。
    /// </summary>
    private static byte[] BuildFrameWithMarker(string marker)
    {
        var frame = new EventProtoData
        {
            Service = 1001,
            Method = 1,
            SeqID = 1,
            PayloadType = "JSON",
            Payload = Encoding.UTF8.GetBytes($"{{\"type\":\"probe\",\"marker\":\"{marker}\"}}")
        };

        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, frame);
        return stream.ToArray();
    }

    #endregion
}

#endif
