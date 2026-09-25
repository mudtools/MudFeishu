// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 更新职位设置请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class UpdateJobConfigRequest
{
    /// <summary>
    /// <para>Offer 申请表 ID，可通过获取 Offer 申请表列表接口获取；update_option_list 包含「更新 Offer 申请表」时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_apply_schema_id")]
    public string? OfferApplySchemaId { get; set; }

    /// <summary>
    /// <para>Offer 审批流程 ID，可通过获取 Offer 审批流配置列表接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_process_conf")]
    public string? OfferProcessConf { get; set; }

    /// <summary>
    /// <para>建议评估人 ID 列表，需与入参 user_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recommended_evaluator_id_list")]
    public string[]? RecommendedEvaluatorIdList { get; set; }

    /// <summary>
    /// <para>更新选项：要更新的配置项编号列表；接口按所选项校验并更新对应参数，必填字段未填写时报错</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("update_option_list")]
    public int[] UpdateOptionList { get; set; } = Array.Empty<int>();

    /// <summary>
    /// <para>面试评价表 ID，可通过获取面试评价表列表接口获取；update_option_list 包含「更新面试评价表」且开启面试轮次类型设置时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("assessment_template_biz_id")]
    public string? AssessmentTemplateBizId { get; set; }

    /// <summary>
    /// <para>建议面试官列表；update_option_list 包含「更新建议面试官」时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_round_conf_list")]
    public JobConfigInterviewRoundConfRequest[]? InterviewRoundConfList { get; set; }

    /// <summary>
    /// <para>关联招聘需求 ID 列表，可通过获取招聘需求信息接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("jr_id_list")]
    public string[]? JrIdList { get; set; }

    /// <summary>
    /// <para>面试登记表 ID；update_option_list 包含「更新面试登记表」时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_registration_schema_id")]
    public string? InterviewRegistrationSchemaId { get; set; }

    /// <summary>
    /// <para>入职登记表 ID；update_option_list 包含「更新入职登记表」时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboard_registration_schema_id")]
    public string? OnboardRegistrationSchemaId { get; set; }

    /// <summary>
    /// <para>面试轮次类型配置列表；update_option_list 包含「更新面试评价表」且开启面试轮次类型设置时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_round_type_conf_list")]
    public JobConfigRoundTypeRequest[]? InterviewRoundTypeConfList { get; set; }

    /// <summary>
    /// <para>关联职位 ID 列表（实体职位关联虚拟职位、虚拟职位关联实体职位）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("related_job_id_list")]
    public string[]? RelatedJobIdList { get; set; }

    /// <summary>
    /// <para>自助约面配置；update_option_list 包含「更新面试官安排面试配置」时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_appointment_config")]
    public InterviewAppointmentConfig? InterviewAppointmentConfig { get; set; }

    /// <summary>
    /// <para>官网申请表 ID，可通过获取招聘官网申请表模板列表接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("portal_website_apply_form_schema_id")]
    public string? PortalWebsiteApplyFormSchemaId { get; set; }
}
