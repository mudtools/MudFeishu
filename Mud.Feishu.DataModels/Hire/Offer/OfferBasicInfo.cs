// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// Offer 基本信息，创建/更新请求与创建响应共用
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class OfferBasicInfo
{
    /// <summary>
    /// <para>部门 ID</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>直属上级用户 ID</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("leader_user_id")]
    public string? LeaderUserId { get; set; }

    /// <summary>
    /// <para>雇佣职位 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employment_job_id")]
    public string? EmploymentJobId { get; set; }

    /// <summary>
    /// <para>人员类型 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employee_type_id")]
    public string? EmployeeTypeId { get; set; }

    /// <summary>
    /// <para>职位序列 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_family_id")]
    public string? JobFamilyId { get; set; }

    /// <summary>
    /// <para>职级 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_level_id")]
    public string? JobLevelId { get; set; }

    /// <summary>
    /// <para>试用期月数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("probation_month")]
    public int? ProbationMonth { get; set; }

    /// <summary>
    /// <para>合同年数（已废弃，推荐使用 contract_period）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contract_year")]
    public int? ContractYear { get; set; }

    /// <summary>
    /// <para>合同期</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contract_period")]
    public ContractPeriodInfo? ContractPeriod { get; set; }

    /// <summary>
    /// <para>预计入职日期，格式为 {"date":"YYYY-MM-DD"} 的 JSON 字符串</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("expected_onboard_date")]
    public string? ExpectedOnboardDate { get; set; }

    /// <summary>
    /// <para>入职地址 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboard_address_id")]
    public string? OnboardAddressId { get; set; }

    /// <summary>
    /// <para>工作地址 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("work_address_id")]
    public string? WorkAddressId { get; set; }

    /// <summary>
    /// <para>Offer 负责人用户 ID</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("owner_user_id")]
    public string? OwnerUserId { get; set; }

    /// <summary>
    /// <para>Offer 推荐语</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recommended_words")]
    public string? RecommendedWords { get; set; }

    /// <summary>
    /// <para>招聘需求 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_requirement_id")]
    public string? JobRequirementId { get; set; }

    /// <summary>
    /// <para>招聘流程类型：1 社招，2 校招</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_process_type_id")]
    public int? JobProcessTypeId { get; set; }

    /// <summary>
    /// <para>附件 ID 列表（文档标注已废弃，请勿使用）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("attachment_id_list")]
    public string[]? AttachmentIdList { get; set; }

    /// <summary>
    /// <para>通用附件 ID 列表，最多 20 个</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("common_attachment_id_list")]
    public string[]? CommonAttachmentIdList { get; set; }

    /// <summary>
    /// <para>附件描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("attachment_description")]
    public string? AttachmentDescription { get; set; }

    /// <summary>
    /// <para>操作人用户 ID</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("operator_user_id")]
    public string? OperatorUserId { get; set; }

    /// <summary>
    /// <para>职位 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("position_id")]
    public string? PositionId { get; set; }

    /// <summary>
    /// <para>职位名称（录用职位）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_offered")]
    public string? JobOffered { get; set; }

    /// <summary>
    /// <para>职等 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_grade_id")]
    public string? JobGradeId { get; set; }

    /// <summary>
    /// <para>晋升通道 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pathway_id")]
    public string? PathwayId { get; set; }
}
