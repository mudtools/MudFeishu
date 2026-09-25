// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 官网投递简历信息（按简历创建官网投递请求体子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class WebsiteDeliveryResume
{
    /// <summary>
    /// <para>实习经历</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("internship_list")]
    public WebsiteDeliveryInternship[]? InternshipList { get; set; }

    /// <summary>
    /// <para>基本信息</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("basic_info")]
    public WebsiteDeliveryBasicInfo? BasicInfo { get; set; }

    /// <summary>
    /// <para>教育经历</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("education_list")]
    public WebsiteDeliveryEducation[]? EducationList { get; set; }

    /// <summary>
    /// <para>自我评价</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("self_evaluation")]
    public WebsiteDeliverySelfEvaluation? SelfEvaluation { get; set; }

    /// <summary>
    /// <para>工作经历</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("career_list")]
    public WebsiteDeliveryCareer[]? CareerList { get; set; }

    /// <summary>
    /// <para>自定义模块</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data")]
    public WebsiteDeliveryCustomizedDataParent[]? CustomizedData { get; set; }

    /// <summary>
    /// <para>简历附件 ID，需通过创建附件接口生成</para>
    /// <para>必填：否</para>
    /// <para>示例值：6960663240925956654</para>
    /// </summary>
    [JsonPropertyName("resume_attachment_id")]
    public string? ResumeAttachmentId { get; set; }

    /// <summary>
    /// <para>社交账号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sns_list")]
    public WebsiteDeliverySns[]? SnsList { get; set; }

    /// <summary>
    /// <para>作品</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("works_list")]
    public WebsiteDeliveryWorks[]? WorksList { get; set; }

    /// <summary>
    /// <para>获奖记录</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("award_list")]
    public WebsiteDeliveryAward[]? AwardList { get; set; }

    /// <summary>
    /// <para>项目经历</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("project_list")]
    public WebsiteDeliveryProject[]? ProjectList { get; set; }

    /// <summary>
    /// <para>语言能力</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("language_list")]
    public WebsiteDeliveryLanguage[]? LanguageList { get; set; }
}
