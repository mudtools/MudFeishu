// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.RoleMembers;

/// <summary>
/// 角色成员权限范围结果响应模型，包含单个角色成员的权限范围信息。
/// 用于获取或设置角色成员权限范围时的响应数据格式。
/// </summary>
public class RoleMemberScopeResult
{
    /// <summary>
    /// 角色成员权限范围信息。
    /// <para>包含单个角色成员的详细权限范围配置。</para>
    /// <para>包括用户ID、权限范围类型和具体部门列表。</para>
    /// </summary>
    [JsonPropertyName("member")]
    public RoleMemberScopeInfo Member { get; set; } = new RoleMemberScopeInfo();
}
