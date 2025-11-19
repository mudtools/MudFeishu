// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 员工基本信息类，包含员工的核心个人信息
/// </summary>
public class EmployeeBaseInfo
{
    /// <summary>
    /// 员工ID
    /// </summary>
    [JsonPropertyName("employee_id")]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// 员工姓名信息
    /// </summary>
    [JsonPropertyName("name")]
    public EmployeeName? Name { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// 邮箱地址
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// 企业邮箱地址
    /// </summary>
    [JsonPropertyName("enterprise_email")]
    public string? EnterpriseEmail { get; set; }

    /// <summary>
    /// 性别 (0: 未设置, 1: 男, 2: 女)
    /// </summary>
    [JsonPropertyName("gender")]
    public int Gender { get; set; }

    /// <summary>
    /// 所属部门列表
    /// </summary>
    [JsonPropertyName("departments")]
    public List<EmployeeDepartmentDetail> Departments { get; set; } = [];

    /// <summary>
    /// 员工在部门中的排序信息
    /// </summary>
    [JsonPropertyName("employee_order_in_departments")]
    public List<EmployeDepartmentOrder> EmployeeOrderInDepartments { get; set; } = [];

    /// <summary>
    /// 员工描述信息
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 激活状态
    /// </summary>
    [JsonPropertyName("active_status")]
    public string? ActiveStatus { get; set; }

    /// <summary>
    /// 是否已离职
    /// </summary>
    [JsonPropertyName("is_resigned")]
    public bool IsResigned { get; set; }

    /// <summary>
    /// 直属领导ID
    /// </summary>
    [JsonPropertyName("leader_id")]
    public string? LeaderId { get; set; }

    /// <summary>
    /// 虚线领导ID列表
    /// </summary>
    [JsonPropertyName("dotted_line_leader_ids")]
    public List<string> DottedLineLeaderIds { get; set; } = [];

    /// <summary>
    /// 是否为主管理员
    /// </summary>
    [JsonPropertyName("is_primary_admin")]
    public bool IsPrimaryAdmin { get; set; }

    /// <summary>
    /// 企业邮箱别名列表
    /// </summary>
    [JsonPropertyName("enterprise_email_aliases")]
    public List<string> EnterpriseEmailAliases { get; set; } = [];

    /// <summary>
    /// 自定义字段值列表
    /// </summary>
    [JsonPropertyName("custom_field_values")]
    public List<CustomFieldValue> CustomFieldValues { get; set; } = [];

    /// <summary>
    /// 部门路径信息列表
    /// </summary>
    [JsonPropertyName("department_path_infos")]
    public List<List<EmployeeDepartmentPathInfo>> DepartmentPathInfos { get; set; } = [];

    /// <summary>
    /// 离职时间
    /// </summary>
    [JsonPropertyName("resign_time")]
    public string? ResignTime { get; set; }

    /// <summary>
    /// 头像信息
    /// </summary>
    [JsonPropertyName("avatar")]
    public AvatarInfo? Avatar { get; set; }

    /// <summary>
    /// 背景图片URL
    /// </summary>
    [JsonPropertyName("background_image")]
    public string? BackgroundImage { get; set; }

    /// <summary>
    /// 是否为管理员
    /// </summary>
    [JsonPropertyName("is_admin")]
    public bool IsAdmin { get; set; }

    /// <summary>
    /// 数据来源 (1: 飞书, 2: 企业微信, 3: 手动录入)
    /// </summary>
    [JsonPropertyName("data_source")]
    public int DataSource { get; set; }

    /// <summary>
    /// 地理位置名称
    /// </summary>
    [JsonPropertyName("geo_name")]
    public string? GeoName { get; set; }

    /// <summary>
    /// 订阅ID列表
    /// </summary>
    [JsonPropertyName("subscription_ids")]
    public List<string> SubscriptionIds { get; set; } = [];
}