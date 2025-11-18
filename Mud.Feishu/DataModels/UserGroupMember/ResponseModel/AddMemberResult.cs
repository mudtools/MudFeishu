namespace Mud.Feishu.DataModels.UserGroupMember;

public class AddMemberResult
{
    [JsonPropertyName("member_id")]
    public string MemberId { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }
}
