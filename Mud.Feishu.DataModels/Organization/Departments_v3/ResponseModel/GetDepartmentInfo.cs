namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 获取部门信息响应模型，包含部门的详细信息。
/// 用于获取单个部门或批量获取部门信息时的响应数据格式。
/// </summary>
public class GetDepartmentInfo : DepartmentBase
{
    /// <summary>
    /// 群聊员工类型列表。
    /// 表示该部门可加入的群聊类型。
    /// </summary>
    [JsonPropertyName("group_chat_employee_types")]
    public List<int> GroupChatEmployeeTypes { get; set; } = new List<int>();

    /// <summary>
    /// 部门HRBP（人力资源业务伙伴）列表。
    /// 包含负责该部门的人力资源业务伙伴的用户ID。
    /// </summary>
    [JsonPropertyName("department_hrbps")]
    public List<string> DepartmentHrbps { get; set; } = new List<string>();

    /// <summary>
    /// 主要成员数量。
    /// 表示该部门中的主要成员（通常为正式员工）的数量。
    /// </summary>
    [JsonPropertyName("primary_member_count")]
    public int PrimaryMemberCount { get; set; }
}
