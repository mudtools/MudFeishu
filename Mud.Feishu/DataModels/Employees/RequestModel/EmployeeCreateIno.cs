namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 创建员工对象信息
/// </summary>
public class EmployeeCreateInfo
{
    /// <summary>
    /// 姓名
    /// </summary>
    [JsonPropertyName("name")]
    public EmployeeName? Name { get; set; }

    /// <summary>
    /// 员工的手机号，最多可输入 255 字。
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// 企业内在职员工的唯一标识。支持自定义，未自定义时系统自动生成。ID支持修改。
    /// </summary>
    [JsonPropertyName("custom_employee_id")]
    public string? CustomEmployeeId { get; set; }

    /// <summary>
    /// 员工的头像key。
    /// <para>示例值："8abc397a-9950-44ea-9302-e1d8fe00858g"</para>
    /// </summary>
    [JsonPropertyName("avatar_key")]
    public string? AvatarKey { get; set; }

    /// <summary>
    /// 员工在工作中的邮箱。
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// 员工的企业邮箱。
    /// </summary>
    [JsonPropertyName("enterprise_email")]
    public string? EnterpriseEmail { get; set; }

    /// <summary>
    /// 性别:可选值有：0：未知 1：男 2：女 3：其他
    /// </summary>
    [JsonPropertyName("gender")]
    public int? Gender { get; set; }

    /// <summary>
    /// 员工在所属部门内的排序信息。
    /// </summary>
    [JsonPropertyName("employee_order_in_departments")]
    public List<DepartmentOrder>? EmployeeOrderInDepartments { get; set; }

    /// <summary>
    /// 员工的直属上级ID，与employee_id_type类型保持一致。
    /// </summary>
    [JsonPropertyName("leader_id")]
    public string? LeaderId { get; set; }

    /// <summary>
    /// 员工的虚线上级ID，与employee_id_type类型保持一致。
    /// </summary>
    [JsonPropertyName("dotted_line_leader_ids")]
    public List<string>? DottedLineLeaderIds { get; set; }

    /// <summary>
    /// 工作地国家/地区码。
    /// </summary>
    [JsonPropertyName("work_country_or_region")]
    public string? WorkCountryOrRegion { get; set; }

    /// <summary>
    /// 工作地点ID
    /// </summary>
    [JsonPropertyName("work_place_id")]
    public string? WorkPlaceId { get; set; }

    /// <summary>
    /// 工位
    /// </summary>
    [JsonPropertyName("work_station")]
    public EmployeeI18nContent? WorkStation { get; set; }

    /// <summary>
    /// 工号。企业内在职员工的工号不可重复。
    /// </summary>
    [JsonPropertyName("job_number")]
    public string? JobNumber { get; set; }

    /// <summary>
    /// 分机号，最多可输入 99 字。企业内所有员工的分机号不可重复。
    /// </summary>
    [JsonPropertyName("extension_number")]
    public string? ExtensionNumber { get; set; }

    /// <summary>
    /// 入职日期 示例值："2022-10-10
    /// </summary>
    [JsonPropertyName("join_date")]
    public string? JoinDate { get; set; }

    /// <summary>
    /// 员工类型 可选值有：1：全职 2：实习 3：外包 4：劳务 5：顾问
    /// </summary>
    [JsonPropertyName("employment_type")]
    public string? EmploymentType { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("staff_status")]
    public string? StaffStatus { get; set; }

    /// <summary>
    /// 职务ID
    /// </summary>
    [JsonPropertyName("job_title_id")]
    public string? JobTitleId { get; set; }

    /// <summary>
    /// 自定义字段
    /// </summary>
    [JsonPropertyName("custom_field_values")]
    public List<CustomFieldValue>? CustomFieldValues { get; set; }
}
