// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mud.Feishu.WebSocket.Tests.Configuration;

/// <summary>
/// FeishuWebSocketOptions 配置绑定测试。
/// 验证 JSON/内存配置 → 对象的绑定路径，覆盖标量、嵌套对象、枚举、TimeSpan 等绑定场景。
/// 对应生产代码：FeishuWebSocketServiceBuilder.cs 中 configuration.GetSection(section).Bind(options) 调用。
/// </summary>
public class FeishuWebSocketOptionsBindingTests
{
    /// <summary>
    /// 构造内存配置源并绑定到 FeishuWebSocketOptions。
    /// </summary>
    private static FeishuWebSocketOptions BindFromDictionary(Dictionary<string, string?> configData)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var options = new FeishuWebSocketOptions();
        configuration.GetSection("FeishuWebSocket").Bind(options);
        return options;
    }

    [Fact]
    public void Bind_ShouldMapScalarBooleanFields_WhenJsonContainsBooleanValues()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:AutoReconnect"] = "false",
            ["FeishuWebSocket:EnableLogging"] = "false",
            ["FeishuWebSocket:EnableReconnectMetrics"] = "false",
            ["FeishuWebSocket:AllowInsecureWebSocket"] = "true",
            ["FeishuWebSocket:ValidateServerCertificate"] = "false",
            ["FeishuWebSocket:AllowSelfSignedCertificates"] = "true",
        });

        options.AutoReconnect.Should().BeFalse();
        options.EnableLogging.Should().BeFalse();
        options.EnableReconnectMetrics.Should().BeFalse();
        options.AllowInsecureWebSocket.Should().BeTrue();
        options.ValidateServerCertificate.Should().BeFalse();
        options.AllowSelfSignedCertificates.Should().BeTrue();
    }

    [Fact]
    public void Bind_ShouldMapScalarIntegerFields_WhenJsonContainsIntegerValues()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:MaxReconnectAttempts"] = "10",
            ["FeishuWebSocket:ReconnectDelayMs"] = "2000",
            ["FeishuWebSocket:MaxReconnectDelayMs"] = "60000",
            ["FeishuWebSocket:InitialReceiveBufferSize"] = "8192",
            ["FeishuWebSocket:HeartbeatIntervalMs"] = "15000",
            ["FeishuWebSocket:ConnectionTimeoutMs"] = "20000",
            ["FeishuWebSocket:HealthCheckIntervalMs"] = "30000",
            ["FeishuWebSocket:MessageHandlerTimeoutMs"] = "45000",
        });

        options.MaxReconnectAttempts.Should().Be(10);
        options.ReconnectDelayMs.Should().Be(2000);
        options.MaxReconnectDelayMs.Should().Be(60000);
        options.InitialReceiveBufferSize.Should().Be(8192);
        options.HeartbeatIntervalMs.Should().Be(15000);
        options.ConnectionTimeoutMs.Should().Be(20000);
        options.HealthCheckIntervalMs.Should().Be(30000);
        options.MessageHandlerTimeoutMs.Should().Be(45000);
    }

    [Fact]
    public void Bind_ShouldMapTimeSpanFields_WhenJsonContainsTimeSpanString()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:MaxTotalReconnectTime"] = "01:30:00",
            ["FeishuWebSocket:ReconnectCooldownTime"] = "00:00:10",
        });

        options.MaxTotalReconnectTime.Should().Be(TimeSpan.FromHours(1.5));
        options.ReconnectCooldownTime.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Bind_ShouldMapUnsignedLongField_WhenJsonContainsNonNegativeInteger()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:SequenceGapThreshold"] = "5000",
        });

        options.SequenceGapThreshold.Should().Be(5000UL);
    }

    [Fact]
    public void Bind_ShouldMapNestedMessageSizeLimits_WhenJsonContainsNestedObject()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:MessageSizeLimits:MaxTextMessageSize"] = "2048",
            ["FeishuWebSocket:MessageSizeLimits:MaxBinaryMessageSize"] = "5242880",
        });

        options.MessageSizeLimits.Should().NotBeNull();
        options.MessageSizeLimits.MaxTextMessageSize.Should().Be(2048);
        options.MessageSizeLimits.MaxBinaryMessageSize.Should().Be(5242880L);
    }

    [Fact]
    public void Bind_ShouldMapEventDeduplicationMode_WhenJsonContainsEnumString()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:EventDeduplication:Mode"] = "Distributed",
        });

        options.EventDeduplication.Mode.Should().Be(EventDeduplicationMode.Distributed);
    }

    [Fact]
    public void Bind_ShouldMapEventDeduplicationMode_WhenJsonContainsInMemoryEnumString()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:EventDeduplication:Mode"] = "InMemory",
        });

        options.EventDeduplication.Mode.Should().Be(EventDeduplicationMode.InMemory);
    }

    [Fact]
    public void Bind_ShouldMapEventDeduplicationTimeSpans_WhenJsonContainsTimeSpanStrings()
    {
        // 注意：ConfigurationBinder 对 TimeSpan 的解析使用 TypeDescriptor，
        // "24:00:00" 会被解释为 24 天而非 24 小时。需使用 "d.hh:mm:ss" 格式明确天数。
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:EventDeduplication:CacheExpiration"] = "1.00:00:00",
            ["FeishuWebSocket:EventDeduplication:CleanupInterval"] = "00:10:00",
            ["FeishuWebSocket:EventDeduplication:ProcessingTimeout"] = "00:15:00",
        });

        options.EventDeduplication.CacheExpiration.Should().Be(TimeSpan.FromDays(1));
        options.EventDeduplication.CleanupInterval.Should().Be(TimeSpan.FromMinutes(10));
        options.EventDeduplication.ProcessingTimeout.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void Bind_ShouldMapEventDeduplicationMaxCacheSize_WhenJsonContainsInteger()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:EventDeduplication:MaxCacheSize"] = "50000",
        });

        options.EventDeduplication.MaxCacheSize.Should().Be(50000);
    }

    [Fact]
    public void Bind_ShouldPreserveDefaultValues_WhenSectionIsEmpty()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Other:Key"] = "value",
            })
            .Build();

        var options = new FeishuWebSocketOptions();
        configuration.GetSection("FeishuWebSocket").Bind(options);

        options.AutoReconnect.Should().BeTrue();
        options.MaxReconnectAttempts.Should().Be(5);
        options.HeartbeatIntervalMs.Should().Be(25000);
        options.ConnectionTimeoutMs.Should().Be(10000);
        options.EventDeduplication.Mode.Should().Be(EventDeduplicationMode.InMemory);
        options.MessageSizeLimits.MaxTextMessageSize.Should().Be(1024 * 1024);
        options.MessageSizeLimits.MaxBinaryMessageSize.Should().Be(10L * 1024 * 1024);
    }

    [Fact]
    public void Bind_ShouldEnforceMinimumValueClamping_WhenJsonContainsBelowMinimumValues()
    {
        // ReconnectDelayMs 最小 1000，HeartbeatIntervalMs 最小 5000
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:ReconnectDelayMs"] = "500",
            ["FeishuWebSocket:HeartbeatIntervalMs"] = "1000",
        });

        // setter 中 Math.Max 应将值提升到最小值
        options.ReconnectDelayMs.Should().Be(1000);
        options.HeartbeatIntervalMs.Should().Be(5000);
    }

    [Fact]
    public void ServiceProvider_ShouldResolveIOptionsWithBoundValues_WhenConfiguredViaConfigure()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuWebSocket:AutoReconnect"] = "false",
                ["FeishuWebSocket:MaxReconnectAttempts"] = "8",
                ["FeishuWebSocket:HeartbeatIntervalMs"] = "20000",
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(configuration.GetSection("FeishuWebSocket"));
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<FeishuWebSocketOptions>>().Value;

        options.AutoReconnect.Should().BeFalse();
        options.MaxReconnectAttempts.Should().Be(8);
        options.HeartbeatIntervalMs.Should().Be(20000);
    }

    [Fact]
    public void ServiceProvider_ShouldResolveIOptionsMonitorWithBoundValues_WhenConfiguredViaConfigure()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuWebSocket:MaxReconnectAttempts"] = "12",
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<FeishuWebSocketOptions>(configuration.GetSection("FeishuWebSocket"));
        using var provider = services.BuildServiceProvider();

        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<FeishuWebSocketOptions>>();
        var options = optionsMonitor.CurrentValue;

        options.MaxReconnectAttempts.Should().Be(12);
    }

    [Fact]
    public void Validate_ShouldNotThrow_AfterBindingValidConfiguration()
    {
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:AutoReconnect"] = "true",
            ["FeishuWebSocket:MaxReconnectAttempts"] = "5",
            ["FeishuWebSocket:ReconnectDelayMs"] = "5000",
            ["FeishuWebSocket:MaxReconnectDelayMs"] = "30000",
            ["FeishuWebSocket:HeartbeatIntervalMs"] = "25000",
            ["FeishuWebSocket:ConnectionTimeoutMs"] = "10000",
            ["FeishuWebSocket:InitialReceiveBufferSize"] = "4096",
            ["FeishuWebSocket:MessageSizeLimits:MaxTextMessageSize"] = "1048576",
            ["FeishuWebSocket:MessageSizeLimits:MaxBinaryMessageSize"] = "10485760",
            ["FeishuWebSocket:EventDeduplication:Mode"] = "InMemory",
            ["FeishuWebSocket:EventDeduplication:CacheExpiration"] = "2.00:00:00",
            ["FeishuWebSocket:EventDeduplication:CleanupInterval"] = "00:05:00",
        });

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_AfterBindingInvalidConfiguration_WhenConnectionTimeoutBelowMinimum()
    {
        // ConnectionTimeoutMs 无 setter clamp，Validate 会检测 < 1000 并抛异常
        var options = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuWebSocket:ConnectionTimeoutMs"] = "500",
        });

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*ConnectionTimeoutMs*");
    }
}
