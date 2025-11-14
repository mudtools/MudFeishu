namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 用户搜索结果信息。
/// </summary>
public class UserSearchResult
{
    /// <summary>
    /// 用户的头像信息。
    /// </summary>
    [JsonPropertyName("avatar")]
    public AvatarInfo? Avatar { get; set; }

    /// <summary>
    /// 用户所在的部门 ID 列表。
    /// </summary>
    [JsonPropertyName("department_ids")]
    public List<string> DepartmentIds { get; set; } = [];

    /// <summary>
    /// 用户名。
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 用户的 open_id。
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// 用户的 user_id。
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
