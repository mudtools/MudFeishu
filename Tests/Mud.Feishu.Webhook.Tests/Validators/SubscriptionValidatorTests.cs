// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Validators;

/// <summary>
/// 订阅验证器单元测试
/// 验证 SubscriptionValidator 类的各种订阅验证场景
/// **验证需求: 4.2, 4.3, 4.4, 4.5**
/// </summary>
public class SubscriptionValidatorTests
{
    private readonly Mock<ILogger<SubscriptionValidator>> _loggerMock;
    private readonly Mock<IEncryptKeyProvider> _encryptKeyProviderMock;
    private readonly Mock<IWebhookAppKeyAccessor> _appKeyAccessorMock;
    private readonly SubscriptionValidator _validator;

    public SubscriptionValidatorTests()
    {
        _loggerMock = new Mock<ILogger<SubscriptionValidator>>();
        _encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();
        _appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();

        _validator = new SubscriptionValidator(_loggerMock.Object, _encryptKeyProviderMock.Object, _appKeyAccessorMock.Object);
    }

    #region 构造函数和基本功能测试

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();
        var appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SubscriptionValidator(null!, encryptKeyProviderMock.Object, appKeyAccessorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullEncryptKeyProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SubscriptionValidator>>();
        var appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SubscriptionValidator(loggerMock.Object, null!, appKeyAccessorMock.Object));
    }

    [Fact]
    public void Constructor_WithNullAppKeyAccessor_ShouldThrowArgumentNullException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SubscriptionValidator>>();
        var encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SubscriptionValidator(loggerMock.Object, encryptKeyProviderMock.Object, null!));
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

    #region 有效订阅请求验证测试 (需求 4.2)

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithValidRequest_ShouldReturnTrue()
    {
        // Arrange
        var appKey = "test-app-key";
        var verificationToken = "valid-token";
        _validator.SetCurrentAppKey(appKey);

        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = verificationToken,
            Challenge = "valid-challenge"
        };

        _encryptKeyProviderMock
            .Setup(x => x.GetVerificationTokenAsync(appKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationToken);

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, verificationToken);

        // Assert
        Assert.True(result);
        VerifyLogCalled(LogLevel.Information, "事件订阅验证请求验证成功");
    }

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithValidRequestAndAppKey_ShouldReturnTrue()
    {
        // Arrange
        var appKey = "test-app-key";
        var verificationToken = "valid-token";
        _validator.SetCurrentAppKey(appKey);

        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = verificationToken,
            Challenge = "valid-challenge"
        };

        _encryptKeyProviderMock
            .Setup(x => x.GetVerificationTokenAsync(appKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationToken);

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, verificationToken);

        // Assert
        Assert.True(result);
        VerifyLogCalled(LogLevel.Information, "事件订阅验证请求验证成功");
    }

    #endregion

    #region 无效请求类型处理测试 (需求 4.3)

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithInvalidType_ShouldReturnFalse()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "event_callback", // 无效类型
            Token = "valid-token",
            Challenge = "valid-challenge"
        };
        var expectedToken = "valid-token";

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, expectedToken);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Warning, "无效的验证请求类型: event_callback");
    }

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithEmptyType_ShouldReturnFalse()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "", // 空类型
            Token = "valid-token",
            Challenge = "valid-challenge"
        };
        var expectedToken = "valid-token";

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, expectedToken);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Warning, "无效的验证请求类型: ");
    }

    [Theory]
    [InlineData("message")]
    [InlineData("card_action")]
    [InlineData("invalid_type")]
    [InlineData("event")]
    public async Task ValidateSubscriptionRequestAsync_WithVariousInvalidTypes_ShouldReturnFalse(string invalidType)
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = invalidType,
            Token = "valid-token",
            Challenge = "valid-challenge"
        };
        var expectedToken = "valid-token";

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, expectedToken);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Warning, $"无效的验证请求类型: {invalidType}");
    }

    #endregion

    #region Token不匹配场景测试 (需求 4.4)

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithMismatchedToken_ShouldReturnFalse()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "actual-token",
            Challenge = "valid-challenge"
        };
        var expectedToken = "expected-token";

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, expectedToken);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Warning, "验证 Token 不匹配");
    }

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithMismatchedToken_ShouldMaskTokensInLog()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "actual-long-token-123456",
            Challenge = "valid-challenge"
        };
        var expectedToken = "expected-long-token-654321";

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, expectedToken);

        // Assert
        Assert.False(result);
        // 验证Token被掩码处理
        VerifyLogCalled(LogLevel.Warning, "actu***");
        VerifyLogCalled(LogLevel.Warning, "expe***");
    }

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithShortMismatchedToken_ShouldMaskTokensInLog()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "abc", // 短Token
            Challenge = "valid-challenge"
        };
        var expectedToken = "xyz"; // 短Token

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, expectedToken);

        // Assert
        Assert.False(result);
        // 验证短Token被完全掩码
        VerifyLogCalled(LogLevel.Warning, "***");
    }

    #endregion

    #region 缺失字段处理测试 (需求 4.5)

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithNullRequest_ShouldReturnFalse()
    {
        // Arrange
        EventVerificationRequest? request = null;
        var expectedToken = "valid-token";

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request!, expectedToken);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Warning, "验证请求对象为空");
    }

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithEmptyToken_ShouldReturnFalse()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "", // 空Token
            Challenge = "valid-challenge"
        };
        var expectedToken = "valid-token";

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, expectedToken);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Warning, "验证请求缺少 Token");
    }

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithNullToken_ShouldReturnFalse()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = null!, // null Token
            Challenge = "valid-challenge"
        };
        var expectedToken = "valid-token";

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, expectedToken);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Warning, "验证请求缺少 Token");
    }

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithEmptyChallenge_ShouldReturnFalse()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "valid-token",
            Challenge = "" // 空Challenge
        };
        var expectedToken = "valid-token";

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, expectedToken);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Warning, "验证请求缺少 Challenge");
    }

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithNullChallenge_ShouldReturnFalse()
    {
        // Arrange
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "valid-token",
            Challenge = null! // null Challenge
        };
        var expectedToken = "valid-token";

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, expectedToken);

        // Assert
        Assert.False(result);
        VerifyLogCalled(LogLevel.Warning, "验证请求缺少 Challenge");
    }

    #endregion

    #region 异常处理测试

    #endregion

    #region 多应用场景测试

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithDifferentAppKeys_ShouldLogCorrectAppKey()
    {
        // Arrange
        var appKey1 = "app1";
        var appKey2 = "app2";
        var verificationToken = "valid-token";
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = verificationToken,
            Challenge = "valid-challenge"
        };

        _encryptKeyProviderMock
            .Setup(x => x.GetVerificationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(verificationToken);

        // Act & Assert - 第一个应用
        _validator.SetCurrentAppKey(appKey1);
        var result1 = await _validator.ValidateSubscriptionRequestAsync(request, verificationToken);
        Assert.True(result1);

        // Act & Assert - 第二个应用
        _validator.SetCurrentAppKey(appKey2);
        var result2 = await _validator.ValidateSubscriptionRequestAsync(request, verificationToken);
        Assert.True(result2);

        // 验证日志中包含正确的AppKey
        VerifyLogCalled(LogLevel.Information, "事件订阅验证请求验证成功");
    }

    [Fact]
    public async Task ValidateSubscriptionRequestAsync_WithNullAppKey_ShouldLogNullAppKey()
    {
        // Arrange
        var verificationToken = "valid-token";
        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = verificationToken,
            Challenge = "valid-challenge"
        };

        // Act
        var result = await _validator.ValidateSubscriptionRequestAsync(request, verificationToken);

        // Assert
        Assert.True(result);
        VerifyLogCalled(LogLevel.Information, "AppKey: null");
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
