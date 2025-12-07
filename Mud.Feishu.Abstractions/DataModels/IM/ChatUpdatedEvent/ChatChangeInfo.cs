// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.IM;


/// <summary>
/// 群更新信息
/// </summary>
public class ChatChangeInfo
{
    /// <summary>
    /// <para>群头像</para>   
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    /// <para>群名称</para>   
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>群描述</para>   
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>群国际化名称</para>   
    /// </summary>
    [JsonPropertyName("i18n_names")]
    public I18nNames? I18nNames { get; set; }

    /// <summary>
    /// <para>谁可以邀请成员（用户或机器人）入群</para>
    /// <para>**可选值有**：</para>
    /// <para>- all_members：所有成员</para>
    /// <para>- only_owner：仅群主和管理员</para>
    /// <para>- unknown：未设置</para>   
    /// <para>可选值：<list type="bullet">
    /// <item>- all_members：所有成员</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("add_member_permission")]
    public string? AddMemberPermission { get; set; }

    /// <summary>
    /// <para>群分享权限</para>
    /// <para>**可选值有**：</para>
    /// <para>- allowed：允许</para>
    /// <para>- not_allowed：不允许</para>
    /// <para>- unknown：未设置</para>
    /// </summary>
    [JsonPropertyName("share_card_permission")]
    public string? ShareCardPermission { get; set; }

    /// <summary>
    /// <para>谁可以 at 所有人</para>
    /// <para>**可选值有**：</para>
    /// <para>- all_members：所有成员</para>
    /// <para>- only_owner：仅群主和管理员</para>
    /// <para>- unknown：未设置</para>
    /// </summary>
    [JsonPropertyName("at_all_permission")]
    public string? AtAllPermission { get; set; }

    /// <summary>
    /// <para>谁可以编辑群</para>
    /// <para>**可选值有**：</para>
    /// <para>- all_members：所有成员</para>
    /// <para>- only_owner：仅群主和管理员</para>
    /// <para>- unknown：未设置</para>
    /// </summary>
    [JsonPropertyName("edit_permission")]
    public string? EditPermission { get; set; }

    /// <summary>
    /// <para>加群是否需要审批</para>
    /// <para>**可选值有**：</para>
    /// <para>- no_approval_required：无需审批</para>
    /// <para>- approval_required：需要审批</para>
    /// </summary>
    [JsonPropertyName("membership_approval")]
    public string? MembershipApproval { get; set; }

    /// <summary>
    /// <para>入群提示消息的可见性</para>
    /// <para>**可选值有**：</para>
    /// <para>- only_owner：仅群主和管理员可见</para>
    /// <para>- all_members：所有成员可见</para>
    /// <para>- not_anyone：任何人均不可见</para>
    /// </summary>
    [JsonPropertyName("join_message_visibility")]
    public string? JoinMessageVisibility { get; set; }

    /// <summary>
    /// <para>出群消息可见性</para>
    /// <para>**可选值有**：</para>
    /// <para>- only_owner：仅群主和管理员可见</para>
    /// <para>- all_members：所有成员可见</para>
    /// <para>- not_anyone：任何人均不可见</para>
    /// </summary>
    [JsonPropertyName("leave_message_visibility")]
    public string? LeaveMessageVisibility { get; set; }

    /// <summary>
    /// <para>发言权限</para>
    /// <para>**可选值有**：</para>
    /// <para>- all_members：所有成员</para>
    /// <para>- only_owner：仅群主和管理员</para>
    /// <para>**说明**：发言权限为指定群成员时，需要参考 `moderator_list` 参数。</para>
    /// </summary>
    [JsonPropertyName("moderation_permission")]
    public string? ModerationPermission { get; set; }

    /// <summary>
    /// <para>群主 ID</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public UserIdInfo? OwnerId { get; set; }

    /// <summary>
    /// <para>防泄密模式设置</para>
    /// </summary>
    [JsonPropertyName("restricted_mode_setting")]
    public RestrictedModeSetting? RestrictedModeSetting { get; set; }

    /// <summary>
    /// <para>群消息形式</para>
    /// <para>**可选值有**：</para>
    /// <para>- chat：会话消息</para>
    /// <para>- thread：话题消息</para>
    /// </summary>
    [JsonPropertyName("group_message_type")]
    public string? GroupMessageType { get; set; }
}