// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Guidance;

/// <summary>
/// 域级 guidance 装配用例（<b>纯字符串函数</b>，零产品依赖）：未启用不注入 / 启用即注入 /
/// 超限截断 + 可断言信号 / 宿主指令恒在前 / 预算只计 guidance 本体 / 预算参数必须为正。
/// </summary>
/// <remarks>
/// <para>
/// 截断信号落在 <see cref="GuidanceResult"/> 上（<c>Truncated</c> + 丢弃清单），
/// 用例<b>不依赖日志断言基建</b>。
/// </para>
/// <para>
/// <b>预算取值的身份（P1-H2a）</b>：本类被测的是<b>产品中立</b>的装配机制，
/// 故用测试自有常量——不得引用任何产品预算（那会把飞书语义焊进中性包的用例）。
/// 产品侧额度（如 <c>FeishuGuidanceBudget.MaxGuidanceLength</c>）由其自己的用例锁定，
/// 见 <c>Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/GuidanceAssetContractGuards.cs</c>
/// 的"全域零丢弃"不变量。
/// </para>
/// </remarks>
public class GuidanceComposerTests
{
    /// <summary>测试自有预算（小型值 ⇒ 截断用例的字符串规模可控，且与任何产品常量解耦）。</summary>
    private const int Budget = 32768;

    private static GuidanceBlock Block(string domain, string content) => new(domain, content);

    // ───────────────────── ① 未启用任何工具：不污染指令 ─────────────────────

    [Fact]
    public void Compose_WithoutBlocks_ShouldLeaveHostInstructionsUnchanged()
    {
        var result = GuidanceComposer.Compose("你是飞书助手", [], Budget);

        result.Instructions.Should().Be("你是飞书助手");
        result.Truncated.Should().BeFalse();
        result.IncludedDomains.Should().BeEmpty();
    }

    [Fact]
    public void Compose_WithNullInputs_ShouldReturnEmptyInstructions()
    {
        var result = GuidanceComposer.Compose(null, null, Budget);

        result.Instructions.Should().BeEmpty();
        result.Truncated.Should().BeFalse();
    }

    // ───────────────────── ② 启用某域工具：指令含该域 guidance，且在宿主指令之后 ─────────────────────

    [Fact]
    public void Compose_ShouldAppendGuidanceAfterHostInstructions()
    {
        var result = GuidanceComposer.Compose(
            "你是飞书助手",
            [Block("bitable", "多维表格：先 list_tables。")],
            Budget);

        result.Instructions.Should().StartWith("你是飞书助手", "宿主指令优先，域 guidance 只是补充");
        result.Instructions.Should().Contain("多维表格：先 list_tables。");
        result.IncludedDomains.Should().Equal("bitable");
        result.Truncated.Should().BeFalse();
    }

    [Fact]
    public void Compose_WithEmptyHost_ShouldProduceGuidanceOnly_WithoutLeadingSeparator()
    {
        var result = GuidanceComposer.Compose("", [Block("docx", "云文档正文。")], Budget);

        result.Instructions.Should().Be("云文档正文。", "宿主指令为空时不得插入前导空行");
    }

    [Fact]
    public void Compose_ShouldPreserveBlockOrder_AndSkipBlankBlocks()
    {
        var result = GuidanceComposer.Compose(
            "host",
            [Block("task", "任务域。"), Block("empty", "   "), Block("im", "消息域。")],
            Budget);

        result.IncludedDomains.Should().Equal(["task", "im"], "空块不占额度也不进清单");
        result.Instructions.Should().Contain("任务域。");
        result.Instructions.Should().Contain("消息域。");
    }

    // ───────────────────── ③ 超限：截断 + 丢弃清单可见 ─────────────────────

    [Fact]
    public void Compose_OverLimit_ShouldTruncateTail_AndReportOmittedDomains()
    {
        // 每块约占上限的 1/3：前两块放得下，第三块触发截断（含分隔符开销）。
        var third = new string('x', Budget / 3);
        var result = GuidanceComposer.Compose(
            string.Empty,
            [Block("first", third), Block("second", third), Block("third", third)],
            Budget);

        result.Truncated.Should().BeTrue("第三个域已放不下");
        result.IncludedDomains.Should().Equal(["first", "second"]);
        result.OmittedDomains.Should().Equal(new[] { "third" }, "丢弃清单必须可见（否则超限是静默的）");
        result.Instructions.Length.Should().BeLessThanOrEqualTo(Budget);
    }

    /// <summary>
    /// 宿主指令超过上限<b>不再</b>导致域资产全丢：额度只计 guidance 本体。
    /// </summary>
    /// <remarks>
    /// 本用例是对旧行为的<b>有意改写</b>：旧实现以 <c>builder.Length</c>（初值 = 宿主指令）为判据，
    /// 企业 system prompt（常见 &gt; 上限）会让全部域 guidance 静默丢弃——那是缺陷而非契约。
    /// </remarks>
    [Fact]
    public void Compose_AfterHostOverLimit_ShouldStillRespectGuidanceLimit()
    {
        var hostOverLimit = new string('h', Budget + 1);

        var result = GuidanceComposer.Compose(hostOverLimit, [Block("bitable", "多维表格。")], Budget);

        result.IncludedDomains.Should().Equal(["bitable"], "宿主指令长度不得挤占域资产预算");
        result.OmittedDomains.Should().BeEmpty();
        result.Truncated.Should().BeFalse();
        result.Instructions.Should().Contain("多维表格。");
    }

    [Fact]
    public void Compose_ShouldBudgetGuidanceOnly_NotHostInstructions()
    {
        // 域 guidance 自身超限时仍按预算截断（不看宿主长度）。
        var third = new string('x', Budget / 3);
        var result = GuidanceComposer.Compose(
            new string('h', 3000),
            [Block("first", third), Block("second", third), Block("third", third)],
            Budget);

        result.IncludedDomains.Should().Equal(["first", "second"]);
        result.OmittedDomains.Should().Equal("third");
        result.Truncated.Should().BeTrue();

        var guidanceLength = result.Instructions.Length - 3000;
        guidanceLength.Should().BeLessThanOrEqualTo(Budget, "上限只约束 guidance 段（含分隔符）");
    }

    // ───────────────────── ④ 预算参数契约 ─────────────────────

    /// <summary>
    /// 预算必须为正：非正值会让全部域资产静默丢弃，故本类<b>不做可选参数</b>（无默认值可退回），
    /// 由 fail-fast 明确拒绝。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Compose_WithNonPositiveBudget_ShouldFailFast(int budget)
    {
        var act = () => GuidanceComposer.Compose("host", [Block("bitable", "内容。")], budget);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxGuidanceLength");
    }

    /// <summary>
    /// 预算参数<b>真的生效</b>（不是被忽略的装饰）：同一输入在更小预算下必须触发截断。
    /// </summary>
    [Fact]
    public void Compose_ShouldHonorProvidedBudget_NotAHiddenConstant()
    {
        var block = Block("bitable", new string('x', 100));

        var tight = GuidanceComposer.Compose("host", [block], 50);
        var loose = GuidanceComposer.Compose("host", [block], 200);

        tight.Truncated.Should().BeTrue("50 字符预算放不下 100 字符的域资产");
        tight.OmittedDomains.Should().Equal("bitable");
        loose.Truncated.Should().BeFalse("200 字符预算足够");
    }
}
