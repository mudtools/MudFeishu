namespace Mud.Feishu.DataModels.UserGroup;

/// <summary>
/// 用户组详细信息结果，包含用户组的详细配置信息。
/// </summary>
public class UserGroupDetailResult : UserGroupBaseResult
{
    /// <summary>
    /// 部门范围列表。
    /// </summary>
    [JsonPropertyName("department_scope_list")]
    public List<string> DepartmentScopeList { get; set; } = new List<string>();

    /// <summary>
    /// 群组ID。
    /// </summary>
    [JsonPropertyName("group_id")]
    public string GroupId { get; set; } = string.Empty;
}
