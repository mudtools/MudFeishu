// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>
/// 手写运行时层的注释纪律守卫（三条判据：评审编号必须可解析；过程叙述必须给出存在的文档链接；
/// 注释预算不得越过冻结基线）。
/// </summary>
/// <remarks>
/// <para>
/// <b>拦截的真实回归（2026-10-14 注释审计）</b>：<c>ConversationSummarizer.cs</c> 的 7 次提交累计新增
/// 461 行、删除 19 行，其中 157 行为注释；两轮审查批次只增注释（1 增 1 注释、5 增 4 注释）。
/// 注释是追加式介质——审查要求"证明想过"，而无人度量"是否可删"，于是过程叙事与悬空编号沉淀为维护成本
/// （审计时 36 个评审编号在全仓任何文档里都查不到，读者无法解析）。
/// </para>
/// <para>
/// <b>定量口径</b>：冻结基线在 <see cref="FeishuDocs.CommentBudgetBaseline"/>（逐文件注释行数与最长注释块），
/// 只允许下压；清理后用 <c>pwsh ./scripts/comment-audit.ps1 -UpdateBaseline</c> 重新固化，
/// 固化的 diff 必须连同 CHANGELOG 一并提交评审。
/// </para>
/// </remarks>
public class CommentConventionContractGuards
{
    private const string UpdateFlag = "FeishuCommentBaselineUpdate";

    private static readonly string[] ScopedProjects =
    [
        "Mud.Feishu.AI",
        "Mud.AI.Agent",
        "Mud.AI.Abstractions",
        "Mud.Feishu.AI.Mcp",
    ];

    private static readonly Lazy<CommentConventionAnalysis> Analysis = new(() =>
        CommentConventionAnalysis.Run(FindRepositoryRoot(), ScopedProjects, ResolveAnchor, LinkExists));

    private static readonly Lazy<string> GovernedDocs = new(ReadGovernedDocs);

    /// <summary>注释里的评审编号必须能在受治理文档里被解析，否则读者无法追溯。</summary>
    [Fact]
    public void AnchorReferences_ShouldResolveToGovernedDocuments()
    {
        var analysis = Analysis.Value;
        analysis.Files.Count.Should().BeGreaterThan(100, "扫描面为空即假绿（工程目录或过滤器坏了）");
        analysis.CommentLines.Should().BeGreaterThan(5000, "扫描到的注释行数下界（扫描面被截断即假绿）");

        var baseline = LoadBaseline(analysis);
        AssertBaselineIsNotPartOfResolutionSource();

        var introduced = analysis.UnresolvedAnchors.Except(baseline.Anchors).ToList();

        introduced.Should().BeEmpty(
            "以下评审编号在 documents/ 与 CHANGELOG.md 里都不存在，属于悬空引用（读者无法追溯）。"
            + "请改写为约束本身，或先把决策/背景登记进文档再引用。重新固化（仅下压，不得用于放行新违规）："
            + "pwsh ./scripts/comment-audit.ps1 -UpdateBaseline → " + string.Join(" | ", introduced));
    }

    /// <summary>叙述历史/过程的注释块必须给出**存在**的文档链接，否则过程叙事应留在提交信息里。</summary>
    [Fact]
    public void ProcessNarration_ShouldCarryExistingDocumentLink()
    {
        var analysis = Analysis.Value;
        var baseline = LoadBaseline(analysis);
        var introduced = analysis.UnlinkedNarration.Except(baseline.Narration).ToList();

        introduced.Should().BeEmpty(
            "以下注释块在叙述「曾经怎样、哪一轮改的」却没有指向存在的文档：过程叙事属于提交信息/CHANGELOG，"
            + "代码里只留约束与取舍链接。请改写为约束，或补 documents/… 或 ADR-nnnn 链接 → "
            + string.Join(" | ", introduced));
    }

    /// <summary>注释行数与最长注释块不得越过冻结基线；新文件受硬上限。</summary>
    [Fact]
    public void CommentBudget_ShouldNotExceedFrozenValues()
    {
        var analysis = Analysis.Value;
        var baseline = LoadBaseline(analysis);

        CommentBaselineJson.ReadScope(File.ReadAllText(BaselinePath()))
            .SequenceEqual(ScopedProjects)
            .Should().BeTrue("基线的作用域必须与守卫一致（作用域漂移会让一批文件静默脱管）");

        var unfrozen = analysis.Files.Keys.Except(baseline.Files.Keys).ToList();
        unfrozen.Should().BeEmpty(
            "以下文件不在基线里（新增或改名）。若为新增，请确认注释量后重新固化入账；若为改名，"
            + "请在同一提交内重新固化 → " + string.Join(" | ", unfrozen));

        var vanished = baseline.Files.Keys.Except(analysis.Files.Keys).ToList();
        vanished.Should().BeEmpty(
            "以下基线条目对应的文件已不存在（删除或改名）。删除即下压，请重新固化以同步台账 → "
            + string.Join(" | ", vanished));

        var violations = FindBudgetViolations(analysis, baseline);

        violations.Should().BeEmpty(
            "注释预算越界：冻结文件不得增长，新文件受硬上限。"
            + "请压缩叙述型长文（决策外移到 documents/AIAgent/decisions/），而不是重新固化以放行 → "
            + string.Join(" | ", violations));
    }

