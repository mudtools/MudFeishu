// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.AI.Abstractions.Channels;
using Mud.AI.Abstractions.Configuration;
using Mud.AI.Abstractions.Context;
using Mud.AI.Abstractions.Observability;
using Mud.AI.Agent.Events;

namespace Mud.AI.Agent.Tests.Events;

/// <summary>
/// 中立回合管线（<see cref="ConversationalTurnPipeline"/>）的行为用例：
/// <b>替身实现 5 端口</b>（会话门面 / 回复端口 / 作用域端口 / 通道 / 遥测），不引用任何飞书工程。
/// </summary>
/// <remarks>
/// <para>
/// <b>N4 DoD</b>：产品侧端到端行为由 <c>Mud.Feishu.AI.Tests/Events/*</c> 既有用例锁定（全部保持绿）；
/// 本文件补齐中立包侧的独立覆盖（Q1「测试随类型走」），并承载
/// <c>ToolContext_Begin_ShouldPrecedeContextAssembly</c> 的<b>行为锚</b>（§6.2：替身记录调用序号，
/// 断言工具作用域先于装配）。
/// </para>
/// </remarks>
public class ConversationalTurnPipelineTests
{
    // ────────────────────────────────────────────────────────────────────
    // 替身（stub）
    // ────────────────────────────────────────────────────────────────────

    private sealed class TestSession : AgentSession
    {
    }

    /// <summary>脚本化模型替身：Run/RunStreaming 计数 + 固定回答 + 首条用户消息捕获。</summary>
    private sealed class StubAgent : AIAgent
    {
        public int RunCount;
        public int StreamCount;
        public string ResponseText = "模型回答";
        public string? LastUserMessage;

        protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
            => new(new TestSession());

        protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
            AgentSession session, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
            => new(JsonDocument.Parse("{}").RootElement.Clone());

        protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
            JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
            => new(new TestSession());

