// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TrustParty;

/// <summary>
/// 关联组织成员详情
/// </summary>
[HttpJsonSerializable(SerializerClassName = "TrustParty")]
public class CollaborationUser
{
    /// <summary>
    /// 对方关联组织用户的open_id
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// 对方关联组织用户的id
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// 对方关联组织用户的union id
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    /// <summary>
    /// 用户的名称
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 目标组织的 i18n 用户名称
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public I18nName? I18nName { get; set; }

    /// <summary>
    /// 用户头像信息
    /// </summary>
    [JsonPropertyName("avatar")]
    public AvatarInfo? Avatar { get; set; }

    /// <summary>
    /// 手机号，需要对方租户授权展示
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    /// <summary>
    /// 用户状态
    /// </summary>
    [JsonPropertyName("status")]
    public UserStatus? Status { get; set; }

    /// <summary>
    /// 用户所属部门的ID列表（已废弃）
    /// </summary>
    [JsonPropertyName("department_ids")]
    public string[]? DepartmentIds { get; set; }

    /// <summary>
    /// 用户的直接主管的用户ID（已废弃）
    /// </summary>
    [JsonPropertyName("leader_user_id")]
    public string? LeaderUserId { get; set; }

    /// <summary>
    /// 职务，需要对方租户授权展示
    /// </summary>
    [JsonPropertyName("job_title")]
    public string? JobTitle { get; set; }

    /// <summary>
    /// 自定义属性列表，需要对方租户授权展示
    /// </summary>
    [JsonPropertyName("custom_attrs")]
    public UserCustomAttr[]? CustomAttrs { get; set; }

    /// <summary>
    /// 工号，需要对方租户授权展示
    /// </summary>
    [JsonPropertyName("employee_no")]
    public string? EmployeeNo { get; set; }

    /// <summary>
    /// 父部门ID列表，必须对父部门有可见性权限才会返回
    /// </summary>
    [JsonPropertyName("parent_department_ids")]
    public CollaborationDepartmentId[]? ParentDepartmentIds { get; set; }

    /// <summary>
    /// 用户的 leader 信息，必须对 leader 有可见性权限才会返回
    /// </summary>
    [JsonPropertyName("leader_id")]
    public CollaborationUserId? LeaderId { get; set; }
}
