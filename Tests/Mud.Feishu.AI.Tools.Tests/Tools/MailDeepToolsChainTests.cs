// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// T-3（mail 域成环，15 个）的<b>真实调用链路</b>用例：草稿四件套 + 处置/附件 2 + 规则 4 + 模板与签名 5。
/// 覆盖三条"看似成功实则未生效"的平台陷阱（automation_send_disable、send_time 过早、规则 DSL 非法组合）。
/// </summary>
public class MailDeepToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuUserV1MailDraft> _draftClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1MailMessage> _messageClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1MailRule> _ruleClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1MailTemplate> _templateClient = new();
    private readonly Mock<Mud.Feishu.IFeishuUserV1MailTemplate> _templateUserClient = new();

    private MailDeepTools CreateTools(
        bool withDraft = true,
        bool withMessage = true,
        bool withRule = true,
        bool withTemplate = true,
        bool withTemplateUser = true)
        => new(
            withDraft ? _draftClient.Object : null,
            withMessage ? _messageClient.Object : null,
            withRule ? _ruleClient.Object : null,
            withTemplate ? _templateClient.Object : null,
            withTemplateUser ? _templateUserClient.Object : null,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }));

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static string DecodeEml(string base64Url)
    {
        var standard = base64Url.Replace('-', '+').Replace('_', '/');
        var pad = standard.Length % 4;
        standard += pad == 2 ? "==" : pad == 3 ? "=" : string.Empty;
        return Encoding.UTF8.GetString(Convert.FromBase64String(standard));
    }

    // ───────────────────── 契约事实（method / route / identity） ─────────────────────

    [Theory]
    [InlineData("mail.create_draft", "POST", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts", "user")]
    [InlineData("mail.update_draft", "PUT", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts/{draft_id}", "user")]
    [InlineData("mail.send", "POST", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts/{draft_id}/send", "user")]
    [InlineData("mail.delete_draft", "DELETE", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/drafts/{draft_id}", "user")]
    [InlineData("mail.trash_message", "POST", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/messages/batch_trash", "tenant")]
    [InlineData("mail.get_attachment_url", "GET", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/messages/{message_id}/attachments/download_url", "tenant")]
    [InlineData("mail.rule_list", "GET", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/rules", "tenant")]
    [InlineData("mail.rule_save", "POST", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/rules", "tenant")]
    [InlineData("mail.rule_delete", "DELETE", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/rules/{rule_id}", "tenant")]
    [InlineData("mail.rule_reorder", "POST", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/rules/reorder", "tenant")]
    [InlineData("mail.template_list", "GET", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/templates", "tenant")]
    [InlineData("mail.template_get", "GET", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/templates/{template_id}", "tenant")]
    [InlineData("mail.template_save", "POST", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/templates", "tenant")]
    [InlineData("mail.template_delete", "DELETE", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/templates/{template_id}", "tenant")]
    [InlineData("mail.signature_list", "GET", "/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/settings/signatures", "user")]
    public void Contracts_ShouldCarrySdkMethodRouteAndIdentity(string toolName, string httpMethod, string route, string identity)
    {
        FeishuToolContracts.ByToolName.Should().ContainKey(toolName);
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be(httpMethod);
        contract.Route.Should().Be(route);
        contract.Identity.Should().Be(identity, "草稿四件套与签名列表走用户令牌，其余走 tenant");
    }

    // ───────────────────── 草稿（create / update / send / delete） ─────────────────────

    [Fact]
    public async Task CreateDraft_ShouldAssembleEmlIntoBase64UrlRaw()
    {
        string? capturedMailbox = null;
        UserMailboxDraftRequest? captured = null;
        _draftClient
            .Setup(c => c.CreateUserMailboxDraftAsync(
                It.IsAny<string>(), It.IsAny<UserMailboxDraftRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string mailbox, UserMailboxDraftRequest request, CancellationToken _) =>
            {
                capturedMailbox = mailbox;
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<UserMailboxDraftOopsResult>
            {
                Code = 0,
                Data = new UserMailboxDraftOopsResult { Draft = new MailDraft { Id = "draft_1" } },
            });

        var result = await CreateTools().CreateDraftAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("to", new[] { "recipient@example.com" }),
                ("cc", new[] { "cc@example.com" }),
                ("subject", "周报"),
                ("body", "本周完成 T-3")),
            CancellationToken.None);

        capturedMailbox.Should().Be("user@example.com", "user_mailbox_id 经 [Path] 下发");
        captured!.Raw.Should().NotBeNullOrEmpty();
        captured.Raw.Should().MatchRegex("^[A-Za-z0-9_-]+$", "base64url——不得出现 + / = 三个字符");
        var eml = DecodeEml(captured.Raw!);
        eml.Should().Contain("To: <recipient@example.com>");
        eml.Should().Contain("Cc: <cc@example.com>");
        eml.Should().Contain("Subject: 周报", "模型给扁平字段，EML 组装在工具层");
        result.ToString().Should().Contain("draft_1", "后续 update/send/delete 的 draft_id 唯一来源");
    }

    [Fact]
    public async Task CreateDraft_DryRun_ShouldNotEchoBody_AndNotTouchDownstream()
    {
        var result = await CreateTools().CreateDraftAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("to", new[] { "recipient@example.com" }),
                ("subject", "绝密主题"),
                ("body", "绝密正文"),
                ("dry_run", true)),
            CancellationToken.None);

        result.ToString().Should().StartWith("[dry_run]");
        result.ToString().Should().NotContain("绝密正文", "dry_run 摘要只报长度，不得成为绕过出站净化的回显通道");
        _draftClient.Verify(
            c => c.CreateUserMailboxDraftAsync(
                It.IsAny<string>(), It.IsAny<UserMailboxDraftRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateDraft_ShouldAddressDraftId_AndReplaceWholeRaw()
    {
        string? capturedDraftId = null;
        UserMailboxDraftRequest? captured = null;
        _draftClient
            .Setup(c => c.UpdateUserMailboxDraftAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserMailboxDraftRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string draftId, UserMailboxDraftRequest request, CancellationToken __) =>
            {
                capturedDraftId = draftId;
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<UserMailboxDraftOopsResult>
            {
                Code = 0,
                Data = new UserMailboxDraftOopsResult { Draft = new MailDraft { Id = "draft_1" } },
            });

        var result = await CreateTools().UpdateDraftAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("draft_id", "draft_1"),
                ("to", new[] { "a@example.com" }),
                ("subject", "改后的主题"),
                ("body", "改后的正文")),
            CancellationToken.None);

        capturedDraftId.Should().Be("draft_1");
        DecodeEml(captured!.Raw!).Should().Contain("Subject: 改后的主题");
        result.ToString().Should().Contain("draft_1");
    }

    [Fact]
    public async Task SendDraftPath_ShouldConvertRfc3339SendTimeIntoUnixSeconds()
    {
        SendUserMailboxDraftRequest? captured = null;
        _draftClient
            .Setup(c => c.SendUserMailboxDraftAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SendUserMailboxDraftRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, SendUserMailboxDraftRequest request, CancellationToken ___) => captured = request)
            .ReturnsAsync(new FeishuApiResult<SendUserMailboxDraftResult>
            {
                Code = 0,
                Data = new SendUserMailboxDraftResult { MessageId = "msg_1", ThreadId = "thr_1" },
            });

        var sendTime = DateTimeOffset.UtcNow.AddDays(2).ToString("yyyy-MM-ddTHH:mm:sszzz");
        var result = await CreateTools().SendAsync(
            Args(("user_mailbox_id", "user@example.com"), ("draft_id", "draft_1"), ("send_time", sendTime)),
            CancellationToken.None);

        captured!.SendTime.Should().Be(
            DateTimeOffset.Parse(sendTime).ToUnixTimeSeconds().ToString(),
            "模型只学 RFC3339；秒级时间戳由工具层转换");
        result.ToString().Should().Contain("msg_1");
        result.ToString().Should().Contain("thr_1");
    }

    [Fact]
    public async Task SendDraftPath_WithoutSendTime_ShouldSendImmediately()
    {
        SendUserMailboxDraftRequest? captured = null;
        _draftClient
            .Setup(c => c.SendUserMailboxDraftAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SendUserMailboxDraftRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, SendUserMailboxDraftRequest request, CancellationToken ___) => captured = request)
            .ReturnsAsync(new FeishuApiResult<SendUserMailboxDraftResult>
            {
                Code = 0,
                Data = new SendUserMailboxDraftResult { MessageId = "msg_2" },
            });

        await CreateTools().SendAsync(
            Args(("user_mailbox_id", "user@example.com"), ("draft_id", "draft_1")), CancellationToken.None);

        captured!.SendTime.Should().BeNull("省略 send_time = 立即发送，不得下发 0（平台按过期定时处理）");
    }

    [Fact]
    public async Task Send_ComposePath_ShouldCreateDraftThenSendIt()
    {
        UserMailboxDraftRequest? created = null;
        string? sentDraftId = null;
        _draftClient
            .Setup(c => c.CreateUserMailboxDraftAsync(
                It.IsAny<string>(), It.IsAny<UserMailboxDraftRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, UserMailboxDraftRequest request, CancellationToken __) => created = request)
            .ReturnsAsync(new FeishuApiResult<UserMailboxDraftOopsResult>
            {
                Code = 0,
                Data = new UserMailboxDraftOopsResult { Draft = new MailDraft { Id = "draft_new" } },
            });
        _draftClient
            .Setup(c => c.SendUserMailboxDraftAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SendUserMailboxDraftRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string draftId, SendUserMailboxDraftRequest __, CancellationToken ___) => sentDraftId = draftId)
            .ReturnsAsync(new FeishuApiResult<SendUserMailboxDraftResult>
            {
                Code = 0,
                Data = new SendUserMailboxDraftResult { MessageId = "msg_3", ThreadId = "thr_3" },
            });

        var result = await CreateTools().SendAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("to", new[] { "recipient@example.com" }),
                ("subject", "现写现发"),
                ("body", "正文")),
            CancellationToken.None);

        DecodeEml(created!.Raw!).Should().Contain("Subject: 现写现发", "现写现发复用同一套 EML 组装");
        sentDraftId.Should().Be("draft_new", "发出的必须是本次新建的那份草稿");
        result.ToString().Should().Contain("msg_3");
        result.ToString().Should().Contain("draft_new", "现写现发回执额外给出中间态 draft_id，便于排查");
    }

    [Fact]
    public async Task Send_WithBothDraftIdAndComposeFields_ShouldRejectBeforeDownstream()
    {
        var result = await CreateTools().SendAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("draft_id", "draft_1"),
                ("to", new[] { "recipient@example.com" })),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] mail.send");
        result.ToString().Should().Contain("互斥", "既给草稿又给正文时「发出的是哪份内容」不可判定");
        _draftClient.Verify(
            c => c.SendUserMailboxDraftAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SendUserMailboxDraftRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Send_WithoutDraftIdAndMissingSubject_ShouldRejectBeforeDownstream()
    {
        var result = await CreateTools().SendAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("to", new[] { "recipient@example.com" }),
                ("body", "只有正文")),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] mail.send");
        result.ToString().Should().Contain("mail.create_draft", "只想存草稿的意图要指向正确入口");
        _draftClient.Verify(
            c => c.CreateUserMailboxDraftAsync(
                It.IsAny<string>(), It.IsAny<UserMailboxDraftRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendDraftPath_AutomationSendDisable_ShouldSurfaceAsError()
    {
        _draftClient
            .Setup(c => c.SendUserMailboxDraftAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SendUserMailboxDraftRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<SendUserMailboxDraftResult>
            {
                Code = 0,
                Data = new SendUserMailboxDraftResult
                {
                    AutomationSendDisable = new AutomationSendDisable
                    {
                        Reason = "admin_policy",
                        Reference = "https://open.feishu.cn/x",
                    },
                },
            });

        var result = await CreateTools().SendAsync(
            Args(("user_mailbox_id", "user@example.com"), ("draft_id", "draft_1")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] mail.send");
        result.ToString().Should().Contain("automation_send_disable",
            "code=0 但邮件未发出——不拦截就是静默反向失败");
        result.ToString().Should().Contain("admin_policy");
    }

    [Theory]
    [InlineData("2026-10-20T09:00:00")]
    [InlineData("明天上午")]
    public async Task SendDraftPath_WithNonRfc3339SendTime_ShouldReturnStructuredError(string sendTime)
    {
        var result = await CreateTools().SendAsync(
            Args(("user_mailbox_id", "u@example.com"), ("draft_id", "d1"), ("send_time", sendTime)),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] mail.send");
        result.ToString().Should().Contain("带时区的 RFC3339");
        _draftClient.Verify(
            c => c.SendUserMailboxDraftAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SendUserMailboxDraftRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendDraftPath_SendTimeTooClose_ShouldReturnStructuredError()
    {
        var tooSoon = DateTimeOffset.UtcNow.AddMinutes(1).ToString("yyyy-MM-ddTHH:mm:sszzz");

        var result = await CreateTools().SendAsync(
            Args(("user_mailbox_id", "u@example.com"), ("draft_id", "d1"), ("send_time", tooSoon)),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] mail.send");
        result.ToString().Should().Contain("5 分钟", "平台要求定时至少晚于当前 5 分钟");
    }

    [Fact]
    public async Task DeleteDraft_ShouldAcceptNullDataSuccess()
    {
        _draftClient
            .Setup(c => c.DeleteUserMailboxDraftAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateTools().DeleteDraftAsync(
            Args(("user_mailbox_id", "user@example.com"), ("draft_id", "draft_1")), CancellationToken.None);

        result.ToString().Should().Contain("deleted");
    }

    [Fact]
    public async Task DeleteDraft_WithoutDraftClient_ShouldReturnStructuredError()
    {
        var result = await CreateTools(withDraft: false).DeleteDraftAsync(
            Args(("user_mailbox_id", "u@example.com"), ("draft_id", "d1")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] mail.delete_draft");
        result.ToString().Should().Contain("IFeishuUserV1MailDraft", "软缺席必须说明宿主须启用什么");
    }

    // ───────────────────── 处置与附件 ─────────────────────

    [Fact]
    public async Task TrashMessage_ShouldBatchIds_AndStateRecycleBinSemantics()
    {
        string? capturedMailbox = null;
        BatchTrashUserMailboxMessageRequest? captured = null;
        _messageClient
            .Setup(c => c.BatchTrashUserMailboxMessageAsync(
                It.IsAny<string>(), It.IsAny<BatchTrashUserMailboxMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string mailbox, BatchTrashUserMailboxMessageRequest request, CancellationToken _) =>
            {
                capturedMailbox = mailbox;
                captured = request;
            })
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateTools().TrashMessageAsync(
            Args(("user_mailbox_id", "user@example.com"), ("message_ids", new[] { "m1", "m2" })),
            CancellationToken.None);

        capturedMailbox.Should().Be("user@example.com");
        captured!.MessageIds.Should().Equal("m1", "m2");
        result.ToString().Should().Contain("trashed");
        result.ToString().Should().Contain("count", "回收站语义：进回收站而非彻底删除，回执给数量");
    }

    [Fact]
    public async Task GetAttachmentUrl_ShouldJoinAttachmentIdsIntoSingleQuery()
    {
        string? capturedAttachmentIds = null;
        string? capturedMessageId = null;
        _messageClient
            .Setup(c => c.GetMessageAttachmentDownloadUrlAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string messageId, string? attachmentIds, CancellationToken __) =>
            {
                capturedMessageId = messageId;
                capturedAttachmentIds = attachmentIds;
            })
            .ReturnsAsync(new FeishuApiResult<AttachmentDownloadUrlResult>
            {
                Code = 0,
                Data = new AttachmentDownloadUrlResult
                {
                    DownloadUrls =
                    [
                        new AttachmentDownloadUrlItem { AttachmentId = "att_1", DownloadUrl = "https://dl/att_1" },
                    ],
                    FailedIds = ["att_2"],
                },
            });

        var result = await CreateTools().GetAttachmentUrlAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("message_id", "msg_1"),
                ("attachment_ids", new[] { "att_1", "att_2" })),
            CancellationToken.None);

        capturedMessageId.Should().Be("msg_1");
        capturedAttachmentIds.Should().Be("att_1,att_2", "平台按逗号分隔单值接收 attachment_ids");
        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("downloads")[0].GetProperty("download_url").GetString()
            .Should().Be("https://dl/att_1");
        document.RootElement.GetProperty("failed_ids")[0].GetString().Should().Be("att_2");
    }

    // ───────────────────── 收件规则（DSL 编译 / 反渲染） ─────────────────────

    [Fact]
    public async Task RuleList_ShouldReverseRenderPlatformCodesIntoSimplifiedForms()
    {
        _ruleClient
            .Setup(c => c.GetMailboxRuleListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetMailboxRuleListResult>
            {
                Code = 0,
                Data = new GetMailboxRuleListResult
                {
                    Items =
                    [
                        new UserMailboxRuleInfo
                        {
                            Id = "rule_1",
                            Name = "发票归档",
                            IsEnable = true,
                            IgnoreTheRestOfRules = false,
                            Condition = new RuleCondition
                            {
                                MatchType = 1,
                                Items = [new RuleConditionItem { Type = 6, Operator = 1, Input = "发票" }],
                            },
                            Action = new RuleAction
                            {
                                Items = [new RuleActionItem { Type = 1 }],
                            },
                        },
                    ],
                },
            });

        var result = await CreateTools().RuleListAsync(
            Args(("user_mailbox_id", "user@example.com")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        var rule = document.RootElement.GetProperty("rules")[0];
        rule.GetProperty("match_all").GetBoolean().Should().BeTrue();
        rule.GetProperty("conditions")[0].GetString().Should().Be("subject|contains|发票");
        rule.GetProperty("actions")[0].GetString().Should().Be("archive");
    }

    [Fact]
    public async Task RuleSave_CreatePath_ShouldCompileSimplifiedFormsIntoPlatformCodes()
    {
        CreateUserMailboxRuleRequest? captured = null;
        _ruleClient
            .Setup(c => c.CreateUserMailboxRuleAsync(
                It.IsAny<string>(), It.IsAny<CreateUserMailboxRuleRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, CreateUserMailboxRuleRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<CreateUserMailboxRuleResult>
            {
                Code = 0,
                Data = new CreateUserMailboxRuleResult { Rule = new UserMailboxRuleInfo { Id = "rule_1" } },
            });

        var result = await CreateTools().RuleSaveAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("name", "发票归档"),
                ("conditions", new[] { "subject|contains|发票", "has_attachment" }),
                ("actions", new[] { "archive", "mark_read" })),
            CancellationToken.None);

        captured!.Name.Should().Be("发票归档");
        captured.IsEnable.Should().BeTrue("缺省即启用——不得下发 null 让平台按未启用创建");
        captured.IgnoreTheRestOfRules.Should().BeFalse();
        captured.Condition!.MatchType.Should().Be(1);
        captured.Condition.Items![0].Type.Should().Be(6);
        captured.Condition.Items[0].Operator.Should().Be(1);
        captured.Condition.Items[0].Input.Should().Be("发票");
        captured.Condition.Items[1].Type.Should().Be(16, "has_attachment 是旗标字段，无 operator/value");
        captured.Action!.Items.Should().HaveCount(2);
        result.ToString().Should().Contain("rule_1");
    }

    [Fact]
    public async Task RuleSave_WithIllegalField_ShouldFailBeforeDownstream_AndShowLegalForms()
    {
        var result = await CreateTools().RuleSaveAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("name", "坏规则"),
                ("conditions", new[] { "totally_unknown|contains|x" }),
                ("actions", new[] { "archive" }),
                ("dry_run", true)),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] mail.rule_save");
        result.ToString().Should().Contain("未知的条件字段");
        result.ToString().Should().Contain("subject|contains|", "错误消息必须附合法写法，模型才能自我纠正");
        _ruleClient.Verify(
            c => c.CreateUserMailboxRuleAsync(
                It.IsAny<string>(), It.IsAny<CreateUserMailboxRuleRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RuleSave_CreatePath_DryRun_ShouldReportCompiledStructure()
    {
        var result = await CreateTools().RuleSaveAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("name", "发票归档"),
                ("conditions", new[] { "subject|contains|发票" }),
                ("actions", new[] { "move_to_folder|fld_1" }),
                ("dry_run", true)),
            CancellationToken.None);

        result.ToString().Should().StartWith("[dry_run]");
        result.ToString().Should().Contain("type=6", "dry_run 展示编译结果，让模型确认简化式被理解成什么平台代码");
        _ruleClient.Verify(
            c => c.CreateUserMailboxRuleAsync(
                It.IsAny<string>(), It.IsAny<CreateUserMailboxRuleRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RuleSave_UpdatePath_ShouldAddressRuleId_AndReplaceWholeRule()
    {
        string? capturedRuleId = null;
        UpdateUserMailboxRuleRequest? captured = null;
        _ruleClient
            .Setup(c => c.UpdateUserMailboxRuleAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateUserMailboxRuleRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string ruleId, UpdateUserMailboxRuleRequest request, CancellationToken __) =>
            {
                capturedRuleId = ruleId;
                captured = request;
            })
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateTools().RuleSaveAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("rule_id", "rule_1"),
                ("name", "改名后的规则"),
                ("conditions", new[] { "sender|is|a@b.com" }),
                ("actions", new[] { "no_notify" })),
            CancellationToken.None);

        capturedRuleId.Should().Be("rule_1");
        captured!.Name.Should().Be("改名后的规则");
        captured.Action!.Items![0].Type.Should().Be(10);
        result.ToString().Should().Contain("updated");
    }

    [Fact]
    public async Task RuleDelete_ShouldReturnDeletedReceipt()
    {
        _ruleClient
            .Setup(c => c.DeleteUserMailboxRuleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateTools().RuleDeleteAsync(
            Args(("user_mailbox_id", "user@example.com"), ("rule_id", "rule_1")), CancellationToken.None);

        result.ToString().Should().Contain("deleted");
    }

    [Fact]
    public async Task RuleDelete_NonZeroCode_ShouldReturnStructuredError()
    {
        _ruleClient
            .Setup(c => c.DeleteUserMailboxRuleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 1000, Msg = "rule not found" });

        var result = await CreateTools().RuleDeleteAsync(
            Args(("user_mailbox_id", "user@example.com"), ("rule_id", "rule_x")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] mail.rule_delete");
        result.ToString().Should().Contain("1000");
    }

    [Fact]
    public async Task RuleReorder_ShouldForwardFullOrderedIdList()
    {
        ReorderUserMailboxRuleRequest? captured = null;
        _ruleClient
            .Setup(c => c.ReorderUserMailboxRuleAsync(
                It.IsAny<string>(), It.IsAny<ReorderUserMailboxRuleRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, ReorderUserMailboxRuleRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateTools().RuleReorderAsync(
            Args(("user_mailbox_id", "user@example.com"), ("rule_ids", new[] { "rule_b", "rule_a" })),
            CancellationToken.None);

        captured!.RuleIds.Should().Equal(new[] { "rule_b", "rule_a" });
        result.ToString().Should().Contain("reordered");
    }

    [Theory]
    [InlineData(6, "subject|contains|发票")]
    [InlineData(12, "all")]
    [InlineData(16, "has_attachment")]
    [InlineData(13, "is_external")]
    public void RuleDsl_ShouldRoundTripFlagAndValueConditions(int type, string simplified)
    {
        var compiled = MailRuleDsl.CompileConditions([simplified], null);

        compiled.Items.Should().ContainSingle();
        compiled.Items![0].Type.Should().Be(type);

        MailRuleDsl.DescribeConditions(compiled).Single().Should().Be(simplified,
            "正向编译与反向渲染必须互逆，否则 rule_list 读出的规则无法原样回写");
    }

    [Fact]
    public void RuleDsl_ShouldRoundTripMoveToFolder_WithFolderIdPreserved()
    {
        var compiled = MailRuleDsl.CompileConditions(["body|not_contains|spam"], matchAll: false);

        compiled.MatchType.Should().Be(2, "match_all=false = 满足任一条件");
        compiled.Items![0].Type.Should().Be(7);
        compiled.Items[0].Operator.Should().Be(2);

        var action = MailRuleDsl.CompileActions(["move_to_folder|fld_9"]);

        action.Items![0].Type.Should().Be(11);
        action.Items[0].Input.Should().Be("fld_9");
        MailRuleDsl.DescribeActions(action).Should().Equal("move_to_folder|fld_9");
    }

    [Fact]
    public void RuleDsl_ShouldRejectOperatorlessFieldWithTrailingPipe()
    {
        var act = () => MailRuleDsl.CompileConditions(["has_attachment|contains|1"], null);

        act.Should().Throw<ArgumentException>().WithMessage("*旗标型字段*");
    }

    // ───────────────────── 模板与签名 ─────────────────────

    [Fact]
    public async Task TemplateList_ShouldProjectTemplateIdNameCreateTime()
    {
        _templateClient
            .Setup(c => c.GetMailTemplateListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetMailTemplateListResult>
            {
                Code = 0,
                Data = new GetMailTemplateListResult
                {
                    Items = [new TemplateItem { TemplateId = "tpl_1", Name = "报价回复", CreateTime = "1759312800" }],
                },
            });

        var result = await CreateTools().TemplateListAsync(
            Args(("user_mailbox_id", "user@example.com")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("total").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("items")[0].GetProperty("template_id").GetString().Should().Be("tpl_1");
    }

    [Fact]
    public async Task TemplateGet_WhenMissing_ShouldReturnFoundSentinel()
    {
        _templateClient
            .Setup(c => c.GetMailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetMailTemplateResult> { Code = 0, Data = new GetMailTemplateResult() });

        var result = await CreateTools().TemplateGetAsync(
            Args(("user_mailbox_id", "user@example.com"), ("template_id", "tpl_missing")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("found").GetBoolean().Should().BeFalse(
            "空结果哨兵：显式未找到，而不是空对象让模型猜");
        document.RootElement.GetProperty("template_id").GetString().Should().Be("tpl_missing");
    }

    [Fact]
    public async Task TemplateGet_ShouldProjectAddressesIntoAddressAndName()
    {
        _templateClient
            .Setup(c => c.GetMailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetMailTemplateResult>
            {
                Code = 0,
                Data = new GetMailTemplateResult
                {
                    Template = new MailTemplateInfo
                    {
                        TemplateId = "tpl_1",
                        Name = "报价回复",
                        Subject = "报价",
                        TemplateContent = "<p>正文</p>",
                        IsPlainTextMode = false,
                        Tos = [new MailAddress { MailAddressSuffix = "a@b.com", Name = "A" }],
                    },
                },
            });

        var result = await CreateTools().TemplateGetAsync(
            Args(("user_mailbox_id", "user@example.com"), ("template_id", "tpl_1")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        var root = document.RootElement;
        root.GetProperty("found").GetBoolean().Should().BeTrue();
        root.GetProperty("template_content").GetString().Should().Be("<p>正文</p>");
        root.GetProperty("tos")[0].GetProperty("address").GetString().Should().Be("a@b.com");
        root.GetProperty("ccs").ValueKind.Should().Be(JsonValueKind.Null,
            "省略的默认地址数组归一为 null，不产出无信息量的空壳");
    }

    [Fact]
    public async Task TemplateSave_CreatePath_ShouldAssembleTemplateObjectFromFlatArgs()
    {
        CreateMailTemplateRequest? captured = null;
        _templateClient
            .Setup(c => c.CreateMailTemplateAsync(
                It.IsAny<string>(), It.IsAny<CreateMailTemplateRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, CreateMailTemplateRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<CreateMailTemplateResult>
            {
                Code = 0,
                Data = new CreateMailTemplateResult { Template = new MailTemplateInfo { TemplateId = "tpl_1" } },
            });

        var result = await CreateTools().TemplateSaveAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("name", "报价回复模板"),
                ("subject", "报价"),
                ("template_content", "<p>正文</p>"),
                ("is_plain_text_mode", false),
                ("tos", new[] { "a@b.com" }),
                ("ccs", new[] { "c@d.com" })),
            CancellationToken.None);

        captured!.Template!.Name.Should().Be("报价回复模板");
        captured.Template.Tos.Should().ContainSingle(a => a.MailAddressSuffix == "a@b.com");
        captured.Template.Bccs.Should().BeNull("省略的默认地址数组不得下发空数组");
        result.ToString().Should().Contain("tpl_1", "template_id 是 get/update/delete 的入参来源");
    }

    [Fact]
    public async Task TemplateSave_UpdatePath_ShouldBackfillInputTemplateId()
    {
        _templateClient
            .Setup(c => c.UpdateMailTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateMailTemplateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<UpdateMailTemplateResult>
            {
                Code = 0,
                Data = new UpdateMailTemplateResult { Template = new MailTemplate { Name = "改名后模板" } },
            });

        var result = await CreateTools().TemplateSaveAsync(
            Args(
                ("user_mailbox_id", "user@example.com"),
                ("template_id", "tpl_1"),
                ("name", "改名后模板")),
            CancellationToken.None);

        result.ToString().Should().Contain("tpl_1", "响应体不带 id——必须回填，否则模型失去后续引用");
        result.ToString().Should().Contain("updated");
    }

    [Fact]
    public async Task TemplateDelete_ShouldReturnDeletedReceipt()
    {
        _templateClient
            .Setup(c => c.DeleteMailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateTools().TemplateDeleteAsync(
            Args(("user_mailbox_id", "user@example.com"), ("template_id", "tpl_1")), CancellationToken.None);

        result.ToString().Should().Contain("deleted");
        _templateClient.Verify(
            c => c.DeleteMailTemplateAsync("user@example.com", "tpl_1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignatureList_ShouldTruncateSignatureContent_AndPairUsages()
    {
        _templateUserClient
            .Setup(c => c.GetUserMailboxSignaturesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MailboxSignaturesResult>
            {
                Code = 0,
                Data = new MailboxSignaturesResult
                {
                    Signatures =
                    [
                        new MailboxSignature
                        {
                            Id = "sig_1",
                            Name = "默认签名",
                            Content = new string('S', 5_000),
                            SignatureType = "1",
                            SignatureDevice = "web",
                        },
                    ],
                    Usages =
                    [
                        new MailboxSignatureUsage
                        {
                            EmailAddress = "user@example.com",
                            SendMailSignatureId = "sig_1",
                            ReplySignatureId = "sig_2",
                        },
                    ],
                },
            });

        var result = await CreateTools().SignatureListAsync(
            Args(("user_mailbox_id", "user@example.com")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        var signature = document.RootElement.GetProperty("signatures")[0];
        signature.GetProperty("signature_id").GetString().Should().Be("sig_1");
        signature.GetProperty("preview").GetString()!.Length.Should().BeLessThan(5_000,
            "签名正文只回填截断预览——全文既超预算也无决策价值");
        document.RootElement.GetProperty("usages")[0].GetProperty("send_mail_signature_id").GetString().Should().Be("sig_1");
    }
}
