// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

    /// <summary>
    /// 令牌键构造的单一真相（D8 契约 / TMA2-02）。
    /// <para>
    /// 四类令牌键（租户 access/refresh、用户 access/refresh）由此类统一产出，
    /// Memory 与 Redis 两条后端路径逐字节一致。键段经 <see cref="NormalizeSegment"/> 统一转义，
    /// 含 <c>:</c> 的段不会与段间分隔符混淆。反向解析（<see cref="TryParseTenantTokenType"/>/
    /// <see cref="TryParseUserTokenType"/>）保证 <c>GetTokenTypesAsync</c> 返回值可原样回灌给
    /// <c>RemoveAsync</c>/<c>GetAccessTokenAsync</c>。
    /// </para>
    /// <para>
    /// TMF2-05：<see cref="BuildKeyPrefix"/> 是键前缀的唯一出口——
    /// Memory（<c>FeishuTokenStore</c> / <c>FeishuUserTokenStore</c>）和 Redis
    /// （<c>PerAppRedisTokenStoreFactory</c>）均委派此方法，消除裸拼接与经
    /// <c>RedisKeyBuilder.Combine</c> 转义的差异（前缀逐字节一致）。
    /// </para>
    /// <para>
    /// TMF2-08：<see cref="NormalizeSegment"/> 转义 glob 元字符（<c>*</c> <c>?</c> <c>[</c> <c>]</c>），
    /// 防止含这些字符的 appKey/userId 在 SCAN 模式中注入通配符。
    /// </para>
    /// </summary>
internal static class TokenKeyBuilder
{
    /// <summary>
    /// 段间分隔符。
    /// </summary>
    private const string Separator = ":";

    /// <summary>
    /// 转义后的分隔符。
    /// </summary>
    private const string EscapedSeparator = @"\:";

    /// <summary>
    /// 转义符自身。
    /// </summary>
    private const string EscapeChar = @"\";

    /// <summary>
    /// 转义后的转义符。
    /// </summary>
    private const string EscapedEscapeChar = @"\\";

    // TMF2-08：glob 元字符集——这些字符在 Redis SCAN pattern 中具有通配语义，
    // 必须在键段转义时一并处理，防止 appKey/userId 含这些字符时模式注入。
    private const char GlobStar = '*';
    private const char GlobQuestion = '?';
    private const char GlobBracketOpen = '[';
    private const char GlobBracketClose = ']';
    private const string EscapedStar = @"\*";
    private const string EscapedQuestion = @"\?";
    private const string EscapedBracketOpen = @"\[";
    private const string EscapedBracketClose = @"\]";

    /// <summary>
    /// 单段最大长度（字符），防止超长键 DoS。
    /// </summary>
    private const int MaxSegmentLength = 256;

    /// <summary>
    /// 构建租户级 access token 键。
    /// </summary>
    /// <param name="keyPrefix">键前缀（含 appKey 维度，如 <c>feishu:cli_a:token</c>）</param>
    /// <param name="tokenType">令牌类型标识符（如 <c>tenant:cli_a</c>）</param>
    /// <returns>规范化后的存储键</returns>
    internal static string TenantAccessKey(string keyPrefix, string tokenType)
        => Combine(keyPrefix, tokenType, "access");

    /// <summary>
    /// 构建租户级 refresh token 键。
    /// </summary>
    /// <param name="keyPrefix">键前缀（含 appKey 维度）</param>
    /// <param name="tokenType">令牌类型标识符</param>
    /// <returns>规范化后的存储键</returns>
    internal static string TenantRefreshKey(string keyPrefix, string tokenType)
        => Combine(keyPrefix, tokenType, "refresh");

    /// <summary>
    /// 构建用户级 access token 键。
    /// </summary>
    /// <param name="keyPrefix">键前缀（含 appKey 维度）</param>
    /// <param name="userId">用户唯一标识符</param>
    /// <param name="tokenType">令牌类型标识符</param>
    /// <returns>规范化后的存储键</returns>
    internal static string UserAccessKey(string keyPrefix, string userId, string tokenType)
        => Combine(keyPrefix, "user", userId, tokenType, "access");

    /// <summary>
    /// 构建用户级 refresh token 键。
    /// </summary>
    /// <param name="keyPrefix">键前缀（含 appKey 维度）</param>
    /// <param name="userId">用户唯一标识符</param>
    /// <param name="tokenType">令牌类型标识符</param>
    /// <returns>规范化后的存储键</returns>
    internal static string UserRefreshKey(string keyPrefix, string userId, string tokenType)
        => Combine(keyPrefix, "user", userId, tokenType, "refresh");

