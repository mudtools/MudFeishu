// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Mud.Feishu.Abstractions.Tests.Configuration;

/// <summary>
/// FeishuAppConfig 配置绑定测试。
/// 验证 JSON/内存配置 → List&lt;FeishuAppConfig&gt; 的绑定路径，覆盖标量、嵌套对象、布尔值等绑定场景。
/// 对应生产代码：FeishuMultiAppExtensions.AddFeishuApp(IConfiguration) 中 section.Bind(configs) 调用。
/// </summary>
public class FeishuAppConfigBindingTests
{
    /// <summary>
    /// 构造内存配置源并绑定到 List&lt;FeishuAppConfig&gt;。
    /// </summary>
    private static List<FeishuAppConfig> BindFromDictionary(Dictionary<string, string?> configData)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var configs = new List<FeishuAppConfig>();
        configuration.GetSection("FeishuApps").Bind(configs);
        return configs;
    }

    [Fact]
    public void Bind_ShouldMapScalarStringFields_WhenJsonContainsStringValues()
    {
        var configs = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuApps:0:AppKey"] = "default",
            ["FeishuApps:0:AppId"] = "cli_a1b2c3d4e5f6g7h8i9j0k",
            ["FeishuApps:0:AppSecret"] = "dsk_secret_key_1234567890",
            ["FeishuApps:0:BaseUrl"] = "https://open.feishu.cn",
        });

        configs.Should().HaveCount(1);
        configs[0].AppKey.Should().Be("default");
        configs[0].AppId.Should().Be("cli_a1b2c3d4e5f6g7h8i9j0k");
        configs[0].AppSecret.Should().Be("dsk_secret_key_1234567890");
        configs[0].BaseUrl.Should().Be("https://open.feishu.cn");
    }

    [Fact]
    public void Bind_ShouldMapIntegerFields_WhenJsonContainsIntegerValues()
    {
        var configs = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuApps:0:AppKey"] = "test",
            ["FeishuApps:0:AppId"] = "cli_a1b2c3d4e5f6g7h8i9j0k",
            ["FeishuApps:0:AppSecret"] = "dsk_secret_key_1234567890",
            ["FeishuApps:0:TimeOut"] = "60",
            ["FeishuApps:0:RetryCount"] = "5",
            ["FeishuApps:0:RetryDelayMs"] = "2000",
            ["FeishuApps:0:TokenRefreshThreshold"] = "600",
        });

        configs[0].TimeOut.Should().Be(60);
        configs[0].RetryCount.Should().Be(5);
        configs[0].RetryDelayMs.Should().Be(2000);
        configs[0].TokenRefreshThreshold.Should().Be(600);
    }

    [Fact]
    public void Bind_ShouldMapBooleanFields_WhenJsonContainsBooleanValues()
    {
        var configs = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuApps:0:AppKey"] = "test",
            ["FeishuApps:0:AppId"] = "cli_a1b2c3d4e5f6g7h8i9j0k",
            ["FeishuApps:0:AppSecret"] = "dsk_secret_key_1234567890",
            ["FeishuApps:0:EnableLogging"] = "false",
            ["FeishuApps:0:IsDefault"] = "true",
            ["FeishuApps:0:AllowCustomBaseUrl"] = "true",
        });

        configs[0].EnableLogging.Should().BeFalse();
        configs[0].IsDefault.Should().BeTrue();
        configs[0].AllowCustomBaseUrl.Should().BeTrue();
    }

    [Fact]
    public void Bind_ShouldMapCircuitBreakerFields_WhenJsonContainsCircuitBreakerConfig()
    {
        var configs = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuApps:0:AppKey"] = "test",
            ["FeishuApps:0:AppId"] = "cli_a1b2c3d4e5f6g7h8i9j0k",
            ["FeishuApps:0:AppSecret"] = "dsk_secret_key_1234567890",
            ["FeishuApps:0:CircuitBreakerEnabled"] = "true",
            ["FeishuApps:0:CircuitBreakerFailureThreshold"] = "50",
            ["FeishuApps:0:CircuitBreakerSamplingDurationSeconds"] = "120",
            ["FeishuApps:0:CircuitBreakerBreakDurationSeconds"] = "30",
            ["FeishuApps:0:CircuitBreakerMinimumThroughput"] = "20",
        });

        configs[0].CircuitBreakerEnabled.Should().BeTrue();
        configs[0].CircuitBreakerFailureThreshold.Should().Be(50);
        configs[0].CircuitBreakerSamplingDurationSeconds.Should().Be(120);
        configs[0].CircuitBreakerBreakDurationSeconds.Should().Be(30);
        configs[0].CircuitBreakerMinimumThroughput.Should().Be(20);
    }

    [Fact]
    public void Bind_ShouldRetainDefaultValues_WhenConfigSectionIsMissing()
    {
        var configs = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuApps:0:AppKey"] = "test",
            ["FeishuApps:0:AppId"] = "cli_a1b2c3d4e5f6g7h8i9j0k",
            ["FeishuApps:0:AppSecret"] = "dsk_secret_key_1234567890",
        });

        // 默认值应被保留
        configs[0].TimeOut.Should().Be(30);
        configs[0].RetryCount.Should().Be(3);
        configs[0].RetryDelayMs.Should().Be(1000);
        configs[0].EnableLogging.Should().BeTrue();
        configs[0].IsDefault.Should().BeFalse();
        configs[0].CircuitBreakerEnabled.Should().BeTrue();
        configs[0].CircuitBreakerFailureThreshold.Should().Be(20);
        configs[0].TokenRefreshThreshold.Should().Be(300);
        configs[0].BaseUrl.Should().Be("https://open.feishu.cn");
    }

    [Fact]
    public void Bind_ShouldMapMultipleApps_WhenJsonContainsMultipleAppEntries()
    {
        var configs = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuApps:0:AppKey"] = "default",
            ["FeishuApps:0:AppId"] = "cli_a1b2c3d4e5f6g7h8i9j0k",
            ["FeishuApps:0:AppSecret"] = "dsk_secret_key_1234567890",
            ["FeishuApps:0:IsDefault"] = "true",
            ["FeishuApps:1:AppKey"] = "hr-app",
            ["FeishuApps:1:AppId"] = "cli_b1b2c3d4e5f6g7h8i9j0k",
            ["FeishuApps:1:AppSecret"] = "dsk_hr_secret_key_1234567",
        });

        configs.Should().HaveCount(2);
        configs[0].AppKey.Should().Be("default");
        configs[0].IsDefault.Should().BeTrue();
        configs[1].AppKey.Should().Be("hr-app");
        configs[1].IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Bind_ShouldReturnEmptyList_WhenConfigSectionIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var configs = new List<FeishuAppConfig>();
        configuration.GetSection("NonExistentSection").Bind(configs);

        configs.Should().BeEmpty();
    }

    [Fact]
    public void Bind_ShouldUseDefaultValues_WhenFieldsAreNotProvided()
    {
        var configs = BindFromDictionary(new Dictionary<string, string?>
        {
            ["FeishuApps:0:AppKey"] = "test",
            ["FeishuApps:0:AppId"] = "cli_a1b2c3d4e5f6g7h8i9j0k",
            ["FeishuApps:0:AppSecret"] = "dsk_secret_key_1234567890",
        });

        // 熔断器默认开启
        configs[0].CircuitBreakerEnabled.Should().BeTrue();
        configs[0].CircuitBreakerFailureThreshold.Should().Be(20);
        configs[0].CircuitBreakerSamplingDurationSeconds.Should().Be(60);
        configs[0].CircuitBreakerBreakDurationSeconds.Should().Be(60);
        configs[0].CircuitBreakerMinimumThroughput.Should().Be(10);
    }
}
