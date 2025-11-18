namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 单位部门列表响应结果，包含指定单位下的部门列表数据。
/// 用于获取单位关联的部门信息时的响应数据格式。
/// </summary>
public class UnitDepartmentListResult : ListApiResult
{
    /// <summary>
    /// 部门列表。
    /// <para>包含属于该单位的所有部门信息。</para>
    /// <para>每个部门包含部门ID、名称等基本信息。</para>
    /// </summary>
    [JsonPropertyName("departmentlist")]
    public List<DepartmentUnit> DepartmentList { get; set; } = new List<DepartmentUnit>();
}
