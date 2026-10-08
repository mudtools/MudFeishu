// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// 工具面诊断<b>门禁链路</b>守卫：判定口径的单一真相源、CI 是否真的执行、基线机制是否活着。
/// </summary>
/// <remarks>
/// <para>
/// <b>R-1+2c 迁移后的职责边界（本类被裁剪过一次，裁剪即是本次迁移的产物）</b>：
/// </para>
/// <list type="bullet">
/// <item><b>上移到组件侧</b>（本仓不再守护）：诊断<b>定义集 == 上报点集</b>、零容忍项的
/// 真实上报点与 severity、driver 负例覆盖 —— 本仓已无 <c>Mud.Feishu.AI.Tools/Diagnostics.cs</c>
/// 与 driver 负例工程，等价守卫在组件侧
/// <c>Tests/Mud.HttpUtils.Generator.Tests/ToolSurface/*</c>（见组件侧设计文档 §14.2）。
/// 原先依赖这些文件的 4 条用例已删除，而不是留在原地空转（"永远为绿的守卫"比没有守卫更糟）。</item>
/// <item><b>本仓保留</b>：门禁<b>链路</b>本身是否活着 —— 真相源被 dot-source、
/// CI 真的执行断言、基线文件与语义量口径到位、golden 快照存在。
/// 这三条都是"门禁的门禁"，与诊断定义在哪一侧无关（R5 根因 R-G 的同型失效永远有犯的可能）。</item>
/// <item><b>槽位口径</b>（零容忍集/级别/槽位号）由
/// <see cref="FeishuToolProfileContractGuards"/> 的「上游槽位镜像」承接。</item>
/// </list>
/// </remarks>
public class GeneratorDiagnosticsContractGuards
{
    /// <summary>
    /// <b>R5 / B-11</b>：MUDFT/SDKT 门禁必须"三方同源且在 CI 上真的生效"。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么守这条</b>：R5 评审发现本地 <c>verify-build.ps1</c> 死守 18 个零容忍 Error，
    /// 而 <b>CI workflow 里一条 MUDFT 断言都没有</b> —— 本地门禁在 CI 上完全无效，
    /// 而当时的守卫对此完全无感（它只看本地脚本）。这条把"CI 必须 dot-source 真相源
    /// 且真的执行断言"变成机械约束。
    /// </para>
    /// <para>
    /// 注：ID 清单与槽位口径已在 R-1+2c 上移到组件侧，本用例只断言<b>链路</b>；
    /// 清单一致性由 <see cref="FeishuToolProfileContractGuards.MudftGateSets_ShouldMatchTheUpstreamSlotMirror"/> 负责。
    /// </para>
    /// </remarks>
    [Fact]
    public void MudftGate_ShouldBeSourcedAndExecutedInCi()
    {
        var gatePath = Path.Combine(FindRepositoryRoot(), "scripts", "diagnostics-gate.ps1");
        File.Exists(gatePath).Should().BeTrue(
            "MUDFT 门禁的单一真相源 scripts/diagnostics-gate.ps1 缺失——本地门禁与 CI 将失去共同口径");
        var gate = File.ReadAllText(gatePath);
        gate.Should().Contain("$MudftZeroToleranceIds",
            "真相源必须声明零容忍集（清单内容由上游槽位镜像守卫比对）");

        // ② verify-build.ps1 不得手抄 ID 正则（单一真相源纪律）。
        var verifyScript = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "verify-build.ps1"));
        Regex.Matches(verifyScript, @"warning\|error\)\s*MUDFT\(").Count.Should().Be(
            0,
            "verify-build.ps1 出现手抄的 MUDFT ID 正则——应改为 Measure-Mudft + $MudftZeroToleranceIds，"
            + "否则本地门禁会与真相源再次漂移");

        // ③ CI 必须 dot-source 真相源。
        var workflowPath = CiWorkflowPath();
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
        var workflow = File.ReadAllText(CiWorkflowPath());

        workflow.Should().Contain(
            "Measure-Mudft -LogPath $log -Ids $MudftAlwaysZeroIds",
            "CI 未断言 MUDFT Warning 恒 0 集（MUDFT005/006/018：描述质量与能力目录退化将无人拦截）");

        workflow.Should().Contain(
            "Get-MudftBaseline",
            "CI 未接入 MUDFT 基线机制 ⇒ 截断率类 Warning（MUDFT009/021）无法拦增量");
    }

    /// <summary>基线文件必须存在，且其中的 ID 都在 <c>$MudftBaselineIds</c> 中声明（否则该行永不生效）。</summary>
    [Fact]
    public void MudftBaselineFile_ShouldExistAndOnlyReferenceDeclaredIds()
    {
        var root = FindRepositoryRoot();
        var baselinePath = Path.Combine(root, "scripts", "mudft-warning-baseline.txt");
        File.Exists(baselinePath).Should().BeTrue(
            "MUDFT Warning 基线缺失——-DenyToolWarnings 与 CI 会把基线当 0 判，任何截断增量都会被误报");

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
        var content = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), "scripts", "mudft-warning-baseline.txt"));

        Regex.IsMatch(content, @"(?m)^\s*009-tools\s*=\s*\d+\s*$").Should().BeTrue(
            "缺少 `009-tools=<N>` 行 ⇒ MUDFT009 只剩'日志出现次数'口径，"
            + "而该口径对聚合单条诊断恒等于编译次数，无法发现被截断的工具数增长（S-23）");

        // 两处断言（本地门禁 + CI）必须都接同一份语义量口径，否则"本地绿、CI 红"会再次出现。
        var verify = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "scripts", "verify-build.ps1"));
        verify.Should().Contain("Get-MudftSemanticBaseline", "本地门禁未接入 MUDFT009 语义量断言");
        verify.Should().Contain("Get-MudftOutputSchemaTruncatedToolCount", "本地门禁未真正提取语义量");

        var workflow = File.ReadAllText(CiWorkflowPath());
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

    /// <summary>从门禁真相源读取形如 <c>$X = @('001','002')</c> 的数组字面量（返回三位数字/前缀后缀序列）。</summary>
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

    private static string CiWorkflowPath()
        => Path.Combine(FindRepositoryRoot(), ".github", "workflows", "dotnet-publish.yml");

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
