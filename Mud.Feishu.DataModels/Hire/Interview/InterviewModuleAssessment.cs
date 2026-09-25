// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 模块评价（面试评价 v2 子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class InterviewModuleAssessment
{
    /// <summary>
    /// <para>面试反馈表模块 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_feedback_form_module_id")]
    public string? InterviewFeedbackFormModuleId { get; set; }

    /// <summary>
    /// <para>模块名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("module_name")]
    public I18nName? ModuleName { get; set; }

    /// <summary>
    /// <para>模块类型：1 系统预置面试结论 / 2 自定义</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("module_type")]
    public int? ModuleType { get; set; }

    /// <summary>
    /// <para>模块权重</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("module_weight")]
    public double? ModuleWeight { get; set; }

    /// <summary>
    /// <para>模块得分</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("module_score")]
    public double? ModuleScore { get; set; }

    /// <summary>
    /// <para>维度评价列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dimension_assessments")]
    public InterviewDimensionAssessmentV2[]? DimensionAssessments { get; set; }
}
