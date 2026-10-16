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
/// Schema 常量」，<c>Readme.md</c> 的 L2 层写「<b>24</b> 个工具」——实测 <c>[AgentTool]</c> 与
/// <c>[ToolHandler]</c> 都是 <b>53</b>。这类漂移<b>没有任何编译器或现有门禁能发现</b>：
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
/// <b>D-2（2026-10-16）扩展：根 README 纳入扫描面</b>。原扫描面只有三份（工具面 csproj / 工具面
/// Readme / AI Readme），因而根 <c>README.md</c> 宣称「213 个策展工具（29 域）」而实际是 242/31
/// ——<b>漂移无构建拦截</b>（BUG-2 / BUG-5）。本扩展做三件事：① 把根 <c>README.md</c> /
/// <c>README_EN.md</c> 纳入 <see cref="CountBearingFiles"/>；② 扫描器新增「域数」类与英文形态
/// （<c>N 个策展工具</c> / <c>N curated tools</c> / <c>N 个域</c> / <c>N domains</c>）；
/// ③ <see cref="RootReadme_ShouldDeclareBothToolAndDomainCounts"/> 断言根 README <b>必须</b>显式声明
/// 两类规模——摘掉计数字段即报红（防「扫描面形同虚设」的静默假绿）。
/// </para>
/// <para>
/// <b>为什么必须把根 README 纳入</b>：它是<b>对外门面</b>（NuGet 首页 / 搜索榜首屏），漂移的代价
/// 比工具面 Readme 更高；而 `.docs/AI/状态基线.md` 位于 gitignored 目录、结构上无法享受构建期拦截，
/// 只能靠 <c>AGENTS.md</c> 的交付清单 + 该文件头部登记的刷新命令维持。
/// </para>
/// <para>
/// <b>自证报红</b>：<see cref="Scanner_ShouldReportDocumentedCount_ThatMismatchesTruth"/> 用合成文本
/// 驱动同一扫描器并断言报红；<see cref="Scanner_ShouldFindAllSurfaceCountDeclarations"/> 反向断言
/// 「无计数字段的文本扫不出任何声明」（即摘掉字段必然让上层守卫报红）。
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

        // D-2（2026-10-16）：根 README 是对外门面，原先在扫描面之外 ⇒ 宣称「213 个策展工具（29 域）」
        // 而实际 242/31（BUG-2 / BUG-5）。纳入后「改工具面忘改 README」即构建期报红。
        "README.md",
        "README_EN.md",

        // BUG-6：状态真相源文档曾纳入扫描面——它是"当前规模"的唯一声明处，
        // 若它自己写错数字，整个系列的漂移治理就落空。⚠️ 原「状态基线」文档位于 `.docs/`（gitignored），
        // 结构上无法享受构建期拦截，扫描面同步移除：扫描一个不存在的文件会让守卫报
        // "必须存在"（假红），而"扫描面静默为空"才是要防的假绿；它的维护改由
        // `AGENTS.md` 交付清单 + 该文件头部的刷新命令承担。
        // 教训（与 R2/R-9 两处扫描面盲区同源）：守卫的扫描面必须随代码/文档迁移同步复核。
    ];

    /// <summary>必须显式声明工具条目数与域数的对外门面（摘掉任一类计数即报红）。</summary>
    private static readonly string[] RootReadmes =
    [
        "README.md",
        "README_EN.md",
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

    /// <summary>
    /// 文档与工程文件中的「&lt;N&gt; 个（策展）工具」「&lt;N&gt; 域」「&lt;N&gt; curated tools」
    /// 「&lt;N&gt; domains」不得与真相源不一致。
    /// </summary>
    [Fact]
    public void DocumentedSurfaceCounts_ShouldMatchTheTruthSource()
    {
        var truth = ReadTruth();
        var violations = new List<string>();
        var declarations = 0;

        foreach (var relativePath in CountBearingFiles)
        {
            var path = Path.Combine(FindRepositoryRoot(), relativePath);
            File.Exists(path).Should().BeTrue($"{relativePath} 必须存在（扫描面不得静默为空）");

            var text = File.ReadAllText(path);
            var found = ToolScaleScanner.FindSurfaceCountDeclarations(text, truth);
            declarations += found.Count;

            violations.AddRange(
                found.Where(static d => !d.IsConsistent)
                    .Select(d => $"{relativePath}:{d.Line} 写的是 {d.DeclaredCount}（{d.Kind}），"
                        + $"真相源是 {truth.For(d.Kind)}（{d.Snippet}）"));
        }

        declarations.Should().BeGreaterThan(0,
            "扫描必须至少命中一处规模声明——否则本守卫在扫描面为空时也会绿（假门禁）");
        violations.Should().BeEmpty(
            "以下位置的工具面规模数字与 golden 快照 / 契约表不一致（宿主会据此误判工具面规模）："
            + string.Join(" | ", violations));
    }

    /// <summary>
    /// 根 README（对外门面）<b>必须</b>显式声明工具条目数与域数——任一缺失即报红。
    /// </summary>
    /// <remarks>
    /// 「摘掉根 README 的计数字段 ⇒ 报错」是 D-2 的第三条验收：只有一致性断言时，
    /// <b>删掉</b>计数字段（而不是写错）会让扫描结果为空、守卫静默变绿——这正是"扫描面形同虚设"。
    /// 本用例把"必须声明"也变成断言，与 <see cref="Scanner_ShouldFindAllSurfaceCountDeclarations"/>
    /// 的"无字段 ⇒ 零声明"互为正反证。
    /// </remarks>
    [Fact]
    public void RootReadme_ShouldDeclareBothToolAndDomainCounts()
    {
        var truth = ReadTruth();
        var silent = new List<string>();

        foreach (var relativePath in RootReadmes)
        {
            var text = File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
            var found = ToolScaleScanner.FindSurfaceCountDeclarations(text, truth);

            foreach (var kind in (SurfaceCountKind[])[SurfaceCountKind.ToolEntry, SurfaceCountKind.Domain])
            {
                if (!found.Any(d => d.Kind == kind))
                {
                    silent.Add($"{relativePath} 未声明 {kind}");
                }
            }
        }

        silent.Should().BeEmpty(
            "根 README 必须显式声明工具条目数与域数——摘掉计数字段即报红（防「扫描面形同虚设」）：{0}",
            string.Join(" | ", silent));
    }

    /// <summary>检查器自证：对"写错的裸数字"必须报红。</summary>
    [Fact]
    public void Scanner_ShouldReportDocumentedCount_ThatMismatchesTruth()
    {
        // 缺陷原始形态：csproj 的「24 个工具接口」注释（真相源是 53）。
        var found = ToolScaleScanner.FindSurfaceCountDeclarations(
            "<!-- 本包产出 24 个工具接口的 Schema 常量 -->", new ToolSurfaceTruth(ToolEntries: 53, Domains: 12));

        found.Should().HaveCount(1, "检查器必须能定位规模声明（找不到说明正则坏了——那是假绿）");
        found[0].DeclaredCount.Should().Be(24);
        found[0].Kind.Should().Be(SurfaceCountKind.ToolEntry);
        found[0].IsConsistent.Should().BeFalse("24 与真相源 53 不一致时 IsConsistent 必须为 false");
    }

    /// <summary>检查器自证：数字写对时不得报红。</summary>
    [Fact]
    public void Scanner_ShouldAcceptDocumentedCount_ThatMatchesTruth()
    {
        var truth = ReadTruth();
        var found = ToolScaleScanner.FindSurfaceCountDeclarations(
            $"| L2 | 标注了 `[AgentTool]` 的 {truth.ToolEntries} 个工具 |", truth);

        found.Should().HaveCount(1);
        found[0].IsConsistent.Should().BeTrue("与真相源一致的数字不得报红（否则守卫会误伤正确文档）");
    }

    /// <summary>
    /// 检查器自证：<b>没有计数字段的文本扫不出任何声明</b>——这正是"摘掉字段 ⇒ 上层报红"的机制。
    /// </summary>
    /// <param name="text">合成文本。</param>
    /// <param name="expected">期望命中数（0 = 未声明规模）。</param>
    [Theory]
    [InlineData("暴露 242 个策展工具，覆盖 31 个业务域。", 2)]
    [InlineData("Ships 242 curated tools across 31 domains.", 2)]
    [InlineData("| 现状 | 242 个：142 只读 + 100 写类 |", 1)]
    [InlineData("本章不再重复规模数字——见编译期契约表。", 0)]
    public void Scanner_ShouldFindAllSurfaceCountDeclarations(string text, int expected)
        => ToolScaleScanner
            .FindSurfaceCountDeclarations(text, new ToolSurfaceTruth(ToolEntries: 242, Domains: 31))
            .Should().HaveCount(expected,
                "「摘掉计数字段 ⇒ 扫不到 ⇒ 上层守卫报红」是 D-2 的验收形态；命中数不符说明正则坏了");

    /// <summary>
    /// 检查器自证：中英文两族计数各按其真相源判定（工具条目 / 域数各自独立）。
    /// </summary>
    /// <param name="text">合成文本。</param>
    /// <param name="kind">期望的分类。</param>
    /// <param name="declared">声明的数字。</param>
    /// <param name="consistent">是否与对应真相源一致。</param>
    [Theory]
    [InlineData("242 个策展工具", SurfaceCountKind.ToolEntry, 242, true)]
    [InlineData("999 个策展工具", SurfaceCountKind.ToolEntry, 999, false)]
    [InlineData("242 curated tools", SurfaceCountKind.ToolEntry, 242, true)]
    [InlineData("31 个业务域", SurfaceCountKind.Domain, 31, true)]
    [InlineData("99 域", SurfaceCountKind.Domain, 99, false)]
    [InlineData("31 domains", SurfaceCountKind.Domain, 31, true)]
    public void Scanner_ShouldJudgeConsistency_AgainstTheTruth(
        string text, SurfaceCountKind kind, int declared, bool consistent)
    {
        var found = ToolScaleScanner.FindSurfaceCountDeclarations(
            text, new ToolSurfaceTruth(ToolEntries: 242, Domains: 31));

        found.Should().HaveCount(1);
        found[0].Kind.Should().Be(kind);
        found[0].DeclaredCount.Should().Be(declared);
        found[0].IsConsistent.Should().Be(consistent);
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

    /// <summary>两类规模数字的真相源（工具条目 / 域数），按类分别判定。</summary>
    private static ToolSurfaceTruth ReadTruth()
        => new(ReadGoldenEntryCount(), ReadDomainCount());

    /// <summary>域数 = 工具名前缀去重（生成器同 pass 的名字契约表，不存在第二个清单）。</summary>
    private static int ReadDomainCount()
        => FeishuToolNames.All
            .Select(static name => name.Substring(0, name.IndexOf(".", StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .Count();

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

/// <summary>工具面规模数字的<b>分类</b>——不同类的真相源不同（工具条目 != 域数），必须分开判定。</summary>
public enum SurfaceCountKind
{
    /// <summary>工具条目数（文档里的「N 个（策展）工具」/「N curated tools」）。</summary>
    ToolEntry,

    /// <summary>业务域数（文档里的「N 个业务域」/「N 域」/「N domains」）。</summary>
    Domain,
}

/// <summary>两类规模数字的真相源（golden 条目数 + 域数）。</summary>
/// <param name="ToolEntries">工具条目数（与 golden 快照同源）。</param>
/// <param name="Domains">域数（工具名前缀去重）。</param>
public readonly record struct ToolSurfaceTruth(int ToolEntries, int Domains)
{
    /// <summary>取该类数字的真相源值。</summary>
    /// <param name="kind">规模数字分类。</param>
    /// <returns>该类的真相源值。</returns>
    internal int For(SurfaceCountKind kind) => kind switch
    {
        SurfaceCountKind.ToolEntry => ToolEntries,
        SurfaceCountKind.Domain => Domains,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "未登记的规模数字分类"),
    };
}

/// <summary>
/// "工具面规模数字"扫描器（可被合成文本驱动 ⇒ 支持检查器自证）。
/// </summary>
/// <remarks>
/// <para>
/// 只识别本仓库文档的<b>实际写法</b>，不做模糊匹配——判据窄才能保证不误报。已登记的写法：
/// 中文 <c>N 个工具</c> / <c>N 个策展工具</c> / <c>N 策展工具</c> / <c>N 个：…</c> / <c>N 个（…</c>
/// 与 <c>N 域</c> / <c>N 个域</c> / <c>N 个业务域</c>；英文 <c>N curated tools</c> / <c>N domains</c>。
/// </para>
/// <para>
/// <b>刻意不登记</b>的写法（会把"子集计数"误当"总量"）：<c>N 个写工具</c> / <c>N 个只读</c> /
/// <c>N 写类</c>——例如工具面 Readme 的「28 个写工具中 10 个暴露 <c>idempotency_key</c>」是某个能力
/// 子集的计数，与全量写工具数（100）不是一回事。新增写法时同批扩展本扫描器与自证用例。
/// </para>
/// </remarks>
internal static class ToolScaleScanner
{
    /// <summary>规模声明的识别模式（分类 + 正则；同一行可命中多类）。</summary>
    private static readonly (SurfaceCountKind Kind, Regex Pattern)[] Patterns =
    [
        // 中文工具条目：242 个工具 / 242 个策展工具 / 242 策展工具
        (SurfaceCountKind.ToolEntry, new Regex(@"(\d+)\s*(?:个\s*)?(?:策展\s*)?工具")),

        // 中文工具条目（现状表头形态）：242 个：142 只读 + 100 写类
        (SurfaceCountKind.ToolEntry, new Regex(@"(\d+)\s*个\s*[：:（(]")),

        // 英文工具条目：242 curated tools
        (SurfaceCountKind.ToolEntry, new Regex(@"(\d+)\s+curated\s+tools?", RegexOptions.IgnoreCase)),

        // 中文域数：31 域 / 31 个域 / 31 个业务域
        (SurfaceCountKind.Domain, new Regex(@"(\d+)\s*(?:个)?\s*(?:业务)?域")),

        // 英文域数：31 domains
        (SurfaceCountKind.Domain, new Regex(@"(\d+)\s+domains?", RegexOptions.IgnoreCase)),
    ];

    /// <summary>一处规模声明。</summary>
    /// <param name="Line">行号（1 起）。</param>
    /// <param name="Kind">规模数字分类。</param>
    /// <param name="DeclaredCount">声明中的数字。</param>
    /// <param name="IsConsistent">是否与该类真相源一致。</param>
    /// <param name="Snippet">该行原文（定位用）。</param>
    internal sealed record SurfaceCountDeclaration(
        int Line, SurfaceCountKind Kind, int DeclaredCount, bool IsConsistent, string Snippet);

    /// <summary>扫描文本中的规模声明，并按给定真相源逐类判定一致性。</summary>
    /// <param name="text">文件/片段全文。</param>
    /// <param name="truth">两类数字的真相源（工具条目 / 域数）。</param>
    /// <returns>声明清单（含分类与一致性判定，按行号、分类序）。</returns>
    /// <remarks>
    /// 真相源作为<b>参数</b>传入而不是静态状态：xUnit 默认让不同测试类并行，
    /// 任何"测试期间可变"的共享静态量都会变成不可复现的偶发红。
    /// </remarks>
    public static IReadOnlyList<SurfaceCountDeclaration> FindSurfaceCountDeclarations(
        string text, ToolSurfaceTruth truth)
    {
        var declarations = new List<SurfaceCountDeclaration>();
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            foreach (var (kind, pattern) in Patterns)
            {
                // 显式限定类型：本工程全局 using 了 Moq，其 `Moq.Match` 与正则的 `Match` 同名。
                foreach (System.Text.RegularExpressions.Match match in pattern.Matches(lines[i]))
                {
                    if (!int.TryParse(match.Groups[1].Value, out var count))
                    {
                        continue;
                    }

                    declarations.Add(new SurfaceCountDeclaration(
                        Line: i + 1,
                        Kind: kind,
                        DeclaredCount: count,
                        IsConsistent: count == truth.For(kind),
                        Snippet: lines[i].Trim()));
                }
            }
        }

        return declarations;
    }
}
