// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 令牌持久化桥接器的编解码口径单一出口（值适配器 / 值工厂共用）。
/// </summary>
/// <remarks>
/// <para>
/// <b>兼容锚点（禁止改动）</b>：存储值格式恒为 <c>{expireTimestampMs}|{token}</c>
/// （<see cref="TokenStoreHelper.EncodeStoredToken"/> / <see cref="TokenStoreHelper.DecodeStoredToken"/>），
/// 与桥接前的落库值逐字节一致 ⇒ 升级 / 回滚双向数据兼容；
/// Redis 端 refresh TTL 推导（<see cref="TokenStoreHelper.TryDecodeExpiry"/>）零改动。
/// </para>
/// <para>
/// <b>TTL 口径</b>：<see cref="RemainingSeconds"/> 以「令牌自身过期时刻」为唯一真相
/// （与管线判定同源），<c>&lt;= 0</c> 时返回 0 —— 桥接器据此<b>跳过</b>访问令牌写穿
/// （宁缺勿滥，防止持久层以无界 TTL 滞留已过期令牌），与改造前
/// <c>UserTokenManager.PersistUserTokenAsync</c> 在剩余 ≤ 0 时只写 refresh 的行为一致。
/// </para>
/// </remarks>
internal static class FeishuTokenBridgeCodec
{
    /// <summary>
    /// 编码存储值：令牌为空返回 null（桥接器据此跳过该字段的写穿）。
    /// </summary>
    /// <param name="token">令牌明文。</param>
    /// <param name="expireTimestampMs">令牌过期时刻（Unix 毫秒）。</param>
    /// <returns>编码后的存储值；<paramref name="token"/> 为空时为 null。</returns>
    internal static string? EncodeToken(string? token, long expireTimestampMs)
        => string.IsNullOrEmpty(token) ? null : TokenStoreHelper.EncodeStoredToken(token!, expireTimestampMs);

    /// <summary>
    /// 计算访问令牌的剩余有效时长（秒），供桥接器作为持久层 TTL。
    /// </summary>
    /// <param name="expireTimestampMs">令牌过期时刻（Unix 毫秒）。</param>
    /// <returns>剩余秒数；已过期或无法判定时为 0。</returns>
    /// <remarks>
    /// 采用四舍五入（<c>+500</c>）而非截断：适配器侧的入参来源于「刚刚写入内存的过期时刻」，
    /// 截断会使持久层 TTL 比真实剩余时长系统性少 1 秒。
    /// </remarks>
    internal static long RemainingSeconds(long expireTimestampMs)
    {
        if (expireTimestampMs <= 0)
            return 0;

        var remainingMs = expireTimestampMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return remainingMs <= 0 ? 0 : (remainingMs + 500) / 1000L;
    }

    /// <summary>
    /// 解码存储值（TMA-15：无 <c>{expireMs}|</c> 前缀的旧格式 / 损坏值按「无过期信息」返回，
    /// 由调用方（值工厂）判定为未命中）。
    /// </summary>
    /// <param name="storedValue">存储值。</param>
    /// <returns>令牌明文与过期时刻；<paramref name="storedValue"/> 为空时为 <c>(null, 0)</c>。</returns>
    internal static (string? Token, long ExpireTimestampMs) DecodeToken(string? storedValue)
        => string.IsNullOrEmpty(storedValue)
            ? (null, 0L)
            : TokenStoreHelper.DecodeStoredToken(storedValue!);
}
