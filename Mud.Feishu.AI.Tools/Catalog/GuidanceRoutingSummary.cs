// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Catalog;

/// <summary>
/// <b>BUG-4 / 方案 §7.4</b>：把域 L1 guidance（全文）推导为<b>一行路由摘要</b>——
/// 供 <c>GuidanceMode=routing</c> 时下发到 instructions。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是"推导"而不是"再写一份摘要资产"</b>：两份同源文本必然漂移（改全文忘改摘要，或反之），
/// 而摘要的作用恰恰是"给模型正确的路由起点"。此处从<b>同一份 L1 常量</b>取两个稳定片段：
/// <list type="number">
/// <item>H1 标题（域职责的自述，如 <c># 消息（im.*）</c>）；</item>
/// <item>「路由优先级」小节的首条（"遇到这类问题该走哪个域/哪个工具"，取首个句号为止）。</item>
/// </list>
/// 全文与摘要<b>同源同 pass</b>（都来自生成器发射的 <c>FeishuToolGuidance.ByDomain</c>），
/// 不存在"摘要说 A、全文说 B"的形态。
/// </para>
/// <para>
/// <b>失败方式必须响亮</b>：取不到标题时回落到域名字面量（绝不产出空串——空摘要会让模型
/// 以为"这个域没有引导"，比不给摘要更糟）；摘要长度硬上界 <see cref="MaxSummaryLength"/> 字符，
/// 超出按句读截断（不是硬切字节，避免半个字/半句话）。
/// </para>
/// <para>
/// <b>为什么 ≤256 字符/域</b>：28 域 × 256 ≈ 7 KB + 宿主指令，<b>恒定可控</b>、不随域数线性增长
/// （全文形态 25 域已实测 ≈25.2 KB / 预算 32.8 KB，域扩容必触顶）。
/// </para>
/// <para>
/// <b>为什么是公开面（M-3 联动）</b>：进程内 Agent（<c>FeishuToolAIFunction.GetGuidance</c>）与
/// MCP 协议层（<c>FeishuMcpToolServer</c>）都要产出同一形态的摘要——两者分别位于不同程序集，
/// 若各自实现一份，就出现了"同一份资产两种摘要"的第二真相源（正是本仓反复禁止的形态）。
/// 故推导函数公开，两条链共用<b>同一个实现</b>（各自的开关 <c>GuidanceMode</c> /
/// <c>InstructionsMode</c> 只决定"用摘要还是全文"）。
/// </para>
/// </remarks>
public static class GuidanceRoutingSummary
{
    /// <summary>单域摘要字符上界（方案 §7.4 的硬预算；守卫断言）。</summary>
    public const int MaxSummaryLength = 256;

    /// <summary>摘要正文（标题 + 路由首条）的字符上界——余量留给固定后缀。</summary>
    private const int MaxBodyLength = 150;

    /// <summary>
    /// 固定后缀：告诉模型"全文在哪"（进程内与 MCP 两条链各一条指引）。
    /// </summary>
    /// <remarks>
    /// 两条指引同时给出是有意的：<b>同一份摘要服务两条链</b>（不造第二真相源）——
    /// 进程内有 <c>feishu.guidance_read</c>，MCP 客户端有 <c>resources/read</c>；
    /// 各自用得上哪条就用哪条，另一条只是无害的一行提示。
    /// </remarks>
    /// <param name="domain">域名。</param>
    private static string RetrievalHint(string domain)
        => $"全文与深层避坑见 feishu.guidance_read({domain}, 主题)；"
            + $"MCP 客户端可 resources/read feishu-guidance://{domain}";

    /// <summary>
    /// 生成域路由摘要。
    /// </summary>
    /// <param name="domain">域名（工具名首个 <c>.</c> 之前的部分）。</param>
    /// <param name="fullContent">该域 L1 全文（生成器发射的常量）。</param>
    /// <returns>一行摘要（非空；≤ <see cref="MaxSummaryLength"/> 字符）。</returns>
    public static string Create(string domain, string fullContent)
    {
        var title = ExtractTitle(domain, fullContent);
        var route = ExtractFirstRouteItem(fullContent);

        var body = route.Length > 0
            ? title + "｜" + route
            : title;

        if (body.Length > MaxBodyLength)
        {
            body = TruncateAtSentenceBoundary(body, MaxBodyLength);
        }

        var summary = $"【{domain}】{body} → {RetrievalHint(domain)}";

        // 双保险：即使标题异常长，也必须落在硬预算内（守卫断言 ≤ MaxSummaryLength）。
        return summary.Length <= MaxSummaryLength
            ? summary
            : TruncateAtSentenceBoundary(summary, MaxSummaryLength);
    }

    /// <summary>取 H1 标题（<c># 消息（im.*）</c> → <c>消息（im.*）</c>）；缺失时回落域名（绝不产空串）。</summary>
    private static string ExtractTitle(string domain, string fullContent)
    {
        foreach (var rawLine in SplitLines(fullContent))
        {
            var line = rawLine.Trim();
            if (line.Length > 1 && line[0] == '#')
            {
                var text = line.TrimStart('#').Trim();
                if (text.Length > 0)
                {
                    return text;
                }
            }
        }

        return domain;
    }

    /// <summary>
    /// 取「路由优先级」小节的首条项目（到首个句号为止）；该小节缺失时返回空串
    /// （此时摘要退化为"标题 + 全文指引"，仍可用）。
    /// </summary>
    private static string ExtractFirstRouteItem(string fullContent)
    {
        var inRouteSection = false;
        foreach (var rawLine in SplitLines(fullContent))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("##", StringComparison.Ordinal))
            {
                // 注：netstandard2.0 没有 Contains(string, StringComparison)（netstandard2.1 才加），用 IndexOf 重载。
                inRouteSection = line.IndexOf("路由优先级", StringComparison.Ordinal) >= 0;
                continue;
            }

            if (!inRouteSection || line.Length == 0)
            {
                continue;
            }

            if (line[0] is '-' or '*')
            {
                var item = line.TrimStart('-', '*').Trim();
                if (item.Length == 0)
                {
                    continue;
                }

                // 句子边界优先（摘要里不要出现半句话）。
                // 注：netstandard2.0 没有 IndexOf(char, StringComparison)，用字符串重载。
                var stop = item.IndexOf("。", StringComparison.Ordinal);
                return stop > 0 ? item.Substring(0, stop) : item;
            }
        }

        return string.Empty;
    }

    /// <summary>按标点/空白边界截断（避免半个字或半句话），截断处补省略号。</summary>
    private static string TruncateAtSentenceBoundary(string text, int maxLength)
    {
        var window = text.Substring(0, maxLength);
        var cut = -1;

        foreach (var boundary in new[] { '；', '，', '、', ' ', '｜' })
        {
            var index = window.LastIndexOf(boundary);
            if (index > cut)
            {
                cut = index;
            }
        }

        return cut > maxLength / 2
            ? window.Substring(0, cut) + "…"
            : window.Substring(0, maxLength - 1) + "…";
    }

    /// <summary>按行切分（兼容 CRLF / LF / CR——生成器常量与磁盘文件的行尾形态可能不同）。</summary>
    private static string[] SplitLines(string text)
        => text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
}
