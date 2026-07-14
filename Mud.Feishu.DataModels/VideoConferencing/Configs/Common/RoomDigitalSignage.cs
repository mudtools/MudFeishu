// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;


/// <summary>
/// <para>飞书会议室数字标牌</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class RoomDigitalSignage
{
    /// <summary>
    /// <para>是否覆盖子层级及会议室</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("if_cover_child_scope")]
    public bool? IfCoverChildScope { get; set; }

    /// <summary>
    /// <para>是否开启数字标牌功能</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("enable")]
    public bool? Enable { get; set; }

    /// <summary>
    /// <para>是否静音播放</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("mute")]
    public bool? Mute { get; set; }

    /// <summary>
    /// <para>在会议结束n分钟后开始播放，取值1~720（仅对飞书会议室数字标牌生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("start_display")]
    public int? StartDisplay { get; set; }

    /// <summary>
    /// <para>在日程会议开始前n分钟停止播放，取值1~720（仅对飞书会议室数字标牌生效）</para>
    /// <para>必填：否</para>
    /// <para>示例值：3</para>
    /// </summary>
    [JsonPropertyName("stop_display")]
    public int? StopDisplay { get; set; }

    /// <summary>
    /// <para>素材列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("materials")]
    public RoomDigitalSignageMaterial[]? Materials { get; set; }

}
