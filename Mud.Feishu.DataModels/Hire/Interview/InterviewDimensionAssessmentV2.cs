// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 维度评价（面试评价 v2 子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewDimensionAssessmentV2
{
    /// <summary>
    /// <para>面试反馈表维度 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_feedback_form_dimension_id")]
    public string? InterviewFeedbackFormDimensionId { get; set; }

    /// <summary>
    /// <para>维度名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_name")]
    public I18nName? DimensionName { get; set; }

    /// <summary>
    /// <para>维度类型：1 单选 / 2 多选 / 3 描述 / 5 职级建议 / 6 打分单选 / 7 打分填空 / 10 结论 / 11 得分 / 12 记录</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_type")]
    public int? DimensionType { get; set; }

    /// <summary>
    /// <para>维度权重</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("weight")]
    public double? Weight { get; set; }

    /// <summary>
    /// <para>维度作答内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_content")]
    public string? DimensionContent { get; set; }

    /// <summary>
    /// <para>维度选中项</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_option")]
    public InterviewDimensionOption? DimensionOption { get; set; }

    /// <summary>
    /// <para>维度选中项列表（多选）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_options")]
    public InterviewDimensionOption[]? DimensionOptions { get; set; }

    /// <summary>
    /// <para>维度分值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_score")]
    public int? DimensionScore { get; set; }

    /// <summary>
    /// <para>职级建议</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recommended_job_level")]
    public InterviewRecommendedJobLevel? RecommendedJobLevel { get; set; }

    /// <summary>
    /// <para>题目评价列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("question_assessments")]
    public InterviewQuestionAssessment[]? QuestionAssessments { get; set; }
}