    /// <summary>自证：编号不可解析时必须报红。</summary>
    [Theory]
    [InlineData("/// 见 R2-01 的约定", "R2-01")]
    [InlineData("// P2D-3b：摘要输入压缩", "P2D-3b")]
    [InlineData("/// <item>AT-F14 判据</item>", "AT-F14")]
    [InlineData("/// T2 / D2 的结论", "T2 / D2")]
    [InlineData("/// 按 DP-R7-4 的裁定", "DP-R7-4")]
    public void Scanner_ShouldReportUnresolvedAnchor(string source, string expected)
        => CommentConventionScanner.FindUnresolvedAnchors(source, static _ => false).Should().Contain(
            expected, "不可解析的评审编号必须报红，否则本守卫是假绿");

    /// <summary>自证：编号可解析、或记号不在注释里时不得报红（只认起首 <c>//</c>，本仓不使用行尾注释）。</summary>
    [Theory]
    [InlineData("/// 见 R2-01 的约定", true)]
    [InlineData("/// <item>AT-F14 判据</item>", true)]
    [InlineData("/// 普通说明", false)]
    [InlineData("var token = \"R2-01\";", false)]
    [InlineData("var x = 1; // 见 R2-01", false)]
    [InlineData("/// 标识符里的 xR7-4foo 不算编号", false)]
    public void Scanner_ShouldNotReportAnchor(string source, bool resolves)
        => CommentConventionScanner.FindUnresolvedAnchors(source, _ => resolves).Should().BeEmpty();

    /// <summary>自证：叙述型注释块缺链接时报红；同块给出存在的链接、或本就不含叙述词时不得报红。</summary>
    [Theory]
    [InlineData("/// 此前只被主路径使用", false, true)]
    [InlineData("/// 此前只被主路径使用\n/// 见 documents/AIAgent/decisions/ADR-0001-x.md", false, true)]
    [InlineData("/// 此前只被主路径使用\n/// 见 documents/AIAgent/decisions/ADR-0001-x.md", true, false)]
    [InlineData("/// 已删除零调用的 Data / FromData", false, true)]
    [InlineData("/// 任务已删除或解析不出负责人", false, false)]
    [InlineData("/// 引用消息可能已删除/无权限，跳过即可", false, false)]
    [InlineData("/// 空 user 消息会被 400 拒绝", false, false)]
    public void Scanner_ShouldRequireExistingLink_ForNarration(string source, bool linkExists, bool expected)
    {
        var violations = CommentConventionScanner.FindUnlinkedNarration(source, _ => linkExists);

        if (expected)
        {
            violations.Should().NotBeEmpty("无链接（或链接指向不存在的文档）的叙述型注释必须报红");
        }
        else
        {
            violations.Should().BeEmpty("块内有存在的链接，或本就不含叙述词 —— 不得报红");
        }
    }

    /// <summary>自证：度量口径（代码行断开叙述块；逐参数文档行不计入块长但计入注释行数）。</summary>
    [Theory]
    [InlineData("// a\n// b\nvar x = 1;\n// c", 4, 3, 2)]
    [InlineData("var x = 1;", 1, 0, 0)]
    [InlineData("// a\n// b\n// c", 3, 3, 3)]
    [InlineData("/// <summary>\n/// 一行说明\n/// </summary>\n/// <param name=\"x\">逐参数文档</param>\n/// <remarks>\n/// 叙述\n/// </remarks>", 7, 7, 3)]
    public void Scanner_ShouldMeasureComments(string source, int lines, int comments, int maxBlock)
        => CommentConventionScanner.Measure(source).Should().Be(new FileCommentStats(lines, comments, maxBlock));

    /// <summary>自证：预算判据（冻结值增长报红，余量内放行；新文件超硬上限报红）。</summary>
    [Theory]
    [InlineData(100, 30, 20, -1, -1, -1, false)]
    [InlineData(100, 34, 20, 100, 30, 20, true)]
    [InlineData(100, 33, 21, 100, 30, 20, true)]
    [InlineData(100, 33, 20, 100, 30, 20, false)]
    public void Scanner_ShouldFlagBudgetOverrun(
        int lines,
        int comments,
        int maxBlock,
        int frozenLines,
        int frozenComments,
        int frozenMaxBlock,
        bool expected)
    {
        var stats = new FileCommentStats(lines, comments, maxBlock);
        var frozen = frozenLines < 0 ? (FileCommentStats?)null : new FileCommentStats(frozenLines, frozenComments, frozenMaxBlock);
        var violations = CommentConventionScanner.FindBudgetViolations("Proj/File.cs", stats, frozen);

        if (expected)
        {
            violations.Should().NotBeEmpty("越过冻结值或新文件硬上限必须报红");
        }
        else
        {
            violations.Should().BeEmpty();
        }
    }

