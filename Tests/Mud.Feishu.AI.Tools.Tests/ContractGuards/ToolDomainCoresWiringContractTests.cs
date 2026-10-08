// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / S-13（根治）</b>：每个执行器类生成的 <c>AddFeishuXxxCore</c> 都<b>必须</b>被
/// 手写的聚合入口 <c>FeishuToolsServiceCollectionExtensions</c> 调用。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要这条守卫（S-13 的真实教训）</b>：新增执行器类后，生成器会<b>照常</b>产出
/// AddFeishuXxxCore 与 XxxToolDomainRegistrar.g.cs，编译通过、MUDFT022（未绑定执行器）与
/// MUDFT025（构造参数不可解析）<b>均为 Error 且全部干净</b>——它们只检查"绑定"与"构造参数"，
/// <b>没有任何检查覆盖"聚合入口是否调用了该 Core"</b>。
/// </para>
/// <para>
/// 而真正调用 Core 的是<b>人工维护</b>的两张表（AddFeishuReadonlyToolCores /
/// AddFeishuWriteToolCores）。漏加一行 ⇒ Core 永不执行 ⇒ 该域工具<b>静默不入注册表</b>：
/// 能力目录里没有、FeishuToolNames.All 里却有，且<b>没有任何构建期信号</b>。
/// 这是典型的"声明存在 ≠ 生效"，只能靠守卫。
/// </para>
/// </remarks>
public class ToolDomainCoresWiringContractTests
{
    private const string InternalDirectory = "Mud.Feishu.AI.Tools/Internal";

    private const string ExtensionsFile =
        "Mud.Feishu.AI.Tools/Extensions/FeishuToolsServiceCollectionExtensions.cs";

    /// <summary>从执行器类声明反推应被聚合的 Core 名（XxxTools → AddFeishuXxxCore）。</summary>
    private static SortedSet<string> ReadExpectedCoreNames()
    {
        var directory = Path.Combine(
            FindRepositoryRoot(), InternalDirectory.Replace('/', Path.DirectorySeparatorChar));

        var expected = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(directory, "*Tools.cs", SearchOption.TopDirectoryOnly))
        {
            var source = File.ReadAllText(file);
            foreach (System.Text.RegularExpressions.Match match in Regex.Matches(
                source, @"internal\s+sealed\s+class\s+(?<n>\w+Tools)\b"))
            {
                // Core 名<b>保留</b>尾部 Tools：WikiTools → AddFeishuWikiToolsCore。
                expected.Add("AddFeishu" + match.Groups["n"].Value + "Core");
            }
        }

        return expected;
    }

    /// <summary>聚合入口里实际被调用到的 Core 方法名。</summary>
    private static HashSet<string> ReadWiredCoreNames()
    {
        var source = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), ExtensionsFile.Replace('/', Path.DirectorySeparatorChar)));

        return Regex.Matches(source, @"(?<name>AddFeishu\w+Core)\s*\(")
            .Select(static m => m.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    [Fact]
    public void ToolDomainCores_ShouldAllBeWiredIntoTheAggregator()
    {
        var expected = ReadExpectedCoreNames();
        var wired = ReadWiredCoreNames();

        expected.Should().NotBeEmpty(
            "未从 Internal/ 解析出任何执行器类——路径或正则坏了（假绿），请先修守卫");

        var missing = expected.Except(wired).OrderBy(static n => n, StringComparer.Ordinal).ToArray();

        missing.Should().BeEmpty(
            "以下执行器类的 AddFeishuXxxCore 未被手写聚合入口调用：{0}。"
            + "后果是该域工具静默不入注册表（能力目录里没有、FeishuToolNames.All 里却有，"
            + "且 MUDFT022/025 全绿、没有任何构建期信号）——R5/S-13",
            string.Join(" | ", missing));
    }

    /// <summary>反向自证：守卫必须真的看得见 Core 名，否则上例会因"扫不到 ⇒ 空集合"而假绿。</summary>
    [Fact]
    public void Guard_ShouldActuallySeeCoreNames()
    {
        var expected = ReadExpectedCoreNames();
        var wired = ReadWiredCoreNames();

        expected.Should().Contain("AddFeishuWikiToolsCore", "判据失效：未能从 WikiTools 推出 Core 名");
        wired.Should().Contain("AddFeishuWikiToolsCore", "判据失效：未能从聚合入口读到 WikiTools 的 Core");
        wired.Should().Contain("AddFeishuMailToolsCore");
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