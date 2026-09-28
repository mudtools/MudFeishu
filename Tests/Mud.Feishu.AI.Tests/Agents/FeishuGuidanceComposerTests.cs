// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.Agents;

/// <summary>
/// WP6 / AT-F09 域级 guidance 装配用例：四条验收（未启用不注入 / 启用即注入 / 超限截断 + 可断言信号 /
/// 宿主指令恒在前）+ 指令装配的端到端（经 <see cref="FeishuAgent"/> 落到 <c>ChatOptions.Instructions</c>）。
/// </summary>
/// <remarks>
/// 截断信号落在 <see cref="FeishuGuidanceResult"/> 上（<c>Truncated</c> + 丢弃清单），
/// 用例<b>不依赖日志断言基建</b>（R4.1 评审 QA 补强）。
/// </remarks>
public class FeishuGuidanceComposerTests
{
    private static FeishuGuidanceBlock Block(string domain, string content) => new(domain, content);

    // ───────────────────── ① 未启用任何工具：不污染指令 ─────────────────────

    [Fact]
    public void Compose_WithoutBlocks_ShouldLeaveHostInstructionsUnchanged()
    {
        var result = FeishuGuidanceComposer.Compose("你是飞书助手", []);

        result.Instructions.Should().Be("你是飞书助手");
        result.Truncated.Should().BeFalse();
        result.IncludedDomains.Should().BeEmpty();
    }

    [Fact]
    public void Compose_WithNullInputs_ShouldReturnEmptyInstructions()
    {
        var result = FeishuGuidanceComposer.Compose(null, null);

        result.Instructions.Should().BeEmpty();
        result.Truncated.Should().BeFalse();
    }

    // ───────────────────── ② 启用某域工具：指令含该域 guidance，且在宿主指令之后 ─────────────────────

    [Fact]
    public void Compose_ShouldAppendGuidanceAfterHostInstructions()
    {
        var result = FeishuGuidanceComposer.Compose(
            "你是飞书助手",
            [Block("bitable", "多维表格：先 list_tables。")]);

        result.Instructions.Should().StartWith("你是飞书助手", "宿主指令优先，域 guidance 只是补充");
        result.Instructions.Should().Contain("多维表格：先 list_tables。");
        result.IncludedDomains.Should().Equal("bitable");
        result.Truncated.Should().BeFalse();
    }

    [Fact]
    public void Compose_WithEmptyHost_ShouldProduceGuidanceOnly_WithoutLeadingSeparator()
    {
        var result = FeishuGuidanceComposer.Compose("", [Block("docx", "云文档正文。")]);

        result.Instructions.Should().Be("云文档正文。", "宿主指令为空时不得插入前导空行");
    }

    [Fact]
    public void Compose_ShouldPreserveBlockOrder_AndSkipBlankBlocks()
    {
        var result = FeishuGuidanceComposer.Compose(
            "host",
            [Block("task", "任务域。"), Block("empty", "   "), Block("im", "消息域。")]);

        result.IncludedDomains.Should().Equal(["task", "im"], "空块不占额度也不进清单");
        result.Instructions.Should().Contain("任务域。");
        result.Instructions.Should().Contain("消息域。");
    }

    // ───────────────────── ③ 超限：截断 + 丢弃清单可见 ─────────────────────

    [Fact]
    public void Compose_OverLimit_ShouldTruncateTail_AndReportOmittedDomains()
    {
        // 每块约占上限的 1/3：前两块放得下，第三块触发截断（含分隔符开销）。
        var third = new string('x', FeishuGuidanceComposer.MaxGuidanceLength / 3);
        var result = FeishuGuidanceComposer.Compose(
            string.Empty,
            [Block("first", third), Block("second", third), Block("third", third)]);

        result.Truncated.Should().BeTrue("第三个域已放不下");
        result.IncludedDomains.Should().Equal(["first", "second"]);
        result.OmittedDomains.Should().Equal(new[] { "third" }, "丢弃清单必须可见（否则超限是静默的）");
        result.Instructions.Length.Should().BeLessThanOrEqualTo(FeishuGuidanceComposer.MaxGuidanceLength);
    }

    /// <summary>
    /// 宿主指令超过上限<b>不再</b>导致域资产全丢（P1-6）：额度只计 guidance 本体。
    /// </summary>
    /// <remarks>
    /// 本用例是对旧行为的<b>有意改写</b>：旧实现以 <c>builder.Length</c>（初值 = 宿主指令）为判据，
    /// 企业 system prompt（常见 &gt; 2048 字符）会让全部域 guidance 静默丢弃——那是缺陷而非契约。
    /// </remarks>
    [Fact]
    public void Compose_AfterHostOverLimit_ShouldStillRespectGuidanceLimit()
    {
        var hostOverLimit = new string('h', FeishuGuidanceComposer.MaxGuidanceLength + 1);

        var result = FeishuGuidanceComposer.Compose(hostOverLimit, [Block("bitable", "多维表格。")]);

        result.IncludedDomains.Should().Equal(["bitable"], "宿主指令长度不得挤占域资产预算（P1-6）");
        result.OmittedDomains.Should().BeEmpty();
        result.Truncated.Should().BeFalse();
        result.Instructions.Should().Contain("多维表格。");
    }

    [Fact]
    public void Compose_ShouldBudgetGuidanceOnly_NotHostInstructions()
    {
        // 域 guidance 自身超限时仍按 2048 截断（不看宿主长度）。
        var third = new string('x', FeishuGuidanceComposer.MaxGuidanceLength / 3);
        var result = FeishuGuidanceComposer.Compose(
            new string('h', 3000),
            [Block("first", third), Block("second", third), Block("third", third)]);

        result.IncludedDomains.Should().Equal(["first", "second"]);
        result.OmittedDomains.Should().Equal("third");
        result.Truncated.Should().BeTrue();

        var guidanceLength = result.Instructions.Length - 3000;
        guidanceLength.Should().BeLessThanOrEqualTo(FeishuGuidanceComposer.MaxGuidanceLength,
            "上限只约束 guidance 段（含分隔符）");
    }

    [Fact]
    public void FeishuAgent_ShouldExposeGuidanceResult_WithOmittedDomains()
    {
        var third = new string('x', FeishuGuidanceComposer.MaxGuidanceLength / 3);
        var agent = new FeishuAgent(
            CreateMockClient().Object,
            new FeishuAgentOptions { Instructions = "宿主指令", MaxHistoryMessages = 10 },
            domainGuidance: [Block("first", third), Block("second", third), Block("third", third)]);

        agent.Guidance.Truncated.Should().BeTrue();
        agent.Guidance.OmittedDomains.Should().Equal(["third"],
            "丢弃清单必须可从 Agent 上断言（超限不再只以日志形式存在）");
        agent.Guidance.IncludedDomains.Should().Equal(["first", "second"]);
    }

    // ───────────────────── ④ 端到端：指令装配的唯一消费点 ─────────────────────

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
