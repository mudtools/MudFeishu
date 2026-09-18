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
        var services = CreateWebhookServicesWithMappingPipeline(
            webhookOptions => webhookOptions.EnableBackgroundProcessing = true);

        var tokenOptions = services.GetRequiredService<IOptions<TokenRefreshBackgroundOptions>>();

        Assert.True(tokenOptions.Value.Enabled);
    }

    [Fact]
    public void EnableBackgroundProcessing_ShouldMapToTokenRefreshBackgroundOptions_WhenDisabled()
    {
        var services = CreateWebhookServicesWithMappingPipeline(
            webhookOptions => webhookOptions.EnableBackgroundProcessing = false);

        var tokenOptions = services.GetRequiredService<IOptions<TokenRefreshBackgroundOptions>>();

        Assert.False(tokenOptions.Value.Enabled);
    }

    [Fact]
    public void EnableBackgroundProcessing_DefaultValue_ShouldMapToTokenRefreshBackgroundOptions()
    {
        var services = CreateWebhookServicesWithMappingPipeline(_ => { });

        var webhookOptions = services.GetRequiredService<IOptions<FeishuWebhookOptions>>();
        var tokenOptions = services.GetRequiredService<IOptions<TokenRefreshBackgroundOptions>>();

        Assert.False(webhookOptions.Value.EnableBackgroundProcessing);
        Assert.False(tokenOptions.Value.Enabled);
    }

    /// <summary>
    /// 逃生开关验证：<see cref="FeishuWebhookOptions.EnableTokenBackgroundRefresh"/> 显式设为 true 时，
    /// 即使 Webhook 后台处理关闭（EnableBackgroundProcessing=false），也应开启令牌后台刷新。
    /// 业务场景：修复"宿主已配置应用、但因 Webhook 后台处理默认关闭而令牌后台刷新被静默关闭且无恢复入口"。
    /// </summary>
    [Fact]
    public void EnableTokenBackgroundRefresh_ShouldOverrideMapping_WhenExplicitlyEnabled()
    {
        var services = CreateWebhookServicesWithMappingPipeline(webhookOptions =>
        {
            webhookOptions.EnableBackgroundProcessing = false;
            webhookOptions.EnableTokenBackgroundRefresh = true;
        });

        var tokenOptions = services.GetRequiredService<IOptions<TokenRefreshBackgroundOptions>>();

        Assert.True(tokenOptions.Value.Enabled,
            "显式 EnableTokenBackgroundRefresh=true 必须覆盖 EnableBackgroundProcessing 的映射");
    }

    /// <summary>
    /// 逃生开关验证：显式设为 false 时应关闭令牌后台刷新，即使 Webhook 后台处理已启用。
    /// </summary>
    [Fact]
    public void EnableTokenBackgroundRefresh_ShouldOverrideMapping_WhenExplicitlyDisabled()
    {
        var services = CreateWebhookServicesWithMappingPipeline(webhookOptions =>
        {
            webhookOptions.EnableBackgroundProcessing = true;
            webhookOptions.EnableTokenBackgroundRefresh = false;
        });

        var tokenOptions = services.GetRequiredService<IOptions<TokenRefreshBackgroundOptions>>();

        Assert.False(tokenOptions.Value.Enabled,
            "显式 EnableTokenBackgroundRefresh=false 必须关闭令牌后台刷新");
    }

    /// <summary>
    /// 逃生开关默认值验证：<c>null</c> = 不干预，沿用 EnableBackgroundProcessing 映射（保持既有行为）。
    /// </summary>
    [Fact]
    public void EnableTokenBackgroundRefresh_DefaultNull_ShouldNotIntervene()
    {
        var services = CreateWebhookServicesWithMappingPipeline(_ => { });

        var webhookOptions = services.GetRequiredService<IOptions<FeishuWebhookOptions>>();
        var tokenOptions = services.GetRequiredService<IOptions<TokenRefreshBackgroundOptions>>();

        Assert.Null(webhookOptions.Value.EnableTokenBackgroundRefresh);
        Assert.False(webhookOptions.Value.EnableBackgroundProcessing);
        Assert.False(tokenOptions.Value.Enabled,
            "null 表示不干预：结果应与 EnableBackgroundProcessing 的映射一致");
    }

    // ============================================================
    // 辅助：经 FeishuWebhookServiceBuilder 真实管道构建映射（替代内联复刻 PostConfigure lambda）。
    // ============================================================

    /// <summary>
    /// 经 FeishuWebhookServiceBuilder（生产代码实际注册 PostConfigure 的入口）构建服务提供者，
    /// 断言基于真实管道而非测试内手写复刻映射表达式，避免生产映射变更时用例假绿。
    /// 通过 ConfigureOptions 注入 FeishuWebhookOptions 配置，AddHandler 满足 Build() 的强制约束。
    /// TokenRefreshBackgroundOptions 的 AddOptions 基础设施与 AddFeishuAppBaseServices 中的
    /// 注册一致（AddFeishuWebhook 管道自身不注册它）。
    /// </summary>
    private static IServiceProvider CreateWebhookServicesWithMappingPipeline(
        Action<FeishuWebhookOptions> configureWebhookOptions)
    {
        var services = new ServiceCollection();
        services.CreateFeishuWebhookServiceBuilder(configureWebhookOptions)
            .AddHandler<NoOpFeishuEventHandler>()
            .Build();

        services.AddOptions<TokenRefreshBackgroundOptions>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 最小化事件处理器桩，仅用于满足 FeishuWebhookServiceBuilder.Build() 的"至少一个处理器"约束。
    /// </summary>
    private sealed class NoOpFeishuEventHandler : Mud.Feishu.Abstractions.IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";
        public Task HandleAsync(Mud.Feishu.Abstractions.EventData eventData, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
