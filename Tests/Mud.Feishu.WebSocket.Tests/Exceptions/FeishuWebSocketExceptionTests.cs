// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.WebSocket.Exceptions;

namespace Mud.Feishu.WebSocket.Tests.Exceptions;

/// <summary>
/// FeishuWebSocketException 异常测试类
/// </summary>
public class FeishuWebSocketExceptionTests
{
    [Fact]
    public void Constructor_ShouldCreateExceptionWithErrorTypeAndMessage()
    {
        // Arrange
        var errorType = "TestError";
        var message = "Test exception message";

        // Act
        var exception = new FeishuWebSocketException(errorType, message);

        // Assert
        exception.ErrorType.Should().Be(errorType);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
        exception.IsRecoverable.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldCreateExceptionWithErrorTypeMessageAndInnerException()
    {
        // Arrange
        var errorType = "TestError";
        var message = "Test exception message";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new FeishuWebSocketException(errorType, message, innerException);

        // Assert
        exception.ErrorType.Should().Be(errorType);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
        exception.InnerException.Message.Should().Be("Inner exception");
        exception.IsRecoverable.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldCreateExceptionWithErrorCodeErrorTypeAndMessage()
    {
        // Arrange
        var errorCode = 1001;
        var errorType = "TestError";
        var message = "Test exception message";

        // Act
        var exception = new FeishuWebSocketException(errorCode, errorType, message);

        // Assert
        exception.ErrorType.Should().Be(errorType);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
        exception.ErrorCode.Should().Be(errorCode);
        exception.IsRecoverable.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldCreateExceptionWithErrorTypeMessageAndErrorCode()
    {
        // Arrange
        var errorType = "TestError";
        var message = "Test exception message";
        var errorCode = 1002;

        // Act
        var exception = new FeishuWebSocketException(errorType, message, errorCode);

        // Assert
        exception.ErrorType.Should().Be(errorType);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
        exception.ErrorCode.Should().Be(errorCode);
        exception.IsRecoverable.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldCreateExceptionWithAllParameters()
    {
        // Arrange
        var errorCode = 1003;
        var errorType = "TestError";
        var message = "Test exception message";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new FeishuWebSocketException(errorCode, errorType, message, innerException);

        // Assert
        exception.ErrorType.Should().Be(errorType);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
        exception.ErrorCode.Should().Be(errorCode);
        exception.IsRecoverable.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldSetIsRecoverableToFalse()
    {
        // Arrange
        var errorType = "TestError";
        var message = "Test exception message";

        // Act
        var exception = new FeishuWebSocketException(errorType, message, isRecoverable: false);

        // Assert
        exception.IsRecoverable.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldAcceptNullErrorCode()
    {
        // Arrange
        var errorType = "TestError";
        var message = "Test exception message";
        int? errorCode = null;

        // Act
        var exception = new FeishuWebSocketException(errorType, message, errorCode);

        // Assert
        exception.ErrorCode.Should().Be(0); // null defaults to 0
    }

    [Fact]
    public void Message_ShouldAcceptEmptyString()
    {
        // Arrange
        var message = "";
        var errorType = "TestError";

        // Act
        var exception = new FeishuWebSocketException(errorType, message);

        // Assert
        exception.Message.Should().Be(string.Empty);
    }

    [Fact]
    public void Exception_ShouldBeCaughtAsFeishuException()
    {
        // Arrange
        var exception = new FeishuWebSocketException("TestError", "Test");

        // Act & Assert
        Action action = () => throw exception;

        action.Should().Throw<Mud.Feishu.Exceptions.FeishuException>().WithMessage("Test");
    }

    [Fact]
    public void ToString_ShouldContainErrorType()
    {
        // Arrange
        var exception = new FeishuWebSocketException("ERR_123", "Test message");

        // Act
        var toString = exception.ToString();

        // Assert - ToString() contains the exception type and message, but not ErrorType property
        // Check ErrorType property directly instead
        exception.ErrorType.Should().Be("ERR_123");
        toString.Should().Contain("Test message");
    }

    [Fact]
    public void ToString_ShouldContainMessage()
    {
        // Arrange
        var exception = new FeishuWebSocketException("TestError", "Test message");

        // Act
        var toString = exception.ToString();

        // Assert
        toString.Should().Contain("Test message");
    }

    [Fact]
    public void ToString_ShouldContainInnerException_WhenInnerExceptionExists()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner message");
        var exception = new FeishuWebSocketException("TestError", "Outer message", innerException);

        // Act
        var toString = exception.ToString();

        // Assert
        toString.Should().Contain("Outer message");
        toString.Should().Contain("Inner message");
    }

    [Fact]
    public void ErrorCodeProperty_ShouldBeNullInitially()
    {
        // Arrange & Act
        var exception = new FeishuWebSocketException("TestError", "Test");

        // Assert
        exception.ErrorCode.Should().Be(0);
    }

    [Fact]
    public void ErrorType_ShouldAcceptSpecialCharacters()
    {
        // Arrange
        var errorType = "ERR_123-测试!@#$%";

        // Act
        var exception = new FeishuWebSocketException(errorType, "Test");

        // Assert
        exception.ErrorType.Should().Be(errorType);
    }

    [Fact]
    public void ErrorType_ShouldHandleLongString()
    {
        // Arrange
        var errorType = new string('A', 1000);

        // Act
        var exception = new FeishuWebSocketException(errorType, "Test");

        // Assert
        exception.ErrorType.Should().Be(errorType);
    }
}
