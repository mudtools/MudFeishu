// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>R6 / S1</b>：guidance <b>资产存在性与引用完整性</b>守卫——
/// 补住「域集合 / L2 键集 / 高频域覆盖」这三个既有守卫没覆盖到的面。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它（R6/S1 的调研结论）</b>：<c>GuidanceAssetContractGuards</c> 已经锁了
/// 「域集合同源 / 单域预算 / 全域零丢弃 / 工具引用一致 / 反义短语」，
/// <c>GuidanceStructureContractTests</c> 锁了「小节结构」；但**没有一处**断言
/// 「每个已启用工具域都有 L1 资产」——实测 <c>minutes</c> 域（2 个工具）长期没有 L1 md，
/// 而全部守卫照样全绿。<b>声明存在 ≠ 资产存在</b>，只能靠本守卫。
/// </para>
/// <para>
/// <b>为什么用生成产物而不是手抄域清单</b>：域集合的唯一真相源是
/// <see cref="FeishuToolContracts"/>（编译期契约表）与
/// <see cref="FeishuToolGuidance"/>（编译期 guidance 资产表）。
/// 手抄清单会与工具面漂移，且新增域时忘记更新——正是本守卫要消灭的失败形态。
/// </para>
/// <para>
/// <b>为什么 L2 键要用"双向相等"而不是"包含"</b>：md 文件与生成产物是两个方向都可能漂移的：
/// md 多了（写了却没人能读到）与 md 少了（模型按 guidance_read 提示去读却 404）都是缺陷。
/// 双向相等让两个方向都报红。
/// </para>
/// </remarks>
public class GuidanceContentContractGuards
{
    private const string GuidanceRelativeDirectory = "Mud.Feishu.AI.Tools/Guidance";

    /// <summary>首批深化域（R6/S1）——每个都必须至少有一个 L2 资产。</summary>
    private static readonly string[] HighFrequencyDomains =
    [
        "approval", "im", "bitable", "task", "calendar", "mail", "docx", "wiki",
    ];

    /// <summary>
    /// <b>L1 存在性</b>：每个已启用工具域的域前缀必须有 <c>Guidance/{domain}.md</c>。
    /// </summary>
    [Fact]
    public void EveryEnabledDomain_ShouldHaveL1Asset()
    {
        var root = GuidanceDirectory();
        var domains = FeishuToolContracts.AllNames
            .Select(static name => name.Split('.')[0])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static d => d, StringComparer.Ordinal)
            .ToArray();

        domains.Should().NotBeEmpty("未从契约表解析出任何域——域前缀规则变了（假绿），请先修守卫");

        var missing = domains
            .Where(domain => !File.Exists(Path.Combine(root, domain + ".md")))
            .ToArray();

        missing.Should().BeEmpty(
            "以下工具域没有 L1 guidance 资产（Guidance/{domain}.md）——模型在该域拿不到路由/避坑，"
            + "而工具却已启用（声明存在 ≠ 资产存在，R6/S1）：{0}",
            string.Join(" | ", missing));
    }

    /// <summary>
    /// <b>L2 键一致（双向）</b>：<c>Guidance/{domain}/{topic}.md</c> 的路径集合必须与
    /// <see cref="FeishuToolGuidance.ReferenceKeys"/> 精确相等。
    /// </summary>
    /// <remarks>
    /// <c>ReferenceKeys</c> 是 <c>feishu.guidance_read</c> 的候选清单来源：
    /// md 有而键无 ⇒ 资产写了却读不到；键有而 md 无 ⇒ 模型按提示去读必然失败。
    /// </remarks>
    [Fact]
    public void L2ReferenceKeys_ShouldMatchGenerated()
    {
        var root = GuidanceDirectory();
        var fromFiles = Directory
            .GetFiles(root, "*.md", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetDirectoryName(path), root, StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(root, path)
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(".md", string.Empty, StringComparison.Ordinal))
            .OrderBy(static k => k, StringComparer.Ordinal)
            .ToArray();

        fromFiles.Should().NotBeEmpty("未扫到任何 L2 资产——路径或过滤条件变了（假绿）");

        var generated = FeishuToolGuidance.ReferenceKeys
            .OrderBy(static k => k, StringComparer.Ordinal)
            .ToArray();

        fromFiles.Should().BeEquivalentTo(
            generated,
            "Guidance/{domain}/{topic}.md 与 FeishuToolGuidance.ReferenceKeys 必须一一对应"
            + "（md 多了 ⇒ 无人能读到；键多了 ⇒ guidance_read 的提示指向不存在的文件）");
    }

    /// <summary><b>高频域覆盖下界</b>：首批 8 个域各至少一个 L2 资产。</summary>
    [Fact]
    public void HighFrequencyDomains_ShouldHaveL2Reference()
    {
        var root = GuidanceDirectory();
        var covered = Directory
            .GetFiles(root, "*.md", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetDirectoryName(path), root, StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar)[0])
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var missing = HighFrequencyDomains.Where(d => !covered.Contains(d)).ToArray();

        missing.Should().BeEmpty(
            "以下高频域没有 L2 资产（条件性 HOW 无处可读）：{0}", string.Join(" | ", missing));
    }

    /// <summary>反向自证：守卫必须真的能解析出域与 L2 键，否则上面的用例会因"空集合"而假绿。</summary>
    [Fact]
    public void Guard_ShouldActuallyResolveDomainsAndReferences()
    {
        var root = GuidanceDirectory();

        File.Exists(Path.Combine(root, "im.md")).Should().BeTrue(
            "判据失效：连 im.md 都定位不到（仓库根定位或路径片段坏了）");

        FeishuToolGuidance.ReferenceKeys.Should().Contain(
            "im/topics-and-replies",
            "判据失效：生成产物未含既有 L2 键——ReferenceKeys 语义变了");

        File.Exists(Path.Combine(root, "im", "topics-and-replies.md")).Should().BeTrue(
            "判据失效：既有 L2 资产未按 {domain}/{topic}.md 形态落盘");
    }

    private static string GuidanceDirectory()
    {
        var root = Path.Combine(
            FindRepositoryRoot(),
            GuidanceRelativeDirectory.Replace('/', Path.DirectorySeparatorChar));

        Directory.Exists(root).Should().BeTrue($"guidance 资产目录必须存在：{GuidanceRelativeDirectory}");
        return root;
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
