// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>会议基本信息</para>
/// </summary>
public class MeetingBaseInfo
{
    /// <summary>
    /// <para>会议ID（视频会议的唯一标识，视频会议开始后才会产生）</para>
    /// <para>必填：否</para>
    /// <para>示例值：6911188411934433028</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>会议主题</para>
    /// <para>必填：否</para>
    /// <para>示例值：my meeting</para>
    /// </summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    /// <summary>
    /// <para>会议链接（飞书用户可通过点击会议链接快捷入会）</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://vc.feishu.cn/j/337736498</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>会议号</para>
    /// <para>必填：否</para>
    /// <para>示例值：123456789</para>
    /// </summary>
    [JsonPropertyName("meeting_no")]
    public string? MeetingNo { get; set; }

    /// <summary>
    /// <para>会议密码</para>
    /// <para>必填：否</para>
    /// <para>示例值：971024</para>
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// <para>该会议是否支持互通</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("meeting_connect")]
    public bool? MeetingConnect { get; set; }

    /// <summary>
    /// <para>纪要ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6943848821689040898</para>
    /// </summary>
    [JsonPropertyName("note_id")]
    public string? NoteId { get; set; }
}