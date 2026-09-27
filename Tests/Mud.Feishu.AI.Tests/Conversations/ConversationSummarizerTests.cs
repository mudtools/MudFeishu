// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Conversations;

namespace Mud.Feishu.AI.Tests.Conversations;

/// <summary>
/// 渐进式会话摘要器测试（Phase 2 §3.2）：阈值触发、摘要注入历史首部、保留窗钳制、
/// 重建后不再立即触发（渐进式缓存语义）、失败隔离、禁用态。
/// </summary>
public class ConversationSummarizerTests
{
    private const string StateKey = FeishuAgent.ChatHistoryStateKey;

    private static Mock<IChatClient> CreateSummaryClient(string summaryText = "纪要：用户要查采购表，已完成表结构确认。")
    {
        var client = new Mock<IChatClient>();
        client
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, summaryText)));
        return client;
    }

    private static readonly FeishuAgent SessionFactoryAgent =
        new(new Mock<IChatClient>().Object, new FeishuAgentOptions { Instructions = "x" });

    private static async Task<AgentSession> CreateSessionWithHistoryAsync(int count)
    {
        var session = await SessionFactoryAgent.CreateSessionAsync();
        var history = new List<ChatMessage>();
        for (var i = 0; i < count; i++)
        {
            history.Add(new ChatMessage(i % 2 == 0 ? ChatRole.User : ChatRole.Assistant, $"消息 {i}"));
        }

        session.SetInMemoryChatHistory(history, StateKey, null);
        return session;
    }

    private static ConversationSummarizer CreateSummarizer(
        Mock<IChatClient>? client = null, int summaryThreshold = 6, int maxHistoryMessages = 8)
        => new(
            (client ?? CreateSummaryClient()).Object,
            new FeishuAgentOptions { Instructions = "x", SummaryThreshold = summaryThreshold, MaxHistoryMessages = maxHistoryMessages },
            NullLogger.Instance);

    [Fact]
    public async Task Summarize_ShouldInjectSummaryHead_AndRetainRecentWindow()
    {
        var session = await CreateSessionWithHistoryAsync(10);

        var summarized = await CreateSummarizer(summaryThreshold: 8, maxHistoryMessages: 8)
            .SummarizeIfNeededAsync(session);

        summarized.Should().BeTrue("历史条数达到阈值须触发压缩");
        session.TryGetInMemoryChatHistory(out var history, StateKey, null).Should().BeTrue();
        history.Should().NotBeNull();

        // 保留窗 = MaxHistoryMessages/2 = 4；重建后 = [摘要 system] + 最近 4 条。
        history!.Should().HaveCount(5);
        history[0].Role.Should().Be(ChatRole.System);
        history[0].Text.Should().Contain("纪要", "摘要以系统要点纪要注入历史首部（渐进式缓存进 session）");
        history.Skip(1).Select(m => m.Text).Should().BeEquivalentTo(
            ["消息 6", "消息 7", "消息 8", "消息 9"], "保留最近 MaxHistoryMessages/2 条，更早历史被压缩");
    }

    [Fact]
    public async Task Summarize_ShouldNotTrigger_BelowThreshold()
    {
        var session = await CreateSessionWithHistoryAsync(5);
        var client = CreateSummaryClient();

        var summarized = await CreateSummarizer(client, summaryThreshold: 6)
            .SummarizeIfNeededAsync(session);

        summarized.Should().BeFalse("未达阈值不触发（避免无谓的摘要模型调用）");
        client.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        session.TryGetInMemoryChatHistory(out var history, StateKey, null).Should().BeTrue();
        history!.Should().HaveCount(5, "历史不被改动");
    }

    [Fact]
    public async Task Summarize_ShouldNotRetriggerImmediately_AfterRebuild()
    {
        var session = await CreateSessionWithHistoryAsync(10);
        var summarizer = CreateSummarizer(summaryThreshold: 8, maxHistoryMessages: 8);

        var first = await summarizer.SummarizeIfNeededAsync(session);
        session.TryGetInMemoryChatHistory(out var history, StateKey, null);

        var second = await summarizer.SummarizeIfNeededAsync(session);

        first.Should().BeTrue();
        history!.Count.Should().BeLessThan(8, "重建后条数须低于阈值（保留窗钳制到 阈值-2 以下）");
        second.Should().BeFalse("渐进式语义：摘要驻留会话内，重建后不每轮重复摘要");
    }

    [Fact]
    public async Task Summarize_ShouldBeDisabled_WhenThresholdZero()
    {
        var session = await CreateSessionWithHistoryAsync(20);

        var summarized = await CreateSummarizer(summaryThreshold: 0).SummarizeIfNeededAsync(session);

        summarized.Should().BeFalse("SummaryThreshold=0 为禁用态（保持既有历史裁剪窗行为）");
    }

    [Fact]
    public async Task Summarize_ShouldSkipSilently_WhenSummaryModelFails()
    {
        var session = await CreateSessionWithHistoryAsync(10);
        var failingClient = new Mock<IChatClient>();
        failingClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("模型网关 5xx"));

        var summarized = await CreateSummarizer(failingClient, summaryThreshold: 8)
            .SummarizeIfNeededAsync(session);

        summarized.Should().BeFalse("摘要模型失败只跳过本轮压缩（失败隔离，绝不中断主对话）");
        session.TryGetInMemoryChatHistory(out var history, StateKey, null).Should().BeTrue();
        history!.Should().HaveCount(10, "失败时不改动会话历史");
    }

    [Fact]
    public async Task Summarize_ShouldNoOp_WhenNoHistory()
    {
        var session = await SessionFactoryAgent.CreateSessionAsync();

        var summarized = await CreateSummarizer().SummarizeIfNeededAsync(session);

        summarized.Should().BeFalse();
    }
}
