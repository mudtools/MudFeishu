// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.VideoConferencing;


/// <summary>
/// 会议数据
/// </summary>
public class MeetingEventMeeting
{
    /// <summary>
    /// <para>会议ID（视频会议的唯一标识，视频会议开始后才会产生）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>会议主题</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    /// <summary>
    /// <para>9位会议号（飞书用户可通过输入9位会议号快捷入会）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_no")]
    public string? MeetingNo { get; set; }

    /// <summary>
    /// <para>会议创建源</para>
    /// <para>**可选值有**：</para>
    /// <para>1:日程会议,2:即时会议,3:面试会议,4:开放平台会议,100:其他会议类型</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：日程会议</item>
    /// <item>2：即时会议</item>
    /// <item>3：面试会议</item>
    /// <item>4：开放平台会议</item>
    /// <item>100：其他会议类型</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("meeting_source")]
    public int? MeetingSource { get; set; }

    /// <summary>
    /// <para>会议开始时间（unix时间，单位：秒）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>会议结束时间（unix时间，单位：秒）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>会议主持人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("host_user")]
    public MeetingEventUser? HostUser { get; set; }

    /// <summary>
    /// <para>会议拥有者</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("owner")]
    public MeetingEventUser? Owner { get; set; }

    /// <summary>
    /// <para>日程实体的唯一标志</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("calendar_event_id")]
    public string? CalendarEventId { get; set; }

    /// <summary>
    /// <para>会议子类型</para>
    /// <para>**可选值有**：</para>
    /// <para>1:会前投屏,2:有线共享,3:会前妙享,4:企业办公电话,5:IP Phone,6:网络研讨会</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：会前投屏</item>
    /// <item>2：有线共享</item>
    /// <item>3：会前妙享</item>
    /// <item>4：企业办公电话</item>
    /// <item>5：IP Phone</item>
    /// <item>6：网络研讨会</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("meeting_sub_type")]
    public int? MeetingSubType { get; set; }

    /// <summary>
    /// <para>会议安全设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("security_setting")]
    public MeetingSecuritySetting? SecuritySetting { get; set; }


    /// <summary>
    /// <para>研讨会相关设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("webinar_setting")]
    public MeetingWebinarSetting? WebinarSetting { get; set; }

}