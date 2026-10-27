// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.DataModels.CardElements;
using Mud.Feishu.DataModels.Cards;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.Tests.Channels;

/// <summary>
/// CardKit 会话内流式通道测试（F-14）：协议三步（建实体→气泡投放→sequence 流式更新）、
/// 目标语义（chatId / 单聊 open_id）、sequence 严格递增、同文去重、失败隔离、终态可观测、
/// 租户上下文切换、终态后状态归零与迟到写入不泄漏。
/// </summary>
/// <remarks>
/// 分层说明：本测试在 SDK 接口层断言（<c>StreamUpdateCardTextByIdAsync</c> 即 cardkit
/// <c>PUT /cards/{card_id}/elements/{element_id}/content</c> 端点的强类型绑定）；「实际 HTTP path」
/// 由 <c>CardKitRouteContractGuards</c> 机械锁定，两处合起来覆盖「调了哪个端点」与「路由没漂移」。
/// <para>
/// <b>成功桩必须带 Data</b>：<c>FeishuApiResultReader</c> 把 <c>Data == null</c> 判为失败，
/// 只用 <c>Code = 0</c> 作桩会让所有「成功路径」断言实际跑在失败分支上（假绿）。
/// </para>
/// </remarks>
public class CardKitStreamMessageChannelTests
{
    private const string AppKey = "appA";

    private readonly Mock<Mud.Feishu.IFeishuTenantV1Card> _cardClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1CardElements> _elementsClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1Message> _messageClient = new();
    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();

