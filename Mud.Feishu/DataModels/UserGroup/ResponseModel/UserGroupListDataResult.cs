namespace Mud.Feishu.DataModels.UserGroup;

/// <summary>
/// 用户组列表数据结果，包含用户组列表和分页信息。
/// </summary>
public class UserGroupListDataResult : ListApiResult
{
    /// <summary>
    /// 用户组列表。
    /// </summary>
    [JsonPropertyName("grouplist")]
    public List<UserGroupQueryDetailResult> GroupList { get; set; } = new List<UserGroupQueryDetailResult>();
}
