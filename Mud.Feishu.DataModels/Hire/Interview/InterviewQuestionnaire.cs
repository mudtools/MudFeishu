// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 面试满意度问卷（满意度问卷列表响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewQuestionnaire
{
    /// <summary>
    /// <para>问卷 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("questionnaire_id")]
    public string? QuestionnaireId { get; set; }

    /// <summary>
    /// <para>投递 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>面试 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_id")]
    public string? InterviewId { get; set; }

    /// <summary>
    /// <para>问卷版本号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("version")]
    public int? Version { get; set; }

    /// <summary>
    /// <para>是否完成作答</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("has_answers")]
    public bool? HasAnswers { get; set; }

    /// <summary>
    /// <para>更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>问卷题目列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("questions")]
    public InterviewQuestionnaireQuestion[]? Questions { get; set; }
}
