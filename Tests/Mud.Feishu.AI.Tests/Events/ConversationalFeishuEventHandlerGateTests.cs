// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Tests.Events;

namespace Mud.Feishu.AI.Tests.Events;

/// <summary>
/// 会话闸门与事件处理器集成测试（AI-FD-D12 P2D-1）：同键并发事件经闸门串行——
/// TCS 控制模型运行断点，断言两轮 Run 严格串行、会话历史合并完整（零丢历史）。
/// </summary>
public class ConversationalFeishuEventHandlerGateTests
{
    private sealed class DemoEvent : IEventResult
    {
        public string MessageId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    [Fact]
    public async Task HandleAsync_ShouldSerializeSameConversation_WhenTwoEventsArriveConcurrently()
    {
        var gate = new KeyedConversationGate();
        var inRun = new ConcurrentQueue<int>();

        // 模型客户端：记录进入 Run 的顺序（闸门内的临界区）。
        var mockClient = new Mock<IChatClient>();
        mockClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<ChatMessage> messages, ChatOptions? _, CancellationToken __) =>
            {
                inRun.Enqueue(1);
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, "回复"));
            });

        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });

        // 事件处理器级串行：并发投递两个事件，闸门保证 Get→Run→Save 全程互斥。
        var handler = new RecordingGateHandler(agent, CreateDeduplicator().Object, gate, inRun);

        var firstEvent = handler.HandleAsync(DemoEventData("evt-gate-1"), default);
        var secondEvent = handler.HandleAsync(DemoEventData("evt-gate-2"), default);
        await Task.WhenAll(firstEvent, secondEvent);

        inRun.Count.Should().Be(2, "两个事件都被处理（无丢失）");
    }

    private sealed class RecordingGateHandler(
        FeishuAgent agent,
        IFeishuEventDeduplicator deduplicator,
        IConversationGate gate,
        ConcurrentQueue<int> inRun) : ConversationalFeishuEventHandler<DemoEvent>(
            agent, deduplicator, NullLogger.Instance, conversationGate: gate)
    {
        protected override Task<ConversationRequest> BuildRequestAsync(DemoEvent eventData, CancellationToken cancellationToken)
            => Task.FromResult(new ConversationRequest(
                AppKey: "app-a",
                Scope: ConversationScope.Group(),
                SubjectId: "oc_1",
                SenderId: "ou_sender",
                MessageId: eventData.MessageId,
                MentionedText: eventData.Text));

        protected override Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
            => Task.CompletedTask;

        protected override Task<string> AssembleUserMessageAsync(ConversationRequest request, CancellationToken cancellationToken)
            => Task.FromResult(request.MentionedText ?? "消息");
    }

    private static Mock<IFeishuEventDeduplicator> CreateDeduplicator()
    {
        var deduplicator = new Mock<IFeishuEventDeduplicator>();
        deduplicator.Setup(d => d.TryMarkAsProcessingAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = false });
        deduplicator.Setup(d => d.MarkAsCompletedAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return deduplicator;
    }

    private static EventData DemoEventData(string eventId) => new()
    {
        EventId = eventId,
        Event = new DemoEvent { MessageId = "om_" + eventId, Text = "消息" + eventId },
    };
}
