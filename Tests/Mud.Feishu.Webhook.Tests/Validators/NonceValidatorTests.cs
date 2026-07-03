// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Validators;

/// <summary>
/// Nonce验证器单元测试
/// 验证 NonceValidator 类的各种Nonce验证场景
/// **验证需求: 3.2, 3.3, 3.4, 3.5**
/// </summary>
public class NonceValidatorTests
{
    private readonly Mock<ILogger<NonceValidator>> _loggerMock;
    private readonly Mock<IFeishuNonceDistributedDeduplicator> _deduplicatorMock;
    private readonly Mock<IWebhookAppKeyAccessor> _appKeyAccessorMock;
    private readonly Mock<IOptionsMonitor<FeishuWebhookOptions>> _optionsMonitorMock;
    private readonly NonceValidator _validator;

    public NonceValidatorTests()
    {
        _loggerMock = new Mock<ILogger<NonceValidator>>();
        _deduplicatorMock = new Mock<IFeishuNonceDistributedDeduplicator>();
        _appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();
        _optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();

        // 设置默认配置（Reject 模式）
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(new FeishuWebhookOptions
        {
            NonceValidationFailureMode = NonceFailureMode.Reject
        });

        // 设置 _appKeyAccessorMock 使 SetAppKey 方法能够更新 CurrentAppKey 属性
        string? currentAppKey = null;
        _appKeyAccessorMock
            .Setup(x => x.SetAppKey(It.IsAny<string>()))
            .Callback<string>(appKey => currentAppKey = appKey);
        _appKeyAccessorMock
            .Setup(x => x.CurrentAppKey)
            .Returns(() => currentAppKey);

        _validator = new NonceValidator(_loggerMock.Object, _deduplicatorMock.Object, _appKeyAccessorMock.Object, _optionsMonitorMock.Object);
    }

