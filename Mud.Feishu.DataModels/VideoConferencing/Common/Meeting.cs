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
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class Meeting : MeetingBaseInfo
{

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
