// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Knowledge;

namespace Mud.Feishu.AI.Tests.Conversations;

/// <summary>
/// token 窗口与摘要治理测试（AI-FD-D12 P2D-3a/3b）：token 计数、双窗口先触发者生效、
/// 摘要输入压缩；知识装配器注入格式与失败隔离（P2D-4a）。
/// </summary>
public class TokenWindowTests
{
    private const string StateKey = FeishuAgent.ChatHistoryStateKey;

    private static readonly FeishuAgent SessionFactoryAgent =
        new(new Mock<IChatClient>().Object, new FeishuAgentOptions { Instructions = "x" });

    /// <summary>
    /// 估算路径的「4 字符/token」比（<see cref="ChatTokenCounter.Estimate"/>）。
    /// </summary>
    /// <remarks>
    /// R2-4 修正：本用例原先用 <see cref="ChatTokenCounter.Count"/> 断言估算语义——精确计数恒不可用时
    /// 恰好等价，R2-4 补齐词表包后 <c>Count</c> 走 Tiktoken 真实词表，两者不再相等。
    /// 估算比是<b>估算路径</b>的契约，必须直接单测 <c>Estimate</c>（net8+ 精确路径下 <c>Count</c> 走不到该分支）。
    /// </remarks>
    [Theory]
    [InlineData("", 0)]
    [InlineData("abcd", 1)]
    [InlineData("abcdefgh", 2)]
    public void Estimate_ShouldCountByCharsPerToken(string text, int expected)
        => ChatTokenCounter.Estimate(text).Should().Be(expected);

    [Theory]
    [InlineData("", 0)]
    [InlineData("abcd", 1)]
    [InlineData("abcdefgh", 6)]
    public void Count_ShouldBeSane_UnderEitherCountingPath(string text, int maxExpected)
        => ChatTokenCounter.Count(text).Should().BeLessThanOrEqualTo(maxExpected,
            "精确/估算两条路径都不得高估到量级之外（8 个 ASCII 字符至多几个 token）");

    [Fact]
    public void Count_ShouldBeMonotonic()
    {
        var shortText = ChatTokenCounter.Count("短文本");
        var longText = ChatTokenCounter.Count("这是一段长很多很多的中文文本，用于验证 token 计数随文本长度单调增长");
        longText.Should().BeGreaterThan(shortText);
    }

    [Fact]
    public void ShouldSummarize_ShouldTriggerByTokenDimension_WhenCountBelowThreshold()
    {
        var summarizer = new ConversationSummarizer(
            new Mock<IChatClient>().Object,
            new FeishuAgentOptions { Instructions = "x", SummaryThreshold = 0, MaxHistoryTokens = 10 });

        summarizer.ShouldSummarize(historyCount: 2, historyTokens: 100).Should().BeTrue(
            "token 超限触发（先于条数窗口）");
        summarizer.ShouldSummarize(historyCount: 2, historyTokens: 5).Should().BeFalse("未超限不触发");
    }

    [Fact]
    public void ShouldSummarize_ShouldTriggerByCountDimension_WhenTokensDisabled()
    {
        var summarizer = new ConversationSummarizer(
            new Mock<IChatClient>().Object,
            new FeishuAgentOptions { Instructions = "x", SummaryThreshold = 4, MaxHistoryTokens = 0 });

        summarizer.ShouldSummarize(historyCount: 4, historyTokens: 0).Should().BeTrue("条数阈值先触发者生效");
        summarizer.ShouldSummarize(historyCount: 3, historyTokens: 0).Should().BeFalse();
    }

    [Fact]
    public async Task Summarize_ShouldCompressPerMessageTranscript()
    {
        var captured = new List<ChatMessage>();
        var client = new Mock<IChatClient>();
        client
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<ChatMessage> messages, ChatOptions? _, CancellationToken __) => captured.AddRange(messages))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "纪要")));

        var summarizer = new ConversationSummarizer(
            client.Object,
            new FeishuAgentOptions { Instructions = "x", SummaryThreshold = 3, MaxHistoryTokens = 0 },
            NullLogger.Instance);

        var longText = new string('长', 2000);
        var session = await SessionFactoryAgent.CreateSessionAsync();
        session.SetInMemoryChatHistory(new List<ChatMessage>
        {
            new(ChatRole.User, longText),
            new(ChatRole.Assistant, longText),
            new(ChatRole.User, "短消息"),
        }, StateKey, null);

        var summarized = await summarizer.SummarizeIfNeededAsync(session, CancellationToken.None);

        summarized.Should().BeTrue();
        var userMessage = captured.FirstOrDefault(m => m.Role == ChatRole.User);
        userMessage.Should().NotBeNull();
        userMessage!.Text.Length.Should().BeLessThan(longText.Length, "摘要输入按每条 500 字压缩（P2D-3b）");
    }

    [Fact]
    public async Task KnowledgeAssembler_ShouldInjectNumberedChunks_WithFormat()
    {
        var retriever = new Mock<IRetriever>();
        retriever
            .Setup(r => r.RetrieveAsync("采购流程是什么", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new RetrievedChunk("采购需先提交申请单", Source: "aily:data-knowledge:asset-1"),
                new RetrievedChunk("审批通过后自动生成采购单"),
            });

        var assembler = new KnowledgeContextAssembler(retriever.Object);
        assembler.Order.Should().Be(KnowledgeContextAssembler.DefaultOrder);

        var fragment = await assembler.AssembleAsync(new ConversationRequest(
            "app-a", ConversationScope.Group(), "oc_1", "ou_1", "om_1", MentionedText: "采购流程是什么"));

        fragment.Should().NotBeNull();
        fragment.Should().StartWith("[参考知识｜来自飞书知识库检索，引用请注明编号]");
        fragment.Should().Contain("[1] 采购需先提交申请单");
        fragment.Should().Contain("[2] 审批通过后自动生成采购单");
        fragment.Should().EndWith("（若与问题无关请忽略本节）");
    }

    [Fact]
    public async Task KnowledgeAssembler_ShouldReturnNull_WhenNoMentionOrNoChunks()
    {
        var retriever = new Mock<IRetriever>();
        retriever
            .Setup(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RetrievedChunk>());
        var assembler = new KnowledgeContextAssembler(retriever.Object);

        (await assembler.AssembleAsync(new ConversationRequest(
            "app-a", ConversationScope.Group(), "oc_1", "ou_1", "om_1", MentionedText: null)))
            .Should().BeNull("无用户问题即无检索语义");

        (await assembler.AssembleAsync(new ConversationRequest(
            "app-a", ConversationScope.Group(), "oc_1", "ou_1", "om_1", MentionedText: "无命中问题")))
            .Should().BeNull("检索无结果时本装配器对本次事件无贡献");
    }

    [Fact]
    public async Task KnowledgeAssembler_ShouldTruncateLongChunks()
    {
        var retriever = new Mock<IRetriever>();
        retriever
            .Setup(r => r.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RetrievedChunk(new string('长', 2000))]);
        var assembler = new KnowledgeContextAssembler(retriever.Object);

        var fragment = await assembler.AssembleAsync(new ConversationRequest(
            "app-a", ConversationScope.Group(), "oc_1", "ou_1", "om_1", MentionedText: "问题"));

        fragment.Should().Contain("…", "单条切片按 500 字截断（P2D-4a）");
        fragment!.Length.Should().BeLessThan(700);
    }
}
