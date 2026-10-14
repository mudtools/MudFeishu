// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

// Moq 的全局 using 也导出 Match（匹配器工厂）⇒ 与正则匹配结果同名，须显式别名。
using RegexMatch = System.Text.RegularExpressions.Match;

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>单个源文件的注释度量。</summary>
/// <param name="Lines">文件行数。</param>
/// <param name="Comments">注释行数（<c>//</c> 起首的行，含 XML 文档行）。</param>
/// <param name="MaxBlock">最长连续注释块的行数。</param>
internal readonly record struct FileCommentStats(int Lines, int Comments, int MaxBlock);

/// <summary>
/// 注释文本扫描器：把「评审编号是否可解析」「过程叙述是否带文档链接」「注释块有多长」
/// 三个判据做成纯函数，从而可被合成源码直接驱动（自证用例的前提）。
/// </summary>
/// <remarks>
/// 只识别 <c>//</c> 形态的注释——本仓源码不使用块注释。
/// </remarks>
internal static class CommentConventionScanner
{
    /// <summary>新文件的最长注释块上限（叙述型长文的形态判据）。</summary>
    internal const int NewFileMaxBlock = 20;

    /// <summary>新文件的注释行数上限（超过即应拆分文件或把决策外移）。</summary>
    internal const int NewFileCommentBudget = 120;

    /// <summary>
    /// 评审编号闭集。判据是「编号必须能在受治理文档里被解析」，不是编号本身的对错。
    /// </summary>
    /// <remarks>
    /// 前后视 <c>(?&lt;![\w-])/(?![\w-])</c>：编号被别的标识符包住时（<c>xR7-4foo</c>）不算编号——
    /// 否则会把 <c>DP-R7-4</c> 这类决策记录号截成 <c>R7-4</c> 上报（诊断信息指向错的东西）。
    /// <c>DP-…</c> 单列一条，它同样是「必须可解析」的引用。
    /// </remarks>
    public static readonly Regex AnchorToken = new(
        @"(?<![\w-])(?:DP-[A-Z]+\d+-\d+|R\d+-\d+[a-z]?|P\dD-\d+[a-z]?|P\d+-\d+[a-z]?|R-\d+|B\d+\.\d+|AT-[A-Z]\d+|AI-FD-[A-Z]+\d+|T\d+ / D\d+)(?![\w-])",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// 过程叙述词表：描述「曾经怎样、哪一轮改的」而非「现在必须怎样」的措辞。
    /// </summary>
    /// <remarks>
    /// <c>已删除/已移除</c> 带排除：<c>任务已删除</c>、<c>引用消息可能已删除/无权限</c> 描述的是
    /// <b>运行期状态</b>（不是代码演进史），本轮清理实测的假红来源，故用前后视断言排除
    /// 「后接 或/、/」「前接 /」两种状态语境。墓碑式叙述（<c>已删除的同义入口 X</c>）仍会被命中。
    /// </remarks>
    public static readonly Regex Narration = new(
        "此前|原先|曾经|旧实现|方案初稿|本轮审查|上一版|(?<![/／])已(?:删除|移除)(?![或、/／])",
        RegexOptions.CultureInvariant);

    /// <summary>可解析的文档链接：仓库内文档路径，或 ADR 编号。</summary>
    public static readonly Regex DocumentLink = new(
        @"documents/[A-Za-z0-9_./\-]+\.(?:md|json)|ADR-\d+",
        RegexOptions.CultureInvariant);

    /// <summary>度量整份源码。</summary>
    /// <remarks>
    /// <c>Comments</c> 统计全部 <c>//</c> 起首行（含 XML 文档）；<c>MaxBlock</c> 只度量<b>叙述块</b>——
    /// 逐成员文档标签（<c>&lt;param&gt;</c> / <c>&lt;typeparam&gt;</c>）起首的行视为块分隔符。
    /// 理由：十个参数的公开方法/record 即使每项各写一行也必然超过块长上限，把逐参数文档算进长块
    /// 会逼迫作者删除合法文档或把多参数挤成一行；病态形态是「散文式长段」，逐参数文档不是。
    /// </remarks>
    /// <param name="source">源码全文。</param>
    /// <returns>注释度量。</returns>
    public static FileCommentStats Measure(string source)
    {
        var lines = SplitLines(source);
        var comments = 0;
        var maxBlock = 0;
        var run = 0;

        foreach (var line in lines)
        {
            if (!IsComment(line))
            {
                run = 0;
                continue;
            }

            comments++;

            if (IsMemberDocTag(line))
            {
                run = 0;
                continue;
            }

            run++;
            maxBlock = Math.Max(maxBlock, run);
        }

        return new FileCommentStats(lines.Length, comments, maxBlock);
    }

    /// <summary>找出注释里无法解析为受治理文档的评审编号（去重）。</summary>
    /// <param name="source">源码全文。</param>
    /// <param name="resolves">编号是否能在受治理文档里解析。</param>
    /// <returns>无法解析的编号。</returns>
    public static IReadOnlyList<string> FindUnresolvedAnchors(string source, Func<string, bool> resolves)
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var line in SplitLines(source))
        {
            if (!IsComment(line))
            {
                continue;
            }

            foreach (RegexMatch match in AnchorToken.Matches(line))
            {
                if (!resolves(match.Value))
                {
                    found.Add(match.Value);
                }
            }
        }