    /// <summary>
    /// 构建用于 SCAN 的租户键模式（通配 tokenType 与 access/refresh）。
    /// </summary>
    internal static string TenantScanPattern(string keyPrefix)
        => $"{NormalizePrefix(keyPrefix)}{Separator}*";

    /// <summary>
    /// 构建用于 SCAN 的用户键模式（通配 tokenType 与 access/refresh）。
    /// </summary>
    internal static string UserScanPattern(string keyPrefix, string userId)
        => $"{NormalizePrefix(keyPrefix)}{Separator}user{Separator}{NormalizeSegment(userId)}{Separator}*";

    /// <summary>
    /// 构建用于 SCAN 的全用户键模式（通配 userId、tokenType 与 access/refresh）。
    /// TMF-01：IFeishuUserTokenStorePurge.ClearAllUsersAsync 的键模式单一出口（D8 契约）。
    /// </summary>
    internal static string AllUsersScanPattern(string keyPrefix)
        => $"{NormalizePrefix(keyPrefix)}{Separator}user{Separator}*";

    /// <summary>
    /// 尝试从完整键中解析出租户级 tokenType。
    /// </summary>
    /// <param name="key">完整存储键</param>
    /// <param name="keyPrefix">键前缀（已规范化）</param>
    /// <param name="tokenType">解析出的原始（已反转义）tokenType</param>
    /// <returns>是否成功解析</returns>
    internal static bool TryParseTenantTokenType(string key, string keyPrefix, out string? tokenType)
    {
        tokenType = null;
        if (string.IsNullOrEmpty(key))
            return false;

        var normalizedPrefix = NormalizePrefix(keyPrefix);
        var prefixWithSep = normalizedPrefix + Separator;
        if (!key.StartsWith(prefixWithSep, StringComparison.Ordinal))
            return false;

        const string accessSuffix = Separator + "access";
        const string refreshSuffix = Separator + "refresh";

        string middle;
        if (key.EndsWith(accessSuffix, StringComparison.Ordinal))
        {
            middle = key.Substring(prefixWithSep.Length, key.Length - prefixWithSep.Length - accessSuffix.Length);
        }
        else if (key.EndsWith(refreshSuffix, StringComparison.Ordinal))
        {
            middle = key.Substring(prefixWithSep.Length, key.Length - prefixWithSep.Length - refreshSuffix.Length);
        }
        else
        {
            return false;
        }

        tokenType = UnescapeSegment(middle);
        return !string.IsNullOrEmpty(tokenType);
    }

    /// <summary>
    /// 尝试从完整键中解析出用户级 tokenType。
    /// </summary>
    /// <param name="key">完整存储键</param>
    /// <param name="keyPrefix">键前缀（已规范化）</param>
    /// <param name="userId">已知的 userId（已规范化）</param>
    /// <param name="tokenType">解析出的原始（已反转义）tokenType</param>
    /// <returns>是否成功解析</returns>
    internal static bool TryParseUserTokenType(string key, string keyPrefix, string userId, out string? tokenType)
    {
        tokenType = null;
        if (string.IsNullOrEmpty(key))
            return false;

        var normalizedPrefix = NormalizePrefix(keyPrefix);
        var prefixWithUser = $"{normalizedPrefix}{Separator}user{Separator}{NormalizeSegment(userId)}{Separator}";
        if (!key.StartsWith(prefixWithUser, StringComparison.Ordinal))
            return false;

        const string accessSuffix = Separator + "access";
        const string refreshSuffix = Separator + "refresh";

        string middle;
        if (key.EndsWith(accessSuffix, StringComparison.Ordinal))
        {
            middle = key.Substring(prefixWithUser.Length, key.Length - prefixWithUser.Length - accessSuffix.Length);
        }
        else if (key.EndsWith(refreshSuffix, StringComparison.Ordinal))
        {
            middle = key.Substring(prefixWithUser.Length, key.Length - prefixWithUser.Length - refreshSuffix.Length);
        }
        else
        {
            return false;
        }

        tokenType = UnescapeSegment(middle);
        return !string.IsNullOrEmpty(tokenType);
    }

