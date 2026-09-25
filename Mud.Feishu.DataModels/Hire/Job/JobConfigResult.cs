// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 职位设置（获取/更新职位设置响应体）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class JobConfigResult
{
    /// <summary>
    /// <para>职位 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>Offer 申请表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_apply_schema")]
    public IdNameObject? OfferApplySchema { get; set; }

    /// <summary>
    /// <para>Offer 审批流</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_process_conf")]
    public IdNameObject? OfferProcessConf { get; set; }

    /// <summary>
    /// <para>建议评估人列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recommended_evaluator_list")]
    public IdNameObject[]? RecommendedEvaluatorList { get; set; }

    /// <summary>
    /// <para>面试评价表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("assessment_template")]
    public IdNameObject? AssessmentTemplate { get; set; }

    /// <summary>
    /// <para>建议面试官列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_round_list")]
    public JobConfigInterviewRound[]? InterviewRoundList { get; set; }

    /// <summary>
    /// <para>招聘需求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_requirement_list")]
    public IdNameObject[]? JobRequirementList { get; set; }

    /// <summary>
    /// <para>面试登记表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_registration")]
    public RegistrationInfo? InterviewRegistration { get; set; }

    /// <summary>
    /// <para>入职登记表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboard_registration")]
    public RegistrationInfo? OnboardRegistration { get; set; }

    /// <summary>
    /// <para>面试轮次类型列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_round_type_list")]
    public JobConfigRoundTypeResult[]? InterviewRoundTypeList { get; set; }

    /// <summary>
    /// <para>关联职位列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("related_job_list")]
    public IdNameObject[]? RelatedJobList { get; set; }

    /// <summary>
    /// <para>职位属性：1 实体职位 / 2 虚拟职位</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_attribute")]
    public int? JobAttribute { get; set; }

    /// <summary>
    /// <para>自助约面配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_appointment_config")]
    public InterviewAppointmentConfig? InterviewAppointmentConfig { get; set; }

    /// <summary>
    /// <para>官网申请表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("portal_website_apply_form_schema_info")]
    public RegistrationInfo? PortalWebsiteApplyFormSchemaInfo { get; set; }
}
