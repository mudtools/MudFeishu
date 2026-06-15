// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;


/// <summary>
/// <para>职业经历</para>
/// </summary>
public class ResumeCareer
{
    /// <summary>
    /// <para>公司名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：XX公司</para>
    /// </summary>
    [JsonPropertyName("company")]
    public string? Company { get; set; }

    /// <summary>
    /// <para>开始时间,格式：YYYY-MM-DD</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022-01-03</para>
    /// </summary>
    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    /// <summary>
    /// <para>始时间,格式：YYYY-MM-DD,跟start_date值一样</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022-01-03</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>结束时间,格式：YYYY-MM-DD</para>
    /// <para>必填：否</para>
    /// <para>示例值：2023-01-03</para>
    /// </summary>
    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }

    /// <summary>
    /// <para>结束时间,格式：YYYY-MM-DD 或 “至今”，当值为“至今”时，end_date=="",值为其他时，end_date==end_time</para>
    /// <para>必填：否</para>
    /// <para>示例值：2023-01-03</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>职位</para>
    /// <para>必填：否</para>
    /// <para>示例值：XXX工程师</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>工作类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：实习</item>
    /// <item>2：全职</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// <para>工作类型——'实习'、'全职'</para>
    /// <para>必填：否</para>
    /// <para>示例值：全职</para>
    /// </summary>
    [JsonPropertyName("type_str")]
    public string? TypeStr { get; set; }

    /// <summary>
    /// <para>工作描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：负责XXX开发...</para>
    /// </summary>
    [JsonPropertyName("job_description")]
    public string? JobDescription { get; set; }
}