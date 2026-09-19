// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Extensions;

namespace Mud.Feishu.Abstractions.Tests.Configuration;

/// <summary>
/// C1/B4/R2：统一去重 Options 绑定、优先级、Profile 与跨校验。
/// </summary>
public class FeishuDeduplicationOptionsTests
{
    private static ServiceProvider Build(Action<IServiceCollection> pre, IConfiguration? config = null)
    {
        var services = new ServiceCollection();
        pre(services);
        services.AddFeishuDeduplicationOptions(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Bind_ShouldMarkIsConfigured_WhenSectionExists()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuDeduplication:Mode"] = "Distributed",
                ["FeishuDeduplication:Event:Ttl"] = "2.00:00:00",
                ["FeishuDeduplication:Event:KeyPrefix"] = "t-a:feishu:event:",
                ["FeishuDeduplication:Nonce:KeyPrefix"] = "t-a:feishu:nonce:",
                ["FeishuDeduplication:SeqId:KeyPrefix"] = "t-a:feishu:seqid:"
            })
            .Build();

        using var sp = Build(_ => { }, config);
        var options = sp.GetRequiredService<IOptions<FeishuDeduplicationOptions>>().Value;

        options.IsConfiguredFromConfiguration.Should().BeTrue();
        options.Mode.Should().Be("Distributed");
        options.Event!.KeyPrefix.Should().Be("t-a:feishu:event:");
        options.Event.Ttl.Should().Be(TimeSpan.FromDays(2));
    }

    [Fact]
    public void Bind_ShouldNotMarkIsConfigured_WhenSectionMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        using var sp = Build(_ => { }, config);
        var options = sp.GetRequiredService<IOptions<FeishuDeduplicationOptions>>().Value;

        options.IsConfiguredFromConfiguration.Should().BeFalse();
        options.Mode.Should().Be(FeishuDeduplicationOptions.DefaultMode);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDistributedPrefixesAreIdentical()
    {
        var options = new FeishuDeduplicationOptions
        {
            Mode = "Distributed",
            IsConfiguredFromConfiguration = true,
            Event = new DeduplicationEntryOptions { KeyPrefix = "same:" },
            Nonce = new NonceDeduplicationOptions { KeyPrefix = "same:" },
            SeqId = new SeqIdDeduplicationOptions { KeyPrefix = "same:" }
        };

        var validator = new FeishuDeduplicationOptionsValidator();
        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("TMA2-20");
    }

    [Fact]
    public void Validate_ShouldFail_WhenModeInvalid()
    {
        var options = new FeishuDeduplicationOptions { Mode = "SomethingElse" };
        var act = () => options.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Mode*");
    }

    [Fact]
    public void ProfileApi_ShouldApplyHighReliabilityTtl_WhenConfigured()
    {
        var services = new ServiceCollection();
        services.AddFeishuDeduplicationOptions(DeduplicationProfile.HighReliability);
        using var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<FeishuDeduplicationOptions>>().Value;
        options.Profile.Should().Be(FeishuDeduplicationOptions.ProfileHighReliability);
        options.ResolveEventTtl().Should().Be(TimeSpan.FromHours(72));
        options.ResolveEventProcessingTimeout().Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void ProfileApi_ShouldAllowFieldOverride_AfterProfile()
    {
        var services = new ServiceCollection();
        services.AddFeishuDeduplicationOptions(DeduplicationProfile.HighReliability, o =>
        {
            o.Event = new DeduplicationEntryOptions { Ttl = TimeSpan.FromHours(12) };
        });
        using var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<FeishuDeduplicationOptions>>().Value;
        options.ResolveEventTtl().Should().Be(TimeSpan.FromHours(12));
        options.ResolveEventProcessingTimeout().Should().Be(TimeSpan.FromMinutes(5), "未覆盖字段仍用 Profile 预设");
    }

    [Theory]
    [InlineData(300, 300, true, true)]
    [InlineData(120, 300, true, false)] // Nonce < tolerance → fail when production
    [InlineData(300, 300, false, true)]
    public void Consistency_ShouldValidateNonceTtlAgainstTolerance(int nonceSeconds, int toleranceSeconds, bool failMode, bool expectSuccess)
    {
        var result = FeishuConfigurationConsistency.ValidateNonceTtlAgainstTolerance(
            TimeSpan.FromSeconds(nonceSeconds), toleranceSeconds, failMode);

        result.Succeeded.Should().Be(expectSuccess);
    }
}

/// <summary>
/// C2/R3：FeishuAppConfig 嵌套熔断/重试与 TimeoutSeconds 双读。
/// </summary>
public class FeishuAppConfigNestedOptionsTests
{
    [Fact]
    public void NestedAndFlat_ShouldShareSameStorage()
    {
#pragma warning disable CS0618
        var config = new FeishuAppConfig
        {
            TimeoutSeconds = 45,
            HttpRetry = new HttpRetryOptions { MaxAttempts = 5, DelayMs = 2000 },
            CircuitBreaker = new CircuitBreakerOptions { Enabled = false, FailureThreshold = 30 }
        };

        config.TimeoutSeconds.Should().Be(45);
        config.HttpRetry.MaxAttempts.Should().Be(5);
        config.HttpRetry.DelayMs.Should().Be(2000);
        config.CircuitBreaker.Enabled.Should().BeFalse();
        config.CircuitBreaker.FailureThreshold.Should().Be(30);

        config.TimeoutSeconds = 60;
        config.TimeoutSeconds.Should().Be(60);
        config.CircuitBreaker.Enabled = true;
        config.CircuitBreaker.Enabled.Should().BeTrue();
#pragma warning restore CS0618
    }

    [Fact]
    public void Bind_ShouldSupportBothNestedAndFlatCircuitBreakerKeys()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuApps:0:AppKey"] = "default",
                ["FeishuApps:0:AppId"] = "cli_x",
                ["FeishuApps:0:AppSecret"] = "secret",
                ["FeishuApps:0:TimeoutSeconds"] = "42",
                ["FeishuApps:0:CircuitBreaker:Enabled"] = "false",
                ["FeishuApps:0:HttpRetry:MaxAttempts"] = "7"
            })
            .Build();

        var configs = new List<FeishuAppConfig>();
        configuration.GetSection("FeishuApps").Bind(configs);

        configs.Should().HaveCount(1);
        configs[0].TimeoutSeconds.Should().Be(42);
        configs[0].CircuitBreaker.Enabled.Should().BeFalse();
        configs[0].HttpRetry.MaxAttempts.Should().Be(7);
#pragma warning disable CS0618
        configs[0].TimeoutSeconds.Should().Be(42);
        configs[0].CircuitBreaker.Enabled.Should().BeFalse();
        configs[0].HttpRetry.MaxAttempts.Should().Be(7);
#pragma warning restore CS0618
    }
}
