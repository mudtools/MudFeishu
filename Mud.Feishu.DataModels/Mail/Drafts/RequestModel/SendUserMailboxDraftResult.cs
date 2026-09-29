// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// 发送草稿响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/send"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class SendUserMailboxDraftResult
{
    /// <summary>
    /// <para>发送后生成的已发送邮件ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：197c5d72e22e1d79</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>邮件所属会话ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：197c5d72e22e1d78</para>
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string? ThreadId { get; set; }

    /// <summary>
    /// <para>撤回状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：unavailable</para>
    /// <para>可选值：<list type="bullet">
    /// <item>unavailable：不可撤回</item>
    /// <item>available：可撤回</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("recall_status")]
    public string? RecallStatus { get; set; }

    /// <summary>
    /// <para>自动化发信被禁用时返回的说明信息；为空表示发信成功</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("automation_send_disable")]
    public AutomationSendDisable? AutomationSendDisable { get; set; }
}

/// <summary>
/// 自动化发信禁用说明
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-draft/send"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class AutomationSendDisable
{
    /// <summary>
    /// <para>自动化发信被禁用的原因</para>
    /// <para>必填：否</para>
    /// <para>示例值：Automation send is disabled by your mailbox setting</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// <para>参考链接</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://open.larksuite.com/mail/settings/automation</para>
    /// </summary>
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }
}
