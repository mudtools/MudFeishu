// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / D-3′</b>：工具声明 scope 的<b>一致性守卫</b>（独立于 F-13）。
/// </summary>
/// <remarks>
/// <para>
/// <b>要解决什么</b>：<c>[FeishuTool(RequiredScopes = […])]</c> 的字符串会
/// ① 进入 <c>x-feishu.required_scopes</c> 供 <c>IToolExecutionAuthorizer</c> 判权、
/// ② 进入工具描述给模型看。scope 拼错时授权器会<b>永远拒绝</b>（工具永不可用）或<b>过宽</b>
/// （授权失效），而两者都<b>不会在构建期报错</b>——SDK 侧没有 scope 的编译期清单。
/// </para>
/// <para>
/// <b>为什么只做"仓内一致性"这一半</b>：本仓<b>没有</b>权威 scope 清单（飞书未提供可离线校验的清单），
/// "平台是否承认该 scope"<b>不可判定</b>。能机械判定的是：工具声明的每个 scope 必须在
/// <b>本仓其它出处</b>（接口文档 / Demos / SDK 注释）也出现过——臆造/拼错的 scope 几乎必然
/// 只出现在工具声明这一处。遵守实施纪律：<b>不把不可判定的事写成假门禁</b>。
/// </para>
/// <para>
/// <b>实测</b>：26 个声明 scope 全部在别处有出处，当前全绿；能在"新增工具顺手编一个 scope"时立刻变红。
/// 语料<b>排除工具接口目录</b>——不排除则每个 scope 必然在自己身上命中，守卫恒绿。
/// </para>
/// </remarks>
public class ToolScopeDeclarationContractTests
{
    private static readonly string ToolInterfacesDirectory = Path.Combine(
        FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Curation");

    /// <summary><b>方向一</b>：工具声明的每个 scope 必须在仓内其它出处也出现过。</summary>
    [Fact]
    public void EveryDeclaredScope_ShouldAppearElsewhereInTheRepository()
    {
        var declared = ReadDeclaredScopes();
        declared.Should().HaveCountGreaterThan(
            0, "未解析到任何 RequiredScopes 声明——解析规则已失效（假绿），请先修守卫");

        var corpus = BuildCorpus();
        var orphans = declared.Where(scope => !corpus.Contains(scope)).OrderBy(static s => s, StringComparer.Ordinal);

        orphans.Should().BeEmpty(
            "以下 scope 只在工具声明里出现、仓内无其它出处 ⇒ 极可能是臆造或拼错。"
            + "后果：授权器比对将<b>永远拒绝</b>（工具永不可用）或反向<b>过宽</b>使授权失效。"
            + "修法：对照飞书开放平台权限清单改用正确 scope；确为平台有效但仓内无出处的，"
            + "请在 documents/ 补记来源：{0}",
            string.Join(" | ", orphans));
    }

    /// <summary>
    /// <b>基线</b>：声明 scope 集合必须与登记一致 —— 新增/删除 scope 是<b>有意的授权面变更</b>，
    /// 需显式更新此基线（防止悄悄放行一个未核对的新 scope）。
    /// </summary>
    [Fact]
    public void DeclaredScopeSet_ShouldMatchRegisteredBaseline()
    {
        const int ExpectedScopeCount = 49; // 2026-10-10 实测（R7/A4~A6：Board 2 + Attendance 3 + Spark 3 新增 scope）

        var declared = ReadDeclaredScopes();
        declared.Should().HaveCount(
            ExpectedScopeCount,
            "声明的 scope 数从 {0} 变为 {1}：{2}。新增/删除 scope 属有意的授权面变更，"
            + "请确认每个新 scope 都已对照飞书权限清单核对，并同步更新本基线",
            ExpectedScopeCount,
            declared.Count,
            string.Join(" | ", declared));
    }

    /// <summary>
    /// <b>形态守卫</b>：scope 必须是小写、点号与冒号分层的形态。
    /// </summary>
    /// <remarks>
    /// <b>为什么必须允许点号</b>：飞书确有含点号的真实 scope，如
    /// <c>contact:user.base:readonly</c> / <c>contact:department.base:readonly</c>
    /// （已登记于 <c>documents/AIAgent/工具权限对照表.md</c>）。本守卫首版按
    /// <c>^[a-z][a-z0-9_]*(:[a-z0-9_]+)+$</c> 判定，把这两个<b>正确</b>的 scope 误报为形态异常
    /// —— 教训同 §13.3：<b>守卫的正则必须先用真实语料验证</b>，否则守卫本身是假红源。
    /// </remarks>
    [Fact]
    public void DeclaredScopes_ShouldUseLowercaseColonSeparatedForm()
    {
        var malformed = ReadDeclaredScopes()
            .Where(static s => !Regex.IsMatch(s, ScopeFormPattern))
            .OrderBy(static s => s, StringComparer.Ordinal);

        malformed.Should().BeEmpty(
            "以下 scope 不符合「小写 + 点号/冒号分层」形态：{0}", string.Join(" | ", malformed));
    }

    // ────────── 采集实现 ──────────

    /// <summary>读取工具接口里 <c>RequiredScopes = […]</c> 声明的全部 scope。</summary>
    private const string ScopeFormPattern = @"^[a-z][a-z0-9_]*(\.[a-z0-9_]+)*(:[a-z0-9_]+(\.[a-z0-9_]+)*)+$";

    private const string ScopeScanPattern = @"\b[a-z][a-z0-9_.]*(?::[a-z0-9_.]+)+\b";

    /// <summary>读取工具接口里 <c>RequiredScopes = […]</c> 声明的全部 scope。</summary>
    private static SortedSet<string> ReadDeclaredScopes()
    {
        var result = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(ToolInterfacesDirectory, "*.cs", SearchOption.TopDirectoryOnly))
        {
            var source = File.ReadAllText(file);
            foreach (System.Text.RegularExpressions.Match list in Regex.Matches(
                source, @"RequiredScopes\s*=\s*\[(?<body>[^\]]*)\]"))
            {
                foreach (System.Text.RegularExpressions.Match item in Regex.Matches(
                    list.Groups["body"].Value, "\"(?<scope>[^\"]+)\""))
                {
                    result.Add(item.Groups["scope"].Value);
                }
            }
        }

        return result;
    }

    /// <summary>构建"仓内其它出处"语料：全部 <c>.cs</c>（<b>排除</b>工具接口目录）与 <c>.md</c>。</summary>
    private static HashSet<string> BuildCorpus()
    {
        var builder = new StringBuilder();

        foreach (var path in Directory.EnumerateFiles(FindRepositoryRoot(), "*.*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(path) is not (".cs" or ".md"))
            {
                continue;
            }

            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.StartsWith(ToolInterfacesDirectory, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                builder.Append(File.ReadAllText(path)).Append('\n');
            }
            catch (IOException)
            {
                // 语料读取失败时跳过：表现为"可能误报"，不会"漏报"。
            }
        }

        return Regex.Matches(builder.ToString(), ScopeScanPattern)
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(static m => m.Value)
            .ToHashSet(StringComparer.Ordinal);
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