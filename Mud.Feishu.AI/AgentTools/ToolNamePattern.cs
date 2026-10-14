// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.AgentTools;

/// <summary>
/// <b>工具名 glob 匹配器</b>（宿主策略配置 <c>FeishuAgent:ToolPolicy</c> 的词汇与判定）。
/// </summary>
/// <remarks>
/// <b>语法（闭集，刻意小）</b>：模式 = 点分<b>段</b>；段内只允许小写字母、数字、下划线与 <c>*</c>（<c>*</c> 匹配段内
/// <b>任意长度</b>（含空）的字符序列，<b>不跨段</b>）；模式与工具名的<b>段数必须相同</b>；唯一例外是单段模式 <c>*</c>（等价"全部工具"）。
/// <para>
/// <b>为什么不支持任意正则/跨段通配</b>：策略面是宿主写错代价很高的配置（写错即"全拒"），词汇越小越容易在 <c>Validate()</c> 期
/// 做<b>完全</b>校验（<see cref="IsLegal"/> 是纯函数，可被测试穷举）；正则表达能力在"工具名"这个只有两段的域上收益几乎为零。
/// </para>
/// <para>
/// <b>职责边界</b>：本类型只负责<b>匹配</b>；拒绝语义（<c>reason_code</c>）在 <c>FeishuToolBinding</c> 的策略轴，
/// 该轴同官方一致为「工具名模式 + 域」双轨。
/// </para>
/// <para>
/// <b>合法</b>：<c>bitable.*</c>（域）、<c>im.get_*</c>（前缀）、<c>drive.transfer_owner</c>（精确）、
/// <c>*.delete_*</c>（跨域危险动词）、<c>*</c>（全部）；<b>非法</b>（<see cref="IsLegal"/> 为 false）：空串、含空段
/// （<c>a..b</c>、<c>.a</c>、<c>a.</c>）、大写或非法字符（<c>Bitable.*</c>、<c>bitable.*-x</c>）、段内连续星号（<c>a.**</c>）。
/// </para>
/// </remarks>
internal static class ToolNamePattern
{
    /// <summary>段内合法字符：小写字母、数字、下划线、星号。</summary>
    private static bool IsLegalSegmentChar(char ch)
        => (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_' || ch == '*';

    /// <summary>
    /// 模式是否合法（<b>fail-closed 的唯一判据</b>：装配期 <c>Validate()</c> 用它与宿主配置对账）。
    /// </summary>
    /// <param name="pattern">模式字面量（可空）。</param>
    /// <returns>合法返回 <see langword="true"/>。</returns>
    public static bool IsLegal(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        var segments = pattern.Split('.');
        foreach (var segment in segments)
        {
            if (segment.Length == 0)
            {
                // 空段（".a" / "a." / "a..b"）会让匹配语义变得不可读，一律拒绝。
                return false;
            }

            var previousWasStar = false;
            foreach (var ch in segment)
            {
                if (!IsLegalSegmentChar(ch))
                {
                    return false;
                }

                if (ch == '*')
                {
                    if (previousWasStar)
                    {
                        // 段内连续星号与单星等价（`a.**` ≡ `a.*`）——保留会让宿主以为它们不同。
                        return false;
                    }

                    previousWasStar = true;
                }
                else
                {
                    previousWasStar = false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 判断工具名是否命中模式。
    /// </summary>
    /// <param name="pattern">模式（调用方须先经 <see cref="IsLegal"/> 校验；非法模式此处返回 false）。</param>
    /// <param name="toolName">契约工具名（<c>域.动作</c> 形态）。</param>
    /// <returns>命中返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// <b>非法模式返回 <c>false</c>（而非抛）</b>：匹配器是热路径上的纯函数，<b>抛异常会让一次
    /// 配置笔误变成运行期 500</b>；正确形态是"装配期 fail-fast（<c>Validate()</c> 拒绝非法模式）
    /// + 运行期按不命中处理"——后者与 fail-closed 一致：非法 Deny 不命中 ⇒ 不额外拒绝，
    /// 而真正的安全仍由 <c>Tools</c>/<c>WriteAllowList</c> 与授权门禁承担。
    /// </remarks>
    public static bool IsMatch(string? pattern, string? toolName)
    {
        if (!IsLegal(pattern) || string.IsNullOrWhiteSpace(toolName))
        {
            return false;
        }

        // 单段模式 `*`：等价"全部工具"（唯一例外，见类型 remarks）。
        if (pattern!.Length == 1)
        {
            return true;
        }

        var patternSegments = pattern.Split('.');
        var toolSegments = toolName!.Split('.');
        if (patternSegments.Length != toolSegments.Length)
        {
            return false;
        }

        for (var i = 0; i < patternSegments.Length; i++)
        {
            if (!SegmentMatches(patternSegments[i], toolSegments[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>段内匹配：<c>*</c> 匹配任意长度（含空）字符序列；无 <c>*</c> 时要求完全相等。</summary>
    /// <remarks>
    /// 朴素贪心回溯：段长度很小（工具名段通常 &lt; 32 字符），不需要 KMP 之类的优化；
    /// 此处<b>刻意</b>用可读实现换掉正则（正则的转义/回溯语义正是策略面最容易踩的坑）。
    /// </remarks>
    private static bool SegmentMatches(string patternSegment, string valueSegment)
    {
        if (patternSegment.IndexOf('*') < 0)
        {
            return string.Equals(patternSegment, valueSegment, StringComparison.Ordinal);
        }

        var patternIndex = 0;
        var valueIndex = 0;
        var starIndex = -1;
        var backtrackValueIndex = 0;

        while (valueIndex < valueSegment.Length)
        {
            if (patternIndex < patternSegment.Length
                && patternSegment[patternIndex] != '*'
                && patternSegment[patternIndex] == valueSegment[valueIndex])
            {
                patternIndex++;
                valueIndex++;
                continue;
            }

            if (patternIndex < patternSegment.Length && patternSegment[patternIndex] == '*')
            {
                starIndex = patternIndex;
                backtrackValueIndex = valueIndex;
                patternIndex++;
                continue;
            }

            if (starIndex >= 0)
            {
                patternIndex = starIndex + 1;
                backtrackValueIndex++;
                valueIndex = backtrackValueIndex;
                continue;
            }

            return false;
        }

        while (patternIndex < patternSegment.Length && patternSegment[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == patternSegment.Length;
    }
}
