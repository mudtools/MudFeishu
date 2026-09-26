// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 背调报告文件
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class EcoBackgroundCheckReportFile
{
    /// <summary>
    /// <para>报告名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：stage_report.pdf</para>
    /// </summary>
    [JsonPropertyName("report_name")]
    public string? ReportName { get; set; }

    /// <summary>
    /// <para>报告地址；report_url_type 为空或为 1 时需为可下载的 pdf 链接，为 2 时为预览型链接</para>
    /// <para>必填：是</para>
    /// <para>示例值：https://xxxxx/xxxxxx/xxxx.pdf</para>
    /// </summary>
    [JsonPropertyName("report_url")]
    public string? ReportUrl { get; set; }

    /// <summary>
    /// <para>报告地址类型：1（可下载链接，枚举值为空时同 1）、2（外部预览链接）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("report_url_type")]
    public int? ReportUrlType { get; set; }
}
