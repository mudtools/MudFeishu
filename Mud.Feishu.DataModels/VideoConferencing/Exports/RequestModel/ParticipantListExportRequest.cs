// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// 导出会议参与人明细请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class ParticipantListExportRequest
{
    /// <summary>
    /// <para>会议开始时间（unix时间，单位sec）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1655276858</para>
    /// </summary>
    [JsonPropertyName("meeting_start_time")]
    public string MeetingStartTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>会议结束时间（unix时间，单位sec，若是进行中会议可填当前时间，否则填准确的会议结束时间）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1655276858</para>
    /// </summary>
    [JsonPropertyName("meeting_end_time")]
    public string MeetingEndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>会议状态（不传默认为已结束会议）</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：进行中</item>
    /// <item>2：已结束</item>
    /// <item>3：待召开</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("meeting_status")]
    public int? MeetingStatus { get; set; }

    /// <summary>
    /// <para>9位会议号</para>
    /// <para>必填：是</para>
    /// <para>示例值：123456789</para>
    /// </summary>
    [JsonPropertyName("meeting_no")]
    public string MeetingNo { get; set; } = string.Empty;

    /// <summary>
    /// <para>按参会Lark用户筛选（最多一个筛选条件）</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_3ec3f6a28a0d08c45d895276e8e5e19b</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>按参会Rooms筛选（最多一个筛选条件）</para>
    /// <para>必填：否</para>
    /// <para>示例值：omm_eada1d61a550955240c28757e7dec3af</para>
    /// </summary>
    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }
}
