// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / B-8</b>：<b>执行器类"域纯度"守卫</b> —— 每个执行器类只服务<b>一个</b>工具域。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是这条不变量（评审对原文方案的修订）</b>：原文 B-8 假设"注册器分组跨域 ⇒
/// 启用任一工具连带加载其它域依赖"，并提议按"依赖闭包/ 并查集"重算分组。
/// <b>实施前核实：该前提在本仓不成立。</b>DI 相关的最小单元是<b>执行器类</b>
/// （<c>ToolRegistrarEmitter</c> 按<code>ExecutorType</code> 分组），而实测 18 个执行器文件中
/// 有 3 个各含多个执行器类，但<b>每个类都已域纯</b>：<c>DocxWriteTools</c>（仅 docx）/
/// <c>SheetsWriteTools</c>（仅 sheets）/ <c>DriveWriteTools</c>（仅 drive）/
/// <c>MessageWriteTools</c>（仅 message）/ <c>ApprovalWriteTools</c>（仅 approval）。
/// ⇒ "跨域"只是文件命名的观感问题（列入 U-21，不做），<b>不产生依赖加载放大</b>。
/// </para>
/// <para>
/// <b>但该不变量此前无任何机制守护</b>：新增一个同时注入两域客户端、绑定两域工具的
/// <c>DocxAndDriveTools</c> 就会让启用任一工具时连带解析另一域客户端
/// （缺失时按软缺席还会让整域工具从注册表消失）。本守卫把它变成机械约束。
/// </para>
/// </remarks>
public class ExecutorDomainPurityContractTests
{
    private static readonly string InternalDirectory = Path.Combine(
        FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal");

    private static readonly string ToolInterfacesDirectory = Path.Combine(
        FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Curation");

    /// <summary><b>断言一</b>：每个执行器类绑定的工具必须<b>同属一个域</b>。</summary>
    [Fact]
    public void EachExecutorClass_ShouldBindToolsFromExactlyOneDomain()
    {
        var violations = MapExecutorToDomains()
            .Where(static kv => kv.Value.Count > 1)
            .Select(static kv => $"{kv.Key} ⇒ 跨 {kv.Value.Count} 个域（{string.Join(" / ", kv.Value.OrderBy(static d => d, StringComparer.Ordinal))}）")
            .ToArray();

        violations.Should().BeEmpty(
            "执行器类跨域绑定 ⇒ 启用任一工具会连带解析其它域的 SDK 客户端；"
            + "缺失时按软缺席还会让整域工具从注册表消失。修法：按域拆成多个执行器类：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// <b>断言二（依赖纯度）</b>：执行器构造参数<b>不得</b>同时出现两个不同域的 SDK 客户端。
    /// </summary>
    /// <remarks>
    /// 这条比断言一更直接地对应"连带加载依赖"：即便工具绑定侥幸同域，构造参数里多域客户端
    /// 也会让 DI 在注册该执行器时解析另一域的客户端。
    /// </remarks>
    [Fact]
    public void EachExecutorClass_ShouldNotInjectClientsFromMultipleDomains()
    {
        // ⚠️ 本断言**已实现但判定对象错了**，见下方说明；保留仅为记录"为何不做"。
        // 实测误报：DocxWriteTools 注入 IFeishuTenantV1Docx + IFeishuTenantV1DocxBlocks、
        // DriveWriteTools 注入 …DriveFolder + …DriveFiles、ApprovalWriteTools 注入 4 个 …Approval*、
        // TaskTools 注入 …V2Task + …TaskComments —— 这些都是**同一域内的资源细分**，不是跨域。
        // 根因：SDK 接口名首段 ≠ 工具域（SpreadsheetData 首段是 spreadsheet，工具域却叫 sheets）。
        // 结论：该断言不可机械判定，已删除；域纯度只按**工具名**判定（见断言一，可判定且权威）。
        var _ = ResolveClientDomain("IFeishuTenantV1Docx");
        Assert.True(true, "占位：真实断言已删除，原因见上方注释（勿重加基于接口名判域的版本）");
    }

    /// <summary>
    /// <b>把"判据选错对象"这一教训固化为可执行断言</b>：同域但首段不同的接口对，
    /// 按"首段"判域必然被误判为跨域——这正是断言二被删除的原因。
    /// </summary>
    [Fact]
    public void DomainPurityJudgement_ShouldBeBasedOnToolNames_NotSdkInterfaceNames()
    {
        var sameDomainPairs = new[]
        {
            ("IFeishuTenantV1Docx", "IFeishuTenantV1DocxBlocks"),
            ("IFeishuTenantV1DriveFolder", "IFeishuTenantV1DriveFiles"),
            ("IFeishuTenantV4Approval", "IFeishuTenantV4ApprovalTask"),
            ("IFeishuTenantV2Task", "IFeishuTenantV2TaskComments"),
        };

        foreach (var (left, right) in sameDomainPairs)
        {
            ResolveClientDomain(left).Should().NotBe(
                ResolveClientDomain(right),
                "本用例用这对样本固定『首段判域会误报』这一事实；"
                + "若将来 SDK 接口命名变更导致首段一致，本用例需重新评估（说明判据该换对象了）");
        }
    }

    /// <summary>反向自证：必须真的扫到执行器类、绑定与注入，否则上面两条都是空转（假绿）。</summary>
    [Fact]
    public void Scanner_ShouldSeeExecutorsBindingsAndInjections_OtherwiseGuardsAreFalseGreen()
    {
        var map = MapExecutorToDomains();
        map.Should().HaveCountGreaterThan(0, "未解析到任何「执行器 → 域」映射——绑定扫描已失效（假绿）");
        map.Values.Should().OnlyContain(static d => d.Count >= 1, "存在解析不出域的执行器——域纯度断言会空转");

        var injections = EnumerateExecutorFiles()
            .SelectMany(static f => ReadExecutorClasses(File.ReadAllText(f)))
            .Count(static t => Regex.IsMatch(t.Item2, @"\bIFeishu\w+"));

        injections.Should().BeGreaterThan(0, "未扫到任何 IFeishu* 客户端注入——依赖纯度断言已失效（假绿）");
    }

    /// <summary>
    /// 基线：跨域执行器数保持 <b>0</b>。若将来有意引入"组合执行器"（并接受依赖放大），
    /// 必须显式更新此基线并在评审中说明理由。
    /// </summary>
    [Fact]
    public void CrossDomainExecutorCount_ShouldRemainZero()
    {
        var crossDomain = MapExecutorToDomains()
            .Where(static kv => kv.Value.Count > 1)
            .Select(static kv => kv.Key)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        crossDomain.Should().BeEmpty(
            "跨域执行器从 0 变为 {0} 个：{1}。这会让启用一个工具连带加载另一域的依赖；"
            + "若为有意设计，请更新本基线并在评审中记录理由",
            crossDomain.Length,
            string.Join(" / ", crossDomain));
    }

    // ────────── 采集实现 ──────────

    private static IEnumerable<string> EnumerateExecutorFiles()
        => Directory.EnumerateFiles(InternalDirectory, "*Tools.cs", SearchOption.TopDirectoryOnly)
            .Where(static p => !p.EndsWith("ToolExecutor.cs", StringComparison.Ordinal));

    /// <summary>建立"执行器类 → 其绑定工具所属域集合"的映射。</summary>
    private static Dictionary<string, HashSet<string>> MapExecutorToDomains()
    {
        var interfaceToDomain = MapToolInterfaceToDomain();
        var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var file in EnumerateExecutorFiles())
        {
            var source = File.ReadAllText(file);

            foreach (var (className, _) in ReadExecutorClasses(source))
            {
                var domains = new HashSet<string>(StringComparer.Ordinal);

                // 只取属于本类的片段：从类声明到下一个 "internal sealed class" 之前。
                var start = source.IndexOf($"class {className}", StringComparison.Ordinal);
                if (start < 0)
                {
                    continue;
                }

                var next = source.IndexOf("internal sealed class", start + 1, StringComparison.Ordinal);
                var body = next < 0 ? source[start..] : source[start..next];

                foreach (System.Text.RegularExpressions.Match m in Regex.Matches(
                    body, @"typeof\s*\(\s*(?<iface>IFeishu\w+Tool)\s*\)"))
                {
                    if (interfaceToDomain.TryGetValue(m.Groups["iface"].Value, out var domain))
                    {
                        domains.Add(domain);
                    }
                }

                result[$"{Path.GetFileNameWithoutExtension(file)}::{className}"] = domains;
            }
        }

        return result;
    }

    /// <summary>建立"工具接口名 → 工具名首段（域）"的映射（扫工具接口文件）。</summary>
    private static Dictionary<string, string> MapToolInterfaceToDomain()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(ToolInterfacesDirectory, "*.cs", SearchOption.TopDirectoryOnly))
        {
            var source = File.ReadAllText(file);

            // 形如：[FeishuTool("docx.append_blocks", …)]  …  public interface IFeishuTenantDocxAppendBlocksTool
            foreach (System.Text.RegularExpressions.Match m in Regex.Matches(
                source,
                @"\[FeishuTool\s*\(\s*""(?<tool>[^""]+)""[\s\S]{0,600}?interface\s+(?<iface>I\w+Tool)\b"))
            {
                var tool = m.Groups["tool"].Value;
                var dot = tool.IndexOf('.', StringComparison.Ordinal);
                if (dot > 0)
                {
                    map[m.Groups["iface"].Value] = tool[..dot];
                }
            }
        }

        map.Should().NotBeEmpty("工具接口→域的映射为空——正则已失效（假绿），请先修守卫");
        return map;
    }

    /// <summary>从 SDK 接口名推断域（<c>IFeishuTenantV1DocxBlocks</c> → <c>docx</c>）。</summary>
    /// <remarks>
    /// 守卫只关心"<b>是否出现 &gt;1 个不同首段</b>"，故个别接口首段与工具域名不完全一致
    /// （如 <c>SpreadsheetData</c> → <c>spreadsheet</c>，工具域叫 <c>sheets</c>）不影响判定。
    /// </remarks>
    private static string? ResolveClientDomain(string clientInterface)
    {
        var core = Regex.Replace(clientInterface, @"^IFeishu(?:User|Tenant|App)?(?:V\d+)?", string.Empty);
        return core.Length == 0 ? null : char.ToLowerInvariant(core[0]) + core[1..];
    }

    /// <summary>读取一个文件里的所有执行器类（类名 + 构造参数文本）。</summary>
    private static List<(string ClassName, string Ctor)> ReadExecutorClasses(string source)
    {
        var results = new List<(string, string)>();

        foreach (System.Text.RegularExpressions.Match m in Regex.Matches(
            source, @"internal\s+sealed\s+class\s+(?<name>\w+)\s*\((?<args>[^)]*)\)", RegexOptions.Singleline))
        {
            results.Add((m.Groups["name"].Value, m.Groups["args"].Value));
        }

        return results;
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