// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.Agents;

/// <summary>
/// 飞书侧 guidance 装配的<b>端到端</b>用例：<see cref="FeishuAgent"/> 是否把装配结果
/// 真的落到 <c>ChatOptions.Instructions</c>，以及超限信号是否可从 Agent 上断言。
/// </summary>
/// <remarks>
/// <para>
/// <b>与中性包用例的分工（P1-H2a）</b>：纯字符串装配语义（顺序 / 截断 / 预算只计本体）
/// 已在 <c>Tests/Mud.AI.Agent.Tests/Guidance/GuidanceComposerTests.cs</c> 覆盖；
/// 本类只保留<b>必须经过飞书产品侧装配</b>才能验证的部分（Agent 构造 → ChatOptions → 模型调用）。
/// </para>
/// <para>
/// 预算用的是飞书产品常量 <see cref="FeishuGuidanceBudget.MaxGuidanceLength"/>——
/// 这正是"产品额度不进中性包"的另一面：飞书侧的用例断言飞书侧的取值。
/// </para>
/// </remarks>
public class FeishuAgentGuidanceTests
{
    private static GuidanceBlock Block(string domain, string content) => new(domain, content);

    [Fact]
    public void FeishuAgent_ShouldExposeGuidanceResult_WithOmittedDomains()
    {
        var third = new string('x', FeishuGuidanceBudget.MaxGuidanceLength / 3);
        var agent = new FeishuAgent(
            CreateMockClient().Object,
            new FeishuAgentOptions { Instructions = "宿主指令", MaxHistoryMessages = 10 },
            domainGuidance: [Block("first", third), Block("second", third), Block("third", third)]);

        agent.Guidance.Truncated.Should().BeTrue();
        agent.Guidance.OmittedDomains.Should().Equal(["third"],
            "丢弃清单必须可从 Agent 上断言（超限不再只以日志形式存在）");
        agent.Guidance.IncludedDomains.Should().Equal(["first", "second"]);
    }

    [Fact]
    public async Task FeishuAgent_ShouldPassComposedInstructionsToChatOptions()
    {
        var mock = CreateMockClient();
        var agent = new FeishuAgent(
            mock.Object,
            new FeishuAgentOptions { Instructions = "宿主指令", MaxHistoryMessages = 10 },
            domainGuidance: [Block("bitable", "多维表格：先 list_tables。")]);

        var session = await agent.CreateSessionAsync();
        await agent.RunAsync("查询", session);

        mock.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.Is<ChatOptions?>(o => o != null
                && o.Instructions!.StartsWith("宿主指令", StringComparison.Ordinal)
                && o.Instructions.Contains("多维表格：先 list_tables。")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FeishuAgent_WithoutGuidance_ShouldKeepBareHostInstructions()
    {
        var mock = CreateMockClient();
        var agent = new FeishuAgent(
            mock.Object,
            new FeishuAgentOptions { Instructions = "宿主指令", MaxHistoryMessages = 10 });

        var session = await agent.CreateSessionAsync();
        await agent.RunAsync("查询", session);

        mock.Verify(c => c.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.Is<ChatOptions?>(o => o != null && o.Instructions == "宿主指令"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IChatClient> CreateMockClient()
    {
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        return mock;
    }
}
