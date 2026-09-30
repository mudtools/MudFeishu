// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Tools;

/// <summary>
/// 入站净化阶段（模型 → 工具参数，AT-B19）：拒绝携带控制字符 / 危险 Unicode 的参数值。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是"拒绝"而不是"剥离"</b>（R3 评审 D-6 拍板 ⓐ）：剥离会<b>静默改写用户内容</b>——
/// 模型发出去的文本与宿主/下游看到的不一致，属语义污染；拒绝让模型立刻知道自己被拒并可自愈
/// （去掉控制字符后重试）。这与 <see cref="ToolResultSanitizer"/>（出站）的"剥离"取法不同是<b>有意</b>的：
/// 出站是"下游真实数据 → 模型上下文"，剥离保留信息；入站是"模型意图 → 下游请求"，改写意图不可接受。
/// </para>
/// <para>
/// <b>与官方 CLI 的偏离（重要）</b>：官方 <c>internal/charcheck/charcheck.go</c> 直接拒绝
/// <c>U+200D</c>（零宽连接符）。本仓<b>不能照抄</b>——<c>U+200D</c> 是 Emoji ZWJ 序列的<b>合法组成</b>
/// （如 👨‍👩‍👧 家庭 emoji 由多个码位 + ZWJ 拼接），拒绝它会把正常的 emoji 输入打成错误。
/// 故本实现只拒绝<b>孤立</b>的零宽字符（其前后都不是 emoji 基码位时），Emoji ZWJ 序列完整放行
/// （见 <c>ToolArgumentSanitizerTests</c> 的正反用例矩阵）。
/// </para>
/// <para>
/// <b>保留 <c>\n</c> / <c>\r</c> / <c>\t</c></b>：<c>im.send_message</c> 的正文允许换行，Tab 亦然。
/// 但<b>独立的 <c>\r</c>（非 <c>\r\n</c>）被拒</b>——它是 HTTP header 注入（CRLF）的经典载体，
/// 而下游把参数拼进 multipart/header 的路径无法在类型层面排除。
/// </para>
/// <para>
/// <b>与授权器的顺序</b>：本阶段在 <c>FeishuToolBinding</c> 的①（上下文校验）之后、②（授权门禁）
/// <b>之前</b>执行——授权器可能把参数写入审计，审计必须看到已被净化的形态（不变量 A5）。
/// </para>
/// </remarks>
internal static class ToolArgumentSanitizer
{
    /// <summary>
    /// 单个参数值的长度上限（超限拒绝）。默认 20000 字符：远大于任何真实工具入参
    /// （最大的 <c>bitable.add_record.fields</c> 也在千字符量级），只用于拦住"模型把整篇文档塞进参数"的病态输入。
    /// </summary>
    public const int MaxArgumentValueLength = 20000;

    /// <summary>
    /// 校验全部参数（返回首个违规描述；全部合法返回 <see langword="null"/>）。
    /// </summary>
    /// <param name="arguments">模型 tool_call 入参。</param>
    /// <returns>违规描述（含参数名与原因），或 <see langword="null"/>。</returns>
    public static string? Validate(IReadOnlyDictionary<string, object?> arguments)
    {
        if (arguments is null)
        {
            return null;
        }

        foreach (var pair in arguments)
        {
            var failure = ValidateValue(pair.Key, pair.Value);
            if (failure is not null)
            {
                return failure;
            }
        }

        return null;
    }

    /// <summary>校验单个参数值（递归处理任意形态的文本值；数值/布尔/null 放行）。</summary>
    /// <param name="name">参数名（错误消息用）。</param>
    /// <param name="value">参数值。</param>
    /// <returns>违规描述，或 <see langword="null"/>。</returns>
    /// <remarks>
    /// <b>WP1 修复（S1）</b>：原先只识别 <c>string</c> 与 <c>IEnumerable&lt;string&gt;</c>，
    /// 不识别运行时真实形态 <c>JsonElement</c>——导致入站净化在主路径从未生效。
    /// 现经 <see cref="ToolArgumentNormalizer.EnumerateTexts"/> 统一遍历，覆盖所有 JSON 形态。
    /// </remarks>
    public static string? ValidateValue(string name, object? value)
    {
        foreach (var (path, text) in ToolArgumentNormalizer.EnumerateTexts(name, value))
        {
            var failure = ValidateText(path, text);
            if (failure is not null)
            {
                return failure;
            }
        }

        return null;
    }

    /// <summary>校验一段文本（长度 + 控制字符 + 危险 Unicode + 独立 CR）。</summary>
    /// <param name="name">参数名（错误消息用）。</param>
    /// <param name="text">文本。</param>
    /// <returns>违规描述，或 <see langword="null"/>。</returns>
    public static string? ValidateText(string name, string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (text!.Length > MaxArgumentValueLength)
        {
            return $"参数 {name} 长度 {text.Length.ToString(CultureInfo.InvariantCulture)} 超过上限 "
                + $"{MaxArgumentValueLength.ToString(CultureInfo.InvariantCulture)} 字符——请拆分后重试";
        }

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (ch == '\r')
            {
                // 允许 CRLF（Windows 换行是正常输入）；独立的 CR 是 header 注入载体，拒绝。
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    continue;
                }

                return $"参数 {name} 含独立回车符 (CR)——该字符可被用于请求头注入，请改用换行 (LF)";
            }

