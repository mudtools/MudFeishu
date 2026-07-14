// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>日程参与人信息，当前只返回会议室，需要其他类型参与人信息请使用[获取日程参与人列表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar-event-attendee/list)接口。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class CalendarEventAttendeeInstance
{
    /// <summary>
    /// <para>参与人类型。</para>
    /// <para>必填：否</para>
    /// <para>示例值：user</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user：用户</item>
    /// <item>chat：群组</item>
    /// <item>resource：会议室</item>
    /// <item>third_party：邮箱</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>参与人 ID。日程参与人在当前日程内的唯一标识。</para>
    /// <para>必填：否</para>
    /// <para>示例值：user_xxxxxx</para>
    /// </summary>
    [JsonPropertyName("attendee_id")]
    public string? AttendeeId { get; set; }

    /// <summary>
    /// <para>参与人 RSVP 状态，即日程回复状态。</para>
    /// <para>必填：否</para>
    /// <para>示例值：accept</para>
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

    /// <summary>
    /// <para>参与人是否为可选参加，该参数值对群组的群成员不生效。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("is_optional")]
    public bool? IsOptional { get; set; }

    /// <summary>
    /// <para>参与人是否为日程组织者。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_organizer")]
    public bool? IsOrganizer { get; set; }

    /// <summary>
    /// <para>参与人是否为外部参与人。外部参与人不支持编辑。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_external")]
    public bool? IsExternal { get; set; }

    /// <summary>
    /// <para>参与人名称。</para>
    /// <para>必填：否</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// <para>群中的群成员，当参与人类型（type）为 chat 时有效。群成员不支持编辑。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chat_members")]
    public AttendeeChatMember[]? ChatMembers { get; set; }


    /// <summary>
    /// <para>用户类型参与人的用户 ID，ID 类型与 user_id_type 的值保持一致。关于用户 ID 可参见[用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>**注意**：当 is_external 返回为 true 时，此字段只会返回 open_id 或者 union_id。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_xxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>群组类型参与人的群组 ID。关于群组 ID 可参见[群 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat-id-description)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：oc_a0553eda9014c201e6969b478895c230</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// <para>会议室类型参与人的会议室 ID。</para>
    /// <para>必填：否</para>
    /// <para>示例值：omm_83d09ad4f6896e02029a6a075f71c9d1</para>
    /// </summary>
    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }

    /// <summary>
    /// <para>外部邮箱类型参与人的邮箱地址。</para>
    /// <para>必填：否</para>
    /// <para>示例值：test@example.com</para>
    /// </summary>
    [JsonPropertyName("third_party_email")]
    public string? ThirdPartyEmail { get; set; }

    /// <summary>
    /// <para>如果日程是使用应用身份创建的，在添加会议室时，指定的会议室联系人 ID。ID 类型与 user_id_type 的值保持一致。</para>
    /// <para>必填：否</para>
    /// <para>示例值：4d7a3c6g</para>
    /// </summary>
    [JsonPropertyName("operate_id")]
    public string? OperateId { get; set; }

    /// <summary>
    /// <para>会议室的个性化配置。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("resource_customization")]
    public CalendarAttendeeResourceCustomization[]? ResourceCustomizations { get; set; }


    /// <summary>
    /// <para>会议室的审批原因。</para>
    /// <para>必填：否</para>
    /// <para>示例值：申请审批原因</para>
    /// <para>最大长度：200</para>
    /// </summary>
    [JsonPropertyName("approval_reason")]
    public string? ApprovalReason { get; set; }
}
