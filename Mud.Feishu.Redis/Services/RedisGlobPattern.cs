// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// Redis glob pattern 的字面量转义。
/// </summary>
/// <remarks>
/// <para>
/// Redis 的 <c>SCAN MATCH</c>（<c>Keys(pattern:)</c>）使用 <c>stringmatchlen</c> 语义：
/// <c>\</c> 转义后继字符；<c>*</c> / <c>?</c> / <c>[</c> / <c>]</c> 为通配元字符。
/// </para>
/// <para>
/// 因此由 <c>TokenKeyBuilder</c> 规范化后的<b>字面量</b>前缀（其中 <c>\</c> 已用于转义 <c>:</c>，
/// 见 <c>TokenKeyBuilder.NormalizeSegment</c>）必须<b>再转义一次</b>才能作为 pattern 的字面量部分：
/// 键中真实存在的<b>两个</b>连续反斜杠在 glob 中写作 <c>\\\\</c>，否则 pattern 会要求
/// 键上只有<b>一个</b>反斜杠，永不命中。
/// </para>
/// <para>
/// <b>不得重复转义</b>：<c>TokenKeyBuilder</c> 已把 <c>:</c> 转义为 <c>\:</c>，若调用方
/// （如 <c>PerAppRedisTokenStoreFactory.BuildKeyPrefix</c>）再转义一次（<c>\</c> → <c>\\</c>），
/// 则 SCAN pattern 与键将由<b>不同</b>份前缀产出而互不匹配 —— 含 <c>:</c>/<c>\</c> 的 appKey 下
/// <c>ClearAsync</c> / <c>GetTokenTypesAsync</c> / <c>ClearAllUsersAsync</c> 会全部零命中。
/// </para>
/// </remarks>
internal static class RedisGlobPattern
{
    /// <summary>
    /// 把字面量片段转为 Redis glob 字面量（通配元字符与转义符全部转义）。
    /// </summary>
    /// <param name="literal">已规范化的字面量片段（如 <c>feishu:app\:with\:colon:token:</c>）。</param>
    /// <returns>可直接拼接 <c>*</c> 的 glob 字面量。</returns>
    public static string EscapeLiteral(string? literal)
    {
        if (string.IsNullOrEmpty(literal))
            return string.Empty;

        var sb = new StringBuilder(literal!.Length + 8);
        foreach (var c in literal)
        {
            switch (c)
            {
                // '\' 必须自转义；'*'/'?'/'['/']' 是 stringmatchlen 的通配元字符。
                case '\\':
                case '*':
                case '?':
                case '[':
                case ']':
                    sb.Append('\\');
                    break;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// 把「已规范化的字面量前缀」转成可直接用于 <c>Keys(pattern:)</c> 的 pattern。
    /// </summary>
    /// <param name="literalPrefix">不含尾随通配符的字面量前缀。</param>
    /// <returns>转义后的字面量 + <c>*</c>。</returns>
    public static string FromLiteralPrefix(string? literalPrefix)
        => EscapeLiteral(literalPrefix) + "*";
}
