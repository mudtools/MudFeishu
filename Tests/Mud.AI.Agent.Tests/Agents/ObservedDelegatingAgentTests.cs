// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Agents;

/// <summary>
/// 运行期编织基类的四条运行期契约：遥测三件套（Span / 用量 / 耗时）、失败标注后<b>原样抛出</b>、
/// 流式路径用量取自 <c>UsageContent</c>、<b>摘要先于模型调用</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须在这里断言</b>：编织逻辑已从产品类下沉到本基类，产品用例只能看到"最终行为对"
/// （例如摘要真的发生了），证不了"先于 Run"这条<b>次序</b>契约——基类若把摘要放到 Run 之后，
/// 本轮历史仍会越限，而产品用例照样可能绿。
/// </para>
/// <para>
/// <b>形态</b>：一律行为断言（驱动 mock <see cref="IChatClient"/> + 遥测替身），不读源码文本。
/// </para>
/// </remarks>
public class ObservedDelegatingAgentTests
{
    private const string HistoryStateKey = "test.history";
    private const string AgentInstructions = "AGENT_INSTR_SENTINEL";
    private const string SummaryInstructions = "SUM_PROMPT_SENTINEL";

    /// <summary>非流式路径：运行 Span 恰好一次（streaming=false）、用量三元组透传、耗时入直方图。</summary>
    [Fact]
    public async Task RunAsync_ShouldRecordSpanUsageAndDuration()
    {
        var telemetry = new RecordingTelemetry();
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok"))
        {
            Usage = new UsageDetails { InputTokenCount = 7, OutputTokenCount = 3, TotalTokenCount = 10 },
        };
        var agent = NewProbe(ResponseClient(response), telemetry);

        await agent.RunAsync("你好", await agent.CreateSessionAsync());

        telemetry.StartRunCount.Should().Be(1, "每次运行恰好开一个 Span");
        telemetry.StreamingFlags.Should().Equal([false], "非流式路径必须以 streaming=false 开启 Span");
        telemetry.RunCompletedCount.Should().Be(1);
        telemetry.LastFailed.Should().BeFalse();
        telemetry.LastInputTokens.Should().Be(7, "用量必须取自内层 Agent 的响应");
        telemetry.LastOutputTokens.Should().Be(3);
        telemetry.LastTotalTokens.Should().Be(10);
        telemetry.LlmDurationCount.Should().Be(1, "耗时直方图每轮恰好记一次");
        telemetry.LlmDurationAgentName.Should().Be("probe");
    }

    /// <summary>失败路径：Span 标失败、耗时仍入直方图，异常类型原样上抛（不包装）。</summary>
    [Fact]
    public async Task RunAsync_ShouldMarkFailedAndRethrowOriginalException()
    {
        var telemetry = new RecordingTelemetry();
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProbeFailureException("boom"));
        var agent = NewProbe(mock, telemetry);

        var act = async () => await agent.RunAsync("你好", await agent.CreateSessionAsync());

