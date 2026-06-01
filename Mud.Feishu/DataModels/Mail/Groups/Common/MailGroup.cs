// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// 邮件组
/// </summary>
public class MailGroup
{
    /// <summary>
    /// <para>邮件组地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：test_mail_group@xxx.xx</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>邮件组名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：testmailgroup</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>邮件组描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：mailgroupfortesting</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>谁可发送邮件到此邮件组</para>
    /// <para>必填：否</para>
    /// <para>示例值：ALL_INTERNAL_USERS</para>
    /// <para>可选值：<list type="bullet">
    /// <item>ANYONE：任何人</item>
    /// <item>ALL_INTERNAL_USERS：仅组织内部成员</item>
    /// <item>ALL_GROUP_MEMBERS：仅邮件组成员</item>
    /// <item>CUSTOM_MEMBERS：自定义成员</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("who_can_send_mail")]
    public string? WhoCanSendMail { get; set; }
}
