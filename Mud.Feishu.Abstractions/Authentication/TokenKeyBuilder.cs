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
        => $"{TenantScanPatternLiteral(keyPrefix)}*";

    /// <summary>
    /// 构建用于 SCAN 的用户键模式（通配 tokenType 与 access/refresh）。
    /// </summary>
    internal static string UserScanPattern(string keyPrefix, string userId)
        => $"{UserScanPatternLiteral(keyPrefix, userId)}*";

    /// <summary>
    /// 构建用于 SCAN 的全用户键模式（通配 userId、tokenType 与 access/refresh）。
    /// TMF-01：IFeishuUserTokenStorePurge.ClearAllUsersAsync 的键模式单一出口（D8 契约）。
    /// </summary>
    internal static string AllUsersScanPattern(string keyPrefix)
        => $"{AllUsersScanPatternLiteral(keyPrefix)}*";

    /// <summary>
    /// TMR2-P1-2：返回租户 SCAN 模式的<b>字面量前缀</b>（已规范化，<b>不含</b>尾随通配符）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 供需要"字面量片段 + 后端特有通配语义"两种转义的消费方使用（如
    /// <c>Mud.Feishu.Redis</c> 需先按 Redis glob 语义对字面量再转义一次，
    /// 见 <c>RedisGlobPattern.FromLiteralPrefix</c>）。
    /// </para>
    /// <para>
    /// <see cref="TenantScanPattern"/> 保持原语义（规范化前缀 + <c>*</c>），继续作为
    /// Memory/诊断路径的单一出口。
    /// </para>
    /// </remarks>
    /// <param name="keyPrefix">键前缀（含 appKey 维度，未规范化亦可）。</param>
    /// <returns>形如 <c>feishu:{appKey}:token:</c> 的规范化字面量前缀。</returns>
    internal static string TenantScanPatternLiteral(string keyPrefix)
        => $"{NormalizePrefix(keyPrefix)}{Separator}";

    /// <summary>
    /// TMR2-P1-2：返回指定用户 SCAN 模式的字面量前缀（不含尾随通配符）。
    /// </summary>
    /// <param name="keyPrefix">键前缀（含 appKey 维度）。</param>
    /// <param name="userId">用户唯一标识符。</param>
    /// <returns>形如 <c>feishu:{appKey}:token:user:{userId}:</c> 的规范化字面量前缀。</returns>
    internal static string UserScanPatternLiteral(string keyPrefix, string userId)
        => $"{NormalizePrefix(keyPrefix)}{Separator}user{Separator}{NormalizeSegment(userId)}{Separator}";

    /// <summary>
    /// TMR2-P1-2：返回全用户 SCAN 模式的字面量前缀（不含尾随通配符）。
    /// </summary>
    /// <param name="keyPrefix">键前缀（含 appKey 维度）。</param>
    /// <returns>形如 <c>feishu:{appKey}:token:user:</c> 的规范化字面量前缀。</returns>
    internal static string AllUsersScanPatternLiteral(string keyPrefix)
        => $"{NormalizePrefix(keyPrefix)}{Separator}user{Separator}";

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
    /// 返回规范化后的键前缀（用于日志和测试诊断）。
    /// </summary>
    internal static string DescribePrefix(string keyPrefix)
        => NormalizePrefix(keyPrefix);

    /// <summary>
    /// 规范化键段：转义分隔符（自洽——先转义 \ 自身，再转义 :）。
    /// </summary>
    /// <remarks>
    /// TMR-P2-9（F9）：超长段抛 <see cref="ArgumentException"/> 而非 <see cref="InvalidOperationException"/>——
    /// 超长段是输入校验失败（外部可控 userId 可触发）而非"对象处于无效状态"，
    /// 脱离与瞬时白名单（IsTransientInitFailure 白名单含 InvalidOperationException）的语义纠缠，
    /// 避免被误判为可重试。键布局逐字节不变，D8 契约不受影响。
    /// </remarks>
    private static string NormalizeSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return string.Empty;

        if (segment.Length > MaxSegmentLength)
            throw new ArgumentException(
                $"键段长度 {segment.Length} 超过上限 {MaxSegmentLength}", nameof(segment));

        // 自洽转义：先 \ → \\，再 : → \:
        return segment
            .Replace(EscapeChar, EscapedEscapeChar)
            .Replace(Separator, EscapedSeparator);
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
    /// 反转义键段（先 : → :，再 \ → \）。
    /// </summary>
    private static string UnescapeSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return string.Empty;

        // 反转义：先 \: → :，再 \\ → \
        return segment
            .Replace(EscapedSeparator, Separator)
            .Replace(EscapedEscapeChar, EscapeChar);
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
