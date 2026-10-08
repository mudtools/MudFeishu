// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

using Mud.Feishu.DataModels.Docx;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / B-1</b>：<see cref="BlockTypes"/> 常量表守卫—— 防止"常量 / 文档 / 反查表"三面漂移。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要这条守卫</b>：<c>Block.BlockType</c> 的取值清单原本只存在于 XML 注释里，
/// 于是"平台新增/调整块类型"这件事没有任何机制能被发现。本守卫把三件事钉在一起：
/// <list type="number">
/// <item><b>常量集合 == 文档清单</b>：常量类里的每个值都能在 <c>Block.cs</c> 的注释清单中找到依据；</item>
/// <item><b>文档清单 == 常量集合</b>：反向也成立（漏登记的块类型同样会被发现）；</item>
/// <item><b>常量集合 == 反查表</b>：<see cref="BlockTypes.All"/> / <see cref="BlockTypes.IsValid"/>
/// / <see cref="BlockTypes.GetName"/> 三者口径一致（防止"有常量但反查表漏了"）。</item>
/// </list>
/// </para>
/// <para>
/// <b>注意保留号 16</b>：官方清单从 15 跳到 17，本仓以 <see cref="BlockTypes.Reserved"/>
/// 显式登记而<b>不臆造语义</b>（R5 实施纪律①：平台事实不凭推测补全）。
/// <see cref="BlockTypes.GetName"/> 对<b>未知</b>值返回 <see cref="BlockTypes.UnknownName"/> 而非抛异常。
/// </para>
/// </remarks>
public class BlockTypesContractTests
{
    private static readonly string BlockFilePath = Path.Combine(
        FindRepositoryRoot(), "Mud.Feishu.DataModels", "Docx", "Common", "Block.cs");

    /// <summary>
    /// 常量集合与 XML 注释清单<b>双向一致</b>（防漂移；新增块类型必须同批改两处）。
    /// </summary>
    [Fact]
    public void BlockTypes_Constants_ShouldMatchDocumentationInventoryBidirectionally()
    {
        var constants = ReadDeclaredConstants();
        constants.Should().NotBeEmpty("未能从 BlockTypes 反射出任何常量——守卫本身坏了（假绿）");

        var documented = ReadDocumentedValues();

        // ① 常量 → 文档：每个常量都必须有文档依据。
        var undocumented = constants.Keys.Where(value => !documented.Contains(value)).OrderBy(v => v).ToArray();
        undocumented.Should().BeEmpty(
            "BlockTypes 中的常量在 Block.BlockType 的 XML 注释清单里没有对应条目（凭空出现的平台值）：{0}",
            string.Join(", ", undocumented));

        // ② 文档 → 常量：注释里的每个取值都必须登记为常量。
        var unregistered = documented.Where(value => !constants.ContainsKey(value)).OrderBy(v => v).ToArray();
        unregistered.Should().BeEmpty(
            "Block.BlockType 注释清单中的取值未登记到 BlockTypes（常量表落后于文档）：{0}",
            string.Join(", ", unregistered));
    }

    /// <summary>
    /// 保留号 <b>16</b> 必须显式登记 —— 官方清单从 15 跳到 17，若哪天文档补上了 16，
    /// 本用例会因"文档多出未登记取值"而红，提示<b>把Reserved 换成真实语义</b>。
    /// </summary>
    [Fact]
    public void BlockTypes_ShouldRegisterPlatformReservedSlot16_WithoutInventingSemantics()
    {
        BlockTypes.Reserved.Should().Be(16);
        BlockTypes.GetName(BlockTypes.Reserved).Should().Be(
            nameof(BlockTypes.Reserved),
            "保留号应登记为 Reserved 而非臆造语义名");
    }

