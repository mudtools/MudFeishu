// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>会议信息</para>
/// </summary>
public class Meeting
{
    /// <summary>
    /// <para>会议ID（视频会议的唯一标识，视频会议开始后才会产生）</para>
    /// <para>必填：否</para>
    /// <para>示例值：6911188411934433028</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>会议主题</para>
    /// <para>必填：否</para>
    /// <para>示例值：my meeting</para>
    /// </summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    /// <summary>
    /// <para>会议链接（飞书用户可通过点击会议链接快捷入会）</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://vc.feishu.cn/j/337736498</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>会议号</para>
    /// <para>必填：否</para>
    /// <para>示例值：123456789</para>
    /// </summary>
    [JsonPropertyName("meeting_no")]
    public string? MeetingNo { get; set; }

    /// <summary>
    /// <para>会议密码</para>
    /// <para>必填：否</para>
    /// <para>示例值：971024</para>
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// <para>会议创建时间（unix时间，单位sec）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1608885566</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>会议开始时间（unix时间，单位sec）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1608883322</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>会议结束时间（unix时间，单位sec）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1608888867</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>主持人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("host_user")]
    public MeetingUser? HostUser { get; set; }

    /// <summary>
    /// <para>该会议是否支持互通（注：该字段内测中）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("meeting_connect")]
    public bool? MeetingConnect { get; set; }

    /// <summary>
    /// <para>会议状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：会议呼叫中</item>
    /// <item>2：会议进行中</item>
    /// <item>3：会议已结束</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>参会峰值人数</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    [JsonPropertyName("participant_count")]
    public string? ParticipantCount { get; set; }

    /// <summary>
    /// <para>累计参会人数</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    [JsonPropertyName("participant_count_accumulated")]
    public string? ParticipantCountAccumulated { get; set; }

    /// <summary>
    /// <para>参会人列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("participants")]
    public MeetingParticipant[]? Participants { get; set; }

    /// <summary>
    /// <para>会中使用的能力</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ability")]
    public MeetingAbility? Ability { get; set; }
}