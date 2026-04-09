// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.WebSocket;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket.Tests;

/// <summary>
/// WebSocketConnectionState 测试类
/// </summary>
public class WebSocketConnectionStateTests
{
    [Fact]
    public void Connected_ShouldCreateConnectedState()
    {
        // Arrange
        var connectedTime = DateTime.UtcNow;

        // Act
        var state = WebSocketConnectionState.Connected(connectedTime);

        // Assert
        state.IsConnected.Should().BeTrue();
        state.State.Should().Be(WebSocketState.Open);
        state.ConnectedTime.Should().Be(connectedTime);
        state.ReconnectCount.Should().Be(0);
        state.IsReconnecting.Should().BeFalse();
    }

    [Fact]
    public void Connected_ShouldSetReconnectCount()
    {
        // Arrange
        var connectedTime = DateTime.UtcNow;
        var reconnectCount = 5;

        // Act
        var state = WebSocketConnectionState.Connected(connectedTime, reconnectCount);

        // Assert
        state.ReconnectCount.Should().Be(reconnectCount);
    }

    [Fact]
    public void Disconnected_ShouldCreateDisconnectedState()
    {
        // Act
        var state = WebSocketConnectionState.Disconnected();

        // Assert
        state.IsConnected.Should().BeFalse();
        state.State.Should().Be(WebSocketState.Closed);
        state.LastError.Should().BeNull();
        state.LastErrorTime.Should().BeNull();
    }

    [Fact]
    public void Disconnected_ShouldSetLastError()
    {
        // Arrange
        var error = new Exception("Connection lost");

        // Act
        var state = WebSocketConnectionState.Disconnected(error);

        // Assert
        state.LastError.Should().Be(error);
        state.LastErrorTime.Should().NotBeNull();
        state.LastErrorTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Connecting_ShouldCreateConnectingState()
    {
        // Act
        var state = WebSocketConnectionState.Connecting;

        // Assert
        state.IsConnected.Should().BeFalse();
        state.State.Should().Be(WebSocketState.Connecting);
        state.IsReconnecting.Should().BeFalse();
    }

    [Fact]
    public void Reconnecting_ShouldCreateReconnectingState()
    {
        // Act
        var state = WebSocketConnectionState.Reconnecting;

        // Assert
        state.IsConnected.Should().BeFalse();
        state.State.Should().Be(WebSocketState.Connecting);
        state.IsReconnecting.Should().BeTrue();
    }

    [Fact]
    public void ConnectionDuration_ShouldBeZero_WhenNotConnected()
    {
        // Arrange
        var state = WebSocketConnectionState.Disconnected();

        // Act
        var duration = state.ConnectionDuration;

        // Assert
        duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ConnectionDuration_ShouldBePositive_WhenConnected()
    {
        // Arrange
        var connectedTime = DateTime.UtcNow.AddSeconds(-10);
        var state = WebSocketConnectionState.Connected(connectedTime);

        // Act
        var duration = state.ConnectionDuration;

        // Assert
        duration.Should().BeGreaterThan(TimeSpan.Zero);
        duration.Should().BeCloseTo(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ConnectionDuration_ShouldBeAccurate()
    {
        // Arrange
        var connectedTime = DateTime.UtcNow.AddMinutes(-5);
        var state = WebSocketConnectionState.Connected(connectedTime);

        // Act
        var duration = state.ConnectionDuration;

        // Assert
        duration.Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ConnectedTime_ShouldBeMinValue_WhenDisconnected()
    {
        // Arrange
        var state = WebSocketConnectionState.Disconnected();

        // Assert
        state.ConnectedTime.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void ConnectedTime_ShouldBeMinValue_WhenConnecting()
    {
        // Arrange
        var state = WebSocketConnectionState.Connecting;

        // Assert
        state.ConnectedTime.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void ConnectedTime_ShouldBeMinValue_WhenReconnecting()
    {
        // Arrange
        var state = WebSocketConnectionState.Reconnecting;

        // Assert
        state.ConnectedTime.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void LastError_ShouldBeSettable()
    {
        var state = WebSocketConnectionState.Disconnected(new Exception("Test error"));

        state.LastError.Should().NotBeNull();
    }

    [Fact]
    public void ReconnectCount_ShouldBeSettable()
    {
        var state = WebSocketConnectionState.Connected(DateTime.UtcNow, reconnectCount: 10);

        state.ReconnectCount.Should().Be(10);
    }

    [Fact]
    public void State_ShouldBeImmutablySet_WhenCreated()
    {
        // Arrange
        var state1 = WebSocketConnectionState.Connected(DateTime.UtcNow);
        var state2 = WebSocketConnectionState.Disconnected();

        // Act & Assert
        state1.State.Should().Be(WebSocketState.Open);
        state2.State.Should().Be(WebSocketState.Closed);
    }

    [Fact]
    public void MultipleConnectedStates_ShouldHaveDifferentConnectedTimes()
    {
        // Arrange
        var time1 = DateTime.UtcNow.AddMinutes(-10);
        var time2 = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var state1 = WebSocketConnectionState.Connected(time1);
        var state2 = WebSocketConnectionState.Connected(time2);

        // Assert
        state1.ConnectionDuration.Should().BeGreaterThan(state2.ConnectionDuration);
    }

    [Fact]
    public void ReconnectingState_ShouldHaveDifferentIsReconnecting_ThanConnectingState()
    {
        // Arrange
        var connectingState = WebSocketConnectionState.Connecting;
        var reconnectingState = WebSocketConnectionState.Reconnecting;

        // Act & Assert
        connectingState.IsReconnecting.Should().BeFalse();
        reconnectingState.IsReconnecting.Should().BeTrue();
    }

    [Fact]
    public void ShouldHandleNullErrorGracefully()
    {
        // Arrange & Act
        var state = WebSocketConnectionState.Disconnected(null);

        // Assert
        state.LastError.Should().BeNull();
        state.LastErrorTime.Should().BeNull();
    }

    [Fact]
    public void ShouldHandleExceptionWithMessage()
    {
        // Arrange
        var errorMessage = "Network timeout occurred";
        var error = new Exception(errorMessage);

        // Act
        var state = WebSocketConnectionState.Disconnected(error);

        // Assert
        state.LastError.Should().NotBeNull();
        state.LastError!.Message.Should().Be(errorMessage);
    }

    [Fact]
    public void ShouldHandleExceptionWithInnerException()
    {
        // Arrange
        var innerError = new InvalidOperationException("Inner error");
        var error = new Exception("Outer error", innerError);

        // Act
        var state = WebSocketConnectionState.Disconnected(error);

        // Assert
        state.LastError.Should().NotBeNull();
        state.LastError!.InnerException.Should().Be(innerError);
    }

    [Fact]
    public void ShouldBeRecordType()
    {
        // Arrange
        var state1 = WebSocketConnectionState.Connected(DateTime.UtcNow, 5);
        var state2 = WebSocketConnectionState.Connected(state1.ConnectedTime, state1.ReconnectCount);

        // Act & Assert
        state1.Should().Be(state2);
    }
}
