// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 人才信息（获取人才 v1 详情/列表响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class Talent
{
    /// <summary>
    /// <para>人才 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>是否在猎头保护期</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_in_agency_period")]
    public bool? IsInAgencyPeriod { get; set; }

    /// <summary>
    /// <para>是否已入职</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_onboarded")]
    public bool? IsOnboarded { get; set; }

    /// <summary>
    /// <para>基本信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("basic_info")]
    public TalentBasicInfo? BasicInfo { get; set; }

    /// <summary>
    /// <para>教育经历列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("education_list")]
    public TalentEducationInfo[]? EducationList { get; set; }

    /// <summary>
    /// <para>工作经历列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("career_list")]
    public TalentCareerInfo[]? CareerList { get; set; }

    /// <summary>
    /// <para>项目经历列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("project_list")]
    public TalentProjectInfo[]? ProjectList { get; set; }

    /// <summary>
    /// <para>作品列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("works_list")]
    public TalentWorksInfo[]? WorksList { get; set; }

    /// <summary>
    /// <para>获奖列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("award_list")]
    public TalentAwardInfo[]? AwardList { get; set; }

    /// <summary>
    /// <para>语言能力列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("language_list")]
    public TalentLanguageInfo[]? LanguageList { get; set; }

    /// <summary>
    /// <para>社交账号列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sns_list")]
    public TalentSnsInfo[]? SnsList { get; set; }

    /// <summary>
    /// <para>简历来源列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("resume_source_list")]
    public TalentResumeSource[]? ResumeSourceList { get; set; }

    /// <summary>
    /// <para>面试登记表列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_registration_list")]
    public TalentInterviewRegistrationSimple[]? InterviewRegistrationList { get; set; }

    /// <summary>
    /// <para>登记表列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("registration_list")]
    public RegistrationBasicInfo[]? RegistrationList { get; set; }

    /// <summary>
    /// <para>简历附件 ID 列表（按创建时间倒序）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("resume_attachment_id_list")]
    public string[]? ResumeAttachmentIdList { get; set; }

    /// <summary>
    /// <para>自定义模块</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data_list")]
    public TalentCustomizedData[]? CustomizedDataList { get; set; }

    /// <summary>
    /// <para>最高学历：1 小学 / 2 初中 / 3 中专 / 4 高中 / 5 大专 / 6 本科 / 7 硕士 / 8 博士 / 9 其他</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("top_degree")]
    public int? TopDegree { get; set; }

    /// <summary>
    /// <para>第一学历：1 大专以下 / 2 大专 / 3 本科 / 4 硕士 / 5 博士 / 6 其他 / 7 空</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("first_degree")]
    public int? FirstDegree { get; set; }
}
