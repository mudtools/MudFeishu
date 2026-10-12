// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Helpdesk;

/// <summary>
/// 工单备注信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Helpdesk")]
public class HelpdeskTicketComments
{
    /// <summary>
    /// <para>备注内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>备注创建时间，单位为毫秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }

    /// <summary>
    /// <para>备注 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    /// <summary>
    /// <para>写备注的用户头像</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_avatar_url")]
    public string? UserAvatarUrl { get; set; }

    /// <summary>
    /// <para>写备注的用户名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    /// <summary>
    /// <para>写备注的用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }
}
