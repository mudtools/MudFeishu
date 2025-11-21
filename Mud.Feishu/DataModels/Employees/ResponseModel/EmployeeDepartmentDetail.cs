// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 员工部门详情类，包含员工所属部门的详细信息
/// </summary>
public class EmployeeDepartmentDetail
{
    /// <summary>
    /// 部门ID
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 部门人员统计信息
    /// </summary>
    [JsonPropertyName("department_count")]
    public EmployeeDepartmentCount? DepartmentCount { get; set; }

    /// <summary>
    /// 是否有子部门
    /// </summary>
    [JsonPropertyName("has_child")]
    public bool HasChild { get; set; }

    /// <summary>
    /// 领导信息列表
    /// </summary>
    [JsonPropertyName("leaders")]
    public List<EmployeeLeaderInfo> Leaders { get; set; } = [];

    /// <summary>
    /// HRBP列表
    /// </summary>
    [JsonPropertyName("hrbps")]
    public List<string> Hrbps { get; set; } = [];

    /// <summary>
    /// 上级部门ID
    /// </summary>
    [JsonPropertyName("parent_department_id")]
    public string? ParentDepartmentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [JsonPropertyName("name")]
    public I18nContents? Name { get; set; }

    /// <summary>
    /// 排序权重
    /// </summary>
    [JsonPropertyName("order_weight")]
    public string? OrderWeight { get; set; }

    /// <summary>
    /// 自定义字段值列表
    /// </summary>
    [JsonPropertyName("custom_field_values")]
    public List<CustomFieldValue> CustomFieldValues { get; set; } = [];

    /// <summary>
    /// 部门路径信息列表
    /// </summary>
    [JsonPropertyName("department_path_infos")]
    public List<EmployeeDepartmentPathInfo> DepartmentPathInfos { get; set; } = [];

    /// <summary>
    /// 数据来源
    /// </summary>
    [JsonPropertyName("data_source")]
    public int? DataSource { get; set; }
}