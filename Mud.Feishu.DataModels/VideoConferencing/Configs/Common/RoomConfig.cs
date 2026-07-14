// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>节点配置</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class RoomConfig
{
    /// <summary>
    /// <para>飞书会议室背景图</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://lf1-ttcdn-tos.pstatp.com/obj/xxx</para>
    /// </summary>
    [JsonPropertyName("room_background")]
    public string? RoomBackground { get; set; }

    /// <summary>
    /// <para>飞书签到板背景图</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://lf1-ttcdn-tos.pstatp.com/obj/xxx</para>
    /// </summary>
    [JsonPropertyName("display_background")]
    public string? DisplayBackground { get; set; }

    /// <summary>
    /// <para>飞书会议室数字标牌</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("digital_signage")]
    public RoomDigitalSignage? DigitalSignage { get; set; }

    /// <summary>
    /// <para>飞书投屏盒子数字标牌</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_box_digital_signage")]
    public RoomDigitalSignage? RoomBoxDigitalSignage { get; set; }

    /// <summary>
    /// <para>会议室状态</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("room_status")]
    public RoomConfigRoomStatus? RoomStatus { get; set; }
}
