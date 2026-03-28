// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket.Tests.SocketEventArgs;

/// <summary>
/// SocketEventArgs 单元测试
/// </summary>
public class SocketEventArgsTests
{
    [Fact]
    public void WebSocketErrorEventArgs_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var args = new WebSocketErrorEventArgs();

        // Assert
        args.Exception.Should().BeNull();
        args.ErrorMessage.Should().BeNull();
        args.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        args.ErrorType.Should().BeNull();
        args.ConnectionState.Should().Be(WebSocketState.None);
        args.IsNetworkError.Should().BeFalse();
        args.IsAuthError.Should().BeFalse();
        args.IsRecoverable.Should().BeFalse();
        args.RecoveryRecommendation.Should().BeNull();
        args.SuggestedDelay.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void WebSocketErrorEventArgs_WithException_ShouldSetProperties()
    {
        // Arrange
        var exception = new Exception("Test error");

        // Act
        var args = new WebSocketErrorEventArgs
        {
            Exception = exception,
            ErrorMessage = "Test error message",
            ErrorType = "TestError",
            ConnectionState = WebSocketState.Closed,
            IsNetworkError = true,
            IsRecoverable = true
        };

        // Assert
        args.Exception.Should().Be(exception);
        args.ErrorMessage.Should().Be("Test error message");
        args.ErrorType.Should().Be("TestError");
        args.ConnectionState.Should().Be(WebSocketState.Closed);
        args.IsNetworkError.Should().BeTrue();
        args.IsRecoverable.Should().BeTrue();
    }

    [Fact]
    public void WebSocketCloseEventArgs_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var args = new WebSocketCloseEventArgs();

