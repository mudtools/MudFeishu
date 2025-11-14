namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 部门路径详细信息。
/// 包含部门层级路径的完整信息，用于表示部门在组织架构中的完整路径。
/// </summary>
public class DepartmentPathInfo
{
    /// <summary>
    /// 部门ID列表。
    /// 包含从根部门到当前部门的完整层级路径中的部门ID序列。
    /// </summary>
    [JsonPropertyName("department_ids")]
    public List<string> DepartmentIds { get; set; } = [];

    /// <summary>
    /// 部门路径名称信息。
    /// 包含部门路径中每个层级的名称和国际化名称信息。
    /// </summary>
    [JsonPropertyName("department_path_name")]
    public DepartmentNameInfo? DepartmentPathName { get; set; }
}