// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Webhook.Exceptions;

namespace Mud.Feishu.Webhook.Tests.Exceptions;

/// <summary>
/// FeishuWebhookException 单元测试
/// </summary>
public class FeishuWebhookExceptionTests
{
    [Fact]
    public void Constructor_WithBasicParameters_ShouldSetProperties()
    {
        // Act
        var exception = new FeishuWebhookException("TestError", "Test message");

        // Assert
        exception.ErrorType.Should().Be("TestError");
        exception.Message.Should().Be("Test message");
        exception.RequestId.Should().BeNull();
        exception.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithRequestId_ShouldSetRequestId()
    {
        // Act
        var exception = new FeishuWebhookException("TestError", "Test message", "req-123");

        // Assert
        exception.RequestId.Should().Be("req-123");
    }

    [Fact]
    public void Constructor_WithIsRetryable_ShouldSetIsRetryable()
    {
        // Act
        var exception = new FeishuWebhookException("TestError", "Test message", isRetryable: true);

        // Assert
        exception.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new FeishuWebhookException("TestError", "Test message", innerException);

        // Assert
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_WithErrorCode_ShouldSetErrorCode()
    {
        // Act
        var exception = new FeishuWebhookException(400, "TestError", "Test message");

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new FeishuWebhookException(
            500,
            "TestError",
            "Test message",
            innerException,
            "req-456",
            isRetryable: true);

        // Assert
        exception.ErrorCode.Should().Be(500);
        exception.ErrorType.Should().Be("TestError");
        exception.Message.Should().Be("Test message");
        exception.InnerException.Should().Be(innerException);
        exception.RequestId.Should().Be("req-456");
        exception.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldIncludeAllInformation()
    {
        // Arrange
        var exception = new FeishuWebhookException(
            "TestError",
            "Test message",
            "req-789",
            isRetryable: true);

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("FeishuWebhookException");
        result.Should().Contain("Test message");
        result.Should().Contain("req-789");
        result.Should().Contain("TestError");
        result.Should().Contain("True");
    }

    [Fact]
    public void ToString_WithoutRequestId_ShouldNotIncludeRequestId()
    {
        // Arrange
        var exception = new FeishuWebhookException("TestError", "Test message");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().NotContain("RequestId:");
    }
}

/// <summary>
/// FeishuWebhookSecurityException 单元测试
/// </summary>
public class FeishuWebhookSecurityExceptionTests
{
    [Fact]
    public void Constructor_WithBasicParameters_ShouldSetProperties()
    {
        // Act
        var exception = new FeishuWebhookSecurityException("Security violation");

        // Assert
        exception.Message.Should().Be("Security violation");
        exception.ErrorType.Should().Be("SecurityException");
    }

    [Fact]
    public void Constructor_WithRequestId_ShouldSetRequestId()
    {
        // Act
        var exception = new FeishuWebhookSecurityException("Security violation", requestId: "req-123");

        // Assert
        exception.RequestId.Should().Be("req-123");
    }

    [Fact]
    public void Constructor_WithClientIp_ShouldSetClientIp()
    {
        // Act
        var exception = new FeishuWebhookSecurityException("Security violation", clientIp: "192.168.1.1");

        // Assert
        exception.ClientIp.Should().Be("192.168.1.1");
    }

    [Fact]
    public void Constructor_WithSecurityEventType_ShouldSetSecurityEventType()
    {
        // Act
        var exception = new FeishuWebhookSecurityException(
            "Security violation",
            securityEventType: SecurityEventType.SignatureValidation);

        // Assert
        exception.SecurityEventType.Should().Be(SecurityEventType.SignatureValidation);
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new FeishuWebhookSecurityException("Security violation", innerException);

        // Assert
        exception.InnerException.Should().Be(innerException);
    }
}

/// <summary>
/// FeishuWebhookValidationException 单元测试
/// </summary>
public class FeishuWebhookValidationExceptionTests
{
    [Fact]
    public void Constructor_WithBasicParameters_ShouldSetProperties()
    {
        // Act
        var exception = new FeishuWebhookValidationException("Validation failed");

        // Assert
        exception.Message.Should().Be("Validation failed");
        exception.ErrorType.Should().Be("ValidationError");
        exception.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithFieldName_ShouldSetFieldName()
    {
        // Act
        var exception = new FeishuWebhookValidationException("Invalid signature", fieldName: "signature");

        // Assert
        exception.FieldName.Should().Be("signature");
    }

    [Fact]
    public void Constructor_WithRequestId_ShouldSetRequestId()
    {
        // Act
        var exception = new FeishuWebhookValidationException("Validation failed", requestId: "req-123");

        // Assert
        exception.RequestId.Should().Be("req-123");
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new FeishuWebhookValidationException("Validation failed", innerException);

        // Assert
        exception.InnerException.Should().Be(innerException);
    }
}

/// <summary>
/// FeishuWebhookProcessingException 单元测试
/// </summary>
public class FeishuWebhookProcessingExceptionTests
{
    [Fact]
    public void Constructor_WithBasicParameters_ShouldSetProperties()
    {
        // Act
        var exception = new FeishuWebhookProcessingException("Processing failed");

        // Assert
        exception.Message.Should().Be("Processing failed");
        exception.ErrorType.Should().Be("ProcessingError");
        exception.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithEventType_ShouldSetEventType()
    {
        // Act
        var exception = new FeishuWebhookProcessingException("Processing failed", eventType: "message.created");

        // Assert
        exception.EventType.Should().Be("message.created");
    }

    [Fact]
    public void Constructor_WithEventId_ShouldSetEventId()
    {
        // Act
        var exception = new FeishuWebhookProcessingException("Processing failed", eventId: "evt-123");

        // Assert
        exception.EventId.Should().Be("evt-123");
    }

    [Fact]
    public void Constructor_WithRequestId_ShouldSetRequestId()
    {
        // Act
        var exception = new FeishuWebhookProcessingException("Processing failed", requestId: "req-123");

        // Assert
        exception.RequestId.Should().Be("req-123");
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new FeishuWebhookProcessingException("Processing failed", innerException);

        // Assert
        exception.InnerException.Should().Be(innerException);
    }
}

/// <summary>
/// FeishuWebhookDecryptionException 单元测试
/// </summary>
public class FeishuWebhookDecryptionExceptionTests
{
    [Fact]
    public void Constructor_WithBasicParameters_ShouldSetProperties()
    {
        // Act
        var exception = new FeishuWebhookDecryptionException("Decryption failed");

        // Assert
        exception.Message.Should().Be("Decryption failed");
        exception.ErrorType.Should().Be("DecryptionError");
        exception.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithAppId_ShouldSetAppId()
    {
        // Act
        var exception = new FeishuWebhookDecryptionException("Decryption failed", appId: "app-123");

        // Assert
        exception.AppId.Should().Be("app-123");
    }

    [Fact]
    public void Constructor_WithRequestId_ShouldSetRequestId()
    {
        // Act
        var exception = new FeishuWebhookDecryptionException("Decryption failed", requestId: "req-123");

        // Assert
        exception.RequestId.Should().Be("req-123");
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var innerException = new System.Security.Cryptography.CryptographicException("Crypto error");

        // Act
        var exception = new FeishuWebhookDecryptionException("Decryption failed", innerException);

        // Assert
        exception.InnerException.Should().Be(innerException);
    }
}

/// <summary>
/// FeishuWebhookSerializationException 单元测试
/// </summary>
public class FeishuWebhookSerializationExceptionTests
{
    [Fact]
    public void Constructor_WithBasicParameters_ShouldSetProperties()
    {
        // Act
        var exception = new FeishuWebhookSerializationException("Serialization failed");

        // Assert
        exception.Message.Should().Be("Serialization failed");
        exception.ErrorType.Should().Be("SerializationError");
        exception.Format.Should().Be("JSON");
        exception.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithCustomFormat_ShouldSetFormat()
    {
        // Act
        var exception = new FeishuWebhookSerializationException("Serialization failed", format: "XML");

        // Assert
        exception.Format.Should().Be("XML");
    }

    [Fact]
    public void Constructor_WithRequestId_ShouldSetRequestId()
    {
        // Act
        var exception = new FeishuWebhookSerializationException("Serialization failed", requestId: "req-123");

        // Assert
        exception.RequestId.Should().Be("req-123");
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var innerException = new System.Text.Json.JsonException("JSON error");

        // Act
        var exception = new FeishuWebhookSerializationException("Serialization failed", innerException);

        // Assert
        exception.InnerException.Should().Be(innerException);
    }
}
