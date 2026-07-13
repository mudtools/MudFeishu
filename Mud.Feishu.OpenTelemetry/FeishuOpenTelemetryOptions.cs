// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Mud.Feishu.OpenTelemetry;

/// <summary>
/// Mud.Feishu OpenTelemetry 配置选项。
/// </summary>
/// <remarks>
/// 用于 <see cref="FeishuOpenTelemetryExtensions.AddFeishuOpenTelemetry"/> 配置追踪、指标、导出器等。
/// 所有开关默认开启（除日志导出），调用方按需关闭。
/// </remarks>
public class FeishuOpenTelemetryOptions
{
    /// <summary>
    /// 是否启用追踪（Tracing）。默认 <c>true</c>。
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// 是否启用指标（Metrics）。默认 <c>true</c>。
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// 是否启用 OTLP 日志导出。默认 <c>false</c>。
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// 是否同时注册 Mud.HttpUtils 的 ActivitySource 和 Meter。
    /// 默认 <c>true</c>，用于自动覆盖 Feishu HTTP API 调用的出站请求追踪与 Token 刷新指标。
    /// </summary>
    public bool IncludeMudHttpUtils { get; set; } = true;

    /// <summary>
    /// 是否启用 .NET HttpClient Instrumentation。默认 <c>true</c>。
    /// 关联出站 HTTP 调用的 <c>System.Net.Http</c> ActivitySource。
    /// </summary>
    public bool EnableHttpClientInstrumentation { get; set; } = true;

    /// <summary>
    /// 是否启用 ASP.NET Core 入站请求的 Instrumentation。默认 <c>true</c>。
    /// 仅在 ASP.NET Core 主机中生效；控制台应用无效。
    /// </summary>
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    /// <summary>
    /// OTLP 导出端点。默认 <c>http://localhost:4317</c>。
    /// 设为 <c>null</c> 则不配置 OTLP 导出器，需要调用方自行追加。
    /// </summary>
    public Uri? OtlpEndpoint { get; set; } = new("http://localhost:4317");

    /// <summary>
    /// 服务名称，用于 OTel Resource 属性 <c>service.name</c>。默认 <c>"Mud.Feishu.Application"</c>。
    /// </summary>
    public string ServiceName { get; set; } = "Mud.Feishu.Application";

    /// <summary>
    /// 服务版本，用于 OTel Resource 属性 <c>service.version</c>。
    /// 默认与 <see cref="Mud.Feishu.Abstractions.Observability.FeishuActivitySource.Version"/> 一致。
    /// </summary>
    public string ServiceVersion { get; set; } = Mud.Feishu.Abstractions.Observability.FeishuActivitySource.Version;

    /// <summary>
    /// 部署环境，用于 OTel Resource 属性 <c>deployment.environment</c>。默认 <c>"production"</c>。
    /// </summary>
    public string DeploymentEnvironment { get; set; } = "production";

    /// <summary>
    /// 采样比率（0.0~1.0），默认 <c>1.0</c>（全采样）。
    /// 生产环境建议 0.1~0.3。使用 <c>ParentBasedSampler(TraceIdRatioBasedSampler)</c> 策略。
    /// </summary>
    public double SamplingRatio { get; set; } = 1.0;

    /// <summary>
    /// 自定义追踪配置委托。在 Feishu 默认配置之后执行，可追加/覆盖配置。
    /// </summary>
    public Action<TracerProviderBuilder>? ConfigureTracing { get; set; }

    /// <summary>
    /// 自定义指标配置委托。在 Feishu 默认配置之后执行，可追加/覆盖配置。
    /// </summary>
    public Action<MeterProviderBuilder>? ConfigureMetrics { get; set; }

    /// <summary>
    /// 自定义日志配置委托。在 Feishu 默认配置之后执行，可追加/覆盖配置。
    /// </summary>
    public Action<LoggerProviderBuilder>? ConfigureLogging { get; set; }
}
