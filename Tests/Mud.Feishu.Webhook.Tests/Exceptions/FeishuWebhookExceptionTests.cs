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
/// FeishuWebhookUnavailableException 单元测试（W-01）
/// </summary>
/// <remarks>
/// 语义契约：<c>ErrorType = ServiceUnavailable</c> 且 <c>IsRetryable = true</c>。
/// 该组合是"无法完成判定 ⇒ 请重推"的唯一表达，任何一处漂移都会让中间件/宿主的
/// 重试判定失准，故单独锁定。
/// </remarks>
public class FeishuWebhookUnavailableExceptionTests
{
    [Fact]
    public void Constructor_ShouldCarryServiceUnavailableSemantics()
    {
        // Act
        var exception = new FeishuWebhookUnavailableException("无法获取密钥");

        // Assert
        exception.ErrorType.Should().Be("ServiceUnavailable");
        exception.IsRetryable.Should().BeTrue("无法判定 = 可重试（503），不得与签名不匹配（403 终态）混淆");
        exception.RequestId.Should().BeNull();
        exception.Message.Should().Be("无法获取密钥");
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldPreserveCause()
    {
        // Arrange
        var inner = new InvalidOperationException("KeyVault 不可用");

        // Act
        var exception = new FeishuWebhookUnavailableException("验签链路内部故障", inner);

        // Assert
        exception.InnerException.Should().Be(inner, "归因必须保留原始异常");
        exception.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void Type_ShouldBeDerivedFromWebhookExceptionBase()
    {
        // 宿主/中间件可能以基类做兜底 catch，派生关系不得漂移
        typeof(FeishuWebhookException).IsAssignableFrom(typeof(FeishuWebhookUnavailableException))
            .Should().BeTrue();
    }
}
