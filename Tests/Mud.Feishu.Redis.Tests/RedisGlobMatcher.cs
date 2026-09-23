// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.Tests;

/// <summary>
/// Redis <c>stringmatchlen</c> 语义的镜像（仅测试用），用于断言 SCAN pattern 是否命中键。
/// </summary>
/// <remarks>
/// <para>
/// 实现语法：<c>*</c>（任意串）、<c>?</c>（单字符）、<c>\</c>（转义后继字符为字面量）、
/// <c>[abc]</c> / <c>[a-z]</c> / <c>[^abc]</c>（字符集，含 <c>a-c</c> 范围）。
/// </para>
/// <para>
/// 关键语义（守卫 7c 的落点）：
/// <list type="bullet">
/// <item>pattern 中的 <c>\\</c> 匹配键上的<b>单个</b>反斜杠
/// ⇒ 键中真实的两个连续反斜杠必须在 pattern 中写作 <c>\\\\</c>；</item>
/// <item><c>[</c> 是字符集起始符（非字面量）⇒ 含 <c>[</c>/<c>]</c> 的 appKey 必须由
/// <c>RedisGlobPattern.EscapeLiteral</c> 转义后，pattern 才能字面量匹配。</item>
/// </list>
/// </para>
/// </remarks>
internal static class RedisGlobMatcher
{
    /// <summary>
    /// 按 Redis glob 语义判断 <paramref name="value"/> 是否匹配 <paramref name="pattern"/>。
    /// </summary>
    /// <param name="pattern">glob 模式（如 <c>feishu:cli:token:*</c>）。</param>
    /// <param name="value">待匹配的键。</param>
    /// <returns>匹配返回 true。</returns>
    public static bool IsMatch(string pattern, string value)
        => Match(pattern, 0, value, 0);

    private static bool Match(string pattern, int pi, string value, int vi)
    {
        while (pi < pattern.Length)
        {
            switch (pattern[pi])
            {
                case '*':
                    // 折叠连续 '*'（stringmatchlen 同语义）
                    while (pi + 1 < pattern.Length && pattern[pi + 1] == '*')
                        pi++;

                    if (pi + 1 == pattern.Length)
                        return true;

                    for (var i = vi; i <= value.Length; i++)
                    {
                        if (Match(pattern, pi + 1, value, i))
                            return true;
                    }

                    return false;

                case '?':
                    if (vi >= value.Length)
                        return false;
                    pi++;
                    vi++;
                    break;

                case '[':
                    if (vi >= value.Length || !MatchSet(pattern, ref pi, value[vi]))
                        return false;
                    vi++;
                    break;

                case '\\':
                    pi++;
                    if (pi >= pattern.Length)
                        return false; // 悬空转义符
                    goto default;

                default:
                    if (vi >= value.Length || value[vi] != pattern[pi])
                        return false;
                    pi++;
                    vi++;
                    break;
            }
        }

        return vi == value.Length;
    }

    /// <summary>
    /// 匹配字符集 <c>[...]</c>：<paramref name="pi"/> 指向 <c>'['</c>，返回后指向 <c>']'</c> 之后。
    /// </summary>
    private static bool MatchSet(string pattern, ref int pi, char value)
    {
        pi++; // 跳过 '['

        var negate = pi < pattern.Length && pattern[pi] == '^';
        if (negate)
            pi++;

        var matched = false;
        while (pi < pattern.Length && pattern[pi] != ']')
        {
            // 字符集内的转义字符
            if (pattern[pi] == '\\' && pi + 1 < pattern.Length)
            {
                pi++;
                if (value == pattern[pi])
                    matched = true;
                pi++;
                continue;
            }

            // 范围 a-c
            if (pi + 2 < pattern.Length && pattern[pi + 1] == '-' && pattern[pi + 2] != ']')
            {
                if (value >= pattern[pi] && value <= pattern[pi + 2])
                    matched = true;
                pi += 3;
                continue;
            }

            if (value == pattern[pi])
                matched = true;
            pi++;
        }

        if (pi < pattern.Length && pattern[pi] == ']')
            pi++;

        return negate ? !matched : matched;
    }
}
