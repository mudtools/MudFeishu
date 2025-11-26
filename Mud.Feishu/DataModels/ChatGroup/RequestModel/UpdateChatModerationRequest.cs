// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroup;

/// <summary>
/// 更新群发言权限请求体。
/// </summary>
public class UpdateChatModerationRequest
{
    /// <summary>
    /// 群发言模式 可选值有：
    /// <para>all_members：所有群成员可发言</para>
    /// <para>only_owner：仅群主或管理员可发言</para>
    /// <para>moderator_list：指定群成员可发言，取该值时需要选择设置 moderator_added_list 和 moderator_removed_list</para>
    /// <para>示例值："moderator_list"</para>
    /// </summary>
    [JsonPropertyName("moderation_setting")]
    public string? ModerationSetting { get; set; }

    /// <summary>
    /// 当 moderation_setting 取值为 moderator_list 时，以 ID 列表形式添加可发言的用户。
    /// <para>示例值：["4d7a3c6g"]</para>
    /// </summary>
    [JsonPropertyName("moderator_added_list")]
    public List<string> ModeratorAddedList { get; set; } = [];

    /// <summary>
    /// 当 moderation_setting 取值为 moderator_list 时，以 ID 列表形式移除可发言的用户。
    /// <para>示例值：["4d7a3c6g"]</para>
    /// </summary>
    [JsonPropertyName("moderator_removed_list")]
    public List<string> ModeratorRemovedList { get; set; } = [];
}
