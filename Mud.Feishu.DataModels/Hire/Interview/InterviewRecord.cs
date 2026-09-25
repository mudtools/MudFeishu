// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 面试评价（面试评价 v1，与面试信息的面试官评价列表共用）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewRecord
{
    /// <summary>
    /// <para>面试评价 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>面试官用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>系统预设「记录」内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>建议定级下限职级 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("min_job_level_id")]
    public string? MinJobLevelId { get; set; }

    /// <summary>
    /// <para>建议定级上限职级 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("max_job_level_id")]
    public string? MaxJobLevelId { get; set; }

    /// <summary>
    /// <para>提交状态：1 已提交 / 2 未提交</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("commit_status")]
    public int? CommitStatus { get; set; }

    /// <summary>
    /// <para>面试评价提交时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feedback_submit_time")]
    public long? FeedbackSubmitTime { get; set; }

    /// <summary>
    /// <para>面试结论：1 通过 / 2 未通过 / 3 未开始 / 4 未提交 / 5 未到场 / 6 待定</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("conclusion")]
    public int? Conclusion { get; set; }

    /// <summary>
    /// <para>面试得分</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_score")]
    public InterviewScore? InterviewScore { get; set; }

    /// <summary>
    /// <para>打分题分数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("assessment_score")]
    public AssessmentScoreInfo? AssessmentScore { get; set; }

    /// <summary>
    /// <para>面试题目列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("question_list")]
    public InterviewQuestion[]? QuestionList { get; set; }

    /// <summary>
    /// <para>在线编程题列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("code_question_list")]
    public InterviewQuestion[]? CodeQuestionList { get; set; }

    /// <summary>
    /// <para>面试官</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interviewer")]
    public IdNameObject? Interviewer { get; set; }

    /// <summary>
    /// <para>附件列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("image_list")]
    public InterviewAttachment[]? ImageList { get; set; }

    /// <summary>
    /// <para>维度评价列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_assessment_list")]
    public InterviewDimensionAssessment[]? DimensionAssessmentList { get; set; }
}
