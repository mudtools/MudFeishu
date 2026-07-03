// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using Mud.Feishu.Webhook.Utilities;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Validators;

/// <summary>
/// 签名验证器单元测试
/// 验证 SignatureValidator 类的各种签名验证场景
/// 参考飞书 Go SDK (oapi-sdk-go) 的签名实现：
/// - 头部签名：SHA256(timestamp + nonce + encryptKey + body) → hex lowercase
/// - 体验签名：HMAC-SHA256(encryptKey, timestamp + "\n" + nonce + "\n" + encryptData) → hex lowercase
/// </summary>
public class SignatureValidatorTests
{
    private readonly Mock<ILogger<SignatureValidator>> _loggerMock;
    private readonly Mock<IOptionsMonitor<FeishuWebhookOptions>> _optionsMock;
    private readonly Mock<IWebhookAppKeyAccessor> _appKeyAccessorMock;
    private readonly Mock<IEnvironmentService> _environmentServiceMock;
    private readonly Mock<ISecurityAuditService> _securityAuditMock;
    private readonly FeishuWebhookOptions _defaultOptions;

    public SignatureValidatorTests()
    {
        _loggerMock = new Mock<ILogger<SignatureValidator>>();
        _optionsMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        _appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();
        _environmentServiceMock = new Mock<IEnvironmentService>();
        _securityAuditMock = new Mock<ISecurityAuditService>();

        _defaultOptions = new FeishuWebhookOptions
        {
            EnableBodySignatureValidation = true,
            EnforceHeaderSignatureValidation = true,
            TimestampToleranceSeconds = 300
        };
        _optionsMock.Setup(x => x.CurrentValue).Returns(_defaultOptions);

        // 默认设置为开发环境
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(false);
        _environmentServiceMock.Setup(x => x.IsDevelopment).Returns(true);
        _environmentServiceMock.Setup(x => x.EnvironmentName).Returns("Development");

        // 设置 AppKeyAccessor
        string? currentAppKey = null;
        _appKeyAccessorMock
            .Setup(x => x.SetAppKey(It.IsAny<string>()))
            .Callback<string>(appKey => currentAppKey = appKey);
        _appKeyAccessorMock
            .Setup(x => x.CurrentAppKey)
            .Returns(() => currentAppKey);
    }

    #region 构造函数测试

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SignatureValidator(null!, _optionsMock.Object, _appKeyAccessorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SignatureValidator(_loggerMock.Object, null!, _appKeyAccessorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullAppKeyAccessor_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SignatureValidator(_loggerMock.Object, _optionsMock.Object, null!));
    }

    #endregion

    #region 请求头签名验证测试 (SHA-256)

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange - 参考飞书 Go SDK: SHA256(timestamp + nonce + encryptKey + body)
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce-123";
        var encryptKey = "test-encrypt-key-0123456789abcdef"; // 32 chars
        var body = "{\"encrypt\":\"test-encrypt-data\"}";
        var signString = $"{timestamp}{nonce}{encryptKey}{body}";
        var expectedSignature = ComputeSha256Hex(signString);

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateHeaderSignatureAsync(timestamp, nonce, body, expectedSignature, encryptKey);

        // Assert
        result.Should().BeTrue("有效的签名应该通过验证");
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce-123";
        var encryptKey = "test-encrypt-key-0123456789abcdef";
        var body = "{\"encrypt\":\"test-encrypt-data\"}";
        var invalidSignature = "invalid-signature-value";

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateHeaderSignatureAsync(timestamp, nonce, body, invalidSignature, encryptKey);

