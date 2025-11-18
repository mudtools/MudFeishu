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
