namespace Mud.Feishu.DataModels.RoleMembers;

/// <summary>
/// 角色成员权限范围信息模型，包含角色成员的权限范围详情。
/// 用于表示角色成员在指定范围内的权限配置。
/// </summary>
public class RoleMemberScopeInfo
{
    /// <summary>
    /// 用户ID。
    /// <para>表示角色成员的唯一标识符。</para>
    /// <para>可以是 open_id、user_id 或其他用户标识类型。</para>
    /// <para>示例值："ou_7d8a6e9d3c2c1b882487c7398e9d8f7"</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 权限范围类型。
    /// <para>表示角色成员的权限范围类型。</para>
    /// <para>常见值："department"（部门范围）、"all"（全部范围）等。</para>
    /// <para>示例值："department"</para>
    /// </summary>
    [JsonPropertyName("scope_type")]
    public string ScopeType { get; set; } = string.Empty;

    /// <summary>
    /// 部门ID列表。
    /// <para>当 scope_type 为 "department" 时，表示权限范围包含的具体部门。</para>
    /// <para>用于限定角色成员在特定部门范围内的权限。</para>
    /// <para>示例值：["od-4e6789c92a3c8e02dbe89d3f9b87c", "od-8f9a2b1c4d3e9f7c3d8e7a0b9f6c"]</para>
    /// </summary>
    [JsonPropertyName("department_ids")]
    public List<string> DepartmentIds { get; set; } = new List<string>();
}
