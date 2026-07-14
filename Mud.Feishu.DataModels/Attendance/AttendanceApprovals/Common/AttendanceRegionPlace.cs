// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceApprovals;

/// <summary>
/// <para>出发地（只有一个）</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class AttendanceRegionPlace
{
    /// <summary>
    /// <para>地理等级（国家｜省｜市｜区）</para>
    /// <para>必填：否</para>
    /// <para>示例值：l1：国家级</para>
    /// </summary>
    [JsonPropertyName("region_level")]
    public string? RegionLevel { get; set; }

    /// <summary>
    /// <para>地理id</para>
    /// <para>必填：否</para>
    /// <para>示例值：6863333418483058189</para>
    /// </summary>
    [JsonPropertyName("region_id")]
    public string? RegionId { get; set; }
}
