// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 维度评价（面试评价 v1 子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewDimensionAssessment
{
    /// <summary>
    /// <para>维度评价 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>维度名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>维度满分</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("full_score")]
    public int? FullScore { get; set; }

    /// <summary>
    /// <para>维度作答内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>维度 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_id")]
    public string? DimensionId { get; set; }

    /// <summary>
    /// <para>维度选中项</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_score")]
    public InterviewDimensionOption? DimensionScore { get; set; }

    /// <summary>
    /// <para>维度选中项列表（多选）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_score_list")]
    public InterviewDimensionOption[]? DimensionScoreList { get; set; }

    /// <summary>
    /// <para>维度自定义分值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_custom_score")]
    public int? DimensionCustomScore { get; set; }

    /// <summary>
    /// <para>关联能力维度列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ability_list")]
    public InterviewAbility[]? AbilityList { get; set; }

    /// <summary>
    /// <para>关联题目列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("question_list")]
    public InterviewQuestion[]? QuestionList { get; set; }

    /// <summary>
    /// <para>维度类型：1 单选 / 2 多选 / 3 描述 / 4 单行文本 / 5 职级范围 / 6 打分单选 / 7 打分填空 / 10 结论 / 11 得分 / 12 记录</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_type")]
    public int? DimensionType { get; set; }
}
