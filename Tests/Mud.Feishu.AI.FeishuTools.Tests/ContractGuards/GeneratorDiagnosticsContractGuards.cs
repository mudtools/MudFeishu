// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// 源生成器诊断的<b>元守卫</b>：把"零容忍诊断定义"与"真实上报点"钉成机械约束。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要元守卫</b>：本仓库出现过一类典型隐性缺陷——<c>Diagnostics.cs</c> 定义了
/// 零容忍集（<c>ZeroToleranceIds</c>，构建期阻断），但集内多个 ID <b>没有任何上报点</b>，
/// 于是 <c>verify-build.ps1</c> 的"MUDFT 零容忍 == 0"断言<b>恒为绿</b>：门禁看起来存在，
/// 实际什么都没锁。诊断一旦无人上报，就不是门禁，是注释。
/// </para>
/// <para>
/// 本守卫断言三件事：① 每个零容忍 ID 在生成器源码中有 <c>Diagnostics.&lt;ID&gt;</c> 形态的上报点；
/// ② 上报点不在 <c>Diagnostics.cs</c> 自身（自引用不算上报）；③ CI 脚本的正则覆盖集与代码定义集一致
/// （防脚本与代码各自漂移，那会让门禁再次静默失效）。
/// </para>
/// <para>
/// 注：<c>Mud.Feishu.AI.Tools</c> 以分析器形态引用（<c>ReferenceOutputAssembly=false</c>），
/// 测试无法以符号方式访问其 <c>internal</c> 类型，故采用源码扫描——这也是本仓库既有的守卫体例
/// （见 <c>FeishuToolContractGuards.ToolMetrics_ShouldNotUseHighCardinalityTags_SourceScan</c>）。
/// </para>
/// </remarks>
public class GeneratorDiagnosticsContractGuards
{
    private const string DiagnosticsFileName = "Diagnostics.cs";
    private const string ToolProjectDirectory = "Mud.Feishu.AI.Tools";
    private const string TestProjectDirectory = "Tests/Mud.Feishu.AI.FeishuTools.Tests/ContractGuards";

    /// <summary>零容忍集内每个 ID 必须有真实上报点（防"定义即死代码"复发）。</summary>
    [Fact]
    public void ZeroToleranceDiagnostics_ShouldHaveReportSites()
    {
        var zeroToleranceIds = ReadZeroToleranceIds();
        zeroToleranceIds.Should().NotBeEmpty("ZeroToleranceIds 必须可解析（解析失败说明 Diagnostics.cs 结构被改坏）");

        var sources = ReadGeneratorSources();
        foreach (var id in zeroToleranceIds)
        {
            var reportSites = sources.Count(source => Regex.IsMatch(source, $@"Diagnostics\.{id}\b"));
            reportSites.Should().BeGreaterThan(0,
                $"零容忍诊断 {id} 在生成器源码中没有任何上报点——它会让 verify-build 的断言恒为绿（假门禁）");
        }
    }

