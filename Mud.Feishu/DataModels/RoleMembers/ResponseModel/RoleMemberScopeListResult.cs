namespace Mud.Feishu.DataModels.RoleMembers;

public class RoleMemberScopeListResult : ListApiResult
{
    [JsonPropertyName("members")]
    public List<RoleMemberScopeInfo> Member { get; set; }
}
