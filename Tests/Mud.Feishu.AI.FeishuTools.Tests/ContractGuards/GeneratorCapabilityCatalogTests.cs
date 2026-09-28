// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

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
/// 注：<c>FeishuCapabilityCatalog</c> 为 <c>internal</c>（不进入公共 API 面），
/// 经 <c>InternalsVisibleTo</c> 对测试可见；其发射由
/// <c>Mud.Feishu.AI.FeishuTools.csproj</c> 的 <c>FeishuToolCatalog=true</c> 开启。
/// </para>
/// </remarks>
public class GeneratorCapabilityCatalogTests
{
    /// <summary>SDK 能力总数必须显著大于"看上去像"的量级——低于此值说明扫描没真正跑起来。</summary>
    [Fact]
    public void SdkMethodCount_ShouldReflectRealSdkSurface_NotAnEmptyScan()
    {
        FeishuCapabilityCatalog.SdkMethodCount.Should().BeGreaterThan(200,
            "Mud.Feishu 的 32 个域 / 383 个接口文件不可能只有寥寥数个方法——" +
            "低于此值说明 Tier R 扫描没扫到 SDK（生成器又退化成只看 [FeishuTool]）");
    }

    /// <summary>策展工具数必须与契约表一致（两条派生路径同源）。</summary>
    [Fact]
    public void CuratedToolCount_ShouldMatchTheDerivedContractTable()
    {
        FeishuCapabilityCatalog.CuratedToolCount.Should().Be(FeishuToolNames.All.Length,
            "能力目录的策展计数与工具名契约表必须同源（都从 [FeishuTool] 派生）");
    }

    /// <summary>
    /// 分组分布必须真实，且各组之和等于总数。
    /// </summary>
    /// <remarks>
    /// 分组轴是<b>接口名的 domain+resource 段</b>（如 <c>BitableAppTable</c>/<c>ApprovalTask</c>），
    /// 比"32 个 Interfaces 目录域"更细——它同时反映资源粒度，故数量显著大于目录域数。
    /// </remarks>
    [Fact]
    public void MethodsByDomain_ShouldBeConsistentAndCoverManyGroups()
    {
        var byDomain = FeishuCapabilityCatalog.MethodsByDomain;

        byDomain.Should().NotBeEmpty();
        FeishuCapabilityCatalog.DomainCount.Should().Be(byDomain.Count);
        FeishuCapabilityCatalog.DomainCount.Should().BeGreaterThan(100,
            "能力分组轴 = domain+resource 段，数量应接近接口文件数（数百），远多于 32 个目录域");
        byDomain.Values.Sum().Should().Be(FeishuCapabilityCatalog.SdkMethodCount,
            "各组方法数之和必须等于总量（否则统计口径不一致）");
    }

    /// <summary>
    /// 策展厅远小于能力面——这正是"目录全量、暴露策展"双层决策的量化体现，
    /// 也让"还差多少能力"从主观判断变成可见数字。
    /// </summary>
    [Fact]
    public void CuratedSurface_ShouldBeASmallSubsetOfTheCapabilityCatalog()
    {
        FeishuCapabilityCatalog.CuratedToolCount.Should().BeLessThan(FeishuCapabilityCatalog.SdkMethodCount,
            "暴露面必须是 SDK 能力面的真子集（无差别全量暴露是明确非目标）");
    }
}
