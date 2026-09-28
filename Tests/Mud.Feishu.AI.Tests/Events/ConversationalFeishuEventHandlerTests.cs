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

    /// <summary>可写应用键上下文（模拟 Webhook 多应用宿主）。</summary>
    private sealed class TestAppKeyAccessor : IAppKeyAccessor
    {
        public string? CurrentAppKey { get; private set; }

        public void SetAppKey(string appKey) => CurrentAppKey = appKey;

        public void Clear() => CurrentAppKey = null;
    }

    /// <summary>记录会话键的存储（用于断言 appKey 是否真的进入会话键命名空间）。</summary>
    private sealed class RecordingConversationStore : IConversationStore
    {
        private readonly Dictionary<string, string> _payloads = new(StringComparer.Ordinal);

        public List<string> SavedKeys { get; } = [];

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(_payloads.TryGetValue(key, out var payload) ? payload : null);

        public Task SaveAsync(string key, string serializedSession, CancellationToken cancellationToken = default)
        {
            SavedKeys.Add(key);
            _payloads[key] = serializedSession;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            _payloads.Remove(key);
            return Task.CompletedTask;
        }
    }

    /// <summary>可注入应用键访问器 / 空用户消息 / 强制 fail-fast 的探针处理器（与内置 IM 处理器同款 appKey 取值口径）。</summary>
    private sealed class ProbeHandler(
        FeishuAgent agent,
        IFeishuEventDeduplicator deduplicator,
        IAppKeyAccessor? accessor,
        bool emptyUserMessage = false,
        bool forceFailFast = false,
        IFeishuToolContextAccessor? toolContextAccessor = null,
        bool allowToolsWithoutAppKey = false) : ConversationalFeishuEventHandler<DemoEvent>(
            agent, deduplicator, NullLogger.Instance, appKeyAccessor: accessor,
            toolContextAccessor: toolContextAccessor)
    {
        public string? LastReply { get; private set; }

        protected override bool AllowMissingAppKey => forceFailFast ? false : base.AllowMissingAppKey;

        protected override bool AllowToolsWithoutAppKey => allowToolsWithoutAppKey;

        protected override Task<ConversationRequest> BuildRequestAsync(DemoEvent eventData, CancellationToken cancellationToken)
            => Task.FromResult(new ConversationRequest(
                // 内置 IM 处理器的既有口径：appKey 取自应用键上下文，缺失时为空串（由基类分级处置）。
                AppKey: CurrentAppKey ?? string.Empty,
                Scope: ConversationScope.Group(),
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
            => Task.FromResult(emptyUserMessage ? "   " : "用户消息");
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

    // ───────────────────── P1-5a / P1-5b：appKey 装配链与分级处置 ─────────────────────

    private static Mock<IChatClient> CreateChatClient(string reply = "模型回答")
    {
        var mockClient = new Mock<IChatClient>();
        mockClient.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
        return mockClient;
    }

    [Fact]
    public async Task HandleAsync_ShouldUseAccessorAppKey_WhenAccessorRegistered()
    {
        var store = new RecordingConversationStore();
        var accessor = new TestAppKeyAccessor();
        accessor.SetAppKey("app-b");
        var agent = new FeishuAgent(CreateChatClient().Object, new FeishuAgentOptions { Instructions = "x" }, store);

        // 事件规范化侧未拿到 appKey（空串）——访问器是唯一事实来源。
        var handler = new ProbeHandler(agent, CreateDeduplicator().Object, accessor);
        await handler.HandleAsync(DemoEventData("evt-appkey-1"), default);

        store.SavedKeys.Should().ContainSingle();
        store.SavedKeys[0].Should().StartWith($"feishu:app-b:conversation:",
            "会话键必须携带真实 appKey（否则多应用共用 default 命名空间、历史跨租户混用）");
    }

    [Fact]
    public async Task HandleAsync_ShouldFailFast_WhenAccessorRegisteredButAppKeyBlank()
    {
        var accessor = new TestAppKeyAccessor(); // 已装配但当前值为空（多应用宿主的配置缺陷）
        var mockClient = CreateChatClient();
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var handler = new ProbeHandler(agent, CreateDeduplicator().Object, accessor);

        var act = async () => await handler.HandleAsync(DemoEventData("evt-appkey-2"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AppKey*", "多应用宿主取不到 appKey 属缺陷，必须 fail-fast（TMA2-20）");
        mockClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never, "拒绝必须在模型调用之前发生（零成本、零下游调用）");
        handler.LastReply.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldDegrade_WhenNoAccessorRegistered()
    {
        // WebSocket 单应用宿主：未注册 IAppKeyAccessor，按既有语义降级（不得打断既有部署）。
        var mockClient = CreateChatClient();
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var handler = new ProbeHandler(agent, CreateDeduplicator().Object, accessor: null);

        await handler.HandleAsync(DemoEventData("evt-appkey-3"), default);

        handler.LastReply.Should().Be("模型回答", "单应用宿主降级到 default 命名空间并正常应答");
    }

    [Fact]
    public async Task HandleAsync_ShouldFailFast_WhenAllowMissingAppKeyOverriddenToFalse()
    {
        var mockClient = CreateChatClient();
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var handler = new ProbeHandler(
            agent, CreateDeduplicator().Object, accessor: null, forceFailFast: true);

        var act = async () => await handler.HandleAsync(DemoEventData("evt-appkey-4"), default);

        await act.Should().ThrowAsync<InvalidOperationException>("宿主可覆写钩子强制多应用强校验");
    }

    // ───────────────────── R2-3：AppKey 缺失 × 工具执行链已装配 ⇒ fail-fast ─────────────────────

    [Fact]
    public async Task HandleAsync_ShouldFailFast_WhenToolChainPresent_AndAppKeyMissing()
    {
        // 单应用宿主（未注册 IAppKeyAccessor）+ 已装配工具执行链 + appKey 为空
        // ⇒ 工具/知识注入必然 100% 失败，不得伪装成「正常降级」。
        var mockClient = CreateChatClient();
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var handler = new ProbeHandler(
            agent, CreateDeduplicator().Object, accessor: null,
            toolContextAccessor: new FeishuToolContextAccessor());

        var act = async () => await handler.HandleAsync(DemoEventData("evt-tools-appkey-1"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*工具执行链已装配*", "异常必须给出可读指引（注册 IAppKeyAccessor 或覆写逃生门）");
        mockClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never, "fail-fast 必须发生在模型调用之前（否则白跑一次模型调用）");
        handler.LastReply.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldDegrade_WhenToolChainAbsent_AndAppKeyMissing()
    {
        // 无工具执行链：保持 R1 既有降级语义（不回归）。
        var mockClient = CreateChatClient();
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var handler = new ProbeHandler(agent, CreateDeduplicator().Object, accessor: null);

        await handler.HandleAsync(DemoEventData("evt-tools-appkey-2"), default);

        handler.LastReply.Should().Be("模型回答", "无工具链时 appKey 缺失仍按既有单应用降级语义处理");
    }

    [Fact]
    public async Task HandleAsync_ShouldDegrade_WhenEscapeHatchOverridden()
    {
        var mockClient = CreateChatClient();
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var handler = new ProbeHandler(
            agent, CreateDeduplicator().Object, accessor: null,
            toolContextAccessor: new FeishuToolContextAccessor(), allowToolsWithoutAppKey: true);

        await handler.HandleAsync(DemoEventData("evt-tools-appkey-3"), default);

        handler.LastReply.Should().Be("模型回答",
            "覆写 AllowToolsWithoutAppKey => true 即显式接受「工具面不可用」，按既有降级继续");
    }

    [Fact]
    public async Task HandleAsync_ShouldNotFailFast_WhenAppKeyPresent_AndToolChainPresent()
    {
        // 有 appKey 时本守卫不得误伤（正向锁定）。
        var store = new RecordingConversationStore();
        var accessor = new TestAppKeyAccessor();
        accessor.SetAppKey("app-c");
        var agent = new FeishuAgent(CreateChatClient().Object, new FeishuAgentOptions { Instructions = "x" }, store);
        var handler = new ProbeHandler(
            agent, CreateDeduplicator().Object, accessor,
            toolContextAccessor: new FeishuToolContextAccessor());

        await handler.HandleAsync(DemoEventData("evt-tools-appkey-4"), default);

        handler.LastReply.Should().Be("模型回答");
        store.SavedKeys.Should().ContainSingle();
        store.SavedKeys[0].Should().StartWith("feishu:app-c:conversation:");
    }

    // ───────────────────── P2-7：空用户消息守卫 ─────────────────────

    [Fact]
    public async Task HandleAsync_ShouldSkipModelCall_WhenUserMessageEmpty()
    {
        var mockClient = CreateChatClient();
        var agent = new FeishuAgent(mockClient.Object, new FeishuAgentOptions { Instructions = "x" });
        var handler = new ProbeHandler(
            agent, CreateDeduplicator().Object, accessor: null, emptyUserMessage: true);

        await handler.HandleAsync(DemoEventData("evt-empty"), default);

        mockClient.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never, "空 user 消息会被端点拒绝（400）且已计费，必须在调用前拦截");
        handler.LastReply.Should().BeNull();
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
