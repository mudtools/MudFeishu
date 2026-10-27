// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 邮件深化工具执行器（T-3 成环：草稿 4 / 处置与附件 2 / 收件规则 4 / 模板与签名 5，共 15 个）。
/// </summary>
/// <remarks>
/// <para>
/// 客户端全部为<b>软依赖</b>（可空）：宿主启用 AddMailApi 的哪一侧，就多出对应工具；
/// 缺席侧的工具在执行期给出"宿主须启用什么"的结构化错误（与 <see cref="MailTools"/> 同款语义，
/// 不影响 mail 其它工具）。
/// </para>
/// <para>
/// 草稿四件套（create/update/send/delete）与 <c>mail.send</c> 同源：
/// 模型给扁平字段，工具层组装 RFC 5822 EML → base64url（<see cref="MailTools.BuildEml"/>）。
/// 收件规则的条件/动作用 <see cref="MailRuleDsl"/> 管道分隔简化式（模型零 JSON、零平台代码）——
/// rule_save 编译式先于 dry_run 执行（非法组合在此报错并附合法写法），rule_list 反向渲染。
/// </para>
/// </remarks>
internal sealed class MailDeepTools(
    Mud.Feishu.IFeishuUserV1MailDraft? mailDraftUserClient,
    Mud.Feishu.IFeishuTenantV1MailMessage? mailMessageClient,
    Mud.Feishu.IFeishuTenantV1MailRule? mailRuleClient,
    Mud.Feishu.IFeishuTenantV1MailTemplate? mailTemplateClient,
    Mud.Feishu.IFeishuUserV1MailTemplate? mailTemplateUserClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuUserV1MailDraft? _mailDraftUserClient = mailDraftUserClient;
    private readonly Mud.Feishu.IFeishuTenantV1MailMessage? _mailMessageClient = mailMessageClient;
    private readonly Mud.Feishu.IFeishuTenantV1MailRule? _mailRuleClient = mailRuleClient;
    private readonly Mud.Feishu.IFeishuTenantV1MailTemplate? _mailTemplateClient = mailTemplateClient;
    private readonly Mud.Feishu.IFeishuUserV1MailTemplate? _mailTemplateUserClient = mailTemplateUserClient;
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    // ────────── 草稿（create / update / send / delete） ──────────

    /// <summary>mail.create_draft：创建草稿（EML 组装 → base64url，模型不感知；<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuUserMailCreateDraftTool))]
    public Task<ToolResult> CreateDraftAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailCreateDraft);
        return executor.RunAsync(async () =>
        {
            var client = Require(
                _mailDraftUserClient, executor.ToolName, "IFeishuUserV1MailDraft",
                "AddMailApi 的草稿侧客户端（用户令牌——须在 AllowedIdentities 放行 user）");
            var args = MailCreateDraftArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts",
                    ToolDryRun.IdempotencyNote(null),
                    ("user_mailbox_id", args.UserMailboxId.Length),
                    ("to", args.To.Length),
                    ("subject", args.Subject.Length),
                    ("body", args.Body.Length),
                    ("cc", args.Cc?.Length ?? 0),
                    ("bcc", args.Bcc?.Length ?? 0)));
            }

            var raw = MailTools.Base64UrlEncode(
                MailTools.BuildEml(args.To, args.Cc, args.Bcc, args.Subject, args.Body));

            var outcome = FeishuApiResultReader.Read(await client
                .CreateUserMailboxDraftAsync(
                    args.UserMailboxId,
                    new UserMailboxDraftRequest { Raw = raw },
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, static data => new JsonObject
            {
                ["draft_id"] = data.Draft?.Id,
            });
        });
    }

    /// <summary>mail.update_draft：更新草稿内容（**整段替换**；PUT 天然幂等；<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuUserMailUpdateDraftTool))]
    public Task<ToolResult> UpdateDraftAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailUpdateDraft);
        return executor.RunAsync(async () =>
        {
            var client = Require(
                _mailDraftUserClient, executor.ToolName, "IFeishuUserV1MailDraft",
                "AddMailApi 的草稿侧客户端（用户令牌——须在 AllowedIdentities 放行 user）");
            var args = MailUpdateDraftArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PUT",
                    $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/drafts/{args.DraftId}",
                    ToolDryRun.IdempotencyNote("PUT 整段替换天然幂等——重复提交相同内容结果一致"),
                    ("user_mailbox_id", args.UserMailboxId.Length),
                    ("draft_id", args.DraftId.Length),
                    ("to", args.To.Length),
                    ("subject", args.Subject.Length),
                    ("body", args.Body.Length),
                    ("cc", args.Cc?.Length ?? 0),
                    ("bcc", args.Bcc?.Length ?? 0)));
            }

            var raw = MailTools.Base64UrlEncode(
                MailTools.BuildEml(args.To, args.Cc, args.Bcc, args.Subject, args.Body));

            var outcome = FeishuApiResultReader.Read(await client
                .UpdateUserMailboxDraftAsync(
                    args.UserMailboxId,
                    args.DraftId,
                    new UserMailboxDraftRequest { Raw = raw },
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["draft_id"] = data.Draft?.Id ?? args.DraftId,
            });
        });
    }

    /// <summary>
    /// mail.send：发送邮件。传 <c>draft_id</c> 发已存草稿；不传则现写现发（内部建草稿 → 发送）。
    /// </summary>
    /// <remarks>
    /// 平台两类"看似成功实则未发出"的形态都必须转结构化成败：① <c>automation_send_disable</c>
    /// （自动发送被策略拦截，code 仍为 0）；② send_time 早于当前时间 5 分钟（本工具在调用前拦截）。
    /// 两种用法互斥：有 draft_id 时出现正文/收件人即拒绝——否则"发出的是哪份内容"不可判定。
    /// </remarks>
    [ToolHandler(typeof(IFeishuUserMailSendTool))]
    public Task<ToolResult> SendAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailSend);
        return executor.RunAsync(async () =>
        {
            var client = Require(
                _mailDraftUserClient, executor.ToolName, "IFeishuUserV1MailDraft",
                "AddMailApi 的草稿侧客户端（用户令牌——须在 AllowedIdentities 放行 user）");
            var args = MailSendArgs.Unpack(arguments);

            var hasDraft = !string.IsNullOrWhiteSpace(args.DraftId);
            var hasCompose = args.To is { Length: > 0 };

            // 结构校验先于 dry_run：互斥/缺失在预演期同样报错（与规则 DSL 编译同纪律）。
            if (hasDraft && (hasCompose || args.Subject is not null || args.Body is not null
                || args.Cc is { Length: > 0 } || args.Bcc is { Length: > 0 }))
            {
                throw new ArgumentException(
                    "mail.send 的 draft_id 与 to/subject/body/cc/bcc 互斥：给 draft_id 即发送该草稿（内容已定），"
                    + "要改内容请先用 mail.update_draft");
            }

            if (!hasDraft && (!hasCompose || string.IsNullOrEmpty(args.Subject) || string.IsNullOrEmpty(args.Body)))
            {
                throw new ArgumentException(
                    "mail.send 不传 draft_id 时须给全 to/subject/body（现写现发）；只想存草稿不发出请用 mail.create_draft");
            }

            var sendTimeSeconds = args.SendTime is null ? null : ToUnixSeconds(args.SendTime);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(hasDraft
                    ? ToolDryRun.Describe(
                        executor.ToolName, "POST",
                        $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/drafts/{args.DraftId}/send",
                        ToolDryRun.IdempotencyNote(null),
                        ("user_mailbox_id", args.UserMailboxId.Length),
                        ("draft_id", args.DraftId!.Length),
                        ("send_time", args.SendTime?.Length ?? 0))
                    : ToolDryRun.Describe(
                        executor.ToolName, "POST", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts",
                        ToolDryRun.IdempotencyNote(null),
                        ("user_mailbox_id", args.UserMailboxId.Length),
                        ("to", args.To!.Length),
                        ("subject", args.Subject!.Length),
                        ("body", args.Body!.Length),
                        ("cc", args.Cc?.Length ?? 0),
                        ("bcc", args.Bcc?.Length ?? 0),
                        ("send_time", args.SendTime?.Length ?? 0)));
            }

            string draftId;
            if (hasDraft)
            {
                draftId = args.DraftId!;
            }
            else
            {
                var eml = MailTools.BuildEml(args.To!, args.Cc, args.Bcc, args.Subject!, args.Body!);
                var createOutcome = FeishuApiResultReader.Read(await client
                    .CreateUserMailboxDraftAsync(
                        args.UserMailboxId,
                        new UserMailboxDraftRequest { Raw = MailTools.Base64UrlEncode(eml) },
                        cancellationToken)
                    .ConfigureAwait(false));

                draftId = createOutcome.Data?.Draft?.Id ?? string.Empty;
                if (string.IsNullOrEmpty(draftId))
                {
                    throw new ArgumentException("创建邮件草稿失败：飞书未返回 draft_id");
                }
            }

            var sendOutcome = FeishuApiResultReader.Read(await client
                .SendUserMailboxDraftAsync(
                    args.UserMailboxId,
                    draftId,
                    new SendUserMailboxDraftRequest { SendTime = sendTimeSeconds },
                    cancellationToken)
                .ConfigureAwait(false));

            // 平台级"自动发送被禁止"：HTTP 与 code 均为成功，但邮件并未发出——
            // 不拦截的话模型会把它读成"发送成功"（静默反向失败）。
            if (sendOutcome.Data?.AutomationSendDisable is { } disabled)
            {
                throw new ArgumentException(
                    $"发送邮件被平台限制（automation_send_disable）：{disabled.Reason ?? "(无原因)"}"
                    + $"（reference: {disabled.Reference ?? "(无)"}）——请检查该邮箱的自动发送设置"
                    + (hasDraft ? string.Empty : $"；本次已建草稿 draft_id={draftId}"));
            }

            return executor.FromApiUntruncated(sendOutcome, data =>
            {
                var envelope = new JsonObject
                {
                    ["message_id"] = data.MessageId,
                    ["thread_id"] = data.ThreadId,
                };
                if (!hasDraft)
                {
                    envelope["draft_id"] = draftId;
                }

                return envelope;
            });
        });
    }

    /// <summary>mail.delete_draft：删除草稿（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuUserMailDeleteDraftTool))]
    public Task<ToolResult> DeleteDraftAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailDeleteDraft);
        return executor.RunAsync(async () =>
        {
            var client = Require(
                _mailDraftUserClient, executor.ToolName, "IFeishuUserV1MailDraft",
                "AddMailApi 的草稿侧客户端（用户令牌——须在 AllowedIdentities 放行 user）");
            var args = MailDeleteDraftArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE",
                    $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/drafts/{args.DraftId}"));
            }

            var result = await client
                .DeleteUserMailboxDraftAsync(args.UserMailboxId, args.DraftId, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["draft_id"] = args.DraftId,
                ["deleted"] = true,
            });
        });
    }

    // ────────── 处置与附件（trash_message / get_attachment_url） ──────────

    /// <summary>mail.trash_message：批量移入已删除文件夹（回收站语义；<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantMailTrashMessageTool))]
    public Task<ToolResult> TrashMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailTrashMessage);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailMessageClient, executor.ToolName, "IFeishuTenantV1MailMessage", "AddMailApi 的消息侧客户端");
            var args = MailTrashMessageArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/messages/batch_trash",
                    ToolDryRun.IdempotencyNote(null),
                    ("user_mailbox_id", args.UserMailboxId.Length),
                    ("message_ids", args.MessageIds.Length)));
            }

            var result = await client
                .BatchTrashUserMailboxMessageAsync(
                    args.UserMailboxId,
                    new BatchTrashUserMailboxMessageRequest { MessageIds = args.MessageIds },
                    cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["trashed"] = true,
                ["count"] = args.MessageIds.Length,
            });
        });
    }

    /// <summary>mail.get_attachment_url：获取邮件附件下载链接（链接有时效；只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantMailGetAttachmentUrlTool))]
    public Task<ToolResult> GetAttachmentUrlAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailGetAttachmentUrl, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailMessageClient, executor.ToolName, "IFeishuTenantV1MailMessage", "AddMailApi 的消息侧客户端");
            var args = MailGetAttachmentUrlArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetMessageAttachmentDownloadUrlAsync(
                    args.UserMailboxId,
                    args.MessageId,
                    attachment_ids: string.Join(",", args.AttachmentIds),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, static data =>
            {
                var downloads = new JsonArray();
                foreach (var item in data.DownloadUrls ?? [])
                {
                    downloads.AddNode(new JsonObject
                    {
                        ["attachment_id"] = item.AttachmentId,
                        ["download_url"] = item.DownloadUrl,
                    });
                }

                return new JsonObject
                {
                    ["downloads"] = downloads,
                    ["failed_ids"] = ToJsonArray(data.FailedIds ?? []),
                };
            });
        });
    }

    // ────────── 收件规则（list / create / update / delete / reorder） ──────────

    /// <summary>mail.rule_list：列出收件规则（条件/动作反渲染为简化式）。</summary>
    [ToolHandler(typeof(IFeishuTenantMailRuleListTool))]
    public Task<ToolResult> RuleListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailRuleList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailRuleClient, executor.ToolName, "IFeishuTenantV1MailRule", "AddMailApi 的规则侧客户端");
            var args = MailRuleListArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetMailboxRuleListAsync(args.UserMailboxId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, static data =>
            {
                var rules = new JsonArray();
                foreach (var rule in data.Items ?? [])
                {
                    rules.AddNode(new JsonObject
                    {
                        ["rule_id"] = rule.Id,
                        ["name"] = rule.Name,
                        ["is_enable"] = rule.IsEnable,
                        ["match_all"] = MailRuleDsl.IsMatchAll(rule.Condition?.MatchType ?? 1),
                        ["ignore_the_rest_of_rules"] = rule.IgnoreTheRestOfRules,
                        ["conditions"] = ToJsonArray(MailRuleDsl.DescribeConditions(rule.Condition)),
                        ["actions"] = ToJsonArray(MailRuleDsl.DescribeActions(rule.Action)),
                    });
                }

                return new JsonObject { ["rules"] = rules, ["total"] = rules.Count };
            });
        });
    }

    /// <summary>mail.rule_save：新建或整条替换收件规则（有 rule_id 走 PUT 覆盖，无则 POST 新建）。</summary>
    /// <remarks>简化式 → 平台代码的编译先于 <c>dry_run</c>：非法 field/operator/action 组合在此报错并附合法写法，不进入下游。</remarks>
    [ToolHandler(typeof(IFeishuTenantMailRuleSaveTool))]
    public Task<ToolResult> RuleSaveAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailRuleSave);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailRuleClient, executor.ToolName, "IFeishuTenantV1MailRule", "AddMailApi 的规则侧客户端");
            var args = MailRuleSaveArgs.Unpack(arguments);

            // 编译期：非法 field/operator/action 组合在此报错并附合法写法；不进入下游。
            var condition = MailRuleDsl.CompileConditions(args.Conditions, args.MatchAll);
            var action = MailRuleDsl.CompileActions(args.Actions);
            var hasRule = !string.IsNullOrWhiteSpace(args.RuleId);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(hasRule
                    ? ToolDryRun.Describe(
                        executor.ToolName, "PUT",
                        $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/rules/{args.RuleId}",
                        "规则结构（由 conditions/actions 编译，非模型手写）：" + DescribeCompiled(condition, action)
                        + ToolDryRun.IdempotencyNote("PUT 整条替换天然幂等——重复提交相同内容结果一致"),
                        ("user_mailbox_id", args.UserMailboxId.Length),
                        ("rule_id", args.RuleId!.Length),
                        ("name", args.Name.Length),
                        ("conditions", args.Conditions.Length),
                        ("actions", args.Actions.Length))
                    : ToolDryRun.Describe(
                        executor.ToolName, "POST",
                        $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/rules",
                        "规则结构（由 conditions/actions 编译，非模型手写）：" + DescribeCompiled(condition, action)
                        + ToolDryRun.IdempotencyNote(null),
                        ("user_mailbox_id", args.UserMailboxId.Length),
                        ("name", args.Name.Length),
                        ("conditions", args.Conditions.Length),
                        ("actions", args.Actions.Length)));
            }

            if (hasRule)
            {
                var updateRequest = new UpdateUserMailboxRuleRequest
                {
                    Condition = condition,
                    Action = action,
                    Name = args.Name,
                    IsEnable = args.IsEnable ?? true,
                    IgnoreTheRestOfRules = args.IgnoreTheRestOfRules ?? false,
                };

                var updateResult = await client
                    .UpdateUserMailboxRuleAsync(args.UserMailboxId, args.RuleId!, updateRequest, cancellationToken)
                    .ConfigureAwait(false);
                RequireNullDataSuccess(executor.ToolName, updateResult);

                return ToolResultPipeline.OkReceipt(new JsonObject
                {
                    ["rule_id"] = args.RuleId,
                    ["updated"] = true,
                });
            }

            var request = new CreateUserMailboxRuleRequest
            {
                Condition = condition,
                Action = action,
                Name = args.Name,
                IsEnable = args.IsEnable ?? true,
                IgnoreTheRestOfRules = args.IgnoreTheRestOfRules ?? false,
            };

            var outcome = FeishuApiResultReader.Read(await client
                .CreateUserMailboxRuleAsync(args.UserMailboxId, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, static data => new JsonObject
            {
                ["rule_id"] = data.Rule?.Id,
            });
        });
    }

    /// <summary>mail.rule_delete：删除收件规则（<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantMailRuleDeleteTool))]
    public Task<ToolResult> RuleDeleteAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailRuleDelete);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailRuleClient, executor.ToolName, "IFeishuTenantV1MailRule", "AddMailApi 的规则侧客户端");
            var args = MailRuleDeleteArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE",
                    $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/rules/{args.RuleId}"));
            }

            var result = await client
                .DeleteUserMailboxRuleAsync(args.UserMailboxId, args.RuleId, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["rule_id"] = args.RuleId,
                ["deleted"] = true,
            });
        });
    }

    /// <summary>mail.rule_reorder：重排收件规则顺序（顺序即优先级，须给全量 id；<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantMailRuleReorderTool))]
    public Task<ToolResult> RuleReorderAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailRuleReorder);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailRuleClient, executor.ToolName, "IFeishuTenantV1MailRule", "AddMailApi 的规则侧客户端");
            var args = MailRuleReorderArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/rules/reorder",
                    ToolDryRun.IdempotencyNote(null),
                    ("user_mailbox_id", args.UserMailboxId.Length),
                    ("rule_ids", args.RuleIds.Length)));
            }

            var result = await client
                .ReorderUserMailboxRuleAsync(
                    args.UserMailboxId,
                    new ReorderUserMailboxRuleRequest { RuleIds = args.RuleIds },
                    cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["reordered"] = true,
                ["count"] = args.RuleIds.Length,
            });
        });
    }

    // ────────── 模板与签名（template_list / get / create / update / delete + signature_list） ──────────

    /// <summary>mail.template_list：列出个人邮件模板（template_id/名称/创建时间；只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantMailTemplateListTool))]
    public Task<ToolResult> TemplateListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailTemplateList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailTemplateClient, executor.ToolName, "IFeishuTenantV1MailTemplate", "AddMailApi 的模板侧客户端");
            var args = MailTemplateListArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetMailTemplateListAsync(args.UserMailboxId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, static data =>
            {
                var items = new JsonArray();
                foreach (var template in data.Items ?? [])
                {
                    items.AddNode(new JsonObject
                    {
                        ["template_id"] = template.TemplateId,
                        ["name"] = template.Name,
                        ["create_time"] = template.CreateTime,
                    });
                }

                return new JsonObject { ["items"] = items, ["total"] = items.Count };
            });
        });
    }

    /// <summary>mail.template_get：获取模板完整内容（编辑模板前回填/按模板起草时读取；只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantMailTemplateGetTool))]
    public Task<ToolResult> TemplateGetAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailTemplateGet, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailTemplateClient, executor.ToolName, "IFeishuTenantV1MailTemplate", "AddMailApi 的模板侧客户端");
            var args = MailTemplateGetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetMailTemplateAsync(args.UserMailboxId, args.TemplateId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => ProjectTemplate(data.Template, args.TemplateId));
        });
    }

    /// <summary>mail.template_save：新建或整段替换个人邮件模板（有 template_id 走 PUT 覆盖，无则 POST 新建）。</summary>
    [ToolHandler(typeof(IFeishuTenantMailTemplateSaveTool))]
    public Task<ToolResult> TemplateSaveAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailTemplateSave);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailTemplateClient, executor.ToolName, "IFeishuTenantV1MailTemplate", "AddMailApi 的模板侧客户端");
            var args = MailTemplateSaveArgs.Unpack(arguments);
            var hasTemplate = !string.IsNullOrWhiteSpace(args.TemplateId);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(hasTemplate
                    ? ToolDryRun.Describe(
                        executor.ToolName, "PUT",
                        $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/templates/{args.TemplateId}",
                        ToolDryRun.IdempotencyNote("PUT 整段替换天然幂等——重复提交相同内容结果一致"),
                        ("user_mailbox_id", args.UserMailboxId.Length),
                        ("template_id", args.TemplateId!.Length),
                        ("name", args.Name.Length),
                        ("subject", args.Subject?.Length ?? 0),
                        ("template_content", args.TemplateContent?.Length ?? 0))
                    : ToolDryRun.Describe(
                        executor.ToolName, "POST",
                        $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/templates",
                        ToolDryRun.IdempotencyNote(null),
                        ("user_mailbox_id", args.UserMailboxId.Length),
                        ("name", args.Name.Length),
                        ("subject", args.Subject?.Length ?? 0),
                        ("template_content", args.TemplateContent?.Length ?? 0)));
            }

            if (hasTemplate)
            {
                var updateRequest = new UpdateMailTemplateRequest
                {
                    Template = BuildTemplate(
                        args.Name, args.Subject, args.TemplateContent, args.IsPlainTextMode,
                        args.Tos, args.Ccs, args.Bccs),
                };

                var updateResult = await client
                    .UpdateMailTemplateAsync(args.UserMailboxId, args.TemplateId!, updateRequest, cancellationToken)
                    .ConfigureAwait(false);
                var outcome = FeishuApiResultReader.Read(updateResult);

                // UpdateMailTemplateResult.Template 是 MailTemplate（无 id）——回填入参 template_id。
                return executor.FromApiUntruncated(outcome, _ => new JsonObject
                {
                    ["template_id"] = args.TemplateId,
                    ["updated"] = true,
                });
            }

            var request = new CreateMailTemplateRequest
            {
                Template = BuildTemplate(
                    args.Name, args.Subject, args.TemplateContent, args.IsPlainTextMode,
                    args.Tos, args.Ccs, args.Bccs),
            };

            var createOutcome = FeishuApiResultReader.Read(await client
                .CreateMailTemplateAsync(args.UserMailboxId, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(createOutcome, static data => new JsonObject
            {
                ["template_id"] = data.Template?.TemplateId,
            });
        });
    }

    /// <summary>mail.template_delete：删除模板（**不可恢复**；<c>dry_run=true</c> 时只预演）。</summary>
    [ToolHandler(typeof(IFeishuTenantMailTemplateDeleteTool))]
    public Task<ToolResult> TemplateDeleteAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailTemplateDelete);
        return executor.RunAsync(async () =>
        {
            var client = Require(_mailTemplateClient, executor.ToolName, "IFeishuTenantV1MailTemplate", "AddMailApi 的模板侧客户端");
            var args = MailTemplateDeleteArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE",
                    $"/open-apis/mail/v1/user_mailboxes/{args.UserMailboxId}/templates/{args.TemplateId}"));
            }

            var result = await client
                .DeleteMailTemplateAsync(args.UserMailboxId, args.TemplateId, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["template_id"] = args.TemplateId,
                ["deleted"] = true,
            });
        });
    }

    /// <summary>mail.signature_list：列出邮件签名与各地址签名配置（user 身份；签名正文只回填截断预览）。</summary>
    [ToolHandler(typeof(IFeishuUserMailSignatureListTool))]
    public Task<ToolResult> SignatureListAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.MailSignatureList, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var client = Require(
                _mailTemplateUserClient, executor.ToolName, "IFeishuUserV1MailTemplate",
                "AddMailApi 的模板 user 侧客户端（用户令牌——须在 AllowedIdentities 放行 user）");
            var args = MailSignatureListArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await client
                .GetUserMailboxSignaturesAsync(args.UserMailboxId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, static data =>
            {
                var signatures = new JsonArray();
                foreach (var signature in data.Signatures ?? [])
                {
                    signatures.AddNode(new JsonObject
                    {
                        ["signature_id"] = signature.Id,
                        ["name"] = signature.Name,
                        ["preview"] = ToolResultText.Truncate(signature.Content, PageSizes.MessagePreviewLength),
                        ["signature_type"] = signature.SignatureType,
                        ["signature_device"] = signature.SignatureDevice,
                    });
                }

                var usages = new JsonArray();
                foreach (var usage in data.Usages ?? [])
                {
                    usages.AddNode(new JsonObject
                    {
                        ["email_address"] = usage.EmailAddress,
                        ["send_mail_signature_id"] = usage.SendMailSignatureId,
                        ["reply_signature_id"] = usage.ReplySignatureId,
                    });
                }

                return new JsonObject { ["signatures"] = signatures, ["usages"] = usages };
            });
        });
    }

    // ────────── 私有辅助 ──────────

    /// <summary>取软依赖客户端；缺席时给出<b>可执行</b>提示（宿主该启用什么），而不是空引用。</summary>
    private static T Require<T>(T? client, string toolName, string clientName, string api)
        where T : class
        => client ?? throw new ArgumentException(
            $"{toolName} 需要 {clientName}——宿主须启用 {api}；未启用时本工具不在工具列表中"
            + "（软缺席，不影响 mail 其它工具）");

    /// <summary>解包 <c>FeishuNullDataApiResult</c>（不走 <c>FeishuApiResultReader.Read&lt;T&gt;</c>）。</summary>
    private static void RequireNullDataSuccess(string toolName, FeishuNullDataApiResult? result)
    {
        if (result is null)
        {
            throw new ArgumentException("飞书接口无响应（result 为空）");
        }

        if (result.Code != 0)
        {
            throw new ArgumentException(
                FeishuToolBinding.StructuredError(
                    toolName,
                    result.Code,
                    $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, "
                    + $"msg={result.Msg ?? "(无错误信息)"}"));
        }
    }

    /// <summary>RFC3339 → Unix 秒字符串（send_time 形态；须带时区且至少晚于当前时间 5 分钟）。</summary>
    private static string ToUnixSeconds(string rfc3339)
    {
        var trimmed = rfc3339.Trim();
        var hasOffset = trimmed.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
            || trimmed.IndexOf('+') > 0
            || trimmed.LastIndexOf('-') > 10;

        if (!hasOffset
            || !DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            throw new ArgumentException(
                $"send_time 需为带时区的 RFC3339 格式（如 2026-10-20T09:00:00+08:00 或 2026-10-20T01:00:00Z），实际: {trimmed}");
        }

        if (parsed < DateTimeOffset.UtcNow.AddMinutes(5))
        {
            throw new ArgumentException(
                $"send_time 需至少晚于当前时间 5 分钟（平台要求），实际: {trimmed}");
        }

        return parsed.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>把编译出的平台代码渲染为单行摘要（rule_create/rule_update 的 dry_run 用）。</summary>
    private static string DescribeCompiled(RuleCondition condition, RuleAction action)
    {
        var conditionParts = (condition.Items ?? [])
            .Select(item => $"type={item.Type.ToString(CultureInfo.InvariantCulture)}"
                + $",operator={item.Operator?.ToString(CultureInfo.InvariantCulture) ?? "-"}"
                + $",input={item.Input ?? "-"}");
        var actionParts = (action.Items ?? [])
            .Select(item => $"type={item.Type.ToString(CultureInfo.InvariantCulture)},input={item.Input ?? "-"}");

        return $"condition[match_type={condition.MatchType.ToString(CultureInfo.InvariantCulture)}] "
            + string.Join("; ", conditionParts)
            + " | action " + string.Join("; ", actionParts);
    }

    /// <summary>template_get 投影：found 哨兵 + 模板完整字段；附件逐条目白名单。</summary>
    private static JsonObject ProjectTemplate(MailTemplateInfo? template, string fallbackTemplateId)
    {
        if (template is null)
        {
            // 空结果哨兵（F-5）：显式"未找到"，而不是空对象让模型猜。
            return new JsonObject
            {
                ["found"] = false,
                ["template_id"] = fallbackTemplateId,
                ["message"] = "未找到该邮件模板，请确认 template_id 是否属于该邮箱。",
            };
        }

        return new JsonObject
        {
            ["found"] = true,
            ["template_id"] = template.TemplateId ?? fallbackTemplateId,
            ["name"] = template.Name,
            ["subject"] = template.Subject,
            ["template_content"] = template.TemplateContent,
            ["is_plain_text_mode"] = template.IsPlainTextMode,
            ["tos"] = ProjectAddresses(template.Tos),
            ["ccs"] = ProjectAddresses(template.Ccs),
            ["bccs"] = ProjectAddresses(template.Bccs),
            ["attachments"] = ProjectTemplateAttachments(template.Attachments),
        };
    }

    /// <summary>地址数组投影（address + name）；空数组归一为 null（不产出无信息量的空壳）。</summary>
    private static JsonArray? ProjectAddresses(MailAddress[]? addresses)
        => addresses is { Length: > 0 }
            ? new JsonArray([.. addresses.Select(a => (JsonNode?)new JsonObject
            {
                ["address"] = a.MailAddressSuffix,
                ["name"] = a.Name,
            })])
            : null;

    /// <summary>模板附件投影（filename/id/is_inline/cid）；空数组归一为 null。</summary>
    private static JsonArray? ProjectTemplateAttachments(TemplateAttachment[]? attachments)
        => attachments is { Length: > 0 }
            ? new JsonArray([.. attachments.Select(a => (JsonNode?)new JsonObject
            {
                ["filename"] = a.Filename,
                ["id"] = a.Id,
                ["is_inline"] = a.IsInline,
                ["cid"] = a.Cid,
            })])
            : null;

    /// <summary>地址数组入参：字符串数组 → <see cref="MailAddress"/> 数组；空数组归一为 null。</summary>
    private static MailAddress[]? ToAddresses(string[]? addresses)
        => addresses is { Length: > 0 }
            ? addresses.Select(a => new MailAddress { MailAddressSuffix = a }).ToArray()
            : null;

    /// <summary>组装模板对象（create/update 共用；省略的可选字段以默认值/空参与整体覆盖）。</summary>
    private static MailTemplate BuildTemplate(
        string name,
        string? subject,
        string? templateContent,
        bool? isPlainTextMode,
        string[]? tos,
        string[]? ccs,
        string[]? bccs)
        => new()
        {
            Name = name,
            Subject = subject,
            TemplateContent = templateContent,
            IsPlainTextMode = isPlainTextMode,
            Tos = ToAddresses(tos),
            Ccs = ToAddresses(ccs),
            Bccs = ToAddresses(bccs),
        };

    /// <summary>字符串数组 → JSON 数组（AddNode 逐项，规避泛型 Add 的裁剪注解）。</summary>
    private static JsonArray ToJsonArray(string[] values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.AddNode(value);
        }

        return array;
    }
}
