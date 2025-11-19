// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 员工职位信息类，包含员工的具体职位信息
/// </summary>
public class EmployeePositionInfo
{
    /// <summary>
    /// 职位编码
    /// </summary>
    [JsonPropertyName("position_code")]
    public string? PositionCode { get; set; }

    /// <summary>
    /// 职位名称
    /// </summary>
    [JsonPropertyName("position_name")]
    public string? PositionName { get; set; }

    /// <summary>
    /// 领导ID
    /// </summary>
    [JsonPropertyName("leader_id")]
    public string? LeaderId { get; set; }

    /// <summary>
    /// 领导职位编码
    /// </summary>
    [JsonPropertyName("leader_position_code")]
    public string? LeaderPositionCode { get; set; }

    /// <summary>
    /// 是否为主要职位
    /// </summary>
    [JsonPropertyName("is_main_position")]
    public bool? IsMainPosition { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }
}