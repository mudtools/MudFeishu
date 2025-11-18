using System.Reflection;

namespace Mud.Feishu.DataModels.UserGroupMember;

public class MemberListRequest : ListApiResult
{
    [JsonPropertyName("memberlist")]
    public List<MemberInfo> MemberList { get; set; }
}
