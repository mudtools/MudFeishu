// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;
using Mud.Feishu.Webhook.Utils;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Validators;

/// <summary>
/// 组合验证器单元测试
/// 验证 CompositeFeishuEventValidator 的验证编排逻辑
/// 重点覆盖 P1 修复：Nonce 消费时机（签名验证前仅检查，签名验证通过后才标记）
/// </summary>
public class CompositeFeishuEventValidatorTests
{
    private readonly Mock<ISignatureValidator> _signatureValidatorMock;
    private readonly Mock<ITimestampValidator> _timestampValidatorMock;
    private readonly Mock<INonceValidator> _nonceValidatorMock;
    private readonly Mock<ISubscriptionValidator> _subscriptionValidatorMock;
    private readonly Mock<ILogger<CompositeFeishuEventValidator>> _loggerMock;
    private readonly Mock<IOptionsMonitor<FeishuWebhookOptions>> _optionsMock;
    private readonly Mock<IWebhookAppKeyAccessor> _appKeyAccessorMock;
    private readonly Mock<IEnvironmentService> _environmentServiceMock;
    private readonly FeishuWebhookOptions _defaultOptions;
    private readonly CompositeFeishuEventValidator _sut;

    public CompositeFeishuEventValidatorTests()
    {
        _signatureValidatorMock = new Mock<ISignatureValidator>();
        _timestampValidatorMock = new Mock<ITimestampValidator>();
        _nonceValidatorMock = new Mock<INonceValidator>();
        _subscriptionValidatorMock = new Mock<ISubscriptionValidator>();
        _loggerMock = new Mock<ILogger<CompositeFeishuEventValidator>>();
        _optionsMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        _appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();
        _environmentServiceMock = new Mock<IEnvironmentService>();

        _defaultOptions = new FeishuWebhookOptions();
        _optionsMock.Setup(x => x.CurrentValue).Returns(_defaultOptions);
        _environmentServiceMock.Setup(x => x.IsProduction).Returns(true);

        string? currentAppKey = null;
        _appKeyAccessorMock
            .Setup(x => x.SetAppKey(It.IsAny<string>()))
            .Callback<string>(appKey => currentAppKey = appKey);
        _appKeyAccessorMock
            .Setup(x => x.CurrentAppKey)
            .Returns(() => currentAppKey);

        _sut = new CompositeFeishuEventValidator(
            _signatureValidatorMock.Object,
            _timestampValidatorMock.Object,
            _nonceValidatorMock.Object,
            _subscriptionValidatorMock.Object,
            _loggerMock.Object,
            _optionsMock.Object,
            _appKeyAccessorMock.Object,
            _environmentServiceMock.Object);
    }

    #region 构造函数测试

    [Fact]
    public void Constructor_WithNullSignatureValidator_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeFeishuEventValidator(
                null!, _timestampValidatorMock.Object, _nonceValidatorMock.Object,
                _subscriptionValidatorMock.Object, _loggerMock.Object,
                _optionsMock.Object, _appKeyAccessorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullTimestampValidator_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeFeishuEventValidator(
                _signatureValidatorMock.Object, null!, _nonceValidatorMock.Object,
                _subscriptionValidatorMock.Object, _loggerMock.Object,
                _optionsMock.Object, _appKeyAccessorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullNonceValidator_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CompositeFeishuEventValidator(
                _signatureValidatorMock.Object, _timestampValidatorMock.Object, null!,
                _subscriptionValidatorMock.Object, _loggerMock.Object,
                _optionsMock.Object, _appKeyAccessorMock.Object));
    }

    #endregion

