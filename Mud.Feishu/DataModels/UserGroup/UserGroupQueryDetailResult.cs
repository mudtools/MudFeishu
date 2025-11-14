namespace Mud.Feishu.DataModels.UserGroup;

/// <summary>
/// 用户组查询结果。
/// </summary>
public class UserGroupQueryResult
{
    [JsonPropertyName("group")]
    public UserGroupQueryDetailResult Group { get; set; }
}

/// <summary>
/// 用户组查询明细结果。
/// </summary>
public class UserGroupQueryDetailResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("member_user_count")]
    public int MemberUserCount { get; set; }

    [JsonPropertyName("member_department_count")]
    public int MemberDepartmentCount { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }
}
