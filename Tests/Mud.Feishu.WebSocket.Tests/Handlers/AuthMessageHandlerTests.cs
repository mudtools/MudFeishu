// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
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
/// AuthMessageHandler 认证消息处理器测试类
/// </summary>
public class AuthMessageHandlerTests
{
    private readonly Mock<ILogger<AuthMessageHandler>> _loggerMock;
    private readonly AuthMessageHandler _handler;
    private bool _authResult;

    public AuthMessageHandlerTests()
    {
        _loggerMock = new Mock<ILogger<AuthMessageHandler>>();
        _authResult = false;
        Action<bool> onAuthResult = (success) => _authResult = success;
        _handler = new AuthMessageHandler(_loggerMock.Object, onAuthResult);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOnAuthResultIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AuthMessageHandler(_loggerMock.Object, null!));
    }

    [Fact]
    public void CanHandle_ShouldReturnTrue_WhenMessageTypeIsAuth()
    {
        // Arrange
        var messageType = "auth";

        // Act
        var result = _handler.CanHandle(messageType);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_ShouldReturnFalse_WhenMessageTypeIsNotAuth()
    {
        // Arrange
        var messageTypes = new[] { "ping", "pong", "event", "heartbeat", "unknown" };

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
        var messageTypes = new[] { "auth", "Auth", "AUTH", "AuTh" };

        // Act & Assert
        foreach (var messageType in messageTypes)
        {
            _handler.CanHandle(messageType).Should().BeTrue();
        }
    }

    [Fact]
    public async Task HandleAsync_ShouldCallOnAuthResultWithTrue_WhenCodeIsZero()
    {
        // Arrange
        _authResult = false; // Reset
        var authResponseMessage = new AuthResponseMessage
        {
            Type = "auth",
            Code = 0,
            Message = "success",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var jsonMessage = JsonSerializer.Serialize(authResponseMessage, JsonOptions.Default);

        // Act
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);

        // Assert
        _authResult.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldCallOnAuthResultWithFalse_WhenCodeIsNonZero()
    {
        // Arrange
        _authResult = true; // Set to true to verify it changes
        var authResponseMessage = new AuthResponseMessage
        {
            Type = "auth",
            Code = 1,
            Message = "authentication failed",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var jsonMessage = JsonSerializer.Serialize(authResponseMessage, JsonOptions.Default);

        // Act
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);

        // Assert
        _authResult.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldCallOnAuthResultWithFalse_WhenCodeIsNegative()
    {
        // Arrange
        _authResult = true;
        var authResponseMessage = new AuthResponseMessage
        {
            Type = "auth",
            Code = -1,
            Message = "internal error",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var jsonMessage = JsonSerializer.Serialize(authResponseMessage, JsonOptions.Default);

        // Act
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);

        // Assert
        _authResult.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleNullCode_WhenCodePropertyIsMissing()
    {
        // Arrange
        _authResult = false;
        var jsonMessage = "{\"type\":\"auth\",\"message\":\"test\",\"timestamp\":1234567890}";

        // Act
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);

        // Assert - When code property is missing, int defaults to 0, which is treated as success
        // Note: This is the actual behavior - missing code defaults to 0 (success)
        _authResult.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleInvalidJson()
    {
        // Arrange
        _authResult = true;
        var invalidJson = "{ invalid auth json }";

        // Act & Assert - Should complete without throwing
        await _handler.HandleAsync(invalidJson, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleEmptyMessage()
    {
        // Arrange
        _authResult = true;
        var emptyMessage = "";

        // Act & Assert - Should complete without throwing
        await _handler.HandleAsync(emptyMessage, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleNullMessageInJson()
    {
        // Arrange
        _authResult = true;
        var authResponseMessage = new AuthResponseMessage
        {
            Type = "auth",
            Code = 1,
            Message = null,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var jsonMessage = JsonSerializer.Serialize(authResponseMessage, JsonOptions.Default);

        // Act & Assert - Should complete without throwing
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);
        _authResult.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleVariousErrorCodes()
    {
        // Arrange
        var errorCodes = new[] { 1, 1001, 1002, 1003, 9999 };

        foreach (var code in errorCodes)
        {
            _authResult = true;
            var authResponseMessage = new AuthResponseMessage
            {
                Type = "auth",
                Code = code,
                Message = $"error {code}",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            var jsonMessage = JsonSerializer.Serialize(authResponseMessage, JsonOptions.Default);

            // Act
            await _handler.HandleAsync(jsonMessage, CancellationToken.None);

            // Assert
            _authResult.Should().BeFalse($"Should return false for error code {code}");
        }
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleSuccessCaseWithErrorMessage()
    {
        // Arrange
        _authResult = false;
        var authResponseMessage = new AuthResponseMessage
        {
            Type = "auth",
            Code = 0,
            Message = "warning message but code is 0",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var jsonMessage = JsonSerializer.Serialize(authResponseMessage, JsonOptions.Default);

        // Act
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);

        // Assert - Code 0 is always success regardless of message
        _authResult.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var authResponseMessage = new AuthResponseMessage
        {
            Type = "auth",
            Code = 0,
            Message = "success",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var jsonMessage = JsonSerializer.Serialize(authResponseMessage, JsonOptions.Default);

        // Act & Assert - Should handle cancellation gracefully
        await _handler.HandleAsync(jsonMessage, cts.Token);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleZeroTimestamp()
    {
        // Arrange
        var authResponseMessage = new AuthResponseMessage
        {
            Type = "auth",
            Code = 0,
            Message = "success",
            Timestamp = 0
        };
        var jsonMessage = JsonSerializer.Serialize(authResponseMessage, JsonOptions.Default);

        // Act & Assert - Should complete without throwing
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);
        _authResult.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleVeryLargeErrorCodes()
    {
        // Arrange
        _authResult = true;
        var authResponseMessage = new AuthResponseMessage
        {
            Type = "auth",
            Code = int.MaxValue,
            Message = "max error code",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        var jsonMessage = JsonSerializer.Serialize(authResponseMessage, JsonOptions.Default);

        // Act
        await _handler.HandleAsync(jsonMessage, CancellationToken.None);

        // Assert
        _authResult.Should().BeFalse();
    }
}