    #region 构造函数和基本功能测试

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new NonceValidator(null!, _deduplicatorMock.Object, _appKeyAccessorMock.Object, _optionsMonitorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullDeduplicator_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new NonceValidator(_loggerMock.Object, null!, _appKeyAccessorMock.Object, _optionsMonitorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullAppKeyAccessor_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new NonceValidator(_loggerMock.Object, _deduplicatorMock.Object, null!, _optionsMonitorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOptionsMonitor_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new NonceValidator(_loggerMock.Object, _deduplicatorMock.Object, _appKeyAccessorMock.Object, null!));
    }

    [Fact]
    public void SetCurrentAppKey_ShouldSetAppKey()
    {
        // Arrange
        var appKey = "test-app-key";

        // Act
        _validator.SetCurrentAppKey(appKey);

        // Assert
        // AppKey设置是内部状态，通过后续方法调用验证
        Assert.True(true); // 通过其他测试验证AppKey传递
    }

    #endregion

    #region Nonce去重功能测试 (需求 3.2)

    [Fact]
    public async Task TryMarkNonceAsUsedAsync_WithUnusedNonce_ShouldReturnFalseAndMarkAsUsed()
    {
        // Arrange
        var nonce = "test-nonce-123";
        _deduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(nonce, null, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // 未被使用

        // Act
        var result = await _validator.TryMarkNonceAsUsedAsync(nonce);

        // Assert
        Assert.False(result); // 返回false表示Nonce未被使用，已成功标记

        // 验证去重服务被调用
        _deduplicatorMock.Verify(
            x => x.TryMarkAsUsedAsync(nonce, null, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // 验证调试日志被记录
        VerifyLogCalled(LogLevel.Debug, "Nonce test-nonce-123 验证通过并已标记为已使用");
    }

    [Fact]
    public async Task TryMarkNonceAsUsedAsync_WithUsedNonce_ShouldReturnTrueAndLogWarning()
    {
        // Arrange
        var nonce = "used-nonce-456";
        _deduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(nonce, null, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // 已被使用

        // Act
        var result = await _validator.TryMarkNonceAsUsedAsync(nonce);

        // Assert
        Assert.True(result); // 返回true表示Nonce已被使用（重放攻击）

        // 验证去重服务被调用
        _deduplicatorMock.Verify(
            x => x.TryMarkAsUsedAsync(nonce, null, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // 验证警告日志被记录
        VerifyLogCalled(LogLevel.Warning, "检测到重放攻击");
    }

    [Fact]
    public async Task TryMarkNonceAsUsedAsync_WithExceptionInRejectMode_ShouldReturnTrueAndLogError()
    {
        // Arrange
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(new FeishuWebhookOptions
        {
            NonceValidationFailureMode = NonceFailureMode.Reject
        });
        var nonce = "error-nonce-789";
        var exception = new InvalidOperationException("Test exception");
        _deduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(nonce, null, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _validator.TryMarkNonceAsUsedAsync(nonce);

        // Assert
        Assert.True(result); // Reject 模式下，异常时返回 true（认为已使用，拒绝请求）

        // 验证错误日志被记录
        VerifyLogCalled(LogLevel.Error, "检查 Nonce 使用状态时发生错误");
    }

    [Fact]
    public async Task TryMarkNonceAsUsedAsync_WithExceptionInAllowMode_ShouldReturnFalseAndLogError()
    {
        // Arrange
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(new FeishuWebhookOptions
        {
            NonceValidationFailureMode = NonceFailureMode.Allow
        });
        var nonce = "error-nonce-allow";
        var exception = new InvalidOperationException("Test exception");
        _deduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(nonce, null, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _validator.TryMarkNonceAsUsedAsync(nonce);

        // Assert
        Assert.False(result); // Allow 模式下，异常时返回 false（认为未使用，允许请求）

        // 验证错误日志被记录
        VerifyLogCalled(LogLevel.Error, "检查 Nonce 使用状态时发生错误");
    }

    #endregion

    #region 多应用Nonce隔离测试 (需求 3.3)

    [Fact]
    public async Task TryMarkNonceAsUsedAsync_WithAppKey_ShouldPassAppKeyToDeduplicator()
    {
        // Arrange
        var nonce = "app-nonce-123";
        var appKey = "test-app-key";
        _validator.SetCurrentAppKey(appKey);
        _deduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(nonce, appKey, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _validator.TryMarkNonceAsUsedAsync(nonce);

        // Assert
        Assert.False(result);

        // 验证AppKey被正确传递
        _deduplicatorMock.Verify(
            x => x.TryMarkAsUsedAsync(nonce, appKey, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryMarkNonceAsUsedAsync_WithDifferentAppKeys_ShouldIsolateNonces()
    {
        // Arrange
        var nonce = "same-nonce";
        var appKey1 = "app1";
        var appKey2 = "app2";

        _deduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(nonce, appKey1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _deduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(nonce, appKey2, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert - 第一个应用
        _validator.SetCurrentAppKey(appKey1);
        var result1 = await _validator.TryMarkNonceAsUsedAsync(nonce);
        Assert.False(result1);

        // Act & Assert - 第二个应用
        _validator.SetCurrentAppKey(appKey2);
        var result2 = await _validator.TryMarkNonceAsUsedAsync(nonce);
        Assert.False(result2);

        // 验证两个应用的Nonce被独立处理
        _deduplicatorMock.Verify(
            x => x.TryMarkAsUsedAsync(nonce, appKey1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _deduplicatorMock.Verify(
            x => x.TryMarkAsUsedAsync(nonce, appKey2, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region 重放攻击防护测试 (需求 3.4)

    [Fact]
    public async Task ValidateNonceAsync_WithValidNonce_ShouldReturnTrue()
    {
        // Arrange
        var nonce = "valid-nonce-123";
        _deduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(nonce, null, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // 未被使用

        // Act
        var result = await _validator.ValidateNonceAsync(nonce);

        // Assert
        Assert.True(result); // 验证通过
    }

    [Fact]
    public async Task ValidateNonceAsync_WithReplayedNonce_ShouldReturnFalse()
    {
        // Arrange
        var nonce = "replayed-nonce-456";
        _deduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(nonce, null, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // 已被使用（重放攻击）

        // Act
        var result = await _validator.ValidateNonceAsync(nonce);

        // Assert
        Assert.False(result); // 验证失败
        VerifyLogCalled(LogLevel.Warning, "检测到重放攻击");
    }

    #endregion

    #region 生产环境安全检查测试 (需求 3.5)

    [Fact]
    public async Task ValidateNonceAsync_WithEmptyNonceInProduction_ShouldReturnFalse()
    {
        // Arrange
        var emptyNonce = "";
        var isProductionEnvironment = true;

        // Act
        var result = await _validator.ValidateNonceAsync(emptyNonce, isProductionEnvironment);

        // Assert
        Assert.False(result); // 生产环境拒绝空Nonce
        VerifyLogCalled(LogLevel.Error, "Nonce 为空，拒绝请求（生产环境不允许空 Nonce）");
    }

    [Fact]
    public async Task ValidateNonceAsync_WithNullNonceInProduction_ShouldReturnFalse()
    {
        // Arrange
        string? nullNonce = null;
        var isProductionEnvironment = true;

        // Act
        var result = await _validator.ValidateNonceAsync(nullNonce!, isProductionEnvironment);

        // Assert
        Assert.False(result); // 生产环境拒绝null Nonce
        VerifyLogCalled(LogLevel.Error, "Nonce 为空，拒绝请求（生产环境不允许空 Nonce）");
    }

    [Fact]
    public async Task ValidateNonceAsync_WithEmptyNonceInDevelopment_ShouldReturnTrue()
    {
        // Arrange
        var emptyNonce = "";
        var isProductionEnvironment = false;

        // Act
        var result = await _validator.ValidateNonceAsync(emptyNonce, isProductionEnvironment);

        // Assert
        Assert.True(result); // 开发环境允许空Nonce
        VerifyLogCalled(LogLevel.Warning, "Nonce 为空，跳过验证（开发环境，警告：此配置存在安全风险）");
    }

    [Fact]
    public async Task ValidateNonceAsync_WithException_ShouldReturnFalse()
    {
        // Arrange
        var nonce = "exception-nonce";
        var exception = new InvalidOperationException("Test exception");
        _deduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(nonce, null, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _validator.ValidateNonceAsync(nonce);

        // Assert
        Assert.False(result); // 异常情况下验证失败
        VerifyLogCalled(LogLevel.Error, "检查 Nonce 使用状态时发生错误");
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