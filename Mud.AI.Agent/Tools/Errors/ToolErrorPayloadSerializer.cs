// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Mud.AI.Agent.Tools.Errors;

/// <summary>
/// B2 错误契约：将 <see cref="ToolError"/> 序列化为紧凑 JSON 首行（≤ 512 字节）。
/// </summary>
/// <remarks>
/// <para>
/// 手工拼接 JSON（不使用 <c>JsonSerializer</c> 反射重载——AOT 安全，对齐 IL2026/IL3050 = 0 纪律）。
/// 载荷字段为闭集枚举 + 数字 + 短字符串，无凭据面，出站净化不作用于载荷本身。
/// </para>
/// <para>
/// <b>截断保护</b>：载荷 ≤ 512 字节且置于首行，正文截断不得吃掉载荷（守卫断言）。
/// </para>
/// <para>
/// <b>BUG-8（恢复字段的预算优先级）</b>：<c>suggestions</c> 是自由文本、长度不可控，
/// 若它把载荷推过 512 上界，则<b>先丢 suggestions、再丢 trace</b>——
/// <c>category</c>/<c>subtype</c>/<c>retryable</c> 这三个"模型可分支的最小集"恒被保留。
/// 该取舍是<b>单向的</b>：不得为了塞进建议而牺牲分类字段（分类没了，建议也无从解读）。
/// </para>
/// </remarks>
public static class ToolErrorPayloadSerializer
{
    /// <summary>载荷最大字节上界（守卫断言基线）。</summary>
    public const int MaxPayloadBytes = 512;

    /// <summary>建议条数上界（超出部分丢弃；恢复候选只需"起手几步"）。</summary>
    public const int MaxSuggestionCount = 3;

    /// <summary>单条建议字符上界（超出截断；保持载荷有界）。</summary>
    public const int MaxSuggestionLength = 96;

    /// <summary>E-1：权限清单条数上界（scope 是短标识符，条数上界即长度上界）。</summary>
    public const int MaxScopeCount = 6;

    /// <summary>
    /// 将 <see cref="ToolError"/> 序列化为紧凑 JSON 行（无换行符）。
    /// </summary>
    /// <remarks>
    /// <b>降级顺序（E-1 扩展，单向不可逆）</b>：完整 → 丢 <c>suggestions</c> → 丢 <c>required_scopes</c>
    /// → 丢 <c>trace</c>。三级降级都不触碰 <c>category</c>/<c>subtype</c>/<c>retryable</c>——
    /// 它们是"模型可分支的最小集"（没有分类，建议与权限清单也无从解读）。
    /// </remarks>
    public static string Serialize(ToolError error)
    {
        var full = SerializeCore(error, includeSuggestions: true, includeRequiredScopes: true);
        if (full.Length <= MaxPayloadBytes)
        {
            return full;
        }

        // 上界保护（BUG-8）：先丢 suggestions（次要恢复信息），保留权限事实与 trace。
        var withoutSuggestions = SerializeCore(error, includeSuggestions: false, includeRequiredScopes: true);
        if (withoutSuggestions.Length <= MaxPayloadBytes)
        {
            return withoutSuggestions;
        }

        // E-1：再丢 required_scopes（权限清单比 trace 更容易从宿主配置侧复原）。
        var coreOnly = SerializeCore(error, includeSuggestions: false, includeRequiredScopes: false);
        if (coreOnly.Length <= MaxPayloadBytes)
        {
            return coreOnly;
        }

        // 仍超界 ⇒ trace 是最后一个无界字段，去掉它。
        return SerializeCore(
            error with { Trace = null }, includeSuggestions: false, includeRequiredScopes: false);
    }

    private static string SerializeCore(ToolError error, bool includeSuggestions, bool includeRequiredScopes)
    {
        // 手工拼接 JSON：字段闭集、值短，不需要转义（category/subtype/tool/trace 均为 ASCII 闭集或白名单化的短串）。
        var sb = new StringBuilder(256);
        sb.Append('{');
        sb.Append("\"tool_error\":{");
        sb.Append("\"category\":\"").Append(error.Category).Append('"');
        sb.Append(",\"subtype\":\"").Append(error.Subtype).Append('"');
        sb.Append(",\"retryable\":").Append(error.Retryable ? "true" : "false");

        if (error.RetryAfterSeconds.HasValue)
        {
            sb.Append(",\"retry_after_seconds\":").Append(error.RetryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (error.ApiCode.HasValue)
        {
            sb.Append(",\"api_code\":").Append(error.ApiCode.Value.ToString(CultureInfo.InvariantCulture));
        }

        sb.Append(",\"attempts\":").Append(error.Attempts.ToString(CultureInfo.InvariantCulture));

        if (!string.IsNullOrEmpty(error.Trace))
        {
            sb.Append(",\"trace\":\"").Append(error.Trace).Append('"');
        }

        if (!string.IsNullOrEmpty(error.Tool))
        {
            sb.Append(",\"tool\":\"").Append(error.Tool).Append('"');
        }

        if (includeSuggestions && error.Suggestions is { Count: > 0 } suggestions)
        {
            AppendSuggestions(sb, suggestions);
        }

        // E-1：工具**声明**的所需权限（结构化事实投影；不是"平台已开通什么"的判断）。
        if (includeRequiredScopes && error.RequiredScopes is { Count: > 0 } scopes)
        {
            AppendStringArray(sb, "required_scopes", scopes, MaxScopeCount);
        }

        sb.Append("}}");
        return sb.ToString();
    }

    /// <summary>
    /// 追加 <c>suggestions</c> 数组（条数与单条长度均上界化）。
    /// </summary>
    private static void AppendSuggestions(StringBuilder sb, IReadOnlyList<string> suggestions)
        => AppendStringArray(sb, "suggestions", suggestions, MaxSuggestionCount);

    /// <summary>
    /// 追加字符串数组字段（条数与单条长度均上界化；<b>必然转义</b>）。
    /// </summary>
    /// <remarks>
    /// 与其它字段不同，建议与 scope 清单里的值都可能是<b>自由文本/外部输入</b>，
    /// 故必须经 <see cref="JsonEncodedText"/> 转义（引号/反斜杠/控制字符），不能沿用直拼。
    /// <c>suggestions</c>（BUG-8）与 <c>required_scopes</c>（E-1）共用本实现——两处口径不会漂移。
    /// </remarks>
    private static void AppendStringArray(
        StringBuilder sb, string key, IReadOnlyList<string> values, int maxCount)
    {
        var rendered = new List<string>(maxCount);

        foreach (var raw in values)
        {
            if (rendered.Count == maxCount)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var text = raw.Length > MaxSuggestionLength ? raw.Substring(0, MaxSuggestionLength) : raw;
            rendered.Add(JsonEncodedText.Encode(text).ToString());
        }

        if (rendered.Count == 0)
        {
            return;
        }

        sb.Append(",\"").Append(key).Append("\":[");
        for (var i = 0; i < rendered.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append('"').Append(rendered[i]).Append('"');
        }

        sb.Append(']');
    }
}