            if (IsForbiddenControl(ch))
            {
                return $"参数 {name} 含控制字符 U+{(int)ch:X4}——控制字符对模型无信息价值，"
                    + "却可被用来伪造工具输出的视觉结构，已拒绝（请移除后重试）";
            }

            if (IsForbiddenUnicode(ch, text, i))
            {
                return $"参数 {name} 含保留用途的不可见字符 U+{(int)ch:X4}"
                    + "（零宽/Bidi 控制/BOM）——该字符可用于隐藏文本内容，已拒绝（请移除后重试）";
            }
        }

        return null;
    }

    /// <summary>
    /// 校验<b>邮件头字段</b>值（R3-12）：拒绝任何 <c>CR</c>/<c>LF</c>——头字段换行是 SMTP 头注入的
    /// 经典载体（<c>Subject: "x\r\nBcc: attacker@evil"</c> 可凭空插入收件人）。
    /// </summary>
    /// <remarks>
    /// <b>为什么不改 <see cref="ValidateText"/></b>：全局入站净化<b>必须</b>允许 CRLF/LF
    /// （正文、文档内容、消息文本都靠换行表达结构），收紧会影响全部域；故换行禁忌只施加于
    /// 「会被逐行解析成头部的字段」这一小面，由调用方（邮件 EML 构造）显式使用本方法。
    /// </remarks>
    /// <param name="name">字段名（错误消息用）。</param>
    /// <param name="value">字段值。</param>
    /// <returns>违规描述，或 <see langword="null"/>。</returns>
    public static string? ValidateHeaderValue(string name, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        for (var i = 0; i < value!.Length; i++)
        {
            var ch = value[i];
            if (ch == '\r' || ch == '\n')
            {
                return $"参数 {name} 含换行符（{(ch == '\r' ? "CR" : "LF")}）——"
                    + "邮件头字段禁止换行（SMTP 头注入防护），换行请放在正文中";
            }
        }

        return null;
    }

    /// <summary>禁止的 C0/C1 控制字符（保留 <c>\n</c> / <c>\r</c> / <c>\t</c>；<c>\r</c> 由调用方单独处理）。</summary>
    /// <remarks>R3-05：判定表达式单源到 <see cref="SecurityTextPrimitives.IsControl"/>（出站剥离 / 入站拒绝共用同一事实）。</remarks>
    private static bool IsForbiddenControl(char ch)
        => SecurityTextPrimitives.IsControl(ch);

    /// <summary>
    /// 禁止的"危险 Unicode"：孤立零宽字符、Bidi 覆盖/嵌入/隔离、BOM 与各类不可见控制。
    /// </summary>
    /// <remarks>
    /// <b>Emoji 例外</b>：<c>U+200D</c>（ZWJ）与 <c>U+FE0F</c>（变体选择符）在 Emoji 序列中是合法组成，
    /// 仅当它们<b>孤立出现</b>（前后为普通空白/ASCII/边界）时才判为可疑。此处采用"邻近码位启发式"：
    /// 前一个码位或后一个码位属于 emoji 常见区段（或本身就是 ZWJ/变体选择符的同伴）即放行。
    /// 该启发式刻意保守——宁可放过也不误伤（误伤用户正文比漏检零宽的后果更严重，见 R-2）。
    /// </remarks>
    private static bool IsForbiddenUnicode(char ch, string text, int index)
        => ch switch
        {
            '\u200B' or '\u200C' or '\u2060' or '\uFEFF' => true,
            '\u200E' or '\u200F' or '\u061C' => true,
            '\u2028' or '\u2029' => true,
            '\u202A' or '\u202B' or '\u202C' or '\u202D' or '\u202E' => true,
            '\u2066' or '\u2067' or '\u2068' or '\u2069' => true,
            '\u200D' or '\uFE0F' => !IsEmojiAdjacent(text, index),
            _ => false,
        };

    /// <summary>
    /// 判断该位置是否处于 Emoji 序列内（前后码位之一属 emoji 常见区段）。
    /// </summary>
    private static bool IsEmojiAdjacent(string text, int index)
    {
        var before = index > 0 ? text[index - 1] : '\0';
        var after = index + 1 < text.Length ? text[index + 1] : '\0';

        return IsEmojiCodePoint(before) || IsEmojiCodePoint(after)
            || before == '\u200D' || after == '\u200D'
            || before == '\uFE0F' || after == '\uFE0F';
    }

    /// <summary>Emoji 常见码位区段（含代理对高位/低位——C# <c>char</c> 是 UTF-16 单元）。</summary>
    private static bool IsEmojiCodePoint(char ch)
        => ch is >= '\uD800' and <= '\uDFFF'   // 代理对（U+1F300+ 等 emoji 的实际载体）
        || ch is >= '\u2600' and <= '\u27BF'   // 杂项符号与装饰符号
        || ch is >= '\u2190' and <= '\u21FF'   // 箭头
        || ch is >= '\u2B00' and <= '\u2BFF'   // 杂项符号与箭头
        || ch is >= '\uFE00' and <= '\uFE0F'   // 变体选择符
        || ch is '\u2764' or '\u203C' or '\u2049' or '\u2122' or '\u2139';
}
