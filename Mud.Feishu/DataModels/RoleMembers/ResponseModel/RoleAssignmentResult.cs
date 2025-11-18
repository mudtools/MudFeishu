namespace Mud.Feishu.DataModels.RoleMembers;

public class RoleAssignmentResult
{
    [JsonPropertyName("results")]
    public List<RoleAssignmentInfo> Results { get; set; }
}
