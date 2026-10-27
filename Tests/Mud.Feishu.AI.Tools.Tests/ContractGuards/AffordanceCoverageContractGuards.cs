// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.AgentTools;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// S-5：affordance **覆盖率**守卫——按风险分级设定最低覆盖率，低于即红。
/// </summary>
/// <remarks>
/// <para>
/// <b>与本目录 <c>AffordanceContractGuards</c> 的分工</b>：那边守「接线」（资产 → 编译期 Schema →
/// 模型可见描述）与「规模冻结」，对「该写而没写」只覆盖到 high-risk 一档。本类只回答一个问题：
/// <b>每个风险级有多少比例的工具真的有资产</b>。判据 <c>EveryHighRiskWriteTool</c> 已从接线守卫
/// 搬到这里（同一 closed set 不得有两个归属，见 AGENTS.md「单点闭合等式」）。
/// </para>
/// <para>
/// <b>要拦的真实回归</b>：新增写类工具时不补资产。规模冻结只盯份数——增工具 + 删资产可以互相抵消，
/// 而 <c>MUDFT028–031</c> 是「有资产才可能报」的诊断（没有文件就没有诊断，全绿）。比例阈值让「工具面
/// 涨了、覆盖没涨」必红：分母来自契约集合，不由人手抄。
/// </para>
/// <para>
/// <b>阈值阶梯（只抬不降，每次抬阈值须过 CHANGELOG）</b>：写作规范
/// <c>documents/AIAgent/affordance-写作规范.md</c>。
/// <list type="table">
/// <item><description>high-risk-write：100%（S-5 之前由强制集合保证，本批改写成比例）</description></item>
/// <item><description>write：100%（S-5 二波补齐后由 ≥80% 抬到满覆盖）</description></item>
/// <item><description>read：18%（S-5 三波接入 im/bitable/docx/drive/mail 的高频 list/get 后按实测起步）</description></item>
/// </list>
/// </para>
/// <para>
/// <b>反假绿</b>：任一风险级工具数为 0 即红（空分母会让「100%」恒成立）；比例判据另有
/// <see cref="CoverageCalculator_ShouldReportRatioAndMissingNames"/> 的合成自证。
/// </para>
/// </remarks>
public class AffordanceCoverageContractGuards
{
    private const string AssetDirectory = "Mud.Feishu.AI.Tools/Affordance";

    private const double HighRiskWriteFloor = 1.00;
    private const double WriteFloor = 1.00;

    // 只读级起步阈值：S-5 三波接入 5 个高频域的 list/get（33/176 = 18.75%），取 18% 起步。
    // 只读面不做满覆盖承诺——低频单点读取的决策路径由域 guidance 承担，资产只会挤占描述预算。
    private const double ReadFloor = 0.18;

    [Theory]
    [InlineData(FeishuToolRisk.HighRiskWrite, HighRiskWriteFloor)]
    [InlineData(FeishuToolRisk.Write, WriteFloor)]
    [InlineData(FeishuToolRisk.Read, ReadFloor)]
    public void TierCoverage_ShouldMeetTheFloor(FeishuToolRisk risk, double floor)
    {
        var assets = ReadAssetToolNames();
        var (total, covered, missing) = MeasureCoverage(risk, assets);

        // 分母防呆：风险级为空集时"0/0 = 100%"会伪装成通过。
        total.Should().BeGreaterThan(0, $"'{risk}' 风险级不得为空集（空分母会让覆盖率判据恒绿）");

        var ratio = (double)covered / total;
        ratio.Should().BeGreaterThanOrEqualTo(
            floor,
            $"'{risk}' 级 affordance 覆盖率 {covered}/{total}（{ratio:P1}）低于阈值 {floor:P0}——"
            + $"缺失工具：{string.Join(", ", missing.Take(40))}"
            + (missing.Length > 40 ? $"…（共 {missing.Length} 个）" : string.Empty));
    }

    /// <summary>
    /// 资产必须落在契约集合内：孤儿资产（工具已改名/删除但资产还留在目录里）既进不了模型可见面，
    /// 又会让分母口径与规模冻结一起漂移。
    /// </summary>
    [Fact]
    public void EveryAsset_ShouldMapToAnExistingTool()
    {
        var orphans = ReadAssetToolNames()
            .Where(name => !FeishuToolContracts.ByToolName.ContainsKey(name))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        orphans.Should().BeEmpty(
            "以下资产名在契约集合里不存在（工具改名或删除后资产未跟进）：{0}",
            string.Join(", ", orphans));
    }

    /// <summary>判据自证：合成输入必须同时给出正确比例与正确缺失名单（否则本守卫是假门禁）。</summary>
    [Fact]
    public void CoverageCalculator_ShouldReportRatioAndMissingNames()
    {
        var surface = new (string Name, FeishuToolRisk Risk)[]
        {
            ("a.delete", FeishuToolRisk.HighRiskWrite),
            ("a.update", FeishuToolRisk.Write),
            ("a.create", FeishuToolRisk.Write),
            ("a.get", FeishuToolRisk.Read),
        };

        var (total, covered, missing) = MeasureCoverage(
            surface, FeishuToolRisk.Write, ["a.update", "a.get"]);

        total.Should().Be(2);
        covered.Should().Be(1);
        missing.Should().Equal(["a.create"], "缺失名单必须只含该风险级");

        MeasureCoverage(surface, FeishuToolRisk.Write, []).Covered.Should().Be(0, "空资产集不得被当成'无需覆盖'");
        MeasureCoverage(surface, FeishuToolRisk.HighRiskWrite, ["a.delete"])
            .Missing.Should().BeEmpty("已覆盖的高危工具不得出现在缺失名单里");
    }

    /// <summary>按风险级复算覆盖率：契约集合出分母，磁盘资产出分子。</summary>
    private static (int Total, int Covered, string[] Missing) MeasureCoverage(
        FeishuToolRisk risk,
        HashSet<string> assets)
        => MeasureCoverage(
            FeishuToolContracts.ByToolName.Values
                .Select(static contract => (contract.Name, contract.Risk))
                .ToArray(),
            risk,
            assets);

    private static (int Total, int Covered, string[] Missing) MeasureCoverage(
        (string Name, FeishuToolRisk Risk)[] surface,
        FeishuToolRisk risk,
        HashSet<string> assets)
    {
        var names = surface
            .Where(pair => pair.Risk == risk)
            .Select(static pair => pair.Name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        return (names.Length, names.Count(assets.Contains), names.Where(name => !assets.Contains(name)).ToArray());
    }

    private static HashSet<string> ReadAssetToolNames()
    {
        var directory = Path.Combine(FindRepositoryRoot(), AssetDirectory.Replace('/', Path.DirectorySeparatorChar));
        Directory.Exists(directory).Should().BeTrue($"{AssetDirectory} 必须存在（否则本守卫退化为空扫描）");

        return new HashSet<string>(
            Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
                .Select(static path => Path.GetFileNameWithoutExtension(path)),
            StringComparer.Ordinal);
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
