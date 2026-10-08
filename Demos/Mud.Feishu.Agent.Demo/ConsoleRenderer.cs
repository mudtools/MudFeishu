// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.Agent.Demo;

/// <summary>提示级别（<see cref="ConsoleRenderer.Notice"/>）。</summary>
internal enum NoticeLevel
{
    /// <summary>信息。</summary>
    Info,

    /// <summary>告警（不中断）。</summary>
    Warn,

    /// <summary>错误。</summary>
    Error,
}

/// <summary>一次工具调用的可视化模型（数据源：<c>ToolExecutionAuditRecord</c>）。</summary>
/// <param name="ToolName">工具名。</param>
/// <param name="RiskLiteral">风险字面量（<c>read</c> / <c>write</c> / <c>high-risk-write</c>）。</param>
/// <param name="Decision">判定（<c>allowed</c> / <c>denied</c> / <c>error</c>）。</param>
/// <param name="DurationMs">耗时（毫秒）。</param>
/// <param name="ArgsDigest">入参摘要（SDK 已脱敏：参数名 + 值长度 + 敏感键掩码）。</param>
/// <param name="Reason">拒绝/错误原因（可空）。</param>
internal sealed record ToolCallViewModel(
    string ToolName,
    string RiskLiteral,
    string Decision,
    long DurationMs,
    string? ArgsDigest,
    string? Reason);

/// <summary>一条待人工确认的写操作（展示用视图）。</summary>
/// <param name="CorrelationId">面向用户的短关联号。</param>
/// <param name="RequestId">框架请求标识（续跑时必须原样带回）。</param>
/// <param name="ToolName">工具名。</param>
/// <param name="ArgumentsDigest">入参摘要（已脱敏）。</param>
/// <param name="RequiredScopes">工具声明的权限点。</param>
/// <param name="ExpiresAt">有效期截止（UTC）。</param>
internal sealed record PendingApprovalViewModel(
    string CorrelationId,
    string RequestId,
    string ToolName,
    string? ArgumentsDigest,
    IReadOnlyList<string> RequiredScopes,
    DateTimeOffset ExpiresAt);

/// <summary>
/// 终端渲染器：<b>所有终端输出的唯一出口</b>（其余类型不得直接 <c>Console.WriteLine</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>安全要求（必须实现）</b>：
/// <list type="number">
/// <item>
/// <b>ANSI 注入过滤</b>：来自模型/飞书文档的文本在写出前剔除 <c>\x1B</c>（及 <c>\x9B</c>）开头的转义序列。
/// 飞书文档正文是<b>不可信输入</b>：终端解释 <c>\x1B]0;…\x07</c> 可改窗口标题、<c>\x1B[2J</c> 可清屏。
/// </item>
/// <item><b>换行归一</b>：<c>\r\n</c> / <c>\r</c> → <c>\n</c>，避免流式增量把行覆盖搞乱。</item>
/// <item><b>宽度兜底</b>：<c>Console.WindowWidth</c> 在无终端（管道/重定向）时会抛，回退为 100。</item>
/// <item><b>无子进程、锁内无 await</b>：所有方法同步且不含 <c>await</c>（仓库异步规范）。</item>
/// </list>
/// </para>
/// <para>
/// <b>可测性</b>：输出目标经构造参数注入（缺省 <see cref="Console.Out"/>），单测用 <see cref="StringWriter"/>
/// 断言"ANSI 被剔除""多行缩进对齐"，无需劫持进程级 <c>Console.Out</c>。
/// </para>
/// </remarks>
internal sealed class ConsoleRenderer
{
    /// <summary>回答正文的行前缀（模型侧输出的可见标记）。</summary>
    private const string AgentMarker = "🤖 ";

    /// <summary>续行缩进：与 <see cref="AgentMarker"/> 的显示宽度对齐（emoji 占 2 列 + 空格 1 列）。</summary>
    private const string AgentIndent = "   ";

    private readonly TextWriter _output;
    private readonly TextWriter _error;
    private readonly bool _useColor;
    private readonly object _sync = new();

    /// <summary>当前是否位于行首（<see cref="AgentDelta"/> 续行缩进的状态位）。</summary>
    private bool _atLineStart = true;

