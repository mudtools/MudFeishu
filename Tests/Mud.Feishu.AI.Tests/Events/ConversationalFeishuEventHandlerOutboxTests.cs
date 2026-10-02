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

    /// <summary>
    /// R5-4：<c>pending_reply*</c> 载荷形状损坏时必须按 <b>miss</b> 处理——不逃逸、不毒化事件循环。
    /// </summary>
    /// <remarks>
    /// 状态袋的值是<b>惰性</b>反序列化的：内层载荷损坏只在首次类型化读取时抛，而该读取就发生在
    /// <c>TryTakePendingReply</c> 内。若异常逃出该守护区 ⇒ 幂等回滚 ⇒ 重投递同点再抛 ⇒ 事件永久毒化。
    /// 本用例把已落盘的 <c>feishu.agent.pending_reply</c> 改成 JSON <b>数字</b>（形状不符），
    /// 断言事件仍正常收尾（坏值按 miss，走模型）。
    /// </remarks>
    [Fact]
    public async Task TryTakePendingReply_ShouldTreatAnyCorruptionAsMiss_ExceptCancellation()
    {
        var store = new MemoryConversationStore();
        var client = CreateClient("第一版回答", "第二版回答");
        var agent = new FeishuAgent(client.Object, new FeishuAgentOptions { Instructions = "x" }, store);
        var handler = new FlakyReplyHandler(agent, CreateDeduplicator().Object, store, failTimes: 1);

        // 第一轮：下发失败 ⇒ 会话已落盘（含待补发条目）
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(DemoEventData("evt-outbox-corrupt", messageId: "om_1"), default));

        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");
        var stored = await store.GetAsync(key);
        stored.Should().NotBeNullOrEmpty("前置：会话必须已落盘（否则本用例验证的是空路径）");

        TryPoisonStringValue(stored!, "feishu.agent.pending_reply", out var poisonedPayload)
            .Should().BeTrue("前置：必须真的改坏了 pending_reply 载荷（否则本用例是假绿）");
        await store.SaveAsync(key, poisonedPayload!);

        // 第二轮：同一事件重投递 ⇒ 坏载荷按 miss，事件正常收尾（必须调用模型，且不得逃逸异常）
        var act = async () => await handler.HandleAsync(DemoEventData("evt-outbox-corrupt", messageId: "om_1"), default);

        await act.Should().NotThrowAsync("损坏的待补发条目不得毒化事件循环（坏值按 miss，R2-2/R4-7 同一哲学）");
        handler.Delivered.Should().ContainSingle().Which.Should().Be("第二版回答",
            "坏载荷按 miss ⇒ 正常走模型（而不是补发不可读的旧条目）");
    }

    /// <summary>把 JSON 中指定键的值改成数字形态（破坏「字符串」契约），并输出改写后的 JSON。</summary>
    private static bool TryPoisonStringValue(string json, string key, out string? poisonedJson)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(json);
        var hit = Poison(node);
        poisonedJson = hit ? node!.ToJsonString() : json;
        return hit;

        bool Poison(System.Text.Json.Nodes.JsonNode? current)
        {
            switch (current)
            {
                case System.Text.Json.Nodes.JsonObject obj:
                    if (obj.ContainsKey(key))
                    {
                        obj[key] = System.Text.Json.Nodes.JsonValue.Create(42);
                        return true;
                    }

                    foreach (var pair in obj)
                    {
                        if (Poison(pair.Value))
                        {
                            return true;
                        }
                    }

                    return false;

                case System.Text.Json.Nodes.JsonArray array:
                    foreach (var item in array)
                    {
                        if (Poison(item))
                        {
                            return true;
                        }
                    }

                    return false;

                default:
                    return false;
            }
        }
    }
}
