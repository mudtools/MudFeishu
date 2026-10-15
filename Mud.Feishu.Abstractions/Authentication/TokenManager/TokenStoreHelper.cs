// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 令牌存储编解码工具类（C-2 裁决落地：<see cref="FeishuTokenStoreCodec"/> 的消费侧门面）。
/// </summary>
/// <remarks>
/// <para>
/// <b>写入格式（3.0.3 起）</b>：<c>{expiresInSeconds}|{token}</c>（相对秒，官方
/// <c>DefaultTokenStoreCodec</c> 形态）——由 <see cref="EncodeStoredToken"/> 以
/// 「令牌自身过期时刻」推导剩余秒后写入。不再新写旧版「绝对毫秒戳」私有形态。
/// </para>
/// <para>
/// <b>读取口径（双读，C-2）</b>：<see cref="DecodeStoredToken"/> / <see cref="TryDecodeExpiry"/>
/// 对存量旧值（绝对毫秒戳）与新值（相对秒）均兼容：
/// 旧值原样给出毫秒戳；新值投影为 <c>nowMs + seconds × 1000</c>（消费方「绝对毫秒戳」契约不变）；
/// <c>seconds &lt;= 0</c>（官方「无 TTL 信息」）与无法解析的值投影为 0 / false——
/// Redis 侧 refresh TTL 由此回落 30 天（TMA-22 语义），旧值中「已过期毫秒戳删键」行为保持不变。
/// 旧数据无需回灌，按条目自然过期收敛；读写分离保证迁移期不混写同一键（全部写入方为本格式）。
/// </para>
/// </remarks>
internal static class TokenStoreHelper
{
    /// <summary>
    /// 编码存储值：把过期时刻（Unix 毫秒）换算为剩余秒后按官方相对秒格式编码
    /// （C-2 裁决：Encode 单写 <c>{expiresInSeconds}|{token}</c>）。
    /// </summary>
    /// <param name="token">令牌明文（不得为空；空令牌由调用方——桥接器——跳过写穿）。</param>
    /// <param name="expireTimestampMs">令牌过期时刻（Unix 毫秒）；<c>&lt;= 0</c> 写为 0（无 TTL 信息）。</param>
    /// <returns>编码后的存储值。</returns>
    public static string EncodeStoredToken(string token, long expireTimestampMs)
        => FeishuTokenStoreCodec.Instance.Encode(token, RemainingSeconds(expireTimestampMs))!;

    /// <summary>
    /// 计算剩余有效时长（秒），供桥接器作为持久层 TTL 与编码入口共用（唯一舍入口径）。
    /// </summary>
    /// <param name="expireTimestampMs">令牌过期时刻（Unix 毫秒）。</param>
    /// <returns>剩余秒数；已过期或无法判定时为 0。</returns>
    /// <remarks>
    /// 采用四舍五入（<c>+500</c>）而非截断：适配器侧的入参来源于「刚刚写入内存的过期时刻」，
    /// 截断会使持久层 TTL 比真实剩余时长系统性少 1 秒。
    /// </remarks>
    public static long RemainingSeconds(long expireTimestampMs)
    {
        if (expireTimestampMs <= 0)
            return 0;

        var remainingMs = expireTimestampMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return remainingMs <= 0 ? 0 : (remainingMs + 500) / 1000L;
    }

    /// <summary>
    /// 解码存储值（TMA-15 / C-2 双读：无 <c>{n}|</c> 前缀的旧格式 / 损坏值按「无过期信息」返回，
    /// 由调用方（值工厂）判定为未命中）。
    /// </summary>
    /// <param name="storedValue">存储值（相对秒新格式 / 绝对毫秒戳旧格式）。</param>
    /// <returns>令牌明文与过期时刻（毫秒投影，见类型级 remarks）；<paramref name="storedValue"/> 为空时为 <c>(null, 0)</c>。</returns>
    public static (string Token, long ExpireTimestampMs) DecodeStoredToken(string storedValue)
    {
        if (string.IsNullOrEmpty(storedValue))
            return (string.Empty, 0);

        var parsed = ParseStoredValue(storedValue);
        if (parsed is null)
        {
            // separatorIndex <= 0 或前导数字无法解析：存储值不含有效过期时间戳，
            // 视为旧格式或损坏数据，返回原始值并将过期时间置零，由调用方决定是否刷新令牌。
            return (storedValue, 0);
        }

        return (parsed.Value.Token, ProjectToExpireMs(parsed.Value.LeadingNumber));
    }

