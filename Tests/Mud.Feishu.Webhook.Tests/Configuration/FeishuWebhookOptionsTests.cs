// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Configuration;

/// <summary>
/// FeishuWebhookOptions 单元测试
/// </summary>
public class FeishuWebhookOptionsTests
{
    [Fact]
    public void FeishuWebhookOptions_DefaultValues_ShouldBeCorrect()
    {
        var options = new FeishuWebhookOptions();

        Assert.Equal(30, options.TimestampToleranceSeconds);
        Assert.Equal(30000, options.EventHandlingTimeoutMs);
        Assert.Equal(10, options.MaxConcurrentEvents);
        Assert.True(options.EnableExceptionHandling);
    }

    [Fact]
    public void FeishuWebhookOptions_SetCustomValues_ShouldWork()
    {
        var options = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 600,
            EventHandlingTimeoutMs = 10000,
            MaxConcurrentEvents = 200,
            EnableExceptionHandling = false
        };

        Assert.Equal(600, options.TimestampToleranceSeconds);
        Assert.Equal(10000, options.EventHandlingTimeoutMs);
        Assert.Equal(200, options.MaxConcurrentEvents);
        Assert.False(options.EnableExceptionHandling);
    }

    [Fact]
    public void FeishuWebhookOptions_SetTimeouts_ShouldAcceptValidValues()
    {
        var options = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 120,
            EventHandlingTimeoutMs = 30000
        };

        Assert.Equal(120, options.TimestampToleranceSeconds);
        Assert.Equal(30000, options.EventHandlingTimeoutMs);
    }

    [Fact]
    public void FeishuWebhookOptions_SetMaxConcurrentEvents_ShouldAcceptPositiveValues()
    {
        var options1 = new FeishuWebhookOptions { MaxConcurrentEvents = 1 };
        var options2 = new FeishuWebhookOptions { MaxConcurrentEvents = 500 };
        var options3 = new FeishuWebhookOptions { MaxConcurrentEvents = 1000 };

        Assert.Equal(1, options1.MaxConcurrentEvents);
        Assert.Equal(500, options2.MaxConcurrentEvents);
        Assert.Equal(1000, options3.MaxConcurrentEvents);
    }

    #region Validate() Method Tests

    [Fact]
    public void Validate_WithValidOptions_ShouldNotThrow()
    {
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

        var exception = Record.Exception(() => options.Validate());
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithInvalidEventHandlingTimeoutMs_ShouldThrow()
    {
        var options = new FeishuWebhookOptions
        {
            EventHandlingTimeoutMs = 100
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("EventHandlingTimeoutMs", ex.Message);
    }

    [Fact]
    public void Validate_WithInvalidMaxConcurrentEvents_ShouldThrow()
    {
        var options = new FeishuWebhookOptions
        {
            MaxConcurrentEvents = 0
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("MaxConcurrentEvents", ex.Message);
    }

    [Fact]
    public void Validate_WithInvalidMaxRequestBodySize_ShouldThrow()
    {
        var options = new FeishuWebhookOptions
        {
            MaxRequestBodySize = 100
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("MaxRequestBodySize", ex.Message);
    }

    [Fact]
    public void Validate_WithNegativeTimestampTolerance_ShouldThrow()
    {
        var options = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = -1
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("TimestampToleranceSeconds", ex.Message);
    }

    [Fact]
    public void Validate_WithMissingEncryptKey_ShouldThrow()
    {
        var options = new FeishuWebhookOptions
        {
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["test-app"] = new FeishuAppWebhookOptions
                {
                    AppKey = "test-app",
                    VerificationToken = "test_token",
                    EncryptKey = ""
                }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("EncryptKey", ex.Message);
    }

    [Fact]
    public void Validate_WithInvalidEncryptKeyLength_ShouldThrow()
    {
        var options = new FeishuWebhookOptions
        {
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["test-app"] = new FeishuAppWebhookOptions
                {
                    AppKey = "test-app",
                    VerificationToken = "test_token",
                    EncryptKey = "short_key"
                }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("EncryptKey", ex.Message);
        Assert.Contains("32", ex.Message);
    }

    [Fact]
    public void Validate_WithMissingVerificationToken_ShouldThrow()
    {
        var options = new FeishuWebhookOptions
        {
            Apps = new Dictionary<string, FeishuAppWebhookOptions>
            {
                ["test-app"] = new FeishuAppWebhookOptions
                {
                    AppKey = "test-app",
                    VerificationToken = "",
                    EncryptKey = "12345678901234567890123456789012"
                }
            }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate());
        Assert.Contains("VerificationToken", ex.Message);
    }

    #endregion
}
