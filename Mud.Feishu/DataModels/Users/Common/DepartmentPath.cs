namespace Mud.Feishu.DataModels.Users;

public class DepartmentPath
{
    [JsonPropertyName("department_id")]
    public string DepartmentId { get; set; }

    [JsonPropertyName("department_name")]
    public DepartmentNameInfo DepartmentName { get; set; }

    [JsonPropertyName("department_path")]
    public DepartmentPathInfo DepartmentPathInfo { get; set; }
}

