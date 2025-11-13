namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 创建用户请求体。
/// </summary>
public class CreateUserRequest : UserData
{
    /// <summary>
    /// 手机号。
    /// </summary>
    [JsonPropertyName("mobile")]
    public new required string Mobile { get; set; }

    /// <summary>
    /// 手机号码是否对其他员工可见。
    /// </summary>
    [JsonPropertyName("mobile_visible")]
    public bool MobileVisible { get; set; } = true;

    /// <summary>
    /// 性别。
    /// 可选值有： 0：保密 1：男 2：女 3：其他
    /// </summary>
    [JsonPropertyName("gender")]
    public int Gender { get; set; }

    /// <summary>
    /// 头像的文件 Key。可以通过上传图片接口，上传并获取头像文件 Key，上传时图片类型需要选择 用于设置头像。
    /// </summary>
    [JsonPropertyName("avatar_key")]
    public string? AvatarKey { get; set; }

    /// <summary>
    /// 用户所属部门的 ID 列表。
    /// </summary>
    [JsonPropertyName("department_ids")]
    public List<string> DepartmentIds { get; set; } = [];

    /// <summary>
    /// 用户的直接主管的用户 ID，ID 类型与查询参数 user_id_type 保持一致。用户 ID 获取方式可参见如何获取不同的用户 ID。
    /// </summary>
    [JsonPropertyName("leader_user_id")]
    public string? LeaderUserId { get; set; }

    /// <summary>
    /// 工作城市。字符长度上限为 100。
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// 国家或地区 Code 缩写。具体写入格式参考 国家/地区 Code 参照表。
    /// <para>具体写入格式参考:</para>
    /// <para>https://open.feishu.cn/document/server-docs/contact-v3/user/country-code-description</para>
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// 工位。字符长度上限为 255。
    /// </summary>
    [JsonPropertyName("work_station")]
    public string? WorkStation { get; set; }

    /// <summary>
    /// 入职时间。秒级时间戳格式，表示从 1970 年 1 月 1 日开始所经过的秒数。如果不传入该参数，则默认填充当前请求时的时间。
    /// </summary>
    [JsonPropertyName("join_time")]
    public long? JoinTime { get; set; }

    /// <summary>
    /// 工号。字符长度上限为 255。
    /// </summary>
    [JsonPropertyName("employee_no")]
    public new string? EmployeeNo { get; set; }
    /// <summary>
    /// 员工类型。 可选值有：1：正式员工  2：实习生  3：外包  4：劳务  5：顾问
    /// </summary>
    [JsonPropertyName("employee_type")]
    public int? EmployeeType { get; set; }

    /// <summary>
    /// 用户排序信息列表。
    /// </summary>
    [JsonPropertyName("orders")]
    public List<UserOrder> Orders { get; set; } = [];

    /// <summary>
    /// 自定义字段。
    /// </summary>
    [JsonPropertyName("custom_attrs")]
    public List<CustomAttribute> CustomAttrs { get; set; } = [];

    /// <summary>
    /// 企业邮箱。
    /// </summary>
    [JsonPropertyName("enterprise_email")]
    public new string? EnterpriseEmail { get; set; }

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
    /// 分配给用户的席位 ID 列表。
    /// </summary>
    [JsonPropertyName("subscription_ids")]
    public List<string> SubscriptionIds { get; set; } = [];

    /// <summary>
    /// 虚线上级的用户 ID 列表。
    /// </summary>
    [JsonPropertyName("dotted_line_leader_user_ids")]
    public List<string> DottedLineLeaderUserIds { get; set; } = [];
}



