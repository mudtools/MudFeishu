namespace Mud.Feishu.DataModels.UserGroupMember;

public class BatchAddMemberResult
{
    [JsonPropertyName("results")]
    public List<AddMemberResult> Results { get; set; }
}