    /// <summary>
    /// TMF2-05：键前缀的唯一出口——Memory 与 Redis 两端均委派此方法。
    /// </summary>
    /// <param name="appKey">应用唯一标识</param>
    /// <returns>规范化后的键前缀（如 <c>feishu:cli_a:token</c>，appKey 含特殊字符时按段转义）</returns>
    internal static string BuildKeyPrefix(string appKey)
    {
        var safeAppKey = string.IsNullOrWhiteSpace(appKey) ? "default" : appKey;
        // 各段经 NormalizeSegment 转义，段间用 Separator 连接。
        // 注意：此处前缀段的转义与 NormalizePrefix 对已有前缀的二次转义不同——
        // 这是构造前缀的唯一入口，前缀内部不再经 NormalizePrefix。
        return string.Join(Separator,
            NormalizeSegment("feishu"),
            NormalizeSegment(safeAppKey),
            NormalizeSegment("token"));
    }

    /// <summary>
    /// 返回规范化后的键前缀（用于日志和测试诊断）。
    /// </summary>
    internal static string DescribePrefix(string keyPrefix)
        => NormalizePrefix(keyPrefix);

    /// <summary>
    /// 规范化键段：转义分隔符与 glob 元字符。
    /// </summary>
    /// <remarks>
    /// TMR-P2-9（F9）：超长段抛 <see cref="ArgumentException"/> 而非 <see cref="InvalidOperationException"/>——
    /// 超长段是输入校验失败（外部可控 userId 可触发）而非"对象处于无效状态"，
    /// 脱离与瞬时白名单（IsTransientInitFailure 白名单含 InvalidOperationException）的语义纠缠，
    /// 避免被误判为可重试。键布局逐字节不变，D8 契约不受影响。
    /// <para>
    /// TMF2-08：单遍扫描转义 <c>\</c> <c>:</c> <c>*</c> <c>?</c> <c>[</c> <c>]</c>——
    /// 分两批（先 \ 再 :/*?[]）会导致中间态不一致（如 appKey="*:"
    /// 先转义 : 得 "*\:"，再转义 * 得 "\*\:"——正确；但反过来先转义 * 得 "\*:"
    /// 再转义 : 得 "\*\:"——也正确。但若先转义 * 得 "\*" 再转义 \ 得 "\\*"——错误）。
    /// 单遍扫描避免此问题。
    /// </para>
    /// </remarks>
    private static string NormalizeSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return string.Empty;

        if (segment.Length > MaxSegmentLength)
            throw new ArgumentException(
                $"键段长度 {segment.Length} 超过上限 {MaxSegmentLength}", nameof(segment));

        // TMF2-08：单遍扫描——遇到 \ : * ? [ ] 时在其前插入 \。
        var sb = new System.Text.StringBuilder(segment.Length * 2);
        foreach (var ch in segment)
        {
            switch (ch)
            {
                case '\\':
                case ':':
                case '*':
                case '?':
                case '[':
                case ']':
                    sb.Append('\\');
                    break;
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 规范化键前缀（转义各段，保持分隔符不变）。
    /// </summary>
    private static string NormalizePrefix(string keyPrefix)
    {
        if (string.IsNullOrEmpty(keyPrefix))
            return string.Empty;

        var parts = keyPrefix.Split([Separator], StringSplitOptions.None);
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = NormalizeSegment(parts[i]);
        }
        return string.Join(Separator, parts);
    }

    /// <summary>
    /// 反转义键段（单遍扫描——遇到 \ 时跳过 \ 并输出下一个字符）。
    /// </summary>
    /// <remarks>
    /// TMF2-08：与 <see cref="NormalizeSegment"/> 对称的单遍扫描——
    /// 反转义 <c>\:</c> <c>\\</c> <c>\*</c> <c>\?</c> <c>\[</c> <c>\]</c> 为原始字符。
    /// </remarks>
    private static string UnescapeSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return string.Empty;

        var sb = new System.Text.StringBuilder(segment.Length);
        for (var i = 0; i < segment.Length; i++)
        {
            if (segment[i] == '\\' && i + 1 < segment.Length)
            {
                // 跳过 \，输出被转义的字符。
                sb.Append(segment[i + 1]);
                i++;
            }
            else
            {
                sb.Append(segment[i]);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// 组合键——规范化前缀 + 转义各段 + 分隔符连接。
    /// </summary>
    private static string Combine(string keyPrefix, params string?[] segments)
    {
        var normalizedPrefix = NormalizePrefix(keyPrefix);
        var parts = new List<string>(segments.Length + 1) { normalizedPrefix };

        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment))
                continue;
            parts.Add(NormalizeSegment(segment!));
        }

        return string.Join(Separator, parts);
    }
}
