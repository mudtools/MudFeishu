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
    /// CI 脚本（<c>verify-build.ps1</c>）的 MUDFT 断言正则必须覆盖代码定义的全部零容忍 ID。
    /// </summary>
    /// <remarks>否则新增零容忍项只在代码里生效、脚本里漏掉，门禁只锁一半。</remarks>
    [Fact]
    public void VerifyBuildScript_ShouldCoverEveryZeroToleranceDiagnostic()
    {
        var zeroToleranceIds = ReadZeroToleranceIds();

        var scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "verify-build.ps1");
        File.Exists(scriptPath).Should().BeTrue();
        var script = File.ReadAllText(scriptPath);

        var covered = Regex.Match(script, @"MUDFT\(([0-9|]+)\)");
        covered.Success.Should().BeTrue("verify-build.ps1 应含 MUDFT 零容忍断言正则");

        var coveredIds = covered.Groups[1].Value
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(static suffix => "MUDFT" + suffix)
            .ToArray();

        foreach (var id in zeroToleranceIds)
        {
            coveredIds.Should().Contain(id,
                $"零容忍诊断 {id} 未进入 verify-build.ps1 的断言正则（门禁覆盖不完整）");
        }
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
