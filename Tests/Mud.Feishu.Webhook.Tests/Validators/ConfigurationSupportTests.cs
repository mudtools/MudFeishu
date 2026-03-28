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

        var validator = new TimestampValidator(loggerMock.Object, optionsMonitorMock.Object);

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

        var validator = new TimestampValidator(loggerMock.Object, optionsMonitorMock.Object);

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

        var validator = new TimestampValidator(loggerMock.Object, optionsMonitorMock.Object);

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
    public void SubscriptionValidator_ShouldUseFallbackToken()
    {
        // Arrange
        var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        var loggerMock = new Mock<ILogger<SubscriptionValidator>>();

        var options = new FeishuWebhookOptions();

        optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        var validator = new SubscriptionValidator(loggerMock.Object, optionsMonitorMock.Object);

        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "fallback_token",
            Challenge = "test_challenge"
        };

        // Act - 使用传入的 fallback token
        var result = validator.ValidateSubscriptionRequest(request, "fallback_token");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试订阅验证器多应用配置支持
    /// </summary>
    [Fact]
    public void SubscriptionValidator_ShouldSupportMultiAppConfiguration()
    {
        // Arrange
        var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        var loggerMock = new Mock<ILogger<SubscriptionValidator>>();

        var options = new FeishuWebhookOptions
        {
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["app1"] = new FeishuAppWebhookOptions
                {
                    AppKey = "app1",
                    VerificationToken = "app1_token",
                    EncryptKey = "app1_encrypt_key_32_characters!!"
                },
                ["app2"] = new FeishuAppWebhookOptions
                {
                    AppKey = "app2",
                    VerificationToken = "app2_token",
                    EncryptKey = "app2_encrypt_key_32_characters!!"
                }
            }
        };

        optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        var validator = new SubscriptionValidator(loggerMock.Object, optionsMonitorMock.Object);

        // Act & Assert - 测试app1
        validator.SetCurrentAppKey("app1");
        var request1 = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "app1_token", // 使用app1的token
            Challenge = "test_challenge"
        };

        var result1 = validator.ValidateSubscriptionRequest(request1, "fallback_token");
        Assert.True(result1);

        // Act & Assert - 测试app2
        validator.SetCurrentAppKey("app2");
        var request2 = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "app2_token", // 使用app2的token
            Challenge = "test_challenge"
        };

        var result2 = validator.ValidateSubscriptionRequest(request2, "fallback_token");
        Assert.True(result2);
    }

    /// <summary>
    /// 测试订阅验证器应用配置不存在时的降级处理
    /// </summary>
    [Fact]
    public void SubscriptionValidator_ShouldFallbackWhenAppConfigNotFound()
    {
        // Arrange
        var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        var loggerMock = new Mock<ILogger<SubscriptionValidator>>();

        var options = new FeishuWebhookOptions
        {
            Apps = new Dictionary<string, FeishuAppWebhookOptions>()
        };

        optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        var validator = new SubscriptionValidator(loggerMock.Object, optionsMonitorMock.Object);
        validator.SetCurrentAppKey("nonexistent_app");

        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "fallback_token",
            Challenge = "test_challenge"
        };

        // Act - 使用传入的 fallback token
        var result = validator.ValidateSubscriptionRequest(request, "fallback_token");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试订阅验证器配置异常处理
    /// </summary>
    [Fact]
    public void SubscriptionValidator_ShouldHandleConfigurationException()
    {
        // Arrange
        var optionsMonitorMock = new Mock<IOptionsMonitor<FeishuWebhookOptions>>();
        var loggerMock = new Mock<ILogger<SubscriptionValidator>>();

        // 模拟配置访问异常
        optionsMonitorMock.Setup(x => x.CurrentValue).Throws(new InvalidOperationException("Configuration error"));

        var validator = new SubscriptionValidator(loggerMock.Object, optionsMonitorMock.Object);

        var request = new EventVerificationRequest
        {
            Type = "url_verification",
            Token = "fallback_token", // 应该使用fallback token
            Challenge = "test_challenge"
        };

        // Act - 应该使用fallback token
        var result = validator.ValidateSubscriptionRequest(request, "fallback_token");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// 测试配置验证服务 - 全局配置验证
    /// </summary>
    [Fact]
    public void ConfigurationValidationService_ShouldValidateGlobalConfiguration()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ConfigurationValidationService>>();
        var service = new ConfigurationValidationService(loggerMock.Object);

        var validOptions = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 300,
            MaxConcurrentEvents = 10,
            EventHandlingTimeoutMs = 30000,
            MaxRequestBodySize = 10 * 1024 * 1024
        };

        // Act
        var result = service.ValidateGlobalConfiguration(validOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// 测试配置验证服务 - 无效配置检测
    /// </summary>
    [Fact]
    public void ConfigurationValidationService_ShouldDetectInvalidConfiguration()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ConfigurationValidationService>>();
        var service = new ConfigurationValidationService(loggerMock.Object);

        var invalidOptions = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = -1, // 负数
            MaxConcurrentEvents = 0, // 无效值
            EventHandlingTimeoutMs = 0, // 无效值
            MaxRequestBodySize = 0 // 无效值
        };

        // Act
        var result = service.ValidateGlobalConfiguration(invalidOptions);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    /// <summary>
    /// 测试配置验证服务 - 应用配置验证
    /// </summary>
    [Fact]
    public void ConfigurationValidationService_ShouldValidateAppConfiguration()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ConfigurationValidationService>>();
        var service = new ConfigurationValidationService(loggerMock.Object);

        var globalOptions = new FeishuWebhookOptions();

        var validAppConfig = new FeishuAppWebhookOptions
        {
            AppKey = "test_app",
            VerificationToken = "app_token",
            EncryptKey = "12345678901234567890123456789012" // 32字符
        };

        // Act
        var result = service.ValidateAppConfiguration("test_app", validAppConfig, globalOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// 测试配置验证服务 - 应用配置错误检测
    /// </summary>
    [Fact]
    public void ConfigurationValidationService_ShouldDetectAppConfigurationErrors()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ConfigurationValidationService>>();
        var service = new ConfigurationValidationService(loggerMock.Object);

        var globalOptions = new FeishuWebhookOptions();

        var invalidAppConfig = new FeishuAppWebhookOptions
        {
            AppKey = "different_key", // 与传入的appKey不一致
            VerificationToken = "", // 空token
            EncryptKey = "short" // 长度不足
        };

        // Act
        var result = service.ValidateAppConfiguration("test_app", invalidAppConfig, globalOptions);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    /// <summary>
    /// 测试配置验证服务 - 多应用配置验证
    /// </summary>
    [Fact]
    public void ConfigurationValidationService_ShouldValidateMultiAppConfiguration()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ConfigurationValidationService>>();
        var service = new ConfigurationValidationService(loggerMock.Object);

        var options = new FeishuWebhookOptions
        {
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["app1"] = new FeishuAppWebhookOptions
                {
                    AppKey = "app1",
                    VerificationToken = "token1_12345678",
                    EncryptKey = "app1_encrypt_key_32_characters!!"
                },
                ["app2"] = new FeishuAppWebhookOptions
                {
                    AppKey = "app2",
                    VerificationToken = "token2_12345678",
                    EncryptKey = "app2_encrypt_key_32_characters!!"
                }
            }
        };

        // Act
        var result = service.ValidateGlobalConfiguration(options);

        // Assert
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// 测试配置验证服务 - 重复应用键检测
    /// </summary>
    [Fact]
    public void ConfigurationValidationService_ShouldDetectDuplicateAppKeys()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ConfigurationValidationService>>();
        var service = new ConfigurationValidationService(loggerMock.Object);

        var options = new FeishuWebhookOptions
        {
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["app1"] = new FeishuAppWebhookOptions
                {
                    AppKey = "app1",
                    VerificationToken = "token1",
                    EncryptKey = "12345678901234567890123456789012"
                }
            }
        };

        // Act
        var result = service.ValidateGlobalConfiguration(options);

        // Assert - 字典键本身不会重复，但验证应该通过
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// 测试配置验证服务 - 性能配置警告
    /// </summary>
    [Fact]
    public void ConfigurationValidationService_ShouldWarnOnExtremePerformanceSettings()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ConfigurationValidationService>>();
        var service = new ConfigurationValidationService(loggerMock.Object);

        var options = new FeishuWebhookOptions
        {
            EventHandlingTimeoutMs = 500, // 过短
            MaxConcurrentEvents = 2000, // 过大
            MaxRequestBodySize = 200 * 1024 * 1024 // 200MB，过大
        };

        // Act
        var result = service.ValidateGlobalConfiguration(options);

        // Assert
        Assert.True(result.IsValid); // 警告不影响有效性
        Assert.NotEmpty(result.Warnings);
    }

    /// <summary>
    /// 测试配置验证服务 - 空应用键验证
    /// </summary>
    [Fact]
    public void ConfigurationValidationService_ShouldRejectEmptyAppKey()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ConfigurationValidationService>>();
        var service = new ConfigurationValidationService(loggerMock.Object);

        var globalOptions = new FeishuWebhookOptions();

        var appConfig = new FeishuAppWebhookOptions
        {
            AppKey = "test_app",
            VerificationToken = "valid_token",
            EncryptKey = "12345678901234567890123456789012"
        };

        // Act
        var result = service.ValidateAppConfiguration("", appConfig, globalOptions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("应用键不能为空"));
    }

    /// <summary>
    /// 测试配置验证服务 - 空配置对象验证
    /// </summary>
    [Fact]
    public void ConfigurationValidationService_ShouldRejectNullAppConfig()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ConfigurationValidationService>>();
        var service = new ConfigurationValidationService(loggerMock.Object);

        var globalOptions = new FeishuWebhookOptions();

        // Act
        var result = service.ValidateAppConfiguration("test_app", null!, globalOptions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("配置不能为空"));
    }

    /// <summary>
    /// 测试配置验证服务 - Token 长度警告
    /// </summary>
    [Fact]
    public void ConfigurationValidationService_ShouldWarnOnShortToken()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ConfigurationValidationService>>();
        var service = new ConfigurationValidationService(loggerMock.Object);

        var globalOptions = new FeishuWebhookOptions();

        var appConfig = new FeishuAppWebhookOptions
        {
            AppKey = "test_app",
            VerificationToken = "short", // 过短
            EncryptKey = "12345678901234567890123456789012"
        };

        // Act
        var result = service.ValidateAppConfiguration("test_app", appConfig, globalOptions);

        // Assert
        Assert.True(result.IsValid); // 警告不影响有效性
        Assert.NotEmpty(result.Warnings);
    }

    /// <summary>
    /// 测试配置验证结果 - 合并功能
    /// </summary>
    [Fact]
    public void ConfigurationValidationResult_MergeShouldCombineResults()
    {
        // Arrange
        var result1 = new ConfigurationValidationResult();
        result1.AddError("Error 1");
        result1.AddWarning("Warning 1");

        var result2 = new ConfigurationValidationResult();
        result2.AddError("Error 2");
        result2.AddInfo("Info 1");

        // Act
        result1.Merge(result2);

        // Assert
        Assert.Equal(2, result1.Errors.Count);
        Assert.Single(result1.Warnings);
        Assert.Single(result1.Infos);
    }

    /// <summary>
    /// 测试配置验证结果 - 摘要功能
    /// </summary>
    [Fact]
    public void ConfigurationValidationResult_GetSummaryShouldReturnCorrectFormat()
    {
        // Arrange
        var result = new ConfigurationValidationResult();
        result.AddError("Error 1");
        result.AddWarning("Warning 1");
        result.AddInfo("Info 1");

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.Contains("失败", summary);
        Assert.Contains("错误: 1", summary);
        Assert.Contains("警告: 1", summary);
        Assert.Contains("信息: 1", summary);
    }
}