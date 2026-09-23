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
            ["FeishuWebhook:EnableExceptionHandling"] = "false",
            ["FeishuWebhook:EnforceHeaderSignatureValidation"] = "false",
            ["FeishuWebhook:EnableTokenBackgroundRefresh"] = "true",
        });

        options.EnableExceptionHandling.Should().BeFalse();
        options.EnforceHeaderSignatureValidation.Should().BeFalse();
        options.EnableTokenBackgroundRefresh.Should().BeTrue();
    }

    /// <summary>
    /// 逃生开关（bool? 可空布尔）的配置绑定三态验证：
    /// "true"/"false" 字符串分别绑定 true/false，缺省键保留 null（= 不干预映射）。
    /// </summary>
    [Fact]
    public void Bind_ShouldMapNullableBoolEscapeHatch_WhenEnableTokenBackgroundRefreshBound()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebhook:EnableTokenBackgroundRefresh"] = "true",
        });
        options.EnableTokenBackgroundRefresh.Should().BeTrue();

        var disabled = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebhook:EnableTokenBackgroundRefresh"] = "false",
        });
        disabled.EnableTokenBackgroundRefresh.Should().BeFalse();

        var absent = BindFromDictionary(new Dictionary<string, string?>
        {
            // 未配置 EnableTokenBackgroundRefresh：必须保留 null
        });
        absent.EnableTokenBackgroundRefresh.Should().BeNull(
            "未配置时必须保留 null（= 不干预基座 TokenRefresh 映射）");
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
            // R3-P1-2/D6：多应用（Apps.Count > 1）下 ExpectedAppId 为必填——
            // 它是 EncryptKey 误配（把 B 的密钥填到 A）导致跨应用串扰的唯一兜底。
            ["FeishuWebhook:Apps:app1:AppKey"] = "cli_a1b2c3d4e5f6g7h8",
            ["FeishuWebhook:Apps:app1:VerificationToken"] = "token1",
            ["FeishuWebhook:Apps:app1:EncryptKey"] = "0123456789abcdef0123456789abcdef",
            ["FeishuWebhook:Apps:app1:ExpectedAppId"] = "cli_a1b2c3d4e5f6g7h8",
            ["FeishuWebhook:Apps:app2:AppKey"] = "cli_b1b2c3d4e5f6g7h8",
            ["FeishuWebhook:Apps:app2:VerificationToken"] = "token2",
            ["FeishuWebhook:Apps:app2:EncryptKey"] = "fedcba9876543210fedcba9876543210",
            ["FeishuWebhook:Apps:app2:ExpectedAppId"] = "cli_b1b2c3d4e5f6g7h8",
        });

        options.Apps.Should().HaveCount(2);
        options.Apps.Should().ContainKey("app1");
        options.Apps.Should().ContainKey("app2");
        options.Apps["app1"].VerificationToken.Should().Be("token1");
        options.Apps["app2"].VerificationToken.Should().Be("token2");

        // R5.2/X8：Apps:{key}:AppKey 不再写入派生字段（属性对宿主只读，internal set）。
        // 应用标识就是字典键本身——JSON 里重复配置 AppKey 只会制造「配置值与路由键分叉」的风险。
        options.Apps["app1"].AppKey.Should().BeEmpty(
            "JSON 中的 Apps:app1:AppKey 必须被忽略（应用标识 = 字典键 'app1'）");

        // 需要标识时使用字典键；Validate 会把它派生到 AppKey 供诊断输出。
        options.Validate();
        options.Apps["app1"].AppKey.Should().Be("app1");
        options.Apps["app2"].AppKey.Should().Be("app2");
    }

    [Fact]
    public void Bind_ShouldRetainDefaultValues_WhenConfigSectionIsMissing()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>());

        options.GlobalRoutePrefix.Should().Be("feishu");
#pragma warning disable CS0618 // R5/X4：该开关已 Obsolete，但仍须能从配置绑定（不得静默失效）
        options.AutoRegisterEndpoint.Should().BeTrue();
#pragma warning restore CS0618
        options.EnableExceptionHandling.Should().BeTrue();
        options.EventHandlingTimeoutMs.Should().Be(30000);
        options.MaxConcurrentEvents.Should().Be(10);
        options.EnforceHeaderSignatureValidation.Should().BeTrue();
        options.TimestampToleranceSeconds.Should().Be(30);
        options.EnableTokenBackgroundRefresh.Should().BeNull("R4：未配置时为 null，不覆盖宿主令牌刷新默认");
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
