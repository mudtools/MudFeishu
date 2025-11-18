namespace Mud.Feishu.DataModels.RoleMembers;

/// <summary>
/// 角色成员权限范围列表响应结果，包含多个角色成员的权限范围信息。
/// 用于批量获取或设置角色成员权限范围时的响应数据格式。
/// </summary>
public class RoleMemberScopeListResult : ApiListResult
{
    /// <summary>
    /// 角色成员权限范围列表。
    /// <para>包含所有角色成员的权限范围信息集合。</para>
    /// <para>每个成员包含用户ID、权限范围类型和具体部门列表。</para>
    /// <para>支持分页查询和批量操作。</para>
    /// </summary>
    [JsonPropertyName("members")]
    public List<RoleMemberScopeInfo> Member { get; set; } = new List<RoleMemberScopeInfo>();
}
