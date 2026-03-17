// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.WebSocket.Configuration;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.Handlers;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Tests.Handlers;

/// <summary>
/// FeishuEventMessageHandler 单元测试
/// </summary>
public class FeishuEventMessageHandlerTests
{
    private readonly Mock<ILogger<FeishuEventMessageHandler>> _loggerMock;
    private readonly Mock<IFeishuEventHandlerFactory> _handlerFactoryMock;
    private readonly FeishuWebSocketOptions _options;

    public FeishuEventMessageHandlerTests()
    {
        _loggerMock = new Mock<ILogger<FeishuEventMessageHandler>>();
        _handlerFactoryMock = new Mock<IFeishuEventHandlerFactory>();
        _options = new FeishuWebSocketOptions();
    }

    private FeishuEventMessageHandler CreateHandler()
    {
        return new FeishuEventMessageHandler(
            _loggerMock.Object,
            _handlerFactoryMock.Object,
            null,
            null,
            null,
            null,
            _options);
    }

    [Fact]
    public void Constructor_WithNullEventHandlerFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuEventMessageHandler(
            _loggerMock.Object,
            null!,
            null,
            null,
            null,
            null,
            _options);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventHandlerFactory");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuEventMessageHandler(
            _loggerMock.Object,
            _handlerFactoryMock.Object,
            null,
            null,
            null,
            null,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Theory]
    [InlineData("event")]
    [InlineData("EVENT")]
    [InlineData("Event")]
    [InlineData("event_callback")]
    [InlineData("EVENT_CALLBACK")]
    [InlineData("binary_event")]
    [InlineData("BINARY_EVENT")]
    public void CanHandle_WithValidMessageType_ShouldReturnTrue(string messageType)
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = handler.CanHandle(messageType);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("auth")]
    [InlineData("ping")]
    [InlineData("pong")]
    [InlineData("")]
    public void CanHandle_WithInvalidMessageType_ShouldReturnFalse(string messageType)
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = handler.CanHandle(messageType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WithEmptyMessage_ShouldNotThrow()
    {
        // Arrange
        var handler = CreateHandler();

        // Act & Assert - should not throw
        await handler.HandleAsync("");
    }

    [Fact]
    public async Task HandleAsync_WithWhitespaceMessage_ShouldNotThrow()
    {
        // Arrange
        var handler = CreateHandler();

        // Act & Assert - should not throw
        await handler.HandleAsync("   ");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidJson_ShouldNotThrow()
    {
        // Arrange
        var handler = CreateHandler();

        // Act & Assert - should not throw
        await handler.HandleAsync("not valid json");
    }
}
