// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;


/// <summary>日程信息</summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class EventInfo
{
    /// <summary>
    /// <para>日程的唯一 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    /// <summary>
    /// <para>日程实例原始时间。非重复性日程和重复性日程，此处为 0；重复性日程的例外日程，此处为对应的 original_time 值（时间戳类型）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("original_time")]
    public long? OriginalTime { get; set; }

    /// <summary>
    /// <para>日程主题。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// <para>视频会议信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("vchat")]
    public VchatInfo? Vchat { get; set; }


}
