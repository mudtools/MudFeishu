// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.IM;

/// <summary>
/// 消息发送者
/// </summary>
public class MessageSender
{
    /// <summary>
    /// <para>用户 ID。</para>    
    /// </summary>
    [JsonPropertyName("sender_id")]
    public UserIdInfo? SenderId { get; set; }

    /// <summary>
    /// <para>消息发送者类型。目前只支持用户(user)发送的消息。</para>    
    /// </summary>
    [JsonPropertyName("sender_type")]
    public string? SenderType { get; set; }

    /// <summary>
    /// <para>tenant key，为租户在飞书上的唯一标识，用来换取对应的tenant_access_token，也可以用作租户在应用里面的唯一标识</para>
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }
}