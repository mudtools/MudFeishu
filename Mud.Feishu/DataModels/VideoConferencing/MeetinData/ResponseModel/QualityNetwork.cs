// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;



/// <summary>
/// <para>网络</para>
/// </summary>
public class QualityNetwork
{
    /// <summary>
    /// <para>时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022.12.23 11:16:00 (GMT+08:00)</para>
    /// </summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    /// <summary>
    /// <para>网络延迟</para>
    /// <para>必填：否</para>
    /// <para>示例值：100ms</para>
    /// </summary>
    [JsonPropertyName("network_delay")]
    public string? NetworkDelay { get; set; }

    /// <summary>
    /// <para>码率（接收）</para>
    /// <para>必填：否</para>
    /// <para>示例值：8kbps</para>
    /// </summary>
    [JsonPropertyName("bitrate_received")]
    public string? BitrateReceived { get; set; }

    /// <summary>
    /// <para>丢包 - 平均（接收）</para>
    /// <para>必填：否</para>
    /// <para>示例值：8%</para>
    /// </summary>
    [JsonPropertyName("packet_loss_avg_received")]
    public string? PacketLossAvgReceived { get; set; }

    /// <summary>
    /// <para>丢包 - 最大（接收）</para>
    /// <para>必填：否</para>
    /// <para>示例值：9%</para>
    /// </summary>
    [JsonPropertyName("packet_loss_max_received")]
    public string? PacketLossMaxReceived { get; set; }

    /// <summary>
    /// <para>码率（发送）</para>
    /// <para>必填：否</para>
    /// <para>示例值：9kbps</para>
    /// </summary>
    [JsonPropertyName("bitrate_sent")]
    public string? BitrateSent { get; set; }

    /// <summary>
    /// <para>丢包 - 平均（发送）</para>
    /// <para>必填：否</para>
    /// <para>示例值：8%</para>
    /// </summary>
    [JsonPropertyName("packet_loss_avg_sent")]
    public string? PacketLossAvgSent { get; set; }

    /// <summary>
    /// <para>丢包 - 最大（发送）</para>
    /// <para>必填：否</para>
    /// <para>示例值：10%</para>
    /// </summary>
    [JsonPropertyName("packet_loss_max_sent")]
    public string? PacketLossMaxSent { get; set; }
}