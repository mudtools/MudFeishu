// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroup;

/// <summary>
/// 聊天群组基础类，包含所有聊天群组相关的通用属性
/// </summary>
public class ChatGroupBase
{
    /// <summary>
    /// 群组头像
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    /// 群组名称
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 群组描述
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 国际化名称
    /// </summary>
    [JsonPropertyName("i18n_names")]
    public I18nName? I18nNames { get; set; }

    /// <summary>
    /// 添加成员权限
    /// </summary>
    [JsonPropertyName("add_member_permission")]
    public string? AddMemberPermission { get; set; }

    /// <summary>
    /// 分享名片权限
    /// </summary>
    [JsonPropertyName("share_card_permission")]
    public string? ShareCardPermission { get; set; }

    /// <summary>
    /// @所有人权限
    /// </summary>
    [JsonPropertyName("at_all_permission")]
    public string? AtAllPermission { get; set; }

    /// <summary>
    /// 编辑权限
    /// </summary>
    [JsonPropertyName("edit_permission")]
    public string? EditPermission { get; set; }

    /// <summary>
    /// 群消息类型
    /// </summary>
    [JsonPropertyName("group_message_type")]
    public string? GroupMessageType { get; set; }

    /// <summary>
    /// 聊天模式
    /// </summary>
    [JsonPropertyName("chat_mode")]
    public string? ChatMode { get; set; }

    /// <summary>
    /// 聊天类型
    /// </summary>
    [JsonPropertyName("chat_type")]
    public string? ChatType { get; set; }

    /// <summary>
    /// 聊天标签
    /// </summary>
    [JsonPropertyName("chat_tag")]
    public string? ChatTag { get; set; }

    /// <summary>
    /// 入群消息可见性
    /// </summary>
    [JsonPropertyName("join_message_visibility")]
    public string? JoinMessageVisibility { get; set; }

    /// <summary>
    /// 退群消息可见性
    /// </summary>
    [JsonPropertyName("leave_message_visibility")]
    public string? LeaveMessageVisibility { get; set; }

    /// <summary>
    /// 成员审批设置
    /// </summary>
    [JsonPropertyName("membership_approval")]
    public string? MembershipApproval { get; set; }

    /// <summary>
    /// 管理权限
    /// </summary>
    [JsonPropertyName("moderation_permission")]
    public string? ModerationPermission { get; set; }

    /// <summary>
    /// 是否外部群组
    /// </summary>
    [JsonPropertyName("external")]
    public bool? External { get; set; }

    /// <summary>
    /// 租户标识
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }

    /// <summary>
    /// 保密模式设置
    /// </summary>
    [JsonPropertyName("restricted_mode_setting")]
    public RestrictedModeSetting? RestrictedModeSetting { get; set; }

    /// <summary>
    /// 紧急消息设置
    /// </summary>
    [JsonPropertyName("urgent_setting")]
    public string? UrgentSetting { get; set; }

    /// <summary>
    /// 视频会议设置
    /// </summary>
    [JsonPropertyName("video_conference_setting")]
    public string? VideoConferenceSetting { get; set; }

    /// <summary>
    /// 隐藏成员数设置
    /// </summary>
    [JsonPropertyName("hide_member_count_setting")]
    public string? HideMemberCountSetting { get; set; }

    /// <summary>
    /// 群主ID
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }
}