// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.Mail;
using System.Text;

namespace Mud.Feishu.AI.Tools.Internal;

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
    IOptions<FeishuAgentOptions> options,

    // R5 / F-11：以下三个客户端一律**软依赖**（可空）。理由与 BitableViewTools 相同——
    // 「客户端缺席 → 执行器缺席 → 注册器不注册」，写成硬依赖会把宿主未启用的那一侧
    // 变成整域缺席，连带拖垮 mail 已有的 3 个工具。
    Mud.Feishu.IFeishuUserV1MailMessage? mailUserMessageClient = null,
    Mud.Feishu.IFeishuTenantV1MailLabel? mailLabelClient = null,
    Mud.Feishu.IFeishuTenantV1MailThread? mailThreadClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV1MailMessage? _mailMessageClient = mailMessageClient;
    private readonly Mud.Feishu.IFeishuUserV1MailDraft? _mailDraftUserClient = mailDraftUserClient;
    private readonly Mud.Feishu.IFeishuUserV1MailMessage? _mailUserMessageClient = mailUserMessageClient;
    private readonly Mud.Feishu.IFeishuTenantV1MailLabel? _mailLabelClient = mailLabelClient;
    private readonly Mud.Feishu.IFeishuTenantV1MailThread? _mailThreadClient = mailThreadClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;
    private readonly int _maxAutoFetchItems = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchPages;

    /// <summary>mail.list_messages：列出用户邮箱中的邮件（分页，白名单 message_id）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantMailListMessagesTool))]
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

            // B1：fetch_all=true 时循环翻页（唯一翻页实现 ToolPagination）。
            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<ApiPageListResult<string>>(
                    async (token, ct) => FeishuApiResultReader.Read(await _mailMessageClient
                        .GetUserMailboxMessagePageListAsync(
                            args.UserMailboxId,
                            folder_id: args.FolderId,
                            only_unread: args.OnlyUnread,
                            page_size: PageSizes.MailMessages,
                            page_token: token,
                            cancellationToken: ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ProjectMessages(page)["items"]!.AsArray(),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

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
            return executor.FromApi(outcome, ProjectMessages);
        });
    }

    /// <summary>list_messages 投影：items（message_id）+ 翻页契约。</summary>
    private static JsonObject ProjectMessages(ApiPageListResult<string> data)
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
    }

    /// <summary>mail.get_message：获取邮件详情（白名单 subject/from/to/cc/body_preview/message_id）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantMailGetMessageTool))]
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
                    new SendUserMailboxDraftRequest(),
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
    /// <remarks>
    /// R3-12：头字段（To/Cc/Bcc/Subject）逐个拒绝任何 <c>CR</c>/<c>LF</c>。全局入站净化
    /// （<see cref="ToolArgumentSanitizer.ValidateText"/>）<b>必须</b>放行换行（正文/文档/消息都靠换行表达结构），
    /// 故头注入面只能在此收口——否则 <c>Subject: "x\r\nBcc: attacker@evil"</c> 会凭空插入收件人。
    /// 正文（<paramref name="args"/>.Body）仍允许 LF。
    /// </remarks>
    /// <exception cref="ArgumentException">头字段含 CR/LF（执行链转结构化错误回填模型，不落到下游）。</exception>
    internal static string BuildEml(MailSendMessageArgs args)
    {
        var sb = new StringBuilder();
        sb.Append("Content-Type: text/plain; charset=\"utf-8\"").Append("\r\n");
        sb.Append("Content-Transfer-Encoding: 8bit").Append("\r\n");
        sb.Append("MIME-Version: 1.0").Append("\r\n");
        sb.Append("To: ").Append(string.Join(", ", args.To.Select(a => $"<{RequireHeaderField("to", a)}>"))).Append("\r\n");
        if (args.Cc is { Length: > 0 })
        {
            sb.Append("Cc: ").Append(string.Join(", ", args.Cc.Select(a => $"<{RequireHeaderField("cc", a)}>"))).Append("\r\n");
        }
        if (args.Bcc is { Length: > 0 })
        {
            sb.Append("Bcc: ").Append(string.Join(", ", args.Bcc.Select(a => $"<{RequireHeaderField("bcc", a)}>"))).Append("\r\n");
        }
        sb.Append("Subject: ").Append(RequireHeaderField("subject", args.Subject)).Append("\r\n");
        sb.Append("\r\n");
        sb.Append(args.Body);
        return sb.ToString();
    }

    /// <summary>头字段守卫（R3-12）：含 CR/LF 即抛（SMTP 头注入防护）。</summary>
    private static string RequireHeaderField(string name, string value)
        => ToolArgumentSanitizer.ValidateHeaderValue(name, value) is { } violation
            ? throw new ArgumentException(violation, name)
            : value;

    /// <summary>Base64Url 编码（飞书邮件 API 要求 base64url 编码的 EML 内容）。</summary>
    private static string Base64UrlEncode(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    // ────────── R5 / F-11：Mail 增强（搜索 / 标签 / 会话线程 / 已读回写） ──────────

    /// <summary>取软依赖客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private static T Require<T>(T? client, string toolName, string clientName, string api)
        where T : class
        => client ?? throw new ArgumentException(
            $"{toolName} 需要 {clientName}——宿主须启用 {api}；未启用时本工具不在工具列表中"
            + "（软缺席，不影响 mail 其它工具）");

    /// <summary>mail.search：按关键字搜索邮件（<b>仅 user 身份接口</b>提供该能力）。</summary>
    [FeishuToolHandler(typeof(IFeishuUserMailSearchTool))]
    public Task<FeishuToolResult> SearchMailAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailSearch, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(
                _mailUserMessageClient, executor.ToolName, "IFeishuUserV1MailMessage", "AddMailApi 的 user 身份侧");
            var args = MailSearchArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .SearchUserMailboxMessageAsync(
                    args.UserMailboxId,
                    new SearchUserMailboxMessageRequest { Query = args.Query },
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data =>
            {
                var items = new JsonArray();
                foreach (var hit in data.Items ?? [])
                {
                    // AddNode（而非 Add）：见 ToolResultText.AddNode——泛型 Add<T>(T) 的裁剪/AOT 注解会红。
                    items.AddNode(new JsonObject
                    {
                        // MailSearchItem 只有 Id / DisplayInfo / MetaData 三个字段
                        // （标题、线程、时间都在 MetaData 里），故按实际可得字段投影。
                        ["message_id"] = hit.Id,
                        ["preview"] = ToolResultText.Truncate(hit.DisplayInfo, PageSizes.MessagePreviewLength),
                    });
                }

                return new JsonObject
                {
                    ["items"] = items,
                    ["total"] = data.Total,
                    ["has_more"] = data.HasMore,
                    ["page_token"] = data.PageToken,
                };
            });
        });
    }

    /// <summary>mail.list_labels：列出邮箱标签（供 mail.list_messages 的 label 过滤取 id）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantMailListLabelsTool))]
    public Task<FeishuToolResult> ListLabelsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailListLabels, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailLabelClient, executor.ToolName, "IFeishuTenantV1MailLabel", "AddMailApi 的标签侧");
            var args = MailListLabelsArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetUserMailboxLabelListAsync(args.UserMailboxId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data =>
            {
                var labels = new JsonArray();
                foreach (var label in data.Items ?? [])
                {
                    labels.AddNode(new JsonObject
                    {
                        ["label_id"] = label.Id,
                        ["name"] = label.Name,
                    });
                }

                return new JsonObject { ["labels"] = labels, ["total"] = labels.Count };
            });
        });
    }

    /// <summary>mail.get_thread：取会话线程（同一主题的往来邮件）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantMailGetThreadTool))]
    public Task<FeishuToolResult> GetThreadAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailGetThread, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailThreadClient, executor.ToolName, "IFeishuTenantV1MailThread", "AddMailApi 的会话侧");
            var args = MailGetThreadArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetUserMailboxThreadAsync(args.UserMailboxId, args.ThreadId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data =>
            {
                var thread = data.Thread;
                if (thread is null)
                {
                    // 空结果哨兵（F-5）：显式"未找到"，而不是空对象让模型猜。
                    return new JsonObject
                    {
                        ["found"] = false,
                        ["thread_id"] = args.ThreadId,
                        ["message"] = "未找到该会话线程，请确认 thread_id 是否属于该邮箱。",
                    };
                }

                // ⚠️ 只回填线程摘要与**成员数量**，不逐封展开正文：
                // 线程内每封邮件正文都可能很长，逐封回填会挤爆上下文；
                // 需要具体某封时用 mail.get_message 按 message_id 取。
                return new JsonObject
                {
                    ["found"] = true,
                    ["thread_id"] = thread.Id,
                    ["messages_count"] = thread.Messages?.Length ?? 0,
                    ["hint"] = "如需某封邮件的正文，请用 mail.get_message 按 message_id 读取（避免一次性灌入整条会话）。",
                };
            });
        });
    }

    /// <summary>
    /// mail.mark_read：回写已读状态。
    /// </summary>
    /// <remarks>
    /// <b>为什么默认 true</b>：工具名即语义（mark_read）。若默认 false，模型忘记传参时
    /// 会把邮件标成<b>未读</b>——与调用方预期相反，属"静默反向"。
    /// </remarks>
    [FeishuToolHandler(typeof(IFeishuTenantMailMarkReadTool))]
    public Task<FeishuToolResult> MarkReadAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailMarkRead, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailMessageClient, executor.ToolName, "IFeishuTenantV1MailMessage", "AddMailApi 的邮件侧");
            var args = MailMarkReadArgs.Unpack(arguments);
            var read = args.Read ?? true;

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH",
                    $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/messages/{args.MessageId}",
                    ToolDryRun.IdempotencyNote(null),
                    ("user_mailbox_id", args.UserMailboxId.Length),
                    ("message_id", args.MessageId.Length),
                    ("read", read ? 1 : 0)));
            }

            var nullDataResult = await client
                .ModifyUserMailboxMessageAsync(
                    args.UserMailboxId,
                    args.MessageId,
                    // 已读状态在飞书邮箱里就是**UNREAD 标签**：移除 = 已读，加上 = 未读。
                    // （核实：ModifyUserMailboxMessageRequest 只有 add_label_ids /
                    //  remove_label_ids / add_folder 三个字段，官方文档亦把 UNREAD 列为
                    //  可增删的标签之一 ⇒ 无需给 SDK 补 read 字段，保持零 SDK 改动。）
                    read
                        ? new ModifyUserMailboxMessageRequest { RemoveLabelIds = NewUnreadLabel() }
                        : new ModifyUserMailboxMessageRequest { AddLabelIds = NewUnreadLabel() },
                    cancellationToken)
                .ConfigureAwait(false);

            if (nullDataResult is null)
            {
                throw new ArgumentException("飞书接口无响应（result 为空）");
            }

            if (nullDataResult.Code != 0)
            {
                throw new ArgumentException(
                    $"飞书接口返回错误 code={nullDataResult.Code.ToString(CultureInfo.InvariantCulture)}, "
                    + $"msg={nullDataResult.Msg ?? "(无错误信息)"}");
            }

            return FeishuToolResult.FromText(ToolResultJson.ToText(new JsonObject
            {
                ["message_id"] = args.MessageId,
                ["read"] = read,
            }));
        });
    }

    /// <summary>飞书邮箱的"未读"标签名（已读 = 移除该标签，未读 = 添加该标签）。</summary>
    private static string[] NewUnreadLabel() => ["UNREAD"];
}