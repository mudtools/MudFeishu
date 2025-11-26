// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroup;

/// <summary>
/// 聊天群组管理员分页列表结果
/// </summary>
public class ChatGroupModeratorPageListResult : ApiPageListResult
{
    /// <summary>
    /// 管理设置
    /// <para>表示群组的管理模式或权限设置</para>
    /// </summary>
    [JsonPropertyName("moderation_setting")]
    public string? ModerationSetting { get; set; }

    /// <summary>
    /// 管理员项目列表
    /// <para>包含群组中所有管理员信息的集合</para>
    /// </summary>
    [JsonPropertyName("items")]
    public List<ModeratorItem>? Items { get; set; }
}


/// <summary>
/// 表示群组中单个管理员的详细信息
/// </summary>
public class ModeratorItem
{
    /// <summary>
    /// 用户ID类型
    /// <para>标识用户ID的类型，如：user_id、open_id、union_id等</para>
    /// </summary>
    [JsonPropertyName("user_id_type")]
    public string? UserIdType { get; set; }

    /// <summary>
    /// 用户ID
    /// <para>管理员的唯一标识符</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// 租户标识
    /// <para>标识用户所属的租户，用于多租户环境下的区分</para>
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }
}
