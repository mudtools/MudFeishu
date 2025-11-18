namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 部门单位关联模型，用于表示部门与单位的关联关系。
/// 包含部门所属的单位信息，用于组织和权限管理。
/// </summary>
public class DepartmentUnit
{
    /// <summary>
    /// 单位ID。
    /// <para>表示部门所属的单位标识符。</para>
    /// <para>用于确定部门的组织归属和权限范围。</para>
    /// <para>示例值："6991111111111111111"</para>
    /// </summary>
    [JsonPropertyName("unit_id")]
    public string UnitId { get; set; } = string.Empty;

    /// <summary>
    /// 部门ID。
    /// <para>表示关联的具体部门标识符。</para>
    /// <para>与单位ID配合，建立部门与单位的从属关系。</para>
    /// <para>示例值："od-4e6789c92a3c8e02dbe89d3f9b87c"</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string DepartmentId { get; set; } = string.Empty;
}