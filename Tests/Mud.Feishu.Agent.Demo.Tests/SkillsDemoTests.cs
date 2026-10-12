// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// R-3 / F-8：Skills 产物消费演示的闭环断言（<b>首个真实消费方</b>）。
/// </summary>
/// <remarks>
/// 断言的是"产物 → 指令面"整条链，而不是"打印了点什么"：
/// ① 产物能被发现并按域读取；② 内容进入 guidance 装配；③ 全量注入不触达预算（触达即报红——
/// 那说明产物规模需要与 <c>MaxGuidanceLength</c> 同批评审）。
/// </remarks>
public class SkillsDemoTests
{
    private static ServiceProvider BuildProvider()
        => new ServiceCollection()
            .AddSingleton(Options.Create(new FeishuAgentOptions { Instructions = "skills-demo" }))
            .AddFeishuTools()
            .BuildServiceProvider();

    [Fact]
    public void BuildReport_ShouldReadProducts_AndInjectGuidance()
    {
        using var provider = BuildProvider();

        var text = string.Join(
            "\n",
            SkillsDemo.BuildReport(provider.GetRequiredService<ISkillDocumentReader>(), "bitable"));

        text.Should().Contain("package_version", "报告必须暴露产物所属包版本（F-8 版本一致性）");
        text.Should().Contain("skills/feishu-bitable/SKILL.md", "必须展开所选域的产物（按域读取）");
        text.Should().Contain("=== 域 bitable 的 SKILL.md", "必须展示产物正文");
        text.Should().Contain("guidance 注入", "闭环最后一环：产物进入模型指令面");
        text.Should().Contain("全量产物规模", "必须报告整体产物规模（外部 Agent 的自省依据）");

        text.Should().NotContain(
            "[warn]",
            "单域产物注入不应触达 guidance 预算——触达说明该域产物与 MaxGuidanceLength 需同批评审");
    }

    [Fact]
    public void BuildReport_ShouldWarn_WhenDomainHasNoSkillFile()
    {
        using var provider = BuildProvider();

        var text = string.Join(
            "\n",
            SkillsDemo.BuildReport(provider.GetRequiredService<ISkillDocumentReader>(), "no-such-domain"));

        text.Should().Contain("[warn]", "未知域必须显式告知（而不是静默输出空内容）");
        text.Should().Contain("可用域", "告警必须给出可用的域清单，否则宿主无从修正");
    }

    [Fact]
    public void BuildReport_ShouldRejectNullReader()
    {
        var act = () => SkillsDemo.BuildReport(null!, "im");

        act.Should().Throw<ArgumentNullException>();
    }
}
