namespace Mud.Feishu.DataModels.Roles;

/// <summary>
/// 角色创建结果。
/// </summary>
public class RoleCreateResult
{
    /// <summary>
    /// 角色ID。
    /// </summary>
    [JsonPropertyName("role_id")]
    public string? RoleId { get; set; }
}