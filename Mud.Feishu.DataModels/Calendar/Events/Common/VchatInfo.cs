// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>视频会议信息</summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class VchatInfo
{
    /// <summary>
    /// <para>视屏会议类型</para>
    /// <para> **可选值有：**</para>
    /// <para>- `vc`：飞书视频会议，取该类型时，其他字段无效。</para>
    /// <para>- `third_party`：第三方链接视频会议，取该类型时，icon_type、description、meeting_url字段生效。</para>
    /// <para>- `no_meeting`：无视频会议，取该类型时，其他字段无效。</para>
    /// <para>- `lark_live`：飞书直播，内部类型，飞书客户端使用，API不支持创建，只读。</para>
    /// <para>- `unknown`：未知类型，做兼容使用，飞书客户端使用，API不支持创建，只读。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("vc_type")]
    public string? VcType { get; set; }

    /// <summary>
    /// <para>第三方视频会议icon类型；可以为空，为空展示默认icon。</para>
    /// <para>**可选值有：**</para>
    /// <para>- `vc`：飞书视频会议icon</para>
    /// <para>- `live`：直播视频会议icon</para>
    /// <para>- `default`：默认icon</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("icon_type")]
    public string? IconType { get; set; }

    /// <summary>
    /// <para>第三方视频会议文案，可以为空，为空展示默认文案</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>视频会议URL</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_url")]
    public string? MeetingUrl { get; set; }
}
