// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 组合创建/更新职位请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CombinedJobRequest
{
    /// <summary>
    /// <para>职位 ID（仅读字段，传值会被忽略）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>职位编号，用于与外部系统对接时映射职位</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// <para>工作年限：1 不限 / 2 应届 / 3 1 年以下 / 4 1-3 年 / 5 3-5 年 / 6 5-7 年 / 7 7-10 年 / 8 10 年以上</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("experience")]
    public int? Experience { get; set; }

    /// <summary>
    /// <para>到期日期（毫秒时间戳，已废弃，请使用 <see cref="ExpiryTimestamp"/>）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("expiry_time")]
    public long? ExpiryTime { get; set; }

    /// <summary>
    /// <para>自定义字段值列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data_list")]
    public CombinedJobObjectValue[]? CustomizedDataList { get; set; }

    /// <summary>
    /// <para>最低职级 ID，与入参 job_level_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("min_level_id")]
    public string? MinLevelId { get; set; }

    /// <summary>
    /// <para>最低薪资，单位：k</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("min_salary")]
    public long? MinSalary { get; set; }

    /// <summary>
    /// <para>职位名称</para>
    /// <para>必填：是（组合更新时必填）</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>职位管理人员（招聘负责人、用人经理、招聘助理）</para>
    /// <para>必填：是（组合更新时必填）</para>
    /// </summary>
    [JsonPropertyName("job_managers")]
    public JobManager? JobManagers { get; set; }

    /// <summary>
    /// <para>招聘流程 ID，可通过获取招聘流程信息接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_process_id")]
    public string? JobProcessId { get; set; }

    /// <summary>
    /// <para>职位流程类型：1 社招流程 / 2 校招流程</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_type")]
    public int? ProcessType { get; set; }

    /// <summary>
    /// <para>职位科目 ID，可通过获取科目列表接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subject_id")]
    public string? SubjectId { get; set; }

    /// <summary>
    /// <para>职能分类 ID，可通过获取职位职能分类列表接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_function_id")]
    public string? JobFunctionId { get; set; }

    /// <summary>
    /// <para>部门 ID，与入参 department_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>招聘数量</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("head_count")]
    public long? HeadCount { get; set; }

    /// <summary>
    /// <para>是否长期有效：true 长期有效 / false 指定到期日期</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_never_expired")]
    public bool? IsNeverExpired { get; set; }

    /// <summary>
    /// <para>最高薪资，单位：k</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("max_salary")]
    public long? MaxSalary { get; set; }

    /// <summary>
    /// <para>职位要求</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("requirement")]
    public string? Requirement { get; set; }

    /// <summary>
    /// <para>工作地点 ID，可通过获取地址列表接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("address_id")]
    public string? AddressId { get; set; }

    /// <summary>
    /// <para>职位描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>职位亮点 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("highlight_list")]
    public string[]? HighlightList { get; set; }

    /// <summary>
    /// <para>职位类别 ID，可通过获取职位类别列表接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_type_id")]
    public string? JobTypeId { get; set; }

    /// <summary>
    /// <para>最高职级 ID，与入参 job_level_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("max_level_id")]
    public string? MaxLevelId { get; set; }

    /// <summary>
    /// <para>雇佣类型 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recruitment_type_id")]
    public string? RecruitmentTypeId { get; set; }

    /// <summary>
    /// <para>学历要求：1 初中及以下 / 2 初中以上 / 3 中专及以上 / 4 高中及以上 / 5 大专及以上 / 6 本科及以上 / 7 硕士及以上 / 8 博士及以上 / 20 不限</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("required_degree")]
    public int? RequiredDegree { get; set; }

    /// <summary>
    /// <para>职位序列 ID，与入参 job_family_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_category_id")]
    public string? JobCategoryId { get; set; }

    /// <summary>
    /// <para>工作地点 ID 列表（可选多个工作地点，若为职位主地址则须包含 address_id）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("address_id_list")]
    public string[]? AddressIdList { get; set; }

    /// <summary>
    /// <para>职位属性：1 实体职位 / 2 虚拟职位</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_attribute")]
    public int? JobAttribute { get; set; }

    /// <summary>
    /// <para>到期日期的毫秒时间戳（推荐使用）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("expiry_timestamp")]
    public string? ExpiryTimestamp { get; set; }

    /// <summary>
    /// <para>面试登记表 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("interview_registration_schema_id")]
    public string? InterviewRegistrationSchemaId { get; set; }

    /// <summary>
    /// <para>入职登记表 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("onboard_registration_schema_id")]
    public string? OnboardRegistrationSchemaId { get; set; }

    /// <summary>
    /// <para>目标专业 ID 列表，"0" 表示不限专业</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("target_major_id_list")]
    public string[]? TargetMajorIdList { get; set; }

    /// <summary>
    /// <para>官网申请表 ID，可通过获取招聘官网申请表模板列表接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("portal_website_apply_form_schema_id")]
    public string? PortalWebsiteApplyFormSchemaId { get; set; }
}
