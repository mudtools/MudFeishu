namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 用户查询结果列表。
/// </summary>
public class UserSearchListResult : ApiListResult
{
    /// <summary>
    /// 用户查询结果列表。
    /// </summary>
    [JsonPropertyName("users")]
    public List<UserSearchResult> Users { get; set; } = [];
}
