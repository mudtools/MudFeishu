// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// bitable.query_records sort 简化文法解析器测试（AI-FD-D12 P1D-1b 批次 A）：
/// 合法 / 越界 / 语法错误三类用例。
/// </summary>
public class BitableSortParserTests
{
    [Fact]
    public void TryParse_ShouldAcceptNullAndEmpty()
    {
        BitableSortParser.TryParse(null, out var parsed, out var error).Should().BeTrue();
        parsed.Should().BeEmpty();
        error.Should().BeNull();
    }

    [Fact]
    public void TryParse_ShouldParseDirectionAndMultipleClauses()
    {
        var ok = BitableSortParser.TryParse(["状态:desc", "姓名:asc"], out var parsed, out var error);

        ok.Should().BeTrue(error);
        parsed.Should().HaveCount(2);
        parsed[0].FieldName.Should().Be("状态");
        parsed[0].Desc.Should().BeTrue();
        parsed[1].FieldName.Should().Be("姓名");
        parsed[1].Desc.Should().BeFalse();
    }

    [Fact]
    public void TryParse_ShouldSupportCommaSeparatedSingleElement_AndCaseInsensitiveDirection()
    {
        var ok = BitableSortParser.TryParse(["a:DESC,b:Asc"], out var parsed, out var error);

        ok.Should().BeTrue(error);
        parsed.Select(p => p.FieldName).Should().Equal("a", "b");
        parsed[0].Desc.Should().BeTrue();
        parsed[1].Desc.Should().BeFalse();
    }

    [Fact]
    public void TryParse_ShouldRejectBadSyntax()
    {
        BitableSortParser.TryParse(["状态"], out var parsed, out var error).Should().BeFalse("缺少方向段");
        error.Should().Contain("语法不支持");
        parsed.Should().BeEmpty();

        BitableSortParser.TryParse(["状态:up"], out _, out error).Should().BeFalse("方向仅 asc/desc");
        error.Should().Contain("方向不支持");

        BitableSortParser.TryParse([":asc"], out _, out error).Should().BeFalse("字段名不能为空");
        error.Should().Contain("字段名不能为空");
    }

    [Fact]
    public void TryParse_ShouldRejectMoreThanThreeClauses()
    {
        BitableSortParser.TryParse(["a:asc", "b:desc", "c:asc", "d:desc"], out var parsed, out var error)
            .Should().BeFalse("最多 3 个子句（入参推断稳定性）");
        error.Should().Contain("最多 3");
        parsed.Should().BeEmpty();
    }
}
