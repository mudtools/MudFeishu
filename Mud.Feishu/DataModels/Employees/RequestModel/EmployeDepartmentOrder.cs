// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 员工在所属部门内的排序信息。
/// </summary>
public class EmployeDepartmentOrder
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