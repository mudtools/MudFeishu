namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 部门更新操作结果，包含更新的部门详细信息。
/// </summary>
public class DepartmentUpdateResult
{
    /// <summary>
    /// 更新的部门信息。
    /// </summary>
    [JsonPropertyName("department")]
    public DepartmentUpdateDetail Department { get; set; } = new DepartmentUpdateDetail();
}
