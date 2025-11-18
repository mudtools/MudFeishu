namespace Mud.Feishu.DataModels.UserGroupMember;

/// <summary>
/// 用户组批量添加成员请求体
/// </summary>
public class BatchMembersRequest
{
    /// <summary>
    /// 待添加成员信息。
    /// </summary>
    [JsonPropertyName("members")]
    public List<UserGroupMemberRequest> Members { get; set; } = [];
}
