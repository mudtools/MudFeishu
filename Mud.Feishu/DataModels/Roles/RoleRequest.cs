namespace Mud.Feishu.DataModels.Roles;

/// <summary>
/// 角色请求体
/// </summary>
public class RoleRequest
{
    /// <summary>
    /// 角色名称。在同一租户下角色名称唯一，不能重复创建。
    /// </summary>
    [JsonPropertyName("role_name")]
    public required string RoleName { get; set; }
}