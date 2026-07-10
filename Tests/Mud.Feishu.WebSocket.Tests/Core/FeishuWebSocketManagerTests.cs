// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.WsEndpoint;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// FeishuWebSocketManager 单元测试
/// </summary>
public class FeishuWebSocketManagerTests
{
    private readonly Mock<ILogger<FeishuWebSocketManager>> _loggerMock;
    private readonly Mock<IFeishuAppContext> _appContextMock;
    private readonly Mock<IFeishuWebSocketClient> _clientMock;
    private readonly FeishuWebSocketOptions _options;
    private readonly Mock<IOptionsMonitor<FeishuWebSocketOptions>> _optionsMonitorMock;

    public FeishuWebSocketManagerTests()
    {
        _loggerMock = new Mock<ILogger<FeishuWebSocketManager>>();
        _appContextMock = new Mock<IFeishuAppContext>();
        _clientMock = new Mock<IFeishuWebSocketClient>();
        _options = new FeishuWebSocketOptions
        {
            EnableLogging = false
        };
        _optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebSocketOptions>>();
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);

        SetupAppContextMock();
    }

    private void SetupAppContextMock()
    {
        var config = new FeishuAppConfig
        {
            AppKey = "test_app_key",
            AppId = "cli_test_app_id",
            AppSecret = "test_app_secret_key_123",
            TimeOut = 30,
            RetryCount = 3,
            RetryDelayMs = 1000
        };

        _appContextMock.Setup(x => x.Config).Returns(config);

        var authMock = new Mock<IFeishuAuthentication>();
        var wsEndpointResult = new WsEndpointResult
        {
            Url = "wss://test.feishu.com/ws"
        };
        var wsEndpointResponse = new FeishuApiResult<WsEndpointResult>
        {
            Code = 0,
            Msg = "success",
            Data = wsEndpointResult
        };
        authMock.Setup(x => x.GetWebSocketEndpointAsync(It.IsAny<WsAppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(wsEndpointResponse);

        _appContextMock.Setup(x => x.Authentication).Returns(authMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuWebSocketManager(
            null!,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullAppContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuWebSocketManager(
            _loggerMock.Object,
            null!,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("appContext");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            null!,
            _clientMock.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("webSocketOptions");
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("webSocketClient");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Assert
        manager.Should().NotBeNull();
        manager.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Client_ShouldReturnWebSocketClient()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        var client = manager.Client;

        // Assert
        client.Should().NotBeNull();
        client.Should().Be(_clientMock.Object);
    }

    [Fact]
    public void IsConnected_WhenClientNotConnected_ShouldReturnFalse()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.None);
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act & Assert
        manager.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void IsConnected_WhenClientConnected_ShouldReturnTrue()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.Open);
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act & Assert
        manager.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void GetConnectionStats_WhenNotStarted_ShouldReturnZeroStats()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        var stats = manager.GetConnectionStats();

        // Assert
        stats.Uptime.Should().Be(TimeSpan.Zero);
        stats.ReconnectCount.Should().Be(0);
        stats.LastError.Should().BeNull();
    }

    [Fact]
    public void GetConnectionState_WhenNotConnected_ShouldReturnDisconnectedState()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.Closed);
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        var state = manager.GetConnectionState();

        // Assert
        state.IsConnected.Should().BeFalse();
        state.State.Should().Be(WebSocketState.Closed);
    }

    [Fact]
    public void GetConnectionState_WhenConnected_ShouldReturnConnectedState()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.Open);
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        var state = manager.GetConnectionState();

        // Assert
        state.IsConnected.Should().BeTrue();
        state.State.Should().Be(WebSocketState.Open);
    }

    [Fact]
    public async Task SendMessageAsync_WhenNotConnected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.Closed);
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act & Assert
        var action = () => manager.SendMessageAsync("test message");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*未连接*");
    }

    [Fact]
    public async Task SendMessageAsync_WhenConnected_ShouldCallClientSendMessage()
    {
        // Arrange
        _clientMock.Setup(x => x.State).Returns(WebSocketState.Open);
        _clientMock.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        await manager.SendMessageAsync("test message");

        // Assert
        _clientMock.Verify(x => x.SendMessageAsync("test message", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhenNotRunning_ShouldReturnWithoutError()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act & Assert
        var action = () => manager.StopAsync();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void Connected_Event_CanBeSubscribed()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);
        var eventRaised = false;
        manager.Connected += (s, e) => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void Disconnected_Event_CanBeSubscribed()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);
        var eventRaised = false;
        manager.Disconnected += (s, e) => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void MessageReceived_Event_CanBeSubscribed()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);
        var eventRaised = false;
        manager.MessageReceived += (s, e) => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void Error_Event_CanBeSubscribed()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);
        var eventRaised = false;
        manager.Error += (s, e) => eventRaised = true;

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldNotThrowWhenCalledMultipleTimes()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act & Assert
        var action = () =>
        {
            manager.Dispose();
            manager.Dispose();
            manager.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldDisposeClient()
    {
        // Arrange
        var manager = new FeishuWebSocketManager(
            _loggerMock.Object,
            _appContextMock.Object,
            _optionsMonitorMock.Object,
            _clientMock.Object);

        // Act
        manager.Dispose();

        // Assert
        _clientMock.Verify(x => x.Dispose(), Times.Once);
    }
}
