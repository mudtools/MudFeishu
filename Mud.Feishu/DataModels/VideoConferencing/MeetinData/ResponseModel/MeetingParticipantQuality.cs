// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>参会人参会质量列表</para>
/// </summary>
public class MeetingParticipantQuality
{
    /// <summary>
    /// <para>网络</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("network")]
    public QualityNetwork? Network { get; set; }

    /// <summary>
    /// <para>音频</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("audio")]
    public QualityAudio? Audio { get; set; }

    /// <summary>
    /// <para>视频</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("video")]
    public QualityVideoSharing? Video { get; set; }


    /// <summary>
    /// <para>共享屏幕</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("screen_sharing")]
    public QualityVideoSharing? ScreenSharing { get; set; }

    /// <summary>
    /// <para>Cpu使用量</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cpu_usage")]
    public QualityCpuUsage? CpuUsage { get; set; }

}