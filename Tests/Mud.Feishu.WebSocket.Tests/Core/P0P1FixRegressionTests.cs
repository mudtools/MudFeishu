// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.WebSocket.Handlers;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Reflection;
using System.Text;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// P0/P1 缺陷修复回归测试。
/// 覆盖：P0-1 二进制分片串行化与重连重置、P0-4 断线事件原子性、P0-7 作用域感知处理器工厂、
/// P1-5 路由结果回传、P1-10 JsonElement 生命周期、P2-5 重试负数次、P2-13 配置校验。
/// </summary>
/// <remarks>
/// 与 <c>WebSocketConnectionManagerTests</c> 共用集合：本类会操作
/// <see cref="WebSocketConnectionManager.ConnectionCount"/> 静态计数器，需串行执行避免相互干扰。
/// </remarks>
[Collection("WebSocketConnectionManager")]
public class P0P1FixRegressionTests
{
    #region 辅助类型与工具

    private sealed class RecordingHandler : IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";
        public int InvocationCount;
        public EventData? LastEventData;

        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref InvocationCount);
            LastEventData = eventData;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IMessageHandler
    {
        public bool CanHandle(string messageType) => true;
        public Task HandleAsync(string message, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("处理器故意失败");
    }

    private static byte[] BuildFrame(string payload)
    {
        var frame = new EventProtoData
        {
            SeqID = 1,
            Method = 1, // 数据帧（Method=0 为控制帧）
            Service = 0,
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadEncoding = "json",
            PayloadType = "application/json"
        };

        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, frame);
        return stream.ToArray();
    }

    private static BinaryMessageProcessor CreateProcessor(
        FeishuWebSocketOptions? options = null,
        Action<WebSocketBinaryMessageEventArgs>? onReceived = null)
    {
        var logger = NullLogger<BinaryMessageProcessor>.Instance;
        var connectionManager = new Mock<WebSocketConnectionManager>(
            NullLogger<WebSocketConnectionManager>.Instance,
            new FeishuWebSocketOptions(),
            NullLoggerFactory.Instance);
        var router = new Mock<MessageRouter>(
            NullLogger<MessageRouter>.Instance,
            new FeishuWebSocketOptions());

        var processor = new BinaryMessageProcessor(
            logger,
            connectionManager.Object,
            options ?? new Mud.Feishu.WebSocket.FeishuWebSocketOptions { },
            router.Object);

        if (onReceived != null)
        {
            processor.BinaryMessageReceived += (_, e) => onReceived(e);
        }

        return processor;
    }

    #endregion

    #region P0-1：二进制分片串行化

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldReassemble_WhenMessageArrivesInFragments()
    {
        // Arrange
        const string payload = "{\"hello\":\"world\"}";
        var data = BuildFrame(payload);
        var half = data.Length / 2;

        var completed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        cts.Token.Register(() => completed.TrySetResult(null));

        using var processor = CreateProcessor(onReceived: e =>
        {
            if (string.IsNullOrEmpty(e.ParseError))
                completed.TrySetResult(e.JsonContent);
        });

        // Act
        await processor.ProcessBinaryDataAsync(data, 0, half, endOfMessage: false);
        await processor.ProcessBinaryDataAsync(data, half, data.Length - half, endOfMessage: true);

        // Assert
        var json = await completed.Task;
        json.Should().Be(payload);
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldNotCorrupt_WhenCalledConcurrently()
    {
        // Arrange
        const int messageCount = 20;
        var received = new System.Collections.Concurrent.ConcurrentBag<string>();
        var allDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var remaining = messageCount;

        using var processor = CreateProcessor(onReceived: e =>
        {
            if (!string.IsNullOrEmpty(e.JsonContent))
                received.Add(e.JsonContent);
            if (Interlocked.Decrement(ref remaining) == 0)
                allDone.TrySetResult();
        });

        // Act：并发提交 20 条完整消息
        var tasks = Enumerable.Range(0, messageCount).Select(i => Task.Run(async () =>
        {
            var payload = $"{{\"index\":{i}}}";
            var data = BuildFrame(payload);
            await processor.ProcessBinaryDataAsync(data, 0, data.Length, endOfMessage: true);
        })).ToArray();

        await Task.WhenAll(tasks);
        var finished = await Task.WhenAny(allDone.Task, Task.Delay(TimeSpan.FromSeconds(10)));

        // Assert：每条消息都完整且未被其它消息污染
        finished.Should().Be(allDone.Task, "所有消息都应在超时前完成处理");
        received.Should().HaveCount(messageCount);
        received.Should().OnlyContain(json => json.StartsWith("{\"index\":") && json.EndsWith("}"));
    }

    #endregion

    #region P1-6：重连重置分片缓冲

    [Fact]
    public async Task Reset_ShouldDiscardPartialData_WhenCalledBeforeMessageCompletes()
    {
        // Arrange
        var firstFrame = BuildFrame("{\"hello\":\"stale\"}");
        var staleHalf = firstFrame.Length / 2;

        var completed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        cts.Token.Register(() => completed.TrySetResult(null));

        using var processor = CreateProcessor(onReceived: e =>
        {
            if (string.IsNullOrEmpty(e.ParseError))
                completed.TrySetResult(e.JsonContent);
        });

        // 先灌入半包（模拟连接中途断开）
        await processor.ProcessBinaryDataAsync(firstFrame, 0, staleHalf, endOfMessage: false);

        // Act：重连重置后投递一条完整的新消息
        processor.Reset();
        var secondFrame = BuildFrame("{\"hello\":\"fresh\"}");
        await processor.ProcessBinaryDataAsync(secondFrame, 0, secondFrame.Length, endOfMessage: true);

        // Assert：不应把旧半包拼到新消息前面
        var json = await completed.Task;
        json.Should().Be("{\"hello\":\"fresh\"}");
    }

    #endregion

    #region P0-4：断线事件原子性

    [Fact]
    public void NotifyDisconnected_ShouldFireExactlyOnce_WhenInvokedConcurrently()
    {
        // Arrange
        var manager = new WebSocketConnectionManager(
            NullLogger<WebSocketConnectionManager>.Instance,
            new Mud.Feishu.WebSocket.FeishuWebSocketOptions { },
            NullLoggerFactory.Instance);

        var fired = 0;
        manager.Disconnected += (_, _) => Interlocked.Increment(ref fired);

        var notifyMethod = typeof(WebSocketConnectionManager)
            .GetMethod("NotifyDisconnected", BindingFlags.NonPublic | BindingFlags.Instance);
        var flagField = typeof(WebSocketConnectionManager)
            .GetField("_disconnectedFired", BindingFlags.NonPublic | BindingFlags.Instance);
        var countField = typeof(WebSocketConnectionManager)
            .GetField("_connectionCount", BindingFlags.NonPublic | BindingFlags.Instance);

        notifyMethod.Should().NotBeNull();
        flagField.Should().NotBeNull();
        countField.Should().NotBeNull();

        // WS-17 修复后 _connectionCount 为实例字段，不再需要静态计数器的串行保护
        var before = manager.ConnectionCount;

        try
        {
            // 模拟"已连接、尚未触发断线"的状态
            flagField!.SetValue(manager, 0);
            // 模拟已连接：设置计数为 1
            countField!.SetValue(manager, 1);

            // Act：32 个线程并发声明断线
            // P1-2 改造同步（M5）：NotifyDisconnected 增加了 owner 参数（socket 身份校验），
            // MethodInfo.Invoke 的参数个数必须精确匹配（可选参数不会被反射补齐），此处显式传 null
            // 表示"不做代次过滤"，从而保持本用例原有语义（只验证 P0-4 的原子性）。
            Parallel.For(0, 32, _ =>
            {
                notifyMethod!.Invoke(manager, new object?[] { new WebSocketCloseEventArgs(), null });
            });

            // Assert：Interlocked.CompareExchange 保证仅触发一次、计数仅递减一次
            fired.Should().Be(1);
            manager.ConnectionCount.Should().Be(0);
        }
        finally
        {
            // WS-17 修复后为实例字段，无需还原静态计数器
        }
    }

    #endregion

    #region P0-7：作用域感知处理器工厂

    [Fact]
    public async Task ScopedFeishuEventHandlerFactory_ShouldCreateScopePerDispatch()
    {
        // Arrange
        var handler = new RecordingHandler();
        int scopeCreated = 0;

        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(It.IsAny<Type>()))
                .Returns((Type t) => t == typeof(RecordingHandler) ? handler : null);

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object);
        scope.Setup(s => s.Dispose()).Callback(() => { });

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope())
                    .Returns(() =>
                    {
                        Interlocked.Increment(ref scopeCreated);
                        return scope.Object;
                    });

        var factory = new ScopedFeishuEventHandlerFactory(
            NullLogger<ScopedFeishuEventHandlerFactory>.Instance,
            scopeFactory.Object,
            new[] { typeof(RecordingHandler) },
            typeof(RecordingHandler));

        var eventData = new EventData { EventType = "test.event", EventId = "evt-1" };

        // Act
        await factory.HandleEventParallelAsync("test.event", eventData);
        await factory.HandleEventParallelAsync("test.event", eventData);

        // Assert：每次分发都创建并释放一个独立作用域
        scopeCreated.Should().BeGreaterThanOrEqualTo(2);
        handler.InvocationCount.Should().BeGreaterThanOrEqualTo(2);
        scope.Verify(s => s.Dispose(), Times.AtLeast(2));
    }

    [Fact]
    public void ScopedFeishuEventHandlerFactory_ShouldExposeHandler_ForInspection()
    {
        // Arrange
        var handler = new RecordingHandler();
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(It.IsAny<Type>()))
                .Returns((Type t) => t == typeof(RecordingHandler) ? handler : null);

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var factory = new ScopedFeishuEventHandlerFactory(
            NullLogger<ScopedFeishuEventHandlerFactory>.Instance,
            scopeFactory.Object,
            new[] { typeof(RecordingHandler) },
            typeof(RecordingHandler));

        // Act & Assert
        factory.GetHandler("test.event").Should().BeSameAs(handler);
        factory.GetHandlers("test.event").Should().Contain(handler);
        factory.IsHandlerRegistered("test.event").Should().BeTrue();
        factory.GetRegisteredEventTypes().Should().Contain("test.event");
    }

    #endregion

    #region P1-5：路由结果回传

    [Fact]
    public async Task RouteBinaryMessageWithResultAsync_ShouldReturnFalse_WhenHandlerThrows()
    {
        // Arrange
        var options = new FeishuWebSocketOptions { MessageHandlerTimeoutMs = 0 };
        var router = new MessageRouter(NullLogger<MessageRouter>.Instance, options);
        router.RegisterHandler(new ThrowingHandler());

        // Act
        var result = await router.RouteBinaryMessageWithResultAsync("{\"type\":\"event\"}", "Frame");

        // Assert：失败必须回传，否则 ACK 会恒为成功导致事件永久丢失
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RouteBinaryMessageWithResultAsync_ShouldReturnTrue_WhenNoHandlerRegistered()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { };
        var router = new MessageRouter(NullLogger<MessageRouter>.Instance, options);

        // Act
        var result = await router.RouteBinaryMessageWithResultAsync("{\"type\":\"unknown\"}", "Frame");

        // Assert：未注册处理器属于"无需处理"，不应判定为失败
        result.Should().BeTrue();
    }

    #endregion

    #region P1-10：JsonElement 生命周期

    [Fact]
    public async Task FeishuEventMessageHandler_ShouldProduceEventPayload_SurvivingAfterHandleAsync()
    {
        // Arrange
        var handler = new RecordingHandler();
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock.Setup(f => f.HandleEventParallelAsync(
                It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns((string _, EventData data, CancellationToken _) => handler.HandleAsync(data));

        var sut = new FeishuEventMessageHandler(
            NullLogger<FeishuEventMessageHandler>.Instance,
            factoryMock.Object,
            null,
            null,
            new Mud.Feishu.WebSocket.FeishuWebSocketOptions { },
            null);

        var message = """
        {
          "schema": "2.0",
          "header": { "event_id": "evt-1", "event_type": "test.event", "create_time": "1700000000000" },
          "event": { "text": "hello" }
        }
        """;

        // Act
        await sut.HandleAsync(message);

        // Assert：HandleAsync 返回后 JsonDocument 已释放，
        // 若未 Clone，读取 event 会拿到已被 ArrayPool 复用的内存。
        handler.LastEventData.Should().NotBeNull();
        var element = handler.LastEventData!.Event.Should().BeOfType<System.Text.Json.JsonElement>().Subject;
        element.TryGetProperty("text", out var textProp).Should().BeTrue();
        textProp.GetString().Should().Be("hello");
    }

    #endregion

    #region P2-5 / P2-13：重试与配置校验

    [Fact]
    public async Task RetryWithExponentialBackoffAsync_ShouldExecuteOnce_WhenMaxRetriesIsNegative()
    {
        // Arrange
        var calls = 0;

        // Act
        var result = await RetryHelper.RetryWithExponentialBackoffAsync(
            NullLogger.Instance,
            () => { Interlocked.Increment(ref calls); return Task.FromResult(42); },
            maxRetries: -1,
            baseDelayMs: 1,
            "负重试次数",
            CancellationToken.None);

        // Assert
        result.Should().Be(42);
        calls.Should().Be(1);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxTotalReconnectTimeIsNotPositive()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { Reconnect = new Mud.Feishu.WebSocket.WebSocketReconnectOptions { TotalBudget = TimeSpan.Zero } };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenInitialReceiveBufferSizeIsTooLarge()
    {
        // Arrange
        var options = new FeishuWebSocketOptions { InitialReceiveBufferSize = 8 * 1024 * 1024 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenMaxAuthRetryAttemptsIsNegative()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { Reconnect = new Mud.Feishu.WebSocket.WebSocketReconnectOptions { MaxAuthRetryAttempts = -1 } };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion
}
