// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroup;

public class GetChatGroupInfoResult
{
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("i18n_names")]
    public I18nName? I18nNames { get; set; }

    [JsonPropertyName("add_member_permission")]
    public string? AddMemberPermission { get; set; }

    [JsonPropertyName("share_card_permission")]
    public string? ShareCardPermission { get; set; }

    [JsonPropertyName("at_all_permission")]
    public string? AtAllPermission { get; set; }

    [JsonPropertyName("edit_permission")]
    public string? EditPermission { get; set; }

    [JsonPropertyName("owner_id_type")]
    public string? OwnerIdType { get; set; }

    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    [JsonPropertyName("user_manager_id_list")]
    public List<string>? UserManagerIdList { get; set; }

    [JsonPropertyName("bot_manager_id_list")]
    public List<string>? BotManagerIdList { get; set; }

    [JsonPropertyName("group_message_type")]
    public string? GroupMessageType { get; set; }

    [JsonPropertyName("chat_mode")]
    public string? ChatMode { get; set; }

    [JsonPropertyName("chat_type")]
    public string? ChatType { get; set; }

    [JsonPropertyName("chat_tag")]
    public string? ChatTag { get; set; }

    [JsonPropertyName("join_message_visibility")]
    public string? JoinMessageVisibility { get; set; }

    [JsonPropertyName("leave_message_visibility")]
    public string? LeaveMessageVisibility { get; set; }

    [JsonPropertyName("membership_approval")]
    public string? MembershipApproval { get; set; }

    [JsonPropertyName("moderation_permission")]
    public string? ModerationPermission { get; set; }

    [JsonPropertyName("external")]
    public bool? External { get; set; }

    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }

    [JsonPropertyName("user_count")]
    public string? UserCount { get; set; }

    [JsonPropertyName("bot_count")]
    public string? BotCount { get; set; }

    [JsonPropertyName("restricted_mode_setting")]
    public RestrictedModeSetting? RestrictedModeSetting { get; set; }

    [JsonPropertyName("urgent_setting")]
    public string? UrgentSetting { get; set; }

    [JsonPropertyName("video_conference_setting")]
    public string? VideoConferenceSetting { get; set; }

    [JsonPropertyName("hide_member_count_setting")]
    public string? HideMemberCountSetting { get; set; }

    [JsonPropertyName("chat_status")]
    public string? ChatStatus { get; set; }
}
