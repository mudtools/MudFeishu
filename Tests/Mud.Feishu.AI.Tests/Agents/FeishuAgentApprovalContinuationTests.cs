// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.AI.Events;

namespace Mud.Feishu.AI.Tests.Agents;

/// <summary>
/// R5-11 / R5-12：HITL 批准回灌续跑闭环 + 「未应答审批」自愈。
/// </summary>
/// <remarks>
/// <para>
/// 本组用例走<b>真实框架管线</b>（<c>ChatClientAgent</c> → <c>ApprovalResponseBindingChatClient</c> →
/// <c>FunctionInvokingChatClient</c>）：写工具经 MEAI <c>ApprovalRequiredAIFunction</c> 包装，
/// fake <see cref="IChatClient"/> 首轮返回工具调用，由框架把它改写成审批请求并写入会话状态袋。
/// 不用桩替身替代框架绑定层——那会绕开本组用例真正要验证的东西（批准资格归框架记录所有）。
/// </para>
/// </remarks>
public class FeishuAgentApprovalContinuationTests
{
    private const string WriteTool = "bitable.add_record";

    private sealed class DemoEvent : IEventResult
    {
        public string MessageId { get; set; } = string.Empty;
    }

    private sealed class RecordingHandler(
        FeishuAgent agent,
        IFeishuEventDeduplicator deduplicator,
        IFeishuToolApprovalChannel? approvalChannel = null)
        : ConversationalFeishuEventHandler<DemoEvent>(
            agent, deduplicator, NullLogger.Instance, approvalChannel: approvalChannel)
    {
        public string? LastReply { get; private set; }

        protected override Task<ConversationRequest> BuildRequestAsync(DemoEvent eventData, CancellationToken cancellationToken)
            => Task.FromResult(new ConversationRequest(
                AppKey: "app-a",
                Scope: ConversationScope.Group(),
                SubjectId: "oc_1",
                SenderId: "ou_sender",
                MessageId: eventData.MessageId,
                MentionedText: "请写一条记录",
                ChatId: "oc_chat"));

        protected override Task ReplyAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken)
        {
            LastReply = responseText;
            return Task.CompletedTask;
        }

