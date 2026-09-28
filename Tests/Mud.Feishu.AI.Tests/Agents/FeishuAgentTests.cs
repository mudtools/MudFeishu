// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Mud.Feishu.Abstractions.Observability;

namespace Mud.Feishu.AI.Tests.Agents;

/// <summary>
/// <see cref="FeishuAgent"/> 单轮闭环（Moq <see cref="IChatClient"/>，不真实调模型——Phase 0 §7）。
/// </summary>
public class FeishuAgentTests
{
    private static Mock<IChatClient> CreateMockClient(string replyText)
    {
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, replyText)));
        return mock;
    }

    private static FeishuAgentOptions ValidOptions() => new()
    {
        Instructions = "你是飞书测试助手",
        MaxHistoryMessages = 10,
    };

    [Fact]
    public async Task RunAsync_ShouldReturnModelText_WhenBareModelCall()
    {
        var mock = CreateMockClient("你好，我是飞书助手");
        var agent = new FeishuAgent(mock.Object, ValidOptions());

        var session = await agent.CreateSessionAsync();
        var response = await agent.RunAsync("你好", session);

        response.Text.Should().Be("你好，我是飞书助手");
        mock.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldPassInstructionsViaChatOptions()
    {
        var mock = CreateMockClient("ok");
        var agent = new FeishuAgent(mock.Object, ValidOptions());

        var session = await agent.CreateSessionAsync();
        await agent.RunAsync("查询", session);

        mock.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.Is<ChatOptions?>(o => o != null && o.Instructions == ValidOptions().Instructions),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Ctor_ShouldRejectInvalidOptions_FailFast()
    {
        var mock = CreateMockClient("ok");

        var act = () => new FeishuAgent(mock.Object, new FeishuAgentOptions { Instructions = "" });

        act.Should().Throw<InvalidOperationException>("Phase 0 §8：工厂启动即校验，不透传非法状态");
    }

    [Fact]
    public async Task SessionPersistence_ShouldRoundTripThroughStore()
    {
        var mock = CreateMockClient("ok");
        var store = new MemoryConversationStore();
        var agent = new FeishuAgent(mock.Object, ValidOptions(), conversationStore: store);
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.P2P(), "ou_1");

        var first = await agent.GetOrCreateSessionAsync(key);
        await agent.RunAsync("第一轮", first);
        await agent.SaveSessionAsync(key, first);

        var second = await agent.GetOrCreateSessionAsync(key);
        second.Should().NotBeSameAs(first, "持久化后读取的是反序列化的新实例");
        second.GetType().Should().Be(first.GetType());
    }

    [Fact]
    public async Task GetOrCreateSession_ShouldRebuild_WhenStoredPayloadCorrupted()
    {
        var mock = CreateMockClient("ok");
        var store = new MemoryConversationStore();
        var agent = new FeishuAgent(mock.Object, ValidOptions(), conversationStore: store);
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_9");

        await store.SaveAsync(key, "{not-valid-json");

        var session = await agent.GetOrCreateSessionAsync(key);
        session.Should().NotBeNull("损坏载荷按 miss 处理并重建会话（Phase 0 §8）");
    }

    [Fact]
    public async Task SaveSession_ShouldBeNoOp_WithoutStore()
    {
        var mock = CreateMockClient("ok");
        var agent = new FeishuAgent(mock.Object, ValidOptions());
        var session = await agent.CreateSessionAsync();

        var act = () => agent.SaveSessionAsync("feishu:app-a:conversation:chat:oc_1", session);

        await act.Should().NotThrowAsync("无存储时持久化为空操作");
    }

    [Fact]
    public async Task RunStreaming_ShouldYieldUpdates()
    {
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(StreamChunks("你", "好"));

        var agent = new FeishuAgent(mock.Object, ValidOptions());
        var session = await agent.CreateSessionAsync();

        var collected = new List<string>();
        await foreach (var update in agent.RunStreamingAsync("流式", session))
        {
            collected.Add(update.Text ?? string.Empty);
        }

        string.Concat(collected).Should().Be("你好");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamChunks(
        params string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
            await Task.Yield();
        }
    }

    // ───────────────────── P1-4：坏载荷自愈（毒事件循环） ─────────────────────

    /// <summary>
    /// 合法 JSON 但根不是对象（<c>"[]"</c>）：MAF <c>ChatClientAgentSession.Deserialize</c> 抛
    /// <see cref="ArgumentException"/>——若 catch 面只覆盖 <see cref="JsonException"/>，
    /// 坏载荷会在每次重投递时再次失败（毒事件循环）。
    /// </summary>
    [Fact]
    public async Task GetOrCreateSession_ShouldRebuild_WhenStoredPayloadIsJsonArray()
    {
        var mock = CreateMockClient("ok");
        var store = new MemoryConversationStore();
        var agent = new FeishuAgent(mock.Object, ValidOptions(), conversationStore: store);
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_array");

        await store.SaveAsync(key, "[]");

        var session = await agent.GetOrCreateSessionAsync(key);

        session.Should().NotBeNull("非对象根载荷必须按 miss 处理并重建（否则毒事件循环）");
    }

    [Fact]
    public async Task GetOrCreateSession_ShouldDeleteCorruptPayload_WhenRebuild()
    {
        var mock = CreateMockClient("ok");
        var store = new MemoryConversationStore();
        var agent = new FeishuAgent(mock.Object, ValidOptions(), conversationStore: store);
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_corrupt");

        await store.SaveAsync(key, "[]");
        await agent.GetOrCreateSessionAsync(key);

        var remaining = await store.GetAsync(key);
        remaining.Should().BeNull("坏键必须被删除——否则旧格式/坏载荷永远不会自愈");
    }

    // ───────────────────── P2-1 / P2-2：MAF 基类契约一致性 ─────────────────────

    [Fact]
    public void Name_ShouldBeConsistent_ThroughAIAgentReference()
    {
        var agent = new FeishuAgent(CreateMockClient("ok").Object, ValidOptions());

        AIAgent asBase = agent;

        asBase.Name.Should().Be(ValidOptions().Name,
            "Name 必须 override：用 new 遮蔽会让经 AIAgent 引用（含 MAF 内部诊断）取到基类默认 null");
    }

    [Fact]
    public void GetService_ShouldReturnSelf_ForAIAgentRequest()
    {
        var agent = new FeishuAgent(CreateMockClient("ok").Object, ValidOptions());

        agent.GetService(typeof(AIAgent)).Should().BeSameAs(agent,
            "先判自身：否则返回内部 ChatClientAgent，宿主经该引用将绕过飞书遥测与摘要");
    }

    // ───────────────────── P2-6：摘要启用条件（token-only 配置） ─────────────────────

    [Fact]
    public async Task SummaryConfiguration_TokenOnly_ShouldStillSummarize()
    {
        var mock = CreateMockClient("纪要：要点。");
        var agent = new FeishuAgent(mock.Object, new FeishuAgentOptions
        {
            Instructions = "x",
            SummaryThreshold = 0,       // 条数阈值禁用
            MaxHistoryTokens = 20,      // 仅 token 预算（P2-6 起必须真正生效）
            MaxHistoryMessages = 8,
        });
        var session = await agent.CreateSessionAsync();

        var history = new List<ChatMessage>();
        for (var i = 0; i < 6; i++)
        {
            history.Add(new ChatMessage(
                i % 2 == 0 ? ChatRole.User : ChatRole.Assistant, $"[{i}]" + new string('x', 100)));
        }

        session.SetInMemoryChatHistory(history, FeishuAgent.ChatHistoryStateKey, null);
        await agent.RunAsync("查询", session);

        mock.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2),
            "token-only 配置必须挂载摘要器（摘要调用 + 主对话调用）；否则该配置静默失效");
    }

    [Fact]
    public async Task SummaryConfiguration_BothDisabled_ShouldNotSummarize()
    {
        var mock = CreateMockClient("回答");
        var agent = new FeishuAgent(mock.Object, new FeishuAgentOptions
        {
            Instructions = "x",
            SummaryThreshold = 0,
            MaxHistoryTokens = 0,       // 双禁用（既有语义：仅保留裁剪窗行为）
            MaxHistoryMessages = 8,
        });
        var session = await agent.CreateSessionAsync();

        var history = new List<ChatMessage>();
        for (var i = 0; i < 20; i++)
        {
            history.Add(new ChatMessage(
                i % 2 == 0 ? ChatRole.User : ChatRole.Assistant, $"[{i}]" + new string('x', 100)));
        }

        session.SetInMemoryChatHistory(history, FeishuAgent.ChatHistoryStateKey, null);
        await agent.RunAsync("查询", session);

        mock.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once, "双禁用时不挂载摘要器（零额外模型调用）");
    }

    // ───────────────────── R2-2：会话恢复期急切校验历史状态（闭合 R1 P1-4 残余守护面） ─────────────────────

    /// <summary>
    /// 状态袋内的历史值损坏（外层 JSON 合法、内层类型不符）——形状与真实落库一致：
    /// <c>{"stateBag":{"feishu.agent.history":{"messages":[...]}}}</c>。
    /// 惰性解析使其只在<b>首次类型化读取</b>时才抛，故必须落在会话入口的守护区内。
    /// </summary>
    private const string CorruptedInnerStatePayload =
        "{\"stateBag\":{\"feishu.agent.history\":{\"messages\":\"oops\"}}}";

    [Fact]
    public async Task GetOrCreateSessionAsync_ShouldDeleteAndRebuild_WhenInnerStateCorrupted()
    {
        var mock = CreateMockClient("ok");
        var store = new MemoryConversationStore();
        var agent = new FeishuAgent(mock.Object, ValidOptions(), store);
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.P2P(), "ou_corrupt");

        // 直接写入「外层合法 + 内层损坏」的载荷（等价于历史数据在库中被破坏/跨版本残留）。
        await store.SaveAsync(key, CorruptedInnerStatePayload);

        var restored = await agent.GetOrCreateSessionAsync(key);

        restored.Should().NotBeNull("损坏载荷必须按 miss 处理并重建，绝不把异常抛给事件循环");
        (await store.GetAsync(key)).Should().BeNull(
            "坏值必须被删除——否则每次重投递都在同一处抛，该会话在 TTL 内永久毒化");
    }

    [Fact]
    public async Task GetOrCreateSessionAsync_ShouldKeepUsableHistory_WhenStateIntact()
    {
        // 正向锁定：急切校验不得破坏健康会话（防"多读一次"演变成"丢历史"）。
        var mock = CreateMockClient("ok");
        var store = new MemoryConversationStore();
        var agent = new FeishuAgent(mock.Object, ValidOptions(), store);
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.P2P(), "ou_intact");

        var session = await agent.GetOrCreateSessionAsync(key);
        session.SetInMemoryChatHistory(
            [new ChatMessage(ChatRole.User, "第一轮"), new ChatMessage(ChatRole.Assistant, "回复一")],
            FeishuAgent.ChatHistoryStateKey, null);
        await agent.SaveSessionAsync(key, session);

        var restored = await agent.GetOrCreateSessionAsync(key);
        restored.TryGetInMemoryChatHistory(out var history, FeishuAgent.ChatHistoryStateKey, null)
            .Should().BeTrue("健康会话的历史必须原样恢复");
        history!.Should().HaveCount(2);
        (await store.GetAsync(key)).Should().NotBeNull("健康载荷不得被误删");
    }

    // ───────────────────── R2-6：Id 对齐 MAF DelegatingAIAgent 约定 ─────────────────────

    [Fact]
    public async Task Id_ShouldMatchInnerAgent_SoResponseAgentIdIsTraceable()
    {
        var mock = CreateMockClient("ok");
        var agent = new FeishuAgent(mock.Object, ValidOptions());

        var inner = agent.GetService(typeof(ChatClientAgent)) as ChatClientAgent;
        inner.Should().NotBeNull();

        agent.Id.Should().Be(inner!.Id,
            "FeishuAgent 与内层 ChatClientAgent 是同一逻辑 Agent 的两层，Id 必须一致（对齐 MAF DelegatingAIAgent.IdCore）");

        var session = await agent.CreateSessionAsync();
        var response = await agent.RunAsync("你好", session);

        response.AgentId.Should().Be(agent.Id,
            "AgentResponse.AgentId 由内层 Id 打标；Id 不一致会让宿主按 AgentId 关联指标时得到「未知 Agent」");
    }

    // ───────────────────── P2-8 / B3.3：失败路径可见性 ─────────────────────

    [Fact]
    public async Task RunAsync_ShouldRecordErrorStatusAndDuration_OnFailure()
    {
        var stopped = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == FeishuActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => stopped.Enqueue(activity),
        };
        ActivitySource.AddActivityListener(listener);

        var durations = new ConcurrentQueue<double>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Name == "feishu.agent.llm.duration")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };
        meterListener.SetMeasurementEventCallback<double>((_, value, _, _) => durations.Enqueue(value));
        meterListener.Start();

        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("模型网关 5xx"));
        var agent = new FeishuAgent(mock.Object, ValidOptions());
        var session = await agent.CreateSessionAsync();

        var act = () => agent.RunAsync("失败", session);
        await act.Should().ThrowAsync<HttpRequestException>("异常语义原样保留（不包装）");

        stopped.Should().Contain(
            a => a.OperationName == "feishu.agent.run" && a.Status == ActivityStatusCode.Error,
            "失败路径必须把 Span 标为 Error（否则失败在链路里不可见）");
        durations.Should().NotBeEmpty(
            "失败样本也必须进入 feishu.agent.llm.duration（否则 P95/P99 只反映成功路径）");
    }

    [Fact]
    public async Task RunStreaming_ShouldStopActivity_WhenConsumerBreaksEarly()
    {
        // 消费方提前断开时迭代器被 Dispose，using Activity 不得泄漏或抛异常。
        var mock = new Mock<IChatClient>();
        static async IAsyncEnumerable<ChatResponseUpdate> InfiniteStream(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                yield return new ChatResponseUpdate(ChatRole.Assistant, "x");
                await Task.Yield();
            }
        }

        mock.Setup(c => c.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(InfiniteStream());

        var agent = new FeishuAgent(mock.Object, ValidOptions());
        var session = await agent.CreateSessionAsync();

        var act = async () =>
        {
            await foreach (var _ in agent.RunStreamingAsync("流式", session))
            {
                break;
            }
        };

        await act.Should().NotThrowAsync();
    }
}
