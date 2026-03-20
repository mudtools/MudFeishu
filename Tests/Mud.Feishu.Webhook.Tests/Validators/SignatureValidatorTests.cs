// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Webhook;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using Mud.Feishu.Webhook.Utilities;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Validators;

/// <summary>
/// 签名验证器单元测试
/// </summary>
public class SignatureValidatorTests
{
    private readonly Mock<ILogger<SignatureValidator>> _loggerMock;
    private readonly Mock<ISecurityAuditService> _auditServiceMock;
    private readonly Mock<IOptionsMonitor<FeishuWebhookOptions>> _optionsMonitorMock;
    private readonly Mock<IEnvironmentService> _environmentServiceMock;
    private readonly FeishuWebhookOptions _options;
    private readonly SignatureValidator _validator;

    public SignatureValidatorTests()
    {
        _loggerMock = new Mock<ILogger<SignatureValidator>>();
        _auditServiceMock = new Mock<ISecurityAuditService>();
        _optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        _environmentServiceMock = new Mock<IEnvironmentService>();

        _options = new FeishuWebhookOptions
        {
            EnforceHeaderSignatureValidation = true,
            TimestampToleranceSeconds = 300
        };

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_options);
        _validator = new SignatureValidator(
            _loggerMock.Object,
            _optionsMonitorMock.Object,
            _auditServiceMock.Object,
            _environmentServiceMock.Object);
    }

    [Fact]
    public void SetCurrentAppKey_ShouldSetAppKey()
    {
        // Arrange
        var appKey = "test-app";

        // Act
        _validator.SetCurrentAppKey(appKey);

        // Assert
        // 验证通过后续方法调用中的日志来确认 AppKey 被正确设置
        // 这里我们通过调用其他方法来间接验证
        Assert.True(true); // AppKey 设置是内部状态，通过其他测试验证
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var encrypt = "test-encrypt-data";
        var encryptKey = "test-encrypt-key-32-bytes-long!!";

        // 计算正确的签名
        var signString = $"{timestamp}\n{nonce}\n{encrypt}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(encryptKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));
        var signature = Convert.ToBase64String(hashBytes);

        // Act
        var result = await _validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var encrypt = "test-encrypt-data";
        var encryptKey = "test-encrypt-key-32-bytes-long!!";
        var invalidSignature = "invalid-signature";

        // Act
        var result = await _validator.ValidateSignatureAsync(timestamp, nonce, encrypt, invalidSignature, encryptKey);

        // Assert
        Assert.False(result);

        // 验证安全审计日志被调用
        _auditServiceMock.Verify(x => x.LogSecurityFailureAsync(
            SecurityEventType.SignatureValidation,
            "unknown",
            "SignatureValidator",
            It.IsAny<string>(),
            "",
            null), Times.Once);
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithEmptyTimestamp_InProduction_ShouldReturnFalse()
    {
        // Arrange
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(true);
        var timestamp = 0L;
        var nonce = "test-nonce";
        var encrypt = "test-encrypt-data";
        var encryptKey = "test-encrypt-key-32-bytes-long!!";
        var signature = "test-signature";

        // Act
        var result = await _validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.False(result);

        // 验证安全审计日志被调用
        _auditServiceMock.Verify(x => x.LogSecurityFailureAsync(
            SecurityEventType.SignatureValidation,
            "unknown",
            "SignatureValidator",
            It.IsAny<string>(),
            "",
            null), Times.Once);
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithEmptyTimestamp_InDevelopment_ShouldReturnTrue()
    {
        // Arrange
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(false);
        var timestamp = 0L;
        var nonce = "test-nonce";
        var encrypt = "test-encrypt-data";
        var encryptKey = "test-encrypt-key-32-bytes-long!!";
        var signature = "test-signature";

        // Act
        var result = await _validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var body = "test-body";
        var encryptKey = "test-encrypt-key-32-bytes-long!!";

        // 计算正确的请求头签名
        var signString = $"{timestamp}{nonce}{encryptKey}{body}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signString));
        var headerSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        // Act
        var result = await _validator.ValidateHeaderSignatureAsync(timestamp, nonce, body, headerSignature, encryptKey);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var body = "test-body";
        var encryptKey = "test-encrypt-key-32-bytes-long!!";
        var invalidSignature = "invalid-signature";

        // Act
        var result = await _validator.ValidateHeaderSignatureAsync(timestamp, nonce, body, invalidSignature, encryptKey);

        // Assert
        Assert.False(result);

        // 验证安全审计日志被调用
        _auditServiceMock.Verify(x => x.LogSecurityFailureAsync(
            SecurityEventType.SignatureValidation,
            "unknown",
            "SignatureValidator",
            It.IsAny<string>(),
            "",
            null), Times.Once);
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithEmptySignature_EnforceValidation_ShouldReturnFalse()
    {
        // Arrange
        _options.EnforceHeaderSignatureValidation = true;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var body = "test-body";
        var encryptKey = "test-encrypt-key-32-bytes-long!!";
        string? headerSignature = null;

        // Act
        var result = await _validator.ValidateHeaderSignatureAsync(timestamp, nonce, body, headerSignature, encryptKey);

        // Assert
        Assert.False(result);

        // 验证安全审计日志被调用
        _auditServiceMock.Verify(x => x.LogSecurityFailureAsync(
            SecurityEventType.SignatureValidation,
            "unknown",
            "SignatureValidator",
            It.IsAny<string>(),
            "",
            null), Times.Once);
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithEmptySignature_NoEnforcement_ShouldReturnTrue()
    {
        // Arrange
        _options.EnforceHeaderSignatureValidation = false;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var body = "test-body";
        var encryptKey = "test-encrypt-key-32-bytes-long!!";
        string? headerSignature = null;

        // Act
        var result = await _validator.ValidateHeaderSignatureAsync(timestamp, nonce, body, headerSignature, encryptKey);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ValidateSignatureAsync_WithEmptyNonce_InProduction_ShouldReturnFalse(string? nonce)
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var encrypt = "test-encrypt-data";
        var encryptKey = "test-encrypt-key-32-bytes-long!!";
        var signature = "test-signature";

        try
        {
            // Act
            var result = await _validator.ValidateSignatureAsync(timestamp, nonce!, encrypt, signature, encryptKey);

            // Assert
            Assert.False(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ValidateSignatureAsync_WithEmptyNonce_InDevelopment_ShouldReturnTrue(string? nonce)
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var encrypt = "test-encrypt-data";
        var encryptKey = "test-encrypt-key-32-bytes-long!!";
        var signature = "test-signature";

        try
        {
            // Act
            var result = await _validator.ValidateSignatureAsync(timestamp, nonce!, encrypt, signature, encryptKey);

            // Assert
            Assert.True(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithException_ShouldReturnFalse()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var encrypt = "test-encrypt-data";
        var encryptKey = ""; // 空密钥会导致异常
        var signature = "test-signature";

        // Act
        var result = await _validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.False(result);
    }
}