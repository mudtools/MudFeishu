// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;



/// <summary>
/// <para>在请求的时间区间内的忙碌时间段列表。</para>
/// </summary>
public class Freebusy
{
    /// <summary>
    /// <para>忙闲信息开始时间，[RFC 3339](https://datatracker.ietf.org/doc/html/rfc3339) date_time 格式。</para>
    /// <para>必填：是</para>
    /// <para>示例值：2020-10-28T22:30:00+08:00</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>忙闲信息结束时间，[RFC 3339](https://datatracker.ietf.org/doc/html/rfc3339) date_time 格式。</para>
    /// <para>必填：是</para>
    /// <para>示例值：2020-10-28T22:45:00+08:00</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户RSVP状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：needs_action</para>
    /// <para>可选值：<list type="bullet">
    /// <item>needs_action：参与人尚未回复状态，或表示会议室预约中</item>
    /// <item>accept：参与人回复接受，或表示会议室预约成功</item>
    /// <item>tentative：参与人回复待定</item>
    /// <item>decline：参与人回复拒绝，或表示会议室预约失败</item>
    /// <item>removed：参与人或会议室已经从日程中被移除</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("rsvp_status")]
    public string? RsvpStatus { get; set; }
}