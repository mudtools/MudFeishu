using System.Reflection;

namespace Mud.Feishu.DataModels.UserGroupMember;

/// <summary>
/// 成员列表请求/响应模型，包含用户组成员列表信息。
/// 用于获取用户组成员列表或批量操作时的成员数据传输。
/// </summary>
public class MemberListRequest : ApiListResult
{
    /// <summary>
    /// 成员列表。
    /// <para>包含用户组中的所有成员信息。</para>
    /// <para>成员类型可以包括用户、部门等不同类型。</para>
    /// <para>每个成员包含基本信息如ID、名称、类型等。</para>
    /// </summary>
    [JsonPropertyName("memberlist")]
    public List<MemberInfo> MemberList { get; set; } = new List<MemberInfo>();
}
