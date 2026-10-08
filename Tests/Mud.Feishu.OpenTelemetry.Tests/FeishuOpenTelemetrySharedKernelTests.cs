// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.Abstractions.Observability;
using Mud.HttpUtils.OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Mud.Feishu.OpenTelemetry.Tests;

/// <summary>
/// 共享装配内核（<c>Mud.HttpUtils.OpenTelemetry</c>）迁移后的产品侧契约锁定。
/// </summary>
/// <remarks>
/// 覆盖四类不可回归的契约：① 产品贡献描述（单根源 + 精确 Meter + MudHttp 源开关）；
/// ② 选项映射的**逐属性完整性**；③ 注册期真校验（修复此前「非法配置静默通过」）；
/// ④ 单一入口禁令（内核 fail-fast）与同产品幂等。
/// </remarks>
public class FeishuOpenTelemetrySharedKernelTests
{
    // ============================================================
    // ① 产品贡献描述
    // ============================================================

    [Fact]
    public void CreateContribution_ShouldDeclareFeishuProductContract()
    {
        var contribution = FeishuOpenTelemetryExtensions.CreateContribution(new FeishuOpenTelemetryOptions());

        contribution.ProductName.Should().Be("Mud.Feishu");
        contribution.ActivitySourceName.Should().Be(FeishuActivitySource.Name);
        contribution.ActivitySourceName.Should().Be("Mud.Feishu");
        // 飞书为单产品线：用精确 Meter（与根源同名），不用通配。
        contribution.MeterName.Should().Be(FeishuMetrics.MeterName);
        contribution.MeterName.Should().Be("Mud.Feishu");
        contribution.MeterWildcard.Should().BeNull("飞书为单产品线，精确 Meter 即可，无需通配");
        contribution.DefaultServiceName.Should().Be("Mud.Feishu.Application");
        contribution.DefaultServiceVersion.Should().Be(FeishuActivitySource.Version);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateContribution_ShouldMirrorIncludeMudHttpUtils(bool includeMudHttpUtils)
    {
        var options = new FeishuOpenTelemetryOptions { IncludeMudHttpUtils = includeMudHttpUtils };

        var contribution = FeishuOpenTelemetryExtensions.CreateContribution(options);

        contribution.IncludeMudHttpSources.Should().Be(includeMudHttpUtils,
            "IncludeMudHttpUtils 必须原样落到内核的 IncludeMudHttpSources（源/Meter 由内核去重注册）");
    }

    // ============================================================
    // ② 选项映射的逐属性完整性
    // ============================================================

    [Fact]
    public void Mapper_ShouldMapEveryPublicOptionProperty()
    {
        var headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token" };
        Action<TracerProviderBuilder> tracing = _ => { };
        Action<MeterProviderBuilder> metrics = _ => { };
        Action<LoggerProviderBuilder> logging = _ => { };

        var options = new FeishuOpenTelemetryOptions
        {
            EnableTracing = false,
            EnableMetrics = false,
            EnableLogging = true,
            IncludeMudHttpUtils = false,
            EnableHttpClientInstrumentation = false,
            EnableAspNetCoreInstrumentation = false,
            OtlpEndpoint = new Uri("http://otel-collector:4317"),
            OtlpExportProtocol = Mud.HttpUtils.OpenTelemetry.OtlpExportProtocol.HttpProtobuf,
            OtlpHeaders = headers,
            UseShortExporterTimeout = true,
            ExportBatchSize = 256,
            ExportIntervalMilliseconds = 3000,
            ServiceName = "my-feishu-app",
            ServiceVersion = "9.9.9",
            DeploymentEnvironment = "staging",
            SamplingRatio = 0.25,
            ConfigureTracing = tracing,
            ConfigureMetrics = metrics,
            ConfigureLogging = logging,
        };

        var core = FeishuOpenTelemetryOptionsMapper.ToCore(options);

        // 每个公开可写属性都必须被映射（漏映射 = 配置静默失效）。
        core.EnableTracing.Should().BeFalse();
        core.EnableMetrics.Should().BeFalse();
        core.EnableLogging.Should().BeTrue();
        core.EnableHttpClientInstrumentation.Should().BeFalse();
        core.EnableAspNetCoreInstrumentation.Should().BeFalse();
        core.OtlpEndpoint.Should().Be(new Uri("http://otel-collector:4317"));
        core.OtlpExportProtocol.Should().Be(Mud.HttpUtils.OpenTelemetry.OtlpExportProtocol.HttpProtobuf);
        core.OtlpHeaders.Should().BeSameAs(headers);
        core.UseShortExporterTimeout.Should().BeTrue();
        core.ExportBatchSize.Should().Be(256);
        core.ExportIntervalMilliseconds.Should().Be(3000);
        core.ServiceName.Should().Be("my-feishu-app");
        core.ServiceVersion.Should().Be("9.9.9");
        core.DeploymentEnvironment.Should().Be("staging");
        core.SamplingRatio.Should().Be(0.25);
        core.ConfigureTracing.Should().BeSameAs(tracing);
        core.ConfigureMetrics.Should().BeSameAs(metrics);
        core.ConfigureLogging.Should().BeSameAs(logging);
    }

    [Fact]
    public void Options_ShouldExposeOtlpEnhancedSurface_WithUpstreamDefaults()
    {
        var options = new FeishuOpenTelemetryOptions();

        options.OtlpExportProtocol.Should().Be(Mud.HttpUtils.OpenTelemetry.OtlpExportProtocol.Grpc);
        options.OtlpHeaders.Should().BeNull();
        options.UseShortExporterTimeout.Should().BeFalse();
        options.ExportBatchSize.Should().BeNull();
        options.ExportIntervalMilliseconds.Should().BeNull();
    }

    // ============================================================
    // ③ 注册期真校验（修复：此前 IValidateOptions 管道永不触发）
    // ============================================================

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldThrowOptionsValidationException_AtRegistration_WhenServiceNameEmpty()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(o => o.ServiceName = "  ");

        act.Should().Throw<OptionsValidationException>()
            .Where(e => e.OptionsType == typeof(FeishuOpenTelemetryOptions))
            .Where(e => e.Message.Contains("ServiceName"));
    }

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldThrowOptionsValidationException_AtRegistration_WhenOtlpEndpointIsRelative()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(
            o => o.OtlpEndpoint = new Uri("/relative/path", UriKind.Relative));

