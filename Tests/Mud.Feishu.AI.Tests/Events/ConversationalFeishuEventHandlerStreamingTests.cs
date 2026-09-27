// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.Tests.Events;

/// <summary>
/// 会话式事件处理器流式路径测试（Phase 2 §3.1）：群聊事件经通道流式回复（Begin → 增量 → Flush，
/// 不再走 ReplyAsync）、单聊回退非流式、Begin 失败回退非流式、流中失败向上传播（幂等回滚）。
/// </summary>
public class ConversationalFeishuEventHandlerStreamingTests
{
    private sealed class DemoEvent : IEventResult
    {
        public string MessageId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    private sealed class RecordingHandler(
        FeishuAgent agent,
        IFeishuEventDeduplicator deduplicator,
        IMessageChannel? messageChannel = null,
        bool useP2pScope = false) : ConversationalFeishuEventHandler<DemoEvent>(
            agent, deduplicator, NullLogger.Instance, messageChannel: messageChannel)
    {
        public string? LastReply { get; private set; }

        protected override Task<ConversationRequest> BuildRequestAsync(DemoEvent eventData, CancellationToken cancellationToken)
            => Task.FromResult(new ConversationRequest(
                AppKey: "app-a",
                Scope: useP2pScope ? ConversationScope.P2P() : ConversationScope.Group(),
                SubjectId: "oc_1",
                SenderId: "ou_sender",
                MessageId: eventData.MessageId,
                MentionedText: eventData.Text));

        protected override Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
        {
            LastReply = responseText;
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
        deduplicator.Setup(d => d.MarkAsCompletedAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return deduplicator;
    }

    private static Mock<IChatClient> CreateStreamingClient(params string[] deltas)
    {
        var client = new Mock<IChatClient>();
        client
            .Setup(c => c.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<ChatMessage> _, ChatOptions? __, CancellationToken ___) => YieldUpdates(deltas));
        // 回退路径（Begin 失败/单聊缺 chatId）走非流式 RunAsync → GetResponseAsync。
        client
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Concat(deltas))));
        return client;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldUpdates(string[] deltas)
    {
        foreach (var delta in deltas)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, delta);
        }

        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldThenThrow()
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, "部分输出");
        throw new HttpRequestException("模型流中断");
    }

    private static EventData DemoEventData(string eventId) => new()
    {
        EventId = eventId,
        Event = new DemoEvent { MessageId = "om_1", Text = "帮我查表" },
    };

    [Fact]
    public async Task HandleAsync_ShouldStreamViaChannel_AndSkipReply_WhenGroupEvent()
    {
        var mockClient = CreateStreamingClient("第一段", "第二段");
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var channel = new Mock<IMessageChannel>();
        channel
            .Setup(c => c.BeginAsync("app-a", "oc_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("om_stream_1");

        var handler = new RecordingHandler(agent, CreateDeduplicator().Object, channel.Object);
        await handler.HandleAsync(DemoEventData("evt-stream-1"), default);

        channel.Verify(c => c.BeginAsync("app-a", "oc_1", It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.WriteStreamAsync("app-a", "oc_1", "om_stream_1", "第一段", It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.WriteStreamAsync("app-a", "oc_1", "om_stream_1", "第二段", It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.FlushAsync("app-a", "oc_1", "om_stream_1", It.IsAny<CancellationToken>()), Times.Once);
        handler.LastReply.Should().BeNull("流式路径已由通道送达，不再走 ReplyAsync");
    }

    [Fact]
    public async Task HandleAsync_ShouldFallbackToNonStreaming_WhenChannelBeginFails()
    {
        var mockClient = CreateStreamingClient("增量");
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var channel = new Mock<IMessageChannel>();
        channel
            .Setup(c => c.BeginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("占位消息创建失败"));

        var handler = new RecordingHandler(agent, CreateDeduplicator().Object, channel.Object);
        await handler.HandleAsync(DemoEventData("evt-stream-2"), default);

        handler.LastReply.Should().Be("增量", "Begin 失败回退非流式回复（模型尚未调用，零重复成本）");
        channel.Verify(c => c.WriteStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldFallbackToNonStreaming_WhenP2pScopeCannotResolveChatId()
    {
        var mockClient = CreateStreamingClient("单聊回答");
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var channel = new Mock<IMessageChannel>();

        var handler = new RecordingHandler(agent, CreateDeduplicator().Object, channel.Object, useP2pScope: true);
        await handler.HandleAsync(DemoEventData("evt-stream-3"), default);

        handler.LastReply.Should().Be("单聊回答", "单聊缺省解析不出 chat_id，回退非流式（派生类可覆写 ResolveStreamTargetChatId）");
        channel.Verify(c => c.BeginAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldPropagateMidStreamFailure_ForDedupRollback()
    {
        var mockClient = new Mock<IChatClient>();
        mockClient
            .Setup(c => c.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<ChatMessage> _, ChatOptions? __, CancellationToken ___) => YieldThenThrow());
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var channel = new Mock<IMessageChannel>();
        channel
            .Setup(c => c.BeginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("om_stream_2");

        var handler = new RecordingHandler(agent, CreateDeduplicator().Object, channel.Object);
        var act = async () => await handler.HandleAsync(DemoEventData("evt-stream-4"), default);

        await act.Should().ThrowAsync<HttpRequestException>(
            "流中失败向上传播——事件层幂等键回滚，事件重投递（at-least-once）");
        handler.LastReply.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepNonStreamingPath_WhenChannelNotConfigured()
    {
        var mockClient = new Mock<IChatClient>();
        mockClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "非流式回答")));
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });

        var handler = new RecordingHandler(agent, CreateDeduplicator().Object, messageChannel: null);
        await handler.HandleAsync(DemoEventData("evt-stream-5"), default);

        handler.LastReply.Should().Be("非流式回答", "未注入通道时保持 Phase 1 非流式行为");
    }
}