        // Assert
        result.Should().BeFalse("无效的签名应该被拒绝");
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithEmptySignatureAndEnforceTrue_ShouldReturnFalse()
    {
        // Arrange
        _defaultOptions.EnforceHeaderSignatureValidation = true;
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateHeaderSignatureAsync(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "nonce", "body", null!, "key");

        // Assert
        result.Should().BeFalse("强制验证模式下，空签名应被拒绝");
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithEmptySignatureAndEnforceFalse_ShouldReturnTrue()
    {
        // Arrange
        _defaultOptions.EnforceHeaderSignatureValidation = false;
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateHeaderSignatureAsync(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "nonce", "body", null!, "key");

        // Assert
        result.Should().BeTrue("非强制验证模式下，空签名应跳过验证");
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithZeroTimestampInProduction_ShouldReturnFalse()
    {
        // Arrange
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(true);
        _environmentServiceMock.Setup(x => x.IsDevelopment).Returns(false);
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateHeaderSignatureAsync(0, "", "body", "some-signature", "key");

        // Assert
        result.Should().BeFalse("生产环境下时间戳为 0 应被拒绝");
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithZeroTimestampInDevelopment_ShouldReturnTrue()
    {
        // Arrange
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(false);
        _environmentServiceMock.Setup(x => x.IsDevelopment).Returns(true);
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateHeaderSignatureAsync(0, "", "body", "some-signature", "key");

        // Assert
        result.Should().BeTrue("开发环境下时间戳为 0 应跳过验证");
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithGoSdkCompatibleSignature_ShouldReturnTrue()
    {
        // Arrange - 使用与飞书 Go SDK 完全相同的签名计算方式
        // Go SDK: Signature(timestamp, nonce, eventEncryptKey, body) = SHA256(timestamp + nonce + eventEncryptKey + body)
        var timestamp = "1700000000";
        var nonce = "abc123nonce";
        var encryptKey = "my-encrypt-key-0123456789abcdef0123456";
        var body = @"{""encrypt"":""encrypted_payload_data""}";

        // 使用 Go SDK 的签名计算方式
        var goSdkSignString = timestamp + nonce + encryptKey + body;
        var goSdkSignature = ComputeSha256Hex(goSdkSignString);

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateHeaderSignatureAsync(
            long.Parse(timestamp), nonce, body, goSdkSignature, encryptKey);

        // Assert
        result.Should().BeTrue("签名计算应与飞书 Go SDK 兼容");
    }

    #endregion

    #region 请求体签名验证测试 (HMAC-SHA256) - P0 修复验证

    [Fact]
    public async Task ValidateBodySignatureAsync_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange - HMAC-SHA256(encryptKey, timestamp + "\n" + nonce + "\n" + encryptData)
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce-456";
        var encryptKey = "test-encrypt-key-0123456789abcdef";
        var encryptData = "encrypted-data-content";
        var signString = $"{timestamp}\n{nonce}\n{encryptData}";
        var expectedSignature = ComputeHmacSha256Hex(encryptKey, signString);

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateBodySignatureAsync(timestamp, nonce, encryptData, encryptKey, expectedSignature);

        // Assert
        result.Should().BeTrue("有效的请求体签名应该通过验证");
    }

    [Fact]
    public async Task ValidateBodySignatureAsync_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange - 这是 P0 修复的核心验证：之前签名计算后从未比较
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce-789";
        var encryptKey = "test-encrypt-key-0123456789abcdef";
        var encryptData = "encrypted-data-content";
        var invalidSignature = "aabbccddee000000invalid-signature";

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateBodySignatureAsync(timestamp, nonce, encryptData, encryptKey, invalidSignature);

        // Assert
        result.Should().BeFalse("无效的请求体签名应该被拒绝（P0 修复：之前从不比较签名）");
    }

    [Fact]
    public async Task ValidateBodySignatureAsync_WithNullExpectedSignature_ShouldReturnTrueWithWarning()
    {
        // Arrange - 当未提供期望签名时，记录警告并返回 true（向后兼容）
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce-null";
        var encryptKey = "test-encrypt-key-0123456789abcdef";
        var encryptData = "encrypted-data-content";

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateBodySignatureAsync(timestamp, nonce, encryptData, encryptKey, null);

        // Assert
        result.Should().BeTrue("未提供期望签名时应返回 true（向后兼容）");
        VerifyLogCalled(LogLevel.Warning, "未提供期望签名值");
    }

    [Fact]
    public async Task ValidateBodySignatureAsync_WithEmptyExpectedSignature_ShouldReturnTrueWithWarning()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce-empty";
        var encryptKey = "test-encrypt-key-0123456789abcdef";
        var encryptData = "encrypted-data-content";

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateBodySignatureAsync(timestamp, nonce, encryptData, encryptKey, "");

        // Assert
        result.Should().BeTrue("空期望签名应返回 true（向后兼容）");
        VerifyLogCalled(LogLevel.Warning, "未提供期望签名值");
    }

    [Fact]
    public async Task ValidateBodySignatureAsync_WithDisabledValidation_ShouldReturnTrue()
    {
        // Arrange
        _defaultOptions.EnableBodySignatureValidation = false;
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateBodySignatureAsync(
            1234567890, "nonce", "data", "key", "invalid-signature");

        // Assert
        result.Should().BeTrue("禁用验证时应跳过并返回 true");
        VerifyLogCalled(LogLevel.Debug, "请求体签名验证已禁用");
    }

    [Fact]
    public async Task ValidateBodySignatureAsync_WithCaseSensitiveSignature_ShouldBeCaseInsensitive()
    {
        // Arrange - 签名比较应区分大小写（hex 小写）
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce-case";
        var encryptKey = "test-encrypt-key-0123456789abcdef";
        var encryptData = "encrypted-data-content";
        var signString = $"{timestamp}\n{nonce}\n{encryptData}";
        var expectedSignature = ComputeHmacSha256Hex(encryptKey, signString);

        // 将签名转为大写，应该不匹配（因为计算结果是小写）
        var upperSignature = expectedSignature.ToUpperInvariant();

        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateBodySignatureAsync(timestamp, nonce, encryptData, encryptKey, upperSignature);

        // Assert
        result.Should().BeFalse("签名比较应区分大小写（计算结果为小写 hex）");
    }

    [Fact]
    public async Task ValidateBodySignatureAsync_WithDifferentEncryptKey_ShouldReturnFalse()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce-diff-key";
        var correctKey = "test-encrypt-key-0123456789abcdef";
        var wrongKey = "wrong-encrypt-key-0123456789abcde";
        var encryptData = "encrypted-data-content";
        var signString = $"{timestamp}\n{nonce}\n{encryptData}";
        var expectedSignature = ComputeHmacSha256Hex(correctKey, signString);

        var validator = CreateValidator();

        // Act - 使用错误的密钥验证
        var result = await validator.ValidateBodySignatureAsync(timestamp, nonce, encryptData, wrongKey, expectedSignature);

        // Assert
        result.Should().BeFalse("使用不同密钥计算的签名不应匹配");
    }

