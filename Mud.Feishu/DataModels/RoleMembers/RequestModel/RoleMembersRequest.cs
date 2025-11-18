namespace Mud.Feishu.DataModels.RoleMembers;

/// <summary>
/// 角色成员的用户 ID 列表请求体。
/// </summary>
public class RoleMembersRequest
{
    /// <summary>
    /// 待添加为角色成员的用户 ID 列表，以 ["xxx", "yyy"] 数组格式进行传值。
    /// <para>ID 类型需要和查询参数 user_id_type 的取值保持一致。</para>
    /// </summary>
    [JsonPropertyName("members")]
    public List<string> Members { get; set; } = [];
}