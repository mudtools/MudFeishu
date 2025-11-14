namespace Mud.Feishu.DataModels.UserGroup;

/// <summary>
/// 用户组查询结果，包含单个用户组的详细信息。
/// </summary>
public class UserGroupQueryResult
{
    /// <summary>
    /// 用户组信息。
    /// </summary>
    [JsonPropertyName("group")]
    public UserGroupQueryDetailResult? Group { get; set; }
}
