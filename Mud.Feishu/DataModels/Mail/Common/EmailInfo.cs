// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>用户可访问的所有邮箱信息，包含主邮箱和公共邮箱</para>
/// </summary>
public class EmailBaseInfo
{
    /// <summary>
    /// <para>邮箱地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：abc@abc.com</para>
    /// </summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }

    /// <summary>
    /// <para>邮箱地址类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：USER_PRIMARY</para>
    /// <para>可选值：<list type="bullet">
    /// <item>MAIL_GROUP：邮件组</item>
    /// <item>PUBLIC_MAILBOX：公共邮箱</item>
    /// <item>USER_PRIMARY：用户主地址</item>
    /// <item>USER_ALIAS：用户别名</item>
    /// <item>PUBLIC_MAILBOX_ALIAS：公共邮箱别名</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("email_type")]
    public string? EmailType { get; set; }
}

/// <summary>
/// <para>可发信地址。包括主地址、别名地址、邮件组。</para>
/// </summary>
public class EmailInfo : EmailBaseInfo
{

    /// <summary>
    /// <para>邮箱名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：Mike</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}