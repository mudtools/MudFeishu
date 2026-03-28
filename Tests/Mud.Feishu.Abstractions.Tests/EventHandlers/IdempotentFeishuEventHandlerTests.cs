// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Tests.EventHandlers;

/// <summary>
/// IdempotentFeishuEventHandler 单元测试
/// </summary>
public class IdempotentFeishuEventHandlerTests
{
    private readonly Mock<ILogger<TestIdempotentEventHandler>> _loggerMock;
    private readonly Mock<IFeishuEventDeduplicator> _deduplicatorMock;
    private readonly Mock<ITestEventProcessor> _eventProcessorMock;
    private readonly TestIdempotentEventHandler _handler;

    public IdempotentFeishuEventHandlerTests()
    {
        _loggerMock = new Mock<ILogger<TestIdempotentEventHandler>>();
        _deduplicatorMock = new Mock<IFeishuEventDeduplicator>();
        _eventProcessorMock = new Mock<ITestEventProcessor>();
        _handler = new TestIdempotentEventHandler(_deduplicatorMock.Object, _loggerMock.Object, _eventProcessorMock.Object);
    }

    // 测试事件数据模型
    public class TestEventData : IEventResult
    {
        public string? EventId { get; set; }
        public string? UserId { get; set; }
    }

    // 测试幂等性事件处理器
    public class TestIdempotentEventHandler : IdempotentFeishuEventHandler<TestEventData>
    {
        private readonly ITestEventProcessor _eventProcessor;

        public TestIdempotentEventHandler(
            IFeishuEventDeduplicator businessDeduplicator,
            ILogger logger,
            ITestEventProcessor eventProcessor)
            : base(businessDeduplicator, logger)
        {
            _eventProcessor = eventProcessor;
        }

        public override string SupportedEventType => "test.idempotent.event";

        protected override async Task ProcessBusinessLogicAsync(EventData eventData, TestEventData? eventEntity, CancellationToken cancellationToken = default)
        {
            await _eventProcessor.ProcessBusinessLogicAsync(eventData, eventEntity, cancellationToken);
        }

        protected override string? GetBusinessKey(EventData eventData)
        {
            return $"{eventData.EventType}:{eventData.EventId}";
        }
    }

    // 测试事件处理器接口
    public interface ITestEventProcessor
    {
        Task ProcessBusinessLogicAsync(EventData eventData, TestEventData? eventEntity, CancellationToken cancellationToken = default);
    }

    [Fact]
    public async Task HandleAsync_WhenFirstTime_ShouldProcessEvent()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = "test.idempotent.event",
            Event = JsonDocument.Parse(JsonSerializer.Serialize(new TestEventData
            {
                EventId = "test-event-id",
                UserId = "user-123"
            }))
        };

        _deduplicatorMock.Setup(d => d.TryMarkAsProcessing(It.IsAny<string>())).Returns(false);

        // Act
        await _handler.HandleAsync(eventData, CancellationToken.None);

        // Assert
        _eventProcessorMock.Verify(p => p.ProcessBusinessLogicAsync(
            It.IsAny<EventData>(),
            It.IsAny<TestEventData>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _deduplicatorMock.Verify(d => d.MarkAsCompleted(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenDuplicate_ShouldSkipProcessing()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = "test.idempotent.event"
        };

        _deduplicatorMock.Setup(d => d.TryMarkAsProcessing(It.IsAny<string>())).Returns(true);

        // Act
        await _handler.HandleAsync(eventData, CancellationToken.None);

        // Assert
        _eventProcessorMock.Verify(p => p.ProcessBusinessLogicAsync(
            It.IsAny<EventData>(),
            It.IsAny<TestEventData>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _deduplicatorMock.Verify(d => d.MarkAsCompleted(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenProcessingFails_ShouldRollback()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = "test.idempotent.event",
            Event = JsonDocument.Parse(JsonSerializer.Serialize(new TestEventData
            {
                EventId = "test-event-id",
                UserId = "user-123"
            }))
        };

        _deduplicatorMock.Setup(d => d.TryMarkAsProcessing(It.IsAny<string>())).Returns(false);
        _eventProcessorMock.Setup(p => p.ProcessBusinessLogicAsync(
            It.IsAny<EventData>(),
            It.IsAny<TestEventData>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Processing failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(eventData, CancellationToken.None));

        _deduplicatorMock.Verify(d => d.RollbackProcessing(It.IsAny<string>()), Times.Once);
        _deduplicatorMock.Verify(d => d.MarkAsCompleted(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenBusinessKeyIsEmpty_ShouldProcessWithoutDeduplication()
    {
        // Arrange
        var handler = new TestIdempotentEventHandlerWithEmptyKey(_deduplicatorMock.Object, _loggerMock.Object, _eventProcessorMock.Object);
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = "test.idempotent.event",
            Event = JsonDocument.Parse(JsonSerializer.Serialize(new TestEventData
            {
                EventId = "test-event-id",
                UserId = "user-123"
            }))
        };

        // Act
        await handler.HandleAsync(eventData, CancellationToken.None);

        // Assert
        _eventProcessorMock.Verify(p => p.ProcessBusinessLogicAsync(
            It.IsAny<EventData>(),
            It.IsAny<TestEventData>(),
            It.IsAny<CancellationToken>()), Times.Once);
        // 当业务键为空时，不会调用 TryMarkAsProcessing，直接处理事件
        _deduplicatorMock.Verify(d => d.TryMarkAsProcessing(It.IsAny<string>()), Times.Never);
    }

    // 测试处理器 - 返回空业务键
    public class TestIdempotentEventHandlerWithEmptyKey : IdempotentFeishuEventHandler<TestEventData>
    {
        private readonly ITestEventProcessor _eventProcessor;

        public TestIdempotentEventHandlerWithEmptyKey(
            IFeishuEventDeduplicator businessDeduplicator,
            ILogger logger,
            ITestEventProcessor eventProcessor)
            : base(businessDeduplicator, logger)
        {
            _eventProcessor = eventProcessor;
        }

        public override string SupportedEventType => "test.idempotent.event";

        protected override async Task ProcessBusinessLogicAsync(EventData eventData, TestEventData? eventEntity, CancellationToken cancellationToken = default)
        {
            await _eventProcessor.ProcessBusinessLogicAsync(eventData, eventEntity, cancellationToken);
        }

        protected override string? GetBusinessKey(EventData eventData)
        {
            return null; // 返回空业务键
        }
    }

    [Fact]
    public void SupportedEventType_ShouldReturnCorrectType()
    {
        // Act
        var result = _handler.SupportedEventType;

        // Assert
        Assert.Equal("test.idempotent.event", result);
    }
}
