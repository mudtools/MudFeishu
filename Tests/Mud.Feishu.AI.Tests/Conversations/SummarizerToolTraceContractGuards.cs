// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Conversations;

namespace Mud.Feishu.AI.Tests.Conversations;

/// <summary>
/// T7 / D7 摘要纳入工具轨迹行为用例与守卫。
/// </summary>
public class SummarizerToolTraceContractGuards
{
    /// <summary>
    /// 守卫 ①：ToolTraceLineLimit 常量存在且值为 200。
    /// </summary>
    [Fact]
    public void ToolTraceLineLimit_ShouldBe200()
    {
        // 通过反射获取 internal 常量
        var field = typeof(ConversationSummarizer)
            .GetField("ToolTraceLineLimit",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        field.Should().NotBeNull("ToolTraceLineLimit 常量必须定义");
        var value = (int?)field!.GetValue(null);
        value.Should().Be(200, "工具轨迹行上限为 200 字符");
    }

    /// <summary>
    /// 用例：长工具会话摘要后「做过什么」可追溯。
    /// </summary>
    [Fact]
    public async Task SummarizeCoreAsync_WithToolCalls_ShouldIncludeToolTrace()
    {
        // Arrange：Mock IChatClient 返回摘要文本
        var mockClient = new Mock<IChatClient>();
        mockClient.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((messages, _, _) =>
            {
                // 验证摘要输入包含工具轨迹行
                var userMessage = messages.Last();
                userMessage.Text.Should().Contain("[tool]",
                    "摘要输入应包含工具轨迹行（T7/D7）");
            })
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "摘要：用户查询了考勤")));

        var options = new FeishuAgentOptions
        {
            Instructions = "你是助手",
            MaxHistoryMessages = 10,
            SummaryThreshold = 5,
        };

        var summarizer = new ConversationSummarizer(mockClient.Object, options);

        // 构造含工具调用的历史
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "帮我查一下上周的考勤"),
            new(ChatRole.Assistant, [
                new FunctionCallContent("call_1", "attendance.query_my_flow",
                    new Dictionary<string, object?>
                    {
                        ["start_date"] = "2026-10-05",
                        ["end_date"] = "2026-10-11",
                    }),
            ]),
            new(ChatRole.Tool, [
                new FunctionResultContent("call_1", new { count = 5 }),
            ]),
            new(ChatRole.Assistant, "查到了 5 条异常记录"),
        };

        // Act：调用 SummarizeCoreAsync（经反射）
        var method = typeof(ConversationSummarizer)
            .GetMethod("SummarizeCoreAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Should().NotBeNull();
        var task = (Task<string?>)method!.Invoke(summarizer, [history, CancellationToken.None])!;
        var result = await task;

        // Assert：摘要不应为空，且 Mock 验证了工具轨迹行被传入
        result.Should().Be("摘要：用户查询了考勤");
        mockClient.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 用例：摘要输入不含工具结果正文/参数值（A17）。
    /// </summary>
    [Fact]
    public async Task SummarizeCoreAsync_ShouldNotIncludeResultContent_OrParamValues()
    {
        string? capturedInput = null;
        var mockClient = new Mock<IChatClient>();
        mockClient.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((messages, _, _) =>
            {
                capturedInput = messages.Last().Text;
            })
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "摘要")));

        var options = new FeishuAgentOptions
        {
            Instructions = "你是助手",
            MaxHistoryMessages = 10,
            SummaryThreshold = 5,
        };

        var summarizer = new ConversationSummarizer(mockClient.Object, options);

        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "查询"),
            new(ChatRole.Assistant, [
                new FunctionCallContent("call_1", "task.list_my_tasks",
                    new Dictionary<string, object?>
                    {
                        ["task_guid"] = "task_abc123",  // 标识类键——应保留
                        ["status"] = "pending",          // 非标识类键——值应省略
                    }),
            ]),
            new(ChatRole.Tool, [
                // 结果正文不应出现在摘要输入中
                new FunctionResultContent("call_1", "这是一个很长的工具结果正文，包含敏感数据和详细信息..."),
            ]),
        };

        var method = typeof(ConversationSummarizer)
            .GetMethod("SummarizeCoreAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<string?>)method!.Invoke(summarizer, [history, CancellationToken.None])!;
        await task;

        capturedInput.Should().NotBeNull();
        capturedInput.Should().Contain("[tool]", "应包含工具轨迹行");
        capturedInput.Should().Contain("task.list_my_tasks", "应包含工具名");
        capturedInput.Should().Contain("task_guid=task_abc123", "标识类键应保留值");
        // 结果正文不应出现在摘要输入中
        capturedInput.Should().NotContain("敏感数据", "工具结果正文不应进入摘要输入");
        capturedInput.Should().NotContain("status=pending", "非标识类参数值不应出现在摘要输入中");
    }
}
