// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 创建人才请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CombinedCreateTalentRequest
{
    /// <summary>
    /// <para>简历来源 ID（已废弃，推荐使用 resume_source_id）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("init_source_id")]
    public string? InitSourceId { get; set; }

    /// <summary>
    /// <para>简历来源 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("resume_source_id")]
    public string? ResumeSourceId { get; set; }

    /// <summary>
    /// <para>文件夹 ID 列表，最大 100 个</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("folder_id_list")]
    public string[]? FolderIdList { get; set; }

    /// <summary>
    /// <para>创建者 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator_id")]
    public string? CreatorId { get; set; }

    /// <summary>
    /// <para>创建者类型：1 员工 / 3 系统</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator_account_type")]
    public int? CreatorAccountType { get; set; }

    /// <summary>
    /// <para>简历附件 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("resume_attachment_id")]
    public string? ResumeAttachmentId { get; set; }

    /// <summary>
    /// <para>基本信息</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("basic_info")]
    public TalentCombinedBasicInfo? BasicInfo { get; set; }

    /// <summary>
    /// <para>教育经历列表，最大 100 条</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("education_list")]
    public TalentCombinedEducationInfo[]? EducationList { get; set; }

    /// <summary>
    /// <para>工作经历列表，最大 100 条</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("career_list")]
    public TalentCombinedCareerInfo[]? CareerList { get; set; }

    /// <summary>
    /// <para>项目经历列表，最大 100 条</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("project_list")]
    public TalentCombinedProjectInfo[]? ProjectList { get; set; }

    /// <summary>
    /// <para>作品列表，最大 100 条</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("works_list")]
    public TalentCombinedWorkInfo[]? WorksList { get; set; }

    /// <summary>
    /// <para>获奖列表，最大 100 条</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("award_list")]
    public TalentCombinedAwardInfo[]? AwardList { get; set; }

    /// <summary>
    /// <para>语言能力列表，最大 100 条</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("language_list")]
    public TalentCombinedLanguageInfo[]? LanguageList { get; set; }

    /// <summary>
    /// <para>社交账号列表，最大 100 条</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sns_list")]
    public TalentCombinedSnsInfo[]? SnsList { get; set; }

    /// <summary>
    /// <para>意向城市编码列表，最大 100 个</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("preferred_city_code_list")]
    public string[]? PreferredCityCodeList { get; set; }

    /// <summary>
    /// <para>自我评价</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("self_evaluation")]
    public TalentSelfEvaluation? SelfEvaluation { get; set; }

    /// <summary>
    /// <para>自定义模块字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data")]
    public TalentCustomizedDataObjectValue[]? CustomizedData { get; set; }
}
