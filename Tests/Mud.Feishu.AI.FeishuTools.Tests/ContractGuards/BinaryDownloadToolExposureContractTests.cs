// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / B-2</b>：AI 工具面的<b>二进制防线</b> —— 断言<b>没有任何工具能把二进制内容送进 JSON 结果</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>架构事实（决定了防线该建在哪）</b>：本仓的工具体面是<b>两层、且目录分离</b>的——
/// <list type="bullet">
/// <item><b>工具接口</b>（<c>Mud.Feishu.AI.FeishuTools/Tools/Feishu*ToolInterfaces.cs</c>）声明
/// <c>[FeishuTool]</c> 方法，返回 JSON 可序列化类型；</item>
/// <item><b>SDK 接口</b>（<c>Mud.Feishu/Interfaces/**</c>）提供底层能力，<b>从不</b>携带
/// <c>[FeishuTool]</c>（实测：含二进制方法的 13 个接口，工具计数全为 0）。</item>
/// </list>
/// 两层靠 <c>[FeishuTool(Source = "接口名.方法名")]</c> 交叉引用。
/// </para>
/// <para>
/// <b>所以真正的暴露向量只有一个</b>：<b><c>Source</c> 指向了返回二进制的 SDK 方法</b>。
/// 此时工具方法声明的返回类型"看起来是正常 JSON"，但执行器会把 SDK 的
/// <c>byte[]</c> 结果回填进 <c>result.Content</c> ⇒ 撑爆上下文并击穿 SSE 帧。
/// 这就是本守卫锁定的<b>唯一且真实</b>的失效路径。
/// </para>
/// <para>
/// <b>为什么不是"统一 N 处注释"</b>（R5 对原文方案的修订）：原文修法是"统一 15 处
/// <c>Task&lt;byte[]?&gt;</c> 的注释口径"。实测为 <b>16 个方法 / 13 个接口</b>，且注释
/// <b>不改变任何行为</b>；真正的风险是"某天有人把下载方法挂上工具、或写进 <c>Source</c>"，
/// 那一刻<b>没有任何机制会拦住</b>。⇒ 把人工不变量升级为机械约束，成本更低、覆盖更全。
/// </para>
/// <para>
/// <b>三条断言</b>：① 工具的 <c>Source</c> 不得指向二进制返回方法（核心）；② 工具方法返回类型
/// 不得是二进制（第二道，覆盖未走 <c>Source</c> 的手写实现）；③ 反向自证——扫描器必须真的
/// 看得见工具与二进制方法，否则"扫不到 = 假绿"。
/// </para>
/// </remarks>
public class BinaryDownloadToolExposureContractTests
{
    // R5 / F-1：工具声明面已从 Tools/ 迁到 Curation/（载体 C：策展面与基础设施物理分离）。
    private const string ToolInterfacesDirectory = "Mud.Feishu.AI.FeishuTools/Curation";
    private const string SdkInterfacesDirectory = "Mud.Feishu/Interfaces";

    /// <summary>二进制返回类型的<b>正则形态</b>（与 <c>Mud.HttpUtils</c> 的下载分支判定同源）。</summary>
    private const string BinaryReturnPattern =
        @"Task\s*<\s*(?:byte\s*\[\s*\]\s*\?|Stream|ReadOnlyMemory\s*<\s*byte\s*>)\s*>";

    /// <summary>
    /// "二进制返回方法"的完整形态：<b>返回类型紧跟方法名</b>（如
    /// <c>Task&lt;byte[]?&gt; DownloadFileAsync(</c>）。
    /// </summary>
    /// <remarks>
    /// <b>为什么必须前向匹配而不是"从返回类型往前回溯"</b>：返回类型上方常压着
    /// <c>[Get("/open-apis/…/{file_token}/download")]</c> 等带括号的特性，前向回溯会命中特性名
    /// （实测<b>一个都匹配不到</b>，防线静默失效）。方法名恒在返回类型<b>之后</b>，故只前向。
    /// </remarks>
    private const string BinaryMethodPattern =
        BinaryReturnPattern + @"\s+(?<name>\w+)\s*\(";

