// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroup;

/// <summary>
/// 获取群组信息结果
/// </summary>
public class GetChatGroupInfoResult : ChatGroupBase
{
    /// <summary>
    /// 群主ID类型
    /// </summary>
    [JsonPropertyName("owner_id_type")]
    public string? OwnerIdType { get; set; }

    /// <summary>
    /// 用户管理员ID列表
    /// </summary>
    [JsonPropertyName("user_manager_id_list")]
    public List<string>? UserManagerIdList { get; set; }

    /// <summary>
    /// 机器人管理员ID列表
    /// </summary>
    [JsonPropertyName("bot_manager_id_list")]
    public List<string>? BotManagerIdList { get; set; }

    /// <summary>
    /// 用户数量
    /// </summary>
    [JsonPropertyName("user_count")]
    public string? UserCount { get; set; }

    /// <summary>
    /// 机器人数量
    /// </summary>
    [JsonPropertyName("bot_count")]
    public string? BotCount { get; set; }

    /// <summary>
    /// 聊天状态
    /// </summary>
    [JsonPropertyName("chat_status")]
    public string? ChatStatus { get; set; }
}
