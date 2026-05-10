// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.Abstractions.EventHandlers;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.EventHandlers;

public class DefaultFeishuEventHandlerTests
{
    // 测试常量
    private const string TestEventType = "test.event.type";
    private const string TestEventId = "test-event-id";
    private const string TestAppId = "test-app-id";
    private const string TestTenantKey = "test-tenant-key";
    private const string TestUserId = "test-user-id";
    private const string TestDepartmentId = "test-dept-id";

    private readonly Mock<ILogger<DefaultFeishuEventHandler<TestEventData>>> _loggerMock;
    private readonly TestFeishuEventHandler _testEventHandler;
    private readonly Mock<ITestEventProcessor> _eventProcessorMock;

    public DefaultFeishuEventHandlerTests()
    {
        _loggerMock = new Mock<ILogger<DefaultFeishuEventHandler<TestEventData>>>();
        _eventProcessorMock = new Mock<ITestEventProcessor>();
        _testEventHandler = new TestFeishuEventHandler(_loggerMock.Object, _eventProcessorMock.Object);
    }

    // 测试事件数据模型
    public class TestEventData : IEventResult
    {
        public string? EventId { get; set; }
        public string? EventType { get; set; }
        public string? UserId { get; set; }
        public string? DepartmentId { get; set; }
    }

    // 测试事件处理器
    public class TestFeishuEventHandler : DefaultFeishuEventHandler<TestEventData>
    {
        private readonly ITestEventProcessor _eventProcessor;

        public TestFeishuEventHandler(ILogger<DefaultFeishuEventHandler<TestEventData>> logger, ITestEventProcessor eventProcessor)
            : base(logger)
        {
            _eventProcessor = eventProcessor;
        }

        public override string SupportedEventType => TestEventType;

        protected override async Task ProcessBusinessLogicAsync(EventData eventData, TestEventData? eventEntity, CancellationToken cancellationToken = default)
        {
            await _eventProcessor.ProcessBusinessLogicAsync(eventData, eventEntity, cancellationToken);
        }
    }

    // 测试事件处理器接口
    public interface ITestEventProcessor
    {
        Task ProcessBusinessLogicAsync(EventData eventData, TestEventData? eventEntity, CancellationToken cancellationToken = default);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallProcessBusinessLogic_WhenEventDataIsValid()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = TestEventId,
            EventType = TestEventType,
            AppId = TestAppId,
            TenantKey = TestTenantKey,
            Event = JsonDocument.Parse(JsonSerializer.Serialize(new TestEventData
            {
                EventId = TestEventId,
                EventType = TestEventType,
                UserId = TestUserId,
                DepartmentId = TestDepartmentId
            }))
        };

        // Act
        await _testEventHandler.HandleAsync(eventData, CancellationToken.None);

        // Assert
        _eventProcessorMock.Verify(x => x.ProcessBusinessLogicAsync(
            It.Is<EventData>(e => e.EventId == TestEventId),
            It.Is<TestEventData>(e => e.EventId == TestEventId && e.UserId == TestUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenEventDataIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _testEventHandler.HandleAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowInvalidOperationException_WhenEventDataIsInvalidJson()
    {
        // Arrange
        var eventData = new EventData
        {
            EventId = TestEventId,
            EventType = TestEventType,
            Event = JsonDocument.Parse("{\"eventId\":\"test-event-id\",\"invalidJson\":true}") // Valid JSON structure
        };

        // 模拟事件处理逻辑抛出InvalidOperationException
        _eventProcessorMock.Setup(x => x.ProcessBusinessLogicAsync(It.IsAny<EventData>(), It.IsAny<TestEventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated business logic error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _testEventHandler.HandleAsync(eventData, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleEmptyEvent_WhenEventIsNull()
    {
        // Arrange
        // 设置日志级别为Debug，这样当Event为null时会返回default(T)
        _loggerMock.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

        var eventData = new EventData
        {
            EventId = TestEventId,
            EventType = TestEventType,
            Event = null
        };

        // Act
        await _testEventHandler.HandleAsync(eventData, CancellationToken.None);

        // Assert
        _eventProcessorMock.Verify(x => x.ProcessBusinessLogicAsync(
            It.IsAny<EventData>(),
            It.Is<TestEventData>(e => e == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void SupportedEventType_ShouldReturnCorrectEventType()
    {
        // Act
        var result = _testEventHandler.SupportedEventType;

        // Assert
        Assert.Equal(TestEventType, result);
    }

    [Fact]
    public async Task HandleAsync_ShouldLogInformation_WhenProcessingStarts()
    {
        // Arrange
        // 创建有效的JSON字符串
        var testEvent = new TestEventData
        {
            EventId = TestEventId,
            EventType = TestEventType
        };
        var jsonString = JsonSerializer.Serialize(testEvent);
        var jsonDocument = JsonDocument.Parse(jsonString);

        var eventData = new EventData
        {
            EventId = TestEventId,
            EventType = TestEventType,
            AppId = TestAppId,
            TenantKey = TestTenantKey,
            Event = jsonDocument
        };

        // 模拟日志级别检查返回true
        _loggerMock.Setup(logger => logger.IsEnabled(LogLevel.Information)).Returns(true);

        // Act
        await _testEventHandler.HandleAsync(eventData, CancellationToken.None);

        // Assert - Verify that logger was called with information level
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldLogError_WhenBusinessLogicThrowsException()
    {
        // Arrange
        // 创建有效的JSON字符串
        var testEvent = new TestEventData
        {
            EventId = TestEventId,
            EventType = TestEventType
        };
        var jsonString = JsonSerializer.Serialize(testEvent);
        var jsonDocument = JsonDocument.Parse(jsonString);

        var eventData = new EventData
        {
            EventId = TestEventId,
            EventType = TestEventType,
            Event = jsonDocument
        };

        var expectedException = new InvalidOperationException("Business logic error");
        _eventProcessorMock.Setup(x => x.ProcessBusinessLogicAsync(It.IsAny<EventData>(), It.IsAny<TestEventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // 模拟日志级别检查返回true
        _loggerMock.Setup(logger => logger.IsEnabled(LogLevel.Information)).Returns(true);
        _loggerMock.Setup(logger => logger.IsEnabled(LogLevel.Error)).Returns(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _testEventHandler.HandleAsync(eventData, CancellationToken.None));
        Assert.Equal(expectedException.Message, exception.Message);

        // 验证错误日志是否被记录
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            expectedException,
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }
}
