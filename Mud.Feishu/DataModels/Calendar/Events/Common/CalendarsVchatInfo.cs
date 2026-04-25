// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;


/// <summary>
/// <para>视频会议信息。</para>
/// </summary>
public class CalendarsVchatInfo
{
    /// <summary>
    /// <para>视频会议类型。如果无需视频会议，则必须传入 `no_meeting`。</para>
    /// <para>**默认值**：空，表示在首次添加日程参与人时，会自动生成飞书视频会议 URL。</para>
    /// <para>必填：否</para>
    /// <para>示例值：third_party</para>
    /// <para>可选值：<list type="bullet">
    /// <item>vc：飞书视频会议。取该类型时，vchat 内的其他字段均无效。</item>
    /// <item>third_party：第三方链接视频会议。取该类型时，仅生效 vchat 内的 icon_type、description、meeting_url 字段。</item>
    /// <item>no_meeting：无视频会议。取该类型时，vchat 内的其他字段均无效。</item>
    /// <item>lark_live：飞书直播。该值用于客户端，不支持通过 API 调用，只读。</item>
    /// <item>unknown：未知类型。该值用于客户端做兼容使用，不支持通过 API 调用，只读。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("vc_type")]
    public string? VcType { get; set; }

    /// <summary>
    /// <para>第三方视频会议的 icon 类型。</para>
    /// <para>**默认值**：default</para>
    /// <para>必填：否</para>
    /// <para>示例值：vc</para>
    /// <para>可选值：<list type="bullet">
    /// <item>vc：飞书视频会议 icon。</item>
    /// <item>live：直播视频会议 icon。</item>
    /// <item>default：默认 icon。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("icon_type")]
    public string? IconType { get; set; }

    /// <summary>
    /// <para>第三方视频会议文案。</para>
    /// <para>**默认值**：空，为空展示默认文案。</para>
    /// <para>必填：否</para>
    /// <para>示例值：发起视频会议</para>
    /// <para>最大长度：500</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>视频会议 URL。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://example.com</para>
    /// <para>最大长度：2000</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("meeting_url")]
    public string? MeetingUrl { get; set; }

    /// <summary>
    /// <para>飞书视频会议（VC）的会前设置，需满足以下全部条件：</para>
    /// <para>- 当 `vc_type` 为 `vc` 时生效。</para>
    /// <para>- 需要有日程的编辑权限。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("meeting_settings")]
    public VchatMeetingSettings? MeetingSettings { get; set; }

}
