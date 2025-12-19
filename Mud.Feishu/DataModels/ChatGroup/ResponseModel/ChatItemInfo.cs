// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroup;

/// <summary>
/// 聊天项目信息
/// </summary>
/// <summary>
/// 表示聊天群组的基本信息，包含群组的标识、名称、描述等核心属性
/// </summary>
public class ChatItemInfo
{
    /// <summary>
    /// 聊天ID
    /// <para>群组的唯一标识符，用于API调用和群组识别</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// 群组头像
    /// <para>群组头像的URL或图片标识</para>
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    /// 群组名称
    /// <para>群组的显示名称，用户在客户端看到的群组名</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 群组描述
    /// <para>群组的详细描述信息，通常用于说明群组的用途或规则</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 群主ID
    /// <para>群组创建者或当前群主的唯一标识符</para>
    /// </summary>
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    /// <summary>
    /// 群主ID类型
    /// <para>标识群主ID的类型，如：user_id、open_id、union_id等</para>
    /// </summary>
    [JsonPropertyName("owner_id_type")]
    public string? OwnerIdType { get; set; }

    /// <summary>
    /// 是否为外部群组
    /// <para>标识群组是否为外部群组，true表示外部群组，false表示内部群组</para>
    /// <para>外部群组通常包含外部用户或跨组织的成员</para>
    /// </summary>
    [JsonPropertyName("external")]
    public bool External { get; set; }

    /// <summary>
    /// 租户标识
    /// <para>标识群组所属的租户，用于多租户环境下的群组管理</para>
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }

    /// <summary>
    /// 聊天状态
    /// <para>表示群组的当前状态，如：active（活跃）、archived（已归档）等</para>
    /// </summary>
    [JsonPropertyName("chat_status")]
    public string? ChatStatus { get; set; }
}
