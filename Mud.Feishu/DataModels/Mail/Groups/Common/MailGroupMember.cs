// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// 邮件组成员
/// </summary>
public class MailGroupMember
{
    /// <summary>
    /// <para>成员邮箱地址（当成员类型是EXTERNAL_USER/MAIL_GROUP/OTHER_MEMBER时有值）</para>
    /// <para>必填：否</para>
    /// <para>示例值：test_member@xxx.xx</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>租户内用户的唯一标识（当成员类型是USER时有值）</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>租户内部门的唯一标识（当成员类型是DEPARTMENT时有值）</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxxxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>成员类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：USER</para>
    /// <para>可选值：<list type="bullet">
    /// <item>USER：内部用户</item>
    /// <item>DEPARTMENT：部门</item>
    /// <item>COMPANY：全员</item>
    /// <item>EXTERNAL_USER：外部用户</item>
    /// <item>MAIL_GROUP：邮件组</item>
    /// <item>PUBLIC_MAILBOX：member is a public mailbox</item>
    /// <item>OTHER_MEMBER：内部成员</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
