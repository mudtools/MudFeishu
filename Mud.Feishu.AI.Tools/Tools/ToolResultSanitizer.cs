// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.FeishuTools.Tools;

/// <summary>
/// 出站净化阶段（工具结果 → 模型上下文）：剥离控制字符与 ANSI 转义、脱敏通信凭据与手机号。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是强制阶段而非钩子</b>：净化保护的是"模型上下文"这一<b>不可撤销</b>的出口——
/// 凭据/手机号一旦进入上下文就无法召回（可能被模型复述、被上游日志留存、被跨租户串用）。
/// 因此本阶段由 <c>FeishuToolBinding</c> 无条件调用，<b>不提供关闭开关</b>，也不能被宿主替换
/// （需要额外整形请用既有的 <c>IToolResultShaper</c>，它在净化<b>之后</b>执行）。
/// </para>
/// <para>
/// <b>误伤防护（脱敏最大的风险，方案 §8.2 R4）</b>——三类"看起来敏感但业务必需"的内容被有意保留：
/// </para>
/// <list type="number">
/// <item>
/// <b>电子邮箱不脱敏</b>：邮箱是飞书平台的<b>寻址货币</b>——
/// <c>im.send_message</c> 的 <c>receive_id_type=email</c>、通讯录按邮箱查人、
/// 邮件域收件人，全靠它。企业邮箱属通讯录信息（已由租户 scope 授权可见），
/// 脱掉它会让一整类"按邮箱找人或发消息"的多步流程直接失效。
/// </item>
/// <item>
/// <b>标识类字段不脱敏</b>：<c>open_id</c>/<c>chat_id</c>/<c>app_token</c>/<c>page_token</c>/
/// <c>document_id</c>/<c>node_token</c>… 是多步工具调用链的必需凭据。
/// 故凭据脱敏只针对<b>完整键名精确匹配</b>（<c>app_secret</c>/<c>access_token</c>/…），
/// 不含 <c>*_token</c> 通配。
/// </item>
/// <item>
/// <b>手机号脱敏会牺牲"按手机号找人"</b>：这一取舍是有意的——手机号属个人隐私，
/// 而按手机号寻址可由邮箱或 <c>open_id</c> 替代（见本方案 §8.2 R4 的处置说明）。
/// </item>
/// </list>
/// <para>匹配一律带边界断言，不会命中更长数字串或标识内部，也不改变 JSON 结构。</para>
/// </remarks>
internal static class ToolResultSanitizer
{
    /// <summary>脱敏掩码。</summary>
    public const string Mask = "***";

    /// <summary>需要脱敏的凭据类 JSON 属性名（R3-05：单源到 SecurityTextPrimitives.CredentialKeys）。</summary>
    private static readonly string[] SecretKeys = SecurityTextPrimitives.CredentialKeys;

    /// <summary>ANSI 转义序列（CSI/OSC）：终端控制，模型上下文里没有任何正当用途。</summary>
    private static readonly Regex AnsiEscape = new(
        @"\x1B\[[0-9;?]*[ -/]*[@-~]|\x1B\][^\x07\x1B]*(?:\x07|\x1B\\)|\x1B[@-Z\\-_]",
        RegexOptions.Compiled,
        SecurityTextPrimitives.MatchTimeout);

    /// <summary>凭据键 → 值脱敏（R3-05：单源到 SecurityTextPrimitives.SecretValuesJson）。</summary>
    private static readonly Regex SecretValues = SecurityTextPrimitives.SecretValuesJson;

    /// <summary>凭据键 → 值脱敏（非 JSON 形态：key=value / key: value；R3-05 新增）。</summary>
    private static readonly Regex SecretValuesFlat = SecurityTextPrimitives.SecretValuesFlat;

    /// <summary>中国大陆手机号（带边界断言，避免命中更长的标识/数字串）。</summary>
    private static readonly Regex ChinaMobile = new(
        @"(?<![\dA-Za-z])1[3-9]\d{9}(?![\dA-Za-z])",
        RegexOptions.Compiled,
        SecurityTextPrimitives.MatchTimeout);

    /// <summary>
    /// 净化工具结果文本。
    /// </summary>
    /// <param name="text">下游执行器回填的原始文本（可能为 null/空）。</param>
    /// <returns>净化后的文本；输入为空时返回空串。</returns>
    public static string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // 顺序不可颠倒：ANSI 转义以 ESC(0x1B) 开头，若先剥控制字符就只剩 "[31m" 这类残渣
        // （实测：先剥控制字符会把 ANSI 序列切成无害但可见的乱码，达不到净化目的）。
        var sanitized = AnsiEscape.Replace(text!, string.Empty);
        sanitized = StripControlCharacters(sanitized);
        // R3-05：JSON 形态 + 非 JSON 形态脱敏（补齐 api_key/client_id 等此前漏网的键）。
        sanitized = SecretValues.Replace(sanitized, m => "\"" + m.Groups["key"].Value + "\":\"" + Mask + "\"");
        sanitized = SecretValuesFlat.Replace(sanitized, m => m.Groups["key"].Value + "=" + Mask);
        sanitized = ChinaMobile.Replace(sanitized, Mask);
        return sanitized;
    }

    /// <summary>
    /// 剥离 C0/C1 控制字符（保留 <c>\n</c> / <c>\r</c> / <c>\t</c>）。
    /// </summary>
    /// <remarks>
    /// 控制字符对模型无信息价值，却可被用来伪造工具输出的视觉结构（提示注入面的常见载体）。
    /// </remarks>
    public static string StripControlCharacters(string text)
    {
        var hasControl = false;
        foreach (var ch in text)
        {
            if (IsStrippedControl(ch))
            {
                hasControl = true;
                break;
            }
        }

        if (!hasControl)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (!IsStrippedControl(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private static bool IsStrippedControl(char ch)
        => SecurityTextPrimitives.IsControl(ch);
}
