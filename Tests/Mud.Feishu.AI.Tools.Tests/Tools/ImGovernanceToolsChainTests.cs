// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// T-3（im 域写面成环，4 个）的<b>真实调用链路</b>用例：
/// <c>im.edit_message</c> / <c>im.patch_card_message</c> / <c>im.urgent_app</c> / <c>im.urgent_sms</c>。
/// </summary>
public class ImGovernanceToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1Message> _messageClient = new();

    private MessageWriteTools CreateTools() => new(_messageClient.Object);

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    // ───────────────────── 契约事实 ─────────────────────

    [Theory]
    [InlineData("im.edit_message", "PUT", "/open-apis/im/v1/messages/{message_id}")]
    [InlineData("im.patch_card_message", "PATCH", "/open-apis/im/v1/messages/{message_id}")]
    [InlineData("im.urgent_app", "PATCH", "/open-apis/im/v1/messages/{message_id}/urgent_app")]
    [InlineData("im.urgent_sms", "PATCH", "/open-apis/im/v1/messages/{message_id}/urgent_sms")]
    public void Contracts_ShouldCarrySdkMethodAndRoute(string toolName, string httpMethod, string route)
    {
        FeishuToolContracts.ByToolName.Should().ContainKey(toolName);
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be(httpMethod);
        contract.Route.Should().Be(route);
        contract.Identity.Should().Be("tenant");
    }

    // ───────────────────── im.edit_message ─────────────────────

    [Fact]
    public async Task EditMessage_ShouldWrapPlainTextIntoContentJson_AndPinMsgTypeText()
    {
        string? capturedMessageId = null;
        EditMessageRequest? captured = null;
        _messageClient
            .Setup(c => c.EditMessageAsync(
                It.IsAny<string>(), It.IsAny<EditMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string messageId, EditMessageRequest request, CancellationToken _) =>
            {
                capturedMessageId = messageId;
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_1" },
            });

        var result = await CreateTools().EditMessageAsync(
            Args(("message_id", "om_1"), ("text", "改后的正文\"含引号\"")), CancellationToken.None);

        capturedMessageId.Should().Be("om_1", "message_id 经 [Path] 下发");
        captured!.MsgType.Should().Be("text", "编辑走文本消息固定形态");
        using (var document = JsonDocument.Parse(captured.Content!))
        {
            document.RootElement.GetProperty("text").GetString().Should().Be("改后的正文\"含引号\"",
                "正文经 JsonNode 转义——手拼字符串会在这里产出非法 JSON");
        }

        result.ToString().Should().Contain("om_1");
    }

    [Fact]
    public async Task EditMessage_MissingText_ShouldReturnStructuredError()
    {
        var result = await CreateTools().EditMessageAsync(Args(("message_id", "om_1")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] im.edit_message");
        _messageClient.Verify(
            c => c.EditMessageAsync(It.IsAny<string>(), It.IsAny<EditMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EditMessage_DryRun_ShouldNotEchoText_AndNotTouchDownstream()
    {
        var result = await CreateTools().EditMessageAsync(
            Args(("message_id", "om_1"), ("text", "绝密改后正文"), ("dry_run", true)), CancellationToken.None);

        result.ToString().Should().StartWith("[dry_run]");
        result.ToString().Should().NotContain("绝密改后正文");
        _messageClient.Verify(
            c => c.EditMessageAsync(It.IsAny<string>(), It.IsAny<EditMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ───────────────────── im.patch_card_message ─────────────────────

    [Fact]
    public async Task PatchCardMessage_ShouldCompileDslIntoWholeCardContent()
    {
        string? capturedMessageId = null;
        PatchMessageRequest? captured = null;
        _messageClient
            .Setup(c => c.PatchMessageAsync(
                It.IsAny<string>(), It.IsAny<PatchMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string messageId, PatchMessageRequest request, CancellationToken _) =>
            {
                capturedMessageId = messageId;
                captured = request;
            })
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateTools().PatchCardMessageAsync(
            Args(
                ("message_id", "om_card"),
                ("title", "构建结果"),
                ("body", "text:全部通过"),
                ("buttons", "查看详情|url|https://example.com\n重新运行|value|rerun")),
            CancellationToken.None);

        capturedMessageId.Should().Be("om_card");
        captured!.Content.Should().NotBeNullOrEmpty("整卡覆盖：下发完整卡片 JSON，不是增量字段");

        // 卡片 JSON 非 ASCII 走 \uXXXX 转义（JsonNode.ToJsonString 默认编码器），故按结构断言而非按原文包含。
        using var document = JsonDocument.Parse(captured.Content);
        var card = document.RootElement.GetProperty("zh_cn");
        card.GetProperty("title").GetString().Should().Be("构建结果");
        var elements = card.GetProperty("elements");
        elements[0].GetProperty("text").GetProperty("content").GetString().Should().Be("全部通过");
        var actions = elements[elements.GetArrayLength() - 1].GetProperty("actions");
        actions[0].GetProperty("url").GetString().Should().Be("https://example.com");
        actions[0].GetProperty("type").GetString().Should().Be("primary", "首个按钮主色：主操作在视觉上可分辨");
        actions[1].GetProperty("value").GetProperty("key").GetString().Should().Be("rerun", "value 型按钮的回调值不得丢失");
        actions[1].GetProperty("type").GetString().Should().Be("default");
        result.ToString().Should().Contain("updated");
    }

    [Fact]
    public async Task PatchCardMessage_WithIllegalDsl_ShouldFailBeforeDownstream_AndShowLegalForms()
    {
        var result = await CreateTools().PatchCardMessageAsync(
            Args(("message_id", "om_card"), ("body", "unknown_kind:内容")),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] im.patch_card_message");
        result.ToString().Should().Contain("未知的卡片元素类型");
        result.ToString().Should().Contain(CardDsl.LegalForms, "错误消息必须附合法写法，模型才能自我纠正");
        _messageClient.Verify(
            c => c.PatchMessageAsync(
                It.IsAny<string>(), It.IsAny<PatchMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PatchCardMessage_WithIllegalButtonCallback_ShouldNameTheClosedSet()
    {
        var result = await CreateTools().PatchCardMessageAsync(
            Args(("message_id", "om_card"), ("body", "text:内容"), ("buttons", "查看详情|open_url|https://example.com")),
            CancellationToken.None);

        result.ToString().Should().Contain("未知的按钮回调类型");
        result.ToString().Should().Contain("只支持 url / value", "给出闭集，模型才能一次改对（open_url 是最常见的误写）");
        _messageClient.Verify(
            c => c.PatchMessageAsync(
                It.IsAny<string>(), It.IsAny<PatchMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PatchCardMessage_NonZeroCode_ShouldReturnStructuredError()
    {
        _messageClient
            .Setup(c => c.PatchMessageAsync(
                It.IsAny<string>(), It.IsAny<PatchMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 230001, Msg = "message not found" });

        var result = await CreateTools().PatchCardMessageAsync(
            Args(("message_id", "om_missing"), ("body", "text:内容")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] im.patch_card_message");
        result.ToString().Should().Contain("230001", "无 data 的成功判定也必须读 code");
    }

    // ───────────────────── im.urgent_app / urgent_sms ─────────────────────

    [Fact]
    public async Task UrgentApp_ShouldDefaultOpenId_AndProjectInvalidUserIds()
    {
        string? capturedMessageId = null;
        string? capturedUserIdType = null;
        MessageUrgentRequest? captured = null;
        _messageClient
            .Setup(c => c.MessageUrgentAppAsync(
                It.IsAny<string>(), It.IsAny<MessageUrgentRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string messageId, MessageUrgentRequest request, string userIdType, CancellationToken _) =>
            {
                capturedMessageId = messageId;
                captured = request;
                capturedUserIdType = userIdType;
            })
            .ReturnsAsync(new FeishuApiResult<MessageUrgentResult>
            {
                Code = 0,
                Data = new MessageUrgentResult { InvalidUserIdList = ["ou_bad"] },
            });

        var result = await CreateTools().UrgentAppAsync(
            Args(("message_id", "om_1"), ("user_ids", new[] { "ou_ok", "ou_bad" })), CancellationToken.None);

        capturedMessageId.Should().Be("om_1");
        capturedUserIdType.Should().Be("open_id", "user_id_type 缺省补 open_id（工具面默认与平台默认同源）");
        captured!.UserIdList.Should().Equal("ou_ok", "ou_bad");

        using var document = JsonDocument.Parse(result.ToString()!);
        var root = document.RootElement;
        root.GetProperty("urgent").GetBoolean().Should().BeTrue();
        root.GetProperty("invalid_user_id_list")[0].GetString().Should().Be("ou_bad",
            "加急的目的就是送达——谁没收到必须显式给出");
    }

    [Fact]
    public async Task UrgentSms_ShouldForwardExplicitUserIdType()
    {
        string? capturedUserIdType = null;
        _messageClient
            .Setup(c => c.MessageUrgentSMSAsync(
                It.IsAny<string>(), It.IsAny<MessageUrgentRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, MessageUrgentRequest __, string userIdType, CancellationToken ___) => capturedUserIdType = userIdType)
            .ReturnsAsync(new FeishuApiResult<MessageUrgentResult>
            {
                Code = 0,
                Data = new MessageUrgentResult { InvalidUserIdList = [] },
            });

        var result = await CreateTools().UrgentSmsAsync(
            Args(
                ("message_id", "om_1"),
                ("user_ids", new[] { "on_1" }),
                ("user_id_type", "union_id")),
            CancellationToken.None);

        capturedUserIdType.Should().Be("union_id");
        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("invalid_user_id_list").GetArrayLength().Should().Be(0,
            "空数组也是有效信息——不得省略该字段让模型误读为未返回");
    }

    [Theory]
    [InlineData("chat_id")]
    [InlineData("email")]
    public async Task UrgentApp_WithIllegalUserIdType_ShouldReturnStructuredError(string userIdType)
    {
        var result = await CreateTools().UrgentAppAsync(
            Args(("message_id", "om_1"), ("user_ids", new[] { "ou_1" }), ("user_id_type", userIdType)),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] im.urgent_app");
        result.ToString().Should().Contain("open_id/user_id/union_id", "闭集校验必须给出可接受的取值");
        _messageClient.Verify(
            c => c.MessageUrgentAppAsync(
                It.IsAny<string>(), It.IsAny<MessageUrgentRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UrgentSms_DryRun_ShouldNotTouchDownstream()
    {
        var result = await CreateTools().UrgentSmsAsync(
            Args(("message_id", "om_1"), ("user_ids", new[] { "ou_1" }), ("dry_run", true)),
            CancellationToken.None);

        result.ToString().Should().StartWith("[dry_run]");
        _messageClient.Verify(
            c => c.MessageUrgentSMSAsync(
                It.IsAny<string>(), It.IsAny<MessageUrgentRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UrgentSms_MissingUserIds_ShouldReturnStructuredError()
    {
        var result = await CreateTools().UrgentSmsAsync(
            Args(("message_id", "om_1")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] im.urgent_sms");
        result.ToString().Should().Contain("user_ids");
    }
}
