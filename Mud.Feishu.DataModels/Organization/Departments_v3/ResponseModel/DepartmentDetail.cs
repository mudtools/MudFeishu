using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 部门详细信息，包含部门的基本信息、配置和状态以及HRBP信息。
/// </summary>
public class DepartmentDetail : DepartmentBase
{
    /// <summary>
    /// 部门HRBP列表。
    /// </summary>
    [JsonPropertyName("department_hrbps")]
    public List<string> DepartmentHrbps { get; set; } = new List<string>();
}
