namespace Mud.Feishu.DataModels.UserGroupMember;

/// <summary>
/// 批量添加成员结果响应模型，包含批量添加用户组成员操作的详细结果。
/// 用于批量添加操作中每个成员的添加结果信息。
/// </summary>
public class BatchAddMemberResult
{
    /// <summary>
    /// 批量操作结果列表。
    /// <para>包含每个成员的添加操作结果。</para>
    /// <para>列表中的每个元素对应一个成员的添加结果，包含成员ID和操作代码。</para>
    /// <para>可用于检查哪些成员添加成功，哪些失败。</para>
    /// </summary>
    [JsonPropertyName("results")]
    public List<AddMemberResult> Results { get; set; } = new List<AddMemberResult>();
}
