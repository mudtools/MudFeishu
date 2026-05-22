// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// 搜索会议室请求体
/// </summary>
public class SearchMeetingRoomsRequest
{
    /// <summary>
    /// <para>用于查询指定会议室的租户自定义会议室ID列表，优先使用该字段进行查询</para>
    /// <para>必填：否</para>
    /// <para>示例值：["10001"]</para>
    /// </summary>
    [JsonPropertyName("custom_room_ids")]
    public string[]? CustomRoomIds { get; set; }

    /// <summary>
    /// <para>会议室搜索关键词（当custom_room_ids为空时，使用该字段进行查询）</para>
    /// <para>必填：否</para>
    /// <para>示例值：测试会议室</para>
    /// </summary>
    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    /// <summary>
    /// <para>在该会议室层级下进行搜索（当custom_room_ids为空时，使用该字段进行查询）</para>
    /// <para>必填：否</para>
    /// <para>示例值：omb_4ad1a2c7a2fbc5fc9570f38456931293</para>
    /// </summary>
    [JsonPropertyName("room_level_id")]
    public string? RoomLevelId { get; set; }

    /// <summary>
    /// <para>搜索会议室是否可以包括层级名称（当custom_room_ids为空时，使用 keyword 字段查询）</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("search_level_name")]
    public bool? SearchLevelName { get; set; }

    /// <summary>
    /// <para>分页大小，该值默认为10，最大为100（当custom_room_ids为空时，使用该字段进行查询）</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// <para>最大值：100</para>
    /// <para>最小值：1</para>
    /// <para>默认值：10</para>
    /// </summary>
    [JsonPropertyName("page_size")]
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }
}