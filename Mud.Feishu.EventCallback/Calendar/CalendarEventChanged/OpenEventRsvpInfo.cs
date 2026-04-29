// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Calendar;


/// <summary></summary>
public class OpenEventRsvpInfo
{
    /// <summary>
    /// <para>用户类型参与人的用户 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("from_user_id")]
    public UserIdInfo? FromUserId { get; set; }

    /// <summary>
    /// <para>RSVP 操作状态。</para>
    /// <para>**可能值有：**</para>
    /// <para>- accept：接收</para>
    /// <para>- decline：拒绝</para>
    /// <para>- tentative：待定</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("rsvp_status")]
    public string? RsvpStatus { get; set; }
}