        protected override Task<AgentResponse> RunCoreAsync(
            IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
        {
            RunCount++;
            LastUserMessage = messages.FirstOrDefault()?.Text;
            return Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, ResponseText)));
        }

        protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
            IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            RunCount++;
            StreamCount++;
            LastUserMessage = messages.FirstOrDefault()?.Text;
            await Task.Yield();
            yield return new AgentResponseUpdate(ChatRole.Assistant, ResponseText);
        }
    }

    /// <summary>会话门面替身：单会话 + 取/存计数。</summary>
    private sealed class StubSessions : IAgentSessionFacade
    {
        public int GetCount;
        public int SaveCount;
        public AgentSession Session { get; } = new TestSession();

        public string HistoryStateKey => "test.history";

        public Task<AgentSession> GetOrCreateSessionAsync(string conversationKey, CancellationToken cancellationToken = default)
        {
            GetCount++;
            return Task.FromResult(Session);
        }

        public Task SaveSessionAsync(string conversationKey, AgentSession session, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>回复端口替身：记录下发文本。</summary>
    private sealed class StubReplySink : IConversationReplySink
    {
        public List<string> Delivered { get; } = [];

        public Task DeliverAsync(ConversationRequest request, string responseText, CancellationToken cancellationToken = default)
        {
            Delivered.Add(responseText);
            return Task.CompletedTask;
        }
    }

    /// <summary>作用域端口替身：记录进出（行为锚用）。</summary>
    private sealed class StubScopeFactory : ITurnScopeFactory
    {
        public List<string> Trace { get; }
        public List<TurnScopeContext> Contexts { get; } = [];

        public StubScopeFactory(List<string> trace) => Trace = trace;

        public IDisposable BeginToolScope(TurnScopeContext context)
        {
            Contexts.Add(context);
            Trace.Add("scope:begin");
            return new Scope(Trace);
        }

        private sealed class Scope(List<string> trace) : IDisposable
        {
            public void Dispose() => trace.Add("scope:end");
        }
    }

    /// <summary>装配器替身：记录调用并返回片段。</summary>
    private sealed class StubAssembler(int order, string? fragment, List<string>? trace = null) : IContextAssembler
    {
        public int Order => order;

        public Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
        {
            trace?.Add("assemble");
            return Task.FromResult(fragment);
        }
    }

    /// <summary>通道替身：记录 Begin/Write/Flush。</summary>
    private sealed class StubChannel : IMessageChannel
    {
        public Exception? BeginException;
        public List<string> Writes { get; } = [];
        public int FlushCount;
        public string? BeginTarget;
        public string? BeginTenantKey;

        public Task<string> BeginAsync(string tenantKey, string chatId, CancellationToken cancellationToken = default)
        {
            if (BeginException is not null)
            {
                throw BeginException;
            }

            BeginTenantKey = tenantKey;
            BeginTarget = chatId;
            return Task.FromResult("m-1");
        }

        public Task WriteStreamAsync(string tenantKey, string chatId, string messageId, string delta, CancellationToken cancellationToken = default)
        {
            Writes.Add(delta);
            return Task.CompletedTask;
        }

        public Task FlushAsync(string tenantKey, string chatId, string messageId, CancellationToken cancellationToken = default)
        {
            FlushCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>遥测替身：记录全部事件点。</summary>
    private sealed class RecordingTelemetry : IAgentTelemetry
    {
        public int StartTurnCount;
        public int ReplayedCount;
        public int StreamedCount;
        public double? GateWaitedMs;

        public IDisposable? StartTurn(string conversationKey, string? tenantKey)
        {
            StartTurnCount++;
            return new Scope();
        }

        public void GateWaited(IDisposable? turn, double milliseconds) => GateWaitedMs = milliseconds;

        public void Replayed(IDisposable? turn) => ReplayedCount++;

        public void Streamed(IDisposable? turn) => StreamedCount++;

        public void LlmDuration(string agentName, long milliseconds)
        {
        }

        public void Degraded(string reason)
        {
        }

        public IDisposable? StartSummarize(int summarizedCount, int retainedCount) => null;

        public void SummarizeCompleted(IDisposable? scope, bool converged)
        {
        }

        public IDisposable? StartRun(string agentName, bool streaming) => null;

        public void RunCompleted(IDisposable? scope, bool failed, long? inputTokens, long? outputTokens, long? totalTokens)
        {
        }

        private sealed class Scope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 夹具
    // ────────────────────────────────────────────────────────────────────

    private static ConversationRequest NewRequest(
        string messageId = "msg-1",
        string? mentionedText = "你好",
        string? chatId = "chat-1")
        => new(
            AppKey: "app-1",
            Scope: ConversationScope.Group(),
            SubjectId: "chat-1",
            SenderId: "user-1",
            MessageId: messageId,
            MentionedText: mentionedText,
            ChatId: chatId);

    private static ConversationalTurnPipeline NewPipeline(
        StubAgent agent,
        StubSessions sessions,
        StubReplySink sink,
        IAgentTelemetry? telemetry = null,
        IReadOnlyList<IContextAssembler>? assemblers = null,
        Func<string, CancellationToken, Task<IDisposable?>>? acquireGate = null,
        IMessageChannel? channel = null,
        ITurnScopeFactory? scopeFactory = null,
        ConversationalTurnOptions? turnOptions = null)
        => new(
            agent,
            new AgentRuntimeOptions { MaxToolCallsPerTurn = 0 },
            sessions,
            keyBuilder: request => $"{request.AppKey}|{request.SubjectId}",
            telemetry: telemetry,
            assemblers: assemblers,
            acquireGate: acquireGate,
            channel: channel,
            scopeFactory: scopeFactory,
            turnOptions: turnOptions);

    // ────────────────────────────────────────────────────────────────────
    // 用例
    // ────────────────────────────────────────────────────────────────────

    /// <summary>R2-8：非流式路径必须「先落库、后回复」。</summary>
    [Fact]
    public async Task RunTurnAsync_HappyPath_ShouldSaveBeforeDeliver()
    {
        var agent = new StubAgent();
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var telemetry = new RecordingTelemetry();
        var pipeline = NewPipeline(agent, sessions, sink, telemetry);

        var outcome = await pipeline.RunTurnAsync(NewRequest(), sink);

        agent.RunCount.Should().Be(1, "正常路径恰好一次模型调用");
        sink.Delivered.Should().ContainSingle("模型回答");
        sessions.SaveCount.Should().Be(2, "下发前一次（含 outbox 条目）+ 送达成功摘除条目后一次");
        outcome.ResponseText.Should().Be("模型回答");
        outcome.Streamed.Should().BeFalse();
        outcome.Replayed.Should().BeFalse();
        telemetry.ReplayedCount.Should().Be(0);
        telemetry.StreamedCount.Should().Be(0);
        telemetry.StartTurnCount.Should().Be(1, "每轮恰好一个遥测作用域");
    }

    /// <summary>P4-4（outbox）：同一轮次重投递 ⇒ 补发文本、不重跑模型。</summary>
    [Fact]
    public async Task RunTurnAsync_OutboxReplay_ShouldDeliverWithoutRerun()
    {
        var agent = new StubAgent();
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var telemetry = new RecordingTelemetry();
        var options = new ConversationalTurnOptions
        {
            PendingReplyStateKey = "test.pending_reply",
            PendingReplyTurnStateKey = "test.pending_reply_turn",
        };
        var pipeline = NewPipeline(agent, sessions, sink, telemetry, turnOptions: options);

        // 预置待补发条目（轮次 = 当前请求的 MessageId）。
        sessions.Session.StateBag.SetValue("test.pending_reply", "上一轮回复", null);
        sessions.Session.StateBag.SetValue("test.pending_reply_turn", "msg-1", null);

        var outcome = await pipeline.RunTurnAsync(NewRequest(), sink);

        agent.RunCount.Should().Be(0, "补发不重跑模型（省一次计费）");
        sink.Delivered.Should().ContainSingle().Which.Should().Be("上一轮回复");
        outcome.Replayed.Should().BeTrue();
        telemetry.ReplayedCount.Should().Be(1);
        sessions.Session.StateBag.TryGetValue<string>("test.pending_reply", out var remaining, null).Should().BeFalse(
            "补发条目已在内存中摘除（随本次落库持久化）");
    }

    /// <summary>P4-4（outbox）：轮次不匹配 ⇒ 陈旧条目丢弃、正常走模型。</summary>
    [Fact]
    public async Task RunTurnAsync_StaleOutboxEntry_ShouldRunModel()
    {
        var agent = new StubAgent();
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var options = new ConversationalTurnOptions
        {
            PendingReplyStateKey = "test.pending_reply",
            PendingReplyTurnStateKey = "test.pending_reply_turn",
        };
        var pipeline = NewPipeline(agent, sessions, sink, turnOptions: options);

        sessions.Session.StateBag.SetValue("test.pending_reply", "陈旧回复", null);
        sessions.Session.StateBag.SetValue("test.pending_reply_turn", "另一轮", null);

        var outcome = await pipeline.RunTurnAsync(NewRequest(), sink);

        agent.RunCount.Should().Be(1, "陈旧条目答非所问，必须正常走模型");
        sink.Delivered.Should().ContainSingle().Which.Should().Be("模型回答");
        outcome.Replayed.Should().BeFalse();
    }

    /// <summary>R2-01：前置判定命中 ⇒ 模型前短路（不调模型、不落历史）。</summary>
    [Fact]
    public async Task RunTurnAsync_DeliveryBlock_ShouldShortCircuitBeforeModel()
    {
        var agent = new StubAgent();
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var options = new ConversationalTurnOptions
        {
            FindDeliveryBlockAsync = (_, _) => Task.FromResult<string?>("投递目标不存在"),
        };
        var pipeline = NewPipeline(agent, sessions, sink, turnOptions: options);

        var outcome = await pipeline.RunTurnAsync(NewRequest(), sink);

        agent.RunCount.Should().Be(0);
        sink.Delivered.Should().BeEmpty();
        sessions.SaveCount.Should().Be(0, "短路发生在任何落库之前");
        outcome.ResponseText.Should().BeNull();
    }

    /// <summary>P2-7：空用户消息守卫（触发文本与装配器都空）⇒ 不调模型。</summary>
    [Fact]
    public async Task RunTurnAsync_EmptyUserMessage_ShouldSkipModel()
    {
        var agent = new StubAgent();
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var pipeline = NewPipeline(agent, sessions, sink);

        var outcome = await pipeline.RunTurnAsync(NewRequest(mentionedText: null), sink);

        agent.RunCount.Should().Be(0, "空 user 消息会被 OpenAI 兼容端点 400 拒绝且已计费");
        sink.Delivered.Should().BeEmpty();
    }

    /// <summary>R2-9：空模型回复 ⇒ 落库但不下发。</summary>
    [Fact]
    public async Task RunTurnAsync_EmptyModelReply_ShouldSaveButNotDeliver()
    {
        var agent = new StubAgent { ResponseText = "   " };
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var pipeline = NewPipeline(agent, sessions, sink);

        var outcome = await pipeline.RunTurnAsync(NewRequest(), sink);

        agent.RunCount.Should().Be(1);
        sessions.SaveCount.Should().Be(1, "历史已落库（按已消费处理）");
        sink.Delivered.Should().BeEmpty("空回复下发会让平台 API 报错 → 重投递重跑模型");
    }

    /// <summary>
    /// 行为锚（§6.2 / P0-1）：工具执行上下文 <b>BeginToolScope</b> 必须先于装配器 <b>AssembleAsync</b>
    /// ——顺序颠倒会让知识/引用类装配器读不到租户上下文（RAG 注入模式永久静默失效）。
    /// </summary>
    [Fact]
    public async Task RunTurnAsync_ShouldBeginToolScopeBeforeContextAssembly()
    {
        var trace = new List<string>();
        var agent = new StubAgent();
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var scopeFactory = new StubScopeFactory(trace);
        var assembler = new StubAssembler(0, "片段", trace);
        var pipeline = NewPipeline(agent, sessions, sink, assemblers: [assembler], scopeFactory: scopeFactory);

        await pipeline.RunTurnAsync(NewRequest(), sink);

        var beginIndex = trace.IndexOf("scope:begin");
        var assembleIndex = trace.IndexOf("assemble");
        beginIndex.Should().BeGreaterThan(-1);
        assembleIndex.Should().BeGreaterThan(-1);
        beginIndex.Should().BeLessThan(assembleIndex, "工具上下文必须先于上下文装配生效");
        trace.Should().Contain("scope:end", "作用域必须随回合释放（恢复 AsyncLocal 进入前的值）");

        var context = scopeFactory.Contexts.Single();
        context.TenantKey.Should().Be("app-1");
        context.ConversationKey.Should().Be("app-1|chat-1");
        context.ChatId.Should().Be("chat-1");
        context.UserId.Should().Be("user-1");
    }

    /// <summary>R2-01b：触发文本在前、装配器片段在后（事件触发文本永远进得了模型）。</summary>
    [Fact]
    public async Task RunTurnAsync_ShouldAssembleTriggerTextFirst()
    {
        var agent = new StubAgent();
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var assembler = new StubAssembler(0, "知识片段");
        var pipeline = NewPipeline(agent, sessions, sink, assemblers: [assembler]);

        await pipeline.RunTurnAsync(NewRequest(mentionedText: "用户提问"), sink);

        agent.LastUserMessage.Should().Be("用户提问\n知识片段",
            "触发文本必须在前（R2-01b），装配器片段在后");
    }

    /// <summary>③ 式闸门委托：每轮恰好一次获取、finally 释放。</summary>
    [Fact]
    public async Task RunTurnAsync_ShouldAcquireAndReleaseGate()
    {
        var agent = new StubAgent();
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var acquired = 0;
        var released = 0;
        var pipeline = NewPipeline(
            agent, sessions, sink,
            acquireGate: (_, _) =>
            {
                acquired++;
                return Task.FromResult<IDisposable?>(new Gate(() => released++));
            });

        await pipeline.RunTurnAsync(NewRequest(), sink);

        acquired.Should().Be(1);
        released.Should().Be(1, "保存完成后（含失败路径）放行；同键下一事件方可进入");
    }

    /// <summary>流式路径：Begin → Write → Flush，回复端口不再被调用。</summary>
    [Fact]
    public async Task RunTurnAsync_Streaming_ShouldStreamAndSkipReply()
    {
        var agent = new StubAgent { ResponseText = "流式回答" };
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var telemetry = new RecordingTelemetry();
        var channel = new StubChannel();
        var pipeline = NewPipeline(agent, sessions, sink, telemetry, channel: channel);

        var outcome = await pipeline.RunTurnAsync(NewRequest(), sink);

        agent.StreamCount.Should().Be(1);
        channel.BeginTarget.Should().Be("chat-1", "P2D-2b：流式目标恒为 request.ChatId");
        channel.Writes.Should().ContainSingle().Which.Should().Be("流式回答");
        channel.FlushCount.Should().Be(1);
        sink.Delivered.Should().BeEmpty("流式路径已由通道送达，不再走回复端口");
        telemetry.StreamedCount.Should().Be(1);
        outcome.Streamed.Should().BeTrue();
        sessions.SaveCount.Should().Be(1, "流式落库恰一次（不可取消令牌路径）");
    }

    /// <summary>R5-1：流式 Begin 失败 ⇒ 回退非流式（共用同一回合实现，模型照常调用）。</summary>
    [Fact]
    public async Task RunTurnAsync_ChannelBeginFailure_ShouldFallbackToNonStreaming()
    {
        var agent = new StubAgent();
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var channel = new StubChannel { BeginException = new InvalidOperationException("占位创建失败") };
        var pipeline = NewPipeline(agent, sessions, sink, channel: channel);

        var outcome = await pipeline.RunTurnAsync(NewRequest(), sink);

        agent.RunCount.Should().Be(1, "Begin 失败时模型尚未调用，回退非流式（零重复成本）");
        sink.Delivered.Should().ContainSingle().Which.Should().Be("模型回答");
        outcome.Streamed.Should().BeFalse();
    }

    /// <summary>状态键未注入 ⇒ outbox 禁用（中立独立装配形态），无键操作不抛。</summary>
    [Fact]
    public async Task RunTurnAsync_WithoutOutboxKeys_ShouldDisableReplay()
    {
        var agent = new StubAgent();
        var sessions = new StubSessions();
        var sink = new StubReplySink();
        var pipeline = NewPipeline(agent, sessions, sink);

        // 即使状态袋里恰好有同键值（不同宿主恰好用同名键），也不应被当作补发条目消费。
        sessions.Session.StateBag.SetValue("test.pending_reply", "遗留值", null);

        var outcome = await pipeline.RunTurnAsync(NewRequest(), sink);

        agent.RunCount.Should().Be(1, "outbox 未配置 ⇒ 恒 miss，正常走模型");
        outcome.Replayed.Should().BeFalse();
        sink.Delivered.Should().ContainSingle().Which.Should().Be("模型回答");
    }

    private sealed class Gate(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
