// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.RoleMembers;

/// <summary>
/// 角色成员权限范围列表响应结果，包含多个角色成员的权限范围信息。
/// 用于批量获取或设置角色成员权限范围时的响应数据格式。
/// </summary>
public class RoleMemberScopeListResult : ApiPageListResult
{
    /// <summary>
    /// 角色成员权限范围列表。
    /// <para>包含所有角色成员的权限范围信息集合。</para>
    /// <para>每个成员包含用户ID、权限范围类型和具体部门列表。</para>
    /// <para>支持分页查询和批量操作。</para>
    /// </summary>
    [JsonPropertyName("members")]
    public List<RoleMemberScopeInfo> Member { get; set; } = new List<RoleMemberScopeInfo>();
}
