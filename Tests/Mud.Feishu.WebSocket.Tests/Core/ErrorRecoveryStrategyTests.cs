// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.WebSocket;
using Mud.Feishu.WebSocket.Exceptions;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// ErrorRecoveryStrategy 单元测试
/// </summary>
public class ErrorRecoveryStrategyTests
{
    private readonly Mock<ILogger<ErrorRecoveryStrategy>> _loggerMock;
    private readonly ErrorRecoveryStrategy _strategy;

    public ErrorRecoveryStrategyTests()
    {
        _loggerMock = new Mock<ILogger<ErrorRecoveryStrategy>>();
        _strategy = new ErrorRecoveryStrategy(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ErrorRecoveryStrategy(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void AnalyzeError_WithGenericException_ShouldReturnRecoverable()
    {
        // Arrange
        var exception = new Exception("Generic error");

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeTrue();
        result.ErrorType.Should().Be("Exception");
    }

    [Fact]
    public void AnalyzeError_WithWebSocketException_Faulted_ShouldReturnRecoverable()
    {
        // Arrange
        var exception = new WebSocketException(WebSocketError.Faulted);

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeTrue();
        result.ErrorType.Should().Be("WebSocketException");
        result.RecoveryRecommendation.Should().Contain("重连");
    }

    [Fact]
    public void AnalyzeError_WithWebSocketException_InvalidState_ShouldReturnRecoverable()
    {
        // Arrange
        var exception = new WebSocketException(WebSocketError.InvalidState);

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeTrue();
        result.RecoveryRecommendation.Should().Contain("重新建立");
    }

    [Fact]
    public void AnalyzeError_WithWebSocketException_NotAWebSocket_ShouldNotBeRecoverable()
    {
        // Arrange
        var exception = new WebSocketException(WebSocketError.NotAWebSocket);

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeFalse();
        result.RecoveryRecommendation.Should().Contain("服务器配置");
    }

    [Fact]
    public void AnalyzeError_WithSocketException_ConnectionRefused_ShouldReturnRecoverable()
    {
        // Arrange
        var exception = new SocketException((int)SocketError.ConnectionRefused);

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeTrue();
        result.ErrorType.Should().Be("SocketException");
    }

    [Fact]
    public void AnalyzeError_WithSocketException_AddressNotAvailable_ShouldNotBeRecoverable()
    {
        // Arrange
        var exception = new SocketException((int)SocketError.AddressNotAvailable);

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeFalse();
        result.RecoveryRecommendation.Should().Contain("地址配置错误");
    }

    [Fact]
    public void AnalyzeError_WithHttpRequestException_ShouldReturnRecoverable()
    {
        // Arrange
        var exception = new HttpRequestException("Server error 500");

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeTrue();
        result.ErrorType.Should().Be("HttpRequestException");
    }

    [Fact]
    public void AnalyzeError_WithHttpRequestException_AuthError_ShouldNotBeRecoverable()
    {
        // Arrange
        var exception = new HttpRequestException("401 Unauthorized");

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeFalse();
        result.RecoveryRecommendation.Should().Contain("凭据");
    }

    [Fact]
    public void AnalyzeError_WithTimeoutException_ShouldReturnRecoverable()
    {
        // Arrange
        var exception = new TimeoutException("Operation timed out");

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeTrue();
        result.ErrorType.Should().Be("TimeoutException");
    }

    [Fact]
    public void AnalyzeError_WithOperationCanceledException_ShouldNotBeRecoverable()
    {
        // Arrange
        var exception = new OperationCanceledException();

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeFalse();
        result.ErrorType.Should().Be("OperationCanceledException");
    }

    [Fact]
    public void AnalyzeError_WithFeishuAuthenticationException_Recoverable_ShouldReturnRecoverable()
    {
        // Arrange - 使用内部构造函数传入 isRecoverable: true
        var exception = new FeishuAuthenticationException("Token expired", inner: null, errorCode: null, isRecoverable: true);

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeTrue();
        result.ErrorType.Should().Be("FeishuAuthenticationException");
    }

    [Fact]
    public void AnalyzeError_WithFeishuAuthenticationException_NotRecoverable_ShouldNotBeRecoverable()
    {
        // Arrange
        var exception = new FeishuAuthenticationException("Invalid credentials");

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.IsRecoverable.Should().BeFalse();
    }

    [Fact]
    public void AnalyzeError_WithFeishuConnectionException_ShouldReturnCorrectRecommendation()
    {
        // Arrange
        var exception = new FeishuConnectionException("Connection failed");

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.ErrorType.Should().Be("FeishuConnectionException");
        result.IsRecoverable.Should().BeTrue();
    }

    [Fact]
    public void AnalyzeError_WithFeishuNetworkException_ShouldReturnCorrectRecommendation()
    {
        // Arrange
        var exception = new FeishuNetworkException("Network error", 1);

        // Act
        var result = _strategy.AnalyzeError(exception);

        // Assert
        result.ErrorType.Should().Be("FeishuNetworkException");
        result.IsRecoverable.Should().BeTrue();
    }

    [Fact]
    public void AnalyzeError_WithContext_ShouldIncludeContextInResult()
    {
        // Arrange
        var exception = new Exception("Test error");
        var context = "TestContext";

        // Act
        var result = _strategy.AnalyzeError(exception, context);

        // Assert
        result.Context.Should().Be(context);
    }

    [Fact]
    public void AnalyzeError_ShouldSetTimestamp()
    {
        // Arrange
        var exception = new Exception("Test error");
        var beforeTime = DateTime.UtcNow;

        // Act
        var result = _strategy.AnalyzeError(exception);
        var afterTime = DateTime.UtcNow;

        // Assert
        result.Timestamp.Should().BeAfter(beforeTime.AddSeconds(-1));
        result.Timestamp.Should().BeBefore(afterTime.AddSeconds(1));
    }
}
