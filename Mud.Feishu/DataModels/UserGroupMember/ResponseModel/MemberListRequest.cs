// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
