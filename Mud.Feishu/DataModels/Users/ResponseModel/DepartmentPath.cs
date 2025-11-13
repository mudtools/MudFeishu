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

public class DepartmentNameInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("i18n_name")]
    public I18nName I18nName { get; set; }
}

public class DepartmentPathInfo
{
    [JsonPropertyName("department_ids")]
    public List<string> DepartmentIds { get; set; }

    [JsonPropertyName("department_path_name")]
    public DepartmentNameInfo DepartmentPathName { get; set; }
}