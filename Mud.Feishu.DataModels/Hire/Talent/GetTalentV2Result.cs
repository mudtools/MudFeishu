// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 获取人才 v2 响应体（data 直接平铺 composite_talent）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class GetTalentV2Result
{
    /// <summary>
    /// <para>人才 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("talent_id")]
    public string? TalentId { get; set; }

    /// <summary>
    /// <para>基础信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("basic_info")]
    public CompositeTalentBasicInfo? BasicInfo { get; set; }

    /// <summary>
    /// <para>教育经历列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("education_list")]
    public CompositeTalentEducationInfo[]? EducationList { get; set; }

    /// <summary>
    /// <para>工作经历列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("career_list")]
    public CompositeTalentCareerInfo[]? CareerList { get; set; }

    /// <summary>
    /// <para>项目经历列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("project_list")]
    public CompositeTalentProjectInfo[]? ProjectList { get; set; }

    /// <summary>
    /// <para>作品列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("works_list")]
    public CompositeTalentWorksInfo[]? WorksList { get; set; }

    /// <summary>
    /// <para>获奖列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("award_list")]
    public CompositeTalentAwardInfo[]? AwardList { get; set; }

    /// <summary>
    /// <para>语言能力列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("language_list")]
    public CompositeTalentLanguageInfo[]? LanguageList { get; set; }

    /// <summary>
    /// <para>社交账号列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sns_list")]
    public CompositeTalentSnsInfo[]? SnsList { get; set; }

    /// <summary>
    /// <para>简历来源列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("resume_source_list")]
    public TalentResumeSource[]? ResumeSourceList { get; set; }

    /// <summary>
    /// <para>实习经历列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("internship_list")]
    public CompositeTalentInternshipInfo[]? InternshipList { get; set; }

    /// <summary>
    /// <para>自定义模块列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data_list")]
    public CompositeTalentCustomizedData[]? CustomizedDataList { get; set; }

    /// <summary>
    /// <para>简历附件 ID 列表（已废弃，改用 resume_attachment_list）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("resume_attachment_id_list")]
    public string[]? ResumeAttachmentIdList { get; set; }

    /// <summary>
    /// <para>简历附件列表（按创建时间倒序）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("resume_attachment_list")]
    public TalentResumeAttachment[]? ResumeAttachmentList { get; set; }

    /// <summary>
    /// <para>面试登记表列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_registration_list")]
    public TalentInterviewRegistrationSimple[]? InterviewRegistrationList { get; set; }

    /// <summary>
    /// <para>信息登记表列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("registration_list")]
    public RegistrationBasicInfo[]? RegistrationList { get; set; }

    /// <summary>
    /// <para>是否已入职</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_onboarded")]
    public bool? IsOnboarded { get; set; }

    /// <summary>
    /// <para>是否在竞业限制期</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_in_agency_period")]
    public bool? IsInAgencyPeriod { get; set; }

    /// <summary>
    /// <para>最高学历：1 博士 / 2 硕士 / 3 本科 / 4 专科 / 5 中专 / 6 高中 / 7 初中 / 8 小学 / 9 其他</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("top_degree")]
    public int? TopDegree { get; set; }

    /// <summary>
    /// <para>人才库 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("talent_pool_id_list")]
    public string[]? TalentPoolIdList { get; set; }

    /// <summary>
    /// <para>人才文件夹列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("talent_folder_ref_list_v2")]
    public TalentFolderRef[]? TalentFolderRefListV2 { get; set; }

    /// <summary>
    /// <para>人才标签列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tag_list")]
    public TalentTag[]? TagList { get; set; }

    /// <summary>
    /// <para>黑名单相似信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("similar_info_v2")]
    public TalentSimilar? SimilarInfoV2 { get; set; }

    /// <summary>
    /// <para>黑名单信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("block_info")]
    public TalentBlock? BlockInfo { get; set; }

    /// <summary>
    /// <para>人才库引用列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("talent_pool_ref_list_v2")]
    public TalentPoolRef[]? TalentPoolRefListV2 { get; set; }

    /// <summary>
    /// <para>备注列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("note_list_v2")]
    public TalentNote[]? NoteListV2 { get; set; }
}
