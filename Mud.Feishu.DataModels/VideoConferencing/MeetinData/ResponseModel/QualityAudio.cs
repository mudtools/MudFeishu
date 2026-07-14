// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;



/// <summary>
/// <para>音频</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class QualityAudio
{
    /// <summary>
    /// <para>时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022.12.23 11:16:00 (GMT+08:00)</para>
    /// </summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    /// <summary>
    /// <para>麦克风采集音量</para>
    /// <para>必填：否</para>
    /// <para>示例值：6dB</para>
    /// </summary>
    [JsonPropertyName("mic_input_volume")]
    public string? MicInputVolume { get; set; }

    /// <summary>
    /// <para>扬声器播放音量</para>
    /// <para>必填：否</para>
    /// <para>示例值：8dB</para>
    /// </summary>
    [JsonPropertyName("speaker_volume")]
    public string? SpeakerVolume { get; set; }

    /// <summary>
    /// <para>码率（接收）</para>
    /// <para>必填：否</para>
    /// <para>示例值：3kbps</para>
    /// </summary>
    [JsonPropertyName("bitrate_received")]
    public string? BitrateReceived { get; set; }

    /// <summary>
    /// <para>延迟（接收）</para>
    /// <para>必填：否</para>
    /// <para>示例值：100ms</para>
    /// </summary>
    [JsonPropertyName("latency_received")]
    public string? LatencyReceived { get; set; }

    /// <summary>
    /// <para>抖动（接收）</para>
    /// <para>必填：否</para>
    /// <para>示例值：100ms</para>
    /// </summary>
    [JsonPropertyName("jitter_received")]
    public string? JitterReceived { get; set; }

    /// <summary>
    /// <para>码率（发送）</para>
    /// <para>必填：否</para>
    /// <para>示例值：9kbps</para>
    /// </summary>
    [JsonPropertyName("bitrate_sent")]
    public string? BitrateSent { get; set; }

    /// <summary>
    /// <para>延迟（发送）</para>
    /// <para>必填：否</para>
    /// <para>示例值：100ms</para>
    /// </summary>
    [JsonPropertyName("latency_sent")]
    public string? LatencySent { get; set; }

    /// <summary>
    /// <para>抖动（发送）</para>
    /// <para>必填：否</para>
    /// <para>示例值：100ms</para>
    /// </summary>
    [JsonPropertyName("jitter_sent")]
    public string? JitterSent { get; set; }
}
