// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// 《工具权限对照表》与工具名契约表的一致性守卫（AT-B21，§8.2 #11）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：新增工具时最容易漏的一步就是"忘了往文档表里加一行"——
/// 而 <c>FeishuToolContractGuards</c> 的 remarks 曾声称"与对照表逐一精确相等"，
/// 实际上对照表缺了 contact 三工具（声称不成立却无人发现）。本用例把该声称变成机械事实。
/// </para>
/// <para>
/// <b>解析规则（评审 C-10 修正）</b>：表头是 <c>| # | 工具名 | RequiredScopes | … |</c>——
/// 工具名在<b>第 2 个单元格</b>（第 1 列是序号），且表内正文（如 <c>FeishuToolContractGuards</c>）
/// 也含反引号。故只取"整格恰好是一个 snake_case 点分工具名"的单元格
/// （正则锚定整格，天然排除表头与正文里的其它反引号片段）。
/// </para>
/// <para>
/// 解析面（WP2 更新）：工具名列 + <b>RequiredScopes 列</b>。scope 列此前刻意不解析
/// （精确值由守卫内嵌的 24 行手抄字典锁定）——手抄字典已删除（WP2 / R-B 根因），
/// 改为<b>契约 vs 文档交叉验证</b>：对照表每行的 scope 列必须与
/// <see cref="FeishuToolContracts"/>（生成器发射的类型化契约）逐一相等。
/// </para>
/// </remarks>
public class PermissionMappingDocContractGuards
{
    private const string DocRelativePath = FeishuDocs.ToolPermissionTable;

