namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 部门请求基础模型，用于包含部门标识信息。
/// </summary>
public class DepartmentRequest
{
    /// <summary>
    /// 部门ID。
    /// <para>用于唯一标识一个部门。</para>
    /// <para>支持的ID类型包括：department_id、open_department_id、custom_id。</para>
    /// <para>示例值："od-4e6789c92a3c8e02dbe89d3f9b87c"</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string DepartmentId { get; set; } = string.Empty;
}