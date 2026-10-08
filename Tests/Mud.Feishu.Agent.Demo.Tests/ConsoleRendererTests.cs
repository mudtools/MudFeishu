// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// <see cref="ConsoleRenderer"/> 的安全与一致性：ANSI 注入过滤、换行归一、续行缩进、表格。
/// </summary>
public class ConsoleRendererTests
{
    /// <summary>
    /// 飞书文档正文是<b>不可信输入</b>：CSI（清屏/改颜色）与 OSC（改窗口标题）都必须被剔除。
    /// </summary>
    [Fact]
    public void AgentDelta_Should_FilterAnsiEscapes()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();

        renderer.AgentPrefix();
        renderer.AgentDelta("\u001B[2Jhello\u001B]0;evil-title\u0007 world\u001B[31m!\u001B[0m");
        renderer.AgentEnd();

        var text = output.ToString();

        text.Should().NotContain("\u001B", "任何 ESC 开头序列都必须在写终端之前被剔除");
        text.Should().NotContain("\u0007", "OSC 的 BEL 终止符也应随序列一起丢弃");
        text.Should().NotContain("evil-title", "OSC 载荷（可改窗口标题）不得泄漏到终端");
        text.Should().Contain("hello");
        text.Should().Contain("world");
        text.Should().Contain("!");
    }

    /// <summary>单字节 CSI 形态（<c>\x9B</c>）同样必须被剔除。</summary>
    [Fact]
    public void StripAnsi_Should_HandleSingleByteCsi()
    {
        ConsoleRenderer.StripAnsi("a\u009B2Jb").Should().Be("ab");
        ConsoleRenderer.StripAnsi("plain").Should().Be("plain");
        ConsoleRenderer.StripAnsi(null).Should().BeEmpty();
    }

    /// <summary>
    /// 换行归一：内容里的 <c>\r\n</c> 与裸 <c>\r</c> 都必须被归一为单个换行
    /// （否则流式增量会把行覆盖搞乱）；输出换行本身用平台换行符。
    /// </summary>
    [Fact]
    public void AgentDelta_Should_NormalizeNewlines()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();

        renderer.AgentPrefix();
        renderer.AgentDelta("line1\r\nline2\rline3");
        renderer.AgentEnd();

        var expected =
            "🤖 line1" + Environment.NewLine
            + "   line2" + Environment.NewLine
            + "   line3" + Environment.NewLine;

        output.ToString().Should().Be(
            expected,
            "裸 CR 不得残留（否则终端回退覆盖会让流式输出错行）");
    }

    /// <summary>多行回答的续行必须缩进对齐到 <c>🤖 </c> 之后（视觉上归属同一回答）。</summary>
    [Fact]
    public void AgentDelta_Should_IndentContinuationLines()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();

        renderer.AgentPrefix();
        renderer.AgentDelta("first\nsecond");
        renderer.AgentEnd();

        var text = output.ToString();
        var lines = text.Split('\n');

        lines[0].Should().StartWith("🤖 first");
        lines[1].Should().StartWith("   second", "续行应与 🤖 之后的文本对齐");
    }

    /// <summary>分批下发（模拟流式分片）与一次下发的渲染结果必须一致。</summary>
    [Fact]
    public void AgentDelta_Should_BeChunkingInvariant()
    {
        var (oneShot, oneShotOutput) = TestDoubles.CreateRenderer();
        oneShot.AgentPrefix();
        oneShot.AgentDelta("alpha\nbeta");
        oneShot.AgentEnd();

        var (chunked, chunkedOutput) = TestDoubles.CreateRenderer();
        chunked.AgentPrefix();
        chunked.AgentDelta("al");
        chunked.AgentDelta("pha\n");
        chunked.AgentDelta("beta");
        chunked.AgentEnd();

        chunkedOutput.ToString().Should().Be(oneShotOutput.ToString());
    }

    /// <summary>空增量不得产生任何输出（控制台通道会频繁下发空串）。</summary>
    [Fact]
    public void AgentDelta_Should_IgnoreEmptyDelta()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();

        renderer.AgentPrefix();
        renderer.AgentDelta(string.Empty);
        renderer.AgentEnd();

        output.ToString().Should().Be("🤖 " + Environment.NewLine);
    }

    /// <summary>表格：列标题与单元格都要出现，且宽度可计算（CJK 计 2 列）。</summary>
    [Fact]
    public void Table_Should_RenderAllCells()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();

        renderer.Table(
            "审计",
            ["工具", "判定"],
            [
                ["docx.delete_blocks", "denied"],
                ["sheets.append_rows", "allowed"],
            ]);

        var text = output.ToString();
        text.Should().Contain("审计");
        text.Should().Contain("工具");
        text.Should().Contain("docx.delete_blocks");
        text.Should().Contain("sheets.append_rows");
        text.Should().Contain("denied");
        text.Should().Contain("allowed");
    }

    /// <summary>多行正文（如 guidance 的 Markdown 原文）用 Block 输出，行结构必须保留。</summary>
    [Fact]
    public void Block_Should_PreserveLineStructure()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();

        renderer.Block("guidance · docx", "# 标题\r\n- 要点 A\r\n- 要点 B");

        var lines = output.ToString().Split(Environment.NewLine);
        lines.Should().Contain(static l => l.Contains("# 标题"));
        lines.Should().Contain(static l => l.Contains("- 要点 A"));
        lines.Should().Contain(static l => l.Contains("- 要点 B"));
        lines.Should().OnlyContain(static l => !l.Contains('\r'), "裸 CR 不得残留");
    }

    /// <summary>表格单元格内的换行必须折叠，否则多行内容会撑破行结构。</summary>
    [Fact]
    public void Table_Should_FoldNewlinesInsideCells()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();

        renderer.Table("t", ["列"], [["第一行\n第二行"]]);

        output.ToString().Split(Environment.NewLine)
            .Should().Contain(static l => l.Contains("第一行 第二行"));
    }

    /// <summary>终端宽度不可得时必须回退（管道/重定向场景下 <c>Console.WindowWidth</c> 会抛）。</summary>
    [Fact]
    public void SafeWidth_Should_ReturnPositiveFallback()
    {
        ConsoleRenderer.SafeWidth().Should().BePositive();
    }

    /// <summary>输出目标（含 stderr）整体失效时不得抛出。</summary>
    [Fact]
    public void Writes_Should_NotThrow_When_OutputWriterFails()
    {
        var (renderer, _) = TestDoubles.CreateRenderer();
        var broken = new ConsoleRenderer(new BrokenWriter(), new BrokenWriter());

        var act = () =>
        {
            broken.AgentPrefix();
            broken.AgentDelta("hello");
            broken.AgentEnd();
            broken.Notice(NoticeLevel.Error, "boom");
            broken.Trace("trace");
            broken.ToolCallCard(new ToolCallViewModel("t", "write", "allowed", 1, "a", null));
            broken.Table("t", ["c"], [["v"]]);
        };

        act.Should().NotThrow("渲染是展示面，任何输出失败都不得中断执行链");
        renderer.Should().NotBeNull();
    }

    /// <summary>写入即抛的 <see cref="TextWriter"/>（模拟终端被关闭）。</summary>
    private sealed class BrokenWriter : TextWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        public override void Write(string? value) => throw new IOException("模拟终端不可用");
    }
}
