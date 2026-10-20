// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// 长正文窗口切片（<c>Mud.Feishu.AI.Tools/Results/ToolContentWindow.cs</c>）的归属用例。
/// </summary>
/// <remarks>
/// 逐字稿与演示文稿 XML 共用同一份窗口口径，故用例也共用：<b>续读游标链条</b>
/// （第 N 段 == 总长的第 N 片，且末段 <c>next_offset</c> 为 null）与<b>越界如实回填</b>
/// 是这条语义的两个不可退化点——任一退化都会让模型「读不到后半段」。
/// </remarks>
public class ToolContentWindowTests
{
    private const string OffsetName = "content_offset";
    private const string LimitName = "content_limit";

    private static string Body(int length) => new string('甲', length);

    [Fact]
    public void Compute_Should_Chain_NextOffset_Across_Slices()
    {
        var content = Body(250);

        var first = ToolContentWindow.Compute(content, null, 100, OffsetName, LimitName);
        first.Text.Should().Be(content.Substring(0, 100));
        first.TotalLength.Should().Be(250);
        first.HasMore.Should().BeTrue();
        first.NextOffset.Should().Be(100);

        var second = ToolContentWindow.Compute(content, first.NextOffset, 100, OffsetName, LimitName);
        second.Text.Should().Be(content.Substring(100, 100));
        second.OffsetOutOfRange.Should().BeFalse();

        var last = ToolContentWindow.Compute(content, second.NextOffset, 100, OffsetName, LimitName);
        last.Text.Should().Be(content.Substring(200, 50));
        last.HasMore.Should().BeFalse();
        last.NextOffset.Should().BeNull();
    }

    [Fact]
    public void Compute_Should_Use_Default_Length_When_Limit_Absent()
    {
        var content = Body(ToolContentWindow.DefaultLength + 5);

        var window = ToolContentWindow.Compute(content, 0, null, OffsetName, LimitName);

        window.Limit.Should().Be(ToolContentWindow.DefaultLength);
        window.Text.Length.Should().Be(ToolContentWindow.DefaultLength);
        window.HasMore.Should().BeTrue();
    }

    [Fact]
    public void Compute_Should_Flag_Offset_Out_Of_Range_When_Offset_Exceeds_Total()
    {
        var window = ToolContentWindow.Compute(Body(100), 5000, 100, OffsetName, LimitName);

        window.Text.Should().BeEmpty();
        window.Offset.Should().Be(100);
        window.HasMore.Should().BeFalse();
        window.OffsetOutOfRange.Should().BeTrue("空串不得被模型读成「没有内容」");
    }

    [Fact]
    public void Compute_Should_Yield_Empty_Window_When_Content_Null()
    {
        var window = ToolContentWindow.Compute(null, null, null, OffsetName, LimitName);

        window.Text.Should().BeEmpty();
        window.TotalLength.Should().Be(0);
        window.HasMore.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Reject_Negative_Offset()
    {
        var act = () => ToolContentWindow.Validate(-1, null, OffsetName, LimitName);

        act.Should().Throw<ArgumentException>().WithMessage($"*{OffsetName}*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(50001)]
    public void Validate_Should_Reject_Limit_Out_Of_Bounds(int limit)
    {
        var act = () => ToolContentWindow.Validate(0, limit, OffsetName, LimitName);

        act.Should().Throw<ArgumentException>().WithMessage($"*{LimitName}*");
    }

    [Fact]
    public void Validate_Should_Accept_Boundary_Limits()
    {
        var act = () => ToolContentWindow.Validate(0, ToolContentWindow.MaxLength, OffsetName, LimitName);

        act.Should().NotThrow();
    }
}
