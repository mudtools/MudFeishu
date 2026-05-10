// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceArchives;

/// <summary>
/// <para>分页查询结果项</para>
/// </summary>
public class ArchiveReportMeta
{
    /// <summary>
    /// <para>引用报表 ID，暂时无用</para>
    /// <para>必填：否</para>
    /// <para>示例值：7341290237441605652</para>
    /// </summary>
    [JsonPropertyName("report_id")]
    public string? ReportId { get; set; }

    /// <summary>
    /// <para>引用报表name</para>
    /// <para>必填：否</para>
    /// <para>示例值：月报汇总</para>
    /// </summary>
    [JsonPropertyName("report_name")]
    public I18nMap? ReportName { get; set; }

    /// <summary>
    /// <para>归档报表规则ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7341290237441605652</para>
    /// </summary>
    [JsonPropertyName("archive_rule_id")]
    public string? ArchiveRuleId { get; set; }

    /// <summary>
    /// <para>归档报表name</para>
    /// <para>必填：否</para>
    /// <para>示例值：归档全员</para>
    /// </summary>
    [JsonPropertyName("archive_rule_name")]
    public I18nMap? ArchiveRuleName { get; set; }
}