    /// <summary>
    /// 零容忍集内的诊断必须是 <c>Error</c> 级——否则"构建期阻断"名不副实（积压的 Warning 不会拦任何东西）。
    /// </summary>
    [Fact]
    public void ZeroToleranceDiagnostics_ShouldAllBeErrorSeverity()
    {
        var diagnosticsSource = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), ToolProjectDirectory, DiagnosticsFileName));

        foreach (var id in ReadZeroToleranceIds())
        {
            var declaration = Regex.Match(
                diagnosticsSource,
                $@"id:\s*""{id}""(?<body>.*?)isEnabledByDefault",
                RegexOptions.Singleline);

            declaration.Success.Should().BeTrue($"{id} 应在 Diagnostics.cs 中声明");
            declaration.Groups["body"].Value.Should().Contain("DiagnosticSeverity.Error",
                $"{id} 既列入 ZeroToleranceIds（构建期阻断），其 severity 必须是 Error");
        }
    }

    /// <summary>
    /// <b>定义集必须等于上报点集</b>（AT-B14 的反向锁：无单侧多余项）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 既有守卫只断言了"零容忍集内每个 ID 有上报点"，属<b>单侧</b>约束——它允许 <c>Diagnostics.cs</c> 里
    /// 长期躺着"没有任何人上报"的定义。R3 复核发现的 <c>MUDFT007/011/012/013</c> 正是这种情形：
    /// 它们引用的机制（<c>[FeishuScopes]</c> / <c>[FeishuToolRisk]</c> / AOT TypeInfoPropertyName 检测 /
    /// JsonPropertyName 裁剪追踪）在仓库中<b>根本不存在</b>，是"指向不存在机制的僵尸定义"。
    /// </para>
    /// <para>
    /// 死定义的危害不是"占位"，而是<b>让覆盖集看起来比实际更广</b>——读者会以为这些场景已被监控。
    /// 故本用例把"定义集 == 上报点集"变成机械约束：要么接线，要么删除，不允许"原地保留 + 只在文档说明"。
    /// </para>
    /// </remarks>
    [Fact]
    public void Diagnostics_ShouldNotDeclareUnreportedDiagnostics()
    {
        var declared = ReadDeclaredDiagnosticIds();
        var reported = ReadReportedDiagnosticIds();

        declared.Should().NotBeEmpty("Diagnostics.cs 的 id 声明必须可解析（解析失败说明结构被改坏）");

        var unreported = declared.Except(reported, StringComparer.Ordinal).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        unreported.Should().BeEmpty(
            "以下诊断有定义但没有任何上报点，属「指向不存在机制的僵尸定义」——"
            + "必须接线或直接删除定义（死定义会让覆盖集看起来比实际更广）："
            + string.Join(", ", unreported));
    }

    /// <summary>
    /// A2（WP1 / R4 方案）：零容忍诊断必须有<b>真实可触发的 driver 负例</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// R3 曾以"显式债务登记"替代（<c>PendingTriggerableIds</c>，债务 11 项）——R4 落地
    /// <c>Tests/Mud.Feishu.AI.Tools.Tests</c>（<c>CSharpGeneratorDriver</c> 驱动生产生成器）后
    /// <b>债务清零</b>，本用例改为交叉断言：负例工程存在并纳入解决方案、且 <c>ZeroToleranceIds</c>
    /// 的每一条都在负例工程中有登记的负例方法（方法名以诊断 ID 开头）。
    /// 新增零容忍项而不同批补负例 → 立即红。
    /// </para>
    /// <para>
    /// 本工程无法以符号方式引用生成器程序集（Analyzer 形态 + <c>PrivateAssets=all</c>），
    /// 故对负例工程的核查采用源码扫描——与仓库既有守卫体例一致；负例本体（driver 运行 + 断言）
    /// 由 <c>Mud.Feishu.AI.Tools.Tests/GeneratorNegativeCaseTests</c> 真实执行，
    /// 其元守卫（<c>ZeroToleranceIds_ShouldEachHaveDriverNegativeCase</c>）反向锁定
    /// <c>ZeroToleranceIds</c> 与负例集的双向一致。
    /// </para>
    /// </remarks>
    [Fact]
    public void ZeroToleranceDiagnostics_ShouldEachHaveATriggerableCase()
    {
        var zeroToleranceIds = ReadZeroToleranceIds();

        // ① 负例工程必须存在并已进解决方案（摘除该工程 = 摘除门禁，不允许）。
        var driverTestsPath = Path.Combine(FindRepositoryRoot(), "Tests", "Mud.Feishu.AI.Tools.Tests");
        Directory.Exists(driverTestsPath).Should().BeTrue(
            "生成器 driver 负例工程 Tests/Mud.Feishu.AI.Tools.Tests 必须存在（WP1 交付物，A2 债务清零的载体）");
        var slnx = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Mud.Feishu.slnx"));
        slnx.Should().Contain("Mud.Feishu.AI.Tools.Tests", "driver 负例工程必须纳入解决方案（质量闸门按 slnx 枚举测试）");

        // ② 每条零容忍 ID 必须有以该 ID 开头的负例方法（登记即义务）。
        var testSource = File.ReadAllText(Path.Combine(driverTestsPath, "GeneratorNegativeCaseTests.cs"));
        foreach (var id in zeroToleranceIds)
        {
            testSource.Should().Contain(
                $"void {id}_",
                $"零容忍诊断 {id} 在 driver 负例工程中没有登记的负例方法（方法名须以 {id}_ 开头）");
        }
    }

    /// <summary>读取 <c>Diagnostics.cs</c> 中<b>全部</b>诊断的 ID（含非零容忍项）。</summary>
    private static IReadOnlyList<string> ReadDeclaredDiagnosticIds()
    {
        var diagnosticsSource = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), ToolProjectDirectory, DiagnosticsFileName));

        return Regex.Matches(diagnosticsSource, @"id:\s*""(MUDFT[0-9]{3})""")
            .Select(static m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>读取生成器全部源码中<b>被上报</b>的 ID（<c>Diagnostics.&lt;ID&gt;</c> 形态）。</summary>
    private static IReadOnlyList<string> ReadReportedDiagnosticIds()
        => ReadGeneratorSources()
            .SelectMany(source => Regex.Matches(source, @"Diagnostics\.(MUDFT[0-9]{3})\b")
                .Select(static m => m.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    /// <summary>
    /// <b>R5 / B-11</b>：MUDFT 零容忍门禁必须<b>三方同源且在 CI 上真的生效</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么改写这条守卫（R5 新增根因 R-G）</b>：原实现从 <c>verify-build.ps1</c> 里
    /// 抓一段手抄正则 <c>MUDFT(...)</c>，只能保证"脚本 ⊇ 代码定义"。
    /// R5 评审发现更根本的问题：<b>CI workflow 里一条 MUDFT 都没有</b> ——
    /// 本地死守的 18 个零容忍 Error 在 CI 上完全无效，而原守卫对此<b>完全无感</b>
    /// （它只看本地脚本）。同时 ID 清单在脚本里手抄，与 <c>Diagnostics.ZeroToleranceIds</c> 无强约束。
    /// </para>
    /// <para>
    /// 现改为断言四件事：① <c>scripts/diagnostics-gate.ps1</c>（单一真相源）覆盖全部零容忍 ID；
    /// ② <c>verify-build.ps1</c> 不再手抄正则（改走真相源）；
    /// ③ CI workflow <b>dot-source 真相源</b>；④ CI workflow <b>真的执行</b>零容忍断言
    /// —— ③④ 缺任一，本用例即红（这是 R-G 的直接判据）。
    /// </para>
    /// </remarks>
    [Fact]
    public void MudftGate_ShouldCoverEveryZeroToleranceDiagnosticAndBeActiveInCi()
    {
        var zeroToleranceIds = ReadZeroToleranceIds();
        zeroToleranceIds.Should().NotBeEmpty("ZeroToleranceIds 必须可解析（解析失败说明 Diagnostics.cs 结构被改坏）");

        var gatePath = Path.Combine(FindRepositoryRoot(), "scripts", "diagnostics-gate.ps1");
        File.Exists(gatePath).Should().BeTrue(
            "MUDFT 门禁的单一真相源 scripts/diagnostics-gate.ps1 缺失——本地门禁与 CI 将失去共同口径");
        var gate = File.ReadAllText(gatePath);

        // ① 真相源覆盖全部零容忍 ID。
        var coveredIds = ReadGateScriptIdList(gate, "$MudftZeroToleranceIds")
            .Select(static suffix => "MUDFT" + suffix)
            .ToArray();
        coveredIds.Should().NotBeEmpty("scripts/diagnostics-gate.ps1 应声明 $MudftZeroToleranceIds");

        foreach (var id in zeroToleranceIds)
        {
            coveredIds.Should().Contain(id,
                $"零容忍诊断 {id} 未进入 scripts/diagnostics-gate.ps1 的 $MudftZeroToleranceIds（门禁覆盖不完整）");
        }

        // ② verify-build.ps1 不得再手抄 ID 正则（单一真相源纪律）。
        var verifyScriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "verify-build.ps1");
        var verifyScript = File.ReadAllText(verifyScriptPath);
        Regex.Matches(verifyScript, @"warning\|error\)\s*MUDFT\(").Count.Should().Be(
            0,
            "verify-build.ps1 出现手抄的 MUDFT ID 正则——应改为 Measure-Mudft + $MudftZeroToleranceIds，"
            + "否则本地门禁会与真相源再次漂移");

        // ③ CI 必须 dot-source 真相源。
        var workflowPath = Path.Combine(FindRepositoryRoot(), ".github", "workflows", "dotnet-publish.yml");
        File.Exists(workflowPath).Should().BeTrue("CI workflow 缺失——MUDFT 断言将无处执行（R5 根因 R-G）");
        var workflow = File.ReadAllText(workflowPath);

        workflow.Should().Contain(
            "diagnostics-gate.ps1",
            "CI 未 dot-source scripts/diagnostics-gate.ps1 ⇒ MUDFT 断言与本地门禁不同源（R5 根因 R-G）");

        // ④ CI 必须真的执行零容忍断言（只 dot-source 不断言 = 半吊子假绿）。
        workflow.Should().Contain(
            "Measure-Mudft -LogPath $log -Ids $MudftZeroToleranceIds",
            "CI 未执行 MUDFT 零容忍断言——本地死守的 Error 在 CI 上仍然无效（R5 根因 R-G）");
    }

    /// <summary>
    /// <b>R5 / B-11</b>：CI 必须同时断言 Warning 恒 0 集与基线集，
    /// 否则 5 条 Warning/Info 诊断在任何地方都无人守护。
    /// </summary>
    [Fact]
    public void Workflow_ShouldAssertMudftWarningsAndBaseline()
    {
        var workflowPath = Path.Combine(FindRepositoryRoot(), ".github", "workflows", "dotnet-publish.yml");
        var workflow = File.ReadAllText(workflowPath);

        workflow.Should().Contain(
            "Measure-Mudft -LogPath $log -Ids $MudftAlwaysZeroIds",
            "CI 未断言 MUDFT Warning 恒 0 集（MUDFT005/006/018：描述质量与能力目录退化将无人拦截）");

        workflow.Should().Contain(
            "Get-MudftBaseline",
            "CI 未接入 MUDFT 基线机制 ⇒ 截断率类 Warning（MUDFT009/021）无法拦增量");
    }

    /// <summary>
    /// 门禁的三个 ID 集合（零容忍 / 恒 0 Warning / 基线）<b>两两不相交</b>，
    /// 且并集覆盖 <c>Diagnostics.cs</c> 中全部已声明诊断——防止某个 ID 落在三不管地带。
    /// </summary>
    [Fact]
    public void MudftGateSets_ShouldBeDisjointAndCoverAllDeclaredDiagnostics()
    {
        var gatePath = Path.Combine(FindRepositoryRoot(), "scripts", "diagnostics-gate.ps1");
        File.Exists(gatePath).Should().BeTrue("scripts/diagnostics-gate.ps1 缺失（MUDFT 门禁真相源）");
        var gate = File.ReadAllText(gatePath);

        var zero = ReadGateScriptIdList(gate, "$MudftZeroToleranceIds").ToHashSet(StringComparer.Ordinal);
        var alwaysZero = ReadGateScriptIdList(gate, "$MudftAlwaysZeroIds").ToHashSet(StringComparer.Ordinal);
        var baseline = ReadGateScriptIdList(gate, "$MudftBaselineIds").ToHashSet(StringComparer.Ordinal);

        alwaysZero.Should().NotBeEmpty("恒 0 Warning 集不得为空（MUDFT005/006/018 需被守护）");
        baseline.Should().NotBeEmpty("基线集不得为空（MUDFT009/021 需按基线拦增量）");

        var overlap = zero.Intersect(alwaysZero)
            .Concat(zero.Intersect(baseline))
            .Concat(alwaysZero.Intersect(baseline))
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        overlap.Should().BeEmpty(
            "同一诊断 ID 同时出现在两个门禁集合中，判定口径会互相矛盾：{0}", string.Join(", ", overlap));

        var uncovered = ReadDeclaredDiagnosticIds()
            .Select(static id => id["MUDFT".Length..])
            .Except(zero)
            .Except(alwaysZero)
            .Except(baseline)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        uncovered.Should().BeEmpty(
            "以下已声明诊断未进入任何门禁集合（既非零容忍、也非恒 0、也不是基线）⇒ 严重级别无人守护：{0}",
            string.Join(", ", uncovered));
    }

    /// <summary>基线文件必须存在，且其中的 ID 都在 <c>$MudftBaselineIds</c> 中声明（否则该行永不生效）。</summary>
    [Fact]
    public void MudftBaselineFile_ShouldExistAndOnlyReferenceDeclaredIds()
    {
        var root = FindRepositoryRoot();
        var baselinePath = Path.Combine(root, "scripts", "mudft-warning-baseline.txt");
        File.Exists(baselinePath).Should().BeTrue(
            "MUDFT Warning 基线缺失——-DenyToolWarnings 与 CI 会把基线当0 判，任何截断增量都会被误报");

        var gate = File.ReadAllText(Path.Combine(root, "scripts", "diagnostics-gate.ps1"));
        var declared = ReadGateScriptIdList(gate, "$MudftBaselineIds").ToHashSet(StringComparer.Ordinal);

        var orphans = new List<string>();
        foreach (var line in File.ReadAllLines(baselinePath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            // 两种行格式（S-23）：
            //   <三位ID>=<count>          ← 日志出现次数口径（Measure-Mudft）
            //   <三位ID>-tools=<count>    ← 语义量口径（仅"聚合单条"诊断需要；
            //                               出现次数对它是"1 × 编译次数"，拦不住"变多"）
            var match = Regex.Match(trimmed, @"^(?<id>\d{3})(?<semantic>-tools)?\s*=\s*(?<count>\d+)$");
            match.Success.Should().BeTrue($"基线行格式非法（应形如 009=0 或 009-tools=30）：{trimmed}");

            if (!declared.Contains(match.Groups["id"].Value))
            {
                orphans.Add(match.Groups["id"].Value);
            }
        }

        orphans.Should().BeEmpty(
            "基线文件里的 ID 未在 $MudftBaselineIds 中声明 ⇒ 该行永远不会被检查：{0}", string.Join(", ", orphans));
    }

    /// <summary>
    /// <b>R5 / S-23</b>：<c>MUDFT009</c> 的<b>语义量</b>基线行必须存在。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 这条守卫守的是"守卫本身的有效性"：<c>MUDFT009</c> 是**聚合单条**诊断，真正的计数写在
    /// 消息文本里（"N 个工具的输出 Schema 被截断"），而 <c>Measure-Mudft</c> 数的是日志出现次数
    /// ⇒ 那个数恒等于"编译次数"（实测 8），**截断工具数 30 → 40 时它不变**。
    /// </para>
    /// <para>
    /// 也就是说：只保留 <c>009=8</c> 这一行，门禁对"截断变多"是**结构性失明**的。
    /// 删掉 <c>009-tools=</c> 会让它悄悄退回失明状态且无任何症状 —— 故在此机械锁死。
    /// </para>
    /// </remarks>
    [Fact]
    public void MudftBaselineFile_ShouldDeclareSemanticCountForAggregatedDiagnostic()
    {
        var baselinePath = Path.Combine(FindRepositoryRoot(), "scripts", "mudft-warning-baseline.txt");
        var content = File.ReadAllText(baselinePath);

        Regex.IsMatch(content, @"(?m)^\s*009-tools\s*=\s*\d+\s*$").Should().BeTrue(
            "缺少 `009-tools=<N>` 行 ⇒ MUDFT009 只剩'日志出现次数'口径，"
            + "而该口径对聚合单条诊断恒等于编译次数，无法发现被截断的工具数增长（S-23）");

        // 两处断言（本地门禁 + CI）必须都接同一份语义量口径，否则"本地绿、CI 红"会再次出现。
        var verify = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "verify-build.ps1"));
        verify.Should().Contain("Get-MudftSemanticBaseline", "本地门禁未接入 MUDFT009 语义量断言");
        verify.Should().Contain("Get-MudftOutputSchemaTruncatedToolCount", "本地门禁未真正提取语义量");

        var workflow = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), ".github", "workflows", "dotnet-publish.yml"));
        workflow.Should().Contain("Get-MudftSemanticBaseline", "CI 未接入 MUDFT009 语义量断言");
        workflow.Should().Contain("Get-MudftOutputSchemaTruncatedToolCount", "CI 未真正提取语义量");
    }

    /// <summary>
    /// 描述符 golden 快照必须存在——它是构建期 MUDFT014 的比对基准，缺失即门禁静默失效。
    /// </summary>
    [Fact]
    public void GoldenSnapshot_ShouldExistForBuildTimeDriftGate()
    {
        var goldenPath = Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.FeishuTools", "FeishuToolSchemas.golden.txt");

        File.Exists(goldenPath).Should().BeTrue(
            "golden 快照缺失会让构建期的 MUDFT014 比对静默失效（csproj 的 AdditionalFiles 声明带 Exists 条件）");
    }

    // ────────── 读取与定位 ──────────

    /// <summary>
    /// 从 <c>scripts/diagnostics-gate.ps1</c> 读取形如
    /// <c>$Xxx = @('001', '002', …)</c> 的数组字面量，返回<b>三位数字后缀</b>序列。
    /// </summary>
    private static IReadOnlyList<string> ReadGateScriptIdList(string gateScript, string variableName)
    {
        var block = Regex.Match(
            gateScript,
            $@"{Regex.Escape(variableName)}\s*=\s*@\((?<body>.*?)\)",
            RegexOptions.Singleline);

        return block.Success
            ? Regex.Matches(block.Groups["body"].Value, "'(?<id>[^']+)'")
                .Select(static m => m.Groups["id"].Value)
                .ToArray()
            : [];
    }

    private static IReadOnlyList<string> ReadZeroToleranceIds()
    {
        var diagnosticsSource = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), ToolProjectDirectory, DiagnosticsFileName));

        var block = Regex.Match(
            diagnosticsSource,
            @"ZeroToleranceIds\s*=\s*\[(?<body>[^\]]*)\]",
            RegexOptions.Singleline);

        return block.Success
            ? Regex.Matches(block.Groups["body"].Value, @"""(MUDFT[0-9]{3})""")
                .Select(static m => m.Groups[1].Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static id => id, StringComparer.Ordinal)
                .ToArray()
            : [];
    }

    /// <summary>读取生成器全部源码（排除 <c>Diagnostics.cs</c> 定义文件与其 obj/bin 产物）。</summary>
    private static IReadOnlyList<string> ReadGeneratorSources()
        => Directory
            .GetFiles(Path.Combine(FindRepositoryRoot(), ToolProjectDirectory), "*.cs", SearchOption.AllDirectories)
            .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.EndsWith(DiagnosticsFileName, StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .ToArray();

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
