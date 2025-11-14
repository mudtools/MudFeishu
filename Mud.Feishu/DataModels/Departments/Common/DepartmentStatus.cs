namespace Mud.Feishu.DataModels.Departments;

public class DepartmentStatus
{
    [JsonPropertyName("is_deleted")]
    public bool IsDeleted { get; set; }
}