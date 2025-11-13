namespace Mud.Feishu.DataModels.Users;


public class DepartmentOrder
{
    [JsonPropertyName("department_id")]
    public string DepartmentId { get; set; }

    [JsonPropertyName("user_order")]
    public int UserOrder { get; set; }

    [JsonPropertyName("department_order")]
    public int DepartmentOrderValue { get; set; }

    [JsonPropertyName("is_primary_dept")]
    public bool IsPrimaryDept { get; set; }
}