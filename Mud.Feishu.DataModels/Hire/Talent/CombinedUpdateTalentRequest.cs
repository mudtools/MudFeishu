// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 更新人才请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CombinedUpdateTalentRequest
{
    /// <summary>
    /// <para>人才 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6930815272790114324</para>
    /// </summary>
    [JsonPropertyName("talent_id")]
    public string? TalentId { get; set; }

    /// <summary>
    /// <para>简历来源 ID（已废弃）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("init_source_id")]
    public string? InitSourceId { get; set; }

    /// <summary>
    /// <para>文件夹 ID 列表，最大 100 个</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("folder_id_list")]
    public string[]? FolderIdList { get; set; }

    /// <summary>
    /// <para>更新者 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public string? OperatorId { get; set; }

    /// <summary>
    /// <para>更新者类型：1 员工 / 3 系统</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_account_type")]
    public int? OperatorAccountType { get; set; }

    /// <summary>
    /// <para>简历附件 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("resume_attachment_id")]
    public string? ResumeAttachmentId { get; set; }

    /// <summary>
    /// <para>是否仅解析简历更新人才</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("only_parse_resume_update_talent")]
    public bool? OnlyParseResumeUpdateTalent { get; set; }

    /// <summary>
    /// <para>基本信息（更新时内部 name 非必填）</para>
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
