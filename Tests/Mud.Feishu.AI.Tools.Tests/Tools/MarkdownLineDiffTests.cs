// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// <see cref="MarkdownLineDiff"/> 的纯函数用例：归一化、增删统计、截断与规模退化。
/// </summary>
public class MarkdownLineDiffTests
{
    [Fact]
    public void Compute_WhenOnlyNewlineStyleDiffers_ShouldReportUnchanged()
    {
        var result = MarkdownLineDiff.Compute("a\r\nb\r\n", "a\nb", 1024);

        result.Changed.Should().BeFalse("CRLF/LF 与末尾单个空行不是内容变化");
        result.AddedLines.Should().Be(0);
        result.RemovedLines.Should().Be(0);
        result.Text.Should().BeEmpty();
    }

    [Fact]
    public void Compute_ShouldCountInsertionsAndDeletionsAcrossHunks()
    {
        var left = "1\n2\n3\n4\n5\n6\n7\n8\n9\n10";
        var right = "1\n2\nX\n4\n5\n6\n7\n8\n9\n10\n11";

        var result = MarkdownLineDiff.Compute(left, right, 4096);

        result.Changed.Should().BeTrue();
        result.AddedLines.Should().Be(2);
        result.RemovedLines.Should().Be(1);
        result.Text.Should().StartWith("@@");
        result.Text.Should().Contain("+X").And.Contain("+11");
        result.Coarse.Should().BeFalse();
    }

    [Fact]
    public void Compute_ShouldTruncateAtLineBoundaryAndReportTotalLength()
    {
        var left = string.Join("\n", Enumerable.Range(0, 200).Select(i => $"line-{i}"));
        var right = string.Join("\n", Enumerable.Range(0, 200).Select(i => $"changed-{i}"));

        var result = MarkdownLineDiff.Compute(left, right, 512);

        result.Truncated.Should().BeTrue();
        result.Text.Length.Should().BeLessThanOrEqualTo(512, "截断发生在行边界之外的上限内");
        result.TotalLength.Should().BeGreaterThan(result.Text.Length);
        result.AddedLines.Should().Be(200, "增删统计是全量，不受文本截断影响");
        result.RemovedLines.Should().Be(200);
    }

    [Fact]
    public void Compute_WhenProductExceedsLcsBudget_ShouldFallBackToCoarseDiff()
    {
        // 1001 × 1001 > 1,000,000 单元格 ⇒ 走粗粒度路径。
        var left = string.Join("\n", Enumerable.Range(0, 1001).Select(i => $"l{i}"));
        var right = string.Join("\n", Enumerable.Range(0, 1001).Select(i => $"r{i}"));

        var result = MarkdownLineDiff.Compute(left, right, 4096);

        result.Coarse.Should().BeTrue("超大输入必须退化而不是分配 n×m 的 DP 表");
        result.Changed.Should().BeTrue();
        result.AddedLines.Should().Be(1001);
        result.RemovedLines.Should().Be(1001);
    }

    [Fact]
    public void Compute_WithEmptySides_ShouldHandleNullAndEmpty()
    {
        MarkdownLineDiff.Compute(null, null, 256).Changed.Should().BeFalse();

        var added = MarkdownLineDiff.Compute(string.Empty, "a\nb", 256);
        added.Changed.Should().BeTrue();
        added.AddedLines.Should().Be(2);
        added.RemovedLines.Should().Be(0);

        var removed = MarkdownLineDiff.Compute("a\nb", null, 256);
        removed.Changed.Should().BeTrue();
        removed.RemovedLines.Should().Be(2);
    }
}
