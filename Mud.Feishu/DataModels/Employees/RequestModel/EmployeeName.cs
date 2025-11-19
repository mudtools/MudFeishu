namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 员工姓名对象。
/// </summary>
public class EmployeeName
{
    /// <summary>
    /// 员工的姓名，最多可输入 64 字。
    /// </summary>
    [JsonPropertyName("name")]
    public required EmployeeI18nContent Name { get; set; }

    /// <summary>
    /// 别名，最多可输入 64 字
    /// </summary>
    [JsonPropertyName("another_name")]
    public string? AnotherName { get; set; }
}