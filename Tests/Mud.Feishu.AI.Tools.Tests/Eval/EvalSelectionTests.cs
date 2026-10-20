// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.Tools.Tests.Eval;

/// <summary>
/// 评测层一「工具选择」：意图 → 模型可见面上的目标工具名次（pass@k）。
/// </summary>
/// <remarks>
/// <para>
/// <b>它拦的是什么</b>：365 枚工具的选择负担。描述与参数名是模型唯一的选址依据，一处静默改写
/// （描述缩水、两枚工具描述趋同）编译期不报错，模型却会选错。评测集给「用户会怎么说」，判据给「可见面接得住吗」。
/// </para>
/// <para>
/// <b>阈值是棘轮，不是目标</b>：打分器是确定性 TF-IDF（离线、可复现、零新增包），度量<b>描述判别度</b>
/// 的下界，不是任何大模型的准确率。数值按首轮实测回填、只升不降；提升方式是改描述，不是调松阈值。
/// </para>
/// <para>
/// <b>首轮实测</b>（2026-10-20，192 条 / 365 枚）：裸面 pass@1 47.9%、@3 71.9%、@5 80.2%；
/// affordance 视图 45.3%——<b>词汇级检索上 affordance 不加分</b>（它承载何时用/勿用/前置条件，属语义面）。
/// 其收益只读报告的 guided−bare 差值，"补 affordance 即提升选择准确率"不得当作前提写进文案。
/// </para>
/// </remarks>
public class EvalSelectionTests
{
    /// <summary>评测集规模下界：黄金集不足 100 条时，pass@k 只是小样本噪声。</summary>
    private const int MinimumCaseCount = 100;

    /// <summary>每个「工具数 ≥2 的域」至少几条用例——防长尾域整体无覆盖。</summary>
    private const int MinimumCasesPerDomain = 2;

    /// <summary>裸可见面 pass@1 基线（2026-10-20 首轮实测 47.9% = 92/192 回填）。</summary>
    private const double MinimumPassAt1 = 0.47;

    /// <summary>pass@3 基线（首轮实测 71.9%）。</summary>
    private const double MinimumPassAt3 = 0.71;

    /// <summary>pass@5 基线（首轮实测 80.2%）：模型可在候选集内自行分辨的下界。</summary>
    private const double MinimumPassAt5 = 0.80;

    /// <summary>affordance 视图 pass@1 基线（首轮实测 45.3%）。</summary>
    private const double MinimumGuidedPassAt1 = 0.45;

    [Fact]
    public void SelectionSet_ShouldBeStratifiedAndFreeOfHallucinatedNames()
    {
        var cases = EvalAssets.Selection;
        cases.Should().HaveCountGreaterThanOrEqualTo(MinimumCaseCount,
            "黄金集规模是 pass@k 读数的可信度前提（不足 100 条即小样本噪声）");

        cases.Should().OnlyHaveUniqueItems(static c => c.Id, "用例 id 是报告与失败消息的寻址键");
        var surface = EvalToolSurface.Instance;
        surface.Entries.Should().HaveCount(FeishuToolNames.All.Length,
            "可见面必须覆盖全部契约工具——少了即评测对象不是产品面");

        var hallucinated = cases
            .SelectMany(c => c.Expect.Select(name => (c.Id, name)))
            .Where(pair => !surface.Contains(pair.name))
            .Select(static pair => $"{pair.Id}→{pair.name}")
            .ToArray();
        hallucinated.Should().BeEmpty(
            "评测集里出现了不存在的工具名（幻觉工具名）——评测集会随工具面改名而静默失真");

        var perDomain = cases.GroupBy(static c => c.Domain).ToDictionary(static g => g.Key, static g => g.Count());
        perDomain.Keys.Should().BeEquivalentTo(surface.Entries.Select(static e => e.Domain).Distinct(),
            "每个业务域都要有选择用例——漏一域即该域的描述质量完全无度量");

        var thin = surface.Entries
            .GroupBy(static e => e.Domain)
            .Where(static g => g.Count() >= 2)
            .Select(g => (Domain: g.Key, Count: perDomain.GetValueOrDefault(g.Key)))
            .Where(static pair => pair.Count < MinimumCasesPerDomain)
            .Select(static pair => $"{pair.Domain}({pair.Count})")
            .ToArray();
        thin.Should().BeEmpty($"工具数 ≥2 的域至少要 {MinimumCasesPerDomain} 条用例（否则该域只测到一枚工具）");
    }

