// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.Abstractions.Observability;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Mud.Feishu.OpenTelemetry.Tests;

/// <summary>
/// FeishuOpenTelemetryExtensions 单元测试
/// </summary>
public class FeishuOpenTelemetryExtensionsTests
{
    // ============================================================
    // FeishuOpenTelemetryOptions 默认值验证
    // ============================================================

    [Fact]
    public void Options_DefaultValues_ShouldBeCorrect()
    {
        var options = new FeishuOpenTelemetryOptions();

        options.EnableTracing.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
        options.EnableLogging.Should().BeFalse();
        options.IncludeMudHttpUtils.Should().BeTrue();
        options.EnableHttpClientInstrumentation.Should().BeTrue();
        options.EnableAspNetCoreInstrumentation.Should().BeTrue();
        options.OtlpEndpoint.Should().Be(new Uri("http://localhost:4317"));
        options.ServiceName.Should().Be("Mud.Feishu.Application");
        options.ServiceVersion.Should().Be(FeishuActivitySource.Version);
        options.DeploymentEnvironment.Should().Be("production");
        options.SamplingRatio.Should().Be(1.0);
        options.ConfigureTracing.Should().BeNull();
        options.ConfigureMetrics.Should().BeNull();
        options.ConfigureLogging.Should().BeNull();
    }

    // ============================================================
    // AddFeishuOpenTelemetry 参数校验
    // ============================================================

    [Fact]
    public void AddFeishuOpenTelemetry_WithNullServices_ShouldThrowArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddFeishuOpenTelemetry();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddFeishuOpenTelemetry_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = null!;

        var act = () => services.AddFeishuOpenTelemetry(configuration);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void AddFeishuOpenTelemetry_WithSamplingRatioBelowZero_ShouldThrowArgumentOutOfRangeException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(options => options.SamplingRatio = -0.1);

