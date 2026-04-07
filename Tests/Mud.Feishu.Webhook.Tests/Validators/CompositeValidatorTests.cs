// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Validators;

/// <summary>
/// 组合验证器单元测试
/// 验证 CompositeFeishuEventValidator 类的各种验证场景
/// **验证需求: 5.2, 5.3, 5.5**
/// </summary>
public class CompositeValidatorTests
{
    private readonly Mock<ISignatureValidator> _signatureValidatorMock;
    private readonly Mock<ITimestampValidator> _timestampValidatorMock;
    private readonly Mock<INonceValidator> _nonceValidatorMock;
    private readonly Mock<ISubscriptionValidator> _subscriptionValidatorMock;
    private readonly Mock<ILogger<CompositeFeishuEventValidator>> _loggerMock;
    private readonly Mock<IOptions<FeishuWebhookOptions>> _optionsMock;
    private readonly FeishuWebhookOptions _options;
    private readonly CompositeFeishuEventValidator _validator;

    public CompositeValidatorTests()
    {
        _signatureValidatorMock = new Mock<ISignatureValidator>();
        _timestampValidatorMock = new Mock<ITimestampValidator>();
        _nonceValidatorMock = new Mock<INonceValidator>();
        _subscriptionValidatorMock = new Mock<ISubscriptionValidator>();
        _loggerMock = new Mock<ILogger<CompositeFeishuEventValidator>>();
        _optionsMock = new Mock<IOptions<FeishuWebhookOptions>>();

        _options = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 300,
            EnforceHeaderSignatureValidation = true
        };

        _optionsMock.Setup(x => x.Value).Returns(_options);

