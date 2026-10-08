// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// <b>R5 / F-2</b>：取值闭集读取器（<c>ToolArgs</c> 的 4 个新 helper）的运行时行为断言。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：<c>FeishuToolArgs</c> 产物受<b>程序集名门槛</b>限制发射
/// （<c>FeishuToolSchemaGenerator.cs:120-126</c>：产物消费 FeishuTools 的 <c>internal ToolArgs</c>，
/// 其他程序集一并发射会 CS0103）⇒ <b>driver 测试观测不到 Args 侧产物</b>。
/// Args 侧因此缺一道行为验证：若生成器发射了错误的读取器调用，driver 全绿也发现不了。
/// </para>
/// <para>
/// <b>核心不变量</b>：<b>非法值必须抛且错误文案附合法值清单</b>——这是 F-8
/// 「<c>suggestions</c> 等价物」的来源；只抛"参数非法"而不给清单，F-8 无从自动填充。
/// </para>
/// </remarks>
public class ToolArgsClosedSetHelperTests
{
    private static readonly Dictionary<string, int> BlockTypeMap = new(StringComparer.Ordinal)
    {
        ["Page"] = 1,
        ["Text"] = 2,
        ["Heading1"] = 3,
    };

    private static Dictionary<string, object?> A(string key, object? value)
        => new(StringComparer.Ordinal) { [key] = value };

    private static Dictionary<string, object?> Empty => new(StringComparer.Ordinal);

    /// <summary>常量名 → 平台整数值：合法值必须正确映射（R5 / R-4 的核心目的）。</summary>
    [Fact]
    public void RequireNamedInt_ShouldMapLegalConstantName_ToPlatformValue()
        => ToolArgs.RequireNamedInt(A("block_type", "Heading1"), "block_type", BlockTypeMap).Should().Be(3);

    /// <summary>非法常量名必须抛，<b>且文案附合法值清单</b>（F-8 的 suggestions 来源）。</summary>
    [Fact]
    public void RequireNamedInt_ShouldThrowWithLegalValueList_WhenNameIsUnknown()
    {
        var error = ((Action)(() => ToolArgs.RequireNamedInt(A("block_type", "标题1"), "block_type", BlockTypeMap)))
            .Should().Throw<ArgumentException>().Which;

        error.Message.Should().Contain("block_type");
        error.Message.Should().Contain("标题1", "错误文案应回显模型给错的原值");
        error.Message.Should().Contain("Page").And.Contain("Text").And.Contain("Heading1",
            "错误文案必须附合法值清单，否则模型无法自我纠正（F-8 的 suggestions 等价物）");
    }

    /// <summary>缺失必填闭集参数必须抛（不得静默返回默认值）。</summary>
    [Fact]
    public void RequireNamedInt_ShouldThrow_WhenArgumentMissing()
        => ((Action)(() => ToolArgs.RequireNamedInt(Empty, "block_type", BlockTypeMap)))
            .Should().Throw<ArgumentException>();

    /// <summary>可选闭集参数：缺失/空白 ⇒ null；给出则映射。</summary>
    [Fact]
    public void OptionalNamedInt_ShouldReturnNull_WhenMissing_AndMapWhenPresent()
    {
        ToolArgs.OptionalNamedInt(Empty, "block_type", BlockTypeMap).Should().BeNull();
        ToolArgs.OptionalNamedInt(A("block_type", "   "), "block_type", BlockTypeMap).Should().BeNull();
        ToolArgs.OptionalNamedInt(A("block_type", "Text"), "block_type", BlockTypeMap).Should().Be(2);
    }

    /// <summary>可选闭集参数<b>给了非法值仍须抛</b>——“可选”不等于“不校验”。</summary>
    [Fact]
    public void OptionalNamedInt_ShouldStillThrow_WhenPresentButUnknown()
        => ((Action)(() => ToolArgs.OptionalNamedInt(A("block_type", "nope"), "block_type", BlockTypeMap)))
            .Should().Throw<ArgumentException>();

    /// <summary>C# enum 闭集：合法成员名解析成功；非法成员名抛 + 文案附清单。</summary>
    [Fact]
    public void RequireEnum_ShouldParseLegalName_AndThrowWithListOnUnknown()
    {
        ToolArgs.RequireEnum<SampleEnum>(A("mode", "Fast"), "mode").Should().Be(SampleEnum.Fast);

        var error = ((Action)(() => ToolArgs.RequireEnum<SampleEnum>(A("mode", "Turbo"), "mode")))
            .Should().Throw<ArgumentException>().Which;

        error.Message.Should().Contain("Turbo");
        error.Message.Should().Contain(nameof(SampleEnum.Fast)).And.Contain(nameof(SampleEnum.Slow),
            "错误文案必须附合法成员清单");
    }

    /// <summary>
    /// <b>关键语义</b>：<c>Enum.TryParse</c> 对<b>未定义但数值可解析</b>的值静默成功
    /// （enum{Fast=1,Slow=2} 传 "99"），必须被 <c>Enum.IsDefined</c> 拦下。
    /// </summary>
    /// <remarks>
    /// 这是“模型猜了一个数字”最常见的情形；不拦则非法值一路带到 SDK 才炸，错误信息对模型毫无指向性。
    /// 先自证前提（TryParse 确实接受 "99"），否则前提消失时本用例会假绿。
    /// </remarks>
    [Fact]
    public void RequireEnum_ShouldRejectUndefinedNumericValue_WhichTryParseAloneWouldAccept()
    {
        System.Enum.TryParse<SampleEnum>("99", ignoreCase: false, out _).Should().BeTrue(
            "前提不成立：TryParse 已拒绝未定义数值 ⇒ 本用例失去意义，请重新评估");

        ((Action)(() => ToolArgs.RequireEnum<SampleEnum>(A("mode", "99"), "mode")))
            .Should().Throw<ArgumentException>("未定义数值必须被 IsDefined 拦下");
    }

    /// <summary>可选 enum：缺失 ⇒ null；合法值则解析；非法值仍抛。</summary>
    [Fact]
    public void OptionalEnum_ShouldReturnNull_WhenMissing_AndThrowWhenPresentButUnknown()
    {
        ToolArgs.OptionalEnum<SampleEnum>(Empty, "mode").Should().BeNull();
        ToolArgs.OptionalEnum<SampleEnum>(A("mode", "Slow"), "mode").Should().Be(SampleEnum.Slow);

        ((Action)(() => ToolArgs.OptionalEnum<SampleEnum>(A("mode", "nope"), "mode")))
            .Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// <b>反向自证</b>：合法值清单必须真的从<b>映射表</b>派生，而非硬编码——换一张表，清单随之改变。
    /// </summary>
    [Fact]
    public void LegalValueList_ShouldBeDerivedFromTheMap_NotHardcoded()
    {
        var custom = new Dictionary<string, int>(StringComparer.Ordinal) { ["Only"] = 42 };

        var error = ((Action)(() => ToolArgs.RequireNamedInt(A("k", "other"), "k", custom)))
            .Should().Throw<ArgumentException>().Which;

        error.Message.Should().Contain("Only");
        error.Message.Should().NotContain("Page", "清单来自映射表，换表后不应再出现旧表的键");
    }

    /// <summary>测试用枚举（<c>IsDefined</c> 语义需要真实 enum，不能用常量类替代）。</summary>
    private enum SampleEnum
    {
        Fast = 1,
        Slow = 2,
    }
}