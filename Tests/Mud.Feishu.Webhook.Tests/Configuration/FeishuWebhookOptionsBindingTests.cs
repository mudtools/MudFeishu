// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Configuration;

/// <summary>
/// FeishuWebhookOptions 配置绑定测试。
/// 验证 JSON/内存配置 → FeishuWebhookOptions 的绑定路径，覆盖标量、嵌套对象、字典、HashSet 等绑定场景。
/// 对应生产代码：FeishuWebhookServiceBuilder.ConfigureFrom(IConfiguration) 调用。
/// </summary>
public class FeishuWebhookOptionsBindingTests
{
    /// <summary>
    /// 构造内存配置源并绑定到 FeishuWebhookOptions。
    /// </summary>
    private static FeishuWebhookOptions BindFromDictionary(Dictionary<string, string?> configData)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var options = new FeishuWebhookOptions();
        configuration.GetSection("FeishuWebhook").Bind(options);
        return options;
    }

    [Fact]
    public void Bind_ShouldMapScalarStringFields_WhenJsonContainsStringValues()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebhook:GlobalRoutePrefix"] = "custom-feishu",
        });

        options.GlobalRoutePrefix.Should().Be("custom-feishu");
    }

    [Fact]
    public void Bind_ShouldMapScalarIntFields_WhenJsonContainsIntegerValues()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebhook:EventHandlingTimeoutMs"] = "60000",
            ["FeishuWebhook:MaxConcurrentEvents"] = "50",
            ["FeishuWebhook:TimestampToleranceSeconds"] = "60",
            ["FeishuWebhook:MaxRequestBodySize"] = "20971520",
        });

        options.EventHandlingTimeoutMs.Should().Be(60000);
        options.MaxConcurrentEvents.Should().Be(50);
        options.TimestampToleranceSeconds.Should().Be(60);
        options.MaxRequestBodySize.Should().Be(20971520);
    }

    [Fact]
    public void Bind_ShouldMapBooleanFields_WhenJsonContainsBooleanValues()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebhook:EnableRequestLogging"] = "false",
            ["FeishuWebhook:EnableExceptionHandling"] = "false",
            ["FeishuWebhook:EnablePerformanceMonitoring"] = "true",
            ["FeishuWebhook:EnforceHeaderSignatureValidation"] = "false",
            ["FeishuWebhook:EnableBackgroundProcessing"] = "true",
        });

        options.EnableRequestLogging.Should().BeFalse();
        options.EnableExceptionHandling.Should().BeFalse();
        options.EnablePerformanceMonitoring.Should().BeTrue();
        options.EnforceHeaderSignatureValidation.Should().BeFalse();
        options.EnableBackgroundProcessing.Should().BeTrue();
    }

    [Fact]
    public void Bind_ShouldMapEnumField_WhenJsonContainsEnumValue()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebhook:NonceValidationFailureMode"] = "Allow",
        });

        options.NonceValidationFailureMode.Should().Be(NonceFailureMode.Allow);
    }

    [Fact]
    public void Bind_ShouldMapNestedRetryOptions_WhenJsonContainsRetrySection()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebhook:Retry:EnableRetry"] = "true",
            ["FeishuWebhook:Retry:MaxRetryCount"] = "5",
            ["FeishuWebhook:Retry:InitialRetryDelaySeconds"] = "15",
            ["FeishuWebhook:Retry:RetryDelayMultiplier"] = "3.0",
            ["FeishuWebhook:Retry:MaxRetryDelaySeconds"] = "600",
            ["FeishuWebhook:Retry:RetryPollIntervalSeconds"] = "60",
            ["FeishuWebhook:Retry:MaxRetryPerPoll"] = "20",
        });

        options.Retry.EnableRetry.Should().BeTrue();
        options.Retry.MaxRetryCount.Should().Be(5);
        options.Retry.InitialRetryDelaySeconds.Should().Be(15);
        options.Retry.RetryDelayMultiplier.Should().Be(3.0);
        options.Retry.MaxRetryDelaySeconds.Should().Be(600);
        options.Retry.RetryPollIntervalSeconds.Should().Be(60);
        options.Retry.MaxRetryPerPoll.Should().Be(20);
    }

    [Fact]
    public void Bind_ShouldMapNestedRateLimitOptions_WhenJsonContainsRateLimitSection()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebhook:RateLimit:EnableRateLimit"] = "true",
            ["FeishuWebhook:RateLimit:MaxRequestsPerWindow"] = "100",
            ["FeishuWebhook:RateLimit:WindowSizeSeconds"] = "30",
        });

        options.RateLimit.EnableRateLimit.Should().BeTrue();
        options.RateLimit.MaxRequestsPerWindow.Should().Be(100);
        options.RateLimit.WindowSizeSeconds.Should().Be(30);
    }

    [Fact]
    public void Bind_ShouldMapAppsDictionary_WhenJsonContainsAppsSection()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebhook:Apps:app1:AppKey"] = "cli_a1b2c3d4e5f6g7h8",
            ["FeishuWebhook:Apps:app1:VerificationToken"] = "token1",
            ["FeishuWebhook:Apps:app1:EncryptKey"] = "encrypt_key_1_32_bytes_long_1234567890",
            ["FeishuWebhook:Apps:app2:AppKey"] = "cli_b1b2c3d4e5f6g7h8",
            ["FeishuWebhook:Apps:app2:VerificationToken"] = "token2",
            ["FeishuWebhook:Apps:app2:EncryptKey"] = "encrypt_key_2_32_bytes_long_1234567890",
        });

        options.Apps.Should().HaveCount(2);
        options.Apps.Should().ContainKey("app1");
        options.Apps.Should().ContainKey("app2");
        options.Apps["app1"].AppKey.Should().Be("cli_a1b2c3d4e5f6g7h8");
        options.Apps["app1"].VerificationToken.Should().Be("token1");
        options.Apps["app2"].AppKey.Should().Be("cli_b1b2c3d4e5f6g7h8");
        options.Apps["app2"].VerificationToken.Should().Be("token2");
    }

    [Fact]
    public void Bind_ShouldRetainDefaultValues_WhenConfigSectionIsMissing()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>());

        options.GlobalRoutePrefix.Should().Be("feishu");
        options.AutoRegisterEndpoint.Should().BeTrue();
        options.EnableRequestLogging.Should().BeTrue();
        options.EnableExceptionHandling.Should().BeTrue();
        options.EventHandlingTimeoutMs.Should().Be(30000);
        options.MaxConcurrentEvents.Should().Be(10);
        options.EnablePerformanceMonitoring.Should().BeFalse();
        options.EnforceHeaderSignatureValidation.Should().BeTrue();
        options.TimestampToleranceSeconds.Should().Be(30);
        options.EnableBackgroundProcessing.Should().BeFalse();
        options.NonceValidationFailureMode.Should().Be(NonceFailureMode.Reject);
    }

    [Fact]
    public void Bind_ShouldRetainDefaultRetryOptions_WhenRetrySectionIsMissing()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>());

        options.Retry.EnableRetry.Should().BeFalse();
        options.Retry.MaxRetryCount.Should().Be(3);
        options.Retry.InitialRetryDelaySeconds.Should().Be(10);
        options.Retry.RetryDelayMultiplier.Should().Be(2.0);
        options.Retry.MaxRetryDelaySeconds.Should().Be(300);
        options.Retry.RetryPollIntervalSeconds.Should().Be(30);
        options.Retry.MaxRetryPerPoll.Should().Be(10);
    }

    [Fact]
    public void Bind_ShouldRetainDefaultRateLimitOptions_WhenRateLimitSectionIsMissing()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>());

        options.RateLimit.EnableRateLimit.Should().BeFalse();
    }
}
