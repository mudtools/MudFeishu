// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.Handlers;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Tests.Handlers;

/// <summary>
/// HeartbeatMessageHandler 心跳消息处理器测试类
/// </summary>
public class HeartbeatMessageHandlerTests
{
    private readonly Mock<ILogger<HeartbeatMessageHandler>> _loggerMock;
    private readonly Mud.Feishu.WebSocket.FeishuWebSocketOptions _options;
    private readonly HeartbeatMessageHandler _handler;

    public HeartbeatMessageHandlerTests()
    {
        _loggerMock = new Mock<ILogger<HeartbeatMessageHandler>>();
        _options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions();
        _handler = new HeartbeatMessageHandler(_loggerMock.Object, _options);
    }

    [Fact]
    public void CanHandle_ShouldReturnTrue_WhenMessageTypeIsHeartbeat()
    {
        // Arrange
        var messageType = "heartbeat";

        // Act
        var result = _handler.CanHandle(messageType);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_ShouldReturnFalse_WhenMessageTypeIsNotHeartbeat()
    {
        // Arrange
        var messageTypes = new[] { "ping", "pong", "event", "auth", "unknown" };

        // Act & Assert
        foreach (var messageType in messageTypes)
        {
            _handler.CanHandle(messageType).Should().BeFalse();
        }
    }

    [Fact]
    public void CanHandle_ShouldBeCaseInsensitive()
    {
        // Arrange
        var messageTypes = new[] { "heartbeat", "Heartbeat", "HEARTBEAT", "HeArTbEaT" };

        // Act & Assert
        foreach (var messageType in messageTypes)
        {
            _handler.CanHandle(messageType).Should().BeTrue();
        }
    }

    [Fact]
    public async Task HandleAsync_ShouldCompleteSuccessfully_WhenValidHeartbeatMessage()
    {
        // Arrange
        var heartbeatMessage = new HeartbeatMessage
        {
            Type = "heartbeat",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Data = new HeartbeatData
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Status = "active"
            }
        };
        var jsonMessage = JsonSerializer.Serialize(heartbeatMessage, JsonOptions.Default);

        // Act & Assert - Should complete without throwing
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldCompleteSuccessfully_WhenHeartbeatMessageHasNullData()
    {
        // Arrange
        var heartbeatMessage = new HeartbeatMessage
        {
            Type = "heartbeat",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Data = null
        };
        var jsonMessage = JsonSerializer.Serialize(heartbeatMessage, JsonOptions.Default);

        // Act & Assert - Should complete without throwing
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldCompleteSuccessfully_WhenInvalidJson()
    {
        // Arrange
        var invalidJson = "{ invalid heartbeat json }";

        // Act & Assert - Should complete without throwing even with invalid JSON
        await _handler.HandleAsync(invalidJson, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldCompleteSuccessfully_WhenEmptyMessage()
    {
        // Arrange
        var emptyMessage = "";

        // Act & Assert - Should complete without throwing
        await _handler.HandleAsync(emptyMessage, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldCompleteSuccessfully_WhenDataFieldsAreMissing()
    {
        // Arrange
        var heartbeatMessage = new HeartbeatMessage
        {
            Type = "heartbeat",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Data = new HeartbeatData
            {
                // Missing Timestamp and Status
            }
        };
        var jsonMessage = JsonSerializer.Serialize(heartbeatMessage, JsonOptions.Default);

        // Act & Assert - Should complete without throwing
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("idle")]
    [InlineData("disconnected")]
    [InlineData("error")]
    public async Task HandleAsync_ShouldHandleDifferentStatusValues(string status)
    {
        // Arrange
        var heartbeatMessage = new HeartbeatMessage
        {
            Type = "heartbeat",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Data = new HeartbeatData
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Status = status
            }
        };
        var jsonMessage = JsonSerializer.Serialize(heartbeatMessage, JsonOptions.Default);

        // Act & Assert - Should complete without throwing for all status values
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MaxValue)]
    public async Task HandleAsync_ShouldHandleEdgeCaseTimestamps(long timestamp)
    {
        // Arrange
        var heartbeatMessage = new HeartbeatMessage
        {
            Type = "heartbeat",
            Timestamp = timestamp,
            Data = new HeartbeatData
            {
                Timestamp = timestamp,
                Status = "active"
            }
        };
        var jsonMessage = JsonSerializer.Serialize(heartbeatMessage, JsonOptions.Default);

        // Act & Assert - Should complete without throwing
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var heartbeatMessage = new HeartbeatMessage
        {
            Type = "heartbeat",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Data = new HeartbeatData
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Status = "active"
            }
        };
        var jsonMessage = JsonSerializer.Serialize(heartbeatMessage, JsonOptions.Default);

        cts.Cancel();

        // Act & Assert - Should handle cancellation gracefully
        await _handler.HandleAsync(jsonMessage, cts.Token);
    }
}
