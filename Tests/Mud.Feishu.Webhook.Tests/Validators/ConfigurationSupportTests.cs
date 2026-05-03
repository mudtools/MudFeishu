// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Validators;

/// <summary>
/// 配置支持单元测试
/// 测试配置读取功能、多应用配置处理和配置错误处理
/// **验证需求: 6.4, 6.5**
/// </summary>
public class ConfigurationSupportTests
{
    private readonly Mock<IWebhookAppKeyAccessor> _appKeyAccessorMock;

    public ConfigurationSupportTests()
    {
        _appKeyAccessorMock = new Mock<IWebhookAppKeyAccessor>();
    }

    /// <summary>
    /// 测试时间戳验证器从配置读取容错时间
    /// </summary>
    [Fact]
    public void TimestampValidator_ShouldReadToleranceFromConfiguration()
    {
        // Arrange
        var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        var loggerMock = new Mock<ILogger<TimestampValidator>>();

        var options = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 120 // 2分钟
        };

        optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        var validator = new TimestampValidator(loggerMock.Object, optionsMonitorMock.Object, _appKeyAccessorMock.Object);

        // Act - 使用默认参数，应该从配置读取
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var pastTime = currentTime - 100; // 100秒前，在120秒容错范围内
        var result = validator.ValidateTimestamp(pastTime);

        // Assert
        Assert.True(result);
        optionsMonitorMock.Verify(x => x.CurrentValue, Times.AtLeastOnce);
    }

    /// <summary>
    /// 测试时间戳验证器超出配置容错范围
    /// </summary>
    [Fact]
    public void TimestampValidator_ShouldRejectWhenExceedsConfiguredTolerance()
    {
        // Arrange
        var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        var loggerMock = new Mock<ILogger<TimestampValidator>>();

        var options = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 60 // 1分钟
        };

        optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        var validator = new TimestampValidator(loggerMock.Object, optionsMonitorMock.Object, _appKeyAccessorMock.Object);

        // Act - 使用默认参数，应该从配置读取
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var pastTime = currentTime - 120; // 120秒前，超出60秒容错范围
        var result = validator.ValidateTimestamp(pastTime);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// 测试时间戳验证器处理无效配置
    /// </summary>
    [Fact]
    public void TimestampValidator_ShouldHandleInvalidConfiguration()
    {
        // Arrange
        var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        var loggerMock = new Mock<ILogger<TimestampValidator>>();

        var options = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = -10 // 无效的负数
        };

        optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        var validator = new TimestampValidator(loggerMock.Object, optionsMonitorMock.Object, _appKeyAccessorMock.Object);

        // Act - 应该使用默认值300秒
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var pastTime = currentTime - 200; // 200秒前，在默认300秒范围内
        var result = validator.ValidateTimestamp(pastTime);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试订阅验证器使用后备 Token
    /// </summary>
    [Fact]
    public async Task SubscriptionValidator_ShouldUseFallbackToken()
    {
        // Arrange
        var encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();
        var loggerMock = new Mock<ILogger<SubscriptionValidator>>();

        var validator = new SubscriptionValidator(loggerMock.Object, encryptKeyProviderMock.Object, _appKeyAccessorMock.Object);

        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "fallback_token",
            Challenge = "test_challenge"
        };

        // Act - 使用传入的 fallback token
        var result = await validator.ValidateSubscriptionRequestAsync(request, "fallback_token");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试订阅验证器多应用配置支持
    /// </summary>
    [Fact]
    public async Task SubscriptionValidator_ShouldSupportMultiAppConfiguration()
    {
        // Arrange
        var encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();
        var loggerMock = new Mock<ILogger<SubscriptionValidator>>();

        encryptKeyProviderMock
            .Setup(x => x.GetVerificationTokenAsync("app1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("app1_token");

        encryptKeyProviderMock
            .Setup(x => x.GetVerificationTokenAsync("app2", It.IsAny<CancellationToken>()))
            .ReturnsAsync("app2_token");

        var validator = new SubscriptionValidator(loggerMock.Object, encryptKeyProviderMock.Object, _appKeyAccessorMock.Object);

        // Act & Assert - 测试app1
        validator.SetCurrentAppKey("app1");
        var request1 = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "app1_token", // 使用app1的token
            Challenge = "test_challenge"
        };

        var result1 = await validator.ValidateSubscriptionRequestAsync(request1, "app1_token");
        Assert.True(result1);

        // Act & Assert - 测试app2
        validator.SetCurrentAppKey("app2");
        var request2 = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "app2_token", // 使用app2的token
            Challenge = "test_challenge"
        };

        var result2 = await validator.ValidateSubscriptionRequestAsync(request2, "app2_token");
        Assert.True(result2);
    }

    /// <summary>
    /// 测试订阅验证器应用配置不存在时的降级处理
    /// </summary>
    [Fact]
    public async Task SubscriptionValidator_ShouldFallbackWhenAppConfigNotFound()
    {
        // Arrange
        var encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();
        var loggerMock = new Mock<ILogger<SubscriptionValidator>>();

        encryptKeyProviderMock
            .Setup(x => x.GetVerificationTokenAsync("nonexistent_app", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var validator = new SubscriptionValidator(loggerMock.Object, encryptKeyProviderMock.Object, _appKeyAccessorMock.Object);
        validator.SetCurrentAppKey("nonexistent_app");

        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "fallback_token",
            Challenge = "test_challenge"
        };

        // Act - 使用传入的 fallback token
        var result = await validator.ValidateSubscriptionRequestAsync(request, "fallback_token");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试订阅验证器配置异常处理
    /// </summary>
    [Fact]
    public async Task SubscriptionValidator_ShouldHandleConfigurationException()
    {
        // Arrange
        var encryptKeyProviderMock = new Mock<IEncryptKeyProvider>();
        var loggerMock = new Mock<ILogger<SubscriptionValidator>>();

        // 模拟配置访问异常
        encryptKeyProviderMock
            .Setup(x => x.GetVerificationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Configuration error"));

        var validator = new SubscriptionValidator(loggerMock.Object, encryptKeyProviderMock.Object, _appKeyAccessorMock.Object);

        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "fallback_token", // 应该使用fallback token
            Challenge = "test_challenge"
        };

        // Act - 应该使用fallback token
        var result = await validator.ValidateSubscriptionRequestAsync(request, "fallback_token");

        // Assert
        Assert.True(result);
    }
}
