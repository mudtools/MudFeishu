// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 招聘需求信息（创建招聘需求/获取招聘需求信息/获取招聘需求列表响应）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class JobRequirement
{
    /// <summary>
    /// <para>招聘需求 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>招聘需求编号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("short_code")]
    public string? ShortCode { get; set; }

    /// <summary>
    /// <para>需求名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>需求状态：1 待启动 / 2 进行中 / 3 已取消 / 4 已暂停 / 5 已完成 / 6 已过期</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("display_progress")]
    public int? DisplayProgress { get; set; }

    /// <summary>
    /// <para>需求人数</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("head_count")]
    public int? HeadCount { get; set; }

    /// <summary>
    /// <para>职位性质（即将下线，建议使用 employee_type）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recruitment_type")]
    public IdNameObject? RecruitmentType { get; set; }

    /// <summary>
    /// <para>人员类型，可通过获取人员类型列表接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employee_type")]
    public IdNameObject? EmployeeType { get; set; }

    /// <summary>
    /// <para>最高职级</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("max_level")]
    public IdNameObject? MaxLevel { get; set; }

    /// <summary>
    /// <para>最低职级</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("min_level")]
    public IdNameObject? MinLevel { get; set; }

    /// <summary>
    /// <para>职位序列</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sequence")]
    public IdNameObject? Sequence { get; set; }

    /// <summary>
    /// <para>需求类型：1 新增 / 2 补充</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("category")]
    public int? Category { get; set; }

    /// <summary>
    /// <para>需求部门，ID 与入参 department_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department")]
    public IdNameObject? Department { get; set; }

    /// <summary>
    /// <para>需求负责人，ID 与入参 user_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recruiter_list")]
    public IdNameObject[]? RecruiterList { get; set; }

    /// <summary>
    /// <para>需求用人经理，ID 与入参 user_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("jr_hiring_managers")]
    public IdNameObject[]? JrHiringManagers { get; set; }

    /// <summary>
    /// <para>直属上级，ID 与入参 user_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("direct_leader_list")]
    public IdNameObject[]? DirectLeaderList { get; set; }

    /// <summary>
    /// <para>开始日期，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>预计完成日期，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("deadline")]
    public string? Deadline { get; set; }

    /// <summary>
    /// <para>招聘优先级：1 高 / 2 中 / 3 低</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    /// <summary>
    /// <para>学历要求：1 小学及以上 / 2 初中及以上 / 3 中专及以上 / 4 高中及以上 / 5 大专及以上 / 6 本科及以上 / 7 硕士及以上 / 8 博士及以上 / 20 不限</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("required_degree")]
    public int? RequiredDegree { get; set; }

    /// <summary>
    /// <para>最高薪资，单位：K</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("max_salary")]
    public string? MaxSalary { get; set; }

    /// <summary>
    /// <para>最低薪资，单位：K</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("min_salary")]
    public string? MinSalary { get; set; }

    /// <summary>
    /// <para>工作地点</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("address")]
    public IdNameObject? Address { get; set; }

    /// <summary>
    /// <para>需求描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>自定义字段，请参考获取招聘需求模板接口中的自定义字段定义</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data_list")]
    public JobRequirementCustomizedDataDto[]? CustomizedDataList { get; set; }

    /// <summary>
    /// <para>关联职位 ID 列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_id_list")]
    public string[]? JobIdList { get; set; }

    /// <summary>
    /// <para>招聘类型：1 社会招聘 / 2 校园招聘</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_type")]
    public int? ProcessType { get; set; }

    /// <summary>
    /// <para>职位类别，可通过获取职位类别列表接口获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_type")]
    public JobTypeInfo? JobType { get; set; }

    /// <summary>
    /// <para>创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>创建人 ID，与入参 user_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator_id")]
    public string? CreatorId { get; set; }

    /// <summary>
    /// <para>更新时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>职务 ID（仅限飞书人事租户使用）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employment_job_id")]
    public string? EmploymentJobId { get; set; }

    /// <summary>
    /// <para>岗位 ID（仅限飞书人事租户使用）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("position_id")]
    public string? PositionId { get; set; }

    /// <summary>
    /// <para>完成时间，毫秒时间戳，仅当需求状态为「已完成」时返回（灰度范围内租户可见）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("completion_time")]
    public string? CompletionTime { get; set; }

    /// <summary>
    /// <para>审批状态：1 未发起 / 2 审批中 / 3 已通过 / 4 已撤回 / 5 审批驳回（灰度范围内租户可见）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approval_status")]
    public int? ApprovalStatus { get; set; }

    /// <summary>
    /// <para>招聘需求招聘进展统计（按 ID 批量查询接口返回）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("count_data")]
    public JrCountDataInfo? CountData { get; set; }
}
