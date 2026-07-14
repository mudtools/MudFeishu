// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>会议室详情</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class MeetingRoom
{
    /// <summary>
    /// <para>会议室名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：测试会议室</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// <para>会议室能容纳的人数</para>
    /// <para>必填：是</para>
    /// <para>示例值：10</para>
    /// </summary>
    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    /// <summary>
    /// <para>会议室的相关描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：测试会议室描述</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>自定义的会议室ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：1234</para>
    /// </summary>
    [JsonPropertyName("custom_room_id")]
    public string? CustomRoomId { get; set; }

    /// <summary>
    /// <para>层级ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：omb_4ad1a2c7a2fbc5fc9570f38456931293</para>
    /// </summary>
    [JsonPropertyName("room_level_id")]
    public string RoomLevelId { get; set; } = string.Empty;

    /// <summary>
    /// <para>会议室状态</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_status")]
    public RoomStatus? RoomStatus { get; set; }


    /// <summary>
    /// <para>设施信息列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("device")]
    public RoomDevice[]? Devices { get; set; }
}
