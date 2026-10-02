// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Events;

namespace Mud.Feishu.AI.Tests.Events;

/// <summary>
/// 流式目标解耦测试（AI-FD-D12 P2D-2b / 测试策略 §十.3 四象限矩阵）：
/// p2p/group × 有无 ChatId——流式目标与工具上下文目标优先取 <c>request.ChatId</c>，
/// 缺省回退群聊 SubjectId（单聊流式解锁，既有派生类零改动兼容）。
/// </summary>
public class ConversationalFeishuEventHandlerChatIdTests
{
    private sealed class DemoEvent : IEventResult
    {
        public string MessageId { get; set; } = string.Empty;
    }

    private sealed class RecordingHandler(FeishuAgent agent, IFeishuEventDeduplicator deduplicator, IMessageChannel? channel)
        : ConversationalFeishuEventHandler<DemoEvent>(agent, deduplicator, NullLogger.Instance, messageChannel: channel)
    {
        protected override Task<ConversationRequest> BuildRequestAsync(DemoEvent eventData, CancellationToken cancellationToken)
            => Task.FromResult(new ConversationRequest(
                AppKey: "app-a",
                Scope: eventData.MessageId.Contains("p2p") ? ConversationScope.P2P() : ConversationScope.Group(),
                SubjectId: "oc_subject",
                SenderId: "ou_sender",
                MessageId: eventData.MessageId,
                MentionedText: "消息",
                ChatId: eventData.MessageId.Contains("with-chat") ? "oc_chatid" : null));

        protected override Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
            => Task.CompletedTask;

        protected override Task<string> AssembleUserMessageAsync(ConversationRequest request, CancellationToken cancellationToken)
            => Task.FromResult("用户消息");
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

    private static FeishuAgent CreateAgent()
    {
        var client = new Mock<IChatClient>();
        client
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "回复")));
        client
            .Setup(c => c.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(EmptyUpdates());
        return new FeishuAgent(client.Object, new FeishuAgentOptions { Instructions = "x" });
    }

    private static EventData DemoEventData(string messageId) => new()
    {
        EventId = "evt-" + messageId,
        Event = new DemoEvent { MessageId = messageId },
    };

    private static async IAsyncEnumerable<ChatResponseUpdate> EmptyUpdates()
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, "回复");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task HandleAsync_ShouldStreamToChatId_ForGroupEvent_WithChatId()
    {
        var channel = new Mock<IMessageChannel>();
        channel.Setup(c => c.BeginAsync("app-a", "oc_chatid", It.IsAny<CancellationToken>())).ReturnsAsync("om_s1");
        var handler = new RecordingHandler(CreateAgent(), CreateDeduplicator().Object, channel.Object);

        await handler.HandleAsync(DemoEventData("group_with-chat"), default);

        channel.Verify(c => c.BeginAsync("app-a", "oc_chatid", It.IsAny<CancellationToken>()), Times.Once,
            "群聊 + ChatId：流式目标优先取 ChatId（P2D-2b）");
    }

    [Fact]
    public async Task HandleAsync_ShouldStreamToChatId_ForP2pEvent_WithChatId()
    {
        var channel = new Mock<IMessageChannel>();
        channel.Setup(c => c.BeginAsync("app-a", "oc_chatid", It.IsAny<CancellationToken>())).ReturnsAsync("om_s2");
        var handler = new RecordingHandler(CreateAgent(), CreateDeduplicator().Object, channel.Object);

        await handler.HandleAsync(DemoEventData("p2p_with-chat"), default);

        channel.Verify(c => c.BeginAsync("app-a", "oc_chatid", It.IsAny<CancellationToken>()), Times.Once,
            "单聊 + ChatId：单聊流式解锁（G12 是映射缺失而非能力缺失）");
    }

    [Fact]
    public async Task HandleAsync_ShouldFallbackToSubjectId_ForGroupEvent_WithoutChatId()
    {
        var channel = new Mock<IMessageChannel>();
        channel.Setup(c => c.BeginAsync("app-a", "oc_subject", It.IsAny<CancellationToken>())).ReturnsAsync("om_s3");
        var handler = new RecordingHandler(CreateAgent(), CreateDeduplicator().Object, channel.Object);

        await handler.HandleAsync(DemoEventData("group_no-chat"), default);

        channel.Verify(c => c.BeginAsync("app-a", "oc_subject", It.IsAny<CancellationToken>()), Times.Once,
            "群聊缺省回退会话主体（既有行为兼容）");
    }

    [Fact]
    public async Task HandleAsync_ShouldFallbackToNonStreaming_ForP2pEvent_WithoutChatId()
    {
        var channel = new Mock<IMessageChannel>();
        var handler = new RecordingHandler(CreateAgent(), CreateDeduplicator().Object, channel.Object);

        await handler.HandleAsync(DemoEventData("p2p_no-chat"), default);

        channel.Verify(c => c.BeginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "单聊无 ChatId：不适用流式，回退非流式（既有行为兼容）");
    }

    [Fact]
    public async Task HandleAsync_ShouldStreamWithDefaultChatId_WhenResolverReturnsNullAndChatIdPresent()
    {
        // R5-3 语义锁定（两段式解析，P2D-2a）：覆写方法返回 null **不是**「放弃流式」，
        // 而是回退 ResolveStreamTargetChatId 的默认解析值，作为 BeginAsync 的入参兜底目标。
        // 本用例钉住 `??` 语义，防止后续按任一方文档「修复」成 null ⇒ 弃流式 时静默漂移。
        var channel = new Mock<IMessageChannel>();
        channel.Setup(c => c.BeginAsync("app-a", "oc_chatid", It.IsAny<CancellationToken>())).ReturnsAsync("om_s4");
        channel
            .As<IMessageChannelTargetResolver>()
            .Setup(r => r.ResolveStreamTarget(It.IsAny<ConversationRequest>()))
            .Returns((string?)null);

        var handler = new RecordingHandler(CreateAgent(), CreateDeduplicator().Object, channel.Object);
        await handler.HandleAsync(DemoEventData("group_with-chat"), default);

        channel.As<IMessageChannelTargetResolver>().Verify(
            r => r.ResolveStreamTarget(It.IsAny<ConversationRequest>()), Times.Once,
            "通道实现了解析器接口 ⇒ 事件处理器必须先咨询它");
        channel.Verify(c => c.BeginAsync("app-a", "oc_chatid", It.IsAny<CancellationToken>()), Times.Once,
            "解析器返回 null ⇒ 回退默认解析值（?? 语义），仍以 ChatId 流式");
    }

    [Fact]
    public async Task HandleAsync_ShouldFallbackToNonStreaming_WhenResolverReturnsNullAndNoDefaultTarget()
    {
        // 对照组：解析器返回 null 且默认解析值也为 null（单聊无 ChatId）⇒ 才真正回退非流式。
        var channel = new Mock<IMessageChannel>();
        channel
            .As<IMessageChannelTargetResolver>()
            .Setup(r => r.ResolveStreamTarget(It.IsAny<ConversationRequest>()))
            .Returns((string?)null);

        var handler = new RecordingHandler(CreateAgent(), CreateDeduplicator().Object, channel.Object);
        await handler.HandleAsync(DemoEventData("p2p_no-chat"), default);

        channel.Verify(c => c.BeginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "兜底目标同样不可得时才回退非流式");
    }
}
