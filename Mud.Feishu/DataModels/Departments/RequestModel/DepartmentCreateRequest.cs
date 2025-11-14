using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu.DataModels.Departments;

/// <summary>
/// 创建部门请求体。
/// </summary>
public class DepartmentCreateRequest : DepartmentRequestBase
{
    /// <summary>
    /// 自定义部门 ID。
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 部门绑定的单位自定义 ID 列表，当前只支持绑定一个单位。
    /// </summary>
    [JsonPropertyName("unit_ids")]
    public List<string> UnitIds { get; set; } = [];

    /// <summary>
    /// 部门 HRBP 的用户 ID 列表。 ID 类型与查询参数 user_id_type 的取值保持一致。
    /// </summary>
    [JsonPropertyName("department_hrbps")]
    public List<string> DepartmentHrbps { get; set; } = [];
}
