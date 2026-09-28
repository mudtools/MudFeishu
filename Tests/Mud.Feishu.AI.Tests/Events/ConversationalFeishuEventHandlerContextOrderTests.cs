// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.Tests.Events;

/// <summary>
/// 工具上下文注入与上下文装配的**顺序契约**（P0-1 + P2-4）：
/// 装配器必须能读到 <see cref="IFeishuToolContextAccessor.Current"/>——知识/引用类装配器
/// （<c>KnowledgeContextAssembler</c> → <c>AilyKnowledgeProvider</c>）靠它切租户，顺序颠倒即
/// 「每轮零知识注入」且被装配器隔离 catch 吞为 Warning。
/// </summary>
public class ConversationalFeishuEventHandlerContextOrderTests
{
    private sealed class DemoEvent : IEventResult
    {
        public string MessageId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>记录装配期读到的工具上下文（修复前恒为 null）。</summary>
    private sealed class ContextProbeAssembler(IFeishuToolContextAccessor accessor, bool yieldBeforeRead = false) : IContextAssembler
    {
        public FeishuToolContext? Observed { get; private set; }

        public int Order => 0;

        public async Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
        {
            if (yieldBeforeRead)
            {
                // 验证 AsyncLocal 在 await 续体（可能切换线程）中同样成立。
                await Task.Yield();
            }

            Observed = accessor.Current;
            return "装配片段";
        }
    }

    private sealed class RecordingHandler(
        FeishuAgent agent,
        IFeishuEventDeduplicator deduplicator,
        IFeishuToolContextAccessor accessor,
        IReadOnlyList<IContextAssembler> assemblers,
        string? chatId = null,
        string? hookChatId = null) : ConversationalFeishuEventHandler<DemoEvent>(
            agent, deduplicator, NullLogger.Instance, assemblers, accessor)
    {
        protected override Task<ConversationRequest> BuildRequestAsync(DemoEvent eventData, CancellationToken cancellationToken)
            => Task.FromResult(new ConversationRequest(
                AppKey: "app-a",
                Scope: ConversationScope.Group(),
                SubjectId: "oc_1",
                SenderId: "ou_sender",
                MessageId: eventData.MessageId,
                MentionedText: eventData.Text,
                ChatId: chatId));

        protected override Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
            => Task.CompletedTask;

        protected override string? ResolveStreamTargetChatId(ConversationRequest request)
            => hookChatId ?? base.ResolveStreamTargetChatId(request);
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
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        return new FeishuAgent(client.Object, new FeishuAgentOptions { Instructions = "x" });
    }

    private static EventData DemoEventData() => new()
    {
        EventId = "evt-order-1",
        Event = new DemoEvent { MessageId = "om_1", Text = "帮我查表" },
    };

    [Fact]
    public async Task AssembleAsync_ShouldSeeToolContext_WhenAssemblerRegistered()
    {
        var accessor = new FeishuToolContextAccessor();
        var probe = new ContextProbeAssembler(accessor);
        var handler = new RecordingHandler(CreateAgent(), CreateDeduplicator().Object, accessor, [probe]);

        await handler.HandleAsync(DemoEventData(), default);

        probe.Observed.Should().NotBeNull(
            "工具上下文必须前置于上下文装配（否则知识/引用装配器读不到 appKey，静默零注入）");
        probe.Observed!.AppKey.Should().Be("app-a");
        probe.Observed.ConversationKey.Should().Be(
            ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1"));
        probe.Observed.UserId.Should().Be("ou_sender");
    }

    [Fact]
    public async Task AssembleAsync_ShouldSeeToolContext_AcrossNestedAwait()
    {
        var accessor = new FeishuToolContextAccessor();
        var probe = new ContextProbeAssembler(accessor, yieldBeforeRead: true);
        var handler = new RecordingHandler(CreateAgent(), CreateDeduplicator().Object, accessor, [probe]);

        await handler.HandleAsync(DemoEventData(), default);

        probe.Observed.Should().NotBeNull("AsyncLocal 在装配期的 await 续体中同样成立（AsyncLocal 沿异步流传播）");
        probe.Observed!.AppKey.Should().Be("app-a");
    }

    [Fact]
    public async Task HandleAsync_ShouldBuildToolContext_FromChatIdHook()
    {
        // P2-4：工具上下文目标与流式目标共用同一解析钩子，派生类覆写一次即两处生效。
        var accessor = new FeishuToolContextAccessor();
        var probe = new ContextProbeAssembler(accessor);
        var handler = new RecordingHandler(
            CreateAgent(), CreateDeduplicator().Object, accessor, [probe], chatId: null, hookChatId: "oc_from_hook");

        await handler.HandleAsync(DemoEventData(), default);

        probe.Observed.Should().NotBeNull();
        probe.Observed!.ChatId.Should().Be("oc_from_hook", "工具上下文 ChatId 必须走 ResolveStreamTargetChatId 钩子");
    }

    [Fact]
    public async Task HandleAsync_ShouldNotRetainToolContext_AfterPipelineCompletes()
    {
        // 回归锁：作用域必须在管线结束后释放（不得把会话级上下文泄漏给同线程的下一个事件）。
        var accessor = new FeishuToolContextAccessor();
        var probe = new ContextProbeAssembler(accessor);
        var handler = new RecordingHandler(CreateAgent(), CreateDeduplicator().Object, accessor, [probe]);

        await handler.HandleAsync(DemoEventData(), default);

        accessor.Current.Should().BeNull("工具上下文作用域在管线结束时释放（AsyncLocal 无跨事件残留）");
    }
}
