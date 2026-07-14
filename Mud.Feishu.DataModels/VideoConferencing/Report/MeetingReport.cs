// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;



/// <summary>
/// <para>会议报告</para>
/// </summary>
public class MeetingReport
{
    /// <summary>
    /// <para>总会议数量</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("total_meeting_count")]
    public string? TotalMeetingCount { get; set; }

    /// <summary>
    /// <para>总会议时长（单位sec）</para>
    /// <para>必填：否</para>
    /// <para>示例值：300000</para>
    /// </summary>
    [JsonPropertyName("total_meeting_duration")]
    public string? TotalMeetingDuration { get; set; }

    /// <summary>
    /// <para>总参会人数</para>
    /// <para>必填：否</para>
    /// <para>示例值：20000</para>
    /// </summary>
    [JsonPropertyName("total_participant_count")]
    public string? TotalParticipantCount { get; set; }

    /// <summary>
    /// <para>每日会议报告列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("daily_report")]
    public ReportMeetingDaily[]? DailyReports { get; set; }
}