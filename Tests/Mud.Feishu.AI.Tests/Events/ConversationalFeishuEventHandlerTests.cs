// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.Tests.Events;

/// <summary>
/// 会话式事件处理器：事件→会话→模型→回复 管线与业务键命名空间（Phase 1 §3.1）。
/// </summary>
public class ConversationalFeishuEventHandlerTests
{
    private sealed class DemoEvent : IEventResult
    {
        public string MessageId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    private sealed class RecordingHandler(
        FeishuAgent agent,
        IFeishuEventDeduplicator deduplicator,
        IReadOnlyList<IContextAssembler>? assemblers = null) : ConversationalFeishuEventHandler<DemoEvent>(
            agent, deduplicator, NullLogger.Instance, assemblers)
    {
        public ConversationRequest? LastRequest { get; private set; }
        public string? LastReply { get; private set; }

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
            LastRequest = request;
            LastReply = responseText;
            return Task.CompletedTask;
        }

        protected override async Task<string> AssembleUserMessageAsync(ConversationRequest request, CancellationToken cancellationToken)
        {
            var baseText = await base.AssembleUserMessageAsync(request, cancellationToken);
            return string.IsNullOrEmpty(baseText) ? "默认用户消息" : $"{baseText}\n默认用户消息";
        }
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

    private static RecordingHandler CreateHandler(
        string replyText = "模型回答",
        IReadOnlyList<IContextAssembler>? assemblers = null)
    {
        var mockClient = new Mock<IChatClient>();
        mockClient.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, replyText)));
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });

        return new RecordingHandler(agent, CreateDeduplicator().Object, assemblers);
    }

    private static EventData DemoEventData(string eventId) => new()
    {
        EventId = eventId,
        Event = new DemoEvent { MessageId = "om_1", Text = "帮我查表" },
    };

    [Fact]
    public async Task HandleAsync_ShouldRunPipeline_AndReply_WhenEventIsValid()
    {
        var handler = CreateHandler(replyText: "模型回答");

        await handler.HandleAsync(DemoEventData("evt-1"), default);

        handler.LastReply.Should().Be("模型回答");
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.AppKey.Should().Be("app-a");
        handler.LastRequest.SubjectId.Should().Be("oc_1");
        handler.LastRequest.MessageId.Should().Be("om_1");
    }

    [Fact]
    public async Task HandleAsync_ShouldSkip_WhenEventEntityMissing()
    {
        var handler = CreateHandler();

        // Event == null → DeserializeEvent 返回 null → 会话管线跳过。
        await handler.HandleAsync(new EventData { EventId = "evt-2" }, default);

        handler.LastReply.Should().BeNull("缺实体的事件不进模型管线");
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipDuplicateBusinessKey()
    {
        var deduplicator = new Mock<IFeishuEventDeduplicator>();
        deduplicator.Setup(d => d.TryMarkAsProcessingAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeduplicationResult { IsDuplicate = true });

        var mockClient = new Mock<IChatClient>();
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var handler = new RecordingHandler(agent, deduplicator.Object);

        await handler.HandleAsync(DemoEventData("evt-dup"), default);

        handler.LastReply.Should().BeNull("重复业务键不二次调模型（事件幂等）");
        mockClient.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void GetBusinessKey_ShouldBeNamespaced()
    {
        var handler = CreateHandler();
        var method = handler.GetType().GetMethod(
            "GetBusinessKey", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var key = method.Invoke(handler, [new EventData { EventId = "evt-3" }]) as string;

        key.Should().Be("feishu.agent.conversation:evt-3", "业务键必须自带命名空间（禁止裸 EventId）");
    }

    [Fact]
    public async Task AssembleUserMessage_ShouldIsolateAssemblerFailures()
    {
        var failing = new Mock<IContextAssembler>();
        failing.SetupGet(a => a.Order).Returns(0);
        failing.Setup(a => a.AssembleAsync(It.IsAny<ConversationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var working = new Mock<IContextAssembler>();
        working.SetupGet(a => a.Order).Returns(1);
        working.Setup(a => a.AssembleAsync(It.IsAny<ConversationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("工作正常的片段");

        var handler = CreateHandler(replyText: "ok", assemblers: [failing.Object, working.Object]);

        // 直接驱动管线，聚焦装配器隔离行为。
        var method = handler.GetType().GetMethod(
            "ProcessBusinessLogicAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(handler,
            [DemoEventData("evt-4"), new DemoEvent { MessageId = "om_2", Text = "t" }, CancellationToken.None])!;

        handler.LastReply.Should().Be("ok", "单个装配器失败不中断事件处理");
    }
}