    /// <summary>
    /// 初始化渲染器。
    /// </summary>
    /// <param name="output">标准输出目标（可空 = <see cref="Console.Out"/>）。</param>
    /// <param name="error">错误输出目标（可空 = <see cref="Console.Error"/>）。</param>
    public ConsoleRenderer(TextWriter? output = null, TextWriter? error = null)
    {
        _useColor = output is null;
        _output = output ?? Console.Out;
        _error = error ?? Console.Error;
    }

    /// <summary>工具结果是否全文展示（<c>/verbose</c> 切换）。</summary>
    public bool Verbose { get; set; }

    /// <summary>是否输出 <c>Trace</c> 行（对齐 SDK 埋点语义的轻量可观测面；默认开启，<c>/verbose</c> 只加"全文"维度）。</summary>
    public bool TraceEnabled { get; set; } = true;

    /// <summary>REPL 是否已请求退出（由 <c>/exit</c> 置位；渲染器只承载状态，不做流程控制）。</summary>
    public bool ExitRequested { get; private set; }

    /// <summary>请求退出 REPL。</summary>
    public void RequestExit() => ExitRequested = true;

    /// <summary>打印启动横幅（标题 + 事实键值对）。</summary>
    /// <param name="title">标题。</param>
    /// <param name="facts">事实清单（顺序即打印顺序）。</param>
    public void Banner(string title, IReadOnlyDictionary<string, string?> facts)
    {
        var width = Math.Max(SafeWidth(), 60);
        WriteLine(string.Empty);
        WriteLine(new string('═', width), ConsoleColor.DarkCyan);
        WriteLine(" " + title, ConsoleColor.Cyan);
        WriteLine(new string('═', width), ConsoleColor.DarkCyan);

        var keyWidth = facts.Count == 0 ? 0 : facts.Keys.Max(DisplayWidth);
        foreach (var (key, value) in facts)
        {
            // 键按**显示宽度**补齐（中文键占 2 列，用 string.PadRight 会错位）；
            // 值可能含来自环境变量的用户输入，写出前统一过滤 ANSI。
            var padded = key + new string(' ', Math.Max(0, keyWidth - DisplayWidth(key)));
            WriteLine($" {padded} : {StripAnsi(value ?? string.Empty)}");
        }

        WriteLine(new string('═', width), ConsoleColor.DarkCyan);
    }

    /// <summary>
    /// 打印输入提示符（不换行；由 REPL 在 <see cref="Console.ReadLine"/> 之前调用）。
    /// </summary>
    /// <param name="label">提示符文本。</param>
    public void InputPrompt(string label = "you › ")
    {
        lock (_sync)
        {
            WriteRaw(label, ConsoleColor.Cyan);
            _atLineStart = false;
        }
    }

    /// <summary>回显用户输入。</summary>
    /// <param name="text">用户原始输入。</param>
    public void UserEcho(string text)
    {
        // 用户输入可能被粘贴进含控制码的内容（从文档复制的文本），同样过滤。
        WriteLine($"you › {StripAnsi(text)}", ConsoleColor.Cyan);
    }

    /// <summary>打印回答起始标记。</summary>
    public void AgentPrefix()
    {
        lock (_sync)
        {
            WriteRaw(AgentMarker, ConsoleColor.Green);
            _atLineStart = false;
        }
    }

    /// <summary>写出流式增量（自动过滤 ANSI、归一换行、续行缩进对齐）。</summary>
    /// <param name="delta">模型增量文本。</param>
    public void AgentDelta(string delta)
    {
        if (string.IsNullOrEmpty(delta))
        {
            return;
        }

        var normalized = StripAnsi(NormalizeNewlines(delta));

        lock (_sync)
        {
            var segments = normalized.Split('\n');
            for (var i = 0; i < segments.Length; i++)
            {
                if (i > 0)
                {
                    WriteRaw(Environment.NewLine, null);
                    _atLineStart = true;
                }

                var segment = segments[i];
                if (segment.Length == 0)
                {
                    continue;
                }

                if (_atLineStart)
                {
                    WriteRaw(AgentIndent, null);
                }

                WriteRaw(segment, null);
                _atLineStart = false;
            }
        }
    }

    /// <summary>结束回答（必要时补换行）。</summary>
    public void AgentEnd()
    {
        lock (_sync)
        {
            if (!_atLineStart)
            {
                WriteRaw(Environment.NewLine, null);
            }

            _atLineStart = true;
        }
    }

