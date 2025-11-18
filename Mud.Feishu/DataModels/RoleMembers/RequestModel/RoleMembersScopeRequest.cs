namespace Mud.Feishu.DataModels.RoleMembers;

/// <summary>
/// 角色成员管理范围请求体
/// </summary>
public class RoleMembersScopeRequest
{
    /// <summary>
    /// 角色成员的用户 ID 列表，以 ["xxx", "yyy"] 数组格式进行传值。ID 类型需要和查询参数 user_id_type 的取值保持一致。
    /// </summary>
    [JsonPropertyName("members")]
    public List<string> Members { get; set; } = [];

    /// <summary>
    /// 设置角色成员可管理的部门范围（部门 ID 列表），以 ["xxx", "yyy"] 数组格式进行传值。ID 类型需要和查询参数 department_id_type 的取值保持一致。
    /// </summary>
    [JsonPropertyName("departments")]
    public List<string> Departments { get; set; } = [];
}