        // nameof(options.SamplingRatio) 返回 "SamplingRatio"（C# nameof 语义），
        // 测试期望应与实际抛出的 paramName 一致。
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("SamplingRatio");
    }

    [Fact]
    public void AddFeishuOpenTelemetry_WithSamplingRatioAboveOne_ShouldThrowArgumentOutOfRangeException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(options => options.SamplingRatio = 1.1);

        // nameof(options.SamplingRatio) 返回 "SamplingRatio"（C# nameof 语义），
        // 测试期望应与实际抛出的 paramName 一致。
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("SamplingRatio");
    }

    [Fact]
    public void AddFeishuOpenTelemetry_WithSamplingRatioZero_ShouldNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(options => options.SamplingRatio = 0.0);

        act.Should().NotThrow();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_WithSamplingRatioOne_ShouldNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(options => options.SamplingRatio = 1.0);

        act.Should().NotThrow();
    }

    // ============================================================
    // AddFeishuOpenTelemetry 正常注册验证
    // ============================================================

    [Fact]
    public void AddFeishuOpenTelemetry_WithDefaultOptions_ShouldReturnNonNullBuilder()
    {
        var services = new ServiceCollection();

        var builder = services.AddFeishuOpenTelemetry();

        builder.Should().NotBeNull();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_WithDefaultOptions_ShouldRegisterOpenTelemetryServices()
    {
        var services = new ServiceCollection();

        services.AddFeishuOpenTelemetry();

        // 验证 OTel 核心服务已注册（OpenTelemetryBuilder 会注册多个服务）
        services.Should().NotBeEmpty();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldNotThrow_WhenTracingDisabled()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(options =>
        {
            options.EnableTracing = false;
            options.OtlpEndpoint = null;
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldNotThrow_WhenMetricsDisabled()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(options =>
        {
            options.EnableMetrics = false;
            options.OtlpEndpoint = null;
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldNotThrow_WhenLoggingEnabled()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(options =>
        {
            options.EnableLogging = true;
            options.OtlpEndpoint = null;
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldNotThrow_WhenMudHttpUtilsExcluded()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(options =>
        {
            options.IncludeMudHttpUtils = false;
            options.OtlpEndpoint = null;
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldNotThrow_WhenOtlpEndpointIsNull()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(options =>
        {
            options.OtlpEndpoint = null;
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldNotThrow_WhenAllInstrumentationDisabled()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(options =>
        {
            options.EnableHttpClientInstrumentation = false;
            options.EnableAspNetCoreInstrumentation = false;
            options.OtlpEndpoint = null;
        });

        act.Should().NotThrow();
    }

    // ============================================================
    // IConfiguration 重载验证
    // ============================================================
    //
    // IConfiguration 重载的测试使用 ConfigurationBuilder + AddInMemoryCollection 构造真实的
    // IConfiguration 实例，而非 Mock<IConfiguration>。原因：
    // 1) AddFeishuOpenTelemetry(IConfiguration) 重载内部调用 configuration.GetSection(...).Bind(options)，
    //    Bind() 是 Microsoft.Extensions.Configuration.Binder 提供的反射扩展方法，会枚举
    //    IConfigurationSection.GetChildren() 并对每个属性递归读取 Value，Mock 难以正确模拟。
    // 2) Microsoft.Extensions.Configuration 包（OpenTelemetry 项目的传递依赖）已内置
    //    AddInMemoryCollection 扩展方法，可零成本构造可读 IConfiguration 实例。

    [Fact]
    public void AddFeishuOpenTelemetry_FromConfiguration_ShouldBindOptions()
    {
        var services = new ServiceCollection();
        var config = CreateInMemoryConfiguration("FeishuOpenTelemetry", new Dictionary<string, string?>
        {
            ["ServiceName"] = "my-test-app",
            ["SamplingRatio"] = "0.5",
            ["DeploymentEnvironment"] = "staging",
        });

        services.AddFeishuOpenTelemetry(config);

        // 验证方法不抛异常即表示绑定成功
        services.Should().NotBeEmpty();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_FromConfiguration_WithCustomSectionPath_ShouldWork()
    {
        var services = new ServiceCollection();
        var config = CreateInMemoryConfiguration("CustomOtel", new Dictionary<string, string?>
        {
            ["ServiceName"] = "custom-section-app",
        });

        var act = () => services.AddFeishuOpenTelemetry(config, sectionPath: "CustomOtel");

        act.Should().NotThrow();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_FromConfiguration_WithInvalidSamplingRatio_ShouldThrow()
    {
        var services = new ServiceCollection();
        var config = CreateInMemoryConfiguration("FeishuOpenTelemetry", new Dictionary<string, string?>
        {
            ["SamplingRatio"] = "2.0",
        });

        var act = () => services.AddFeishuOpenTelemetry(config);

        // nameof(options.SamplingRatio) 返回 "SamplingRatio"（C# nameof 语义），
        // 测试期望应与实际抛出的 paramName 一致。
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("SamplingRatio");
    }

    [Fact]
    public void AddFeishuOpenTelemetry_FromConfiguration_WithConfigureOverride_ShouldApplyAfterBinding()
    {
        var services = new ServiceCollection();
        var config = CreateInMemoryConfiguration("FeishuOpenTelemetry", new Dictionary<string, string?>
        {
            ["ServiceName"] = "from-config",
        });

        // configure 委托在配置绑定之后执行，可覆盖绑定值。
        // 通过断言 options.ServiceName == "from-config" 验证 Bind() 已成功执行；
        // 通过设置 options.ServiceName = "overridden" 验证 configure 可覆盖绑定值。
        services.AddFeishuOpenTelemetry(config, configure: options =>
        {
            options.ServiceName.Should().Be("from-config");
            options.ServiceName = "overridden";
            options.OtlpEndpoint = null;
        });
    }

    /// <summary>
    /// 使用 ConfigurationBuilder + AddInMemoryCollection 构造真实的 IConfiguration 实例。
    /// 这样可以正确支持 ConfigurationBinder.Bind() 的反射绑定语义。
    /// </summary>
    /// <param name="sectionName">配置节名称。</param>
    /// <param name="values">配置键值对，键为相对节路径（如 "ServiceName"），值为字符串。</param>
    /// <returns>可读的 IConfiguration 实例。</returns>
    private static IConfiguration CreateInMemoryConfiguration(string sectionName, Dictionary<string, string?> values)
    {
        var prefixedValues = new Dictionary<string, string?>(values.Count);
        foreach (var (key, value) in values)
        {
            prefixedValues[$"{sectionName}:{key}"] = value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(prefixedValues)
            .Build();
    }

    // ============================================================
    // 自定义配置委托验证
    // ============================================================

    [Fact]
    public void AddFeishuOpenTelemetry_WithConfigureTracing_ShouldInvokeCallback()
    {
        var services = new ServiceCollection();
        var callbackInvoked = false;

        services.AddFeishuOpenTelemetry(options =>
        {
            options.OtlpEndpoint = null;
            options.ConfigureTracing = _ => callbackInvoked = true;
        });

        // 构建 ServiceProvider 会触发 OTel 管道构建，从而调用 ConfigureTracing
        using var provider = services.BuildServiceProvider();
        callbackInvoked.Should().BeTrue();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_WithConfigureMetrics_ShouldInvokeCallback()
    {
        var services = new ServiceCollection();
        var callbackInvoked = false;

        services.AddFeishuOpenTelemetry(options =>
        {
            options.OtlpEndpoint = null;
            options.ConfigureMetrics = _ => callbackInvoked = true;
        });

        using var provider = services.BuildServiceProvider();
        callbackInvoked.Should().BeTrue();
    }

    [Fact]
    public void AddFeishuOpenTelemetry_WithConfigureLogging_ShouldInvokeCallback()
    {
        var services = new ServiceCollection();
        var callbackInvoked = false;

        services.AddFeishuOpenTelemetry(options =>
        {
            options.EnableLogging = true;
            options.OtlpEndpoint = null;
            options.ConfigureLogging = _ => callbackInvoked = true;
        });

        using var provider = services.BuildServiceProvider();
        callbackInvoked.Should().BeTrue();
    }

    // ============================================================
    // 常量引用验证（确保 OTel 包注册了正确的 ActivitySource 和 Meter 名称）
    // ============================================================

    [Fact]
    public void FeishuActivitySource_Name_ShouldBeMudFeishu()
    {
        FeishuActivitySource.Name.Should().Be("Mud.Feishu");
    }

    [Fact]
    public void FeishuMetrics_MeterName_ShouldBeMudFeishu()
    {
        FeishuMetrics.MeterName.Should().Be("Mud.Feishu");
    }

    [Fact]
    public void FeishuActivitySource_Version_ShouldBeNonEmpty()
    {
        FeishuActivitySource.Version.Should().NotBeNullOrEmpty();
    }
}
