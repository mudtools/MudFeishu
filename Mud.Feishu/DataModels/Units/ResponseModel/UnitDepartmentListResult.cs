// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 单位部门列表响应结果，包含指定单位下的部门列表数据。
/// 用于获取单位关联的部门信息时的响应数据格式。
/// </summary>
public class UnitDepartmentListResult : ApiPageListResult
{
    /// <summary>
    /// 部门列表。
    /// <para>包含属于该单位的所有部门信息。</para>
    /// <para>每个部门包含部门ID、名称等基本信息。</para>
    /// </summary>
    [JsonPropertyName("departmentlist")]
    public List<DepartmentUnit> DepartmentList { get; set; } = new List<DepartmentUnit>();
}
