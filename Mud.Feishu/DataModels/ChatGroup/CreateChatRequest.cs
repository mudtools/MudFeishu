// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroup;

/// <summary>
/// 创建消息请求体。
/// </summary>
public class CreateChatRequest
{
    /// <summary>
    /// 群头像对应的 Image Key
    /// <para>示例值："default-avatar_44ae0ca3-e140-494b-956f-78091e348435"</para>
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    /// 群名称
    /// <para>示例值："测试群名称"</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 群描述，建议不超过 100 字符
    /// <para>示例值："测试群描述"</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 群国际化名称
    /// </summary>
    [JsonPropertyName("i18n_names")]
    public I18nName? I18nNames { get; set; }

    /// <summary>
    /// 创建群时指定的群主，不填时指定建群的机器人为群主。
    /// <para>示例值："ou_7d8a6e6df7621556ce0d21922b676706ccs"</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    /// <summary>
    /// 创建群时邀请的群成员，不填则不邀请成员。
    /// <para>示例值：["ou_7d8a6e6df7621556ce0d21922b676706ccs"]</para>
    /// </summary>
    [JsonPropertyName("user_id_list")]
    public List<string>? UserIdList { get; set; }

    /// <summary>
    /// 创建群时邀请的群机器人，不填则不邀请机器人。
    /// <para>示例值：["cli_a10fbf7e94b8d01d"]</para>
    /// </summary>
    [JsonPropertyName("bot_id_list")]
    public List<string>? BotIdList { get; set; }

    /// <summary>
    /// 群消息形式
    /// <para>示例值："chat"</para>
    /// <para>可选值有：</para>
    /// <para>chat：对话消息</para>
    /// <para>thread：话题消息</para>
    /// </summary>
    [JsonPropertyName("group_message_type")]
    public string? GroupMessageType { get; set; } = "chat";

    /// <summary>
    /// 群模式
    /// <para>可选值有：</para>
    /// <para>group：群组</para>
    /// <para>示例值："group"</para>
    /// </summary>
    [JsonPropertyName("chat_mode")]
    public string? ChatMode { get; set; } = "group";

    /// <summary>
    /// 群类型
    /// <para>可选值有：</para>
    /// <para>private：私有群</para>
    /// <para>public：公开群</para>
    /// <para>示例值："private"</para>
    /// </summary>
    [JsonPropertyName("chat_type")]
    public string? ChatType { get; set; } = "private";

    /// <summary>
    /// 成员入群提示消息的可见性
    /// <para>可选值有：</para>
    /// <para>only_owner：仅群主和管理员可见</para>
    /// <para>all_members：所有成员可见</para>
    /// <para>not_anyone：任何人均不可见</para>
    /// <para>示例值："all_members"</para>
    /// </summary>
    [JsonPropertyName("join_message_visibility")]
    public string? JoinMessageVisibility { get; set; } = "all_members";

    /// <summary>
    /// 成员退群提示消息的可见性
    /// <para>可选值有：</para>
    /// <para>only_owner：仅群主和管理员可见</para>
    /// <para>all_members：所有成员可见</para>
    /// <para>not_anyone：任何人均不可见</para>
    /// <para>示例值："all_members"</para>
    /// </summary>
    [JsonPropertyName("leave_message_visibility")]
    public string? LeaveMessageVisibility { get; set; } = "all_members";

    /// <summary>
    /// 加群是否需要审批
    /// <para>可选值有：</para>
    /// <para>no_approval_required：无需审批</para>
    /// <para>approval_required：需要审批</para>
    /// <para>示例值："no_approval_required"</para>
    /// </summary>
    [JsonPropertyName("membership_approval")]
    public string? MembershipApproval { get; set; } = "no_approval_required";

    /// <summary>
    /// 保密模式设置
    /// <para>注意：保密模式适用于企业旗舰版。</para>
    /// </summary>
    [JsonPropertyName("restricted_mode_setting")]
    public RestrictedModeSetting? RestrictedModeSetting { get; set; }

    /// <summary>
    /// 谁可以加急
    /// <para>默认值：all_members</para>
    /// <para>示例值："all_members"</para>
    /// <para>可选值有：</para>
    /// <para>only_owner：仅群主和管理员</para>
    /// <para>all_members：所有成员</para>
    /// </summary>
    [JsonPropertyName("urgent_setting")]
    public string? UrgentSetting { get; set; } = "all_members";

    /// <summary>
    /// 谁可以发起视频会议
    /// <para>默认值：all_members</para>
    /// <para>示例值："all_members"</para>
    /// <para>可选值有：</para>
    /// <para>only_owner：仅群主和管理员</para>
    /// <para>all_members：所有成员</para>
    /// </summary>
    [JsonPropertyName("video_conference_setting")]
    public string? VideoConferenceSetting { get; set; } = "all_members";

    /// <summary>
    /// 谁可以编辑群信息
    /// <para>默认值：all_members</para>
    /// <para>示例值："all_members"</para>
    /// <para>可选值有：</para>
    /// <para>only_owner：仅群主和管理员</para>
    /// <para>all_members：所有成员</para>
    /// </summary>
    [JsonPropertyName("edit_permission")]
    public string? EditPermission { get; set; } = "all_members";

    /// <summary>
    /// 隐藏群成员人数设置
    /// <para>默认值：all_members</para>
    /// <para>示例值："all_members"</para>
    /// <para>可选值有：</para>
    /// <para>all_members：所有群成员可见</para>
    /// <para>only_owner：仅群主群管理员可见</para>
    /// </summary>
    [JsonPropertyName("hide_member_count_setting")]
    public string? HideMemberCountSetting { get; set; } = "all_members";
}
