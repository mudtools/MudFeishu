// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.Abstractions.Observability;
using Mud.HttpUtils;

namespace Mud.Feishu.OpenTelemetry;

/// <summary>
/// Mud.Feishu OpenTelemetry 适配包的 DI 扩展方法。
/// </summary>
/// <remarks>
/// <para>通过 <see cref="AddFeishuOpenTelemetry(IServiceCollection, Action{FeishuOpenTelemetryOptions}?)"/> 一键启用飞书 SDK 的分布式追踪与指标采集。</para>
/// <para>自动注册以下 ActivitySource 和 Meter：</para>
/// <list type="bullet">
/// <item><c>Mud.Feishu</c> — 飞书事件处理、Webhook、WebSocket 追踪与指标</item>
/// <item><c>Mud.HttpUtils.HttpClient</c> — HTTP 出站请求追踪、Token 刷新、重试、熔断器指标（可选，默认开启）</item>
/// </list>
/// <para>默认导出至本地 OTLP gRPC 端点（<c>http://localhost:4317</c>），通过 <see cref="FeishuOpenTelemetryOptions.OtlpEndpoint"/> 自定义。</para>
/// </remarks>
public static class FeishuOpenTelemetryExtensions
{
    /// <summary>
    /// 一键开启 Mud.Feishu 的 OpenTelemetry 追踪与指标采集。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configure">可选的配置委托。</param>
    /// <returns>返回 <see cref="OpenTelemetryBuilder"/>，便于调用方继续追加配置。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 为 null。</exception>
    /// <example>
    /// <code>
    /// builder.Services.AddFeishuOpenTelemetry(options =>
    /// {
    ///     options.OtlpEndpoint = new Uri("http://otel-collector:4317");
    ///     options.ServiceName = "my-feishu-app";
    ///     options.SamplingRatio = 0.1;
    /// });
    /// </code>
    /// </example>
    public static OpenTelemetryBuilder AddFeishuOpenTelemetry(
        this IServiceCollection services,
        Action<FeishuOpenTelemetryOptions>? configure = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        var options = new FeishuOpenTelemetryOptions();
        configure?.Invoke(options);

        return AddFeishuOpenTelemetryCore(services, options);
    }

    /// <summary>
    /// 一键开启 Mud.Feishu 的 OpenTelemetry 追踪与指标采集，从 <see cref="IConfiguration"/> 绑定选项。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">配置实例，用于绑定 <see cref="FeishuOpenTelemetryOptions"/>。</param>
    /// <param name="sectionPath">配置节点路径，默认 <c>"FeishuOpenTelemetry"</c>。</param>
    /// <param name="configure">可选的附加配置委托，在配置绑定之后执行，可覆盖绑定值。</param>
    /// <returns>返回 <see cref="OpenTelemetryBuilder"/>，便于调用方继续追加配置。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 或 <paramref name="configuration"/> 为 null。</exception>
    /// <example>
    /// appsettings.json：
    /// <code>
    /// {
    ///   "FeishuOpenTelemetry": {
    ///     "ServiceName": "my-feishu-app",
    ///     "SamplingRatio": 0.1,
    ///     "OtlpEndpoint": "http://otel-collector:4317"
    ///   }
    /// }
    /// </code>
    /// 代码：
    /// <code>
    /// builder.Services.AddFeishuOpenTelemetry(builder.Configuration);
    /// </code>
    /// </example>
    public static OpenTelemetryBuilder AddFeishuOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionPath = "FeishuOpenTelemetry",
        Action<FeishuOpenTelemetryOptions>? configure = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var options = new FeishuOpenTelemetryOptions();
        configuration.GetSection(sectionPath).Bind(options);
        configure?.Invoke(options);

        return AddFeishuOpenTelemetryCore(services, options);
    }

    private static OpenTelemetryBuilder AddFeishuOpenTelemetryCore(
        IServiceCollection services,
        FeishuOpenTelemetryOptions options)
    {
        // 校验 SamplingRatio 范围
        if (options.SamplingRatio < 0 || options.SamplingRatio > 1)
            throw new ArgumentOutOfRangeException(nameof(options.SamplingRatio),
                $"SamplingRatio 必须在 0.0~1.0 范围内，当前值为 {options.SamplingRatio}。");

        // 配置 Resource：service.name / service.version / deployment.environment
        var builder = services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(serviceName: options.ServiceName, serviceVersion: options.ServiceVersion)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", options.DeploymentEnvironment)
                }));

        if (options.EnableTracing)
        {
            builder.WithTracing(tp =>
            {
                // 采样器：ParentBased + TraceIdRatioBased
                tp.SetSampler(new ParentBasedSampler(
                    new TraceIdRatioBasedSampler(options.SamplingRatio)));

                // Feishu ActivitySource（事件处理、Webhook、WebSocket）
                tp.AddSource(FeishuActivitySource.Name);

                // Mud.HttpUtils ActivitySource（HTTP 出站请求 + Token 恢复）
                if (options.IncludeMudHttpUtils)
                    tp.AddSource(MudHttpActivitySource.Name);

                if (options.EnableHttpClientInstrumentation)
                    tp.AddHttpClientInstrumentation();

                if (options.EnableAspNetCoreInstrumentation)
                    tp.AddAspNetCoreInstrumentation();

                if (options.OtlpEndpoint != null)
                    tp.AddOtlpExporter(o => o.Endpoint = options.OtlpEndpoint);

                options.ConfigureTracing?.Invoke(tp);
            });
        }

        if (options.EnableMetrics)
        {
            builder.WithMetrics(mp =>
            {
                // Feishu Meter（事件处理、WebSocket、Webhook 指标）
                mp.AddMeter(FeishuMetrics.MeterName);

                // Mud.HttpUtils Meter（HTTP 请求、Token 刷新、重试、熔断器、下载）
                if (options.IncludeMudHttpUtils)
                    mp.AddMeter(MudHttpMeter.MeterName);

                if (options.EnableHttpClientInstrumentation)
                    mp.AddHttpClientInstrumentation();

                if (options.OtlpEndpoint != null)
                    mp.AddOtlpExporter(o => o.Endpoint = options.OtlpEndpoint);

                options.ConfigureMetrics?.Invoke(mp);
            });
        }

        if (options.EnableLogging)
        {
            builder.WithLogging(lp =>
            {
                if (options.OtlpEndpoint != null)
                    lp.AddOtlpExporter(o => o.Endpoint = options.OtlpEndpoint);

                options.ConfigureLogging?.Invoke(lp);
            });
        }

        return builder;
    }
}
