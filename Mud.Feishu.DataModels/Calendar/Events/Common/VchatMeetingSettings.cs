// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>飞书视频会议（VC）的会前设置，需满足以下全部条件：</para>
/// <para>- 当 `vc_type` 为 `vc` 时生效。</para>
/// <para>- 需要有日程的编辑权限。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class VchatMeetingSettings
{
    /// <summary>
    /// <para>设置会议 owner 的用户 ID，ID 类型需和 user_id</para>
    /// <para>_type 保持一致。</para>
    /// <para>该参数需满足以下全部条件才会生效：</para>
    /// <para>- 应用身份（tenant_access_token）请求，且在应用日历上操作日程。</para>
    /// <para>- 首次将日程设置为 VC 会议时，才能设置owner。</para>
    /// <para>- owner 不能为非用户身份。</para>
    /// <para>- owner 不能为外部租户用户身份。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_7d8a6e6df7621556ce0d21922b676706ccs</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    /// <summary>
    /// <para>设置入会范围。</para>
    /// <para>**默认值**：anyone_can_join</para>
    /// <para>必填：否</para>
    /// <para>示例值：only_organization_employees</para>
    /// <para>可选值：<list type="bullet">
    /// <item>anyone_can_join：所有人可以加入会议</item>
    /// <item>only_organization_employees：仅企业内的用户可以加入会议</item>
    /// <item>only_event_attendees：仅日程参与者可以加入会议</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("join_meeting_permission")]
    public string? JoinMeetingPermission { get; set; }

    /// <summary>
    /// <para>通过用户 ID 指定主持人，ID 类型需和 user_id</para>
    /// <para>_type 保持一致。</para>
    /// <para>**注意**：</para>
    /// <para>- 仅日程组织者可以指定主持人。</para>
    /// <para>- 主持人不能是非用户身份。</para>
    /// <para>- 主持人不能是外部租户用户身份。</para>
    /// <para>- 在应用日历上操作日程时，不允许指定主持人。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10</para>
    /// </summary>
    [JsonPropertyName("assign_hosts")]
    public string[]? AssignHosts { get; set; }

    /// <summary>
    /// <para>是否开启自动录制。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true：开启</para>
    /// <para>- false（默认值）：不开启</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("auto_class")]
    public bool? Autoclass { get; set; }

    /// <summary>
    /// <para>是否开启等候室。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true（默认值）：开启</para>
    /// <para>- false：不开启</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("open_lobby")]
    public bool? OpenLobby { get; set; }

    /// <summary>
    /// <para>是否允许日程参与者发起会议。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true（默认值）：允许</para>
    /// <para>- false：不允许</para>
    /// <para>**注意**：应用日历上操作日程时，该字段必须为 true，否则没有人能发起会议。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_attendees_start")]
    public bool? AllowAttendeesStart { get; set; }
}