        protected override Task<string> AssembleUserMessageAsync(ConversationRequest request, CancellationToken cancellationToken)
            => Task.FromResult("请写一条记录");
    }

    private sealed class CapturingChannel : IFeishuToolApprovalChannel
    {
        internal List<FrameworkToolApprovalRequest> Requests { get; } = [];

        public Task<string?> RequestApprovalAsync(ToolApprovalRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<string?> RequestFrameworkApprovalAsync(
            FrameworkToolApprovalRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult<string?>("approval-1");
        }
    }

    /// <summary>首轮返回写工具调用（由框架改写为审批请求），其后的轮次返回最终文本。</summary>
    private sealed class ScriptedChatClient(Func<ChatMessage> firstResponse, string finalText) : IChatClient
    {
        private int _calls;

        public int Calls => _calls;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var index = Interlocked.Increment(ref _calls) - 1;
            var message = index == 0 ? firstResponse() : new ChatMessage(ChatRole.Assistant, finalText);
            return Task.FromResult(new ChatResponse(message));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("本组用例只覆盖非流式续跑路径");

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    /// <summary>测试装配件：Agent + 会话存储 + 批准通道 + 工具上下文访问器 + 工具执行观测点。</summary>
    private sealed class Harness(
        FeishuAgent agent,
        MemoryConversationStore store,
        CapturingChannel channel,
        FeishuToolContextAccessor accessor,
        List<string?> executedAppKeys)
    {
        internal FeishuAgent Agent { get; } = agent;

        internal MemoryConversationStore Store { get; } = store;

        internal CapturingChannel Channel { get; } = channel;

        internal FeishuToolContextAccessor Accessor { get; } = accessor;

        /// <summary>工具**每次真实执行**时采样到的租户 appKey（未执行即空）。</summary>
        internal List<string?> ExecutedAppKeys { get; } = executedAppKeys;

        internal bool Executed => ExecutedAppKeys.Count > 0;
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

    private static EventData DemoEventData(string eventId, string messageId) => new()
    {
        EventId = eventId,
        Event = new DemoEvent { MessageId = messageId },
    };

    private static FunctionCallContent WriteCall()
        => new("call-1", WriteTool, new Dictionary<string, object?> { ["table_id"] = "tbl_x" });

    /// <summary>首轮模型回应：直接给出写工具调用（经框架改写为审批请求）。</summary>
    private static ChatMessage ApprovalFirstResponse() => new(ChatRole.Assistant, [WriteCall()]);

    /// <summary>
    /// 装配：写工具经框架审批包装；工具执行时采样当前租户上下文（<c>AsyncLocal</c> 只能在使用点读取）。
    /// </summary>
    private static Harness CreateHarness(IChatClient client)
    {
        var accessor = new FeishuToolContextAccessor();
        var store = new MemoryConversationStore();
        var channel = new CapturingChannel();
        var executedAppKeys = new List<string?>();

        var function = AIFunctionFactory.Create(
            (string table_id) =>
            {
                executedAppKeys.Add(accessor.Current?.AppKey);
                return "已写入";
            },
            WriteTool);

        var agent = new FeishuAgent(
            client,
            new FeishuAgentOptions { Instructions = "x" },
            conversationStore: store,
            tools: [new ApprovalRequiredAIFunction(function)]);

        return new Harness(agent, store, channel, accessor, executedAppKeys);
    }

    private static string ConversationKey
        => ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");

    /// <summary>跑一轮事件，制造「框架已记录待审批请求」的前置状态。</summary>
    private static async Task<(Harness Harness, FrameworkToolApprovalRequest Approval)> ArrangePendingApprovalAsync(
        IChatClient client)
    {
        var harness = CreateHarness(client);
        var handler = new RecordingHandler(harness.Agent, CreateDeduplicator().Object, harness.Channel);

        await handler.HandleAsync(DemoEventData("evt-approval-1", "om_1"), default);

        harness.Channel.Requests.Should().ContainSingle("前置：首轮必须把待确认项交给宿主批准通道");
        return (harness, harness.Channel.Requests[0]);
    }

    [Fact]
    public async Task RunApprovalContinuation_ShouldExecuteApprovedTool_AndPersistSession()
    {
        var client = new ScriptedChatClient(ApprovalFirstResponse, "已完成写入");
        var (harness, approval) = await ArrangePendingApprovalAsync(client);

        approval.ToolName.Should().Be(WriteTool, "前置：审批要素必须来自框架改写后的真实工具调用");
        approval.RequestId.Should().Be("ficc_call-1", "RequestId 由框架按 ficc_{callId} 生成");
        approval.ChatId.Should().Be("oc_chat", "续跑轮重建工具上下文需要 chat_id（R5-11 追加要素）");

        var response = await harness.Agent.RunApprovalContinuationAsync(
            harness.Accessor, approval, approved: true, reason: "管理员已确认", cancellationToken: default);

        response.Text.Should().Contain("已完成", "续跑轮必须拿到模型最终回答");
        harness.Executed.Should().BeTrue(
            "批准后写工具必须真实执行——续跑轮重建了租户上下文（批准 ≠ 放行，但批准 + 上下文 = 放行）");
        harness.ExecutedAppKeys.Should().OnlyContain(key => key == "app-a",
            "工具执行期间的租户上下文必须由宿主显式重建并指向同一应用（TMA2-20）");

        var reloaded = await harness.Agent.GetOrCreateSessionAsync(ConversationKey);
        FeishuPendingApprovalState.Read(reloaded).Should().BeEmpty(
            "框架绑定层在续跑成功后消费掉待审批记录——迟到/重复的批准不得再被接受");
        (await harness.Store.GetAsync(ConversationKey)).Should().NotBeNullOrEmpty(
            "续跑轮必须落库（含本次工具执行的历史）");
    }

    [Fact]
    public async Task RunApprovalContinuation_ShouldFailFast_WhenToolContextAccessorMissing()
    {
        var client = new ScriptedChatClient(ApprovalFirstResponse, "已完成写入");
        var (harness, approval) = await ArrangePendingApprovalAsync(client);

        var act = async () => await harness.Agent.RunApprovalContinuationAsync(
            null!, approval, approved: true, cancellationToken: default);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "缺工具上下文访问器属装配缺陷：静默跳过会让工具 fail-closed 拒绝，表现为『批准了却不生效』");
    }

    [Fact]
    public async Task RunApprovalContinuation_ShouldFailClosed_WhenPendingApprovalMissing()
    {
        var client = new ScriptedChatClient(ApprovalFirstResponse, "已完成写入");
        var (harness, approval) = await ArrangePendingApprovalAsync(client);

        // 伪造一个框架从未记录过的 requestId（等价于「会话已重建 / 确认已被放弃」）。
        var stale = approval with { RequestId = "ficc_never-recorded" };
        var act = async () => await harness.Agent.RunApprovalContinuationAsync(
            harness.Accessor, stale, approved: true, cancellationToken: default);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "绝不凭空构造批准响应——未命中框架记录即 fail-closed");
        harness.Executed.Should().BeFalse("不得执行任何工具");
    }

    /// <summary>
    /// R7 / C4a：装配了待确认快照存储时，<b>已被消费/已过期</b>的批准必须 fail-closed 丢弃——
    /// 同一 <c>RequestId</c> 只生效一次（重复回灌批准不得二次执行写操作）。
    /// </summary>
    [Fact]
    public async Task RunApprovalContinuation_ShouldFailClosed_WhenSnapshotAlreadyConsumed()
    {
        var client = new ScriptedChatClient(ApprovalFirstResponse, "已完成写入");
        var (harness, approval) = await ArrangePendingApprovalAsync(client);

        // 模拟「该快照已被消费/已过期」：登记后立即消费掉（幂等消费只成功一次）。
        var store = new InMemoryPendingApprovalStore();
        await store.SaveAsync(PendingApprovalSnapshot.FromRequest(approval, DateTimeOffset.UtcNow));
        (await store.TryConsumeAsync(approval.AppKey, approval.RequestId)).Should().BeTrue(
            "前置：首次消费必须成功，否则本用例测的不是「已消费」路径");

        var act = async () => await harness.Agent.RunApprovalContinuationAsync(
            harness.Accessor, approval, approved: true, pendingApprovalStore: store, cancellationToken: default);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "已被消费的确认不得再次生效（同一 RequestId 只生效一次，fail-closed 不重放）");
        harness.Executed.Should().BeFalse("丢弃迟到批准 ⇒ 写工具绝不执行");
    }

    /// <summary>
    /// R7 / C4a：装配存储且快照<b>有效</b>时，续跑正常执行并把快照消费掉（不再出现在待办）。
    /// </summary>
    [Fact]
    public async Task RunApprovalContinuation_ShouldConsumeSnapshot_WhenApproved()
    {
        var client = new ScriptedChatClient(ApprovalFirstResponse, "已完成写入");
        var (harness, approval) = await ArrangePendingApprovalAsync(client);

        var store = new InMemoryPendingApprovalStore();
        await store.SaveAsync(PendingApprovalSnapshot.FromRequest(approval, DateTimeOffset.UtcNow));

        var response = await harness.Agent.RunApprovalContinuationAsync(
            harness.Accessor, approval, approved: true, pendingApprovalStore: store, cancellationToken: default);

        response.Text.Should().Contain("已完成");
        harness.Executed.Should().BeTrue("批准的写工具必须执行（批准 + 显式重建上下文 = 放行）");
        (await store.ListPendingAsync(approval.AppKey)).Should().BeEmpty(
            "续跑成功后快照必须被消费（否则待办列表会永久堆积已处理项）");
    }

    [Fact]
    public async Task RunApprovalContinuation_ShouldRejectIncompleteApprovalElements()
    {
        var client = new ScriptedChatClient(ApprovalFirstResponse, "已完成写入");
        var (harness, approval) = await ArrangePendingApprovalAsync(client);

        var missingKey = async () => await harness.Agent.RunApprovalContinuationAsync(
            harness.Accessor, approval with { ConversationKey = null }, approved: true, cancellationToken: default);
        var missingAppKey = async () => await harness.Agent.RunApprovalContinuationAsync(
            harness.Accessor, approval with { AppKey = string.Empty }, approved: true, cancellationToken: default);

        await missingKey.Should().ThrowAsync<ArgumentException>("无会话键即无法定位待续跑的会话");
        await missingAppKey.Should().ThrowAsync<ArgumentException>("无 appKey 禁止默认应用兜底（TMA2-20）");
    }

    [Fact]
    public async Task RunApprovalContinuation_ShouldSerializeWithEventPath_WhenGateProvided()
    {
        var client = new ScriptedChatClient(ApprovalFirstResponse, "已完成写入");
        var (harness, approval) = await ArrangePendingApprovalAsync(client);

        var handle = new Mock<IConversationGateHandle>();
        var gate = new Mock<IConversationGate>();
        gate.Setup(g => g.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(handle.Object);

        await harness.Agent.RunApprovalContinuationAsync(
            harness.Accessor, approval, approved: true, conversationGate: gate.Object, cancellationToken: default);

        gate.Verify(g => g.AcquireAsync(ConversationKey, It.IsAny<CancellationToken>()), Times.Once,
            "续跑轮与事件轮共享同一会话状态，必须以同键闸门串行");
        handle.Verify(h => h.Dispose(), Times.Once, "闸门句柄必须释放（finally 语义）");
    }

    [Fact]
    public async Task RunApprovalContinuation_ShouldReject_WhenNotApproved()
    {
        var client = new ScriptedChatClient(ApprovalFirstResponse, "好的，已取消该操作");
        var (harness, approval) = await ArrangePendingApprovalAsync(client);

        var response = await harness.Agent.RunApprovalContinuationAsync(
            harness.Accessor, approval, approved: false, reason: "权限不足", cancellationToken: default);

        response.Text.Should().Contain("已取消", "拒绝结果必须回填模型（框架生成『调用被拒绝』结果后模型继续作答）");
        harness.Executed.Should().BeFalse("拒绝 ⇒ 写工具绝不执行");
    }

    /// <summary>
    /// R5-12：<b>无人续跑</b>时新用户轮次必须自愈，而不是把会话永久毒化。
    /// </summary>
    /// <remarks>
    /// 修复前形态：待应答的审批请求残留在会话历史里，MEAI <c>FunctionInvokingChatClient</c> 对
    /// <b>整段入站历史</b>做配对校验并直接抛 <c>InvalidOperationException</c>
    /// （"ToolApprovalRequestContent found with FunctionCall.CallId(s) … that have no matching
    /// ToolApprovalResponseContent."）——异常早于任何新消息落库 ⇒ 该会话此后每一轮都抛。
    /// </remarks>
    [Fact]
    public async Task HandleAsync_ShouldAbandonUnansweredApproval_AndKeepConversationUsable()
    {
        var client = new ScriptedChatClient(ApprovalFirstResponse, "本轮正常回答");
        var (harness, _) = await ArrangePendingApprovalAsync(client);
        var handler = new RecordingHandler(harness.Agent, CreateDeduplicator().Object, harness.Channel);

        // 用户没去批准，直接发了新消息：必须自愈（放弃待确认项），正常回答。
        var act = async () => await handler.HandleAsync(DemoEventData("evt-approval-2", "om_2"), default);

        await act.Should().NotThrowAsync("残留的未应答审批不得毒化会话（框架会对整段历史做配对校验）");
        handler.LastReply.Should().Be("本轮正常回答", "放弃待确认项后本轮必须正常走模型");

        // 孤儿审批内容必须已从落盘历史中摘除，且框架的待审批记录必须已清空（否则迟到的批准仍会放行）。
        var reloaded = await harness.Agent.GetOrCreateSessionAsync(ConversationKey);
        FeishuPendingApprovalState.HasPending(reloaded).Should().BeFalse(
            "自愈后：历史中不得残留孤儿审批请求，框架记录也必须清空（迟到批准一律 fail-closed 丢弃）");

        var stored = await harness.Store.GetAsync(ConversationKey);
        stored.Should().NotContain("ficc_call-1", "落盘载荷里不得再残留未应答审批的请求标识");
        harness.Executed.Should().BeFalse("放弃 ≠ 放行：写工具仍不得执行");
    }
}
