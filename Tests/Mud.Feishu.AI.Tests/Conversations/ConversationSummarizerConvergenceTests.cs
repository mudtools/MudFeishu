// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions.Observability;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Diagnostics;

namespace Mud.Feishu.AI.Tests.Conversations;

/// <summary>
/// 摘要重建后的 token 预算收敛（P1-1）：重建结果必须低于 <c>MaxHistoryTokens</c>，
/// 否则下一轮立即再次触发摘要（每轮一次模型调用、摘要被反复摘要）。
/// </summary>
/// <remarks>
/// 收敛靠两件事：①保留窗起点取「条数窗口 ∩ token 预算窗口」的较晚者（被挤出的旧消息进摘要输入，
/// 而非丢弃最新内容）；②重建后仍越限时从保留窗头部整组回退。
/// 断言中刻意包含「最新消息被保留」——只断言「第二次返回 false」会让「从尾部删除」的错误实现也蒙混过关。
/// </remarks>
public class ConversationSummarizerConvergenceTests
{
    private const string StateKey = FeishuAgent.ChatHistoryStateKey;

    /// <summary>每条历史的消息体（400 个 ASCII 字符 ≈ 100 token，估算路径 4 字符/token）。</summary>
    private const int MessageChars = 400;

    private static readonly FeishuAgent SessionFactoryAgent =
        new(new Mock<IChatClient>().Object, new FeishuAgentOptions { Instructions = "x" });

