// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 行级 unified diff（纯函数；无 IO、无反射，AOT 友好）。
/// </summary>
/// <remarks>
/// <para>
/// <b>有界输出</b>：差异文本按 <c>maxCharacters</c> 在行边界截断并置 <c>Truncated</c>；
/// 增删行数仍是**全量**统计（截断只影响可读文本，不影响事实）。
/// </para>
/// <para>
/// <b>规模退化而非失败</b>：LCS 的 DP 表按 <c>n×m</c> 计。两侧行数乘积超过
/// <see cref="MaxLcsCells"/> 时退化为「公共前缀/后缀 + 中间整块」的粗粒度差异——
/// 大文档仍能回答"是否一致、增删多少行"，而不是把内存交给输入规模。
/// </para>
/// <para>
/// <b>换行归一</b>：CRLF 与 LF 视为同一内容；末尾单个空行不计入差异（Markdown 常见形态差异，非内容变化）。
/// </para>
/// </remarks>
internal static class MarkdownLineDiff
{
    /// <summary>LCS 表规模上限（单元格数；1e6 ≈ 4 MB DP 表）。</summary>
    internal const int MaxLcsCells = 1_000_000;

    /// <summary>统一差异的上下文行数（与 git 默认一致）。</summary>
    internal const int ContextLines = 3;

    /// <summary>计算差异。</summary>
    /// <param name="left">左侧正文（可为 null ⇒ 空）。</param>
    /// <param name="right">右侧正文（可为 null ⇒ 空）。</param>
    /// <param name="maxCharacters">差异文本预算（超出即在行边界截断）。</param>
    internal static MarkdownDiffResult Compute(string? left, string? right, int maxCharacters)
    {
        var leftLines = SplitLines(left);
        var rightLines = SplitLines(right);

        if (leftLines.AsSpan().SequenceEqual(rightLines))
        {
            return new MarkdownDiffResult(false, 0, 0, string.Empty, false, 0, false);
        }

        var budget = Math.Max(256, maxCharacters);

        if ((long)leftLines.Length * rightLines.Length > MaxLcsCells)
        {
            return CoarseDiff(leftLines, rightLines, budget);
        }

        return ExactDiff(leftLines, rightLines, budget);
    }

    /// <summary>按行拆分并归一换行/末尾空行。</summary>
    private static string[] SplitLines(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');
        if (lines.Length > 1 && lines[^1].Length == 0)
        {
            // netstandard2.0 没有 RuntimeHelpers.GetSubArray，不能用数组切片 lines[..^1]。
            var trimmed = new string[lines.Length - 1];
            Array.Copy(lines, trimmed, trimmed.Length);
            return trimmed;
        }

        return lines;
    }

    /// <summary>精确 LCS 差异。</summary>
    private static MarkdownDiffResult ExactDiff(string[] left, string[] right, int budget)
    {
        var n = left.Length;
        var m = right.Length;
        var dp = new int[n + 1, m + 1];

        for (var i = n - 1; i >= 0; i--)
        {
            for (var j = m - 1; j >= 0; j--)
            {
                dp[i, j] = string.Equals(left[i], right[j], StringComparison.Ordinal)
                    ? dp[i + 1, j + 1] + 1
                    : Math.Max(dp[i + 1, j], dp[i, j + 1]);
            }
        }

        var ops = new List<DiffOp>(n + m);
        var x = 0;
        var y = 0;
        while (x < n && y < m)
        {
            if (string.Equals(left[x], right[y], StringComparison.Ordinal))
            {
                ops.Add(new DiffOp(DiffKind.Equal, left[x]));
                x++;
                y++;
            }
            else if (dp[x + 1, y] >= dp[x, y + 1])
            {
                ops.Add(new DiffOp(DiffKind.Remove, left[x]));
                x++;
            }
            else
            {
                ops.Add(new DiffOp(DiffKind.Add, right[y]));
                y++;
            }
        }

        while (x < n)
        {
            ops.Add(new DiffOp(DiffKind.Remove, left[x++]));
        }

        while (y < m)
        {
            ops.Add(new DiffOp(DiffKind.Add, right[y++]));
        }

        var added = ops.Count(static op => op.Kind == DiffKind.Add);
        var removed = ops.Count(static op => op.Kind == DiffKind.Remove);
        var text = Render(ops, budget, out var truncated, out var totalLength);

        return new MarkdownDiffResult(true, added, removed, text, truncated, totalLength, false);
    }

