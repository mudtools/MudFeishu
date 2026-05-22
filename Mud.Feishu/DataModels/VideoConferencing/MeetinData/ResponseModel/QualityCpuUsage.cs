// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;


/// <summary>
/// <para>Cpu使用量</para>
/// </summary>
public class QualityCpuUsage
{
    /// <summary>
    /// <para>时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022.12.23 11:17:00</para>
    /// </summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    /// <summary>
    /// <para>客户端平均 CPU 占用</para>
    /// <para>必填：否</para>
    /// <para>示例值：0.8%</para>
    /// </summary>
    [JsonPropertyName("client_avg_cpu_usage")]
    public string? ClientAvgCpuUsage { get; set; }

    /// <summary>
    /// <para>客户端最大 CPU 占用</para>
    /// <para>必填：否</para>
    /// <para>示例值：2.3%</para>
    /// </summary>
    [JsonPropertyName("client_max_cpu_usage")]
    public string? ClientMaxCpuUsage { get; set; }

    /// <summary>
    /// <para>系统平均 CPU 占用</para>
    /// <para>必填：否</para>
    /// <para>示例值：8.3%</para>
    /// </summary>
    [JsonPropertyName("system_avg_cpu_usage")]
    public string? SystemAvgCpuUsage { get; set; }

    /// <summary>
    /// <para>系统最大 CPU 占用</para>
    /// <para>必填：否</para>
    /// <para>示例值：30%</para>
    /// </summary>
    [JsonPropertyName("system_max_cpu_usage")]
    public string? SystemMaxCpuUsage { get; set; }
}