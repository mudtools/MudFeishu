// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.Eval;

/// <summary>
/// 评测层三「端到端场景」：按装配剖面跑多步链路，逐步断言结局桶（含 HITL 模拟批准）。
/// </summary>
/// <remarks>
/// <para>
/// <b>离线容器的边界</b>：SDK 客户端是 Mock，读类工具拿不到有意义的响应，所以只有<b>无下游调用</b>
/// 的元工具断言 <c>ok</c>；写类工具一律走 <c>dry_run</c> 预演断言 <c>plan</c>（副作用为零）。
/// 场景层度量的是<b>门禁与链路的编排语义</b>（可见性 → 授权 → 确认 → 风险 → 参数 → 预演），
/// 不是平台返回值。
/// </para>
/// <para>
/// <b>HITL 怎么离线测</b>：脚本化授权器对写工具先返回"需人工确认"，宿主批准通道登记后由用例
/// 显式模拟"用户点了批准"再重调本步——两步都必须断言，缺任一步即批准闭环不可验证。
/// </para>
/// </remarks>
public class EvalScenarioTests
{
    /// <summary>剖面全集必须与 <see cref="EvalRunner"/> 的装配分支一一对应（新增剖面两处同批改）。</summary>
    private static readonly string[] KnownProfiles =
    [
        "readonly_default", "write_forced", "write_open", "hitl_confirm", "risk_ceiling_read",
    ];

    /// <summary>结局桶词汇；<c>unknown</c> 刻意不在其中——任何步骤落进 unknown 都判评测判据失效。</summary>
    private static readonly string[] KnownBuckets =
    [
        "ok", "plan", "validation", "confirmation", "denied", "not_enabled", "risk",
    ];

    [Fact]
    public void ScenarioSet_ShouldDeclareKnownProfilesBucketsAndApprovals()
    {
        var scenarios = EvalAssets.Scenarios;
        scenarios.Should().NotBeEmpty("场景层用例为空即端到端判据整体缺席");
        scenarios.Should().OnlyHaveUniqueItems(static s => s.Name, "场景名是失败消息的寻址键");

        var surface = EvalToolSurface.Instance;
        foreach (var scenario in scenarios)
        {
            KnownProfiles.Should().Contain(scenario.Profile, $"场景「{scenario.Name}」用了未登记的剖面");
            scenario.Steps.Should().NotBeEmpty($"场景「{scenario.Name}」至少一步才有链路语义");

            var ghost = scenario.Steps.Select(static s => s.Tool).Where(t => !surface.Contains(t)).ToArray();
            ghost.Should().BeEmpty($"场景「{scenario.Name}」引用了不存在的工具名（幻觉工具名）");

            foreach (var step in scenario.Steps)
            {
                var tokens = step.Expect.Split('|').Select(static s => s.Trim()).ToArray();
                tokens.Should().OnlyContain(static t => KnownBuckets.Contains(t),
                    $"场景「{scenario.Name}」/ {step.Tool} 的 expect 用了桶词汇外的值（unknown 不可声明）");

                if (step.ApproveAfterHost)
                {
                    (surface.Find(step.Tool)?.IsWrite ?? false).Should().BeTrue(
                        $"approve 只对有写语义的步骤有意义（读工具无需批准）：场景「{scenario.Name}」");
                }
            }
        }

        var unused = KnownProfiles.Except(scenarios.Select(static s => s.Profile), StringComparer.Ordinal).ToArray();
        unused.Should().BeEmpty("剖面没有被任何场景使用 ⇒ 该装配分支的门禁语义无人验证，剖面即死代码");
    }

    [Fact]
    public async Task Scenarios_ShouldLandInDeclaredBuckets()
    {
        using var runner = new EvalRunner();
        var findings = await runner.RunScenariosAsync();

        findings.Should().NotBeEmpty("一步都没跑成 ⇒ 场景层是空转（假绿）");

        var drift = findings.Where(static f => !f.Passed)
            .Select(static f => $"  - {f.Scenario} 第 {f.Step} 步 {f.Tool}（期望 {f.Expect}）实落 {EvalRunner.Literal(f.Actual)}：{f.Detail}")
            .ToArray();
        drift.Should().BeEmpty(
            $"场景层结局与声明不一致（{drift.Length}/{findings.Count} 步）——门禁/授权/预演任一条语义改变都必须显式裁决：" +
            $"改场景资产还是改产品。\n报告：\n{runner.Render([], [], findings)}");
    }

    [Fact]
    public async Task ScenarioRun_ShouldExerciseEveryBucket()
    {
        using var runner = new EvalRunner();
        var findings = await runner.RunScenariosAsync();

        var exercised = findings.Select(static f => EvalRunner.Literal(f.Actual)).Distinct(StringComparer.Ordinal).ToArray();
        var missing = KnownBuckets.Except(exercised, StringComparer.Ordinal).ToArray();

        missing.Should().BeEmpty(
            $"每个结局桶都至少被一个场景真实触发，桶分类才有意义（缺 {string.Join('/', missing)}）；" +
            $"\n报告：\n{runner.Render([], [], findings)}");
    }
}
