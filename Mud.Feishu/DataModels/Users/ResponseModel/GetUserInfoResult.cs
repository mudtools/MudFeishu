namespace Mud.Feishu.DataModels.Users;

public class GetUserInfoResult
{
    [JsonPropertyName("union_id")]
    public string UnionId { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("open_id")]
    public string OpenId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("en_name")]
    public string EnName { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("mobile")]
    public string Mobile { get; set; }

    [JsonPropertyName("mobile_visible")]
    public bool MobileVisible { get; set; }

    [JsonPropertyName("gender")]
    public int Gender { get; set; }

    [JsonPropertyName("avatar")]
    public AvatarInfo Avatar { get; set; }

    [JsonPropertyName("status")]
    public UserStatus Status { get; set; }

    [JsonPropertyName("department_ids")]
    public List<string> DepartmentIds { get; set; }

    [JsonPropertyName("leader_user_id")]
    public string LeaderUserId { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; }

    [JsonPropertyName("work_station")]
    public string WorkStation { get; set; }

    [JsonPropertyName("join_time")]
    public long JoinTime { get; set; }

    [JsonPropertyName("is_tenant_manager")]
    public bool IsTenantManager { get; set; }

    [JsonPropertyName("employee_no")]
    public string EmployeeNo { get; set; }

    [JsonPropertyName("employee_type")]
    public int EmployeeType { get; set; }

    [JsonPropertyName("orders")]
    public List<DepartmentOrder> Orders { get; set; }

    [JsonPropertyName("custom_attrs")]
    public List<CustomAttribute> CustomAttrs { get; set; }

    [JsonPropertyName("enterprise_email")]
    public string EnterpriseEmail { get; set; }

    [JsonPropertyName("job_title")]
    public string JobTitle { get; set; }

    [JsonPropertyName("geo")]
    public string Geo { get; set; }

    [JsonPropertyName("job_level_id")]
    public string JobLevelId { get; set; }

    [JsonPropertyName("job_family_id")]
    public string JobFamilyId { get; set; }

    [JsonPropertyName("assign_info")]
    public List<AssignInfo> AssignInfo { get; set; }

    [JsonPropertyName("department_path")]
    public List<DepartmentPath> DepartmentPath { get; set; }

    [JsonPropertyName("dotted_line_leader_user_ids")]
    public List<string> DottedLineLeaderUserIds { get; set; }
}
