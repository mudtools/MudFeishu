// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 人才教育经历（获取人才 v1 详情/列表响应 education_list 子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class TalentEducationInfo
{
    /// <summary>
    /// <para>教育经历 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>学历：1 小学 / 2 初中 / 3 中专 / 4 高中 / 5 大专 / 6 本科 / 7 硕士 / 11 MBA / 8 博士 / 9 其他</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("degree")]
    public int? Degree { get; set; }

    /// <summary>
    /// <para>学校名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("school")]
    public string? School { get; set; }

    /// <summary>
    /// <para>专业</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_of_study")]
    public string? FieldOfStudy { get; set; }

    /// <summary>
    /// <para>开始时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>结束时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>结束时间（推荐使用，避免毕业时间歧义）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time_v2")]
    public string? EndTimeV2 { get; set; }

    /// <summary>
    /// <para>教育类型：1 港台海外 / 2 统招全日制 / 3 在职 / 4 自考 / 5 其他</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("education_type")]
    public int? EducationType { get; set; }

    /// <summary>
    /// <para>成绩排名：5/10/20/30/50 表示前 X%，-1 表示其他</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("academic_ranking")]
    public int? AcademicRanking { get; set; }

    /// <summary>
    /// <para>教育标签：1 985 / 2 211 / 3 一本 / 4 海外 QS200</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tag_list")]
    public int[]? TagList { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data_list")]
    public TalentCustomizedDataChild[]? CustomizedDataList { get; set; }
}
