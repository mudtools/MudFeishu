namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 用户查询结果列表。
/// </summary>
public class UserQueryListResult
{
    /// <summary>
    /// 用户查询结果列表。
    /// </summary>
    [JsonPropertyName("user_list")]
    public List<UserQueryResult> UserList { get; set; } = [];
}