    #region ValidateHeaderSignatureAsync - 完整验证流程测试

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WithAllValidationsPassed_ShouldReturnTrue()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test-nonce";
        var body = "{}";
        var signature = "valid-signature";
        var encryptKey = "test-key";

        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(timestamp, null)).Returns(true);
        _nonceValidatorMock.Setup(x => x.CheckNonceAsync(nonce)).ReturnsAsync(true);
        _signatureValidatorMock.Setup(x => x.ValidateHeaderSignatureAsync(timestamp, nonce, body, signature, encryptKey))
            .ReturnsAsync(true);
        _nonceValidatorMock.Setup(x => x.TryMarkNonceAsUsedAsync(nonce)).ReturnsAsync(false); // false = 未被使用，成功标记

        // Act
        var result = await _sut.ValidateHeaderSignatureAsync(timestamp, nonce, body, signature, encryptKey);

        // Assert
        result.Should().BeTrue();
        _timestampValidatorMock.Verify(x => x.ValidateTimestamp(timestamp, null), Times.Once);
        _nonceValidatorMock.Verify(x => x.CheckNonceAsync(nonce), Times.Once);
        _signatureValidatorMock.Verify(x => x.ValidateHeaderSignatureAsync(timestamp, nonce, body, signature, encryptKey), Times.Once);
        _nonceValidatorMock.Verify(x => x.TryMarkNonceAsUsedAsync(nonce), Times.Once);
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WhenTimestampInvalid_ShouldReturnFalse_AndNotCheckNonce()
    {
        // Arrange
        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), null)).Returns(false);

        // Act
        var result = await _sut.ValidateHeaderSignatureAsync(0, "nonce", "body", "sig", "key");

        // Assert
        result.Should().BeFalse();
        _nonceValidatorMock.Verify(x => x.CheckNonceAsync(It.IsAny<string>()), Times.Never);
        _signatureValidatorMock.Verify(x => x.ValidateHeaderSignatureAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _nonceValidatorMock.Verify(x => x.TryMarkNonceAsUsedAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region P1 修复核心测试：Nonce 消费时机

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WhenNonceAlreadyUsed_ShouldReturnFalse_AndNotCallSignatureValidation()
    {
        // Arrange - Nonce 已被使用（检测到重放攻击）
        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), null)).Returns(true);
        _nonceValidatorMock.Setup(x => x.CheckNonceAsync(It.IsAny<string>())).ReturnsAsync(false); // false = 已被使用

        // Act
        var result = await _sut.ValidateHeaderSignatureAsync(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "used-nonce", "body", "sig", "key");

        // Assert
        result.Should().BeFalse("Nonce 已被使用时应拒绝请求");
        _signatureValidatorMock.Verify(x => x.ValidateHeaderSignatureAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never,
            "Nonce 检查失败时不应调用签名验证");
        _nonceValidatorMock.Verify(x => x.TryMarkNonceAsUsedAsync(It.IsAny<string>()), Times.Never,
            "Nonce 检查失败时不应标记 Nonce");
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WhenSignatureInvalid_ShouldReturnFalse_AndNotMarkNonceAsUsed()
    {
        // Arrange - P1 核心测试：签名验证失败时 Nonce 不应被标记
        var nonce = "test-nonce-not-to-be-consumed";
        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), null)).Returns(true);
        _nonceValidatorMock.Setup(x => x.CheckNonceAsync(nonce)).ReturnsAsync(true); // Nonce 未被使用
        _signatureValidatorMock.Setup(x => x.ValidateHeaderSignatureAsync(
            It.IsAny<long>(), nonce, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false); // 签名验证失败

        // Act
        var result = await _sut.ValidateHeaderSignatureAsync(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), nonce, "body", "invalid-sig", "key");

        // Assert
        result.Should().BeFalse("签名验证失败应返回 false");
        _nonceValidatorMock.Verify(x => x.TryMarkNonceAsUsedAsync(nonce), Times.Never,
            "P1 修复核心断言：签名验证失败时，Nonce 不应被标记为已使用");
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WhenSignatureValid_ShouldMarkNonceAsUsed()
    {
        // Arrange - 签名验证通过后应标记 Nonce
        var nonce = "test-nonce-to-be-marked";
        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), null)).Returns(true);
        _nonceValidatorMock.Setup(x => x.CheckNonceAsync(nonce)).ReturnsAsync(true);
        _signatureValidatorMock.Setup(x => x.ValidateHeaderSignatureAsync(
            It.IsAny<long>(), nonce, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _nonceValidatorMock.Setup(x => x.TryMarkNonceAsUsedAsync(nonce)).ReturnsAsync(false); // false = 成功标记

        // Act
        var result = await _sut.ValidateHeaderSignatureAsync(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), nonce, "body", "valid-sig", "key");

        // Assert
        result.Should().BeTrue();
        _nonceValidatorMock.Verify(x => x.TryMarkNonceAsUsedAsync(nonce), Times.Once,
            "签名验证通过后应标记 Nonce 为已使用");
    }

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WhenNonceMarkedAsUsedByConcurrentRequest_ShouldReturnFalse()
    {
        // Arrange - 并发场景：签名验证通过后标记 Nonce 时发现已被其他请求标记
        var nonce = "concurrent-nonce";
        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), null)).Returns(true);
        _nonceValidatorMock.Setup(x => x.CheckNonceAsync(nonce)).ReturnsAsync(true); // 预检查通过
        _signatureValidatorMock.Setup(x => x.ValidateHeaderSignatureAsync(
            It.IsAny<long>(), nonce, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _nonceValidatorMock.Setup(x => x.TryMarkNonceAsUsedAsync(nonce)).ReturnsAsync(true); // true = 已被其他请求标记

        // Act
        var result = await _sut.ValidateHeaderSignatureAsync(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(), nonce, "body", "valid-sig", "key");

        // Assert
        result.Should().BeFalse("并发场景下 Nonce 已被其他请求标记时应拒绝");
    }

    #endregion

    #region ValidateSubscriptionRequestAsync 测试

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_ShouldDelegateToSubscriptionValidator()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "token",
            Challenge = "challenge"
        };
        _subscriptionValidatorMock.Setup(x => x.ValidateSubscriptionRequestAsync(request, "token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ValidateSubscriptionRequestAsync(request, "token");

        // Assert
        result.Should().BeTrue();
        _subscriptionValidatorMock.Verify(x => x.ValidateSubscriptionRequestAsync(request, "token", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ValidateTimestamp 测试

    [Fact]
    public void ValidateTimestamp_ShouldDelegateToTimestampValidator()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(timestamp, 60)).Returns(true);

        // Act
        var result = _sut.ValidateTimestamp(timestamp, 60);

        // Assert
        result.Should().BeTrue();
        _timestampValidatorMock.Verify(x => x.ValidateTimestamp(timestamp, 60), Times.Once);
    }

    #endregion

    #region 异常处理测试

    [Fact]
    public async Task ValidateHeaderSignatureAsync_WhenExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        _timestampValidatorMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), null))
            .Throws(new InvalidOperationException("Test exception"));

        // Act
        var result = await _sut.ValidateHeaderSignatureAsync(0, "nonce", "body", "sig", "key");

        // Assert
        result.Should().BeFalse("异常情况应安全失败");
    }

    #endregion
}
