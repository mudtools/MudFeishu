namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 用户查询结果列表。
/// </summary>
public class UserSearchListResult : ListApiResult
{
    /// <summary>
    /// 用户查询结果列表。
    /// </summary>
    [JsonPropertyName("users")]
    public List<UserSearchResult> Users { get; set; } = [];
}
