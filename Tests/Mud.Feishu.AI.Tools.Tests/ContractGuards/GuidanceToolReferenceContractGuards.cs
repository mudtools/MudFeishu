// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R-12 防复发守卫：guidance 文本里出现的 <c>feishu.{name}</c> 标识符<b>必须</b>是真实存在的工具名。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须新增（R-12 的直接教训）</b>：删除 <c>feishu.api_call</c> 时，工具面与测试都可以
/// 干净删除，但 guidance（模型的行为依据）里仍留着"用 <c>feishu.api_call</c> 兜底调用"的指引——
/// <b>模型会照着一个不存在的工具名持续臆造调用</b>，而这在编译期与既有 44 个守卫里<b>没有任何信号</b>
/// （guidance 是散文，工具面是代码，两者之间原本没有机械联系）。
/// </para>
/// <para>
/// 本守卫把这条联系补上：guidance 的 <c>feishu.*</c> 引用集合必须 ⊆ <c>FeishuToolNames.All</c>。
/// 覆盖范围是<b>源文本</b>（<c>Guidance/**/*.md</c>）——派生产物 <c>skills/**</c> 由
/// <c>SkillsExportContractTests</c> 逐字节锁定其与源的等价性，故不必重复扫描。
/// </para>
/// <para>
/// <b>只扫 <c>feishu.{name}</c> 形态</b>：域级工具名（如 <c>bitable.list_tables</c>）在 guidance 里
/// 以自然语言出现（"用 bitable 的查询工具"），逐条断言会产出大量假红；而元工具引用是
/// <b>指令式</b>的（"→ 用 `feishu.xxx`"），正是"删了工具却留下指引"这类缺陷的高发面。
/// </para>
/// </remarks>
public class GuidanceToolReferenceContractGuards
{
    /// <summary>匹配 guidance 里的元工具引用形态：<c>feishu.{snake_case}</c>。</summary>
    private static readonly Regex ToolReference =
        new(@"feishu\.([a-z][a-z0-9_]*)", RegexOptions.Compiled);

    [Fact]
    public void GuidanceToolReferences_ShouldAllExistInToolContracts()
    {
        var guidanceRoot = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Guidance");
        var files = Directory.GetFiles(guidanceRoot, "*.md", SearchOption.AllDirectories);

        files.Should().NotBeEmpty("未扫到 guidance 源文件——路径错了（假绿），请先修守卫");

        var known = new HashSet<string>(FeishuToolNames.All, StringComparer.Ordinal);
        known.Should().NotBeEmpty("工具名契约表为空——判据无意义（假绿）");

        var violations = new List<string>();
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(guidanceRoot, file).Replace('\\', '/');
            // 显式限定 Match 类型：本工程全局 using 了 Moq，其 Moq.Match 与 Regex.Match 同名（CS0104）。
            foreach (System.Text.RegularExpressions.Match match in ToolReference.Matches(File.ReadAllText(file)))
            {
                var fullName = "feishu." + match.Groups[1].Value;
                if (!known.Contains(fullName))
                {
                    violations.Add($"{relative}: 引用了不存在的工具 {fullName}");
                }
            }
        }

        violations.Should().BeEmpty(
            "guidance 是模型的行为依据——引用不存在的工具会让模型持续臆造调用（R-12）：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// 反向自证：若把已删除的 <c>feishu.api_call</c> 写回任一 guidance 文件，本守卫必须报红。
    /// </summary>
    /// <remarks>
    /// 这里用"已知不在契约表里的名字"做金丝雀，而不是去读源文件——后者会让守卫依赖被守卫对象的当前状态。
    /// </remarks>
    [Fact]
    public void Guard_ShouldRejectARemovedToolName()
    {
        var known = new HashSet<string>(FeishuToolNames.All, StringComparer.Ordinal);

        const string canary = "「这个能力没策展」→ 用 `feishu.api_call` 兜底调用。";
        var matches = ToolReference.Matches(canary);
        matches.Should().NotBeEmpty("金丝雀：判据形态变了，本守卫会静默失效");

        matches.Cast<System.Text.RegularExpressions.Match>()
            .Select(static m => "feishu." + m.Groups[1].Value)
            .Should().NotContain(name => known.Contains(name),
                "R-12 已删除 feishu.api_call——它必须不再出现在契约表里，否则本守卫形同虚设");
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
