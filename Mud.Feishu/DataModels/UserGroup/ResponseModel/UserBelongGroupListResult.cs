namespace Mud.Feishu.DataModels.UserGroup;

/// <summary>
/// 用户所属用户组列表结果，包含用户所属的用户组ID列表和分页信息。
/// </summary>
public class UserBelongGroupListResult : ListApiResult
{
    /// <summary>
    /// 用户所属的用户组ID列表。
    /// </summary>
    [JsonPropertyName("group_list")]
    public List<string> GroupList { get; set; } = [];
}
