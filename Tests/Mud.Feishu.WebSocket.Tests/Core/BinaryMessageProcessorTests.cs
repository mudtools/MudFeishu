// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mud.Feishu.DataModels.WsEndpoint;
using Mud.Feishu.WebSocket.SocketEventArgs;
using ProtoBuf;
using System.Text;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// BinaryMessageProcessor 二进制消息处理器测试类
/// </summary>
public class BinaryMessageProcessorTests
{
    private readonly Mock<ILogger<BinaryMessageProcessor>> _loggerMock;
    private readonly Mock<WebSocketConnectionManager> _connectionManagerMock;
    private readonly Mock<MessageRouter> _messageRouterMock;
    private readonly FeishuWebSocketOptions _options;

    public BinaryMessageProcessorTests()
    {
        _loggerMock = new Mock<ILogger<BinaryMessageProcessor>>();
        _connectionManagerMock = new Mock<WebSocketConnectionManager>(
            Mock.Of<ILogger<WebSocketConnectionManager>>(),
            new FeishuWebSocketOptions(),
            Mock.Of<ILoggerFactory>());

        _messageRouterMock = new Mock<MessageRouter>(
            Mock.Of<ILogger<MessageRouter>>(),
            new FeishuWebSocketOptions());

        _options = new FeishuWebSocketOptions
        {
            MessageSizeLimits = new MessageSizeLimits
            {
                MaxBinaryMessageSize = 1024 * 1024 // 1MB for testing
            }
        };
    }

