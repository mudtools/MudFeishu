// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.VideoConferencing;

/// <summary>
/// 录制完成
/// <para>发生在录制完成时【仅通过Open API预约的会议会产生此类事件】</para>
/// <para>事件类型:vc.meeting.recording_ready_v1</para>
/// <para>使用时请继承：<see cref="MeetingRecordingReadyEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/vc-v1/meeting/events/recording_ready</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.MeetingRecordingReady, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class MeetingRecordingReadyResult : IEventResult
{
    /// <summary>
    /// <para>会议数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting")]
    public MeetingEventMeeting? Meeting { get; set; }

    /// <summary>
    /// <para>会议录制链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>录制总时长（单位msec）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

}
