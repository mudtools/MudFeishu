// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;



/// <summary>
/// <para>会议搜索的过滤条件</para>
/// </summary>
public class MeetingFilterParam
{
    /// <summary>
    /// <para>按会议组织者过滤，传入用户 open_id 列表，可通过用户查询接口获取。默认值为空数组，不设置时不过滤该条件。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：128</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("organizer_ids")]
    public string[]? OrganizerIds { get; set; }

    /// <summary>
    /// <para>按参会人过滤，传入用户 open_id 列表，可通过用户查询接口获取。默认值为空数组，不设置时不过滤该条件。长度范围：0～128。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：128</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("participant_ids")]
    public string[]? ParticipantIds { get; set; }

    /// <summary>
    /// <para>按会议室过滤，传入会议室 open_id 列表，可通过会议室查询接口获取。默认值为空数组，不设置时不过滤该条件。长度范围：0～128。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：128</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("open_room_ids")]
    public string[]? OpenRoomIds { get; set; }

    /// <summary>
    /// <para>按会议开始时间过滤，传入时间范围对象。其中 start_time 必须小于等于 end_time（即 meeting_filter.start_time.end_time）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public TimeRange? StartTime { get; set; }
}