    public CardKitStreamMessageChannelTests()
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Returns(new DisposableScope());
    }

    private sealed class DisposableScope : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private CardKitStreamMessageChannel CreateChannel(
        int chunkLength = 10,
        TimeSpan? interval = null,
        ILogger<CardKitStreamMessageChannel>? logger = null)
        => new(
            _cardClient.Object,
            _elementsClient.Object,
            _messageClient.Object,
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test", MaxStreamChunkLength = chunkLength }),
            logger ?? NullLogger<CardKitStreamMessageChannel>.Instance,
            interval ?? TimeSpan.Zero);

    private void SetupCreateOk(string cardId = "card_1")
        => _cardClient
            .Setup(c => c.CreateCardAsync(It.IsAny<CreateCardRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<CreateCardResult>
            {
                Code = 0,
                Data = new CreateCardResult { CardId = cardId },
            });

    /// <summary>按调用次序依次返回不同的 card_id（Moq 的多次 Setup 是「后者覆盖前者」，不能用于序列）。</summary>
    private void SetupCreateSequence(params string[] cardIds)
    {
        var queue = new Queue<string>(cardIds);
        _cardClient
            .Setup(c => c.CreateCardAsync(It.IsAny<CreateCardRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new FeishuApiResult<CreateCardResult>
            {
                Code = 0,
                Data = new CreateCardResult { CardId = queue.Dequeue() },
            });
    }

    private void SetupSendOk()
        => _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_card_1" },
            });

    /// <summary>流式更新成功桩（<b>必须带 Data</b>：空 Data 会被解包器判为失败）。</summary>
    private void SetupStreamUpdateOk()
        => _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0, Data = new object() });

    private static int DictionaryCount(object instance, string fieldName)
        => ((ICollection)instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
            .GetValue(instance)!).Count;

    [Fact]
    public async Task Begin_ShouldCreateStreamingCardAndDeliverBubble_ThenReturnCardId()
    {
        CreateCardRequest? capturedCreate = null;
        _cardClient
            .Setup(c => c.CreateCardAsync(It.IsAny<CreateCardRequest>(), It.IsAny<CancellationToken>()))
            .Callback((CreateCardRequest request, CancellationToken _) => capturedCreate = request)
            .ReturnsAsync(new FeishuApiResult<CreateCardResult>
            {
                Code = 0,
                Data = new CreateCardResult { CardId = "card_stream_1" },
            });
        SendMessageRequest? capturedSend = null;
        string? capturedReceiveIdType = null;
        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((SendMessageRequest request, string receiveIdType, CancellationToken _) =>
            {
                capturedSend = request;
                capturedReceiveIdType = receiveIdType;
            })
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_card_1" },
            });

        var cardId = await CreateChannel().BeginAsync(AppKey, "oc_group_chat");

        cardId.Should().Be("card_stream_1");
        capturedCreate.Should().NotBeNull();
        capturedCreate!.Type.Should().Be("card_json", "官方 cardkit 建实体形态（type=card_json + data=卡片 JSON 字符串）");
        using var cardDoc = JsonDocument.Parse(capturedCreate.Data);
        cardDoc.RootElement.GetProperty("schema").GetString().Should().Be("2.0", "流式文本更新仅支持卡片 JSON 2.0 结构");
        cardDoc.RootElement.GetProperty("config").GetProperty("streaming_mode").GetBoolean().Should().BeTrue(
            "streaming_mode=true 是流式更新接口的前置条件（否则飞书 300309 拒绝）");
        var element = cardDoc.RootElement.GetProperty("body").GetProperty("elements")[0];
        element.GetProperty("tag").GetString().Should().Be("markdown");
        element.GetProperty("element_id").GetString().Should().Be(CardKitStreamMessageChannel.StreamElementId);

        capturedSend.Should().NotBeNull();
        capturedSend!.MsgType.Should().Be("interactive", "卡片实体经会话气泡投放（群聊/单聊通用）");
        capturedSend.ReceiveId.Should().Be("oc_group_chat", "流式目标 = 会话 chatId（群聊不再降级编辑通道）");
        capturedReceiveIdType.Should().Be("chat_id", "无流式环境量时沿用 chat_id 语义（外部直调通道）");
        using var contentDoc = JsonDocument.Parse(capturedSend.Content!);
        contentDoc.RootElement.GetProperty("type").GetString().Should().Be("card", "官方「共享卡片实体」content 形态");
        contentDoc.RootElement.GetProperty("data").GetProperty("card_id").GetString().Should().Be("card_stream_1");
    }

    /// <summary>
    /// 业务事件（审批 / 表格变更）的单聊流式：目标 = 操作人 open_id，投放用 <c>receive_id_type=open_id</c>。
    /// </summary>
    /// <remarks>
    /// 接收方类型由<b>目标 ID 形态</b>判定（<c>ou_</c> ⇒ open_id，其余 ⇒ chat_id），与
    /// <c>ResolveStreamTarget</c> 的准入形态同源；故不依赖管线是否保留环境量里的 <c>ChatId</c>
    /// （跨包隐式耦合是这版最容易被改坏的地方，见上一个用例的 <c>chat_id</c> 分支）。
    /// </remarks>
    [Fact]
    public async Task Begin_ShouldDeliverToOpenId_WhenStreamTargetIsP2pOperator()
    {
        SetupCreateOk();
        string? capturedReceiveIdType = null;
        string? capturedReceiveId = null;
        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((SendMessageRequest request, string receiveIdType, CancellationToken _) =>
            {
                capturedReceiveId = request.ReceiveId;
                capturedReceiveIdType = receiveIdType;
            })
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_card_1" },
            });

        var request = new ConversationRequest(
            AppKey: AppKey, Scope: ConversationScope.P2P(), SubjectId: "ou_operator",
            SenderId: "ou_operator", MessageId: string.Empty, MentionedText: "event",
            EventKey: Mud.Feishu.AI.Events.FeishuEventKeys.ApprovalTask);

        var channel = CreateChannel();
        channel.ResolveStreamTarget(request).Should().Be("ou_operator", "单聊业务事件的目标是操作人 open_id");

        await channel.BeginAsync(AppKey, "ou_operator");

        capturedReceiveId.Should().Be("ou_operator");
        capturedReceiveIdType.Should().Be("open_id", "单聊气泡按 open_id 投放（会话 chat_id 不存在）");
    }

    [Fact]
    public async Task Begin_ShouldThrow_WhenCreateFails_SoChainCanFallback()
    {
        _cardClient
            .Setup(c => c.CreateCardAsync(It.IsAny<CreateCardRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<CreateCardResult> { Code = 99991663, Msg = "cardkit:card:write 未开通" });

        var act = async () => await CreateChannel().BeginAsync(AppKey, "oc_1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*99991663*", "Begin 失败向上抛——降级链尝试编辑通道 / 事件处理器回退非流式");
    }

    /// <summary>实体已建、气泡未出 ⇒ 平台残留实体：必须计数（无删除接口，只能等 14 天过期）。</summary>
    [Fact]
    public async Task Begin_ShouldCountOrphanedCard_WhenBubbleSendFails()
    {
        SetupCreateOk();
        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 230001, Msg = "invalid receive_id" });

        var reasons = await CaptureDegradedAsync(AppKey, async () =>
        {
            try
            {
                await CreateChannel().BeginAsync(AppKey, "oc_1");
            }
            catch (InvalidOperationException)
            {
                // 预期：Begin 失败向上抛（降级链回退），指标在抛出前已记。
            }
        });

        reasons.Should().Contain(
            FeishuMetrics.DegradedReasons.ChannelBeginOrphaned,
            "卡片实体已创建而气泡未投放 = 平台残留资源，必须与「什么都没创建」的失败区分");
    }

    [Fact]
    public async Task WriteStream_AndFlush_ShouldPushCumulativeTextWithStrictlyIncreasingSequence()
    {
        SetupCreateOk();
        SetupSendOk();
        var updates = new List<(string CardId, string ElementId, string Content, int Sequence)>();
        _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string cardId, string elementId, StreamUpdateTextRequest request, CancellationToken _)
                => updates.Add((cardId, elementId, request.Content, request.Sequence)))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0, Data = new object() });

        var logger = new RecordingLogger();
        var channel = CreateChannel(chunkLength: 10, logger: logger);
        await channel.BeginAsync(AppKey, "oc_1");
        await channel.WriteStreamAsync(AppKey, "oc_1", "card_1", "0123456789", CancellationToken.None);
        await channel.WriteStreamAsync(AppKey, "oc_1", "card_1", "abc", CancellationToken.None);
        await channel.FlushAsync(AppKey, "oc_1", "card_1", CancellationToken.None);

        logger.Entries.Should().BeEmpty(
            "成功路径不得产生任何 Warning/Error——桩缺 Data 时全部落失败分支，这条断言是「成功路径真被覆盖」的判据");
        updates.Should().HaveCount(2, "两次达阈值推送；Flush 的终态文本与上一次相同 ⇒ 同文去重不再多推一次");
        updates.Should().OnlyContain(u => u.CardId == "card_1" && u.ElementId == "stream_md",
            "更新须锚定 Begin 创建的卡片实体与流式正文元素");
        updates[0].Content.Should().Be("0123456789");
        updates[1].Content.Should().Be("0123456789abc");
        updates.Select(static u => u.Sequence).Should().BeInAscendingOrder(
            "sequence 必须严格递增（飞书 300317 拒绝回退/重复号）");
        updates.Select(static u => u.Sequence).Should().OnlyHaveUniqueItems(
            "并发/乱序 chunk 不得产生重复 sequence");
        updates[0].Sequence.Should().BeGreaterThanOrEqualTo(1, "sequence 从 1 起（飞书要求 int32 正整数）");
    }

    [Fact]
    public async Task Sequence_ShouldRestartPerCard_WhenNewStreamBegins()
    {
        SetupCreateSequence("card_a", "card_b");
        SetupSendOk();
        var sequences = new List<(string CardId, int Sequence)>();
        _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string cardId, string _, StreamUpdateTextRequest request, CancellationToken _)
                => sequences.Add((cardId, request.Sequence)))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0, Data = new object() });

        var channel = CreateChannel(chunkLength: 1);
        var first = await channel.BeginAsync(AppKey, "oc_1");
        await channel.WriteStreamAsync(AppKey, "oc_1", first, "x", CancellationToken.None);
        await channel.FlushAsync(AppKey, "oc_1", first, CancellationToken.None);
        var second = await channel.BeginAsync(AppKey, "oc_1");
        await channel.WriteStreamAsync(AppKey, "oc_1", second, "y", CancellationToken.None);
        await channel.FlushAsync(AppKey, "oc_1", second, CancellationToken.None);

        first.Should().Be("card_a");
        second.Should().Be("card_b");
        sequences.Should().Contain(s => s.CardId == "card_a" && s.Sequence == 1, "每张卡片 sequence 独立从 1 递增");
        sequences.Should().Contain(s => s.CardId == "card_b" && s.Sequence == 1,
            "新卡片的 sequence 不受上一张卡片累计影响（per-cardId 计数）");
    }

    [Fact]
    public async Task UpdateFailure_ShouldNotThrow_FailureIsolation()
    {
        SetupCreateOk();
        SetupSendOk();
        _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("流式更新接口 5xx"));

        var channel = CreateChannel(chunkLength: 1);
        await channel.BeginAsync(AppKey, "oc_1");

        var act = async () => await channel.WriteStreamAsync(AppKey, "oc_1", "card_1", "abc", CancellationToken.None);
        await act.Should().NotThrowAsync("单次更新失败不中断模型流（失败隔离，接口契约）");
    }

    [Fact]
    public async Task Channel_ShouldSwitchTenantScope_PerApiCall()
    {
        SetupCreateOk();
        SetupSendOk();
        SetupStreamUpdateOk();
        var scopeLog = new List<string>();
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Callback((string appKey) => scopeLog.Add($"scope:{appKey}"))
            .Returns(new DisposableScope());

        var channel = CreateChannel(chunkLength: 1);
        await channel.BeginAsync(AppKey, "oc_1");
        await channel.WriteStreamAsync(AppKey, "oc_1", "card_1", "abc", CancellationToken.None);

        scopeLog.Should().OnlyContain(s => s == "scope:appA", "每次下游调用前经 BeginScope 切换租户上下文（TMA2-20）");
        scopeLog.Should().HaveCount(2, "Begin 一次作用域覆盖「建实体 + 气泡投放」，流式更新各自一次（作用域不跨 await 边界共享）");
    }

    /// <summary>
    /// 速率钳制：注入 1 小时间隔做<b>确定性</b>钳制，不依赖「两次调用落在 800ms 内」这种墙钟假设。
    /// </summary>
    [Fact]
    public async Task Update_ShouldApplyMinInterval_WhenRateClamped()
    {
        SetupCreateOk();
        SetupSendOk();
        SetupStreamUpdateOk();
        var updateCount = 0;
        _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => updateCount++)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0, Data = new object() });

        var channel = CreateChannel(chunkLength: 1, interval: TimeSpan.FromHours(1));
        await channel.BeginAsync(AppKey, "oc_1");
        await channel.WriteStreamAsync(AppKey, "oc_1", "card_1", "a", CancellationToken.None);
        await channel.WriteStreamAsync(AppKey, "oc_1", "card_1", "b", CancellationToken.None);

        updateCount.Should().Be(1, "两次下游更新间隔远小于最小间隔时继续缓冲（速率自适应钳制，确定性注入）");

        await channel.FlushAsync(AppKey, "oc_1", "card_1", CancellationToken.None);
        updateCount.Should().Be(2, "Flush 终态无条件落地（不受最小间隔钳制）");
    }

    /// <summary>终态文本与最后一次成功推送相同 ⇒ 不再多打一次等价的 PUT（每轮省一次写入配额）。</summary>
    [Fact]
    public async Task Flush_ShouldSkipDownstream_WhenTextUnchanged()
    {
        SetupCreateOk();
        SetupSendOk();
        SetupStreamUpdateOk();
        var updateCount = 0;
        _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => updateCount++)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0, Data = new object() });

        var channel = CreateChannel(chunkLength: 1);
        await channel.BeginAsync(AppKey, "oc_1");
        await channel.WriteStreamAsync(AppKey, "oc_1", "card_1", "abc", CancellationToken.None);
        await channel.FlushAsync(AppKey, "oc_1", "card_1", CancellationToken.None);

        updateCount.Should().Be(1, "Flush 时全文与上次成功推送逐字节相同 ⇒ 跳过下游调用（内容已落地）");
    }

    /// <summary>终态之后的迟到写入必须被丢弃：不重放、不重建字典键（否则平台 300317 + 单例字典泄漏）。</summary>
    [Fact]
    public async Task LateWriteAfterFlush_ShouldSkipAndNotLeak()
    {
        SetupCreateOk();
        SetupSendOk();
        SetupStreamUpdateOk();
        var updateCount = 0;
        _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => updateCount++)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0, Data = new object() });

        var channel = CreateChannel(chunkLength: 1);
        var cardId = await channel.BeginAsync(AppKey, "oc_1");
        await channel.WriteStreamAsync(AppKey, "oc_1", cardId, "abc", CancellationToken.None);
        await channel.FlushAsync(AppKey, "oc_1", cardId, CancellationToken.None);
        var afterFlush = updateCount;

        await channel.WriteStreamAsync(AppKey, "oc_1", cardId, "迟到的增量", CancellationToken.None);

        updateCount.Should().Be(afterFlush, "终态后的写入不得再次调用下游（sequence 已随状态移除，重放会被 300317 拒绝）");
        DictionaryCount(channel, "_streams").Should().Be(0, "迟到写入不得让 per-cardId 状态重新生长（Singleton 字典泄漏）");
    }

    /// <summary>终态未落地必须可观测：Error 日志 + 独立降级原因（<c>ChannelTerminalFailed</c>）。</summary>
    [Fact]
    public async Task TerminalUpdateFailure_ShouldEmitErrorAndMetric()
    {
        SetupCreateOk();
        SetupSendOk();
        var logger = new RecordingLogger();
        _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 230020, Msg = "card entity expired" });

        var channel = CreateChannel(chunkLength: 1, logger: logger);
        var cardId = await channel.BeginAsync(AppKey, "oc_1");

        var reasons = await CaptureDegradedAsync(AppKey, async () =>
        {
            await channel.WriteStreamAsync(AppKey, "oc_1", cardId, "abc", CancellationToken.None);
            await channel.FlushAsync(AppKey, "oc_1", cardId, CancellationToken.None);
        });

        logger.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("终态未落地", StringComparison.Ordinal),
            "终态失败必须是 Error（用户看到的是半截内容），不能与中途单次失败同级别");
        reasons.Should().Contain(FeishuMetrics.DegradedReasons.ChannelTerminalFailed,
            "终态失败必须有独立指标原因（与 channel_update_failed 区分：那次是「少一次增量」而非「永远看不到完整回答」）");
        DictionaryCount(channel, "_streams").Should().Be(0, "终态后 per-cardId 状态必须归零");
    }

    /// <summary>锁定基类契约：终态失败<b>不得</b>抛（<c>FlushAsync</c> 为 sealed，抛出会打断回合管线）。</summary>
    [Fact]
    public async Task TerminalUpdateFailure_ShouldNotThrow()
    {
        SetupCreateOk();
        SetupSendOk();
        _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 230020, Msg = "card entity expired" });

        var channel = CreateChannel(chunkLength: 1);
        var cardId = await channel.BeginAsync(AppKey, "oc_1");

        var act = async () => await channel.FlushAsync(AppKey, "oc_1", cardId, CancellationToken.None);

        await act.Should().NotThrowAsync("产品侧只能可观测，不能把终态失败升级为异常（中立基类契约）");
    }

    /// <summary>流式目标解析（同步契约，不调平台 API）：会话 chatId 优先，单聊回退操作人 open_id。</summary>
    [Theory]
    [InlineData("oc_chat", "ou_user", false, "oc_chat")]
    [InlineData(null, "ou_user", false, "ou_user")]
    [InlineData(null, "cli_userid_without_prefix", false, null)]
    [InlineData(null, "ou_user", true, null)]
    public void ResolveStreamTarget_ShouldFollowChannelTargetSemantics(
        string? chatId, string senderId, bool isGroup, string? expected)
    {
        var request = new ConversationRequest(
            AppKey: AppKey,
            Scope: isGroup ? ConversationScope.Group() : ConversationScope.P2P(),
            SubjectId: chatId ?? senderId,
            SenderId: senderId,
            MessageId: "om_1",
            MentionedText: "hi",
            ChatId: chatId);

        CreateChannel().ResolveStreamTarget(request).Should().Be(expected);
    }

    /// <summary>在监听期内执行 <paramref name="emit"/>，只收本用例 app_key 的降级读数（指标是进程级通道）。</summary>
    private static async Task<List<string>> CaptureDegradedAsync(string appKey, Func<Task> emit)
    {
        var seen = new List<string>();

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Name == "feishu.agent.tool.degraded")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var owner = string.Empty;
            var reason = string.Empty;
            foreach (var tag in tags)
            {
                if (tag.Key == FeishuMetrics.Tags.AppKey)
                {
                    owner = tag.Value as string ?? string.Empty;
                }
                else if (tag.Key == FeishuMetrics.Tags.Reason)
                {
                    reason = tag.Value as string ?? string.Empty;
                }
            }

            if (string.Equals(owner, appKey, StringComparison.Ordinal))
            {
                seen.Add(reason);
            }
        });
        listener.Start();

        await emit().ConfigureAwait(false);

        return seen;
    }

    /// <summary>记录日志的替身（终态可观测断言用；零第三方依赖）。</summary>
    private sealed class RecordingLogger : ILogger<CardKitStreamMessageChannel>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
