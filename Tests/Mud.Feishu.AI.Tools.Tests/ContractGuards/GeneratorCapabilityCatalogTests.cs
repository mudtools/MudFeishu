// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// Tier R 能力目录守卫：证明源生成器<b>确实消费了</b> <c>Mud.Feishu</c> SDK 接口并产出了可度量的能力事实。
/// </summary>
/// <remarks>
/// <para>
/// 这组用例是"生成器未接线"（原审查 G1/G2）的<b>可验证回归面</b>：在此之前，
/// 生成器只扫描 <c>[FeishuTool]</c> 接口，对 SDK 的 1000+ 个方法一无所知，
/// "能力面差距"只能靠人肉统计。现在它是构建期产出的事实，并由本用例在 CI 中锁定。
/// </para>
/// <para>
/// <b>扫描路径的真实分工（AT-B18 修正）</b>：本类锁定的是<b>路径②</b>——SDK 接口
/// （<c>IFeishu[Tenant|User]V*</c>）<b>仅</b>聚合为能力目录事实，<b>不产任何工具</b>；
/// 产工具的唯一路径是手写 <c>[FeishuTool]</c> 接口（路径①，经 <c>CuratedToolScanner</c>）。
/// 原注释/原 <c>Extractors</c> 类注释声称"SDK 接口自动派生 Tier R 工具"，与实现不符——
/// 该误解会诱导后续开发者按"能力已存在"的错误前提重复建设，故此处显式纠正。
/// </para>
/// <para>
/// 注：<c>FeishuToolCapabilityCatalog</c> 为 <c>internal</c>（不进入公共 API 面），
/// 经 <c>InternalsVisibleTo</c> 对测试可见；其发射由
/// <c>Mud.Feishu.AI.Tools.csproj</c> 的 <c>FeishuToolCatalog=true</c> 开启。
/// </para>
/// </remarks>
public class GeneratorCapabilityCatalogTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // AT-B17：覆盖数字精确锁定（原为宽松下界断言，SDK 面缩水一半仍绿）
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SDK 接口声明的方法总数——<b>精确值</b>。
    /// </summary>
    /// <remarks>
    /// <b>为什么必须精确（AT-B17）</b>：这两个数字是"能力面差距"的<b>唯一量化依据</b>，
    /// 被上游主方案与对外文档反复引用。原断言是 <c>BeGreaterThan(200)</c>——
    /// 等于只要扫描器还活着就恒绿，SDK 接口被批量改名/挪出程序集导致能力面缩水一半也照样通过。
    /// <para>
    /// <b>变更流程（重要）</b>：本值随 SDK 演进变化属正常，但<b>必须经评审</b>后显式改这一处常量——
    /// 因为它同时意味着"能力面差距"这一对外口径的变化。禁止改成区间/下界断言
    /// （那正是本次修复要消除的假绿形态）。
    /// </para>
    /// </remarks>
    private const int ExpectedSdkMethodCount = 1228;

    /// <summary>能力分组个数（分组轴 = 接口名的 domain+resource 段）——精确值（AT-B17）。</summary>
    private const int ExpectedDomainCount = 192;

    /// <summary>SDK 能力总数与 <c>Mud.Feishu</c> 的实际规模一致（精确锁定，非下界）。</summary>
    [Fact]
    public void SdkMethodCount_ShouldMatchExactExpectedValue()
    {
        FeishuToolCapabilityCatalog.SdkMethodCount.Should().Be(ExpectedSdkMethodCount,
            "SDK 方法总数是「能力面差距」的量化依据，必须精确锁定（原 BeGreaterThan(200) 是假绿："
            + "生成器退化成只看 [FeishuTool] 也照样通过）。变更该值须先评审。");
    }

    /// <summary>策展工具数必须与契约表一致（两条派生路径同源）。</summary>
    [Fact]
    public void CuratedToolCount_ShouldMatchTheDerivedContractTable()
    {
        FeishuToolCapabilityCatalog.CuratedToolCount.Should().Be(FeishuToolNames.All.Length,
            "能力目录的策展计数与工具名契约表必须同源（都从 [FeishuTool] 派生）");
    }

    /// <summary>分组数与分布一致性（精确锁定 + 组内求和自洽）。</summary>
    [Fact]
    public void MethodsByDomain_ShouldBeConsistentAndMatchExactDomainCount()
    {
        var byDomain = FeishuToolCapabilityCatalog.MethodsByDomain;

        byDomain.Should().NotBeEmpty();
        FeishuToolCapabilityCatalog.DomainCount.Should().Be(byDomain.Count);
        FeishuToolCapabilityCatalog.DomainCount.Should().Be(ExpectedDomainCount,
            "能力分组数（= 接口文件级的 domain+resource 段）必须精确锁定——原 BeGreaterThan(100) 同样是假绿");
        byDomain.Values.Sum().Should().Be(FeishuToolCapabilityCatalog.SdkMethodCount,
            "各组方法数之和必须等于总量（否则统计口径不一致）");
    }

    /// <summary>
    /// 非空守卫的"存在性"面：扫描必须真的扫到 SDK（补足精确值"手改常量即通过"的残余风险）。
    /// </summary>
    /// <remarks>
    /// 精确值断言能防"缩水"，但防不住"有人把常量改成扫描器的实际产出"。故再用两个<b>与扫描器输出同源</b>
    /// 的量级事实交叉验证：分组数必须显著多于 <c>Interfaces/</c> 的目录域数（约 32），
    /// 且策展面必须是能力面的真子集。
    /// </remarks>
    [Fact]
    public void CapabilityCatalog_ShouldBeInternallyCoherent()
    {
        FeishuToolCapabilityCatalog.DomainCount.Should().BeGreaterThan(100,
            "分组轴是 domain+resource 段，数量应接近接口文件数（数百），远多于 32 个目录域");

        FeishuToolCapabilityCatalog.CuratedToolCount.Should().BeLessThan(FeishuToolCapabilityCatalog.SdkMethodCount,
            "暴露面必须是 SDK 能力面的真子集（无差别全量暴露是明确非目标）");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // R4-9：能力目录对本包是**必需产物**（"opt-in"措辞与实际强依赖对齐）
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// R4-9：<c>Mud.Feishu.AI.Tools</c> 必须显式开启 <c>FeishuToolCatalog</c>。
    /// </summary>
    /// <remarks>
    /// 消费方 <c>CapabilityLookupTools</c> <b>编译期无条件</b>引用 <see cref="FeishuToolCapabilityCatalog"/>，
    /// 关闭该属性即 <c>CS0103</c>（构建失败）——故"显式 opt-in、关闭即不产出"的措辞与事实不符：
    /// <b>对本包它是必需产物</b>（属性仍是构建开关，但"可关闭"只对 AI 底座/测试等工程成立）。
    /// 本守卫把该耦合显式化：删掉 csproj 属性时失败信息直接指出消费方依赖，
    /// 而不是抛一个难以定位的裸 <c>CS0103</c>。
    /// </remarks>
    [Fact]
    public void FeishuToolCatalog_ShouldBeEnabledForThisPackage_AsRequiredArtifact()
    {
        var root = FindRepositoryRoot();
        var csproj = Path.Combine(root, "Mud.Feishu.AI.Tools", "Mud.Feishu.AI.Tools.csproj");
        var consumer = Path.Combine(root, "Mud.Feishu.AI.Tools", "Internal", "CapabilityLookupTools.cs");

        File.Exists(csproj).Should().BeTrue();
        File.ReadAllText(csproj).Should().MatchRegex(
            @"<FeishuToolCatalog>\s*true\s*</FeishuToolCatalog>",
            "FeishuTools 必须开启 FeishuToolCatalog——CapabilityLookupTools 编译期依赖 FeishuToolCapabilityCatalog（R4-9）");

        File.Exists(consumer).Should().BeTrue();
        File.ReadAllText(consumer).Should().Contain("FeishuToolCapabilityCatalog.",
            "消费点必须存在；若该引用被移除，则本属性对本包不再必需，守卫与注释口径需同步修订（R4-9）");
    }

    /// <summary>跨平台定位仓库根目录（以 <c>Mud.Feishu.slnx</c> 为锚，对齐本仓其余守卫）。</summary>
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
