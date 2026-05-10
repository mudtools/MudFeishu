// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Tests.EventHandlers;

/// <summary>
/// DefaultFeishuObjectEventHandler 单元测试
/// </summary>
public class DefaultFeishuObjectEventHandlerTests
{
    private readonly Mock<ILogger<TestObjectEventHandler>> _loggerMock;
    private readonly Mock<ITestEventProcessor> _eventProcessorMock;
    private readonly TestObjectEventHandler _handler;

    public DefaultFeishuObjectEventHandlerTests()
    {
        _loggerMock = new Mock<ILogger<TestObjectEventHandler>>();
        _eventProcessorMock = new Mock<ITestEventProcessor>();
        _handler = new TestObjectEventHandler(_loggerMock.Object, _eventProcessorMock.Object);
    }

    // 测试事件数据模型
    public class TestEventData : IEventResult
    {
        public string? EventId { get; set; }
        public string? UserId { get; set; }
        public string? DepartmentId { get; set; }
    }

    // 测试对象事件处理器
    public class TestObjectEventHandler : DefaultFeishuObjectEventHandler<TestEventData>
    {
        private readonly ITestEventProcessor _eventProcessor;

        public TestObjectEventHandler(ILogger logger, ITestEventProcessor eventProcessor)
            : base(logger)
        {
            _eventProcessor = eventProcessor;
        }

        public override string SupportedEventType => "test.object.event";

        protected override async Task ProcessBusinessLogicAsync(
            EventData eventData,
            ObjectEventResult<TestEventData>? eventEntity,
            CancellationToken cancellationToken = default)
        {
            await _eventProcessor.ProcessBusinessLogicAsync(eventData, eventEntity, cancellationToken);
        }
    }

    // 测试事件处理器接口
    public interface ITestEventProcessor
    {
        Task ProcessBusinessLogicAsync(EventData eventData, ObjectEventResult<TestEventData>? eventEntity, CancellationToken cancellationToken = default);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallProcessBusinessLogic_WhenEventDataIsValid()
    {
        // Arrange
        var eventId = "test-event-id";
        var eventType = "test.object.event";

        var objectEventData = new ObjectEventResult<TestEventData>
        {
            Object = new TestEventData
            {
                EventId = eventId,
                UserId = "test-user-id",
                DepartmentId = "test-dept-id"
            }
        };

        var eventData = new EventData
        {
            EventId = eventId,
            EventType = eventType,
            Event = JsonDocument.Parse(JsonSerializer.Serialize(objectEventData))
        };

        // Act
        await _handler.HandleAsync(eventData, CancellationToken.None);

        // Assert
        _eventProcessorMock.Verify(x => x.ProcessBusinessLogicAsync(
            It.IsAny<EventData>(),
            It.IsAny<ObjectEventResult<TestEventData>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenEventDataIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleEmptyEvent_WhenEventIsNull()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = "test.object.event",
            Event = null
        };

        // Act
        await _handler.HandleAsync(eventData, CancellationToken.None);

        // Assert
        _eventProcessorMock.Verify(x => x.ProcessBusinessLogicAsync(
            It.IsAny<EventData>(),
            It.Is<ObjectEventResult<TestEventData>?>(e => e == null || e.Object == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void SupportedEventType_ShouldReturnCorrectEventType()
    {
        // Act
        var result = _handler.SupportedEventType;

        // Assert
        Assert.Equal("test.object.event", result);
    }

    [Fact]
    public async Task HandleAsync_ShouldPropagateException_WhenBusinessLogicThrows()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = "test.object.event",
            Event = JsonDocument.Parse(JsonSerializer.Serialize(new TestEventData
            {
                EventId = "test-event-id",
                UserId = "test-user-id"
            }))
        };

        var expectedException = new InvalidOperationException("Business logic error");
        _eventProcessorMock.Setup(x => x.ProcessBusinessLogicAsync(
            It.IsAny<EventData>(),
            It.IsAny<ObjectEventResult<TestEventData>>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(eventData, CancellationToken.None));
        Assert.Equal(expectedException.Message, exception.Message);
    }
}
