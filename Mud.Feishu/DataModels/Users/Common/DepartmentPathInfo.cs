namespace Mud.Feishu.DataModels.Users;

public class DepartmentPathInfo
{
    [JsonPropertyName("department_ids")]
    public List<string> DepartmentIds { get; set; }

    [JsonPropertyName("department_path_name")]
    public DepartmentNameInfo DepartmentPathName { get; set; }
}