        // Assert
        args.CloseStatus.Should().BeNull();
        args.CloseStatusDescription.Should().BeNull();
        args.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        args.IsServerInitiated.Should().BeFalse();
        args.ConnectionDuration.Should().BeNull();
    }

    [Fact]
    public void WebSocketCloseEventArgs_WithValues_ShouldSetProperties()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(5);

        // Act
        var args = new WebSocketCloseEventArgs
        {
            CloseStatus = WebSocketCloseStatus.NormalClosure,
            CloseStatusDescription = "Normal closure",
            IsServerInitiated = true,
            ConnectionDuration = duration
        };

        // Assert
        args.CloseStatus.Should().Be(WebSocketCloseStatus.NormalClosure);
        args.CloseStatusDescription.Should().Be("Normal closure");
        args.IsServerInitiated.Should().BeTrue();
        args.ConnectionDuration.Should().Be(duration);
    }

    [Fact]
    public void WebSocketMessageEventArgs_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var args = new WebSocketMessageEventArgs();

        // Assert
        args.Message.Should().BeEmpty();
        args.MessageType.Should().Be(WebSocketMessageType.Text);
        args.EndOfMessage.Should().BeFalse();
        args.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        args.MessageSize.Should().Be(0);
        args.QueueCount.Should().Be(0);
    }

    [Fact]
    public void WebSocketMessageEventArgs_WithValues_ShouldSetProperties()
    {
        // Arrange & Act
        var args = new WebSocketMessageEventArgs
        {
            Message = "Test message",
            MessageType = WebSocketMessageType.Binary,
            EndOfMessage = true,
            MessageSize = 100,
            QueueCount = 5
        };

        // Assert
        args.Message.Should().Be("Test message");
        args.MessageType.Should().Be(WebSocketMessageType.Binary);
        args.EndOfMessage.Should().BeTrue();
        args.MessageSize.Should().Be(100);
        args.QueueCount.Should().Be(5);
    }

    [Fact]
    public void WebSocketFeishuEventArgs_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var args = new WebSocketFeishuEventArgs();

        // Assert
        args.EventMessage.Should().BeNull();
        args.IsV2Message.Should().BeFalse();
        args.EventData.Should().BeNull();
        args.EventType.Should().BeNull();
        args.EventTypeV2.Should().BeNull();
        args.EventId.Should().BeNull();
        args.EventIdV2.Should().BeNull();
        args.AppId.Should().BeNull();
        args.TenantKey.Should().BeNull();
        args.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        args.RawEvent.Should().BeNull();
    }

    [Fact]
    public void WebSocketFeishuEventArgs_WithV1Message_ShouldSetProperties()
    {
        // Arrange
        var eventMessage = new EventMessage
        {
            Data = new EventData
            {
                EventType = "contact.user.created",
                AppId = "cli_test",
                TenantKey = "tenant_test"
            }
        };

        // Act
        var args = new WebSocketFeishuEventArgs
        {
            EventMessage = eventMessage,
            IsV2Message = false,
            RawEvent = "{\"type\":\"event\"}"
        };

        // Assert
        args.EventMessage.Should().Be(eventMessage);
        args.IsV2Message.Should().BeFalse();
        args.EventData.Should().NotBeNull();
        args.EventType.Should().Be("contact.user.created");
        args.AppId.Should().Be("cli_test");
        args.TenantKey.Should().Be("tenant_test");
        args.RawEvent.Should().Be("{\"type\":\"event\"}");
    }

    [Fact]
    public void WebSocketFeishuEventArgs_WithV2Message_ShouldSetProperties()
    {
        // Arrange & Act
        var args = new WebSocketFeishuEventArgs
        {
            IsV2Message = true,
            EventTypeV2 = "im.message.receive_v1",
            EventIdV2 = "event_v2_id",
            RawEvent = "{\"header\":{\"event_type\":\"im.message.receive_v1\"}}"
        };

        // Assert
        args.IsV2Message.Should().BeTrue();
        args.EventType.Should().Be("im.message.receive_v1");
        args.EventId.Should().Be("event_v2_id");
        args.EventTypeV2.Should().Be("im.message.receive_v1");
        args.EventIdV2.Should().Be("event_v2_id");
    }

    [Fact]
    public void WebSocketHeartbeatEventArgs_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var args = new WebSocketHeartbeatEventArgs();

        // Assert
        args.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        args.HeartbeatMessage.Should().BeNull();
        args.Interval.Should().BeNull();
        args.Status.Should().BeNull();
    }

    [Fact]
    public void WebSocketHeartbeatEventArgs_WithValues_ShouldSetProperties()
    {
        // Arrange & Act
        var args = new WebSocketHeartbeatEventArgs
        {
            Interval = 30,
            Status = "ok"
        };

        // Assert
        args.Interval.Should().Be(30);
        args.Status.Should().Be("ok");
    }

    [Fact]
    public void WebSocketPingEventArgs_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var args = new WebSocketPingEventArgs();

        // Assert
        args.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void WebSocketPongEventArgs_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var args = new WebSocketPongEventArgs();

        // Assert
        args.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        args.LatencyMs.Should().BeNull();
        args.PongMessage.Should().BeNull();
    }

    [Fact]
    public void WebSocketPongEventArgs_WithValues_ShouldSetProperties()
    {
        // Arrange & Act
        var args = new WebSocketPongEventArgs
        {
            LatencyMs = 25
        };

        // Assert
        args.LatencyMs.Should().Be(25);
    }

    [Fact]
    public void WebSocketBinaryMessageEventArgs_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var args = new WebSocketBinaryMessageEventArgs();

        // Assert
        args.Data.Should().BeEmpty();
        args.DataSize.Should().Be(0);
        args.IsCompleteMessage.Should().BeTrue();
        args.MessageType.Should().BeNull();
        args.JsonContent.Should().BeNull();
        args.ParseError.Should().BeNull();
        args.SkipReason.Should().BeNull();
        args.IsParseSuccess.Should().BeTrue();
        args.MessageSequence.Should().Be(0);
        args.QueueCount.Should().Be(0);
    }

    [Fact]
    public void WebSocketBinaryMessageEventArgs_WithValues_ShouldSetProperties()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var args = new WebSocketBinaryMessageEventArgs
        {
            Data = data,
            MessageType = "event",
            JsonContent = "{\"type\":\"test\"}",
            MessageSequence = 12345,
            QueueCount = 10
        };

        // Assert
        args.Data.Should().Equal(data);
        args.DataSize.Should().Be(5);
        args.MessageType.Should().Be("event");
        args.JsonContent.Should().Be("{\"type\":\"test\"}");
        args.MessageSequence.Should().Be(12345);
        args.QueueCount.Should().Be(10);
    }

    [Fact]
    public void WebSocketBinaryMessageEventArgs_WithParseError_ShouldSetIsParseSuccessToFalse()
    {
        // Arrange & Act
        var args = new WebSocketBinaryMessageEventArgs
        {
            ParseError = "Invalid JSON format"
        };

        // Assert
        args.IsParseSuccess.Should().BeFalse();
        args.ParseError.Should().Be("Invalid JSON format");
    }
}
