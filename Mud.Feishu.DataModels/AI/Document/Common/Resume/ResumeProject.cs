// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;


/// <summary>
/// <para>项目经历</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "AI")]
public class ResumeProject
{
    /// <summary>
    /// <para>项目名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：XX项目</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>项目岗位</para>
    /// <para>必填：否</para>
    /// <para>示例值：客户端研发</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>开始时间,格式：YYYY-MM-DD</para>
    /// <para>必填：否</para>
    /// <para>示例值：2023-01-03</para>
    /// </summary>
    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    /// <summary>
    /// <para>开始时间,格式：YYYY-MM-DD,跟start_date值一样</para>
    /// <para>必填：否</para>
    /// <para>示例值：2023-01-03</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>结束时间,格式：YYYY-MM-DD</para>
    /// <para>必填：否</para>
    /// <para>示例值：2023-01-04</para>
    /// </summary>
    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }

    /// <summary>
    /// <para>结束时间,格式：YYYY-MM-DD 或 “至今”，当值为“至今”时，end_date=="",值</para>
    /// <para>必填：否</para>
    /// <para>示例值：2023-01-04</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>项目描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：XXX项目是一个...</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
