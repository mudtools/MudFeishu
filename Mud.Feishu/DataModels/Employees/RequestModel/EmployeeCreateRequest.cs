namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 创建员工对象请求体。
/// </summary>
public class EmployeeRequest
{
    /// <summary>
    /// 待创建员工对象
    /// </summary>
    [JsonPropertyName("employee")]
    public EmployeeCreateInfo Employee { get; set; } = new EmployeeCreateInfo();

    /// <summary>
    /// 接口拓展选项
    /// </summary>
    [JsonPropertyName("options")]
    public EmployeeCreateOptions? Options { get; set; }
}