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
/// 摘要切片的工具调用组保护（P0-2）：切片起点不得落在
/// <c>assistant(tool_calls)</c> + <c>tool(result)</c> 组内——否则下发给 OpenAI 兼容端点即 400
/// （tool 消息缺少前驱 tool_calls），且会话因保存失败而永久卡死（重投递再次摘要、再次 400）。
/// </summary>
public class ConversationSummarizerToolBoundaryTests
{
    private const string StateKey = FeishuAgent.ChatHistoryStateKey;

    private static readonly FeishuAgent SessionFactoryAgent =
        new(new Mock<IChatClient>().Object, new FeishuAgentOptions { Instructions = "x" });

    private static Mock<IChatClient> CreateSummaryClient(string summaryText = "纪要：用户要查采购表。")
    {
        var client = new Mock<IChatClient>();
        client
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, summaryText)));
        return client;
    }

    private static ChatMessage ToolCall(string callId)
        => new(ChatRole.Assistant, [new FunctionCallContent(callId, "bitable.list_tables")]);

    private static ChatMessage ToolResult(string callId)
        => new(ChatRole.Tool, [new FunctionResultContent(callId, "ok")]);

    private static async Task<AgentSession> CreateSessionAsync(params ChatMessage[] history)
    {
        var session = await SessionFactoryAgent.CreateSessionAsync();
        session.SetInMemoryChatHistory([.. history], StateKey, null);
        return session;
    }

    private static ConversationSummarizer CreateSummarizer(Mock<IChatClient> client, int threshold, int maxHistoryMessages)
        => new(
            client.Object,
            new FeishuAgentOptions
            {
                Instructions = "x",
                SummaryThreshold = threshold,
                MaxHistoryMessages = maxHistoryMessages,
            },
            NullLogger.Instance);

    [Fact]
    public async Task SummarizeIfNeededAsync_ShouldKeepToolCallGroupIntact()
    {
        // 10 条历史 + 阈值 6 + 裁剪窗 8 ⇒ 条数保留窗 = 4，切片点落在 index 6（工具组内）。
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "消息 0"),
            ToolCall("call_0"),
            ToolResult("call_0"),
            new(ChatRole.User, "消息 3"),
            new(ChatRole.Assistant, "消息 4"),
            new(ChatRole.User, "消息 5"),
            ToolCall("call_1"),
            ToolResult("call_1"),
            new(ChatRole.User, "消息 8"),
            new(ChatRole.Assistant, "消息 9"),
        };
        var session = await CreateSessionAsync([.. history]);
        var client = CreateSummaryClient();

        var summarized = await CreateSummarizer(client, threshold: 6, maxHistoryMessages: 8)
            .SummarizeIfNeededAsync(session);

        summarized.Should().BeTrue("条数达到阈值须触发压缩");
        session.TryGetInMemoryChatHistory(out var rebuilt, StateKey, null).Should().BeTrue();
        rebuilt.Should().NotBeNull();

        rebuilt![0].Role.Should().Be(ChatRole.System, "摘要以 system 消息注入历史首部");
        rebuilt[1].Role.Should().NotBe(ChatRole.Tool,
            "保留窗首条不得是孤立 tool 消息（否则 OpenAI 兼容端点 400，会话永久卡死）");
        rebuilt[1].Text.Should().Be("消息 5", "切片起点回退到工具组之前，且只回退到必要位置");

        rebuilt.Count(m => m.Contents.Any(c => c is FunctionCallContent)).Should().Be(1, "tool_calls 必须保留");
        rebuilt.Count(m => m.Contents.Any(c => c is FunctionResultContent)).Should().Be(1, "tool 结果必须与其调用成对保留");

        var callIndex = rebuilt.FindIndex(m => m.Contents.Any(c => c is FunctionCallContent));
        var resultIndex = rebuilt.FindIndex(m => m.Contents.Any(c => c is FunctionResultContent));
        callIndex.Should().BeLessThan(resultIndex, "配对顺序不得颠倒");
        callIndex.Should().BeGreaterThan(0, "工具组不得落在摘要之前");
    }

    [Fact]
    public async Task SummarizeIfNeededAsync_ShouldSkip_WhenAllHistoryIsToolRelated()
    {
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "用户提问"),
            ToolCall("call_0"),
            ToolResult("call_0"),
            ToolCall("call_1"),
            ToolResult("call_1"),
        };
        var session = await CreateSessionAsync([.. history]);
        var client = CreateSummaryClient();

        var summarized = await CreateSummarizer(client, threshold: 4, maxHistoryMessages: 8)
            .SummarizeIfNeededAsync(session);

        summarized.Should().BeFalse("全部历史都在工具调用组内：宁可跳过本轮压缩，也不产出非法历史");
        client.Verify(
            c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()),
            Times.Never, "跳过时不得调用摘要模型（零成本）");
        session.TryGetInMemoryChatHistory(out var unchanged, StateKey, null).Should().BeTrue();
        unchanged.Should().HaveCount(5, "跳过时历史保持原样（幂等）");
    }

    [Fact]
    public async Task SummarizeIfNeededAsync_ShouldNotOverExpand_WhenHistoryHasNoToolMessages()
    {
        var history = new List<ChatMessage>();
        for (var i = 0; i < 10; i++)
        {
            history.Add(new ChatMessage(i % 2 == 0 ? ChatRole.User : ChatRole.Assistant, $"消息 {i}"));
        }

        var session = await CreateSessionAsync([.. history]);

        var summarized = await CreateSummarizer(CreateSummaryClient(), threshold: 6, maxHistoryMessages: 8)
            .SummarizeIfNeededAsync(session);

        summarized.Should().BeTrue();
        session.TryGetInMemoryChatHistory(out var rebuilt, StateKey, null).Should().BeTrue();
        rebuilt!.Should().HaveCount(5, "无工具消息时保留窗严格等于条数窗（4 条），不因保护逻辑回退");
        rebuilt.Skip(1).Select(m => m.Text).Should().BeEquivalentTo(["消息 6", "消息 7", "消息 8", "消息 9"]);
    }
}
