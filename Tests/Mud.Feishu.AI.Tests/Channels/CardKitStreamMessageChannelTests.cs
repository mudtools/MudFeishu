// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.DataModels.CardElements;
using Mud.Feishu.DataModels.Cards;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.Tests.Channels;

/// <summary>
/// CardKit 会话内流式通道测试（F-14）：协议三步（建实体→气泡投放→sequence 流式更新）、
/// sequence 严格递增、失败隔离、群聊/单聊通用、租户上下文切换、终态后状态归零。
/// </summary>
/// <remarks>
/// 分层说明：本测试在 SDK 接口层断言（<c>StreamUpdateCardTextByIdAsync</c> 即 cardkit
/// <c>PUT /cards/{card_id}/elements/{element_id}/content</c> 端点的强类型绑定——路由与 HTTP method
/// 由 SDK 生成器契约锁定）；「实际 HTTP path」的底层断言属 SDK 层职责，此处锁定的是
/// <b>AI 通道调用了 cardkit 端点而非 app_feed_card</b>（接错通道在 mock 层面的暴露方式 = 方法绑定断言）。
/// </remarks>
public class CardKitStreamMessageChannelTests
{
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

    private CardKitStreamMessageChannel CreateChannel(int chunkLength = 10, TimeSpan? interval = null)
        => new(
            _cardClient.Object,
            _elementsClient.Object,
            _messageClient.Object,
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test", MaxStreamChunkLength = chunkLength }),
            NullLogger<CardKitStreamMessageChannel>.Instance,
            interval ?? TimeSpan.Zero);

    private void SetupCreateOk(string cardId = "card_1")
        => _cardClient
            .Setup(c => c.CreateCardAsync(It.IsAny<CreateCardRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<CreateCardResult>
            {
                Code = 0,
                Data = new CreateCardResult { CardId = cardId },
            });

    private void SetupSendOk()
        => _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_card_1" },
            });

    private void SetupStreamUpdateOk()
        => _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

    [Fact]
    public async Task Begin_ShouldCreateStreamingCardAndDeliverBubble_ThenReturnCardId()
    {
        SetupCreateOk("card_stream_1");
        SetupSendOk();
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

        var cardId = await CreateChannel().BeginAsync("appA", "oc_group_chat");

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
        capturedReceiveIdType.Should().Be("chat_id");
        using var contentDoc = JsonDocument.Parse(capturedSend.Content!);
        contentDoc.RootElement.GetProperty("type").GetString().Should().Be("card", "官方「共享卡片实体」content 形态");
        contentDoc.RootElement.GetProperty("data").GetProperty("card_id").GetString().Should().Be("card_stream_1");
    }

    [Fact]
    public async Task Begin_ShouldThrow_WhenCreateFails_SoChainCanFallback()
    {
        _cardClient
            .Setup(c => c.CreateCardAsync(It.IsAny<CreateCardRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<CreateCardResult> { Code = 99991663, Msg = "cardkit:card:write 未开通" });

        var act = async () => await CreateChannel().BeginAsync("appA", "oc_1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*99991663*", "Begin 失败向上抛——降级链尝试编辑通道 / 事件处理器回退非流式");
    }

    [Fact]
    public async Task WriteStream_AndFlush_ShouldPushCumulativeTextWithStrictlyIncreasingSequence()
    {
        SetupCreateOk();
        SetupSendOk();
        SetupStreamUpdateOk();
        var updates = new List<(string CardId, string ElementId, string Content, int Sequence)>();
        _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string cardId, string elementId, StreamUpdateTextRequest request, CancellationToken _)
                => updates.Add((cardId, elementId, request.Content, request.Sequence)))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var channel = CreateChannel(chunkLength: 10);
        await channel.BeginAsync("appA", "oc_1");
        await channel.WriteStreamAsync("appA", "oc_1", "card_1", "0123456789", CancellationToken.None);
        await channel.WriteStreamAsync("appA", "oc_1", "card_1", "abc", CancellationToken.None);
        await channel.FlushAsync("appA", "oc_1", "card_1", CancellationToken.None);

        updates.Should().HaveCount(3, "达到分片阈值更新两次，Flush 落地最终全文（与编辑通道缓冲语义同构）");
        updates.Should().OnlyContain(u => u.CardId == "card_1" && u.ElementId == "stream_md",
            "更新须锚定 Begin 创建的卡片实体与流式正文元素");
        updates[0].Content.Should().Be("0123456789");
        updates[1].Content.Should().Be("0123456789abc");
        updates[2].Content.Should().Be("0123456789abc");
        updates.Select(static u => u.Sequence).Should().BeInAscendingOrder(
            "sequence 必须严格递增（飞书 300317 拒绝回退/重复号）");
        updates.Select(static u => u.Sequence).Should().OnlyHaveUniqueItems(
            "并发/乱序 chunk 不得产生重复 sequence");
        updates[0].Sequence.Should().BeGreaterThanOrEqualTo(1, "sequence 从 1 起（飞书要求 int32 正整数）");
    }

    [Fact]
    public async Task Sequence_ShouldRestartPerCard_WhenNewStreamBegins()
    {
        SetupCreateOk("card_a");
        SetupCreateOk("card_b");
        SetupSendOk();
        SetupStreamUpdateOk();
        var sequences = new List<(string CardId, int Sequence)>();
        _elementsClient
            .Setup(c => c.StreamUpdateCardTextByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamUpdateTextRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string cardId, string _, StreamUpdateTextRequest request, CancellationToken _)
                => sequences.Add((cardId, request.Sequence)))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var channel = CreateChannel(chunkLength: 1);
        var first = await channel.BeginAsync("appA", "oc_1");
        await channel.WriteStreamAsync("appA", "oc_1", first, "x", CancellationToken.None);
        await channel.FlushAsync("appA", "oc_1", first, CancellationToken.None);
        var second = await channel.BeginAsync("appA", "oc_1");
        await channel.WriteStreamAsync("appA", "oc_1", second, "y", CancellationToken.None);
        await channel.FlushAsync("appA", "oc_1", second, CancellationToken.None);

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
        await channel.BeginAsync("appA", "oc_1");

        var act = async () => await channel.WriteStreamAsync("appA", "oc_1", "card_1", "abc", CancellationToken.None);
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
        await channel.BeginAsync("appA", "oc_1");
        await channel.WriteStreamAsync("appA", "oc_1", "card_1", "abc", CancellationToken.None);

        scopeLog.Should().OnlyContain(s => s == "scope:appA", "每次下游调用前经 BeginScope 切换租户上下文（TMA2-20）");
        scopeLog.Should().HaveCountGreaterThanOrEqualTo(3, "建实体 + 气泡投放 + 流式更新各切换一次");
    }

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
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var channel = CreateChannel(chunkLength: 1, interval: BufferedMessageChannel.MinUpdateInterval);
        await channel.BeginAsync("appA", "oc_1");
        await channel.WriteStreamAsync("appA", "oc_1", "card_1", "a", CancellationToken.None);
        await channel.WriteStreamAsync("appA", "oc_1", "card_1", "b", CancellationToken.None);

        updateCount.Should().Be(1, "两次下游更新间隔 < 800ms 时继续缓冲（速率自适应钳制）");

        await channel.FlushAsync("appA", "oc_1", "card_1", CancellationToken.None);
        updateCount.Should().Be(2, "Flush 终态无条件落地");
    }
}