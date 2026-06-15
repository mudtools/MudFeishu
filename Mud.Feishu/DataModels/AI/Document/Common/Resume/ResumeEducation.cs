// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;


/// <summary>
/// <para>教育经历</para>
/// </summary>
public class ResumeEducation
{
    /// <summary>
    /// <para>学校名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：A大学</para>
    /// </summary>
    [JsonPropertyName("school")]
    public string? School { get; set; }

    /// <summary>
    /// <para>开始时间,格式：YYYY-MM-DD</para>
    /// <para>必填：否</para>
    /// <para>示例值：2020-01-03</para>
    /// </summary>
    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    /// <summary>
    /// <para>开始时间,格式：YYYY-MM-DD,跟start_date值一样</para>
    /// <para>必填：否</para>
    /// <para>示例值：2020-01-03</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>结束时间,格式：YYYY-MM-DD</para>
    /// <para>必填：否</para>
    /// <para>示例值：2021-01-03</para>
    /// </summary>
    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }

    /// <summary>
    /// <para>结束时间,格式：YYYY-MM-DD 或 “至今”，当值为“至今”时，end_date=="",值为其他时，end_date==end_time</para>
    /// <para>必填：否</para>
    /// <para>示例值：至今</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>专业</para>
    /// <para>必填：否</para>
    /// <para>示例值：XX工程</para>
    /// </summary>
    [JsonPropertyName("major")]
    public string? Major { get; set; }

    /// <summary>
    /// <para>学历——小学、初中、中职、高中、专科、本科、硕士、博士、其他</para>
    /// <para>必填：否</para>
    /// <para>示例值：本科</para>
    /// </summary>
    [JsonPropertyName("degree")]
    public string? Degree { get; set; }

    /// <summary>
    /// <para>学历对应ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：小学</item>
    /// <item>2：初中</item>
    /// <item>3：中职</item>
    /// <item>4：高中</item>
    /// <item>5：专科</item>
    /// <item>6：本科</item>
    /// <item>7：硕士</item>
    /// <item>8：博士</item>
    /// <item>9：其他</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("qualification")]
    public int? Qualification { get; set; }
}