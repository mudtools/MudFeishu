namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 部门创建结果，包含创建的部门详细信息。
/// </summary>
public class DepartmentCreateUpdateResult
{
    /// <summary>
    /// 创建的部门信息。
    /// </summary>
    [JsonPropertyName("department")]
    public DepartmentDetail Department { get; set; } = new DepartmentDetail();
}
