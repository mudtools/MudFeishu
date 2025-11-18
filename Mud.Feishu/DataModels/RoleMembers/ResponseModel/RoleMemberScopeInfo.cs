namespace Mud.Feishu.DataModels.RoleMembers;

public class RoleMemberScopeInfo
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("scope_type")]
    public string ScopeType { get; set; }

    [JsonPropertyName("department_ids")]
    public List<string> DepartmentIds { get; set; }
}
