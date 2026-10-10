// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// G1（R2-09）：<b>静默 catch 必须显式留痕</b>——每个"既不上抛也不记日志"的 <c>catch</c> 块，
/// 必须带 <see cref="SilentCatchScanner.WhitelistMarker"/> 标记说明"为什么可以静默"。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是元守卫而不是补 3 条日志</b>（根因 R-C）：R2-06 修的 3 处空 <c>catch</c>
/// （<c>AttachmentTools</c>×2、<c>DocxSheetsDriveWriteTools</c>×1）是<b>当时想到的</b>位置；行为用例的覆盖面
/// 等于你想到的路径数。本守卫断言的是<b>结构</b>——"这类形态不允许无声存在"，因此
/// <b>新增的静默 catch 自动被覆盖</b>，不需要任何人记得回来补用例。
/// </para>
/// <para>
/// <b>例外白名单的形态</b>：不说"某文件某行可以静默"（行号会漂移），而要求<b>就地标注意图</b>——
/// 标记必须与代码在一起，读代码的人（以及下一轮的评审者）立刻能看到"这里是刻意吞掉的，理由是 X"。
/// 这比一张放在测试里的行号白名单更难腐化。
/// </para>
/// <para>
/// <b>自证报红（本守卫可信度的来源）</b>：<see cref="Scanner_ShouldReportViolation_ForUnmarkedSilentCatch"/>
/// 用一段<b>缺陷原始形态</b>的合成源码直接驱动同一个检查器并断言报红。这取代了"临时把生产代码改坏、
/// 跑一次、再改回来"的一次性自证——后者不会被留存，前者是永久的回归保护。
/// </para>
/// </remarks>
public class SilentCatchContractGuards
{
    private static readonly string[] ScannedProjects =
    [
        "Mud.Feishu.AI",
        "Mud.Feishu.AI.Tools",

        // R7 / C6b：MCP 包同属"协议面"，静默 catch 在这里的后果更重——
        // 吞掉异常会让客户端只看到"连接没响应"，而缺陷在服务端毫无痕迹。
        "Mud.Feishu.AI.Mcp",
    ];

    /// <summary>生产源码中不得存在"未标注理由的静默 catch"。</summary>
    [Fact]
    public void EverySilentCatch_ShouldDeclareItsIntent()
    {
        var violations = new List<string>();
        var catchCount = 0;

        foreach (var project in ScannedProjects)
        {
            foreach (var file in EnumerateSources(project))
            {
                var source = File.ReadAllText(file);
                catchCount += SilentCatchScanner.CountCatchBlocks(source);
                violations.AddRange(
                    SilentCatchScanner.FindViolations(Path.GetFileName(file), source)
                        .Select(violation => $"{project}/{violation}"));
            }
        }

        catchCount.Should().BeGreaterThan(0, "扫描必须命中真实源码（扫描面为空说明路径解析坏了，那是假绿）");
        violations.Should().BeEmpty(
            "以下 catch 块既不上抛也不留痕——它是「缺陷被静默吞掉」的形态。"
            + $"请补日志/诊断上报，或就地标注 `{SilentCatchScanner.WhitelistMarker}` 并写明理由："
            + string.Join(" | ", violations));
    }

    /// <summary>
    /// 检查器自证：对<b>缺陷原始形态</b>（无标注的静默 <c>catch</c>）必须报红。
    /// </summary>
    [Fact]
    public void Scanner_ShouldReportViolation_ForUnmarkedSilentCatch()
    {
        const string Defective = """
            internal sealed class Sampler
            {
                public void Cleanup()
                {
                    try { Do(); }
                    catch (Exception)
                    {
                    }
                }
            }
            """;

        SilentCatchScanner.FindViolations("Sampler.cs", Defective)
            .Should().NotBeEmpty("无标注的静默 catch 是缺陷原始形态——检查器对它必须报红，否则本守卫是假绿");
    }

    /// <summary>检查器自证：三种合规形态（上抛 / 留痕 / 显式标注）都不得报红。</summary>
    [Theory]
    [InlineData("""
        internal sealed class Sampler
        {
            public void A()
            {
                try { Do(); }
                catch (Exception) { throw; }
            }
        }
        """)]
    [InlineData("""
        internal sealed class Sampler
        {
            private readonly ILogger? _logger;
            public void B()
            {
                try { Do(); }
                catch (Exception ex) { _logger?.LogWarning(ex, "失败"); }
            }
        }
        """)]
    [InlineData("""
        internal sealed class Sampler
        {
            public void C()
            {
                try { Do(); }
                catch (Exception) { /* 有意静默（守卫白名单）：清理失败不改变业务语义 */ }
            }
        }
        """)]
    public void Scanner_ShouldNotReport_ForCompliantCatch(string source)
        => SilentCatchScanner.FindViolations("Sampler.cs", source).Should().BeEmpty();

    private static IEnumerable<string> EnumerateSources(string project)
    {
        var root = Path.Combine(FindRepositoryRoot(), project);
        Directory.Exists(root).Should().BeTrue($"{project} 目录必须存在（扫描面不得静默为空）");

        return Directory
            .GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(static path => !IsBuildOutput(path));
    }

