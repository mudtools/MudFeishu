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

        /// <summary>本轮是否发生了回复下发（用于断言「空回复不下发」）。</summary>
        public int ReplyCount { get; private set; }

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
            ReplyCount++;
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
        deduplicator.Setup(d => d.RollbackProcessingAsync(
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
    public async Task HandleAsync_ShouldFlushPlaceholder_WhenModelStreamThrows()
    {
        // P1-2：模型流中断时占位消息会停留在「上一次成功内容」（通道的既定语义），
        // 必须用**不可取消**的令牌补一次 FlushAsync 把它落到终结态。
        var mockClient = new Mock<IChatClient>();
        mockClient
            .Setup(c => c.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<ChatMessage> _, ChatOptions? __, CancellationToken ___) => YieldThenThrow());
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var channel = new Mock<IMessageChannel>();
        channel
            .Setup(c => c.BeginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("om_stream_comp");

        var handler = new RecordingHandler(agent, CreateDeduplicator().Object, channel.Object);
        var act = async () => await handler.HandleAsync(DemoEventData("evt-stream-comp"), default);

        await act.Should().ThrowAsync<HttpRequestException>("异常仍须向上传播（幂等键回滚，既有语义不变）");
        channel.Verify(
            c => c.FlushAsync("app-a", "oc_1", "om_stream_comp", CancellationToken.None),
            Times.Once,
            "补偿收尾必须使用不可取消的令牌（否则补偿本身被取消，问题依旧）");
    }

    [Fact]
    public async Task HandleAsync_ShouldStillPropagate_WhenCompensationFlushFails()
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
            .ReturnsAsync("om_stream_comp2");
        channel
            .Setup(c => c.FlushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("通道故障"));

        var handler = new RecordingHandler(agent, CreateDeduplicator().Object, channel.Object);
        var act = async () => await handler.HandleAsync(DemoEventData("evt-stream-comp2"), default);

        await act.Should().ThrowAsync<HttpRequestException>(
            "补偿失败只记 Warning，不得掩盖原始异常类型（幂等回滚按原类型判定）");
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

    // ───────────────────── R2-7：流式增量写入的调用方兜底 ─────────────────────

    [Fact]
    public async Task HandleAsync_ShouldIsolateWriteStreamFailure_WhenChannelViolatesContract()
    {
        // 通道违反「实现必须自带失败隔离」的契约（IMessageChannel.WriteStreamAsync 注释）。
        var mockClient = CreateStreamingClient("第一段", "第二段", "第三段");
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var channel = new Mock<IMessageChannel>();
        channel
            .Setup(c => c.BeginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("om_stream_bad");
        channel
            .Setup(c => c.WriteStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("通道写入失败"));

        var deduplicator = CreateDeduplicator();
        var handler = new RecordingHandler(agent, deduplicator.Object, channel.Object);

        // 不得抛异常：单次写入失败只跳过分片，绝不升级为「补偿 + 回滚 + 重投递（模型重复计费）」。
        await handler.HandleAsync(DemoEventData("evt-stream-isolate"), default);

        channel.Verify(c => c.FlushAsync("app-a", "oc_1", "om_stream_bad", It.IsAny<CancellationToken>()),
            Times.Once, "模型流必须跑完并正常收尾");
        handler.LastReply.Should().BeNull("流式路径不走派生类回复");
        deduplicator.Verify(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never, "通道问题不得触发幂等回滚重投递（那会让模型重复计费）");
        deduplicator.Verify(d => d.MarkAsCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once, "本轮按已消费处理");
    }

    [Fact]
    public async Task HandleAsync_ShouldStillPropagate_WhenModelStreamThrows()
    {
        // 正向锁定：R2-7 的兜底只隔离**通道写入**失败，模型侧异常仍必须传播（既有语义不变）。
        var mockClient = new Mock<IChatClient>();
        mockClient
            .Setup(c => c.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<ChatMessage> _, ChatOptions? __, CancellationToken ___) => YieldThenThrow());
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var channel = new Mock<IMessageChannel>();
        channel
            .Setup(c => c.BeginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("om_stream_model_fail");

        var handler = new RecordingHandler(agent, CreateDeduplicator().Object, channel.Object);
        var act = async () => await handler.HandleAsync(DemoEventData("evt-stream-model-fail"), default);

        await act.Should().ThrowAsync<HttpRequestException>("模型侧异常不得被通道兜底吞掉");
    }

    // ───────────────────── R2-9：空模型回复守卫 ─────────────────────

    [Fact]
    public async Task HandleAsync_ShouldSkipReply_AndNotRollback_WhenModelReplyEmpty()
    {
        var mockClient = new Mock<IChatClient>();
        mockClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "   ")));
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var deduplicator = CreateDeduplicator();

        var handler = new RecordingHandler(agent, deduplicator.Object, messageChannel: null);
        await handler.HandleAsync(DemoEventData("evt-empty-reply"), default);

        handler.ReplyCount.Should().Be(0, "空回复无可下发内容，下发会让飞书 API 报错 → 回滚重投递 → 重复计费");
        deduplicator.Verify(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never, "按「已消费」处理，不触发重投递重跑模型");
        deduplicator.Verify(d => d.MarkAsCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldStillPersistSession_WhenModelReplyEmpty()
    {
        // R2-9 复核修正：守卫置于 SaveSessionAsync **之后**，历史不得因空回复丢一轮。
        var mockClient = new Mock<IChatClient>();
        mockClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty)));

        var store = new RecordingConversationStore();
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" }, store);
        var handler = new RecordingHandler(agent, CreateDeduplicator().Object, messageChannel: null);

        await handler.HandleAsync(DemoEventData("evt-empty-reply-persist"), default);

        handler.ReplyCount.Should().Be(0);
        store.SavedKeys.Should().NotBeEmpty("空回复只跳过下发，会话历史仍须落库（否则凭空丢一轮）");
    }

    [Fact]
    public async Task HandleAsync_ShouldStillReply_WhenStreamed_AndTextEmpty()
    {
        // 流式路径不适用本守卫：内容已由通道送达（Begin/Write/Flush），不得误伤。
        var mockClient = new Mock<IChatClient>();
        mockClient
            .Setup(c => c.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<ChatMessage> _, ChatOptions? __, CancellationToken ___) => YieldUpdates([]));
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var channel = new Mock<IMessageChannel>();
        channel
            .Setup(c => c.BeginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("om_stream_empty");

        var deduplicator = CreateDeduplicator();
        var handler = new RecordingHandler(agent, deduplicator.Object, channel.Object);

        await handler.HandleAsync(DemoEventData("evt-stream-empty"), default);

        channel.Verify(c => c.FlushAsync("app-a", "oc_1", "om_stream_empty", It.IsAny<CancellationToken>()),
            Times.Once, "空增量的流式回合仍须正常收尾（守卫只作用于非流式路径）");
        deduplicator.Verify(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class RecordingConversationStore : IConversationStore
    {
        public List<string> SavedKeys { get; } = [];

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task SaveAsync(string key, string serializedSession, CancellationToken cancellationToken = default)
        {
            SavedKeys.Add(key);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
