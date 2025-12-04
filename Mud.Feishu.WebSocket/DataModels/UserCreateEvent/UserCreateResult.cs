// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket.DataModels.UserCreateEvent;

/// <summary>
/// 用户创建事件结果类，用于表示飞书平台中用户创建事件的相关信息
/// </summary>
public class UserCreateResult : IEventResult
{
    /// <summary>
    /// 用户在当前应用中的唯一标识
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// 用户在飞书开放平台下的唯一标识
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// 用户姓名
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 用户英文名
    /// </summary>
    [JsonPropertyName("en_name")]
    public string? EnName { get; set; }

    /// <summary>
    /// 用户昵称
    /// </summary>
    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    /// <summary>
    /// 用户邮箱地址
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// 企业邮箱地址
    /// </summary>
    [JsonPropertyName("enterprise_email")]
    public string? EnterpriseEmail { get; set; }

    /// <summary>
    /// 用户职务
    /// </summary>
    [JsonPropertyName("job_title")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// 用户手机号码
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// 用户性别，0：保密，1：男，2：女
    /// </summary>
    [JsonPropertyName("gender")]
    public int Gender { get; set; }

    /// <summary>
    /// 用户头像信息
    /// </summary>
    [JsonPropertyName("avatar")]
    public UserAvatar? Avatar { get; set; }

    /// <summary>
    /// 用户状态信息
    /// </summary>
    [JsonPropertyName("status")]
    public UserStatus? Status { get; set; }

    /// <summary>
    /// 用户所属部门ID列表
    /// </summary>
    [JsonPropertyName("department_ids")]
    public List<string>? DepartmentIds { get; set; }

    /// <summary>
    /// 直属领导的用户ID
    /// </summary>
    [JsonPropertyName("leader_user_id")]
    public string? LeaderUserId { get; set; }

    /// <summary>
    /// 用户所在城市
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// 用户所在国家
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// 工位信息
    /// </summary>
    [JsonPropertyName("work_station")]
    public string? WorkStation { get; set; }

    /// <summary>
    /// 入职时间（时间戳）
    /// </summary>
    [JsonPropertyName("join_time")]
    public long JoinTime { get; set; }

    /// <summary>
    /// 员工工号
    /// </summary>
    [JsonPropertyName("employee_no")]
    public string? EmployeeNo { get; set; }

    /// <summary>
    /// 员工类型，1：正式员工，2：实习生，3：外包，4：劳务，5：顾问
    /// </summary>
    [JsonPropertyName("employee_type")]
    public int EmployeeType { get; set; }

    /// <summary>
    /// 用户排序信息列表
    /// </summary>
    [JsonPropertyName("orders")]
    public List<UserOrder>? Orders { get; set; }

    /// <summary>
    /// 用户自定义属性列表
    /// </summary>
    [JsonPropertyName("custom_attrs")]
    public List<UserCustomAttr>? CustomAttrs { get; set; }

    /// <summary>
    /// 职级ID
    /// </summary>
    [JsonPropertyName("job_level_id")]
    public string? JobLevelId { get; set; }

    /// <summary>
    /// 职位序列ID
    /// </summary>
    [JsonPropertyName("job_family_id")]
    public string? JobFamilyId { get; set; }

    /// <summary>
    /// 虚线领导用户ID列表
    /// </summary>
    [JsonPropertyName("dotted_line_leader_user_ids")]
    public List<string>? DottedLineLeaderUserIds { get; set; }
}