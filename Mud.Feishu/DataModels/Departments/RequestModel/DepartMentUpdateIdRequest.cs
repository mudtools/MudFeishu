namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 更新部门 ID请求体。
/// </summary>
public class DepartMentUpdateIdRequest
{
    /// <summary>
    /// 新的自定义部门 ID
    /// </summary>
    [JsonPropertyName("new_department_id")]
    public required string NewDepartmentId { get; set; }
}
