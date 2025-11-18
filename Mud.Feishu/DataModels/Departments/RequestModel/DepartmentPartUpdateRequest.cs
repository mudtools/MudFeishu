namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 部分更新部门请求体。
/// </summary>
public class DepartmentPartUpdateRequest : DepartmentRequestBase
{
    /// <summary>
    /// 部门 HRBP 的用户 ID 列表。 ID 类型与查询参数 user_id_type 的取值保持一致。
    /// </summary>
    [JsonPropertyName("department_hrbps")]
    public List<string> DepartmentHrbps { get; set; } = [];
}
