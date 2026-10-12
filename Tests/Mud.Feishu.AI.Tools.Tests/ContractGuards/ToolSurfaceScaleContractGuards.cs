// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// G4（R2-09）：<b>工具面规模数字必须与唯一真相源一致</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>缺陷原始形态</b>：<c>Mud.Feishu.AI.Tools.csproj</c> 写「产出本包 <b>24</b> 个工具接口的
/// Schema 常量」，<c>Readme.md</c> 的 L2 层写「<b>24</b> 个工具」——实测 <c>[FeishuTool]</c> 与
/// <c>[FeishuToolHandler]</c> 都是 <b>53</b>。这类漂移<b>没有任何编译器或现有门禁能发现</b>：
/// 数字写在注释/文档里，宿主据此判断"这个包能干什么"，误差 2 倍以上。
/// </para>
/// <para>
/// <b>判据选择（与方案初稿的差异）</b>：方案初稿给的是"断言 csproj/README 中的工具数与 golden 条目数一致
/// <b>或</b>断言文档不含该数字"——后者等于放弃信息（读者拿不到规模感）。本守卫取<b>前者</b>，
/// 并把它扩展成<b>三源一致</b>：golden 快照行数 ≡ <c>FeishuToolNames.All</c> ≡
/// <c>FeishuToolContracts.AllNames</c>，再断言文档/工程文件里出现的裸数字等于该值。
/// 于是"改工具面忘了改文档"与"改文档时笔误"都被机械拦下。
/// </para>
/// <para>
/// <b>自证报红</b>：<see cref="Scanner_ShouldReportDocumentedCount_ThatMismatchesTruth"/> 用合成文本
/// 驱动同一扫描器并断言报红。
/// </para>
/// </remarks>
public class ToolSurfaceScaleContractGuards
{
    /// <summary>被断言"不得写错规模数字"的文件（注释/文档中的裸数字）。</summary>
    private static readonly string[] CountBearingFiles =
    [
        "Mud.Feishu.AI.Tools/Mud.Feishu.AI.Tools.csproj",
        "Mud.Feishu.AI.Tools/Readme.md",
        "Mud.Feishu.AI/Readme.md",

        // BUG-6：状态真相源文档纳入扫描面——它是"当前规模"的唯一声明处，
        // 若它自己写错数字，整个系列的漂移治理就落空（本行即该治理的机械保障）。
        ".docs/AI/状态基线.md",
    ];

    /// <summary>三个真相源必须两两一致（golden 快照 / 名字契约表 / 类型化契约表）。</summary>
    [Fact]
    public void ToolCount_ShouldBeIdenticalAcrossGoldenNamesAndContracts()
    {
        var goldenCount = ReadGoldenEntryCount();
        var namesCount = FeishuToolNames.All.Length;
        var contractsCount = FeishuToolContracts.AllNames.Length;

        goldenCount.Should().BeGreaterThan(0, "golden 快照不得为空（空快照会让 MUDFT014 静默失效）");
        namesCount.Should().Be(goldenCount,
            "FeishuToolNames.All 与 golden 快照同源同 pass 产出，条目数必须一致");
        contractsCount.Should().Be(goldenCount,
            "FeishuToolContracts.AllNames 与 golden 快照同源同 pass 产出，条目数必须一致");

        FeishuToolNames.ReadonlyAll.Length.Should().Be(FeishuToolNames.All.Length - FeishuToolNames.WriteAll.Length,
            "ReadonlyAll + WriteAll 必须恰好等于 All（拆分口径不得漏项或重复）");
    }

    /// <summary>文档与工程文件中的"&lt;N&gt; 个工具"不得与真相源不一致。</summary>
    [Fact]
    public void DocumentedToolCount_ShouldMatchTheTruthSource()
    {
        var truth = ReadGoldenEntryCount();
        var violations = new List<string>();
        var declarations = 0;

        foreach (var relativePath in CountBearingFiles)
        {
            var path = Path.Combine(FindRepositoryRoot(), relativePath);
            File.Exists(path).Should().BeTrue($"{relativePath} 必须存在（扫描面不得静默为空）");

            var text = File.ReadAllText(path);
            var found = ToolScaleScanner.FindToolCountDeclarations(text, truth);
            declarations += found.Count;

            violations.AddRange(
                found.Where(static d => !d.IsConsistent)
                    .Select(d => $"{relativePath}:{d.Line} 写的是 {d.DeclaredCount}，真相源是 {truth}（{d.Snippet}）"));
        }

        declarations.Should().BeGreaterThan(0,
            "扫描必须至少命中一处规模声明——否则本守卫在扫描面为空时也会绿（假门禁）");
        violations.Should().BeEmpty(
            "以下位置的工具数量与 golden 快照 / 契约表不一致（宿主会据此误判工具面规模）："
            + string.Join(" | ", violations));
    }