    /// <summary>
    /// 尝试从编码值中解码过期时间戳，不解析令牌本身。
    /// 用于 refresh token 存储 TTL 计算（Redis 路径需要从编码值推断 TTL）。
    /// </summary>
    /// <param name="storedValue">编码后的存储值（相对秒新格式 / 绝对毫秒戳旧格式）。</param>
    /// <param name="expireTimestampMs">解码出的过期时刻（毫秒投影，见类型级 remarks）；解码失败时为 0。</param>
    /// <returns>
    /// 存在有效过期信息返回 true；「无 TTL 信息」（前导数 &lt;= 0）或无法解析返回 false
    /// （Redis 侧由此回落 30 天，TMA-22）。
    /// </returns>
    public static bool TryDecodeExpiry(string storedValue, out long expireTimestampMs)
    {
        expireTimestampMs = 0;
        if (string.IsNullOrEmpty(storedValue))
            return false;

        var parsed = ParseStoredValue(storedValue);
        if (parsed is null)
            return false;

        expireTimestampMs = ProjectToExpireMs(parsed.Value.LeadingNumber);
        return expireTimestampMs > 0;
    }

    /// <summary>解析结果：令牌明文 + 前导数值原样（量级未判别）。</summary>
    private readonly record struct ParsedValue(string Token, long LeadingNumber);

    /// <summary>
    /// 共享解析核心：<c>{n}|{token}</c> 形态校验 + 前导数值解析（不判别量级、不换算）；
    /// v2 版本化信封（<c>v2:{n}|{token}</c> / <c>v2:expired|</c>）经官方
    /// <see cref="VersionedTokenStoreCodec.TryDecodeEnvelope"/> 解析后归一到同一结果形态。
    /// </summary>
    /// <remarks>
    /// U-2（上游 3.0.10 Batch 3）：读取侧 v2 兼容 —— 上游默认 codec 已具备 v2 前缀识别，
    /// 本门面（Feishu 消费侧）同步支持，使「宿主经官方 store + <c>VersionedTokenStoreCodec</c> 组合写入 v2」
    /// 或「官方未来切换写入形态」两种情形下的消费侧（含 Redis 侧 TTL 推导）不丢失过期信息。
    /// 写侧形态不变（C-2：默认形态不得漂移）——<see cref="EncodeStoredToken"/> 仍单写相对秒 v1 形态。
    /// <para>
    /// <c>v2:expired|</c>（显式已过期）投影为「无令牌 + 过期时刻 0」——与上游
    /// <see cref="ITokenStoreCodec.Decode"/> 的 <c>(null, 0)</c> 投影同口径：
    /// 调用方按「无令牌」处置（miss）。
    /// </para>
    /// </remarks>
    private static ParsedValue? ParseStoredValue(string storedValue)
    {
        if (VersionedTokenStoreCodec.IsVersionedEnvelope(storedValue))
        {
            // v2：结构非法 ⇒ 与「无法解析」同口径（null ⇒ 调用方按无过期信息处理）。
            if (!VersionedTokenStoreCodec.TryDecodeEnvelope(storedValue, out var state, out var v2Token, out var v2Seconds))
                return null;

            // 显式已过期 ⇒ 不携带可消费令牌（前导 0 ⇒ 投影 0 = 无 TTL 信息，Redis 侧按既有回落语义处置）。
            return state == TokenStoreEnvelopeState.Expired
                ? new ParsedValue(string.Empty, 0)
                : new ParsedValue(v2Token ?? string.Empty, v2Seconds);
        }

        var separatorIndex = storedValue.IndexOf('|');
        if (separatorIndex <= 0)
            return null;

        if (!long.TryParse(storedValue.Substring(0, separatorIndex), out var leading))
            return null;

        return new ParsedValue(storedValue.Substring(separatorIndex + 1), leading);
    }

    /// <summary>
    /// C-2 双读投影：前导数 <c>&gt;= <see cref="FeishuTokenStoreCodec.AbsoluteMsThreshold"/></c>
    /// ⇒ 旧格式绝对毫秒戳（原样）；<c>&gt; 0 且 &lt; 阈值</c> ⇒ 新格式相对秒（投影
    /// <c>nowMs + s × 1000</c>）；<c>&lt;= 0</c> ⇒ 无 TTL 信息（投影 0）。
    /// </summary>
    private static long ProjectToExpireMs(long leadingNumber)
    {
        if (leadingNumber <= 0)
            return 0;

        if (leadingNumber >= FeishuTokenStoreCodec.AbsoluteMsThreshold)
            return leadingNumber;

        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (leadingNumber * 1000L);
    }
}
