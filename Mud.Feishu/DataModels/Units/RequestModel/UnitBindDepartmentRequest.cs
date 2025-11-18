namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 部门与单位的绑定关系请求体
/// </summary>
public class UnitBindDepartmentRequest
{
    /// <summary>
    /// 单位 ID。
    /// </summary>
    [JsonPropertyName("unit_id")]
    public required string UnitId { get; set; }

    /// <summary>
    /// 单位关联的部门 ID，ID 类型与 department_id_type 的取值保持一致。
    /// </summary>
    [JsonPropertyName("department_id")]
    public required string DepartmentId { get; set; }

    /// <summary>
    /// 此次调用中的部门 ID 类型。
    /// </summary>
    [JsonPropertyName("department_id_type")]
    public string? DepartmentIdType { get; set; } = "open_department_id";
}
