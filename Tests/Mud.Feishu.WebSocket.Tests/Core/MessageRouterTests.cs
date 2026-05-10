// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// MessageRouter 消息路由器测试类
/// </summary>
public class MessageRouterTests
{
    private readonly Mock<ILogger<MessageRouter>> _loggerMock;
    private readonly FeishuWebSocketOptions _options;
    private readonly MessageRouter _router;

    public MessageRouterTests()
    {
        _loggerMock = new Mock<ILogger<MessageRouter>>();
        _options = new FeishuWebSocketOptions();
        _router = new MessageRouter(_loggerMock.Object, _options);
    }

    [Fact]
    public void RegisterHandler_ShouldAddHandler_WhenHandlerIsValid()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();

        // Act
        _router.RegisterHandler(handlerMock.Object);

        // Assert - No exception should be thrown
    }

    [Fact]
    public void RegisterHandler_ShouldThrowArgumentNullException_WhenHandlerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _router.RegisterHandler(null!));
    }

    [Fact]
    public void UnregisterHandler_ShouldReturnTrue_WhenHandlerExists()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();
        _router.RegisterHandler(handlerMock.Object);

        // Act
        var result = _router.UnregisterHandler(handlerMock.Object);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void UnregisterHandler_ShouldReturnFalse_WhenHandlerDoesNotExist()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();

        // Act
        var result = _router.UnregisterHandler(handlerMock.Object);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldNotThrow_WhenMessageIsEmpty()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();
        _router.RegisterHandler(handlerMock.Object);

        // Act & Assert - Should not throw
        await _router.RouteMessageAsync("", CancellationToken.None);
        await _router.RouteMessageAsync("   ", CancellationToken.None);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldRouteToCorrectHandler_WhenHandlerExists()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle("ping")).Returns(true);
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _router.RegisterHandler(handlerMock.Object);
        var message = "{\"type\":\"ping\",\"timestamp\":1234567890}";

        // Act
        await _router.RouteMessageAsync(message, CancellationToken.None);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldRouteToV2EventMessage_WhenSchemaIsV2()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle("event")).Returns(true);
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _router.RegisterHandler(handlerMock.Object);
        var message = "{\"schema\":\"2.0\",\"header\":{\"event_type\":\"im.message.receive_v1\"}}";

        // Act
        await _router.RouteMessageAsync(message, CancellationToken.None);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldRouteEventCallbackToEvent_WhenV1Format()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle("event")).Returns(true);
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _router.RegisterHandler(handlerMock.Object);
        var message = "{\"type\":\"event_callback\",\"timestamp\":1234567890}";

        // Act
        await _router.RouteMessageAsync(message, CancellationToken.None);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldRoutePingMessage_WhenTypeIsPing()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle("ping")).Returns(true);
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _router.RegisterHandler(handlerMock.Object);
        var message = "{\"type\":\"ping\",\"timestamp\":1234567890}";

        // Act
        await _router.RouteMessageAsync(message, CancellationToken.None);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldRoutePongMessage_WhenTypeIsPong()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle("pong")).Returns(true);
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _router.RegisterHandler(handlerMock.Object);
        var message = "{\"type\":\"pong\",\"timestamp\":1234567890}";

        // Act
        await _router.RouteMessageAsync(message, CancellationToken.None);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldNotCallHandler_WhenNoHandlerMatches()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle(It.IsAny<string>())).Returns(false);
        _router.RegisterHandler(handlerMock.Object);
        var message = "{\"type\":\"unknown_type\",\"timestamp\":1234567890}";

        // Act
        await _router.RouteMessageAsync(message, CancellationToken.None);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteBinaryMessageAsync_ShouldRouteCorrectly_WhenValidJsonProvided()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle("event")).Returns(true);
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _router.RegisterHandler(handlerMock.Object);
        var jsonContent = "{\"type\":\"event\",\"data\":\"test\"}";

        // Act
        await _router.RouteBinaryMessageAsync(jsonContent, "event", CancellationToken.None);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteBinaryMessageAsync_ShouldNotRoute_WhenJsonContentIsEmpty()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle(It.IsAny<string>())).Returns(true);
        _router.RegisterHandler(handlerMock.Object);

        // Act
        await _router.RouteBinaryMessageAsync("", "event", CancellationToken.None);
        await _router.RouteBinaryMessageAsync("   ", "event", CancellationToken.None);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldHandleMultipleHandlers_Correctly()
    {
        // Arrange
        var pingHandlerMock = new Mock<IMessageHandler>();
        pingHandlerMock.Setup(h => h.CanHandle("ping")).Returns(true);
        pingHandlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pongHandlerMock = new Mock<IMessageHandler>();
        pongHandlerMock.Setup(h => h.CanHandle("pong")).Returns(true);
        pongHandlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _router.RegisterHandler(pingHandlerMock.Object);
        _router.RegisterHandler(pongHandlerMock.Object);

        var pingMessage = "{\"type\":\"ping\",\"timestamp\":1234567890}";
        var pongMessage = "{\"type\":\"pong\",\"timestamp\":1234567890}";

        // Act
        await _router.RouteMessageAsync(pingMessage, CancellationToken.None);
        await _router.RouteMessageAsync(pongMessage, CancellationToken.None);

        // Assert
        pingHandlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        pongHandlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldSelectFirstMatchingHandler_WhenMultipleHandlersMatch()
    {
        // Arrange
        var firstHandlerMock = new Mock<IMessageHandler>();
        firstHandlerMock.Setup(h => h.CanHandle("ping")).Returns(true);
        firstHandlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var secondHandlerMock = new Mock<IMessageHandler>();
        secondHandlerMock.Setup(h => h.CanHandle("ping")).Returns(true);
        secondHandlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _router.RegisterHandler(firstHandlerMock.Object);
        _router.RegisterHandler(secondHandlerMock.Object);

        var message = "{\"type\":\"ping\",\"timestamp\":1234567890}";

        // Act
        await _router.RouteMessageAsync(message, CancellationToken.None);

        // Assert - Only first handler should be called
        firstHandlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        secondHandlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldHandleInvalidJson_WithoutThrowing()
    {
        // Arrange
        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle(It.IsAny<string>())).Returns(true);
        _router.RegisterHandler(handlerMock.Object);

        var invalidJson = "{ invalid json }";

        // Act & Assert - Should not throw
        await _router.RouteMessageAsync(invalidJson, CancellationToken.None);

        // Handler should not be called for invalid JSON
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
