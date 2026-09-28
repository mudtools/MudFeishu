// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Conversations;

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
        rebuilt!.Should().HaveCount(3, "摘要 1 条 + 保留 2 条");
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
}
