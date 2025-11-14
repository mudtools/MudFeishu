namespace Mud.Feishu.DataModels.UserGroup;

/// <summary>
/// 用户组创建结果。
/// </summary>
public class UserGroupCreateResult
{
    /// <summary>
    /// 用户组 ID。可使用该 ID 更新、删除、查询用户组。
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }
}
