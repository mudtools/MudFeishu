
namespace Mud.Feishu.DataModels.DepartmentsV1;

/// <summary>
/// 部门创建结果
/// </summary>
public class DepartmentCreateResult
{
    /// <summary>
    /// 部门ID
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }
}