    private static bool IsBuildOutput(string path)
        => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
           || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

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

/// <summary>
/// "静默 <c>catch</c>" 检查器（可被合成源码直接驱动，从而支持检查器自证）。
/// </summary>
/// <remarks>
/// <b>判定口径（刻意保守）</b>：一个 <c>catch</c> 块若在<b>块内</b>同时不含
/// <c>throw</c>（上抛/重抛）与任何 <c>Log</c>/<c>Report</c> 调用，即视为"静默"。
/// 保守之处在于**不解析语义**，只看文本形态——因此它不会漏报，但可能误报
/// （例如块内调用了自定义的 <c>SomethingReport</c>）；误报的处置是就地补标注，成本极低。
/// </remarks>
internal static class SilentCatchScanner
{
    /// <summary>例外白名单标记（必须与代码在一起，附理由）。</summary>
    public const string WhitelistMarker = "守卫白名单";

    /// <summary>统计源码中的 <c>catch</c> 块数量（用于断言扫描面非空，防假绿）。</summary>
    public static int CountCatchBlocks(string source) => EnumerateCatchBlocks(source).Count();

    /// <summary>找出未标注理由的静默 <c>catch</c> 块（返回"文件名:行号"形态的定位）。</summary>
    /// <param name="fileName">文件名（仅用于定位输出）。</param>
    /// <param name="source">源码全文。</param>
    /// <returns>违规定位列表。</returns>
    public static IReadOnlyList<string> FindViolations(string fileName, string source)
    {
        var violations = new List<string>();

        foreach (var (headerStart, blockEnd) in EnumerateCatchBlocks(source))
        {
            var bodyStart = source.IndexOf('{', headerStart);
            var body = source.Substring(bodyStart, blockEnd - bodyStart + 1);
            if (!IsSilent(body))
            {
                continue;
            }

            var header = source.Substring(headerStart, bodyStart - headerStart);
            if (header.Contains(WhitelistMarker, StringComparison.Ordinal)
                || body.Contains(WhitelistMarker, StringComparison.Ordinal))
            {
                continue;
            }

            violations.Add($"{fileName}:{LineOf(source, headerStart)}");
        }

        return violations;
    }

    /// <summary>块内既无上抛也无留痕调用 ⇒ 静默。</summary>
    private static bool IsSilent(string blockBody)
        => !blockBody.Contains("throw", StringComparison.Ordinal)
           && !blockBody.Contains("Log", StringComparison.Ordinal)
           && !blockBody.Contains("Report", StringComparison.Ordinal);

    /// <summary>枚举全部 <c>catch</c> 块（词边界 + 大括号配对；不依赖语法分析器）。</summary>
    private static IEnumerable<(int HeaderStart, int BlockEnd)> EnumerateCatchBlocks(string source)
    {
        var searchFrom = 0;
        while (searchFrom < source.Length)
        {
            var index = source.IndexOf("catch", searchFrom, StringComparison.Ordinal);
            if (index < 0)
            {
                yield break;
            }

            searchFrom = index + 5;

            // 词边界：排除标识符内部（如 SwiftCatchHandler）与注释/字符串里的偶然命中。
            if (index > 0 && (char.IsLetterOrDigit(source[index - 1]) || source[index - 1] == '_'))
            {
                continue;
            }

            if (IsInsideCommentOrString(source, index))
            {
                continue;
            }

            var bodyStart = source.IndexOf('{', index);
            if (bodyStart < 0)
            {
                yield break;
            }

            // 头与块之间不允许出现分号（防把 `catch { }` 之后的下一块当成自己的块体）。
            var header = source.Substring(index, bodyStart - index);
            if (header.Contains(';'))
            {
                continue;
            }

            var blockEnd = MatchBrace(source, bodyStart);
            if (blockEnd < 0)
            {
                yield break;
            }

            yield return (index, blockEnd);
            searchFrom = blockEnd + 1;
        }
    }

    /// <summary>匹配 <paramref name="openIndex"/> 处大括号的配对位置。</summary>
    private static int MatchBrace(string source, int openIndex)
    {
        var depth = 0;
        for (var i = openIndex; i < source.Length; i++)
        {
            var ch = source[i];
            if (ch == '{')
            {
                depth++;
            }
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>该位置是否落在行注释/块注释/字符串字面量内（粗判，用于排除文档注释里的 "catch"）。</summary>
    /// <remarks>同程序集的其它源码扫描守卫（如 <c>ReplyScopeScanner</c>）复用本状态机，避免复制。</remarks>
    internal static bool IsInsideCommentOrString(string source, int index)
    {
        var inLineComment = false;
        var inBlockComment = false;
        var inString = false;
        var inVerbatim = false;

        for (var i = 0; i < index; i++)
        {
            var ch = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';

            if (inLineComment)
            {
                if (ch == '\n')
                {
                    inLineComment = false;
                }

                continue;
            }

            if (inBlockComment)
            {
                if (ch == '*' && next == '/')
                {
                    inBlockComment = false;
                    i++;
                }

                continue;
            }

            if (inString)
            {
                if (inVerbatim)
                {
                    if (ch == '"' && next == '"')
                    {
                        i++;
                    }
                    else if (ch == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (ch == '\\')
                {
                    i++;
                }
                else if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '/' && next == '/')
            {
                inLineComment = true;
                i++;
            }
            else if (ch == '/' && next == '*')
            {
                inBlockComment = true;
                i++;
            }
            else if (ch == '@' && next == '"')
            {
                inString = true;
                inVerbatim = true;
                i++;
            }
            else if (ch == '"')
            {
                inString = true;
                inVerbatim = false;
            }
        }

        return inLineComment || inBlockComment || inString;
    }

    private static int LineOf(string source, int index)
    {
        var line = 1;
        for (var i = 0; i < index && i < source.Length; i++)
        {
            if (source[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }
}