    /// <summary>打印工具调用卡片。</summary>
    /// <param name="vm">工具调用视图。</param>
    public void ToolCallCard(ToolCallViewModel vm)
    {
        var (badge, color) = vm.RiskLiteral switch
        {
            "high-risk-write" => ("high-risk-write", ConsoleColor.Red),
            "write" => ("write", ConsoleColor.Yellow),
            _ => ("read", ConsoleColor.Green),
        };

        var mark = vm.Decision switch
        {
            "allowed" => "✓",
            "denied" => "✗",
            _ => "⚠",
        };

        lock (_sync)
        {
            WriteRaw("  ", null);
            WriteRaw("⚙ ", ConsoleColor.DarkGray);
            WriteRaw(vm.ToolName, ConsoleColor.White);
            WriteRaw($"  risk={badge}", color);
            WriteRaw($"  {mark} {vm.Decision}", vm.Decision == "allowed" ? ConsoleColor.Green : ConsoleColor.Red);
            WriteRaw($"  {vm.DurationMs}ms", ConsoleColor.DarkGray);
            WriteRaw(Environment.NewLine, null);
            _atLineStart = true;

            if (!string.IsNullOrWhiteSpace(vm.ArgsDigest))
            {
                WriteRaw($"    args: {StripAnsi(vm.ArgsDigest!)}", ConsoleColor.DarkGray);
                WriteRaw(Environment.NewLine, null);
            }

            if (!string.IsNullOrWhiteSpace(vm.Reason))
            {
                WriteRaw($"    reason: {StripAnsi(vm.Reason!)}", ConsoleColor.DarkGray);
                WriteRaw(Environment.NewLine, null);
            }
        }
    }

    /// <summary>打印工具结果提示（截断标记 / 拒绝原因）。</summary>
    /// <param name="toolName">工具名。</param>
    /// <param name="truncated">是否被截断。</param>
    /// <param name="reason">截断原因（可空）。</param>
    /// <param name="originalLength">原文长度（等价字符数）。</param>
    public void ToolResultHint(string toolName, bool truncated, string? reason, int originalLength)
    {
        if (!truncated)
        {
            return;
        }

        var detail = reason is null ? string.Empty : "，" + reason;
        Notice(
            NoticeLevel.Warn,
            $"{toolName} 结果已截断（原文 {originalLength} 字符{detail}）——"
            + "大文档读取必然截断，请分批读取或调整 MaxToolResultLength。");
    }

    /// <summary>打印待人工确认卡片。</summary>
    /// <param name="vm">待确认视图。</param>
    public void ApprovalCard(PendingApprovalViewModel vm)
    {
        lock (_sync)
        {
            WriteRaw("  ┌─ ", ConsoleColor.DarkYellow);
            WriteRaw("🔒 待人工确认", ConsoleColor.Yellow);
            WriteRaw($"  【{vm.CorrelationId}】", ConsoleColor.Yellow);
            WriteRaw(Environment.NewLine, null);
            WriteRaw($"  │ 工具      : {vm.ToolName}", null);
            WriteRaw(Environment.NewLine, null);
            WriteRaw($"  │ 参数摘要  : {StripAnsi(vm.ArgumentsDigest ?? "（无）")}", null);
            WriteRaw(Environment.NewLine, null);
            WriteRaw($"  │ 权限点    : {(vm.RequiredScopes.Count == 0 ? "（无）" : string.Join("、", vm.RequiredScopes))}", null);
            WriteRaw(Environment.NewLine, null);
            WriteRaw($"  │ request   : {vm.RequestId}", ConsoleColor.DarkGray);
            WriteRaw(Environment.NewLine, null);
            WriteRaw($"  │ 有效期至  : {vm.ExpiresAt.ToLocalTime():HH:mm:ss}", null);
            WriteRaw(Environment.NewLine, null);
            WriteRaw($"  └─ 输入 /approve {vm.CorrelationId} 放行；/deny {vm.CorrelationId} 拒绝；/pending 查看全部", ConsoleColor.DarkYellow);
            WriteRaw(Environment.NewLine, null);
            _atLineStart = true;
        }
    }