    /// <summary>整格匹配"形如 <c>a.b.c</c> 的工具名"（锚定整格，故表头/正文片段不会被误取）。</summary>
    private static readonly Regex ToolNameCell = new(
        @"^`(?<name>[a-z][a-z0-9_]*(?:\.[a-z][a-z0-9_]*)+)`$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>scope 单元格内的反引号 token（scope 名形如 <c>im:message:send_as_bot</c>）。</summary>
    private static readonly Regex ScopeToken = new(
        @"`(?<scope>[a-z][a-z0-9_.:\-]*:[a-z0-9_.:\-]+)`",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>对照表列出的工具名集合必须与 <c>FeishuToolNames.All</c> 精确相等（漂移即红）。</summary>
    [Fact]
    public void PermissionDoc_ShouldCoverExactlyAllContractTools()
    {
        var docPath = Path.Combine(FindRepositoryRoot(), DocRelativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(docPath).Should().BeTrue($"《工具权限对照表》必须存在：{DocRelativePath}");

        var documented = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(docPath))
        {
            if (!line.StartsWith('|'))
            {
                continue;
            }

            var cells = line.Split('|');
            if (cells.Length < 3)
            {
                continue;
            }

            // cells[0] 是行首空串，cells[1] 是序号列，cells[2] 是工具名列。
            var match = ToolNameCell.Match(cells[2].Trim());
            if (match.Success)
            {
                documented.Add(match.Groups["name"].Value);
            }
        }

        documented.Should().NotBeEmpty(
            "解析结果为空说明文档表结构被改动（解析规则见本类注释）——不得降级为跳过");

        documented.Should().BeEquivalentTo(FeishuToolNames.All,
            "《工具权限对照表》必须与工具名契约表逐一对应：新增/删除工具时文档与守卫同批更新"
            + "（该声称此前不成立——对照表长期缺 contact 三工具）");
    }

    /// <summary>
    /// <b>BUG-1 守卫</b>：对照表<b>散文（prose）里的总数</b>必须等于契约表实际计数。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么需要它</b>：表格数据行早就靠 <see cref="PermissionDoc_ShouldCoverExactlyAllContractTools"/>
    /// 锁死了（182 行 = 182 工具），但文档头部/小节的<b>散文数字</b>长期漂移——
    /// 实际是"总数 163（88 只读 + 75 写类）"，而真值是 182（108 只读 + 74 写类）。
    /// 读者（宿主/评审者）会据此误判权限面规模，且这种漂移<b>不触发任何门禁</b>。
    /// </para>
    /// <para>
    /// 判据（纯文本扫描，不解析 markdown 结构）：
    /// <list type="bullet">
    /// <item>文中出现的 <c>全部 **N 个工具**（A 只读 + B 写类）</c> 形态必须与契约计数相等；</item>
    /// <item><c>合计 N</c> 形态的只读/写类小计必须与对应计数相等。</item>
    /// </list>
    /// 扫描面自证：一条都没匹配到时必须报红（防正则写坏后静默假绿）。
    /// </para>
    /// </remarks>
    [Fact]
    public void PermissionDoc_ProseTotals_ShouldMatchContractCounts()
    {
        var docPath = Path.Combine(FindRepositoryRoot(), DocRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var content = File.ReadAllText(docPath);

        var readOnly = FeishuToolNames.ReadonlyAll.Length;
        var write = FeishuToolNames.WriteAll.Length;
        var total = FeishuToolNames.All.Length;

        var examined = 0;
        var violations = new List<string>();

        // 形态 ①：全部 **182 个工具**（108 只读 + 74 写类）
        // 注：显式写全限定名——本文件同时 using 了 Moq（其 Match 静态类）与正则，裸 `Match` 会 CS0104。
        foreach (System.Text.RegularExpressions.Match match in ProseGrandTotal.Matches(content))
        {
            examined++;
            var parsed = (Total: int.Parse(match.Groups["total"].Value, CultureInfo.InvariantCulture),
                Read: int.Parse(match.Groups["read"].Value, CultureInfo.InvariantCulture),
                Write: int.Parse(match.Groups["write"].Value, CultureInfo.InvariantCulture));
            if (parsed.Total != total || parsed.Read != readOnly || parsed.Write != write)
            {
                violations.Add(
                    $"散文总数 {parsed.Total}（{parsed.Read} 只读 + {parsed.Write} 写类）"
                    + $"≠ 契约实测 {total}（{readOnly} 只读 + {write} 写类）");
            }
        }

        // 形态 ②：合计 108 / 合计 74（只读表 / 写类表的小节标题）
        foreach (System.Text.RegularExpressions.Match match in ProseSectionTotal.Matches(content))
        {
            examined++;
            var label = match.Groups["label"].Value;
            var value = int.Parse(match.Groups["count"].Value, CultureInfo.InvariantCulture);
            var expected = label.Contains("只读", StringComparison.Ordinal) ? readOnly : write;
            if (value != expected)
            {
                violations.Add($"小节合计「{label}」= {value}，契约实测 = {expected}");
            }
        }

        examined.Should().BeGreaterThan(0,
            "对照表散文总数扫描一条都没命中——正则失效（假绿）。本守卫至少要覆盖「全部 N 个工具（A 只读 + B 写类）」与两个小节合计");

        violations.Should().BeEmpty(
            "对照表散文总数必须与编译期契约计数一致（BUG-1：表格行早已锁定，散文数字长期漂移）：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>散文「全部 **N 个工具**（A 只读 + B 写类）」形态。</summary>
    private static readonly Regex ProseGrandTotal = new(
        @"全部\s*\*\*(?<total>\d+)\s*个工具\*\*\s*（(?<read>\d+)\s*只读\s*\+\s*(?<write>\d+)\s*写类）",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>小节合计「…，合计 N）」形态（只读表 / 写类表标题）。</summary>
    private static readonly Regex ProseSectionTotal = new(
        @"#+\s*(?<label>只读工具|写类工具)[^\r\n]*?合计\s*(?<count>\d+)\s*）",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// 契约 vs 文档交叉验证（WP2）：对照表每行的 RequiredScopes 列必须与
    /// <see cref="FeishuToolContracts"/>（生成器发射的类型化契约）逐一相等。
    /// </summary>
    /// <remarks>
    /// 取代原 <c>FeishuToolContractGuards</c> 中的 24 行手抄期望字典——scope 的代码侧真相
    /// 只有生成器一处，文档侧由本守卫锁"人读视图与契约不漂移"。
    /// </remarks>
    [Fact]
    public void PermissionDoc_RequiredScopesColumn_ShouldMatchToolContracts()
    {
        var docPath = Path.Combine(FindRepositoryRoot(), DocRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var documentedScopes = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var line in File.ReadAllLines(docPath))
        {
            if (!line.StartsWith('|'))
            {
                continue;
            }

            var cells = line.Split('|');
            if (cells.Length < 4)
            {
                continue;
            }

            var nameMatch = ToolNameCell.Match(cells[2].Trim());
            if (!nameMatch.Success)
            {
                continue;
            }

            var scopes = ScopeToken.Matches(cells[3])
                .Select(static m => m.Groups["scope"].Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static s => s, StringComparer.Ordinal)
                .ToArray();

            documentedScopes[nameMatch.Groups["name"].Value] = scopes;
        }

        foreach (var (toolName, contract) in FeishuToolContracts.ByToolName)
        {
            documentedScopes.Should().ContainKey(toolName, $"对照表缺工具 {toolName} 的行（工具名守卫应先行拦截）");
            var documented = documentedScopes[toolName]
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static s => s, StringComparer.Ordinal)
                .ToArray();

            documented.Should().BeEquivalentTo(contract.RequiredScopes,
                $"工具 {toolName} 的对照表 scope 列必须与编译期契约一致（漂移 = 文档过期或契约被改）");
        }
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
