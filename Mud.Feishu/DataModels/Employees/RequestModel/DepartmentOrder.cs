namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 员工在所属部门内的排序信息。
/// </summary>
public class DepartmentOrder
{
    /// <summary>
    /// 指定员工所在的部门，标识企业内一个唯一的部门，与department_id_type类型保持一致。
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 员工在部门内的排序权重。
    /// </summary>
    [JsonPropertyName("order_weight_in_deparment")]
    public string? OrderWeightInDepartment { get; set; }

    /// <summary>
    /// 该部门在用户所属的多个部门间的排序权重。
    /// </summary>
    [JsonPropertyName("order_weight_among_deparments")]
    public string? OrderWeightAmongDepartments { get; set; }

    /// <summary>
    /// 是否为用户的主部门（用户只能有一个主部门，且排序权重应最大，不填则默认使用排序第一的部门作为主部门),可选值:true/false。
    /// </summary>
    [JsonPropertyName("is_main_department")]
    public bool? IsMainDepartment { get; set; }
}