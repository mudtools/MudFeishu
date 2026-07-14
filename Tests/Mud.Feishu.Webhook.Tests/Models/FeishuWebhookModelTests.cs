// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Webhook.Models;
using System.Text.Json;

namespace Mud.Feishu.Webhook.Tests.Models;

/// <summary>
/// EventVerificationRequest 单元测试
/// </summary>
public class EventVerificationRequestTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var request = new EventVerificationRequest();

        // Assert
        request.Type.Should().BeEmpty();
        request.Challenge.Should().BeEmpty();
        request.Token.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new EventVerificationRequest();

        // Act
        request.Type = "url_verification";
        request.Challenge = "test_challenge_code";
        request.Token = "test_token";

        // Assert
        request.Type.Should().Be("url_verification");
        request.Challenge.Should().Be("test_challenge_code");
        request.Token.Should().Be("test_token");
    }

    [Fact]
    public void Serialize_ShouldProduceCorrectJson()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Challenge = "test_challenge",
            Token = "test_token"
        };

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert
        json.Should().Contain("\"type\":\"url_verification\"");
        json.Should().Contain("\"challenge\":\"test_challenge\"");
        json.Should().Contain("\"token\":\"test_token\"");
    }

    [Fact]
    public void Deserialize_ShouldProduceCorrectObject()
    {
        // Arrange
        var json = "{\"type\":\"url_verification\",\"challenge\":\"test_challenge\",\"token\":\"test_token\"}";

        // Act
        var request = JsonSerializer.Deserialize<EventVerificationRequest>(json);

        // Assert
        request.Should().NotBeNull();
        request!.Type.Should().Be("url_verification");
        request.Challenge.Should().Be("test_challenge");
        request.Token.Should().Be("test_token");
    }
}

/// <summary>
/// EventVerificationResponse 单元测试
/// </summary>
public class EventVerificationResponseTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var response = new EventVerificationResponse();

        // Assert
        response.Challenge.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var response = new EventVerificationResponse();

        // Act
        response.Challenge = "test_challenge_code";

        // Assert
        response.Challenge.Should().Be("test_challenge_code");
    }

    [Fact]
    public void Serialize_ShouldProduceCorrectJson()
    {
        // Arrange
        var response = new EventVerificationResponse
        {
            Challenge = "test_challenge"
        };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert
        json.Should().Contain("\"challenge\":\"test_challenge\"");
    }

    [Fact]
    public void Deserialize_ShouldProduceCorrectObject()
    {
        // Arrange
        var json = "{\"challenge\":\"test_challenge\"}";

        // Act
        var response = JsonSerializer.Deserialize<EventVerificationResponse>(json);

        // Assert
        response.Should().NotBeNull();
        response!.Challenge.Should().Be("test_challenge");
    }
}

/// <summary>
/// FeishuWebhookRequest 单元测试
/// </summary>
public class FeishuWebhookRequestTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var request = new FeishuWebhookRequest();

        // Assert
        request.Encrypt.Should().BeNull();
        request.Timestamp.Should().Be(0);
        request.Nonce.Should().BeEmpty();
        request.Signature.Should().BeEmpty();
        request.Type.Should().BeEmpty();
        request.Challenge.Should().BeNull();
        request.AppId.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new FeishuWebhookRequest();

        // Act
        request.Encrypt = "encrypted_data";
        request.Timestamp = 1234567890;
        request.Nonce = "abc123";
        request.Signature = "signature_value";
        request.Type = "event";
        request.Challenge = "challenge_code";
        request.AppId = "cli_test_app";

        // Assert
        request.Encrypt.Should().Be("encrypted_data");
        request.Timestamp.Should().Be(1234567890);
        request.Nonce.Should().Be("abc123");
        request.Signature.Should().Be("signature_value");
        request.Type.Should().Be("event");
        request.Challenge.Should().Be("challenge_code");
        request.AppId.Should().Be("cli_test_app");
    }

    [Fact]
    public void Serialize_ShouldProduceCorrectJson()
    {
        // Arrange
        var request = new FeishuWebhookRequest
        {
            Encrypt = "encrypted_data",
            Timestamp = 1234567890,
            Nonce = "abc123",
            Signature = "signature_value",
            Type = "event"
        };

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert
        json.Should().Contain("\"encrypt\":\"encrypted_data\"");
        json.Should().Contain("\"timestamp\":1234567890");
        json.Should().Contain("\"nonce\":\"abc123\"");
        json.Should().Contain("\"signature\":\"signature_value\"");
        json.Should().Contain("\"type\":\"event\"");
        json.Should().NotContain("appId");
    }

    [Fact]
    public void Deserialize_ShouldProduceCorrectObject()
    {
        // Arrange
        var json = "{\"encrypt\":\"encrypted_data\",\"timestamp\":1234567890,\"nonce\":\"abc123\",\"signature\":\"signature_value\",\"type\":\"event\"}";

        // Act
        var request = JsonSerializer.Deserialize<FeishuWebhookRequest>(json);

        // Assert
        request.Should().NotBeNull();
        request!.Encrypt.Should().Be("encrypted_data");
        request.Timestamp.Should().Be(1234567890);
        request.Nonce.Should().Be("abc123");
        request.Signature.Should().Be("signature_value");
        request.Type.Should().Be("event");
    }

    [Fact]
    public void AppId_ShouldBeIgnoredInSerialization()
    {
        // Arrange
        var request = new FeishuWebhookRequest
        {
            Encrypt = "test",
            AppId = "cli_test_app"
        };

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert
        json.Should().NotContain("cli_test_app");
        json.Should().NotContain("AppId");
    }
}

/// <summary>
/// WebhookErrorResponse 单元测试
/// </summary>
public class WebhookErrorResponseTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var response = new WebhookErrorResponse();

        // Assert
        response.Success.Should().BeFalse();
        response.RequestId.Should().BeNull();
        response.Error.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange & Act
        var response = new WebhookErrorResponse
        {
            Success = false,
            RequestId = "req-001",
            Error = new WebhookErrorDetail { Code = 429, Message = "Too Many Requests" }
        };

        // Assert
        response.Success.Should().BeFalse();
        response.RequestId.Should().Be("req-001");
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(429);
        response.Error.Message.Should().Be("Too Many Requests");
    }

    [Fact]
    public void Serialize_ShouldProduceCorrectJson_WhenFullyPopulated()
    {
        // Arrange
        var response = new WebhookErrorResponse
        {
            Success = false,
            RequestId = "req-001",
            Error = new WebhookErrorDetail { Code = 500, Message = "Internal Server Error" }
        };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert
        json.Should().Contain("\"success\":false");
        json.Should().Contain("\"request_id\":\"req-001\"");
        json.Should().Contain("\"code\":500");
        json.Should().Contain("\"message\":\"Internal Server Error\"");
    }
}

/// <summary>
/// WebhookEmptyResponse 单元测试
/// </summary>
public class WebhookEmptyResponseTests
{
    [Fact]
    public void Constructor_ShouldInitializeSuccessAsTrue()
    {
        // Act
        var response = new WebhookEmptyResponse();

        // Assert
        response.Success.Should().BeTrue();
    }

    [Fact]
    public void Serialize_ShouldProduceCorrectJson()
    {
        // Arrange
        var response = new WebhookEmptyResponse();

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert
        json.Should().Contain("\"success\":true");
    }
}