namespace Mud.Feishu.DataModels.Departments;

public class GetDepartmentInfo : DepartmentBase
{
    [JsonPropertyName("group_chat_employee_types")]
    public List<int> GroupChatEmployeeTypes { get; set; } = new List<int>();

    [JsonPropertyName("department_hrbps")]
    public List<string> DepartmentHrbps { get; set; } = new List<string>();

    [JsonPropertyName("primary_member_count")]
    public int PrimaryMemberCount { get; set; }
}
