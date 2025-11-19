// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 员工工作信息类，包含员工的工作相关信息
/// </summary>
public class EmployeeWorkInfo
{
    /// <summary>
    /// 工作国家或地区
    /// </summary>
    [JsonPropertyName("work_country_or_region")]
    public string? WorkCountryOrRegion { get; set; }

    /// <summary>
    /// 工作地点信息
    /// </summary>
    [JsonPropertyName("work_place")]
    public EmployeeWorkPlace? WorkPlace { get; set; }

    /// <summary>
    /// 工位信息
    /// </summary>
    [JsonPropertyName("work_station")]
    public EmployeeI18nContent? WorkStation { get; set; }

    /// <summary>
    /// 工号
    /// </summary>
    [JsonPropertyName("job_number")]
    public string? JobNumber { get; set; }

    /// <summary>
    /// 分机号
    /// </summary>
    [JsonPropertyName("extension_number")]
    public string? ExtensionNumber { get; set; }

    /// <summary>
    /// 入职日期
    /// </summary>
    [JsonPropertyName("join_date")]
    public string? JoinDate { get; set; }

    /// <summary>
    /// 用工类型
    /// </summary>
    [JsonPropertyName("employment_type")]
    public string? EmploymentType { get; set; }

    /// <summary>
    /// 员工状态
    /// </summary>
    [JsonPropertyName("staff_status")]
    public string? StaffStatus { get; set; }

    /// <summary>
    /// 职位信息列表
    /// </summary>
    [JsonPropertyName("positions")]
    public List<EmployeePositionInfo> Positions { get; set; } = [];

    /// <summary>
    /// 职务信息
    /// </summary>
    [JsonPropertyName("job_title")]
    public EmployeeJobTitleInfo? JobTitle { get; set; }

    /// <summary>
    /// 工作族信息
    /// </summary>
    [JsonPropertyName("job_family")]
    public EmployeeJobFamilyInfo? JobFamily { get; set; }
}