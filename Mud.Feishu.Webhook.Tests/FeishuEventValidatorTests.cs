// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using Xunit;
using Xunit.Abstractions;

namespace Mud.Feishu.Webhook.Tests;

/// <summary>
/// 飞书事件验证器单元测试
/// </summary>
public class FeishuEventValidatorTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<FeishuEventValidator> _logger;
    private readonly FeishuWebhookNonceDeduplicator _nonceDeduplicator;

    public FeishuEventValidatorTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new TestLogger<FeishuEventValidator>(output);
        _nonceDeduplicator = new FeishuWebhookNonceDeduplicator(_logger);
    }

    [Fact]
    public void ValidateSubscriptionRequest_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var validator = new FeishuEventValidator(_logger, _nonceDeduplicator);
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "test_token",
            Challenge = "test_challenge"
        };
        const string expectedToken = "test_token";

        // Act
        var result = validator.ValidateSubscriptionRequest(request, expectedToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSubscriptionRequest_InvalidType_ReturnsFalse()
    {
        // Arrange
        var validator = new FeishuEventValidator(_logger, _nonceDeduplicator);
        var request = new EventVerificationRequest
        {
            Type = "invalid_type",
            Token = "test_token",
            Challenge = "test_challenge"
        };
        const string expectedToken = "test_token";

        // Act
        var result = validator.ValidateSubscriptionRequest(request, expectedToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSubscriptionRequest_TokenMismatch_ReturnsFalse()
    {
        // Arrange
        var validator = new FeishuEventValidator(_logger, _nonceDeduplicator);
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "wrong_token",
            Challenge = "test_challenge"
        };
        const string expectedToken = "test_token";

        // Act
        var result = validator.ValidateSubscriptionRequest(request, expectedToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateTimestamp_ValidTimestamp_ReturnsTrue()
    {
        // Arrange
        var validator = new FeishuEventValidator(_logger, _nonceDeduplicator);
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var result = validator.ValidateTimestamp(currentTimestamp);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateTimestamp_TooOldTimestamp_ReturnsFalse()
    {
        // Arrange
        var validator = new FeishuEventValidator(_logger, _nonceDeduplicator);
        var oldTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeMilliseconds();

        // Act
        var result = validator.ValidateTimestamp(oldTimestamp);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateTimestamp_TooNewTimestamp_ReturnsFalse()
    {
        // Arrange
        var validator = new FeishuEventValidator(_logger, _nonceDeduplicator);
        var futureTimestamp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeMilliseconds();

        // Act
        var result = validator.ValidateTimestamp(futureTimestamp);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_ValidSignature_ReturnsTrue()
    {
        // Arrange
        var validator = new FeishuEventValidator(_logger, _nonceDeduplicator);
        const string encryptKey = "test_encrypt_key_32_chars_long!!!";
        const string nonce = "test_nonce";
        const string encrypt = "test_encrypt_data";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 计算正确的签名
        var signString = $"{timestamp}\n{nonce}\n{encrypt}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(encryptKey));
        var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signString));
        var signature = Convert.ToBase64String(hashBytes);

        // Act
        var result = validator.ValidateSignature(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_InvalidSignature_ReturnsFalse()
    {
        // Arrange
        var validator = new FeishuEventValidator(_logger, _nonceDeduplicator);
        const string encryptKey = "test_encrypt_key_32_chars_long!!!";
        const string nonce = "test_nonce";
        const string encrypt = "test_encrypt_data";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        const string invalidSignature = "invalid_signature";

        // Act
        var result = validator.ValidateSignature(timestamp, nonce, encrypt, invalidSignature, encryptKey);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_ReusedNonce_ReturnsFalse()
    {
        // Arrange
        var validator = new FeishuEventValidator(_logger, _nonceDeduplicator);
        const string encryptKey = "test_encrypt_key_32_chars_long!!!";
        const string nonce = "reused_nonce";
        const string encrypt = "test_encrypt_data";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 计算正确的签名
        var signString = $"{timestamp}\n{nonce}\n{encrypt}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(encryptKey));
        var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signString));
        var signature = Convert.ToBase64String(hashBytes);

        // 第一次验证应该成功
        var firstResult = validator.ValidateSignature(timestamp, nonce, encrypt, signature, encryptKey);
        Assert.True(firstResult);

        // 第二次使用相同的 nonce 应该失败（防重放）
        var secondResult = validator.ValidateSignature(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.False(secondResult);
    }

    [Fact]
    public void ValidateHeaderSignature_ValidSignature_ReturnsTrue()
    {
        // Arrange
        var validator = new FeishuEventValidator(_logger, _nonceDeduplicator);
        const string encryptKey = "test_encrypt_key_32_chars_long!!!";
        const string nonce = "test_nonce";
        const string body = "{\"test\": \"data\"}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 计算正确的签名
        var signString = $"{timestamp}\n{nonce}\n{body}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(encryptKey));
        var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signString));
        var signature = Convert.ToBase64String(hashBytes);

        // Act
        var result = validator.ValidateHeaderSignature(timestamp, nonce, body, signature, encryptKey);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateHeaderSignature_NullHeaderSignature_ReturnsTrue()
    {
        // Arrange
        var validator = new FeishuEventValidator(_logger, _nonceDeduplicator);
        const string encryptKey = "test_encrypt_key_32_chars_long!!!";
        const string nonce = "test_nonce";
        const string body = "{\"test\": \"data\"}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var result = validator.ValidateHeaderSignature(timestamp, nonce, body, null, encryptKey);

        // Assert
        Assert.True(result); // 如果请求头没有签名，跳过验证
    }
}

/// <summary>
/// 测试用的日志记录器
/// </summary>
internal class TestLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _output;

    public TestLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _output.WriteLine($"[{logLevel}] {formatter(state, exception)}");
    }
}
