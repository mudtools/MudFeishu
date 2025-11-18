namespace Mud.Feishu.DataModels.RoleMembers;

/// <summary>
/// 角色分配信息模型，包含角色分配给用户的详细记录。
/// 用于表示用户被分配到特定角色的操作记录和结果。
/// </summary>
public class RoleAssignmentInfo
{
    /// <summary>
    /// 用户ID。
    /// <para>表示被分配角色的用户唯一标识符。</para>
    /// <para>可以是 open_id、user_id 或其他用户标识类型。</para>
    /// <para>示例值："ou_7d8a6e9d3c2c1b882487c7398e9d8f7"</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 分配原因代码。
    /// <para>表示角色分配的操作原因或结果状态。</para>
    /// <para>常见值：0（成功）、1（已存在）、2（失败）等。</para>
    /// <para>可用于判断角色分配的具体结果和处理后续逻辑。</para>
    /// <para>示例值：0（分配成功）</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public int Reason { get; set; }
}
