// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.DepartmentsV1;

/// <summary>
/// 部门统计数据模型，用于表示部门下成员和子部门的数量统计信息。
/// 该数据模型包含递归统计（包含所有子部门）和直接统计（仅包含直属成员/部门）两种计算方式。
/// </summary>
public class DepartmentCount
{
    /// <summary>
    /// 递归成员总数，包含该部门及所有子部门下的所有成员数量。
    /// </summary>
    [JsonPropertyName("recursive_members_count")]
    public string? RecursiveMembersCount { get; set; }

    /// <summary>
    /// 直属成员数量，仅包含直接属于该部门的成员数量，不包含子部门的成员。
    /// </summary>
    [JsonPropertyName("direct_members_count")]
    public string? DirectMembersCount { get; set; }

    /// <summary>
    /// 递归成员总数（排除部门领导），包含该部门及所有子部门下的成员，但不包含各层级的部门领导。
    /// </summary>
    [JsonPropertyName("recursive_members_count_exclude_leaders")]
    public string? RecursiveMembersCountExcludeLeaders { get; set; }

    /// <summary>
    /// 递归部门总数，包含该部门下的所有子部门数量（包括多层嵌套）。
    /// </summary>
    [JsonPropertyName("recursive_departments_count")]
    public string? RecursiveDepartmentsCount { get; set; }

    /// <summary>
    /// 直属部门数量，仅包含直接隶属于当前部门的子部门数量，不包括更深层的子部门。
    /// </summary>
    [JsonPropertyName("direct_departments_count")]
    public string? DirectDepartmentsCount { get; set; }
}