    [Fact]
    public async Task ValidateBodySignatureAsync_WithTamperedEncryptData_ShouldReturnFalse()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce-tamper";
        var encryptKey = "test-encrypt-key-0123456789abcdef";
        var originalData = "original-encrypted-data";
        var tamperedData = "tampered-encrypted-data";

        // 使用原始数据计算签名
        var signString = $"{timestamp}\n{nonce}\n{originalData}";
        var expectedSignature = ComputeHmacSha256Hex(encryptKey, signString);

        var validator = CreateValidator();

        // Act - 使用篡改后的数据验证
        var result = await validator.ValidateBodySignatureAsync(timestamp, nonce, tamperedData, encryptKey, expectedSignature);

        // Assert
        result.Should().BeFalse("篡改后的数据不应通过签名验证");
    }

    #endregion

    #region 多应用配置继承测试

    [Fact]
    public async Task ValidateBodySignatureAsync_WithAppLevelDisabled_ShouldReturnTrue()
    {
        // Arrange - 应用级配置禁用体验证
        _defaultOptions.EnableBodySignatureValidation = true;
        _defaultOptions.Apps = new Dictionary<string, FeishuAppWebhookOptions>
        {
            ["test_app"] = new FeishuAppWebhookOptions
            {
                AppKey = "test_app",
                EncryptKey = "test-encrypt-key-0123456789abcdef",
                VerificationToken = "token",
                EnableBodySignatureValidation = false // 应用级禁用
            }
        };

        var validator = CreateValidator();
        validator.SetCurrentAppKey("test_app");

        // Act
        var result = await validator.ValidateBodySignatureAsync(
            1234567890, "nonce", "data", "key", "invalid-signature");

        // Assert
        result.Should().BeTrue("应用级配置禁用体验证时应跳过");
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithAppLevelEnforceFalse_ShouldReturnTrue()
    {
        // Arrange - 应用级配置禁用强制头部验证
        _defaultOptions.EnforceHeaderSignatureValidation = true;
        _defaultOptions.Apps = new Dictionary<string, FeishuAppWebhookOptions>
        {
            ["test_app"] = new FeishuAppWebhookOptions
            {
                AppKey = "test_app",
                EncryptKey = "test-encrypt-key-0123456789abcdef",
                VerificationToken = "token",
                EnforceHeaderSignatureValidation = false // 应用级禁用
            }
        };

        var validator = CreateValidator();
        validator.SetCurrentAppKey("test_app");

        // Act
        var result = await validator.ValidateHeaderSignatureAsync(
            1234567890, "nonce", "body", null!, "key");

        // Assert
        result.Should().BeTrue("应用级配置禁用强制验证时应跳过");
    }

    #endregion

    #region 静态方法测试

    [Fact]
    public void ComputeSha256Signature_ShouldReturnLowercaseHex()
    {
        // Arrange
        var input = "1700000000abc123noncekey123body";
        // 使用标准 SHA256 计算
        var expected = ComputeSha256Hex(input);

        // Act
        var result = SignatureValidator.ComputeSha256Signature(input);

        // Assert
        result.Should().Be(expected);
        result.Should().MatchRegex("^[0-9a-f]{64}$", "SHA-256 签名应为 64 位小写十六进制");
    }

    [Fact]
    public void ComputeSha256Signature_ShouldBeConsistentWithGoSdk()
    {
        // Arrange - 使用与 Go SDK 相同的输入验证
        // Go: sha256(timestamp + nonce + eventEncryptKey + body)
        var timestamp = "1700000000";
        var nonce = "testnonce";
        var encryptKey = "event_encrypt_key_value_0123456789";
        var body = @"{""event"":""test""}";
        var input = timestamp + nonce + encryptKey + body;

        // Act
        var result = SignatureValidator.ComputeSha256Signature(input);

        // Assert - 手动计算验证
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        var expected = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        result.Should().Be(expected);
    }

    [Fact]
    public void FixedTimeEquals_WithSameBytes_ShouldReturnTrue()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("hello-world-signature-123");

        // Act
        var result = SignatureValidator.FixedTimeEquals(bytes, bytes);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FixedTimeEquals_WithDifferentBytes_ShouldReturnFalse()
    {
        // Arrange
        var left = Encoding.UTF8.GetBytes("signature-A");
        var right = Encoding.UTF8.GetBytes("signature-B");

        // Act
        var result = SignatureValidator.FixedTimeEquals(left, right);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FixedTimeEquals_WithDifferentLength_ShouldReturnFalse()
    {
        // Arrange
        var left = Encoding.UTF8.GetBytes("short");
        var right = Encoding.UTF8.GetBytes("longer-string");

        // Act
        var result = SignatureValidator.FixedTimeEquals(left, right);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FixedTimeEquals_WithEmptyArrays_ShouldReturnTrue()
    {
        // Arrange
        var left = Array.Empty<byte>();
        var right = Array.Empty<byte>();

        // Act
        var result = SignatureValidator.FixedTimeEquals(left, right);

        // Assert
        result.Should().BeTrue("两个空数组应视为相等");
    }

    #endregion

    #region 异常处理测试

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithException_ShouldReturnFalse()
    {
        // Arrange - 让 options 抛出异常
        _optionsMock.Setup(x => x.CurrentValue).Throws(new InvalidOperationException("Config error"));
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateHeaderSignatureAsync(
            1234567890, "nonce", "body", "signature", "key");

        // Assert
        result.Should().BeFalse("异常情况应返回 false（安全失败）");
    }

    [Fact]
    public async Task ValidateBodySignatureAsync_WithException_ShouldReturnFalse()
    {
        // Arrange - 使用 null encryptKey 触发异常
        var validator = CreateValidator();

        // Act
        var result = await validator.ValidateBodySignatureAsync(
            1234567890, "nonce", "data", null!, "expected-signature");

        // Assert
        result.Should().BeFalse("异常情况应返回 false（安全失败）");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 创建 SignatureValidator 实例
    /// </summary>
    private SignatureValidator CreateValidator()
    {
        return new SignatureValidator(
            _loggerMock.Object,
            _optionsMock.Object,
            _appKeyAccessorMock.Object,
            _securityAuditMock.Object,
            _environmentServiceMock.Object);
    }

    /// <summary>
    /// 计算 SHA-256 十六进制小写签名（与 Go SDK 一致）
    /// </summary>
    private static string ComputeSha256Hex(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 计算 HMAC-SHA256 十六进制小写签名
    /// </summary>
    private static string ComputeHmacSha256Hex(string key, string input)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 验证日志是否被调用
    /// </summary>
    private void VerifyLogCalled(LogLevel logLevel, string message)
    {
        _loggerMock.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}
