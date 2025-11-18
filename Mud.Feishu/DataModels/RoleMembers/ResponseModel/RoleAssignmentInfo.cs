namespace Mud.Feishu.DataModels.RoleMembers;

public class RoleAssignmentInfo
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("reason")]
    public int Reason { get; set; }
}