        _validator = new CompositeFeishuEventValidator(
            _signatureValidatorMock.Object,
            _timestampValidatorMock.Object,
            _nonceValidatorMock.Object,
            _subscriptionValidatorMock.Object,
            _loggerMock.Object,
            _optionsMock.Object);
    }

    #region 构造函数测试

    [Fact]
    public void Constructor_WithNullSignatureValidator_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeFeishuEventValidator(
                null!,
                _timestampValidatorMock.Object,
                _nonceValidatorMock.Object,
                _subscriptionValidatorMock.Object,
                _loggerMock.Object,
                _optionsMock.Object));
    }

    [Fact]
    public void Constructor_WithNullTimestampValidator_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeFeishuEventValidator(
                _signatureValidatorMock.Object,
                null!,
                _nonceValidatorMock.Object,
                _subscriptionValidatorMock.Object,
                _loggerMock.Object,
                _optionsMock.Object));
    }

    [Fact]
    public void Constructor_WithNullNonceValidator_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeFeishuEventValidator(
                _signatureValidatorMock.Object,
                _timestampValidatorMock.Object,
                null!,
                _subscriptionValidatorMock.Object,
                _loggerMock.Object,
                _optionsMock.Object));
    }

    [Fact]
    public void Constructor_WithNullSubscriptionValidator_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeFeishuEventValidator(
                _signatureValidatorMock.Object,
                _timestampValidatorMock.Object,
                _nonceValidatorMock.Object,
                null!,
                _loggerMock.Object,
                _optionsMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeFeishuEventValidator(
                _signatureValidatorMock.Object,
                _timestampValidatorMock.Object,
                _nonceValidatorMock.Object,
                _subscriptionValidatorMock.Object,
                null!,
                _optionsMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeFeishuEventValidator(
                _signatureValidatorMock.Object,
                _timestampValidatorMock.Object,
                _nonceValidatorMock.Object,
                _subscriptionValidatorMock.Object,
                _loggerMock.Object,
                null!));
    }

    #endregion

    #region 方法委托正确性测试 (需求 5.2, 5.3)

    [Fact]
    public void ValidateTimestamp_ShouldDelegateToTimestampValidator()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var toleranceSeconds = 300;
        var expectedResult = true;

        _timestampValidatorMock
            .Setup(x => x.ValidateTimestamp(timestamp, toleranceSeconds))
            .Returns(expectedResult);

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.Equal(expectedResult, result);
        _timestampValidatorMock.Verify(x => x.ValidateTimestamp(timestamp, toleranceSeconds), Times.Once);
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithAllValidationsPass_ShouldReturnTrue()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var encrypt = "test-encrypt";
        var signature = "test-signature";
        var encryptKey = "test-key";

        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(timestamp, _options.TimestampToleranceSeconds)).Returns(true);
        _nonceValidatorMock.Setup(x => x.ValidateNonceAsync(nonce, It.IsAny<bool>())).ReturnsAsync(true);
        _signatureValidatorMock.Setup(x => x.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey)).ReturnsAsync(true);

        // Act
        var result = await _validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.True(result);
        _timestampValidatorMock.Verify(x => x.ValidateTimestamp(timestamp, _options.TimestampToleranceSeconds), Times.Once);
        _nonceValidatorMock.Verify(x => x.ValidateNonceAsync(nonce, It.IsAny<bool>()), Times.Once);
        _signatureValidatorMock.Verify(x => x.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey), Times.Once);
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithTimestampValidationFail_ShouldReturnFalse()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var encrypt = "test-encrypt";
        var signature = "test-signature";
        var encryptKey = "test-key";

        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(timestamp, _options.TimestampToleranceSeconds)).Returns(false);

        // Act
        var result = await _validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.False(result);
        _timestampValidatorMock.Verify(x => x.ValidateTimestamp(timestamp, _options.TimestampToleranceSeconds), Times.Once);
        // 时间戳验证失败后，不应该继续验证Nonce和签名
        _nonceValidatorMock.Verify(x => x.ValidateNonceAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _signatureValidatorMock.Verify(x => x.ValidateSignatureAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithNonceValidationFail_ShouldReturnFalse()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var encrypt = "test-encrypt";
        var signature = "test-signature";
        var encryptKey = "test-key";

        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(timestamp, _options.TimestampToleranceSeconds)).Returns(true);
        _nonceValidatorMock.Setup(x => x.ValidateNonceAsync(nonce, It.IsAny<bool>())).ReturnsAsync(false);

        // Act
        var result = await _validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.False(result);
        _timestampValidatorMock.Verify(x => x.ValidateTimestamp(timestamp, _options.TimestampToleranceSeconds), Times.Once);
        _nonceValidatorMock.Verify(x => x.ValidateNonceAsync(nonce, It.IsAny<bool>()), Times.Once);
        // Nonce验证失败后，不应该继续验证签名
        _signatureValidatorMock.Verify(x => x.ValidateSignatureAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ValidateSignatureAsync_WithSignatureValidationFail_ShouldReturnFalse()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var encrypt = "test-encrypt";
        var signature = "test-signature";
        var encryptKey = "test-key";

        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(timestamp, _options.TimestampToleranceSeconds)).Returns(true);
        _nonceValidatorMock.Setup(x => x.ValidateNonceAsync(nonce, It.IsAny<bool>())).ReturnsAsync(true);
        _signatureValidatorMock.Setup(x => x.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey)).ReturnsAsync(false);

        // Act
        var result = await _validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.False(result);
        _timestampValidatorMock.Verify(x => x.ValidateTimestamp(timestamp, _options.TimestampToleranceSeconds), Times.Once);
        _nonceValidatorMock.Verify(x => x.ValidateNonceAsync(nonce, It.IsAny<bool>()), Times.Once);
        _signatureValidatorMock.Verify(x => x.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey), Times.Once);
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithAllValidationsPass_ShouldReturnTrue()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var body = "test-body";
        var headerSignature = "test-header-signature";
        var encryptKey = "test-key";

        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(timestamp, _options.TimestampToleranceSeconds)).Returns(true);
        _nonceValidatorMock.Setup(x => x.ValidateNonceAsync(nonce, It.IsAny<bool>())).ReturnsAsync(true);
        _signatureValidatorMock.Setup(x => x.ValidateHeaderSignatureAsync(timestamp, nonce, body, headerSignature, encryptKey)).ReturnsAsync(true);

        // Act
        var result = await _validator.ValidateHeaderSignatureAsync(timestamp, nonce, body, headerSignature, encryptKey);

        // Assert
        Assert.True(result);
        _timestampValidatorMock.Verify(x => x.ValidateTimestamp(timestamp, _options.TimestampToleranceSeconds), Times.Once);
        _nonceValidatorMock.Verify(x => x.ValidateNonceAsync(nonce, It.IsAny<bool>()), Times.Once);
        _signatureValidatorMock.Verify(x => x.ValidateHeaderSignatureAsync(timestamp, nonce, body, headerSignature, encryptKey), Times.Once);
    }

    #endregion

    #region 多应用键传播测试 (需求 5.5)

    [Fact]
    public void SetCurrentAppKey_ShouldPropagateToAllSupportedValidators()
    {
        // Arrange
        var appKey = "test-app-key";

        // Act
        _validator.SetCurrentAppKey(appKey);

        // Assert
        _signatureValidatorMock.Verify(x => x.SetCurrentAppKey(appKey), Times.Once);
        _nonceValidatorMock.Verify(x => x.SetCurrentAppKey(appKey), Times.Once);
        _subscriptionValidatorMock.Verify(x => x.SetCurrentAppKey(appKey), Times.Once);
        // 时间戳验证器不支持多应用，不应该被调用
    }

    [Fact]
    public void SetCurrentAppKey_WithEmptyAppKey_ShouldStillPropagate()
    {
        // Arrange
        var appKey = "";

        // Act
        _validator.SetCurrentAppKey(appKey);

        // Assert
        _signatureValidatorMock.Verify(x => x.SetCurrentAppKey(appKey), Times.Once);
        _nonceValidatorMock.Verify(x => x.SetCurrentAppKey(appKey), Times.Once);
        _subscriptionValidatorMock.Verify(x => x.SetCurrentAppKey(appKey), Times.Once);
    }

    [Fact]
    public void SetCurrentAppKey_WithNullAppKey_ShouldStillPropagate()
    {
        // Arrange
        string? appKey = null;

        // Act
        _validator.SetCurrentAppKey(appKey!);

        // Assert
        _signatureValidatorMock.Verify(x => x.SetCurrentAppKey(appKey!), Times.Once);
        _nonceValidatorMock.Verify(x => x.SetCurrentAppKey(appKey!), Times.Once);
        _subscriptionValidatorMock.Verify(x => x.SetCurrentAppKey(appKey!), Times.Once);
    }

    #endregion

    #region Mock验证器集成测试 (需求 7.3)

    [Fact]
    public async Task ValidateSignatureAsync_WithMockValidators_ShouldWorkCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "mock-nonce";
        var encrypt = "mock-encrypt";
        var signature = "mock-signature";
        var encryptKey = "mock-key";

        // 设置Mock验证器的行为
        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), It.IsAny<int>())).Returns(true);
        _nonceValidatorMock.Setup(x => x.ValidateNonceAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);
        _signatureValidatorMock.Setup(x => x.ValidateSignatureAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        // Act
        var result = await _validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.True(result);

        // 验证所有Mock验证器都被正确调用
        _timestampValidatorMock.Verify(x => x.ValidateTimestamp(timestamp, _options.TimestampToleranceSeconds), Times.Once);
        _nonceValidatorMock.Verify(x => x.ValidateNonceAsync(nonce, It.IsAny<bool>()), Times.Once);
        _signatureValidatorMock.Verify(x => x.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey), Times.Once);
    }

    [Fact]
    public void ValidateTimestamp_WithMockValidator_ShouldWorkCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var toleranceSeconds = 600;

        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), It.IsAny<int>())).Returns(true);

        // Act
        var result = _validator.ValidateTimestamp(timestamp, toleranceSeconds);

        // Assert
        Assert.True(result);
        _timestampValidatorMock.Verify(x => x.ValidateTimestamp(timestamp, toleranceSeconds), Times.Once);
    }

    #endregion

    #region 异常处理测试

    [Fact]
    public async Task ValidateSignatureAsync_WithException_ShouldReturnFalseAndLogError()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var encrypt = "test-encrypt";
        var signature = "test-signature";
        var encryptKey = "test-key";

        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), It.IsAny<int>())).Returns(true);
        _nonceValidatorMock.Setup(x => x.ValidateNonceAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);
        _signatureValidatorMock.Setup(x => x.ValidateSignatureAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, encryptKey);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Error, "验证请求签名时发生错误");
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithException_ShouldReturnFalseAndLogError()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var body = "test-body";
        var headerSignature = "test-header-signature";
        var encryptKey = "test-key";

        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), It.IsAny<int>())).Returns(true);
        _nonceValidatorMock.Setup(x => x.ValidateNonceAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);
        _signatureValidatorMock.Setup(x => x.ValidateHeaderSignatureAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _validator.ValidateHeaderSignatureAsync(timestamp, nonce, body, headerSignature, encryptKey);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Error, "验证请求头签名时发生错误");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 验证日志是否被调用
    /// </summary>
    /// <param name="logLevel">日志级别</param>
    /// <param name="message">日志消息关键字</param>
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