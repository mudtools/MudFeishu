// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Events;
using Mud.Feishu.Abstractions;
using Mud.Feishu.AI.Conversations;

namespace Mud.Feishu.AI.Tests.Events;

/// <summary>
/// P4-4（outbox / 交付态补发）：回复在下发<b>之前</b>随会话落盘；下发改失败 ⇒ 幂等回滚 ⇒
/// 重投递时<b>补发同一份文本</b>而不是重跑模型。
/// </summary>
/// <remarks>
/// 治理目标不是 exactly-once（做不到），而是把 R2-8 取舍的代价从
/// 「重投递重跑模型——多一次计费 + 历史内部多一轮」降为「补发同一份文本」。
/// </remarks>
public class ConversationalFeishuEventHandlerOutboxTests
{
    private sealed class DemoEvent : IEventResult
    {
        public string MessageId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>可编排下发成败的处理器桩：记录每次实际下发的文本。</summary>
    private sealed class FlakyReplyHandler(
        FeishuAgent agent,
        IFeishuEventDeduplicator deduplicator,
        MemoryConversationStore store,
        int failTimes)
        : ConversationalFeishuEventHandler<DemoEvent>(agent, deduplicator, NullLogger.Instance)
    {
        private int _failuresLeft = failTimes;

        internal List<string> Delivered { get; } = [];

        protected override Task<ConversationRequest> BuildRequestAsync(DemoEvent eventData, CancellationToken cancellationToken)
            => Task.FromResult(new ConversationRequest(
                AppKey: "app-a",
                Scope: ConversationScope.Group(),
                SubjectId: "oc_1",
                SenderId: "ou_sender",
                MessageId: eventData.MessageId,
                MentionedText: eventData.Text));

        protected override Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
        {
            if (_failuresLeft > 0)
            {
                _failuresLeft--;
                throw new InvalidOperationException("飞书下发失败（模拟）");
            }

            Delivered.Add(responseText);
            return Task.CompletedTask;
        }

        protected override Task<string> AssembleUserMessageAsync(ConversationRequest request, CancellationToken cancellationToken)
            => Task.FromResult("用户消息");
    }

    private static Mock<IFeishuEventDeduplicator> CreateDeduplicator()
    {
        var deduplicator = new Mock<IFeishuEventDeduplicator>();
        deduplicator.Setup(d => d.TryMarkAsProcessingAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false });
        deduplicator.Setup(d => d.MarkAsCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        deduplicator.Setup(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return deduplicator;
    }

    private static Mock<IChatClient> CreateClient(params string[] replies)
    {
        var client = new Mock<IChatClient>();
        var queue = new Queue<string>(replies);
        client
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ChatResponse(new ChatMessage(ChatRole.Assistant, queue.Count > 0 ? queue.Dequeue() : "兜底回答")));
        return client;
    }

    private static EventData DemoEventData(string eventId, string messageId = "om_1") => new()
    {
        EventId = eventId,
        Event = new DemoEvent { MessageId = messageId, Text = "帮我查表" },
    };

    [Fact]
    public async Task HandleAsync_ShouldReplayStoredReply_WithoutRerunningModel_WhenDeliveryFailedThenRedelivered()
    {
        var store = new MemoryConversationStore();
        var client = CreateClient("第一版回答", "第二版回答（不应出现）");
        var agent = new FeishuAgent(client.Object, new FeishuAgentOptions { Instructions = "x" }, store);
        var handler = new FlakyReplyHandler(agent, CreateDeduplicator().Object, store, failTimes: 1);

        // 第一轮：模型生成 → 落盘（含待补发）→ 下发失败 ⇒ 异常（幂等回滚）
        var first = async () => await handler.HandleAsync(DemoEventData("evt-outbox-1"), default);
        await first.Should().ThrowAsync<InvalidOperationException>("下发失败必须继续向上传播（幂等回滚，既有语义不变）");

        // 第二轮：同一事件重投递 ⇒ 补发已落盘的第一版，不得再调模型
        await handler.HandleAsync(DemoEventData("evt-outbox-1"), default);

        handler.Delivered.Should().ContainSingle()
            .Which.Should().Be("第一版回答",
                "补发必须用已落盘的同一份文本——重跑模型会多一次计费，还会往历史里多加一轮（R2-8 的代价）");
        client.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once, "整个『失败 + 重投递』过程只能调用一次模型");
    }

    [Fact]
    public async Task HandleAsync_ShouldDropStalePendingReply_WhenDifferentTurnArrives()
    {
        var store = new MemoryConversationStore();
        var client = CreateClient("上一轮回答", "本轮回答");
        var agent = new FeishuAgent(client.Object, new FeishuAgentOptions { Instructions = "x" }, store);
        var handler = new FlakyReplyHandler(agent, CreateDeduplicator().Object, store, failTimes: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(DemoEventData("evt-outbox-2", messageId: "om_1"), default));

        // 用户发了**新消息**（不同 MessageId）：待补发条目属于上一轮 ⇒ 不得补发（答非所问）。
        await handler.HandleAsync(DemoEventData("evt-outbox-2b", messageId: "om_2"), default);

        handler.Delivered.Should().ContainSingle()
            .Which.Should().Be("本轮回答",
                "轮次不匹配的待补发条目必须丢弃——补发上一轮的回复属于答非所问");
        client.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2), "新轮次必须正常走模型");
    }

    [Fact]
    public async Task HandleAsync_ShouldClearPendingReply_AfterSuccessfulDelivery()
    {
        var store = new MemoryConversationStore();
        var client = CreateClient("第一版回答", "第二版回答");
        var agent = new FeishuAgent(client.Object, new FeishuAgentOptions { Instructions = "x" }, store);
        var handler = new FlakyReplyHandler(agent, CreateDeduplicator().Object, store, failTimes: 0);

        await handler.HandleAsync(DemoEventData("evt-outbox-3"), default);
        await handler.HandleAsync(DemoEventData("evt-outbox-3"), default);

        handler.Delivered.Should().Equal(
            new[] { "第一版回答", "第二版回答" },
            "送达成功后必须摘除待补发条目——否则下一轮会被误当成『未送达』而补发旧文本");
    }
}
