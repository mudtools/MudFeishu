// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.CapabilityDemo.Common;

internal enum NoticeLevel
{
    Info,
    Warn,
    Error,
}

/// <summary>控制台渲染（无第三方 UI 依赖；重定向输出时自动去色，便于 CI 断言）。</summary>
internal static class DemoConsole
{
    private static bool ColorEnabled => !Console.IsOutputRedirected;

    public static void Banner(string title, IReadOnlyDictionary<string, string?> facts)
    {
        Write(ConsoleColor.Cyan, $"=== {title} ===");
        foreach (var (key, value) in facts)
        {
            Write(ConsoleColor.DarkGray, $"  {key}：");
            Console.WriteLine(value ?? "（空）");
        }

        Console.WriteLine();
    }

    public static void Section(string title)
    {
        Console.WriteLine();
        Write(ConsoleColor.Cyan, $"── {title} ──");
    }

    public static void Line(string text = "") => Console.WriteLine(text);

    public static void Notice(NoticeLevel level, string message)
    {
        var (color, prefix) = level switch
        {
            NoticeLevel.Warn => (ConsoleColor.Yellow, "[warn]"),
            NoticeLevel.Error => (ConsoleColor.Red, "[error]"),
            _ => (ConsoleColor.Gray, "[info]"),
        };
        Write(color, $"{prefix} {message}");
    }

    public static void Agent(string? text)
    {
        Write(ConsoleColor.Green, "Agent:");
        Console.WriteLine(string.IsNullOrWhiteSpace(text) ? "（本轮没有文本输出——写工具可能停在审批处）" : text);
    }

    public static void ToolCard(
        string toolName, string risk, string decision, long durationMs, string? argsDigest, string? reason)
    {
        var color = decision switch
        {
            "allowed" => ConsoleColor.Green,
            "denied" => ConsoleColor.Yellow,
            _ => ConsoleColor.Red,
        };
        Write(ConsoleColor.DarkGray, "  ↳ ");
        Write(color, $"{decision,-7}");
        Console.Write($" {toolName} [{risk}] {durationMs}ms");
        if (!string.IsNullOrEmpty(argsDigest))
        {
            Console.Write($" args={argsDigest}");
        }

        if (!string.IsNullOrEmpty(reason))
        {
            Console.Write($" → {reason}");
        }

        Console.WriteLine();
    }

    public static void ApprovalCard(
        string correlationId, string toolName, string? argsDigest, IReadOnlyList<string> scopes, DateTimeOffset expiresAt)
    {
        Console.WriteLine();
        Write(ConsoleColor.Magenta, "🔒 需要人工确认");
        Console.WriteLine($"  {correlationId}  {toolName}");
        Console.WriteLine($"     参数摘要：{argsDigest ?? "-"}");
        Console.WriteLine($"     权限点：{(scopes.Count == 0 ? "-" : string.Join(",", scopes))}");
        Console.WriteLine($"     过期：{expiresAt.ToLocalTime():HH:mm:ss}（默认 10 分钟）");
        Write(ConsoleColor.DarkGray, "     工具尚未执行。下一步：/approve " + correlationId + " 或 /deny " + correlationId);
        Console.WriteLine();
    }

    public static void Table(IReadOnlyList<string> headers, IReadOnlyList<string[]> rows)
    {
        if (headers.Count == 0)
        {
            return;
        }

        var widths = new int[headers.Count];
        for (var i = 0; i < headers.Count; i++)
        {
            widths[i] = headers[i].Length;
        }

        foreach (var row in rows)
        {
            for (var i = 0; i < widths.Length && i < row.Length; i++)
            {
                widths[i] = Math.Max(widths[i], Cell(row[i]).Length);
            }
        }

        Console.WriteLine(string.Join(" | ", headers.Select((h, i) => Pad(h, widths[i]))));
        Console.WriteLine(string.Join("-+-", widths.Select(w => new string('-', w))));
        foreach (var row in rows)
        {
            Console.WriteLine(string.Join(" | ", row.Select((c, i) => Pad(Cell(c), widths[Math.Min(i, widths.Length - 1)]))));
        }
    }

    public static void AuditTable(IReadOnlyList<ToolExecutionAuditRecord> records)
    {
        Section($"审计（{records.Count} 条）");
        if (records.Count == 0)
        {
            Line("（空）");
            return;
        }

        Table(
            ["#", "工具", "风险", "判定", "appKey", "耗时", "原因"],
            records.Select((r, i) => new[]
            {
                (i + 1).ToString(CultureInfo.InvariantCulture),
                r.ToolName,
                r.Risk,
                r.Decision,
                r.AppKey,
                $"{r.DurationMs}ms",
                r.Reason ?? "-",
            }).ToArray());
    }

    public static void Prompt() => Write(ConsoleColor.White, "> ");

    public static void Divider() => Console.WriteLine(new string('-', 72));

    private static string Cell(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "-";
        }

        return value.Length <= 42 ? value : value[..39] + "...";
    }

    private static string Pad(string value, int width)
        => value.Length >= width ? value : value + new string(' ', width - value.Length);

    private static void Write(ConsoleColor color, string text)
    {
        if (!ColorEnabled)
        {
            Console.Write(text);
            return;
        }

        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }
}
