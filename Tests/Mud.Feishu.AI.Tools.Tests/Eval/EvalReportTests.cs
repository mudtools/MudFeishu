// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.Eval;

/// <summary>
/// 评测体系的验收线「报告可复现」：一次跑完三层，报告文本逐字稳定且四层结构齐全。
/// </summary>
/// <remarks>
/// 阈值断言在各层自己的测试里，本测试只锁"报告完整 + 可复现"。落盘评审件：
/// <c>FeishuEvalDumpDir=&lt;目录&gt; dotnet test Tests/Mud.Feishu.AI.Tools.Tests --filter "FullyQualifiedName~Tools.Tests.Eval"</c>
/// → <c>&lt;目录&gt;/eval-report.md</c>（相对值按仓库根解析，不是测试输出目录）；未设置该变量时不落盘（CI 不产副产物）。
/// </remarks>
public class EvalReportTests
{
    [Fact]
    public async Task Report_ShouldBeCompleteAndByteStableAcrossRuns()
    {
        using var first = new EvalRunner();
        var early = await first.RenderAllAsync();

        using var second = new EvalRunner();
        var late = await second.RenderAllAsync();

        late.Should().Be(early, "同一份代码 + 同一份资产两次运行必须逐字一致，否则报告不可复现");

        early.Should().Contain("## 一、选择层未中首位的用例", "选择层缺席即报告只剩标题");
        early.Should().Contain("## 二、参数层");
        early.Should().Contain("## 三、场景层结局桶");
        early.Should().Contain("## 四、失败分类");
        early.Should().NotContain("n/a", "任一层读数为 n/a 说明该层没跑到（空集假绿）");
        early.Should().NotContain("- unknown：", "有步骤落进未归类结局，桶分布不再可信");
    }
}
