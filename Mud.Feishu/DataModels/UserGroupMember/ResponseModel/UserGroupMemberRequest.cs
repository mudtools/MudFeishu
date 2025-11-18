namespace Mud.Feishu.DataModels.UserGroupMember;

/// <summary>
/// 用户组成员信息
/// </summary>
public class UserGroupMemberInffo
{
    /// <summary>
    /// 用户组成员的类型，目前仅支持选择 user。
    /// </summary>
    [JsonPropertyName("member_type")]
    public string MemberType { get; set; } = "user";

    /// <summary>
    /// 当 member_type 取值为 user时，通过该参数设置用户 ID 类型。
    /// </summary>
    [JsonPropertyName("member_id_type")]
    public string MemberIdType { get; set; } = "open_id";

    /// <summary>
    /// 添加的用户 ID，ID 类型与 member_id_type 的取值保持一致。
    /// </summary>
    [JsonPropertyName("member_id")]
    public string MemberId { get; set; } = string.Empty;
}
