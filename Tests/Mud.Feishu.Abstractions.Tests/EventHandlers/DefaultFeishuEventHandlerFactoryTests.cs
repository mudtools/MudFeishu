// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Tests.EventHandlers;

/// <summary>
/// DefaultFeishuEventHandlerFactory 单元测试
/// </summary>
public class DefaultFeishuEventHandlerFactoryTests
{
    private readonly Mock<ILogger<DefaultFeishuEventHandlerFactory>> _loggerMock;
    private readonly Mock<IFeishuEventHandler> _defaultHandlerMock;
    private readonly Mock<IFeishuEventHandler> _handler1Mock;
    private readonly Mock<IFeishuEventHandler> _handler2Mock;

    public DefaultFeishuEventHandlerFactoryTests()
    {
        _loggerMock = new Mock<ILogger<DefaultFeishuEventHandlerFactory>>();
        _defaultHandlerMock = new Mock<IFeishuEventHandler>();
        _handler1Mock = new Mock<IFeishuEventHandler>();
        _handler2Mock = new Mock<IFeishuEventHandler>();

        _defaultHandlerMock.Setup(h => h.SupportedEventType).Returns("default");
        _handler1Mock.Setup(h => h.SupportedEventType).Returns("test.event.type1");
        _handler2Mock.Setup(h => h.SupportedEventType).Returns("test.event.type2");
    }

    [Fact]
    public void Constructor_ShouldRegisterAllHandlers()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, _handler2Mock.Object };

        // Act
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Assert
        Assert.True(factory.IsHandlerRegistered("test.event.type1"));
        Assert.True(factory.IsHandlerRegistered("test.event.type2"));
    }

    [Fact]
    public void GetHandler_WhenEventTypeExists_ShouldReturnCorrectHandler()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.GetHandler("test.event.type1");

        // Assert
        Assert.Equal(_handler1Mock.Object, result);
    }

    [Fact]
    public void GetHandler_WhenEventTypeNotExists_ShouldReturnDefaultHandler()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.GetHandler("non.existent.type");

        // Assert
        Assert.Equal(_defaultHandlerMock.Object, result);
    }

    [Fact]
    public void GetHandlers_WhenEventTypeExists_ShouldReturnAllMatchingHandlers()
    {
        // Arrange
        var handler3Mock = new Mock<IFeishuEventHandler>();
        handler3Mock.Setup(h => h.SupportedEventType).Returns("test.event.type1");

        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, handler3Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.GetHandlers("test.event.type1");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(_handler1Mock.Object, result);
        Assert.Contains(handler3Mock.Object, result);
    }

    [Fact]
    public void RegisterHandler_ShouldAddHandlerToFactory()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler>();
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        factory.RegisterHandler(_handler1Mock.Object);

        // Assert
        Assert.True(factory.IsHandlerRegistered("test.event.type1"));
    }

    [Fact]
    public void RegisterHandler_WhenHandlerIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler>();
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.RegisterHandler(null!));
    }

    [Fact]
    public void UnregisterHandler_WhenHandlerExists_ShouldReturnTrue()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.UnregisterHandler(_handler1Mock.Object);

        // Assert
        Assert.True(result);
        Assert.False(factory.IsHandlerRegistered("test.event.type1"));
    }

    [Fact]
    public void UnregisterHandler_WhenHandlerNotExists_ShouldReturnFalse()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler>();
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.UnregisterHandler(_handler1Mock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UnregisterHandlerByEventType_WhenEventTypeExists_ShouldReturnTrue()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.UnregisterHandler("test.event.type1");

        // Assert
        Assert.True(result);
        Assert.False(factory.IsHandlerRegistered("test.event.type1"));
    }

    [Fact]
    public void GetRegisteredEventTypes_ShouldReturnAllRegisteredTypes()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, _handler2Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.GetRegisteredEventTypes();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("test.event.type1", result);
        Assert.Contains("test.event.type2", result);
    }

    [Fact]
    public async Task HandleEventParallelAsync_ShouldCallAllHandlers()
    {
        // Arrange
        var handler3Mock = new Mock<IFeishuEventHandler>();
        handler3Mock.Setup(h => h.SupportedEventType).Returns("test.event.type1");

        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, handler3Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = "test.event.type1"
        };

        // Act
        await factory.HandleEventParallelAsync("test.event.type1", eventData, CancellationToken.None);

        // Assert
        _handler1Mock.Verify(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Once);
        handler3Mock.Verify(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleEventParallelAsync_WhenHandlerThrows_ShouldNotAffectOtherHandlers()
    {
        // Arrange
        var handler3Mock = new Mock<IFeishuEventHandler>();
        handler3Mock.Setup(h => h.SupportedEventType).Returns("test.event.type1");
        handler3Mock.Setup(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, handler3Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = "test.event.type1"
        };

        // Act
        await factory.HandleEventParallelAsync("test.event.type1", eventData, CancellationToken.None);

        // Assert - 两个处理器都应该被调用
        _handler1Mock.Verify(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Once);
        handler3Mock.Verify(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ClearHandlers_ShouldRemoveAllHandlers()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, _handler2Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        factory.ClearHandlers();

        // Assert
        Assert.Empty(factory.GetRegisteredEventTypes());
    }

    [Fact]
    public void GetHandlerInfo_ShouldReturnHandlerInformation()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, _handler2Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.GetHandlerInfo();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("test.event.type1"));
        Assert.True(result.ContainsKey("test.event.type2"));
    }
}
