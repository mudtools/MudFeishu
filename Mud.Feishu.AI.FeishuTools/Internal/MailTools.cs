// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Mud.Feishu.AI.FeishuTools.Tools;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// 邮件工具执行器（<c>mail.list_messages</c> / <c>mail.get_message</c> / <c>mail.send_message</c>，WP5/R5）。
/// </summary>
/// <remarks>
/// <para>
/// 读侧（list/get）走 tenant 令牌（<c>IFeishuTenantV1MailMessage</c>），发信（send）走 user 令牌
/// （<c>IFeishuUserV1MailDraft</c>）——邮件域是首个触发 T5-0 身份放行的域。
/// </para>
/// <para>
/// <c>mail.send_message</c> 内部为两步操作（创建草稿 → 发送草稿），模型不感知中间态：
/// 草稿创建失败 → 返回结构化错误；草稿创建成功但发送失败 → 返回结构化错误（含 draft_id 供排查）。
/// </para>
/// </remarks>
internal sealed class MailTools(
    Mud.Feishu.IFeishuTenantV1MailMessage? mailMessageClient,
    Mud.Feishu.IFeishuUserV1MailDraft? mailDraftUserClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1MailMessage? _mailMessageClient = mailMessageClient;
    private readonly Mud.Feishu.IFeishuUserV1MailDraft? _mailDraftUserClient = mailDraftUserClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>mail.list_messages：列出用户邮箱中的邮件（分页，白名单 message_id）。</summary>
    [FeishuToolHandler(typeof(IFeishuMailListMessagesTool))]
    public Task<FeishuToolResult> ListMessagesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailListMessages, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            if (_mailMessageClient is null)
            {
                throw new ArgumentException(
                    "mail.list_messages 需要 IFeishuTenantV1MailMessage——宿主须启用 AddMailApi 的消息侧客户端");
            }

            var args = MailListMessagesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _mailMessageClient
                .GetUserMailboxMessagePageListAsync(
                    args.UserMailboxId,
                    folder_id: args.FolderId,
                    only_unread: args.OnlyUnread,
                    page_size: PageSizes.MailMessages,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            // GetUserMailboxMessagePageListAsync 返回 FeishuApiPageListResult<string>（message_id 列表）
            // FeishuApiResultReader.Read 解包后 Data 为 ApiPageListResult<string>
            return executor.FromApi(outcome, data =>
            {
                var envelope = new JsonObject
                {
                    ["items"] = new JsonArray(),
                    ["has_more"] = data.HasMore,
                };
                if (!string.IsNullOrEmpty(data.PageToken))
                {
                    envelope["page_token"] = data.PageToken;
                }

                foreach (var messageId in data.Items ?? [])
                {
                    envelope["items"]!.AsArray().AddNode(new JsonObject
                    {
                        ["message_id"] = messageId,
                    });
                }

                return envelope;
            });
        });
    }

    /// <summary>mail.get_message：获取邮件详情（白名单 subject/from/to/cc/body_preview/message_id）。</summary>
    [FeishuToolHandler(typeof(IFeishuMailGetMessageTool))]
    public Task<FeishuToolResult> GetMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailGetMessage, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            if (_mailMessageClient is null)
            {
                throw new ArgumentException(
                    "mail.get_message 需要 IFeishuTenantV1MailMessage——宿主须启用 AddMailApi 的消息侧客户端");
            }

            var args = MailGetMessageArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _mailMessageClient
                .GetUserMailboxMessageAsync(
                    args.UserMailboxId,
                    args.MessageId,
                    format: args.Format,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => ProjectMessage(data.Message));
        });
    }

    /// <summary>mail.send_message：以用户身份发送邮件（两步操作：创建草稿 → 发送草稿）。</summary>
    /// <remarks>
    /// 模型不感知两步细节——工具层把 subject/body/to/cc/bcc 组装成 RFC 5822 EML → base64url 编码 →
    /// 创建草稿 → 发送草稿。草稿创建失败或发送失败均返回结构化错误。
    /// </remarks>
    [FeishuToolHandler(typeof(IFeishuUserMailSendMessageTool))]
    public Task<FeishuToolResult> SendMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailSendMessage);
        return executor.RunAsync(async () =>
        {
            if (_mailDraftUserClient is null)
            {
                throw new ArgumentException(
                    "mail.send_message 需要 IFeishuUserV1MailDraft（用户令牌）——宿主须启用 AddMailApi 的草稿侧客户端并在 AllowedIdentities 放行 user");
            }

            var args = MailSendMessageArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts",
                    ToolDryRun.IdempotencyNote(null),
                    ("user_mailbox_id", args.UserMailboxId.Length),
                    ("to", args.To.Length),
                    ("subject", args.Subject.Length),
                    ("body", args.Body.Length),
                    ("cc", args.Cc?.Length ?? 0),
                    ("bcc", args.Bcc?.Length ?? 0)));
            }

            // 构造 RFC 5822 EML 邮件
            var eml = BuildEml(args);
            var rawBase64 = Base64UrlEncode(eml);

            // 步骤 1：创建草稿
            var draftOutcome = FeishuApiResultReader.Read(await _mailDraftUserClient
                .CreateUserMailboxDraftAsync(
                    args.UserMailboxId,
                    new UserMailboxDraftRequest { Raw = rawBase64 },
                    cancellationToken)
                .ConfigureAwait(false));

            var draftId = draftOutcome.Data?.Draft?.Id;
            if (string.IsNullOrEmpty(draftId))
            {
                throw new ArgumentException("创建邮件草稿失败：飞书未返回 draft_id");
            }

            // draftId 非空保证（消除 CS8604：SendUserMailboxDraftAsync 的 draft_id 参数不接受 null）
            var nonNullDraftId = draftId!;

            // 步骤 2：发送草稿
            var sendOutcome = FeishuApiResultReader.Read(await _mailDraftUserClient
                .SendUserMailboxDraftAsync(
                    args.UserMailboxId,
                    nonNullDraftId,
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApiUntruncated(sendOutcome, data => new JsonObject
            {
                ["message_id"] = data.MessageId,
                ["thread_id"] = data.ThreadId,
            });
        });
    }

    /// <summary>get_message 投影：subject/from/to/cc/body_preview/message_id。</summary>
    private static JsonObject ProjectMessage(MailMessage? message)
    {
        if (message is null)
        {
            return new JsonObject { ["message"] = null };
        }

        return new JsonObject
        {
            ["message_id"] = message.MessageId,
            ["subject"] = message.Subject,
            ["from"] = message.HeadFrom?.MailAddressSuffix,
            ["to"] = message.Tos is { Length: > 0 }
                ? new JsonArray([.. message.Tos.Select(a => (JsonNode?)new JsonObject { ["address"] = a.MailAddressSuffix })])
                : null,
            ["cc"] = message.Ccs is { Length: > 0 }
                ? new JsonArray([.. message.Ccs.Select(a => (JsonNode?)new JsonObject { ["address"] = a.MailAddressSuffix })])
                : null,
            ["body_preview"] = message.BodyPreview,
            ["internal_date"] = message.InternalDate,
            ["folder_id"] = message.FolderId,
        };
    }

    /// <summary>构造 RFC 5822 EML 文本（纯文本邮件）。</summary>
    private static string BuildEml(MailSendMessageArgs args)
    {
        var sb = new StringBuilder();
        sb.Append("Content-Type: text/plain; charset=\"utf-8\"").Append("\r\n");
        sb.Append("Content-Transfer-Encoding: 8bit").Append("\r\n");
        sb.Append("MIME-Version: 1.0").Append("\r\n");
        sb.Append("To: ").Append(string.Join(", ", args.To.Select(a => $"<{a}>"))).Append("\r\n");
        if (args.Cc is { Length: > 0 })
        {
            sb.Append("Cc: ").Append(string.Join(", ", args.Cc.Select(a => $"<{a}>"))).Append("\r\n");
        }
        if (args.Bcc is { Length: > 0 })
        {
            sb.Append("Bcc: ").Append(string.Join(", ", args.Bcc.Select(a => $"<{a}>"))).Append("\r\n");
        }
        sb.Append("Subject: ").Append(args.Subject).Append("\r\n");
        sb.Append("\r\n");
        sb.Append(args.Body);
        return sb.ToString();
    }

    /// <summary>Base64Url 编码（飞书邮件 API 要求 base64url 编码的 EML 内容）。</summary>
    private static string Base64UrlEncode(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
