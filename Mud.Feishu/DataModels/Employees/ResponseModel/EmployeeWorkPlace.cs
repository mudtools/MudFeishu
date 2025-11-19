// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 员工工作地点类，包含工作地点的相关信息
/// </summary>
public class EmployeeWorkPlace
{
    /// <summary>
    /// 地点ID
    /// </summary>
    [JsonPropertyName("place_id")]
    public string? PlaceId { get; set; }

    /// <summary>
    /// 地点名称
    /// </summary>
    [JsonPropertyName("place_name")]
    public EmployeeI18nContent? PlaceName { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 地点描述信息
    /// </summary>
    [JsonPropertyName("description")]
    public EmployeeI18nContent? Description { get; set; }
}