    private static Mock<IChatClient> CreateSummaryClient()
    {
        var client = new Mock<IChatClient>();
        client
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "纪要：历史要点。")));
        return client;
    }

    private static async Task<AgentSession> CreateLongHistoryAsync(int count)
    {
        var session = await SessionFactoryAgent.CreateSessionAsync();
        var history = new List<ChatMessage>();
        for (var i = 0; i < count; i++)
        {
            history.Add(new ChatMessage(
                i % 2 == 0 ? ChatRole.User : ChatRole.Assistant,
                $"[{i}]" + new string('x', MessageChars)));
        }

        session.SetInMemoryChatHistory(history, StateKey, null);
        return session;
    }

    /// <summary>token-only 配置（<c>SummaryThreshold=0</c>，P2-6 起合法）：仅按 token 预算触发摘要。</summary>
    private static ConversationSummarizer CreateTokenOnlySummarizer(
        Mock<IChatClient> client, int maxHistoryTokens, int maxHistoryMessages = 8)
        => new(
            client.Object,
            new FeishuAgentOptions
            {
                Instructions = "x",
                SummaryThreshold = 0,
                MaxHistoryTokens = maxHistoryTokens,
                MaxHistoryMessages = maxHistoryMessages,
            },
            NullLogger.Instance);

    [Fact]
    public async Task SummarizeIfNeededAsync_ShouldNotResummarize_WhenRetainedWindowOverTokenBudget()
    {
        // 每条 ≈ 100 token，预算 300 ⇒ 保留窗最多 2 条（3 条即 300 ≥ 预算）。
        var session = await CreateLongHistoryAsync(20);
        var client = CreateSummaryClient();
        var summarizer = CreateTokenOnlySummarizer(client, maxHistoryTokens: 300);

        var first = await summarizer.SummarizeIfNeededAsync(session);
        session.TryGetInMemoryChatHistory(out var rebuilt, StateKey, null);
        var second = await summarizer.SummarizeIfNeededAsync(session);

        first.Should().BeTrue("历史 token 量远超预算，须触发压缩");
        second.Should().BeFalse("重建后必须低于预算——否则每轮重复摘要（模型调用 ×N，摘要被反复摘要）");
        client.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once, "收敛后第二轮不得再调用摘要模型");

        rebuilt.Should().NotBeNull();
        ChatTokenCounter.CountMessages(rebuilt!).Should().BeLessThan(300, "重建结果必须落在预算内");

        // 条数**不**硬编码：R2-4 之前精确计数恒不可用，本用例的条数是按「4 字符/token」估算校准的；
        // 精确计数生效后同一份历史的 token 量变小 ⇒ 保留窗变大。绑定条数会把「计数实现」误当成契约。
        // 这里断言真正的不变量：摘要在首部、最新一条保留、结果落在预算内。
        rebuilt![0].Role.Should().Be(ChatRole.System, "摘要注入历史首部");
        rebuilt[^1].Text.Should().StartWith("[19]", "最新一条必须保留");
        rebuilt.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task SummarizeIfNeededAsync_ShouldKeepNewestMessages_WhenShrinkingForBudget()
    {
        var session = await CreateLongHistoryAsync(20);
        var summarizer = CreateTokenOnlySummarizer(CreateSummaryClient(), maxHistoryTokens: 300);

        await summarizer.SummarizeIfNeededAsync(session);
        session.TryGetInMemoryChatHistory(out var rebuilt, StateKey, null);

        rebuilt.Should().NotBeNull();
        rebuilt![0].Role.Should().Be(ChatRole.System);

        // 预算收缩必须丢弃「最旧的保留消息」，而不是最新上下文（从尾部删除会让模型丢失最新信息）。
        rebuilt[^1].Text.Should().StartWith("[19]", "最后一条（最新）必须保留");
        rebuilt[^2].Text.Should().StartWith("[18]", "倒数第二条必须保留");
    }

    [Fact]
    public async Task SummarizeIfNeededAsync_ShouldKeepAtLeastLastMessage_WhenSingleMessageExceedsBudget()
    {
        // 单条 ≈ 100 token 而预算仅 50：窗口无论怎么收缩都会越限（配置与内容不匹配的必然结果）。
        // 要求：不死循环、不丢光上下文（最后一条必须保留）、不抛异常。
        var session = await CreateLongHistoryAsync(10);
        var summarizer = CreateTokenOnlySummarizer(CreateSummaryClient(), maxHistoryTokens: 50);

        var summarized = await summarizer.SummarizeIfNeededAsync(session);

        summarized.Should().BeTrue();
        session.TryGetInMemoryChatHistory(out var rebuilt, StateKey, null).Should().BeTrue();

        rebuilt.Should().NotBeNull();
        rebuilt!.Should().HaveCount(2, "收敛兜底收缩到「摘要 + 最后一条」即停，不会无限删除");
        rebuilt[^1].Text.Should().StartWith("[9]", "最后一条（当前上下文）不得被删除");
    }

    // ───────────────────── R2-11：不可收敛边界（不再每轮「摘要的摘要」） ─────────────────────

    [Fact]
    public async Task SummarizeIfNeededAsync_ShouldNotReSummarize_WhenSingleMessageExceedsBudget()
    {
        // 场景：上一次已压缩到「摘要 + 1 条」，而该条本身即超预算 ⇒ 摘要不可能把 token 降下来。
        // R2-11 前：每轮 ShouldSummarize 立即为真 ⇒ 每轮一次模型调用且摘要被反复摘要（语义持续退化）。
        var session = await SessionFactoryAgent.CreateSessionAsync();

        // 单条远超预算（精确/估算两种计数路径下都必然越限，不依赖具体 token 数）。
        var oversized = new string('x', MessageChars * 20);
        ChatTokenCounter.Count(oversized).Should().BeGreaterThan(50);

        session.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.System, "[历史要点纪要]\n上一轮摘要"),
            new ChatMessage(ChatRole.User, oversized),
        ], StateKey, null);

        var client = CreateSummaryClient();
        var summarizer = CreateTokenOnlySummarizer(client, maxHistoryTokens: 50);

        // 第一轮：仍会触发一次压缩（保留既有语义：先尝试收敛）。
        var first = await summarizer.SummarizeIfNeededAsync(session);
        session.TryGetInMemoryChatHistory(out var rebuilt, StateKey, null);
        rebuilt.Should().NotBeNull();
        rebuilt!.Should().HaveCountGreaterThanOrEqualTo(2);

        var callsAfterFirst = client.Invocations.Count;

        // 第二轮起：历史已是「摘要 + 1 条」且由 token 维度触发 ⇒ 必须短路返回 false。
        var second = await summarizer.SummarizeIfNeededAsync(session);
        var third = await summarizer.SummarizeIfNeededAsync(session);

        second.Should().BeFalse("已缩到「摘要 + 1 条」仍越限时，继续摘要只会每轮多一次模型调用");
        third.Should().BeFalse();
        client.Invocations.Count.Should().Be(callsAfterFirst, "不可收敛时不得再产生任何摘要模型调用");
    }

    [Fact]
    public async Task SummarizeIfNeededAsync_ShouldWarnAndMarkSpan_WhenUnconverged()
    {
        var activities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == FeishuActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Enqueue(activity),
        };
        ActivitySource.AddActivityListener(listener);

        // 预算极小：即使压到「摘要 + 最后一条」也必然越限。
        var historyCount = 13;
        var session = await CreateLongHistoryAsync(historyCount);
        var logger = new CapturingLogger();
        var summarizer = new ConversationSummarizer(
            CreateSummaryClient().Object,
            new FeishuAgentOptions
            {
                Instructions = "x",
                SummaryThreshold = 0,
                MaxHistoryTokens = 10,
                MaxHistoryMessages = 8,
            },
            logger);

        await summarizer.SummarizeIfNeededAsync(session);

        // ① 日志告警（确定性断言，不依赖全局设施）。
        logger.Warnings.Should().Contain(w => w.Contains("无法收敛"),
            "不可收敛必须留下可观测信号——否则宿主只看到「每轮都在摘要」而不知原因");

        // ② Span 属性：用 (summarized=12, retained=1) 这对**唯一指纹**定位本次 Span。
        // ActivitySource.AddActivityListener 是**进程级全局**设施，而 xUnit 默认并行执行不同测试类——
        // 其它用例的 feishu.agent.summarize Span 会混入队列，用 `FirstOrDefault(opName)` 定位会偶发
        // 命中「已收敛」的 Span（本机全量门禁实测出现 1 次 flaky）。指纹定位使断言与执行顺序无关。
        var summarizeSpan = activities.FirstOrDefault(a =>
            a.OperationName == "feishu.agent.summarize"
            && Equals(a.GetTagItem("feishu.agent.summarized_messages"), historyCount - 1)
            && Equals(a.GetTagItem("feishu.agent.retained_messages"), 1));

        summarizeSpan.Should().NotBeNull("本次不可收敛的摘要 Span 必须被捕获");
        summarizeSpan!.GetTagItem(FeishuAgentDiagnostics.TagSummarizeUnconverged).Should().Be(true,
            "不可收敛必须标记 Span 属性——让宿主能区分「配置/内容不匹配」与「摘要失效」");
    }

    /// <summary>捕获 Warning 及以上日志的 <see cref="ILogger"/> 桩（用于断言可观测信号）。</summary>
    private sealed class CapturingLogger : ILogger
    {
        private readonly List<string> _warnings = [];

        internal IReadOnlyList<string> Warnings => _warnings;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
            {
                _warnings.Add(formatter(state, exception));
            }
        }
    }

    [Fact]
    public async Task SummarizeIfNeededAsync_ShouldStillSummarize_WhenCountThresholdTriggers_WithTinyHistory()
    {
        // 复核修正的边界锁定：R2-11 的短路只在**token 维度单独触发**时生效；
        // 条数维度触发时（SummaryThreshold=2 的极端合法配置）压缩仍能降低条数，不得被误伤。
        var session = await SessionFactoryAgent.CreateSessionAsync();
        session.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.User, "u1"),
            new ChatMessage(ChatRole.Assistant, "a1"),
        ], StateKey, null);

        var summarizer = new ConversationSummarizer(
            CreateSummaryClient().Object,
            new FeishuAgentOptions
            {
                Instructions = "x",
                SummaryThreshold = 2,      // 条数维度触发
                MaxHistoryTokens = 5,      // token 维度也触发
                MaxHistoryMessages = 8,
            },
            NullLogger.Instance);

        var summarized = await summarizer.SummarizeIfNeededAsync(session);

        summarized.Should().BeTrue("条数维度触发时不得被「不可收敛」短路——压缩仍能降低条数");
    }
}
