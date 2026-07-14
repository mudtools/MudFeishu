// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels;

/// <summary>
/// <para>邮件 Owner 信息</para>
/// </summary>
public class MailUserInfo
{
    /// <summary>
    /// <para>owner是个人邮箱还是公共邮箱</para>
    /// <para>**示例值**：</para>
    /// <para>- `user`：个人邮箱</para>
    /// <para>- `public_mailbox`：公共邮箱</para>
    /// <para>必填：是</para>
    /// <para>示例值：user</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>邮件卡片owner的ID，type为`user`时非空（与`user_id_type`对应）</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_7dab8a3d3cdcc9da365777c7ad115d62</para>
    /// </summary>
    [JsonPropertyName("owner_user_id")]
    public string? OwnerUserId { get; set; }

    /// <summary>
    /// <para>公共邮箱唯一标识，type为`public_mailbox`时非空</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxx</para>
    /// </summary>
    [JsonPropertyName("public_mailbox_id")]
    public string? PublicMailboxId { get; set; }
}