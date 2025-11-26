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
public class CreateChatRequest : ChatGroupBase
{
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
    public new string? GroupMessageType { get; set; } = "chat";

    /// <summary>
    /// 群模式
    /// <para>可选值有：</para>
    /// <para>group：群组</para>
    /// <para>示例值："group"</para>
    /// </summary>
    [JsonPropertyName("chat_mode")]
    public new string? ChatMode { get; set; } = "group";

    /// <summary>
    /// 群类型
    /// <para>可选值有：</para>
    /// <para>private：私有群</para>
    /// <para>public：公开群</para>
    /// <para>示例值："private"</para>
    /// </summary>
    [JsonPropertyName("chat_type")]
    public new string? ChatType { get; set; } = "private";

    /// <summary>
    /// 成员入群提示消息的可见性
    /// <para>可选值有：</para>
    /// <para>only_owner：仅群主和管理员可见</para>
    /// <para>all_members：所有成员可见</para>
    /// <para>not_anyone：任何人均不可见</para>
    /// <para>示例值："all_members"</para>
    /// </summary>
    [JsonPropertyName("join_message_visibility")]
    public new string? JoinMessageVisibility { get; set; } = "all_members";

    /// <summary>
    /// 成员退群提示消息的可见性
    /// <para>可选值有：</para>
    /// <para>only_owner：仅群主和管理员可见</para>
    /// <para>all_members：所有成员可见</para>
    /// <para>not_anyone：任何人均不可见</para>
    /// <para>示例值："all_members"</para>
    /// </summary>
    [JsonPropertyName("leave_message_visibility")]
    public new string? LeaveMessageVisibility { get; set; } = "all_members";

    /// <summary>
    /// 加群是否需要审批
    /// <para>可选值有：</para>
    /// <para>no_approval_required：无需审批</para>
    /// <para>approval_required：需要审批</para>
    /// <para>示例值："no_approval_required"</para>
    /// </summary>
    [JsonPropertyName("membership_approval")]
    public new string? MembershipApproval { get; set; } = "no_approval_required";

    /// <summary>
    /// 谁可以加急
    /// <para>默认值：all_members</para>
    /// <para>示例值："all_members"</para>
    /// <para>可选值有：</para>
    /// <para>only_owner：仅群主和管理员</para>
    /// <para>all_members：所有成员</para>
    /// </summary>
    [JsonPropertyName("urgent_setting")]
    public new string? UrgentSetting { get; set; } = "all_members";

    /// <summary>
    /// 谁可以发起视频会议
    /// <para>默认值：all_members</para>
    /// <para>示例值："all_members"</para>
    /// <para>可选值有：</para>
    /// <para>only_owner：仅群主和管理员</para>
    /// <para>all_members：所有成员</para>
    /// </summary>
    [JsonPropertyName("video_conference_setting")]
    public new string? VideoConferenceSetting { get; set; } = "all_members";

    /// <summary>
    /// 谁可以编辑群信息
    /// <para>默认值：all_members</para>
    /// <para>示例值："all_members"</para>
    /// <para>可选值有：</para>
    /// <para>only_owner：仅群主和管理员</para>
    /// <para>all_members：所有成员</para>
    /// </summary>
    [JsonPropertyName("edit_permission")]
    public new string? EditPermission { get; set; } = "all_members";

    /// <summary>
    /// 隐藏群成员人数设置
    /// <para>默认值：all_members</para>
    /// <para>示例值："all_members"</para>
    /// <para>可选值有：</para>
    /// <para>all_members：所有群成员可见</para>
    /// <para>only_owner：仅群主群管理员可见</para>
    /// </summary>
    [JsonPropertyName("hide_member_count_setting")]
    public new string? HideMemberCountSetting { get; set; } = "all_members";
}