    private BinaryMessageProcessor CreateProcessor()
    {
        return new BinaryMessageProcessor(
            _loggerMock.Object,
            _connectionManagerMock.Object,
            _options,
            _messageRouterMock.Object);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BinaryMessageProcessor(
            null!,
            _connectionManagerMock.Object,
            _options,
            _messageRouterMock.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConnectionManagerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BinaryMessageProcessor(
            _loggerMock.Object,
            null!,
            _options,
            _messageRouterMock.Object));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMessageRouterIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BinaryMessageProcessor(
            _loggerMock.Object,
            _connectionManagerMock.Object,
            _options,
            null!));
    }

    [Fact]
    public void Constructor_ShouldUseDefaultOptions_WhenOptionsIsNull()
    {
        // Act
        var processor = new BinaryMessageProcessor(
            _loggerMock.Object,
            _connectionManagerMock.Object,
            null!,
            _messageRouterMock.Object);

        // Assert - Should not throw and use default options
        processor.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldInitializeStream_WhenFirstChunkReceived()
    {
        // Arrange
        var processor = CreateProcessor();
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        await processor.ProcessBinaryDataAsync(data, 0, data.Length, true, CancellationToken.None);

        // Assert - No exception should be thrown
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldTriggerError_WhenMessageSizeExceedsLimit()
    {
        // Arrange
        var smallOptions = new FeishuWebSocketOptions
        {
            MessageSizeLimits = new MessageSizeLimits
            {
                MaxBinaryMessageSize = 10 // Very small limit
            }
        };

        var processor = new BinaryMessageProcessor(
            _loggerMock.Object,
            _connectionManagerMock.Object,
            smallOptions,
            _messageRouterMock.Object);

        var errorTriggered = false;
        processor.Error += (sender, args) =>
        {
            errorTriggered = true;
            args.ErrorType.Should().Be("MessageSizeExceeded");
        };

        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        // Act
        await processor.ProcessBinaryDataAsync(data, 0, data.Length, false, CancellationToken.None);

        // Assert
        errorTriggered.Should().BeTrue();

        processor.Dispose();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldHandleMultipleChunks_BeforeEndOfMessage()
    {
        // Arrange
        var processor = CreateProcessor();

        // Act - Send multiple chunks
        await processor.ProcessBinaryDataAsync(new byte[] { 1, 2, 3 }, 0, 3, false, CancellationToken.None);
        await processor.ProcessBinaryDataAsync(new byte[] { 4, 5, 6 }, 0, 3, false, CancellationToken.None);
        await processor.ProcessBinaryDataAsync(new byte[] { 7, 8, 9 }, 0, 3, true, CancellationToken.None);

        // Assert - No exception should be thrown
        processor.Dispose();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldTriggerBinaryMessageReceived_WhenEndOfMessage()
    {
        // Arrange
        var processor = CreateProcessor();
        WebSocketBinaryMessageEventArgs? receivedArgs = null;
        processor.BinaryMessageReceived += (sender, args) => receivedArgs = args;

        var validProtobufData = CreateValidProtobufData();
        var argsReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        processor.BinaryMessageReceived += (sender, args) => argsReady.TrySetResult(true);

        // Act
        await processor.ProcessBinaryDataAsync(validProtobufData, 0, validProtobufData.Length, true, CancellationToken.None);

        // 等待异步派发（有界条件等待）：固定 Task.Delay(100) 在并行负载下偶发不足
        // （net10.0 全量运行时曾出现 ProcessBinaryDataAsync_ShouldTriggerEvent_* 因此偶发失败）
        var completed = await Task.WhenAny(argsReady.Task, Task.Delay(3000));
        completed.Should().BeSameAs(argsReady.Task, "有效帧必须在异步派发后触发 BinaryMessageReceived");

        // Assert
        receivedArgs.Should().NotBeNull();

        processor.Dispose();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldSetReceiveStartTime_WhenMessageCompleted()
    {
        // Arrange（P2-4 回归）：修复前 ReceiveStartTime 从不赋值（恒为 default(DateTime)），
        // ReceiveDurationMs 恒为 ~6.39e14ms（0001-01-01 至今），观测指标完全失真。
        var processor = CreateProcessor();
        var beforeReceive = DateTime.UtcNow.AddSeconds(-1);
        WebSocketBinaryMessageEventArgs? receivedArgs = null;
        var argsReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        processor.BinaryMessageReceived += (sender, args) =>
        {
            if (args.MessageType == "Frame")
            {
                receivedArgs = args;
                argsReady.TrySetResult(true);
            }
        };

        var validProtobufData = CreateValidProtobufData();

        // Act
        await processor.ProcessBinaryDataAsync(validProtobufData, 0, validProtobufData.Length, true, CancellationToken.None);

        var completed = await Task.WhenAny(argsReady.Task, Task.Delay(3000));
        completed.Should().BeSameAs(argsReady.Task, "有效 DATA 帧必须在异步派发后触发 BinaryMessageReceived（MessageType=Frame）");

        // Assert：ReceiveStartTime 必须是首帧到达时间，而不是 default(DateTime)
        receivedArgs.Should().NotBeNull();
        receivedArgs!.ReceiveStartTime.Should().BeAfter(beforeReceive,
            "修复前 ReceiveStartTime 恒为 default(DateTime)（0001-01-01）");
        receivedArgs.ReceiveEndTime.Should().BeOnOrAfter(receivedArgs.ReceiveStartTime);
        receivedArgs.ReceiveDurationMs.Should().BeInRange(0, 60_000,
            "修复前因 default 起点恒为 ~6.39e14ms");

        processor.Dispose();
    }

    [Fact]
    public void Dispose_ShouldClearActiveProcessingTasks()
    {
        // Arrange
        var processor = CreateProcessor();

        // Act
        processor.Dispose();

        // Assert - Should not throw on multiple dispose
        processor.Dispose();
    }

    [Fact]
    public void Dispose_ShouldBeCallableMultipleTimes()
    {
        // Arrange
        var processor = CreateProcessor();

        // Act & Assert - Should not throw
        processor.Dispose();
        processor.Dispose();
        processor.Dispose();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldHandleEmptyData()
    {
        // Arrange
        var processor = CreateProcessor();

        // Act & Assert - Should not throw
        await processor.ProcessBinaryDataAsync(Array.Empty<byte>(), 0, 0, true, CancellationToken.None);

        processor.Dispose();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldHandleNullData()
    {
        // Arrange
        var processor = CreateProcessor();

        // Act & Assert - Should handle gracefully
        await processor.ProcessBinaryDataAsync(null!, 0, 0, true, CancellationToken.None);

        processor.Dispose();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldTriggerEvent_WhenBinaryMessageReceived()
    {
        // Arrange
        var processor = CreateProcessor();
        var eventTriggered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        processor.BinaryMessageReceived += (sender, args) => eventTriggered.TrySetResult(true);

        var validProtobufData = CreateValidProtobufData();

        // Act
        await processor.ProcessBinaryDataAsync(validProtobufData, 0, validProtobufData.Length, true, CancellationToken.None);

        // Wait for async processing（有界条件等待，替代固定 Task.Delay(200)——并行负载下偶发不足）
        var completed = await Task.WhenAny(eventTriggered.Task, Task.Delay(3000));
        completed.Should().BeSameAs(eventTriggered.Task, "BinaryMessageReceived 必须被异步触发");

        // Assert
        (await eventTriggered.Task).Should().BeTrue();

        processor.Dispose();
    }

    #region HandleControlFrame 测试

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldTriggerPongReceived_WhenControlPongFrameReceived()
    {
        // Arrange
        var processor = CreateProcessor();
        ClientConfigInfo? receivedConfig = null;
        processor.PongReceived += (sender, config) => receivedConfig = config;

        var pongFrame = new EventProtoData
        {
            Service = 1001,
            Method = FrameBuilder.MethodControl, // CONTROL = 0
            Headers = new[] { new ProtoHeader { Key = "type", Value = "pong" } },
            Payload = Encoding.UTF8.GetBytes("{\"ReconnectCount\":5,\"ReconnectInterval\":120,\"ReconnectNonce\":30,\"PingInterval\":60}")
        };
        var pongData = SerializeFrame(pongFrame);
        var configReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        processor.PongReceived += (sender, config) => configReady.TrySetResult(true);

        // Act
        await processor.ProcessBinaryDataAsync(pongData, 0, pongData.Length, true, CancellationToken.None);

        // 等待异步处理（有界条件等待，替代固定 Task.Delay(200)）
        var completed = await Task.WhenAny(configReady.Task, Task.Delay(3000));
        completed.Should().BeSameAs(configReady.Task, "Pong 控制帧必须触发 PongReceived");

        // Assert
        receivedConfig.Should().NotBeNull();
        receivedConfig!.PingInterval.Should().Be(60);
        receivedConfig.ReconnectCount.Should().Be(5);
        receivedConfig.ReconnectInterval.Should().Be(120);
        receivedConfig.ReconnectNonce.Should().Be(30);

        processor.Dispose();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldNotTriggerPongReceived_WhenControlPingFrameReceived()
    {
        // Arrange
        var processor = CreateProcessor();
        ClientConfigInfo? receivedConfig = null;
        processor.PongReceived += (sender, config) => receivedConfig = config;

        var pingFrame = new EventProtoData
        {
            Service = 1001,
            Method = FrameBuilder.MethodControl, // CONTROL = 0
            Headers = new[] { new ProtoHeader { Key = "type", Value = "ping" } }
        };
        var pingData = SerializeFrame(pingFrame);

        // Act
        await processor.ProcessBinaryDataAsync(pingData, 0, pingData.Length, true, CancellationToken.None);
        await Task.Delay(200);

        // Assert - Ping 控制帧不应触发 PongReceived
        receivedConfig.Should().BeNull();

        processor.Dispose();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldTriggerBinaryMessageReceived_WhenControlPongFrameReceived()
    {
        // Arrange
        var processor = CreateProcessor();
        WebSocketBinaryMessageEventArgs? receivedArgs = null;
        processor.BinaryMessageReceived += (sender, args) => receivedArgs = args;

        var pongFrame = new EventProtoData
        {
            Service = 1001,
            Method = FrameBuilder.MethodControl,
            Headers = new[] { new ProtoHeader { Key = "type", Value = "pong" } },
            Payload = Encoding.UTF8.GetBytes("{\"PingInterval\":60}")
        };
        var pongData = SerializeFrame(pongFrame);
        var argsReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        processor.BinaryMessageReceived += (sender, args) => argsReady.TrySetResult(true);

        // Act
        await processor.ProcessBinaryDataAsync(pongData, 0, pongData.Length, true, CancellationToken.None);

        // 等待异步处理（有界条件等待，替代固定 Task.Delay(200)）
        var completed = await Task.WhenAny(argsReady.Task, Task.Delay(3000));
        completed.Should().BeSameAs(argsReady.Task, "Pong 控制帧必须触发 BinaryMessageReceived（MessageType=Control_*）");

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs!.MessageType.Should().StartWith("Control_");

        processor.Dispose();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldHandleControlFrameWithEmptyPayload()
    {
        // Arrange
        var processor = CreateProcessor();
        ClientConfigInfo? receivedConfig = null;
        processor.PongReceived += (sender, config) => receivedConfig = config;

        var pongFrame = new EventProtoData
        {
            Service = 1001,
            Method = FrameBuilder.MethodControl,
            Headers = new[] { new ProtoHeader { Key = "type", Value = "pong" } },
            Payload = null // 空 Payload
        };
        var pongData = SerializeFrame(pongFrame);

        // Act
        await processor.ProcessBinaryDataAsync(pongData, 0, pongData.Length, true, CancellationToken.None);
        await Task.Delay(200);

        // Assert - Pong 帧但 Payload 为空，应触发 PongReceived 但 config 为 null
        receivedConfig.Should().BeNull();

        processor.Dispose();
    }

    [Fact]
    public async Task ProcessBinaryDataAsync_ShouldNotRouteToMessageRouter_WhenControlFrameReceived()
    {
        // Arrange - 使用真实的 MessageRouter，注册一个可追踪的 handler
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { };
        var realRouter = new MessageRouter(NullLogger<MessageRouter>.Instance, options);

        var handlerCalled = false;
        var trackingHandler = new Mock<IMessageHandler>();
        trackingHandler.Setup(h => h.CanHandle(It.IsAny<string>())).Returns(true);
        trackingHandler.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => handlerCalled = true)
            .Returns(Task.CompletedTask);
        realRouter.RegisterHandler(trackingHandler.Object);

        var processor = new BinaryMessageProcessor(
            _loggerMock.Object,
            _connectionManagerMock.Object,
            options,
            realRouter);

        var pongFrame = new EventProtoData
        {
            Service = 1001,
            Method = FrameBuilder.MethodControl,
            Headers = new[] { new ProtoHeader { Key = "type", Value = "pong" } },
            Payload = Encoding.UTF8.GetBytes("{\"PingInterval\":60}")
        };
        var pongData = SerializeFrame(pongFrame);

        // Act
        await processor.ProcessBinaryDataAsync(pongData, 0, pongData.Length, true, CancellationToken.None);
        await Task.Delay(200);

        // Assert - CONTROL 帧不应路由到 MessageRouter，handler 不应被调用
        handlerCalled.Should().BeFalse();
        trackingHandler.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        processor.Dispose();
    }

    #endregion

    /// <summary>
    /// 序列化 ProtoBuf 帧
    /// </summary>
    private static byte[] SerializeFrame(EventProtoData frame)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, frame);
        return stream.ToArray();
    }

    /// <summary>
    /// 创建有效的 ProtoBuf 数据用于测试
    /// </summary>
    private byte[] CreateValidProtobufData()
    {
        // 创建一个简单的 EventProtoData 序列化后的字节数组
        // 注意：这里我们需要创建一个最小有效的测试数据
        var eventData = new EventProtoData
        {
            Service = 1001,
            Method = 1,
            SeqID = 1,
            PayloadType = "JSON",
            Payload = System.Text.Encoding.UTF8.GetBytes("{\"type\":\"test\"}")
        };

        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, eventData);
        return stream.ToArray();
    }

    // ===== P0-1：路由失败后传输层状态回滚 =====

    private sealed class ThrowingMessageHandler : IMessageHandler
    {
        public bool CanHandle(string messageType) => true;

        public Task HandleAsync(string message, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("simulated business failure");
    }

    private static byte[] CreateDataFrame(ulong seqId, string payloadJson)
    {
        var frame = new EventProtoData
        {
            Service = 1001,
            Method = FrameBuilder.MethodData,
            SeqID = seqId,
            PayloadType = "JSON",
            Payload = Encoding.UTF8.GetBytes(payloadJson)
        };
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, frame);
        return stream.ToArray();
    }

    [Fact]
    public async Task ProcessCompleteBinaryMessageAsync_WhenRoutingFails_ShouldRollbackSeqIdDedup_AndSendAck500()
    {
        // Arrange：真实 Router + 抛异常 handler → RouteBinaryMessageWithResultAsync 返回 false
        var router = new MessageRouter(NullLogger<MessageRouter>.Instance, new FeishuWebSocketOptions { MessageHandlerTimeoutMs = 0 });
        router.RegisterHandler(new ThrowingMessageHandler());

        var seqDedupMock = new Mock<Mud.Feishu.Abstractions.Services.IFeishuSeqIDDeduplicator>();
        seqDedupMock.Setup(d => d.TryMarkAsProcessedAsync(42UL)).ReturnsAsync(false);
        seqDedupMock.Setup(d => d.RollbackAsync(42UL)).Returns(Task.CompletedTask);

        var connectionManager = new Mock<WebSocketConnectionManager>(
            Mock.Of<ILogger<WebSocketConnectionManager>>(),
            new FeishuWebSocketOptions(),
            Mock.Of<ILoggerFactory>());

        var processor = new BinaryMessageProcessor(
            _loggerMock.Object,
            connectionManager.Object,
            _options,
            router,
            seqDedupMock.Object,
            sequenceValidator: null);

        var payload = Encoding.UTF8.GetBytes("{\"type\":\"event\",\"event_id\":\"evt_p01\"}");
        var data = CreateDataFrame(42UL, Encoding.UTF8.GetString(payload));

        var ackSent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        processor.BinaryMessageReceived += (_, args) =>
        {
            if (args.ProcessingSuccess == false)
                ackSent.TrySetResult(true);
        };

        // Act
        await processor.ProcessBinaryDataAsync(data, 0, data.Length, true, CancellationToken.None);
        await Task.WhenAny(ackSent.Task, Task.Delay(3000));

        // Assert：P0-1 生产路径——路由失败必须回滚 SeqID 去重
        seqDedupMock.Verify(d => d.TryMarkAsProcessedAsync(42UL), Times.Once);
        seqDedupMock.Verify(d => d.RollbackAsync(42UL), Times.Once);

        processor.Dispose();
    }

    [Fact]
    public async Task ProcessCompleteBinaryMessageAsync_WhenRoutingFails_ShouldRemoveSequenceFromValidatorWindow()
    {
        var router = new MessageRouter(NullLogger<MessageRouter>.Instance, new FeishuWebSocketOptions { MessageHandlerTimeoutMs = 0 });
        router.RegisterHandler(new ThrowingMessageHandler());

        var options = new FeishuWebSocketOptions();
        var validator = new MessageSequenceValidator(NullLogger<MessageSequenceValidator>.Instance, options);

        var seqDedupMock = new Mock<Mud.Feishu.Abstractions.Services.IFeishuSeqIDDeduplicator>();
        seqDedupMock.Setup(d => d.TryMarkAsProcessedAsync(77UL)).ReturnsAsync(false);

        var connectionManager = new Mock<WebSocketConnectionManager>(
            Mock.Of<ILogger<WebSocketConnectionManager>>(),
            new FeishuWebSocketOptions(),
            Mock.Of<ILoggerFactory>());

        var processor = new BinaryMessageProcessor(
            _loggerMock.Object,
            connectionManager.Object,
            options,
            router,
            seqDedupMock.Object,
            validator);

        var data = CreateDataFrame(77UL, "{\"type\":\"event\",\"event_id\":\"evt_validator\"}");
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        processor.BinaryMessageReceived += (_, args) =>
        {
            if (args.ProcessingSuccess == false)
                done.TrySetResult(true);
        };

        await processor.ProcessBinaryDataAsync(data, 0, data.Length, true, CancellationToken.None);
        await Task.WhenAny(done.Task, Task.Delay(3000));

        // 回滚后同 SeqID 不应再被判 Duplicate
        var second = validator.ValidateSequence(77UL);
        second.Should().NotBe(SequenceValidationResult.Duplicate,
            "P0-1：路由失败后验证器窗口记录必须被移除，同 SeqID 重发可重新处理");

        processor.Dispose();
    }
}
