// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.WebSocket.Configuration;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.Handlers;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Tests.Handlers;

/// <summary>
/// PingPongMessageHandler 单元测试
/// </summary>
public class PingPongMessageHandlerTests
{
    private readonly Mock<ILogger<PingPongMessageHandler>> _loggerMock;
    private readonly FeishuWebSocketOptions _options;
    private readonly Mock<Func<string, Task>> _sendMessageCallbackMock;
    private readonly PingPongMessageHandler _handler;

    public PingPongMessageHandlerTests()
    {
        _loggerMock = new Mock<ILogger<PingPongMessageHandler>>();
        _options = new FeishuWebSocketOptions { EnableLogging = false };
        _sendMessageCallbackMock = new Mock<Func<string, Task>>();

        _handler = new PingPongMessageHandler(
            _loggerMock.Object,
            _options,
            _sendMessageCallbackMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new PingPongMessageHandler(
            null!,
            _options,
            _message => Task.CompletedTask);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullCallback_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new PingPongMessageHandler(
            _loggerMock.Object,
            _options,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sendMessageCallback");
    }

    [Theory]
    [InlineData("ping")]
    [InlineData("PING")]
    [InlineData("Ping")]
    [InlineData("pong")]
    [InlineData("PONG")]
    [InlineData("Pong")]
    public void CanHandle_WithValidMessageType_ShouldReturnTrue(string messageType)
    {
        // Act
        var result = _handler.CanHandle(messageType);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("event")]
    [InlineData("auth")]
    [InlineData("")]
    [InlineData(null)]
    public void CanHandle_WithInvalidMessageType_ShouldReturnFalse(string? messageType)
    {
        // Act
        var result = _handler.CanHandle(messageType ?? "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WithPingMessage_ShouldSendPong()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var pingJson = JsonSerializer.Serialize(new PingMessage { Timestamp = timestamp });

        _sendMessageCallbackMock
            .Setup(x => x.Invoke(It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _handler.HandleAsync(pingJson);

        // Assert
        _sendMessageCallbackMock.Verify(
            x => x.Invoke(It.Is<string>(s => s.Contains("pong"))),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithPongMessage_ShouldTriggerEvent()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var pongJson = JsonSerializer.Serialize(new PongMessage { Timestamp = timestamp });

        var eventRaised = false;
        _handler.PongReceived += (s, e) => eventRaised = true;

        // Act
        await _handler.HandleAsync(pongJson);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidJson_ShouldNotThrow()
    {
        // Arrange
        var invalidJson = "not valid json";

        // Act & Assert - should not throw
        await _handler.HandleAsync(invalidJson);
    }

    [Fact]
    public async Task HandleAsync_WithPingMessage_AndLoggingEnabled_ShouldLog()
    {
        // Arrange
        var options = new FeishuWebSocketOptions { EnableLogging = true };
        var handler = new PingPongMessageHandler(
            _loggerMock.Object,
            options,
            _sendMessageCallbackMock.Object);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var pingJson = JsonSerializer.Serialize(new PingMessage { Timestamp = timestamp });

        _sendMessageCallbackMock
            .Setup(x => x.Invoke(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.HandleAsync(pingJson);

        // Assert - verify logging was called (debug level)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
