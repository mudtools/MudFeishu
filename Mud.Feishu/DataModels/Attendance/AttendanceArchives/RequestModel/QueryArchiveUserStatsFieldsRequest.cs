// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceArchives;

/// <summary>
/// 查询归档报表表头请求体
/// </summary>
public class QueryArchiveUserStatsFieldsRequest
{
    /// <summary>
    /// <para>语言类型。默认为zh。</para>
    /// <para>可选值有：</para>
    /// <para>* `en`：英语</para>
    /// <para>* `ja`：日语</para>
    /// <para>* `zh`：中文</para>
    /// <para>必填：否</para>
    /// <para>示例值：zh</para>
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// <para>月份，日期格式为yyyyMM</para>
    /// <para>必填：是</para>
    /// <para>示例值：202409</para>
    /// </summary>
    [JsonPropertyName("month")]
    public string Month { get; set; } = string.Empty;

    /// <summary>
    /// <para>归档规则id，可根据[查询所有归档规则](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/attendance-v1/archive_rule/list)获得</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("archive_rule_id")]
    public string ArchiveRuleId { get; set; } = string.Empty;

    /// <summary>
    /// <para>操作者id，对应employee_type</para>
    /// <para>必填：是</para>
    /// <para>示例值：ax8ud</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public string OperatorId { get; set; } = string.Empty;
}
