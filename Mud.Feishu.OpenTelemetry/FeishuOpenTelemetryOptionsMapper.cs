// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.HttpUtils.OpenTelemetry;
// 别名消歧：Mud.HttpUtils.OpenTelemetry 也导出 OtlpExportProtocol（CS0104 防护）。
using MudOtlpExportProtocol = Mud.HttpUtils.OpenTelemetry.OtlpExportProtocol;

namespace Mud.Feishu.OpenTelemetry;

/// <summary>
/// <see cref="FeishuOpenTelemetryOptions"/> → 上游共享内核选项 <see cref="MudObservabilityOptions"/> 的显式映射。
/// </summary>
/// <remarks>
/// <para><b>为何用映射而非 options 继承</b>：配置绑定源生成器（<c>EnableConfigurationBindingGenerator</c>）
/// 对**继承属性**的支持需先实测，未验证即用会导致 AOT 下绑定静默失效；显式映射零风险且可静态审查。</para>
/// <para>本方法不做任何校验与默认值回填：校验在 <see cref="FeishuOpenTelemetryExtensions"/> 注册期完成，
/// 缺省回填（<c>ServiceName</c> / <c>ServiceVersion</c>）由内核按贡献默认值完成。</para>
/// </remarks>
internal static class FeishuOpenTelemetryOptionsMapper
{
    /// <summary>
    /// 逐属性映射到共享内核选项（每个公开可写属性都必须被读取，漏映射即配置静默失效）。
    /// </summary>
    /// <param name="options">飞书可观测性选项。</param>
    /// <returns>共享内核选项。</returns>
    internal static MudObservabilityOptions ToCore(FeishuOpenTelemetryOptions options)
    {
        return new MudObservabilityOptions
        {
            EnableTracing = options.EnableTracing,
            EnableMetrics = options.EnableMetrics,
            EnableLogging = options.EnableLogging,
            EnableHttpClientInstrumentation = options.EnableHttpClientInstrumentation,
            EnableAspNetCoreInstrumentation = options.EnableAspNetCoreInstrumentation,
            OtlpEndpoint = options.OtlpEndpoint,
            // 两个枚举取值一一对应（Grpc = 0 / HttpProtobuf = 1），转型安全；
            // 两侧均为「本仓/上游自定义枚举」而非 OTel SDK 枚举（后者的 Grpc 成员已过时）。
            OtlpExportProtocol = (MudOtlpExportProtocol)options.OtlpExportProtocol,
            OtlpHeaders = options.OtlpHeaders,
            UseShortExporterTimeout = options.UseShortExporterTimeout,
            ExportBatchSize = options.ExportBatchSize,
            ExportIntervalMilliseconds = options.ExportIntervalMilliseconds,
            ServiceName = options.ServiceName,
            ServiceVersion = options.ServiceVersion,
            DeploymentEnvironment = options.DeploymentEnvironment,
            SamplingRatio = options.SamplingRatio,
            ConfigureTracing = options.ConfigureTracing,
            ConfigureMetrics = options.ConfigureMetrics,
            ConfigureLogging = options.ConfigureLogging,
        };
    }
}
