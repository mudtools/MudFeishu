// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// Offer 基本信息（响应形态）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ApplicationOfferBasicInfo
{
    /// <summary>
    /// <para>Offer 类型（已废弃）：1 社招，2 校招，3 实习，4 实习生转正</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("offer_type")]
    public int? OfferType { get; set; }

    /// <summary>
    /// <para>备注（敏感字段）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }

    /// <summary>
    /// <para>过期时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("expire_time")]
    public long? ExpireTime { get; set; }

    /// <summary>
    /// <para>Offer 负责人用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("owner_user_id")]
    public string? OwnerUserId { get; set; }

    /// <summary>
    /// <para>创建人用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator_user_id")]
    public string? CreatorUserId { get; set; }

    /// <summary>
    /// <para>人员类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employee_type")]
    public BaseBilingualWithId? EmployeeType { get; set; }

    /// <summary>
    /// <para>创建时间，毫秒时间戳（文档标 string）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>直属上级用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("leader_user_id")]
    public string? LeaderUserId { get; set; }

    /// <summary>
    /// <para>入职日期，格式 YYYY-MM-DD</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboard_date")]
    public string? OnboardDate { get; set; }

    /// <summary>
    /// <para>部门 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>试用期月数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("probation_month")]
    public int? ProbationMonth { get; set; }

    /// <summary>
    /// <para>合同年数</para>
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
    /// <para>雇员类型（招聘类型）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recruitment_type")]
    public BaseBilingualWithId? RecruitmentType { get; set; }

    /// <summary>
    /// <para>序列</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sequence")]
    public BaseBilingualWithId? Sequence { get; set; }

    /// <summary>
    /// <para>级别（敏感字段）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("level")]
    public BaseBilingualWithId? Level { get; set; }

    /// <summary>
    /// <para>入职地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboard_address")]
    public OfferAddress? OnboardAddress { get; set; }

    /// <summary>
    /// <para>工作地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("work_address")]
    public OfferAddress? WorkAddress { get; set; }

    /// <summary>
    /// <para>自定义字段值列表（人员 ID 值不转换，返回 people_admin_id）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customize_info_list")]
    public OfferCustomValue[]? CustomizeInfoList { get; set; }

    /// <summary>
    /// <para>工作地点信息（仅详情接口返回，目前仅字节可用）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("work_location_address_info")]
    public MasterLocationAddressInfo? WorkLocationAddressInfo { get; set; }

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
    /// <para>通用附件 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("common_attachment_id_list")]
    public string[]? CommonAttachmentIdList { get; set; }

    /// <summary>
    /// <para>晋升通道 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pathway_id")]
    public string? PathwayId { get; set; }
}