    /// <summary>
    /// <b>核心断言</b>：<c>[FeishuTool]</c> 的 <c>Source</c> 不得指向返回二进制的 SDK 方法。
    /// </summary>
    [Fact]
    public void ToolSources_ShouldNeverPointToBinaryReturningSdkMethods()
    {
        var binaryMethods = CollectBinarySdkMethods();
        binaryMethods.Should().NotBeEmpty(
            "未解析到任何二进制返回的 SDK 方法——防线无从校验（扫描失效），请先修守卫");

        var binaryNames = binaryMethods
            .Select(static entry => entry.Split('|')[0])
            .ToHashSet(StringComparer.Ordinal);

        var violations = new List<string>();
        foreach (var (toolFile, toolName, source) in CollectToolSourceReferences())
        {
            // Source 形如 "IFeishuTenantV1DriveFiles.BatchQueryMetasAsync"（也允许带命名空间前缀）。
            var referenced = source.Split('.').Last();
            if (binaryNames.Contains(referenced))
            {
                violations.Add($"{toolFile}::{toolName} → Source=\"{source}\"");
            }
        }

        violations.Should().BeEmpty(
            "以下工具的 [FeishuTool(Source=…)] 指向了返回二进制的 SDK 方法 ⇒ 二进制会被回填进工具 JSON 结果"
            + "（撑爆上下文并击穿 SSE）。修法：改用返回临时 URL 的能力（如 GetDownloadUrlAsync），"
            + "或把下载能力移出工具面：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// 工具方法自身的返回类型不得是二进制（第二道防线，覆盖未走 <c>Source</c> 的手写实现）。
    /// </summary>
    [Fact]
    public void ToolMethods_ShouldNeverDeclareBinaryReturnType()
    {
        var violations = new List<string>();

        foreach (var file in EnumerateFiles(ToolInterfacesDirectory))
        {
            foreach (var (toolName, block) in SplitToolMethodBlocks(File.ReadAllText(file)))
            {
                if (Regex.IsMatch(block, BinaryReturnPattern))
                {
                    violations.Add($"{Path.GetFileName(file)}::{toolName}");
                }
            }
        }

        violations.Should().BeEmpty(
            "以下 [FeishuTool] 方法声明了二进制返回类型——工具结果必须是可序列化 JSON：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// 反向自证：扫描器必须<b>同时</b>看得见工具 <c>Source</c> 与二进制 SDK 方法。
    /// 本仓已吃过一次"扫描失效 = 假绿"的亏（见 <c>CapabilityEqualityFieldCoverageTests</c> 的表达式体教训）。
    /// </summary>
    [Fact]
    public void Scanner_ShouldSeeBothToolSourcesAndBinaryMethods_OtherwiseGuardsAreFalseGreen()
    {
        CollectToolSourceReferences().Should().NotBeEmpty(
            $"未扫到任何 [FeishuTool(Source=…)]（路径 {ToolInterfacesDirectory} 是否已失效？）");

        CollectBinarySdkMethods().Should().NotBeEmpty(
            $"未扫到任何二进制返回的 SDK 方法（路径 {SdkInterfacesDirectory} 是否已失效？）");
    }

    /// <summary>
    /// 基线锁定：二进制方法数 / 接口数都是<b>有意登记的存量</b>，变动时必须显式更新期望值——
    /// 避免"新增下载方法"这类有意的接口面扩张静默发生。
    /// </summary>
    [Fact]
    public void BinarySdkMethods_ShouldMatchRegisteredBaseline()
    {
        // 2026-10-06 实测（R5 / B-2）：16 个方法 / 13 个接口。
        const int ExpectedMethodCount = 16;
        const int ExpectedInterfaceCount = 13;

        var methodCount = CollectBinarySdkMethods().Count;
        var interfaceCount = EnumerateFiles(SdkInterfacesDirectory)
            .Count(path => Regex.IsMatch(File.ReadAllText(path), BinaryReturnPattern));

        methodCount.Should().Be(
            ExpectedMethodCount,
            $"二进制返回方法数从 {ExpectedMethodCount} 变为 {methodCount}——"
            + "新增/删除下载方法属有意的接口面扩张，请同步更新本基线与 B-2 文档");

        interfaceCount.Should().Be(
            ExpectedInterfaceCount,
            $"含二进制方法的接口数从 {ExpectedInterfaceCount} 变为 {interfaceCount}——请同步更新本基线");
    }

    // ────────── 采集实现 ──────────

    /// <summary>
    /// 采集 SDK 侧全部二进制返回方法，返回 "方法名|所在文件名" 集合（带文件名以消解同名歧义）。
    /// </summary>
    private static HashSet<string> CollectBinarySdkMethods()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in EnumerateFiles(SdkInterfacesDirectory))
        {
            var source = File.ReadAllText(path);
            foreach (System.Text.RegularExpressions.Match match in Regex.Matches(source, BinaryMethodPattern))
            {
                names.Add($"{match.Groups["name"].Value}|{Path.GetFileName(path)}");
            }
        }

        return names;
    }

    /// <summary>采集工具侧全部 <c>[FeishuTool]</c> 的工具名与 <c>Source</c> 值。</summary>
    private static IEnumerable<(string ToolFile, string ToolName, string Source)> CollectToolSourceReferences()
    {
        foreach (var path in EnumerateFiles(ToolInterfacesDirectory))
        {
            var fileName = Path.GetFileName(path);
            foreach (var (toolName, block) in SplitToolMethodBlocks(File.ReadAllText(path)))
            {
                var source = Regex.Match(block, @"Source\s*=\s*""(?<value>[^""]+)""");
                if (source.Success)
                {
                    yield return (fileName, toolName, source.Groups["value"].Value);
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateFiles(string relativeDirectory)
    {
        var root = Path.Combine(FindRepositoryRoot(), relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(root))
        {
            yield break;
        }

        foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            yield return path;
        }
    }

    /// <summary>
    /// 把源码按 <c>[FeishuTool(</c> 切块，每块含"特性 + 其后方法签名"，返回（方法名, 块文本）。
    /// </summary>
    private static IEnumerable<(string ToolName, string Block)> SplitToolMethodBlocks(string source)
    {
        var starts = Regex.Matches(source, @"\[FeishuTool\s*\(");
        for (var i = 0; i < starts.Count; i++)
        {
            var from = starts[i].Index;
            var to = i + 1 < starts.Count ? starts[i + 1].Index : source.Length;
            var block = source[from..to];

            var name = Regex.Match(block, @"\b(?<name>\w+)\s*\(");
            yield return (name.Success ? name.Groups["name"].Value : "?", block);
        }
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