// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Webhook.Configuration;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Configuration;

/// <summary>
/// 配置验证器单元测试
/// </summary>
public class ConfigurationValidatorsTests
{
    #region FeishuWebhookOptionsValidator Tests

    [Fact]
    public void FeishuWebhookOptionsValidator_WithNullOptions_ShouldReturnFail()
    {
        var validator = new FeishuWebhookOptionsValidator();
        var result = validator.Validate(null, null!);

        Assert.True(result.Failed);
        Assert.Contains("不能为 null", result.FailureMessage);
    }

    [Fact]
    public void FeishuWebhookOptionsValidator_WithValidOptions_ShouldReturnSuccess()
    {
        var validator = new FeishuWebhookOptionsValidator();
        var options = new FeishuWebhookOptions
        {
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["test-app"] = new FeishuAppWebhookOptions
                {
                    AppKey = "test-app",
                    VerificationToken = "test_token",
                    EncryptKey = "12345678901234567890123456789012"
                }
            }
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void FeishuWebhookOptionsValidator_WithInvalidTimeout_ShouldReturnFail()
    {
        var validator = new FeishuWebhookOptionsValidator();
        var options = new FeishuWebhookOptions
        {
            EventHandlingTimeoutMs = 100,
            Apps = new Dictionary<string, FeishuAppWebhookOptions>()
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("EventHandlingTimeoutMs", result.FailureMessage);
    }

    #endregion

    #region FeishuAppWebhookOptionsValidator Tests

    [Fact]
    public void FeishuAppWebhookOptionsValidator_WithNullOptions_ShouldReturnFail()
    {
        var validator = new FeishuAppWebhookOptionsValidator();
        var result = validator.Validate(null, null!);

        Assert.True(result.Failed);
        Assert.Contains("不能为 null", result.FailureMessage);
    }

    [Fact]
    public void FeishuAppWebhookOptionsValidator_WithValidOptions_ShouldReturnSuccess()
    {
        var validator = new FeishuAppWebhookOptionsValidator();
        var options = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            VerificationToken = "test_token",
            EncryptKey = "12345678901234567890123456789012"
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void FeishuAppWebhookOptionsValidator_WithEmptyAppKey_ShouldReturnFail()
    {
        var validator = new FeishuAppWebhookOptionsValidator();
        var options = new FeishuAppWebhookOptions
        {
            AppKey = "",
            VerificationToken = "test_token",
            EncryptKey = "12345678901234567890123456789012"
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("AppKey", result.FailureMessage);
    }

    [Fact]
    public void FeishuAppWebhookOptionsValidator_WithInvalidEncryptKeyLength_ShouldReturnFail()
    {
        var validator = new FeishuAppWebhookOptionsValidator();
        var options = new FeishuAppWebhookOptions
        {
            AppKey = "test-app",
            VerificationToken = "test_token",
            EncryptKey = "short_key"
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("EncryptKey", result.FailureMessage);
    }

    #endregion

    #region RateLimitOptionsValidator Tests

    [Fact]
    public void RateLimitOptionsValidator_WithNullOptions_ShouldReturnFail()
    {
        var validator = new RateLimitOptionsValidator();
        var result = validator.Validate(null, null!);

        Assert.True(result.Failed);
        Assert.Contains("不能为 null", result.FailureMessage);
    }

    [Fact]
    public void RateLimitOptionsValidator_WithDisabledRateLimit_ShouldReturnSuccess()
    {
        var validator = new RateLimitOptionsValidator();
        var options = new RateLimitOptions
        {
            EnableRateLimit = false
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void RateLimitOptionsValidator_WithValidEnabledRateLimit_ShouldReturnSuccess()
    {
        var validator = new RateLimitOptionsValidator();
        var options = new RateLimitOptions
        {
            EnableRateLimit = true,
            WindowSizeSeconds = 60,
            MaxRequestsPerWindow = 100
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void RateLimitOptionsValidator_WithInvalidWindowSize_ShouldReturnFail()
    {
        var validator = new RateLimitOptionsValidator();
        var options = new RateLimitOptions
        {
            EnableRateLimit = true,
            WindowSizeSeconds = 0
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("WindowSizeSeconds", result.FailureMessage);
    }

    [Fact]
    public void RateLimitOptionsValidator_WithInvalidStatusCode_ShouldReturnFail()
    {
        var validator = new RateLimitOptionsValidator();
        var options = new RateLimitOptions
        {
            EnableRateLimit = true,
            WindowSizeSeconds = 60,
            MaxRequestsPerWindow = 100,
            TooManyRequestsStatusCode = 200
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("TooManyRequestsStatusCode", result.FailureMessage);
    }

    #endregion
}
