// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>参与人类型对应的 ID，该 ID 是 attendee_ids 字段的补充字段。</para>
/// <para>- 一次最多删除500个参与人（与attendee_ids一起计算）</para>
/// </summary>
public class CalendarEventAttendeeId
{
    /// <summary>
    /// <para>参与人类型。</para>
    /// <para>必填：否</para>
    /// <para>示例值：user</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user：用户</item>
    /// <item>chat：群组</item>
    /// <item>resource：会议室</item>
    /// <item>third_party：外部邮箱</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>用户 ID。当选择用户类型参与人（type 取值为 user）时，需要传入该参数。传入的用户 ID 类型需要和 user_id_type 的值保持一致。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_xxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>群组 ID。当选择群组类型参与人（type 取值为 chat）时，需要传入该参数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：oc_xxxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// <para>会议室 ID。当选择会议室类型参与人（type 取值为 resource）时，需要传入该参数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：omm_xxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }

    /// <summary>
    /// <para>邮箱地址。当选择外部邮箱类型参与人（type 取值为 third_party）时，需要传入该参数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：wangwu@email.com</para>
    /// </summary>
    [JsonPropertyName("third_party_email")]
    public string? ThirdPartyEmail { get; set; }
}