    /// <summary>检查器自证：对"写错的裸数字"必须报红。</summary>
    [Fact]
    public void Scanner_ShouldReportDocumentedCount_ThatMismatchesTruth()
    {
        // 缺陷原始形态：csproj 的「24 个工具接口」注释（真相源是 53）。
        var found = ToolScaleScanner.FindToolCountDeclarations(
            "<!-- 本包产出 24 个工具接口的 Schema 常量 -->", truth: 53);

        found.Should().HaveCount(1, "检查器必须能定位规模声明（找不到说明正则坏了——那是假绿）");
        found[0].DeclaredCount.Should().Be(24);
        found[0].IsConsistent.Should().BeFalse("24 与真相源 53 不一致时 IsConsistent 必须为 false");
    }

    /// <summary>检查器自证：数字写对时不得报红。</summary>
    [Fact]
    public void Scanner_ShouldAcceptDocumentedCount_ThatMatchesTruth()
    {
        var truth = ReadGoldenEntryCount();
        var found = ToolScaleScanner.FindToolCountDeclarations(
            $"| L2 | 标注了 `[FeishuTool]` 的 {truth} 个工具 |", truth);

        found.Should().HaveCount(1);
        found[0].IsConsistent.Should().BeTrue("与真相源一致的数字不得报红（否则守卫会误伤正确文档）");
    }

    /// <summary>
    /// C1 新守卫：对照表数据行总数 == <c>FeishuToolNames.All</c> 条目数。
    /// </summary>
    /// <remarks>
    /// 只读表数据行数 + 写类表数据行数 = 全部工具数。
    /// 金丝雀：在对照表中删一行即红。
    /// </remarks>
    [Fact]
    public void PermissionDoc_RowCount_ShouldMatchToolCount()
    {
        var docPath = Path.Combine(
            FindRepositoryRoot(), "documents", "AIAgent", "工具权限对照表.md");
        File.Exists(docPath).Should().BeTrue("对照表必须存在");

        var lines = File.ReadAllLines(docPath);
        var dataRows = lines
            .Where(static l => l.StartsWith("| ", StringComparison.Ordinal)
                && !l.StartsWith("| #", StringComparison.Ordinal)
                && !l.StartsWith("| ---", StringComparison.Ordinal)
                && !l.StartsWith("| 域", StringComparison.Ordinal)
                && !l.Contains("合计", StringComparison.Ordinal)
                && l.Split('|', StringSplitOptions.RemoveEmptyEntries).Length >= 3)
            .Count();

        var truth = FeishuToolNames.All.Length;
        dataRows.Should().Be(truth,
            $"对照表数据行总数 ({dataRows}) 必须等于 FeishuToolNames.All 条目数 ({truth})——" +
            "只读表 + 写类表的总数据行数 = 全部工具数");
    }

    // ────────── 读取 ──────────

    private static int ReadGoldenEntryCount()
        => File
            .ReadAllLines(Path.Combine(
                FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "FeishuToolSchemas.golden.txt"))
            .Count(static line => !string.IsNullOrWhiteSpace(line));

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

/// <summary>
/// "工具规模数字"扫描器（可被合成文本驱动 ⇒ 支持检查器自证）。
/// </summary>
/// <remarks>
/// 只识别 <c>&lt;N&gt; 个工具</c> 这一种写法（本仓库文档的实际形态），不做模糊匹配——
/// 判据窄才能保证不误报；新增别的写法时同批扩展本扫描器与自证用例。
/// </remarks>
internal static class ToolScaleScanner
{
    /// <summary>一处规模声明。</summary>
    /// <param name="Line">行号（1 起）。</param>
    /// <param name="DeclaredCount">声明中的数字。</param>
    /// <param name="IsConsistent">是否与真相源一致。</param>
    /// <param name="Snippet">该行原文（定位用）。</param>
    internal sealed record ToolCountDeclaration(int Line, int DeclaredCount, bool IsConsistent, string Snippet);

    /// <summary>扫描文本中的规模声明，并按给定真相源判定一致性。</summary>
    /// <param name="text">文件/片段全文。</param>
    /// <param name="truth">真相源条目数（golden 快照行数）。</param>
    /// <returns>声明清单（含与真相源的一致性判定）。</returns>
    /// <remarks>
    /// 真相源作为<b>参数</b>传入而不是静态状态：xUnit 默认让不同测试类并行，
    /// 任何"测试期间可变"的共享静态量都会变成不可复现的偶发红。
    /// </remarks>
    public static IReadOnlyList<ToolCountDeclaration> FindToolCountDeclarations(string text, int truth)
    {
        var declarations = new List<ToolCountDeclaration>();
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            // 显式限定类型：本工程全局 using 了 Moq，其 `Moq.Match` 与正则的 `Match` 同名。
            // R7/WP2-T2-3：原正则 `(?<count>\d+)\s*个工具` 无法匹配「31 个：24 只读 + 7 写类」形态，
            // 扩展为同时匹配「N 个工具」与「N 个：…」/「N 个（…）」等写法。
            foreach (System.Text.RegularExpressions.Match match in Regex.Matches(lines[i], @"(?<count>\d+)\s*个(?:工具|：|（|\()"))
            {
                if (!int.TryParse(match.Groups["count"].Value, out var count))
                {
                    continue;
                }

                declarations.Add(new ToolCountDeclaration(
                    Line: i + 1,
                    DeclaredCount: count,
                    IsConsistent: count == truth,
                    Snippet: lines[i].Trim()));
            }
        }

        return declarations;
    }
}
