// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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