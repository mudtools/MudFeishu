// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// FeishuWebSocketHostedService 单元测试
/// </summary>
public class FeishuWebSocketHostedServiceTests
{
    private readonly Mock<ILogger<FeishuWebSocketHostedService>> _loggerMock;
    private readonly Mock<IFeishuWebSocketManager> _managerMock;
    private readonly FeishuWebSocketOptions _options;

    public FeishuWebSocketHostedServiceTests()
    {
        _loggerMock = new Mock<ILogger<FeishuWebSocketHostedService>>();
        _managerMock = new Mock<IFeishuWebSocketManager>();
        _options = new FeishuWebSocketOptions
        {
            EnableLogging = false,
            AutoReconnect = true,
            MaxReconnectAttempts = 3,
            ReconnectDelayMs = 1000,
            MaxReconnectDelayMs = 5000,
            HealthCheckIntervalMs = 60000,
            HeartbeatIntervalMs = 30000
        };
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuWebSocketHostedService(
            null!,
            _managerMock.Object,
            Options.Create(_options));

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuWebSocketHostedService(
            _loggerMock.Object,
            null!,
            Options.Create(_options));

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("webSocketManager");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            Options.Create(_options));

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void GetConnectionStats_ShouldReturnManagerStats()
    {
        // Arrange
        var expectedStats = (TimeSpan.FromMinutes(10), 2, (Exception?)null);
        _managerMock.Setup(x => x.GetConnectionStats()).Returns(expectedStats);

        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            Options.Create(_options));

        // Act
        var stats = service.GetConnectionStats();

        // Assert
        stats.Should().Be(expectedStats);
        _managerMock.Verify(x => x.GetConnectionStats(), Times.Once);
    }

    [Fact]
    public void GetConnectionState_ShouldReturnManagerState()
    {
        // Arrange
        var expectedState = WebSocketConnectionState.Connected(DateTime.UtcNow, 0);
        _managerMock.Setup(x => x.GetConnectionState()).Returns(expectedState);

        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            Options.Create(_options));

        // Act
        var state = service.GetConnectionState();

        // Assert
        state.Should().Be(expectedState);
        _managerMock.Verify(x => x.GetConnectionState(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldCallManagerStopAsync()
    {
        // Arrange
        _managerMock.Setup(x => x.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            Options.Create(_options));

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert
        _managerMock.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Dispose_ShouldNotThrowWhenCalledMultipleTimes()
    {
        // Arrange
        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            Options.Create(_options));

        // Act & Assert
        var action = () =>
        {
            service.Dispose();
            service.Dispose();
            service.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ShouldSubscribeToManagerEvents()
    {
        // Arrange & Act
        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            Options.Create(_options));

        // Assert - 验证事件订阅
        _managerMock.VerifyAdd(x => x.Connected += It.IsAny<EventHandler<EventArgs>>(), Times.Once);
        _managerMock.VerifyAdd(x => x.Disconnected += It.IsAny<EventHandler<WebSocketCloseEventArgs>>(), Times.Once);
        _managerMock.VerifyAdd(x => x.Error += It.IsAny<EventHandler<WebSocketErrorEventArgs>>(), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldUnsubscribeFromManagerEvents()
    {
        // Arrange
        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            Options.Create(_options));

        // Act
        service.Dispose();

        // Assert - 验证事件取消订阅
        _managerMock.VerifyRemove(x => x.Connected -= It.IsAny<EventHandler<EventArgs>>(), Times.Once);
        _managerMock.VerifyRemove(x => x.Disconnected -= It.IsAny<EventHandler<WebSocketCloseEventArgs>>(), Times.Once);
        _managerMock.VerifyRemove(x => x.Error -= It.IsAny<EventHandler<WebSocketErrorEventArgs>>(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldStopManagerAndDispose()
    {
        // Arrange
        _managerMock.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _managerMock.Setup(x => x.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _managerMock.Setup(x => x.IsConnected).Returns(true);

        var service = new FeishuWebSocketHostedService(
            _loggerMock.Object,
            _managerMock.Object,
            Options.Create(_options));

        // Act - 启动服务
        await service.StartAsync(CancellationToken.None);

        // 等待服务启动
        await Task.Delay(50);

        // 停止服务
        await service.StopAsync(CancellationToken.None);

        // Assert
        _managerMock.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }
}