    /// <summary>粗粒度差异：公共前缀/后缀之外的中间整块（大文档的退化路径）。</summary>
    private static MarkdownDiffResult CoarseDiff(string[] left, string[] right, int budget)
    {
        var prefix = 0;
        while (prefix < left.Length && prefix < right.Length
            && string.Equals(left[prefix], right[prefix], StringComparison.Ordinal))
        {
            prefix++;
        }

        var suffix = 0;
        while (suffix < left.Length - prefix && suffix < right.Length - prefix
            && string.Equals(left[^(suffix + 1)], right[^(suffix + 1)], StringComparison.Ordinal))
        {
            suffix++;
        }

        var leftMiddle = left.AsSpan(prefix, left.Length - prefix - suffix);
        var rightMiddle = right.AsSpan(prefix, right.Length - prefix - suffix);

        var ops = new List<DiffOp>();
        var contextStart = Math.Max(0, prefix - ContextLines);
        for (var index = contextStart; index < prefix; index++)
        {
            ops.Add(new DiffOp(DiffKind.Equal, left[index]));
        }

        foreach (var line in leftMiddle)
        {
            ops.Add(new DiffOp(DiffKind.Remove, line));
        }

        foreach (var line in rightMiddle)
        {
            ops.Add(new DiffOp(DiffKind.Add, line));
        }

        var contextEnd = Math.Min(left.Length, left.Length - suffix + ContextLines);
        for (var index = left.Length - suffix; index < contextEnd; index++)
        {
            ops.Add(new DiffOp(DiffKind.Equal, left[index]));
        }

        var added = rightMiddle.Length;
        var removed = leftMiddle.Length;
        var text = Render(ops, budget, out var truncated, out var totalLength);

        return new MarkdownDiffResult(true, added, removed, text, truncated, totalLength, true);
    }

    /// <summary>把操作序列渲染成 unified diff（按 hunk 合并 + 上下文）。</summary>
    private static string Render(List<DiffOp> ops, int budget, out bool truncated, out int totalLength)
    {
        var builder = new StringBuilder();
        truncated = false;
        totalLength = 0;

        // 预计算每个位置之前「旧侧已消费行数」与「新侧已消费行数」，供 hunk 头使用。
        var oldBefore = new int[ops.Count + 1];
        var newBefore = new int[ops.Count + 1];
        for (var index = 0; index < ops.Count; index++)
        {
            oldBefore[index + 1] = oldBefore[index] + (ops[index].Kind == DiffKind.Add ? 0 : 1);
            newBefore[index + 1] = newBefore[index] + (ops[index].Kind == DiffKind.Remove ? 0 : 1);
        }

        var cursor = 0;
        while (cursor < ops.Count)
        {
            // 跳过不变行，直到下一个变化点前 ContextLines 行。
            var nextChange = cursor;
            while (nextChange < ops.Count && ops[nextChange].Kind == DiffKind.Equal)
            {
                nextChange++;
            }

            if (nextChange >= ops.Count)
            {
                break;
            }

            var hunkStart = Math.Max(cursor, nextChange - ContextLines);
            var hunkEnd = nextChange;
            while (hunkEnd < ops.Count)
            {
                if (ops[hunkEnd].Kind != DiffKind.Equal)
                {
                    hunkEnd++;
                    continue;
                }

                // 连续不变行超过 2×上下文 ⇒ 当前 hunk 结束。
                var runEnd = hunkEnd;
                while (runEnd < ops.Count && ops[runEnd].Kind == DiffKind.Equal)
                {
                    runEnd++;
                }

                if (runEnd >= ops.Count || runEnd - hunkEnd > ContextLines * 2)
                {
                    hunkEnd = Math.Min(runEnd, hunkEnd + ContextLines);
                    break;
                }

                hunkEnd = runEnd;
            }

            var oldCount = oldBefore[hunkEnd] - oldBefore[hunkStart];
            var newCount = newBefore[hunkEnd] - newBefore[hunkStart];
            Append(builder, $"@@ -{oldBefore[hunkStart] + 1},{oldCount} +{newBefore[hunkStart] + 1},{newCount} @@\n", ref truncated, ref totalLength, budget);

            for (var index = hunkStart; index < hunkEnd; index++)
            {
                var prefix = ops[index].Kind switch
                {
                    DiffKind.Add => "+",
                    DiffKind.Remove => "-",
                    _ => " ",
                };
                Append(builder, prefix + ops[index].Text + "\n", ref truncated, ref totalLength, budget);
            }

            cursor = hunkEnd;
        }

        return builder.ToString();
    }

    /// <summary>追加一行；超出预算即停止追加但继续累计总长度（行边界截断）。</summary>
    private static void Append(StringBuilder builder, string chunk, ref bool truncated, ref int totalLength, int budget)
    {
        totalLength += chunk.Length;
        if (truncated || builder.Length + chunk.Length > budget)
        {
            truncated = true;
            return;
        }

        builder.Append(chunk);
    }

    private enum DiffKind
    {
        Equal,
        Remove,
        Add,
    }

    /// <summary>单条差异操作。</summary>
    private readonly record struct DiffOp(DiffKind Kind, string Text);
}

/// <summary>行级差异结果。</summary>
/// <param name="Changed">两侧是否不同。</param>
/// <param name="AddedLines">右侧新增行数（全量）。</param>
/// <param name="RemovedLines">左侧删除行数（全量）。</param>
/// <param name="Text">unified diff 文本（可能被 <paramref name="Truncated"/> 截断）。</param>
/// <param name="Truncated">差异文本是否超出预算。</param>
/// <param name="TotalLength">未截断时差异文本的总长度。</param>
/// <param name="Coarse">是否走了大文档的粗粒度路径。</param>
internal readonly record struct MarkdownDiffResult(
    bool Changed,
    int AddedLines,
    int RemovedLines,
    string Text,
    bool Truncated,
    int TotalLength,
    bool Coarse);
