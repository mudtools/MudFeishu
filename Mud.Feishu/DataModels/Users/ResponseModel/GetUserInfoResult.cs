namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 获取用户详细信息的返回结果数组
/// </summary>
public class GetUserInfosResult
{
    /// <summary>
    /// 获取用户详细信息的返回结果数组。
    /// </summary>
    [JsonPropertyName("items")]
    public GetUserInfoResult[] Items { get; set; }
}

/// <summary>
/// 获取用户详细信息的返回结果。
/// </summary>
public class GetUserInfoResult : UserData
{
    /// <summary>
    /// 用户在飞书开放平台下的唯一标识。
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    /// <summary>
    /// 用户在当前应用中的唯一标识。
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// 手机号码是否对其他员工可见。
    /// </summary>
    [JsonPropertyName("mobile_visible")]
    public bool MobileVisible { get; set; }

    /// <summary>
    /// 性别。
    /// 可选值有：0：保密 1：男 2：女 3：其他
    /// </summary>
    [JsonPropertyName("gender")]
    public int Gender { get; set; }

    /// <summary>
    /// 头像信息。
    /// </summary>
    [JsonPropertyName("avatar")]
    public AvatarInfo? Avatar { get; set; }

    /// <summary>
    /// 用户状态信息。
    /// </summary>
    [JsonPropertyName("status")]
    public UserStatus? Status { get; set; }

    /// <summary>
    /// 用户所属部门的 ID 列表。
    /// </summary>
    [JsonPropertyName("department_ids")]
    public List<string> DepartmentIds { get; set; } = [];

    /// <summary>
    /// 用户的直接主管的用户 ID。
    /// </summary>
    [JsonPropertyName("leader_user_id")]
    public string? LeaderUserId { get; set; }

    /// <summary>
    /// 工作城市。字符长度上限为 100。
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// 国家或地区 Code 缩写。
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// 工位。字符长度上限为 255。
    /// </summary>
    [JsonPropertyName("work_station")]
    public string? WorkStation { get; set; }

    /// <summary>
    /// 入职时间。秒级时间戳格式。
    /// </summary>
    [JsonPropertyName("join_time")]
    public long JoinTime { get; set; }

    /// <summary>
    /// 是否为租户管理员。
    /// </summary>
    [JsonPropertyName("is_tenant_manager")]
    public bool IsTenantManager { get; set; }

    /// <summary>
    /// 员工类型。 可选值有：1：正式员工 2：实习生 3：外包 4：劳务 5：顾问
    /// </summary>
    [JsonPropertyName("employee_type")]
    public int EmployeeType { get; set; }

    /// <summary>
    /// 用户排序信息列表。
    /// </summary>
    [JsonPropertyName("orders")]
    public List<DepartmentOrder> Orders { get; set; } = [];

    /// <summary>
    /// 自定义字段。
    /// </summary>
    [JsonPropertyName("custom_attrs")]
    public List<CustomAttribute> CustomAttrs { get; set; } = [];

    /// <summary>
    /// 职务名称。字符数量上限为 255。
    /// </summary>
    [JsonPropertyName("job_title")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// 数据驻留地。
    /// </summary>
    [JsonPropertyName("geo")]
    public string? Geo { get; set; }

    /// <summary>
    /// 职级 ID。
    /// </summary>
    [JsonPropertyName("job_level_id")]
    public string? JobLevelId { get; set; }

    /// <summary>
    /// 序列 ID。
    /// </summary>
    [JsonPropertyName("job_family_id")]
    public string? JobFamilyId { get; set; }

    /// <summary>
    /// 分配信息。
    /// </summary>
    [JsonPropertyName("assign_info")]
    public List<AssignInfo> AssignInfo { get; set; } = [];

    /// <summary>
    /// 部门路径。
    /// </summary>
    [JsonPropertyName("department_path")]
    public List<DepartmentPath> DepartmentPath { get; set; } = [];

    /// <summary>
    /// 虚线上级的用户 ID 列表。
    /// </summary>
    [JsonPropertyName("dotted_line_leader_user_ids")]
    public List<string> DottedLineLeaderUserIds { get; set; } = [];
}
