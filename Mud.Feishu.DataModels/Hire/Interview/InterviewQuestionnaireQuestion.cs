// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 面试满意度问卷题目（问卷子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewQuestionnaireQuestion
{
    /// <summary>
    /// <para>题目 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("question_id")]
    public string? QuestionId { get; set; }

    /// <summary>
    /// <para>题目中文名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("question_name")]
    public string? QuestionName { get; set; }

    /// <summary>
    /// <para>题目英文名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("question_en_name")]
    public string? QuestionEnName { get; set; }

    /// <summary>
    /// <para>题目中文描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("question_desc")]
    public string? QuestionDesc { get; set; }

    /// <summary>
    /// <para>题目英文描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("question_en_desc")]
    public string? QuestionEnDesc { get; set; }

    /// <summary>
    /// <para>作答结果描述（描述题作答内容）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description_result")]
    public string? DescriptionResult { get; set; }

    /// <summary>
    /// <para>题目类型：1 单选 / 2 多选 / 3 描述 / 4 评分</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("question_type")]
    public int? QuestionType { get; set; }

    /// <summary>
    /// <para>是否必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_required")]
    public bool? IsRequired { get; set; }

    /// <summary>
    /// <para>选项作答列表（单选/多选题）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("select_option_result_list")]
    public InterviewQuestionnaireOptionResult[]? SelectOptionResultList { get; set; }

    /// <summary>
    /// <para>评分作答结果（评分题）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("five_start_scoring_result")]
    public InterviewQuestionnaireScoringResult? FiveStartScoringResult { get; set; }
}
