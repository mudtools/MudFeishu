// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// 表情回复面（<c>im.list_reactions</c> / <c>im.batch_query_reactions</c> /
/// <c>im.add_reaction</c> / <c>im.delete_reaction</c>）的链路用例。
/// </summary>
/// <remarks>
/// <para>
/// 读面在 <see cref="ImTools"/>、写面在 <see cref="MessageWriteTools"/>（同一 SDK 客户端、
/// 同一 emoji 词表，故两侧合锁一份契约）。
/// </para>
/// <para>
/// <b>本域值得锁住的非显然事实</b>：① list 端点的 <c>reaction_type</c> 平台侧必填，工具层必须
/// 把「要全部表情」引导到 batch_query 而不是下发一个空值；② batch_query 把「计数」与「明细」
/// 分成两个数组返回，投影按 message_id 合并——合错即把 A 消息的明细算到 B 头上；
/// ③ 平台<b>不去重</b>，所以 add 刻意不提供幂等键，回执必须带回 <c>reaction_id</c>
/// 否则用户无法撤回（delete 只认实例 ID）。
/// </para>
/// </remarks>
public class ImReactionToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1Message> _messageClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember> _chatMemberClient = new();

    private ImTools CreateReadTools()
        => new(_messageClient.Object, _chatMemberClient.Object, Options.Create(new FeishuAgentOptions { Instructions = "test" }));

    private MessageWriteTools CreateWriteTools() => new(_messageClient.Object);

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    // ───────────────────── 契约事实（method / path / 身份 / 风险） ─────────────────────

    [Theory]
    [InlineData("im.list_reactions", "GET", "/open-apis/im/v1/messages/{message_id}/reactions", "read")]
    [InlineData("im.batch_query_reactions", "POST", "/open-apis/im/v1/messages/reactions/batch_query", "read")]
    [InlineData("im.add_reaction", "POST", "/open-apis/im/v1/messages/{message_id}/reactions", "write")]
    [InlineData("im.delete_reaction", "DELETE", "/open-apis/im/v1/messages/{message_id}/reactions/{reaction_id}", "high-risk-write")]
    public void Contracts_ShouldCarrySdkMethodAndRoute(string toolName, string httpMethod, string route, string risk)
    {
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be(httpMethod, "batch_query 是 POST（查询体带 message_ids 数组，不走 URL 拼接）");
        contract.Route.Should().Be(route);
        contract.Identity.Should().Be("tenant");
        contract.Risk.Should().Be(
            risk == "read" ? FeishuToolRisk.Read
                : risk == "write" ? FeishuToolRisk.Write
                : FeishuToolRisk.HighRiskWrite,
            "撤回表情命中危险词表，需宿主授权与 affordance 资产");
        contract.IsWrite.Should().Be(risk != "read");
        contract.RequiredScopes.Should().Contain("im:message");
    }

    // ───────────────────── im.list_reactions ─────────────────────

    [Fact]
    public async Task ListReactions_ShouldSendFixedPageSize_AndProjectReactionWhitelist()
    {
        string? capturedMessageId = null;
        string? capturedReactionType = null;
        var capturedPageSize = 0;
        string? capturedPageToken = "unset";
        string? capturedUserIdType = null;

        _messageClient
            .Setup(c => c.GetMessageReactionsPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string messageId, string reactionType, int pageSize, string? pageToken, string? userIdType, CancellationToken _) =>
            {
                capturedMessageId = messageId;
                capturedReactionType = reactionType;
                capturedPageSize = pageSize;
                capturedPageToken = pageToken;
                capturedUserIdType = userIdType;
            })
            .ReturnsAsync(new FeishuApiListResult<EmojiReactionResult>
            {
                Code = 0,
                Data = new ApiListResult<EmojiReactionResult>
                {
                    Items =
                    [
                        new EmojiReactionResult
                        {
                            ReactionId = "rc_1",
                            ReactionType = new ReactionType { EmojiType = "THUMBSUP" },
                            Operator = new ReactionOperator { OperatorId = "ou_1", OperatorType = "user" },
                            ActionTime = "1730000000",
                        },
                        new EmojiReactionResult { ReactionId = "rc_2" },
                    ],
                },
            });

        var text = (await CreateReadTools().ListReactionsAsync(
            Args(("message_id", "om_1"), ("reaction_type", "THUMBSUP"), ("user_id_type", "union_id")),
            CancellationToken.None)).ToString()!;

        capturedMessageId.Should().Be("om_1", "message_id 经 [Path] 下发");
        capturedReactionType.Should().Be("THUMBSUP", "reaction_type 经 [Query] 下发（平台必填）");
        capturedPageSize.Should().Be(PageSizes.ImReactions, "页大小由绑定层固定，模型不接触翻页参数");
        capturedPageToken.Should().BeNull("单页语义：首屏即全量，不代模型翻页");
        capturedUserIdType.Should().Be("union_id", "user_id_type 透传");

        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        root.GetProperty("returned_count").GetInt32().Should().Be(2,
            "本端点不透出 has_more，返回条数是模型唯一的『可能还有更多』信号");
        var first = root.GetProperty("items")[0];
        first.GetProperty("reaction_id").GetString().Should().Be("rc_1");
        first.GetProperty("emoji_type").GetString().Should().Be("THUMBSUP", "回复的表情取 reaction_type.emoji_type");
        first.GetProperty("operator_id").GetString().Should().Be("ou_1", "谁回的——PII 字段只到 ID 粒度");
        first.GetProperty("operator_type").GetString().Should().Be("user");
        first.GetProperty("action_time").GetString().Should().Be("1730000000");
    }

    /// <summary>
    /// 空白的必填值由<b>生成的解包层</b>判为「缺少必填参数」，先于执行器自身的空白检查——
    /// 这条顺序是真实的链路事实（执行器的文案是第二道防线，只在参数改为可选时才生效）。
    /// </summary>
    [Fact]
    public async Task ListReactions_WhenReactionTypeBlank_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateReadTools().ListReactionsAsync(
            Args(("message_id", "om_1"), ("reaction_type", "  ")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] im.list_reactions");
        text.Should().Contain("invalid_args");
        _messageClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ListReactions_WhenUserIdTypeNotInClosedSet_ShouldReject()
    {
        var text = (await CreateReadTools().ListReactionsAsync(
            Args(("message_id", "om_1"), ("reaction_type", "THUMBSUP"), ("user_id_type", "employee_id")),
            CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] im.list_reactions");
        text.Should().Contain("user_id_type 只能是");
        _messageClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ListReactions_WhenPlatformFails_ShouldReturnStructuredErrorWithCode()
    {
        _messageClient
            .Setup(c => c.GetMessageReactionsPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiListResult<EmojiReactionResult> { Code = 234001, Msg = "invalid message id" });

        var text = (await CreateReadTools().ListReactionsAsync(
            Args(("message_id", "om_missing"), ("reaction_type", "THUMBSUP")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] im.list_reactions");
        text.Should().Contain("234001", "平台错误码必须回填（F-10：模型据此自我纠正，而不是重试三次）");
    }

    // ───────────────────── im.batch_query_reactions ─────────────────────

    [Fact]
    public async Task BatchQueryReactions_ShouldTrimAndDedupeMessageIds_AndFixPerMessagePageSize()
    {
        BatchQueryMessageReactionsRequest? captured = null;

        _messageClient
            .Setup(c => c.BatchQueryMessageReactionsAsync(It.IsAny<BatchQueryMessageReactionsRequest>(), It.IsAny<CancellationToken>()))
            .Callback((BatchQueryMessageReactionsRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<BatchQueryMessageReactionsResult>
            {
                Code = 0,
                Data = new BatchQueryMessageReactionsResult
                {
                    SuccessMsgReactionCounts =
                    [
                        new MessageReactionCount
                        {
                            MessageId = "om_1",
                            ReactionCount = [new MessageReactionCountItem { ReactionType = "THUMBSUP", Count = 3 }],
                        },
                    ],
                    FailMsgReactionDetails = [new MessageReactionFailDetail { MessageId = "om_3", FailReason = "消息不可见" }],
                },
            });

        var text = (await CreateReadTools().BatchQueryReactionsAsync(
            Args(("message_ids", new[] { "om_1", " om_1 ", "om_2", "  " })), CancellationToken.None)).ToString()!;

        captured!.Queries.Should().HaveCount(2, "重复与空白项在本地消掉——平台按 queries 逐条返回，重复只会让同一份明细出现两次");
        captured.Queries!.Select(q => q.MessageId).Should().Equal("om_1", "om_2");
        captured.PageSizePerMessage.Should().Be(PageSizes.ImReactionsPerMessage);
        captured.ReactionType.Should().BeNull("省略 reaction_type 即查全部表情");

        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        root.GetProperty("items")[0].GetProperty("reactions")[0].GetProperty("count").GetInt32().Should().Be(3);
        root.GetProperty("items")[0].TryGetProperty("reaction_details", out _).Should().BeFalse(
            "默认只给计数——明细默认进上下文会烧掉整轮预算");
        root.GetProperty("failed")[0].GetProperty("message_id").GetString().Should().Be("om_3");
        root.GetProperty("failed")[0].GetProperty("reason").GetString().Should().Be("消息不可见",
            "不可见的消息必须逐条给出原因，否则模型以为『这条没表情』");
    }

    [Fact]
    public async Task BatchQueryReactions_WithDetails_ShouldMergeEachMessageOwnsOwnItems()
    {
        _messageClient
            .Setup(c => c.BatchQueryMessageReactionsAsync(It.IsAny<BatchQueryMessageReactionsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<BatchQueryMessageReactionsResult>
            {
                Code = 0,
                Data = new BatchQueryMessageReactionsResult
                {
                    SuccessMsgReactionCounts =
                    [
                        new MessageReactionCount { MessageId = "om_1", ReactionCount = [new MessageReactionCountItem { ReactionType = "THUMBSUP", Count = 1 }] },
                        new MessageReactionCount { MessageId = "om_2", ReactionCount = [] },
                    ],
                    SuccessMsgReactionDetails =
                    [
                        new MessageReactionDetail
                        {
                            MessageId = "om_1",
                            MessageReactionItems =
                            [
                                new MessageReactionItem
                                {
                                    ReactionId = "rc_1",
                                    ReactionType = "THUMBSUP",
                                    OperatorId = "ou_1",
                                    ActionTime = "1730000000",
                                },
                            ],
                        },
                    ],
                },
            });

        var text = (await CreateReadTools().BatchQueryReactionsAsync(
            Args(("message_ids", new[] { "om_1", "om_2" }), ("with_details", true), ("reaction_type", "THUMBSUP")),
            CancellationToken.None)).ToString()!;

        using var document = JsonDocument.Parse(text);
        var items = document.RootElement.GetProperty("items");
        items[0].GetProperty("reaction_details")[0].GetProperty("reaction_id").GetString().Should().Be("rc_1",
            "with_details 才能拿到 reaction_id——im.delete_reaction 的唯一入参来源");
        items[1].GetProperty("reaction_details").GetArrayLength().Should().Be(0,
            "明细按 message_id 归属：om_2 不得继承 om_1 的明细");
        items[1].GetProperty("reactions").GetArrayLength().Should().Be(0, "计数与明细是两个独立数组，缺一侧不得互相顶替");
    }

    [Fact]
    public async Task BatchQueryReactions_WhenAllMessageIdsBlank_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateReadTools().BatchQueryReactionsAsync(
            Args(("message_ids", new[] { " ", "" })), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] im.batch_query_reactions");
        text.Should().Contain("invalid_args", "全空白数组被归一化为空⇒解包层判缺少必填参数，不下发到平台");
        _messageClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BatchQueryReactions_WhenMoreThanFiftyMessages_ShouldReject_WithoutCallingDownstream()
    {
        var ids = Enumerable.Range(0, 51).Select(static i => $"om_{i}").ToArray();

        var text = (await CreateReadTools().BatchQueryReactionsAsync(
            Args(("message_ids", ids)), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] im.batch_query_reactions");
        text.Should().Contain("一次最多 50 条", "平台 queries 数组上限——超了整批失败，本地拦比让平台报错省一轮");
        _messageClient.VerifyNoOtherCalls();
    }

    // ───────────────────── im.add_reaction ─────────────────────

    [Fact]
    public async Task AddReaction_ShouldPostEmojiTypeBody_AndReturnReactionIdForUndo()
    {
        string? capturedMessageId = null;
        EmojiReactionRequest? captured = null;

        _messageClient
            .Setup(c => c.AddMessageReactionsAsync(It.IsAny<string>(), It.IsAny<EmojiReactionRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string messageId, EmojiReactionRequest request, CancellationToken _) =>
            {
                capturedMessageId = messageId;
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<EmojiReactionResult>
            {
                Code = 0,
                Data = new EmojiReactionResult
                {
                    ReactionId = "rc_new",
                    ReactionType = new ReactionType { EmojiType = "THUMBSUP" },
                    Operator = new ReactionOperator { OperatorId = "ou_bot", OperatorType = "app" },
                },
            });

        var text = (await CreateWriteTools().AddReactionAsync(
            Args(("message_id", "om_1"), ("emoji_type", "THUMBSUP")), CancellationToken.None)).ToString()!;

        capturedMessageId.Should().Be("om_1");
        captured!.ReactionType!.EmojiType.Should().Be("THUMBSUP", "请求体只带 reaction_type.emoji_type，无 uuid 类去重参数");
        text.Should().Contain("rc_new", "回执必须带回 reaction_id——否则这次表情永远撤不掉");
        text.Should().Contain("app");
    }

    [Fact]
    public async Task AddReaction_WhenEmojiTypeBlank_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateWriteTools().AddReactionAsync(
            Args(("message_id", "om_1"), ("emoji_type", "   ")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] im.add_reaction");
        text.Should().Contain("invalid_args", "空白 emoji_type 归一化为缺失，由解包层先拦下");
        _messageClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddReaction_DryRun_ShouldPreviewRoute_EmitNoIdempotencyKey_AndNotTouchDownstream()
    {
        var text = (await CreateWriteTools().AddReactionAsync(
            Args(("message_id", "om_1"), ("emoji_type", "THUMBSUP"), ("dry_run", true)), CancellationToken.None)).ToString()!;

        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/im/v1/messages/om_1/reactions");
        text.Should().NotContain("THUMBSUP", "预演摘要只回字段长度，不回表情名原文");
        _messageClient.VerifyNoOtherCalls();
    }

    // ───────────────────── im.delete_reaction ─────────────────────

    [Fact]
    public async Task DeleteReaction_ShouldDeleteTheSingleInstance_AndReturnReceipt()
    {
        string? capturedMessageId = null;
        string? capturedReactionId = null;

        _messageClient
            .Setup(c => c.DeleteMessageReactionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string messageId, string reactionId, CancellationToken _) =>
            {
                capturedMessageId = messageId;
                capturedReactionId = reactionId;
            })
            .ReturnsAsync(new FeishuApiResult<EmojiReactionResult>
            {
                Code = 0,
                Data = new EmojiReactionResult { ReactionId = "rc_1", ReactionType = new ReactionType { EmojiType = "THUMBSUP" } },
            });

        var text = (await CreateWriteTools().DeleteReactionAsync(
            Args(("message_id", "om_1"), ("reaction_id", "rc_1")), CancellationToken.None)).ToString()!;

        capturedMessageId.Should().Be("om_1");
        capturedReactionId.Should().Be("rc_1", "删除按实例 ID 定位（不是按表情名批量清空）");
        using var document = JsonDocument.Parse(text);
        document.RootElement.GetProperty("reaction_id").GetString().Should().Be("rc_1");
        document.RootElement.GetProperty("emoji_type").GetString().Should().Be("THUMBSUP");
    }

    [Fact]
    public async Task DeleteReaction_DryRun_ShouldPreviewDeleteRoute_AndNotTouchDownstream()
    {
        var text = (await CreateWriteTools().DeleteReactionAsync(
            Args(("message_id", "om_1"), ("reaction_id", "rc_1"), ("dry_run", true)), CancellationToken.None)).ToString()!;

        text.Should().StartWith("[dry_run]");
        text.Should().Contain("DELETE /open-apis/im/v1/messages/om_1/reactions/rc_1");
        _messageClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteReaction_WhenPlatformFails_ShouldReturnStructuredErrorWithCode()
    {
        _messageClient
            .Setup(c => c.DeleteMessageReactionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<EmojiReactionResult> { Code = 234015, Msg = "reaction not found" });

        var text = (await CreateWriteTools().DeleteReactionAsync(
            Args(("message_id", "om_1"), ("reaction_id", "rc_gone")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] im.delete_reaction");
        text.Should().Contain("234015", "高危写失败必须给码——『没找到』与『无权删』的后续处置完全不同");
    }
}
