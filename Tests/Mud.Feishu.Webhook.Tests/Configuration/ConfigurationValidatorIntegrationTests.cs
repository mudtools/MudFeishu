// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Webhook.Configuration;
using Mud.HttpUtils;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Configuration;

/// <summary>
/// 配置验证器 DI 集成测试
/// </summary>
public class ConfigurationValidatorIntegrationTests
{
    [Fact]
    public void ServiceCollection_ShouldRegisterFeishuWebhookOptionsValidator()
    {
        var services = new ServiceCollection();
        services.AddOptions<FeishuWebhookOptions>();
        services.AddSingleton<IValidateOptions<FeishuWebhookOptions>, FeishuWebhookOptionsValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var validator = serviceProvider.GetService<IValidateOptions<FeishuWebhookOptions>>();

        Assert.NotNull(validator);
        Assert.IsType<FeishuWebhookOptionsValidator>(validator);
    }

    [Fact]
    public void ServiceCollection_ShouldRegisterRateLimitOptionsValidator()
    {
        var services = new ServiceCollection();
        services.AddOptions<RateLimitOptions>();
        services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var validator = serviceProvider.GetService<IValidateOptions<RateLimitOptions>>();

        Assert.NotNull(validator);
        Assert.IsType<RateLimitOptionsValidator>(validator);
    }

    [Fact]
    public void OptionsValidation_WithValidOptions_ShouldSucceed()
    {
        var services = new ServiceCollection();
        services.AddOptions<FeishuWebhookOptions>()
            .Configure(options =>
            {
                options.Apps = new Dictionary<string, FeishuAppWebhookOptions>
                {
                    ["test-app"] = new FeishuAppWebhookOptions
                    {
                        AppKey = "test-app",
                        VerificationToken = "test_token",
                        EncryptKey = "12345678901234567890123456789012"
                    }
                };
            });
        services.AddSingleton<IValidateOptions<FeishuWebhookOptions>, FeishuWebhookOptionsValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<FeishuWebhookOptions>>();

        Assert.NotNull(options.Value);
    }

    [Fact]
    public void OptionsValidation_WithInvalidOptions_ShouldFail()
    {
        var validator = new FeishuWebhookOptionsValidator();
        var options = new FeishuWebhookOptions
        {
            EventHandlingTimeoutMs = 100
        };

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("EventHandlingTimeoutMs", result.FailureMessage);
    }

    [Fact]
    public void RateLimitOptions_Validation_WithDisabledRateLimit_ShouldSucceed()
    {
        var services = new ServiceCollection();
        services.AddOptions<RateLimitOptions>()
            .Configure(options =>
            {
                options.EnableRateLimit = false;
            });
        services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RateLimitOptions>>();

        Assert.NotNull(options.Value);
        Assert.False(options.Value.EnableRateLimit);
    }

    [Fact]
    public void RateLimitOptions_Validation_WithEnabledRateLimit_ShouldValidate()
    {
        var services = new ServiceCollection();
        services.AddOptions<RateLimitOptions>()
            .Configure(options =>
            {
                options.EnableRateLimit = true;
                options.WindowSizeSeconds = 60;
                options.MaxRequestsPerWindow = 100;
            });
        services.AddSingleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>();

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<RateLimitOptions>>();

        Assert.NotNull(options.Value);
        Assert.True(options.Value.EnableRateLimit);
    }

    [Fact]
    public void EnableBackgroundProcessing_ShouldMapToTokenRefreshBackgroundOptions_WhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddOptions<FeishuWebhookOptions>()
            .Configure(options =>
            {
                options.EnableBackgroundProcessing = true;
            });
        services.AddOptions<TokenRefreshBackgroundOptions>()
            .PostConfigure<IOptions<FeishuWebhookOptions>>((tokenOptions, webhookOptions) =>
            {
                tokenOptions.Enabled = webhookOptions.Value.EnableBackgroundProcessing;
            });

        var serviceProvider = services.BuildServiceProvider();
        var tokenOptions = serviceProvider.GetRequiredService<IOptions<TokenRefreshBackgroundOptions>>();

        Assert.True(tokenOptions.Value.Enabled);
    }

    [Fact]
    public void EnableBackgroundProcessing_ShouldMapToTokenRefreshBackgroundOptions_WhenDisabled()
    {
        var services = new ServiceCollection();
        services.AddOptions<FeishuWebhookOptions>()
            .Configure(options =>
            {
                options.EnableBackgroundProcessing = false;
            });
        services.AddOptions<TokenRefreshBackgroundOptions>()
            .PostConfigure<IOptions<FeishuWebhookOptions>>((tokenOptions, webhookOptions) =>
            {
                tokenOptions.Enabled = webhookOptions.Value.EnableBackgroundProcessing;
            });

        var serviceProvider = services.BuildServiceProvider();
        var tokenOptions = serviceProvider.GetRequiredService<IOptions<TokenRefreshBackgroundOptions>>();

        Assert.False(tokenOptions.Value.Enabled);
    }

    [Fact]
    public void EnableBackgroundProcessing_DefaultValue_ShouldMapToTokenRefreshBackgroundOptions()
    {
        var services = new ServiceCollection();
        services.AddOptions<FeishuWebhookOptions>();
        services.AddOptions<TokenRefreshBackgroundOptions>()
            .PostConfigure<IOptions<FeishuWebhookOptions>>((tokenOptions, webhookOptions) =>
            {
                tokenOptions.Enabled = webhookOptions.Value.EnableBackgroundProcessing;
            });

        var serviceProvider = services.BuildServiceProvider();
        var webhookOptions = serviceProvider.GetRequiredService<IOptions<FeishuWebhookOptions>>();
        var tokenOptions = serviceProvider.GetRequiredService<IOptions<TokenRefreshBackgroundOptions>>();

        Assert.False(webhookOptions.Value.EnableBackgroundProcessing);
        Assert.False(tokenOptions.Value.Enabled);
    }
}
