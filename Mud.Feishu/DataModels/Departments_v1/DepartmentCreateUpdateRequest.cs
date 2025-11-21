
namespace Mud.Feishu.DataModels.DepartmentsV1;

/// <summary>
/// 创建部门请求体
/// </summary>
public class DepartmentCreateUpdateRequest
{
    /// <summary>
    /// 部门信息
    /// </summary>
    [JsonPropertyName("department")]
    public required DepartmentInfo Department { get; set; }
}