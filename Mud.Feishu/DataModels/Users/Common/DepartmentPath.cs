namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 部门路径信息。
/// 包含部门的层级路径信息，用于表示部门在组织架构中的位置。
/// </summary>
public class DepartmentPath
{
    /// <summary>
    /// 部门ID。
    /// 当前部门的唯一标识符。
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 部门名称信息。
    /// 包含部门的名称和国际化名称。
    /// </summary>
    [JsonPropertyName("department_name")]
    public DepartmentNameInfo? DepartmentName { get; set; }

    /// <summary>
    /// 部门路径详细信息。
    /// 包含部门的完整层级路径信息。
    /// </summary>
    [JsonPropertyName("department_path")]
    public DepartmentPathInfo? DepartmentPathInfo { get; set; }
}

