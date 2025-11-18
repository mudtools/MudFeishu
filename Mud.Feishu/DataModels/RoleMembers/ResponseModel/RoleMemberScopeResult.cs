namespace Mud.Feishu.DataModels.RoleMembers;

/// <summary>
/// 角色成员权限范围结果响应模型，包含单个角色成员的权限范围信息。
/// 用于获取或设置角色成员权限范围时的响应数据格式。
/// </summary>
public class RoleMemberScopeResult
{
    /// <summary>
    /// 角色成员权限范围信息。
    /// <para>包含单个角色成员的详细权限范围配置。</para>
    /// <para>包括用户ID、权限范围类型和具体部门列表。</para>
    /// </summary>
    [JsonPropertyName("member")]
    public RoleMemberScopeInfo Member { get; set; } = new RoleMemberScopeInfo();
}
