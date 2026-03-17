// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.WebSocket.SocketEventArgs;

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
            EnableLogging = true,
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
            EnableLogging = false,
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

        // Act
        await processor.ProcessBinaryDataAsync(validProtobufData, 0, validProtobufData.Length, true, CancellationToken.None);

        // Wait for async processing
        await Task.Delay(100);

        // Assert
        receivedArgs.Should().NotBeNull();

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
        var eventTriggered = false;
        processor.BinaryMessageReceived += (sender, args) => eventTriggered = true;

        var validProtobufData = CreateValidProtobufData();

        // Act
        await processor.ProcessBinaryDataAsync(validProtobufData, 0, validProtobufData.Length, true, CancellationToken.None);

        // Wait for async processing
        await Task.Delay(200);

        // Assert
        eventTriggered.Should().BeTrue();

        processor.Dispose();
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
}