    /// <summary>打印提示。</summary>
    /// <param name="level">级别。</param>
    /// <param name="message">消息。</param>
    public void Notice(NoticeLevel level, string message)
    {
        var (mark, color, writer) = level switch
        {
            NoticeLevel.Warn => ("[!]", ConsoleColor.Yellow, _output),
            NoticeLevel.Error => ("[x]", ConsoleColor.Red, _error),
            _ => ("[i]", ConsoleColor.DarkGray, _output),
        };

        lock (_sync)
        {
            if (!_atLineStart)
            {
                WriteRaw(Environment.NewLine, null);
                _atLineStart = true;
            }

            WriteTo(writer, $"{mark} {StripAnsi(message)}{Environment.NewLine}", color);
        }
    }

    /// <summary>输出一条对齐 SDK 埋点语义的 Trace 行（<c>/verbose</c> 之外也可见）。</summary>
    /// <param name="message">Trace 文本。</param>
    public void Trace(string message)
    {
        if (!TraceEnabled)
        {
            return;
        }

        lock (_sync)
        {
            if (!_atLineStart)
            {
                WriteRaw(Environment.NewLine, null);
                _atLineStart = true;
            }

            WriteRaw($"  · {StripAnsi(message)}{Environment.NewLine}", ConsoleColor.DarkGray);
        }
    }

    /// <summary>
    /// 打印一段多行正文块（标题 + 边框 + 逐行原文）。
    /// </summary>
    /// <param name="title">块标题。</param>
    /// <param name="content">正文（可含换行；Markdown 原文按字面输出，不做解释）。</param>
    /// <remarks>
    /// <b>为什么不用 <see cref="Table"/></b>：表格把单元格当成单行文本，含换行的长文会把行结构撑破
    /// （guidance 是 Markdown 原文，必然多行且含 <c>|</c>），观感严重受损。
    /// </remarks>
    public void Block(string title, string? content)
    {
        var width = Math.Max(SafeWidth() - 2, 40);

        lock (_sync)
        {
            WriteRaw($"{StripAnsi(title)}{Environment.NewLine}", ConsoleColor.Cyan);
            WriteRaw("┌" + new string('─', width) + Environment.NewLine, ConsoleColor.DarkGray);

            foreach (var line in NormalizeNewlines(StripAnsi(content ?? string.Empty)).Split('\n'))
            {
                WriteRaw("│ " + line + Environment.NewLine, null);
            }

            WriteRaw("└" + new string('─', width) + Environment.NewLine, ConsoleColor.DarkGray);
            _atLineStart = true;
        }
    }

    /// <summary>打印表格（列宽按显示宽度计算，CJK 计 2 列）。</summary>
    /// <param name="title">表标题。</param>
    /// <param name="columns">列名。</param>
    /// <param name="rows">行数据。</param>
    public void Table(string title, IReadOnlyList<string> columns, IReadOnlyList<string[]> rows)
    {
        if (columns.Count == 0)
        {
            return;
        }

        var widths = new int[columns.Count];
        for (var c = 0; c < columns.Count; c++)
        {
            widths[c] = DisplayWidth(columns[c]);
        }

        foreach (var row in rows)
        {
            for (var c = 0; c < columns.Count && c < row.Length; c++)
            {
                widths[c] = Math.Max(widths[c], DisplayWidth(row[c]));
            }
        }

        lock (_sync)
        {
            WriteRaw($"{StripAnsi(title)}{Environment.NewLine}", ConsoleColor.Cyan);
            WriteRaw(BuildSeparator(widths), ConsoleColor.DarkGray);
            WriteRaw(BuildRow([.. columns], widths), null);
            WriteRaw(BuildSeparator(widths), ConsoleColor.DarkGray);

            foreach (var row in rows)
            {
                WriteRaw(BuildRow(row, widths), null);
            }

            _atLineStart = false;
        }
    }

