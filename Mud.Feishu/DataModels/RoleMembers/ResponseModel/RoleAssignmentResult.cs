// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.RoleMembers;

/// <summary>
/// 角色分配结果响应模型，包含批量角色分配的详细信息。
/// 用于批量分配角色操作中每个用户的分配结果信息。
/// </summary>
public class RoleAssignmentResult
{
    /// <summary>
    /// 角色分配结果列表。
    /// <para>包含每个用户的角色分配结果。</para>
    /// <para>列表中的每个元素对应一个用户的分配信息，包含用户ID和分配结果代码。</para>
    /// <para>可用于检查哪些用户分配成功，哪些失败。</para>
    /// <para>支持批量角色分配操作的完整结果反馈。</para>
    /// </summary>
    [JsonPropertyName("results")]
    public List<RoleAssignmentInfo> Results { get; set; } = new List<RoleAssignmentInfo>();
}
