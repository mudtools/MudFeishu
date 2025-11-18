namespace Mud.Feishu.DataModels.Departments;

public class DepartmentRequest
{
    [JsonPropertyName("department_id")]
    public string DepartmentId { get; set; }
}