    /// <summary>剔除 ANSI/VT 转义序列（含 <c>CSI</c>、<c>OSC</c> 与单字节 <c>\x9B</c> 形态）。</summary>
    /// <param name="text">原始文本（可空）。</param>
    /// <returns>可安全写入终端的文本。</returns>
    internal static string StripAnsi(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // 快路径：绝大多数文本不含 ESC，避免逐字符扫描。
        if (text.IndexOf('\u001B') < 0 && text.IndexOf('\u009B') < 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '\u009B')
            {
                i = SkipCsi(text, i + 1) - 1;
                continue;
            }

            if (ch != '\u001B')
            {
                builder.Append(ch);
                continue;
            }

            if (i + 1 >= text.Length)
            {
                break;
            }

            var next = text[i + 1];
            switch (next)
            {
                case '[':
                    i = SkipCsi(text, i + 2) - 1;
                    break;
                case ']':
                    i = SkipOsc(text, i + 2) - 1;
                    break;
                default:
                    // 单字符转义（ESC c / ESC 7 等）：连同 ESC 一起丢弃。
                    i++;
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>取终端可用宽度（无终端时回退 100）。</summary>
    /// <returns>列数。</returns>
    internal static int SafeWidth()
    {
        try
        {
            var width = Console.WindowWidth;
            return width > 0 ? width : 100;
        }
        catch (IOException)
        {
            return 100;
        }
        catch (PlatformNotSupportedException)
        {
            return 100;
        }
    }

    private static string NormalizeNewlines(string text)
        => text.IndexOf('\r') < 0 ? text : text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    /// <summary>跳过 CSI 序列（参数/中间字节 0x20..0x3F，终结字节 0x40..0x7E）。</summary>
    private static int SkipCsi(string text, int start)
    {
        var i = start;
        while (i < text.Length)
        {
            var ch = text[i];
            if (ch >= '\u0040' && ch <= '\u007E')
            {
                return i + 1;
            }

            i++;
        }

        return i;
    }

    /// <summary>跳过 OSC 序列（至 <c>BEL</c> 或 <c>ST</c>）。</summary>
    private static int SkipOsc(string text, int start)
    {
        var i = start;
        while (i < text.Length)
        {
            var ch = text[i];
            if (ch == '\u0007')
            {
                return i + 1;
            }

            if (ch == '\u001B' && i + 1 < text.Length && text[i + 1] == '\\')
            {
                return i + 2;
            }

            i++;
        }

        return i;
    }

    /// <summary>显示宽度（CJK / 全角字符计 2 列）。</summary>
    private static int DisplayWidth(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var width = 0;
        foreach (var ch in StripAnsi(text))
        {
            width += IsWide(ch) ? 2 : 1;
        }

        return width;
    }

    private static bool IsWide(char ch)
        => (ch >= '\u1100' && ch <= '\u115F')
        || (ch >= '\u2E80' && ch <= '\uA4CF')
        || (ch >= '\uAC00' && ch <= '\uD7A3')
        || (ch >= '\uF900' && ch <= '\uFAFF')
        || (ch >= '\uFE30' && ch <= '\uFE6F')
        || (ch >= '\uFF00' && ch <= '\uFF60')
        || (ch >= '\uFFE0' && ch <= '\uFFE6');

    private static string BuildSeparator(int[] widths)
        => "+" + string.Join("+", widths.Select(static w => new string('-', w + 2))) + "+" + Environment.NewLine;

    private static string BuildRow(string[] cells, int[] widths)
    {
        var builder = new StringBuilder("|");
        for (var c = 0; c < widths.Length; c++)
        {
            // 单元格内的换行必须折叠——否则内容会撑破行结构（多行正文请用 Block）。
            var cell = c < cells.Length
                ? NormalizeNewlines(StripAnsi(cells[c])).Replace('\n', ' ')
                : string.Empty;

            builder.Append(' ').Append(cell);
            builder.Append(' ', Math.Max(0, widths[c] - DisplayWidth(cell)) + 1);
            builder.Append('|');
        }

        return builder.Append(Environment.NewLine).ToString();
    }

    private void WriteLine(string text, ConsoleColor? color = null)
    {
        lock (_sync)
        {
            WriteRaw(text + Environment.NewLine, color);
            _atLineStart = true;
        }
    }

    private void WriteRaw(string text, ConsoleColor? color) => WriteTo(_output, text, color);

    private void WriteTo(TextWriter writer, string text, ConsoleColor? color)
    {
        try
        {
            if (color is null || !_useColor)
            {
                writer.Write(text);
                writer.Flush();
                return;
            }

            var previous = Console.ForegroundColor;
            Console.ForegroundColor = color.Value;
            writer.Write(text);
            writer.Flush();
            Console.ForegroundColor = previous;
        }
        catch (Exception)
        {
            // 输出侧失败**一律吞掉**（管道被关闭、终端被回收、无控制台等）：
            // 渲染是"展示面"，任何失败都不得中断模型流或工具执行链（降级矩阵 §10.2）。
            // 此处无日志通道（输出本身已坏），故为有意静默；观感上的表现是"停留上一次成功内容"。
        }
    }
}
