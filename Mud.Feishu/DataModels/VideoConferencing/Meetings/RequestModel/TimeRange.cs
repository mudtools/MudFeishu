// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;


/// <summary>
/// <para>按会议开始时间过滤，传入时间范围对象。其中 start_time 必须小于等于 end_time（即 meeting_filter.start_time.end_time）。</para>
/// </summary>
public class TimeRange
{
    /// <summary>
    /// <para>时间范围的起始时间，需符合 ISO 8601 标准并携带时区信息。</para>
    /// <para>必填：否</para>
    /// <para>示例值：2026-03-21T16:15:30+08:00</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>时间范围的结束时间，需符合 ISO 8601 标准并携带时区信息。</para>
    /// <para>必填：否</para>
    /// <para>示例值：2026-03-21T16:15:30+08:00</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }
}