    /// <summary>常量集合与反查表（<see cref="BlockTypes.All"/>）必须一致，且无重复。</summary>
    [Fact]
    public void BlockTypes_LookupTable_ShouldBeConsistentWithConstants()
    {
        var constants = ReadDeclaredConstants().Keys.OrderBy(static v => v).ToArray();
        var all = BlockTypes.All.OrderBy(static v => v).ToArray();

        all.Should().Equal(constants, "BlockTypes.All 与常量集合不一致（反查表漏项或多项）");
        all.Should().OnlyHaveUniqueItems("BlockTypes.All 出现重复取值");
    }

    /// <summary>
    /// <see cref="BlockTypes.GetName"/> 对<b>未知</b>取值必须返回
    /// <see cref="BlockTypes.UnknownName"/> 而<b>不抛异常</b> —— 否则未知块型会让整个响应解析失败。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(53)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void BlockTypes_GetName_ShouldReturnUnknownNameInsteadOfThrowing(int unknownValue)
    {
        BlockTypes.IsValid(unknownValue).Should().BeFalse($"{unknownValue} 不是已登记的块类型");
        BlockTypes.GetName(unknownValue).Should().Be(
            BlockTypes.UnknownName,
            "未知块型必须返回 unknown 而不是抛异常（否则单个新块型会导致整份文档解析失败）");
    }

    /// <summary>每个已登记取值的反查名称必须<b>非空且等于其常量名</b>（可读的诊断输出依赖它）。</summary>
    [Fact]
    public void BlockTypes_GetName_ShouldReturnConstantName_ForEveryRegisteredValue()
    {
        var constants = ReadDeclaredConstants();

        var problems = new List<string>();
        foreach (var (value, constantName) in constants)
        {
            var name = BlockTypes.GetName(value);
            if (!string.Equals(name, constantName, StringComparison.Ordinal))
            {
                problems.Add($"{value}:期望 {constantName}，实际 {name}");
            }
        }

        problems.Should().BeEmpty("BlockTypes 反查名称与常量名不一致：{0}", string.Join(" | ", problems));
    }

    // ────────── 读取 ──────────

    /// <summary>反射读取 <see cref="BlockTypes"/> 的全部 <c>public const int</c>（取值 → 常量名）。</summary>
    private static Dictionary<int, string> ReadDeclaredConstants()
    {
        var literals = typeof(BlockTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(int));

        var fields = new Dictionary<int, string>();
        foreach (var field in literals)
        {
            var value = (int)field.GetRawConstantValue();
            fields[value] = field.Name;
        }

        // All / IsValid / GetName 是成员不是常量，GetRawConstantValue 对非字面量会抛异常——上面已按 IsLiteral 过滤。
        return fields;
    }

    /// <summary>
    /// 从 <c>Block.cs</c> 的 <c>BlockType</c> XML 注释里解析"已文档化的取值"。
    /// </summary>
    /// <remarks>
    /// <b>为什么解析区间</b>：注释在 R5 / B-1 后改为"按值域分组 + 指向 <see cref="BlockTypes"/> 的引用"，
    /// 因此本守卫解析形如 <c>3~11：</c>、<c>36~39：</c>、<c>16：</c>、<c>999：</c>、<c>1：</c> 的记法，
    /// 并展开区间。这是与"文档清单"保持机械对应的唯一可靠方式。
    /// </remarks>
    private static HashSet<int> ReadDocumentedValues()
    {
        var source = File.ReadAllText(BlockFilePath);
        var documented = new HashSet<int>();

        // 匹配 "<数字>：说明" 与 "<数字>~<数字>：说明" 两种记法（注释内）。
        foreach (System.Text.RegularExpressions.Match match in Regex.Matches(source, @"(?<from>\d+)\s*(?:~(?<to>\d+))?\s*[：:]"))
        {
            var from = int.Parse(match.Groups["from"].Value, CultureInfo.InvariantCulture);
            var to = match.Groups["to"].Success
                ? int.Parse(match.Groups["to"].Value, CultureInfo.InvariantCulture)
                : from;

            for (var value = from; value <= to; value++)
            {
                documented.Add(value);
            }
        }

        documented.Should().NotBeEmpty("未能从 Block.cs 注释解析出任何取值——解析规则已失效（假绿）");
        return documented;
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}