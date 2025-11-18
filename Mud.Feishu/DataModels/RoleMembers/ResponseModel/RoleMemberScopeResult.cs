namespace Mud.Feishu.DataModels.RoleMembers;

public class RoleMemberScopeResult
{
    [JsonPropertyName("member")]
    public RoleMemberScopeInfo Member { get; set; }
}
