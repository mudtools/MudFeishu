namespace Mud.Feishu.DataModels.UserGroupMember;

/// <summary>
/// 添加成员结果响应模型，包含用户组成员添加操作的结果信息。
/// 用于单个用户添加到用户组操作成功后返回的详细信息。
/// </summary>
public class AddMemberResult
{
    /// <summary>
    /// 成员ID。
    /// <para>表示成功添加到用户组的成员唯一标识符。</para>
    /// <para>可以是用户ID、部门ID或其他类型的成员标识。</para>
    /// <para>示例值："ou_7d8a6e9d3c2c1b882487c7398e9d8f7"</para>
    /// </summary>
    [JsonPropertyName("member_id")]
    public string MemberId { get; set; } = string.Empty;

    /// <summary>
    /// 操作结果代码。
    /// <para>表示添加成员操作的执行结果状态。</para>
    /// <para>0表示成功，非0值表示失败或警告。</para>
    /// <para>示例值：0（成功）</para>
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }
}
