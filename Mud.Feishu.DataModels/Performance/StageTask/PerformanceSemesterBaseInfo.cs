// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 周期任务接口返回的周期基本信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceSemesterBaseInfo
{
    /// <summary>
    /// <para>周期 ID</para>
    /// <para>示例值：7235911950407352321</para>
    /// </summary>
    [JsonPropertyName("semester_id")]
    public string? SemesterId { get; set; }

    /// <summary>
    /// <para>周期名称（中划线形态 zh-CN / en-US）</para>
    /// </summary>
    [JsonPropertyName("semester_name")]
    public PerformanceI18nName? SemesterName { get; set; }

    /// <summary>
    /// <para>周期开始时间，毫秒时间戳</para>
    /// <para>示例值：1684684800000</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>周期结束时间，毫秒时间戳</para>
    /// <para>示例值：1748707140000</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }
}
