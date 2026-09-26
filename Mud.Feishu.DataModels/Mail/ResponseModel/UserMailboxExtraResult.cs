// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// 查询用户邮箱地址响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox/profile"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class UserMailboxProfileResult
{
    /// <summary>
    /// <para>用户主邮箱地址。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("primary_email_address")]
    public string? PrimaryEmailAddress { get; set; }

    /// <summary>
    /// <para>邮箱地址不存在时的未命中原因；当 primary_email_address 为空时可用于判断未命中。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("not_found_reason")]
    public string? NotFoundReason { get; set; }
}

/// <summary>
/// 查询邮件发送状态响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-message/send_status"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MessageSendStatusResult
{
    /// <summary>
    /// <para>邮件业务标识 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }

    /// <summary>
    /// <para>收件人投递状态列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("details")]
    public MessageSendStatusDetail[]? Details { get; set; }
}

/// <summary>
/// 收件人投递状态
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MessageSendStatusDetail
{
    /// <summary>
    /// <para>收件人信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recipient")]
    public MailAddressInfo? Recipient { get; set; }

    /// <summary>
    /// <para>投递状态。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>最后更新时间（Unix 时间戳，秒）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("last_updated_time")]
    public long? LastUpdatedTime { get; set; }
}

/// <summary>
/// 邮件地址信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MailAddressInfo
{
    /// <summary>
    /// <para>邮箱地址。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }

    /// <summary>
    /// <para>显示名称。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// 获取邮件附件下载链接响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-message-attachment/download_url"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class AttachmentDownloadUrlResult
{
    /// <summary>
    /// <para>下载链接列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("download_urls")]
    public AttachmentDownloadUrlItem[]? DownloadUrls { get; set; }

    /// <summary>
    /// <para>获取失败的附件 ID 列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("failed_ids")]
    public string[]? FailedIds { get; set; }
}

/// <summary>
/// 撤回已发送邮件响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-sent_message/recall"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class RecallMessageResult
{
    /// <summary>
    /// <para>撤回任务状态。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recall_status")]
    public string? RecallStatus { get; set; }

    /// <summary>
    /// <para>不允许撤回的原因。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recall_restriction_reason")]
    public string? RecallRestrictionReason { get; set; }
}

/// <summary>
/// 查询邮件撤回详情响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-sent_message/get_recall_detail"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class RecallMessageDetailResult
{
    /// <summary>
    /// <para>撤回任务状态。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recall_status")]
    public string? RecallStatus { get; set; }

    /// <summary>
    /// <para>撤回结果。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recall_result")]
    public string? RecallResult { get; set; }

    /// <summary>
    /// <para>撤回成功的收件人数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("success_count")]
    public int? SuccessCount { get; set; }

    /// <summary>
    /// <para>撤回失败的收件人数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("failure_count")]
    public int? FailureCount { get; set; }

    /// <summary>
    /// <para>处理中的收件人数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("processing_count")]
    public int? ProcessingCount { get; set; }

    /// <summary>
    /// <para>每个收件人的撤回详情列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("items")]
    public RecallDetailItem[]? Items { get; set; }
}

/// <summary>
/// 单个收件人的撤回详情
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class RecallDetailItem
{
    /// <summary>
    /// <para>收件人邮箱地址。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recipient_address")]
    public string? RecipientAddress { get; set; }

    /// <summary>
    /// <para>收件人显示名称。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recipient_name")]
    public string? RecipientName { get; set; }

    /// <summary>
    /// <para>该收件人的撤回状态。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>撤回失败原因，仅 status 为 fail 时有值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("fail_reason")]
    public string? FailReason { get; set; }

    /// <summary>
    /// <para>是否为邮件组地址。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_mailing_list")]
    public bool? IsMailingList { get; set; }

    /// <summary>
    /// <para>邮件组内成功撤回人数，仅 is_mailing_list 为 true 时有值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mailing_list_success_count")]
    public int? MailingListSuccessCount { get; set; }

    /// <summary>
    /// <para>邮件组内撤回失败人数，仅 is_mailing_list 为 true 时有值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mailing_list_failure_count")]
    public int? MailingListFailureCount { get; set; }
}

/// <summary>
/// 查询用户邮箱签名响应体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/mail-v1/user_mailbox-setting/get_signatures"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MailboxSignaturesResult
{
    /// <summary>
    /// <para>用户邮箱签名列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("signatures")]
    public MailboxSignature[]? Signatures { get; set; }

    /// <summary>
    /// <para>用户邮箱签名使用情况列表。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("usages")]
    public MailboxSignatureUsage[]? Usages { get; set; }
}

/// <summary>
/// 用户邮箱签名
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MailboxSignature
{
    /// <summary>
    /// <para>签名 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>签名名称。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>签名内容（HTML 格式）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// <para>签名类型，可选值：USER（用户签名）、TENANT（租户签名）。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("signature_type")]
    public string? SignatureType { get; set; }

    /// <summary>
    /// <para>签名适用设备类型，可选值：PC、MOBILE。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("signature_device")]
    public string? SignatureDevice { get; set; }
}

/// <summary>
/// 用户邮箱签名使用情况
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class MailboxSignatureUsage
{
    /// <summary>
    /// <para>邮箱地址。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }

    /// <summary>
    /// <para>发送邮件时使用的签名 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("send_mail_signature_id")]
    public string? SendMailSignatureId { get; set; }

    /// <summary>
    /// <para>回复邮件时使用的签名 ID。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reply_signature_id")]
    public string? ReplySignatureId { get; set; }
}
