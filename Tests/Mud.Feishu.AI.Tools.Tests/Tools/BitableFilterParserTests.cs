// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Bitable filter 简化文法解析器单测（§3.3.3）：合法文法 → <c>RecordQueryFilterInfo</c>；
/// 超 5 子句 / 嵌套 or / 语法错误 → 结构化失败。
/// </summary>
public class BitableFilterParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_ShouldTreatBlankAsNoFilter(string? filter)
    {
        var ok = BitableFilterParser.TryParse(filter, out var parsed, out var error);

        ok.Should().BeTrue();
        parsed.Should().BeNull();
        error.Should().BeNull();
    }

    [Fact]
    public void TryParse_ShouldMapEqualsClause()
    {
        var ok = BitableFilterParser.TryParse("状态 = \"done\"", out var parsed, out var error);

        ok.Should().BeTrue();
        error.Should().BeNull();
        parsed!.Conjunction.Should().Be("and");
        parsed.Conditions.Should().ContainSingle();
        parsed.Conditions![0].FieldName.Should().Be("状态");
        parsed.Conditions[0].Operator.Should().Be("is");
        parsed.Conditions[0].Value.Should().Equal(["done"]);
    }

    [Fact]
    public void TryParse_ShouldMapContainsClause_AndBareValue()
    {
        var ok = BitableFilterParser.TryParse("owner contains 张三", out var parsed, out var error);

        ok.Should().BeTrue();
        parsed!.Conditions![0].Operator.Should().Be("contains");
        parsed.Conditions[0].Value.Should().Equal(["张三"]);
    }

    [Fact]
    public void TryParse_ShouldJoinClausesWithAnd()
    {
        var ok = BitableFilterParser.TryParse("状态 = done and owner contains 张三 and 金额 = 100", out var parsed, out var error);

        ok.Should().BeTrue();
        parsed!.Conjunction.Should().Be("and");
        parsed.Conditions.Should().HaveCount(3);
    }

    [Fact]
    public void TryParse_ShouldRejectMoreThanFiveClauses()
    {
        var filter = string.Join(" and ", Enumerable.Range(1, 6).Select(i => $"f{i} = v{i}"));

        var ok = BitableFilterParser.TryParse(filter, out var parsed, out var error);

        ok.Should().BeFalse();
        parsed.Should().BeNull();
        error.Should().Contain("最多 5 个子句");
    }

    [Theory]
    [InlineData("a = 1 or b = 2")]
    [InlineData("(a = 1) and (b = 2)")]
    [InlineData("a = 1 and (b = 2)")]
    [InlineData("a === 1")]
    [InlineData("a 1")]
    [InlineData("contains x")]
    public void TryParse_ShouldRejectUnsupportedGrammar(string filter)
    {
        var ok = BitableFilterParser.TryParse(filter, out var parsed, out var error);

        ok.Should().BeFalse();
        parsed.Should().BeNull();
        error.Should().NotBeNullOrWhiteSpace().And.Contain("filter 语法不支持");
    }
}