        act.Should().Throw<OptionsValidationException>()
            .Where(e => e.Message.Contains("OtlpEndpoint"));
    }

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldThrowOptionsValidationException_AtRegistration_WhenExportIntervalNegative()
    {
        var services = new ServiceCollection();

        var act = () => services.AddFeishuOpenTelemetry(o => o.ExportIntervalMilliseconds = -1);

        act.Should().Throw<OptionsValidationException>()
            .Where(e => e.Message.Contains("ExportIntervalMilliseconds"));
    }

    // ============================================================
    // ④ 单一入口禁令（内核 fail-fast）+ 幂等
    // ============================================================

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldThrow_WhenCalledTogetherWithMudHttpBootstrap()
    {
        var services = new ServiceCollection();
        services.AddFeishuOpenTelemetry();

        var act = () => services.AddMudHttpOpenTelemetry();

        act.Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("Mud.Feishu") && e.Message.Contains("Mud.HttpUtils"));
    }

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldBeIdempotent_WhenCalledTwiceWithSameProduct()
    {
        var services = new ServiceCollection();
        services.AddFeishuOpenTelemetry();

        var act = () => services.AddFeishuOpenTelemetry();

        act.Should().NotThrow("同一产品重复注册由内核幂等短路（不再重复装配，避免 Span/指标翻倍）");
    }

    // ============================================================
    // ⑤ 既有 DI 面保留（宿主可解析，勿回归删除）
    // ============================================================

    [Fact]
    public void AddFeishuOpenTelemetry_ShouldKeepOptionsPipeline_AfterKernelMigration()
    {
        var services = new ServiceCollection();
        services.AddFeishuOpenTelemetry(o => o.ServiceName = "my-app");
        using var provider = services.BuildServiceProvider();

        provider.GetService<IValidateOptions<FeishuOpenTelemetryOptions>>().Should().NotBeNull();
        provider.GetRequiredService<IOptions<FeishuOpenTelemetryOptions>>().Value.ServiceName.Should().Be("my-app");
    }
}
