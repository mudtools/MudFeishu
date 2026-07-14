// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;


/// <summary>
/// 导出会议室预定数据请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class ResourceReservationListExportRequest
{
    /// <summary>
    /// <para>会议室层级id</para>
    /// <para>必填：是</para>
    /// <para>示例值：omb_57c9cc7d9a81e27e54c8fabfd02759e7</para>
    /// </summary>
    [JsonPropertyName("room_level_id")]
    public string RoomLevelId { get; set; } = string.Empty;

    /// <summary>
    /// <para>是否展示会议主题</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("need_topic")]
    public bool? NeedTopic { get; set; }

    /// <summary>
    /// <para>查询开始时间（unix时间，单位sec）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1655276858</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>查询结束时间（unix时间，单位sec）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1655276858</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>待筛选的会议室id列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：["omm_eada1d61a550955240c28757e7dec3af"]</para>
    /// </summary>
    [JsonPropertyName("room_ids")]
    public string[]? RoomIds { get; set; }

    /// <summary>
    /// <para>若为true表示导出room_ids范围外的会议室，默认为false</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_exclude")]
    public bool? IsExclude { get; set; }
}
