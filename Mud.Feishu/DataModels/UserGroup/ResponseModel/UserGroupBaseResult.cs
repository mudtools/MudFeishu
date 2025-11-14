namespace Mud.Feishu.DataModels.UserGroup;

/// <summary>
/// 用户组基础结果类，包含用户组的基本信息。
/// </summary>
public abstract class UserGroupBaseResult
{
    /// <summary>
    /// 用户组ID。
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 用户组名称。
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 用户组描述。
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 用户组成员数量。
    /// </summary>
    [JsonPropertyName("member_user_count")]
    public int MemberUserCount { get; set; }

    /// <summary>
    /// 用户组部门成员数量。
    /// </summary>
    [JsonPropertyName("member_department_count")]
    public int MemberDepartmentCount { get; set; }

    /// <summary>
    /// 用户组类型。
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }
}