        await act.Should().ThrowAsync<ProbeFailureException>(
            "异常不得包装：上层幂等回滚依赖原异常类型");
        telemetry.LastFailed.Should().BeTrue("失败必须写进 Span 状态");
        telemetry.LlmDurationCount.Should().Be(1, "失败样本同样入直方图（否则 P95/P99 只反映成功路径）");
        telemetry.LastInputTokens.Should().BeNull("失败时不得伪造用量（MAF 已把 token 计入历史用量）");
    }

    /// <summary>流式路径：用量取自流中的 <c>UsageContent</c>（取最后一次），耗时与状态同非流式。</summary>
    [Fact]
    public async Task RunStreamingAsync_ShouldExtractUsageFromStream()
    {
        var telemetry = new RecordingTelemetry();
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(StreamUpdates());
        var agent = NewProbe(mock, telemetry);

        var text = string.Empty;
        await foreach (var update in agent.RunStreamingAsync("你好", await agent.CreateSessionAsync()))
        {
            text += update.Text;
        }

        text.Should().Be("片段一片段二", "基类只做编织，不得吞改流内容");
        telemetry.StreamingFlags.Should().Equal([true], "流式路径必须以 streaming=true 开启 Span");
        telemetry.LastFailed.Should().BeFalse();
        telemetry.LastInputTokens.Should().Be(11, "用量取自流中最后一次 UsageContent");
        telemetry.LastOutputTokens.Should().Be(5);
        telemetry.LastTotalTokens.Should().Be(16);
        telemetry.LlmDurationCount.Should().Be(1);
    }

    /// <summary>
    /// 次序契约：历史越限时<b>摘要调用先于模型调用</b>——否则本轮仍会带着越限的历史进入模型。
    /// </summary>
    [Fact]
    public async Task RunAsync_ShouldSummarizeBeforeInnerRun()
    {
        var calls = new List<string>();
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((messages, options, _) =>
                calls.Add((options?.Instructions ?? "<none>") + "|" + string.Join(" ", messages.Select(m => m.Text))))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        var inner = AgentAssemblyFactory.Create(
            mock.Object, "probe", AgentInstructions, tools: null, HistoryStateKey, maxHistoryMessages: 8);

        // 先积累两条历史（4 条消息），使 SummaryThreshold=2 在下一轮触发。
        var session = await inner.CreateSessionAsync();
        await inner.RunAsync("第一轮", session);
        await inner.RunAsync("第二轮", session);
        calls.Clear();

        var summarizer = new ConversationSummarizer(
            mock.Object,
            new AgentRuntimeOptions { SummaryThreshold = 2 },
            HistoryStateKey,
            summaryInstruction: SummaryInstructions);
        var agent = new ProbeAgent(inner, "probe", new RecordingTelemetry(), summarizer);

        await agent.RunAsync("第三轮", session);

        calls.Should().HaveCountGreaterThanOrEqualTo(2, "应先有一次摘要调用，再有一次模型调用");
        calls[0].Should().Contain(SummaryInstructions, "摘要必须先于模型调用发生（次序契约）");
        calls[^1].Should().Contain("第三轮", "主对话调用在摘要之后");
    }

    // ───────────────────── 辅助 ─────────────────────

    private static ProbeAgent NewProbe(Mock<IChatClient> client, RecordingTelemetry telemetry)
        => new(
            AgentAssemblyFactory.Create(
                client.Object, "probe", AgentInstructions, tools: null, HistoryStateKey, maxHistoryMessages: 8),
            "probe",
            telemetry);

    private static Mock<IChatClient> ResponseClient(ChatResponse response)
    {
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        return mock;
    }

    /// <summary>流：两条文本更新 + 结尾一条 <c>UsageContent</c>（本轮累计用量）。</summary>
    private static async IAsyncEnumerable<ChatResponseUpdate> StreamUpdates()
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, "片段一");
        yield return new ChatResponseUpdate(ChatRole.Assistant, "片段二");

        var usage = new UsageDetails { InputTokenCount = 11, OutputTokenCount = 5, TotalTokenCount = 16 };
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(usage)]);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>被观察类的具体化：仅把四个事实交给基类，不含任何产品语义。</summary>
    private sealed class ProbeAgent(
        AIAgent innerAgent,
        string agentName,
        IAgentTelemetry telemetry,
        ConversationSummarizer? summarizer = null)
        : ObservedDelegatingAgent(innerAgent, agentName, telemetry, summarizer)
    {
    }

    /// <summary>失败样本用的非 BCL 异常类型：区分得出「异常类型被原样保留」而非上游包装产物。</summary>
    private sealed class ProbeFailureException(string message) : Exception(message)
    {
    }

    /// <summary>遥测替身：记录编织层全部事件点。</summary>
    private sealed class RecordingTelemetry : IAgentTelemetry
    {
        public int StartRunCount;
        public int RunCompletedCount;
        public int LlmDurationCount;
        public string? LlmDurationAgentName;
        public bool LastFailed;
        public long? LastInputTokens;
        public long? LastOutputTokens;
        public long? LastTotalTokens;
        public List<bool> StreamingFlags { get; } = [];

        public IDisposable StartRun(string agentName, bool streaming)
        {
            StartRunCount++;
            StreamingFlags.Add(streaming);
            return new Scope();
        }

        public void RunCompleted(IDisposable? scope, bool failed, long? inputTokens, long? outputTokens, long? totalTokens)
        {
            RunCompletedCount++;
            LastFailed = failed;
            LastInputTokens = inputTokens;
            LastOutputTokens = outputTokens;
            LastTotalTokens = totalTokens;
        }

        public void LlmDuration(string agentName, long milliseconds)
        {
            LlmDurationCount++;
            LlmDurationAgentName = agentName;
        }

        public IDisposable? StartTurn(string conversationKey, string? tenantKey) => null;

        public void GateWaited(IDisposable? turn, double milliseconds)
        {
        }

        public void Replayed(IDisposable? turn)
        {
        }

        public void Streamed(IDisposable? turn)
        {
        }

        public void Degraded(string reason)
        {
        }

        public IDisposable? StartSummarize(int summarizedCount, int retainedCount) => null;

        public void SummarizeCompleted(IDisposable? scope, bool converged)
        {
        }

        private sealed class Scope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
