// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.DepartmentsV1;

/// <summary>
/// 部门详细信息模型，用于表示飞书组织架构中部门的完整信息。
/// 该模型包含部门的基本属性、层级关系、统计数据和扩展字段等全方位信息。
/// </summary>
public class DepartmentDetail
{
    /// <summary>
    /// 部门唯一标识符，用于在系统中唯一标识一个部门。
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// 部门统计数据，包含该部门下成员和子部门的数量信息。
    /// </summary>
    [JsonPropertyName("department_count")]
    public DepartmentCount? DepartmentCount { get; set; }

    /// <summary>
    /// 部门是否有子部门的标识，用于表示部门在组织架构中的层级状态。
    /// </summary>
    [JsonPropertyName("has_child")]
    public bool HasChild { get; set; }

    /// <summary>
    /// 部门领导列表，包含该部门所有具有管理权限的人员信息。
    /// </summary>
    [JsonPropertyName("leaders")]
    public List<DepartmentLeader> Leaders { get; set; } = [];

    /// <summary>
    /// 父部门标识符，用于表示部门在组织架构中的层级关系。
    /// </summary>
    [JsonPropertyName("parent_department_id")]
    public string? ParentDepartmentId { get; set; }

    /// <summary>
    /// 部门名称的多语言内容，支持国际化显示。
    /// </summary>
    [JsonPropertyName("name")]
    public I18nContent? Name { get; set; }

    /// <summary>
    /// 部门启用状态，表示部门当前是否处于活跃使用状态。
    /// </summary>
    [JsonPropertyName("enabled_status")]
    public bool EnabledStatus { get; set; }

    /// <summary>
    /// 部门排序权重，用于控制部门在同级中的显示顺序。
    /// </summary>
    [JsonPropertyName("order_weight")]
    public string? OrderWeight { get; set; }

    /// <summary>
    /// 部门自定义字段值列表，用于存储扩展的业务属性信息。
    /// </summary>
    [JsonPropertyName("custom_field_values")]
    public List<CustomFieldValue>? CustomFieldValues { get; set; }

    /// <summary>
    /// 部门路径信息列表，包含从根部门到当前部门的完整路径信息。
    /// </summary>
    [JsonPropertyName("department_path_infos")]
    public List<DepartmentPathInfo>? DepartmentPathInfos { get; set; }

    /// <summary>
    /// 数据源标识，表示部门数据的来源或同步方式。
    /// </summary>
    [JsonPropertyName("data_source")]
    public int DataSource { get; set; }
}