    /// <summary>
    /// 判定：编号/叙述的解析源必须排除冻结基线**自身**。
    /// </summary>
    /// <remarks>
    /// 基线台账里印着全部未解析记号，一旦被当作解析源，编号就会自我认证（首次固化实测踩中：
    /// 锚点清单恒为空）。哨兵取基线文件里的专属标记——它只可能出现在基线里，
    /// 因此台账清空到零条目时本断言依然有效。
    /// </remarks>
    private static void AssertBaselineIsNotPartOfResolutionSource()
        => GovernedDocs.Value.Should().NotContain(
            CommentBaselineJson.ResolutionExclusionMarker,
            "解析源必须排除冻结基线自身——否则台账里的记号会让编号自我认证（守卫变假绿）");

    private static CommentBaseline LoadBaseline(CommentConventionAnalysis analysis)
    {
        var path = BaselinePath();
        var previous = File.Exists(path) ? CommentBaselineJson.Read(File.ReadAllText(path)) : null;

        if (IsUpdateRequested())
        {
            // 重新固化**只能下压**：树上仍有真实违规时拒绝写入，否则「再固化一次」就是放行通道
            // （违规会被原样记入台账，从此永久合规）。
            if (previous is not null)
            {
                var blocking = analysis.UnresolvedAnchors.Except(previous.Anchors)
                    .Concat(analysis.UnlinkedNarration.Except(previous.Narration))
                    .Concat(FindBudgetViolations(analysis, previous))
                    .ToList();

                blocking.Should().BeEmpty(
                    "重新固化被拒绝：树上还有真实违规（悬空编号/未链接叙述/预算越界）。固化只允许下压——"
                    + "请先修违规，再固化 → " + string.Join(" | ", blocking));
            }

            File.WriteAllText(path, CommentBaselineJson.Write(analysis, ScopedProjects), new UTF8Encoding(false));
        }

        File.Exists(path).Should().BeTrue($"冻结基线必须受版本控制：{FeishuDocs.CommentBudgetBaseline}");
        return CommentBaselineJson.Read(File.ReadAllText(path));
    }

    private static List<string> FindBudgetViolations(CommentConventionAnalysis analysis, CommentBaseline baseline)
        => analysis.Files
            .SelectMany(entry => CommentConventionScanner.FindBudgetViolations(
                entry.Key, entry.Value, baseline.Files.TryGetValue(entry.Key, out var frozen) ? frozen : null))
            .ToList();

    private static bool IsUpdateRequested()
        => string.Equals(Environment.GetEnvironmentVariable(UpdateFlag), "true", StringComparison.OrdinalIgnoreCase);

    private static string BaselinePath() => Path.Combine(FindRepositoryRoot(), FeishuDocs.CommentBudgetBaseline);

    /// <summary>评审编号的解析源：受治理文档 + 变更日志（**不含**基线文件本身，否则编号会自我认证）。</summary>
    private static string ReadGovernedDocs()
    {
        var root = FindRepositoryRoot();
        var baselinePath = Path.GetFullPath(Path.Combine(root, FeishuDocs.CommentBudgetBaseline));
        var corpus = new StringBuilder();

        foreach (var pattern in new[] { "*.md", "*.json" })
        {
            foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "documents"), pattern, SearchOption.AllDirectories))
            {
                if (!string.Equals(Path.GetFullPath(path), baselinePath, StringComparison.OrdinalIgnoreCase))
                {
                    corpus.AppendLine(File.ReadAllText(path));
                }
            }
        }

        foreach (var name in new[] { "CHANGELOG.md", "README.md", "README_EN.md" })
        {
            var path = Path.Combine(root, name);
            if (File.Exists(path))
            {
                corpus.AppendLine(File.ReadAllText(path));
            }
        }

        return corpus.ToString();
    }

    private static bool ResolveAnchor(string token) => GovernedDocs.Value.Contains(token, StringComparison.Ordinal);

    private static bool LinkExists(string link)
    {
        var root = FindRepositoryRoot();
        if (link.StartsWith("documents/", StringComparison.Ordinal))
        {
            return File.Exists(Path.Combine(root, link.Replace('/', Path.DirectorySeparatorChar)));
        }

        return Regex.IsMatch(link, @"^ADR-\d+$", RegexOptions.CultureInvariant)
               && Directory
                   .EnumerateFiles(Path.Combine(root, "documents"), $"*{link}*.md", SearchOption.AllDirectories)
                   .Any();
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