    [Fact]
    public void Selection_ShouldReachPassAt1Floor()
    {
        using var runner = new EvalRunner();
        var findings = runner.RunSelection();
        var total = findings.Count;

        var passAt1 = findings.Count(static f => f.Rank == 1);
        var passAt3 = findings.Count(static f => f.Rank is > 0 and <= 3);
        var passAt5 = findings.Count(static f => f.Rank is > 0 and <= 5);
        var guidedAt1 = findings.Count(static f => f.GuidedRank == 1);

        passAt1.Should().BeGreaterThanOrEqualTo((int)Math.Ceiling(total * MinimumPassAt1),
            $"裸可见面 pass@1 低于基线（实测 {passAt1}/{total}）——描述判别度退化。" +
            $"失败用例见报告：\n{runner.Render(findings, [], [])}");
        passAt3.Should().BeGreaterThanOrEqualTo((int)Math.Ceiling(total * MinimumPassAt3),
            $"pass@3 低于基线（实测 {passAt3}/{total}）：目标工具连前三候选都进不去，模型没有自我纠正的余地");
        passAt5.Should().BeGreaterThanOrEqualTo((int)Math.Ceiling(total * MinimumPassAt5),
            $"pass@5 低于基线（实测 {passAt5}/{total}）：意图与可见面词汇脱节——改描述，别松阈值");
        guidedAt1.Should().BeGreaterThanOrEqualTo((int)Math.Ceiling(total * MinimumGuidedPassAt1),
            $"affordance 视图 pass@1 低于基线（实测 {guidedAt1}/{total}）");
    }

    [Fact]
    public void Selection_ShouldSeparateWriteToolsFromTheDefaultReadonlySurface()
    {
        // 选对但用不上，也是失败：写类工具默认不在启用面上，模型看不见就等于不存在。
        using var runner = new EvalRunner();
        var findings = runner.RunSelection();

        var writeExpectations = findings.Count(static f => f.ExpectsWriteTool);
        writeExpectations.Should().BeGreaterThan(0,
            "评测集必须覆盖写类意图——否则「只读面够用吗」这个真问题永远测不到");
        writeExpectations.Should().BeLessThan(findings.Count / 2,
            "选择用例应以只读意图为主（写意图的可达性由场景层的授权/预演路径度量）");
    }

    [Theory]
    // 分词器自证：拉丁串按 . _ 切、CJK 同时产出单字与二字组（否则判据可能只是"永远返回空"或"永远同序"）。
    [InlineData("bitable.query_records", new[] { "bitable", "query", "records" })]
    [InlineData("多维表格", new[] { "多", "维", "表", "格", "多维", "维表", "表格" })]
    [InlineData("dry_run", new[] { "dry", "run" })]
    public void Tokenizer_ShouldSplitBothNamingSystems(string text, string[] expectedContains)
    {
        var tokens = Tokenizer.Tokenize(text);
        tokens.Should().Contain(expectedContains);
    }

    [Theory]
    [InlineData("在多维表格里新增一行记录", "bitable.add_record")]
    [InlineData("把云空间这个文件下载到本地", "drive.download_file")]
    [InlineData("把刚发出去的那条消息撤回", "im.revoke_message")]
    [InlineData("我有哪些邮件签名", "mail.signature_list")]
    public void Scorer_ShouldRankExpectedToolFirst_OnDistinctiveIntents(string intent, string expected)
    {
        // 判据自证：打分器必须真的能分辨（否则 pass@1 阈值只是随机数上的断言 = 假绿）。
        var ranked = EvalToolSurface.Instance.Rank(intent, guided: false, topK: 5);
        ranked.ContainExpectedFirst(expected, intent);
    }

    [Fact]
    public void Scorer_ShouldProduceDeterministicOrdering()
    {
        var surface = EvalToolSurface.Instance;
        var first = surface.Rank("查询多维表格记录", guided: false, topK: 10);
        var second = surface.Rank("查询多维表格记录", guided: false, topK: 10);
        first.Should().Equal(second, "同一意图两次排序不一致 ⇒ 报告不可复现（评测的立身之本）");
    }

    [Theory]
    [InlineData("bitable.query_records", true)]
    [InlineData("docx.history_list", true)]
    [InlineData("ghost.not_registered", false)]
    public void Surface_ContainsShouldTrackTheContractTools(string tool, bool expected)
    {
        // 幻觉检测判据自证：正例与负例都必须能被触发（只测一侧等于没测）。
        EvalToolSurface.Instance.Contains(tool).Should().Be(expected);
    }
}

internal static class EvalSelectionAssertionExtensions
{
    /// <summary>首位必须是期望工具；同时断言候选集非空（防空集假绿）。</summary>
    internal static void ContainExpectedFirst(
        this string[] ranked,
        string expected,
        string intent)
    {
        ranked.Should().NotBeEmpty($"意图「{intent}」在可见面上应至少命中一个候选");
        ranked[0].Should().Be(expected, $"意图「{intent}」的判别度不足：首位被 {ranked[0]} 抢走");
    }
}