        return found.ToList();
    }

    /// <summary>
    /// 找出「叙述过程但没有给出可解析文档链接」的注释块（按块判，块内任一行有链接即视为已交代来源）。
    /// </summary>
    /// <param name="source">源码全文。</param>
    /// <param name="linkExists">链接指向的文档是否存在。</param>
    /// <returns>触发块的关键词（去重）。</returns>
    public static IReadOnlyList<string> FindUnlinkedNarration(string source, Func<string, bool> linkExists)
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);
        var lines = SplitLines(source);
        var block = new List<string>();

        for (var i = 0; i <= lines.Length; i++)
        {
            var inBlock = i < lines.Length && IsComment(lines[i]);
            if (inBlock)
            {
                block.Add(lines[i]);
                continue;
            }

            if (block.Count > 0)
            {
                CollectUnlinkedNarration(block, linkExists, found);
                block.Clear();
            }
        }

        return found.ToList();
    }

    /// <summary>按预算判据给出违规说明（冻结文件不得增长；新文件受硬上限）。</summary>
    /// <param name="relativePath">仓库相对路径。</param>
    /// <param name="stats">当前度量。</param>
    /// <param name="frozen">冻结度量（不在基线中时为 <see langword="null"/>）。</param>
    /// <returns>违规说明列表。</returns>
    public static IReadOnlyList<string> FindBudgetViolations(string relativePath, FileCommentStats stats, FileCommentStats? frozen)
    {
        var violations = new List<string>();

        if (frozen is null)
        {
            if (stats.MaxBlock > NewFileMaxBlock)
            {
                violations.Add($"{relativePath}: 新文件最长注释块 {stats.MaxBlock} 行 > {NewFileMaxBlock} 行");
            }

            if (stats.Comments > NewFileCommentBudget)
            {
                violations.Add($"{relativePath}: 新文件注释 {stats.Comments} 行 > {NewFileCommentBudget} 行");
            }

            return violations;
        }

        var slack = Math.Max(3, frozen.Value.Comments / 20);
        if (stats.Comments > frozen.Value.Comments + slack)
        {
            violations.Add(
                $"{relativePath}: 注释 {stats.Comments} 行 > 冻结值 {frozen.Value.Comments} + 余量 {slack}");
        }

        if (stats.MaxBlock > frozen.Value.MaxBlock)
        {
            violations.Add($"{relativePath}: 最长注释块 {stats.MaxBlock} 行 > 冻结值 {frozen.Value.MaxBlock}");
        }

        return violations;
    }

    private static void CollectUnlinkedNarration(
        IReadOnlyList<string> block,
        Func<string, bool> linkExists,
        SortedSet<string> found)
    {
        var keywords = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var line in block)
        {
            foreach (RegexMatch match in Narration.Matches(line))
            {
                keywords.Add(match.Value);
            }
        }

        if (keywords.Count == 0)
        {
            return;
        }

        foreach (RegexMatch link in DocumentLink.Matches(string.Join("\n", block)))
        {
            if (linkExists(link.Value))
            {
                return;
            }
        }

        foreach (var keyword in keywords)
        {
            found.Add(keyword);
        }
    }

    private static bool IsComment(string line)
        => line.TrimStart().StartsWith("//", StringComparison.Ordinal);

    private static bool IsMemberDocTag(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("/// <param", StringComparison.Ordinal)
               || trimmed.StartsWith("/// <typeparam", StringComparison.Ordinal);
    }

    private static string[] SplitLines(string source)
    {
        var lines = source.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimEnd('\r');
        }

        return lines.Length > 0 && lines[^1].Length == 0 ? lines[..^1] : lines;
    }
}
