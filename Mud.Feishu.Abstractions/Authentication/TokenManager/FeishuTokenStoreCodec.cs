// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 飞书令牌存储值编解码器 —— 上游 <see cref="ITokenStoreCodec"/>（F-2/C-2 裁决）的飞书侧实现。
/// </summary>
/// <remarks>
/// <para>
/// <b>写入格式（C-2 裁决：Encode 单写相对秒）</b>：<c>{expiresInSeconds}|{token}</c>——
/// 与上游 <see cref="DefaultTokenStoreCodec"/> 逐字节一致（Encode 直接委派官方实现）。
/// 自 3.0.3 起不再新写旧版「绝对毫秒戳」私有形态；存量旧值经双读兼容，按条目自然过期收敛，无需回灌。
/// </para>
/// <para>
/// <b>双读兼容（Decode）</b>：前导数值按量级判别——
/// <list type="bullet">
/// <item><c>&gt;= <see cref="AbsoluteMsThreshold"/></c>（10<sup>10</sup>）⇒ 旧格式绝对毫秒戳：
/// 按 <c>(expireMs − now) / 1000</c> 换算剩余秒（已过期为负，由调用方判定）——
/// 旧毫秒戳不可能是相对秒（相对秒 10<sup>10</sup> ≈ 317 年），毫秒戳低于阈值则早于 1970-04，量级互斥；</item>
/// <item><c>&lt; 阈值</c> ⇒ 新格式相对秒：原样返回（<c>&lt;= 0</c> 为官方「无 TTL 信息」语义）。</item>
/// </list>
/// </para>
/// <para>
/// <b>互操作锚点</b>：宿主将官方组件（如 <c>Mud.HttpUtils.Redis.RedisTokenStore</c>，其
/// <c>ITokenStore</c> 契约入参为<b>明文</b>令牌、内部经 codec 编解码落盘）接到飞书形态的
/// 存量数据上时，可将 <see cref="Instance"/> 注入其 <c>codec</c> 参数——读侧旧值可解析、
/// 写侧收敛为官方格式。注意官方 store 与飞书管线（桥接器直存编码值，见
/// <c>FeishuTokenBridgeCodec</c>）是两种 <c>ITokenStore</c> 使用约定，不可互换直插（混用会双重编码）。
/// </para>
/// <para>
/// <b>回滚代价（须知）</b>：回滚到 3.0.2 及更早的二进制时，新格式值的前导数字会被旧解析
/// 当作毫秒戳（远小于当前时刻）→ 判为已过期 → 未命中 → 重新获取令牌；Redis 侧 refresh
/// 键会被旧 <c>SetRefreshTokenAsync</c> 的「已过期删键」逻辑删除（用户需重新授权）。
/// 不产生错误令牌，但升级 / 回滚不对称——发布前请评估回滚窗口。
/// </para>
/// </remarks>
public sealed class FeishuTokenStoreCodec : ITokenStoreCodec
{
    /// <summary>
    /// 绝对毫秒戳与相对秒的量级分界：相对秒达到 10<sup>10</sup>（约 317 年）不可能；
    /// 绝对毫秒戳低于 10<sup>10</sup> 则早于 1970-04，也不可能。两侧量级互斥 ⇒ 判别稳健。
    /// </summary>
    public const long AbsoluteMsThreshold = 10_000_000_000L;

    /// <summary>
    /// 共享实例（无状态、线程安全）。
    /// </summary>
    public static FeishuTokenStoreCodec Instance { get; } = new();

    /// <inheritdoc />
    /// <remarks>直接委派 <see cref="DefaultTokenStoreCodec.Instance"/>（C-2：单写官方相对秒格式）。</remarks>
    public string? Encode(string? token, long expiresInSeconds)
        => DefaultTokenStoreCodec.Instance.Encode(token, expiresInSeconds);

    /// <inheritdoc />
    /// <remarks>双读兼容：旧「绝对毫秒戳」值换算为剩余秒后返回，新「相对秒」值原样返回。</remarks>
    public (string? Token, long ExpiresInSeconds)? Decode(string? encoded)
    {
        if (encoded is null || encoded.Length == 0)
            return null;

        var index = encoded.IndexOf('|');
        if (index <= 0)
            return null;   // 无分隔符 / 分隔符在首位 ⇒ 非法格式（与官方 codec 同口径：不抛异常）

        if (!long.TryParse(
                encoded.Substring(0, index),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var leading))
        {
            return null;
        }

        var token = encoded.Substring(index + 1);
        if (leading < AbsoluteMsThreshold)
            return (token, leading);   // 新格式：相对秒（<= 0 = 无 TTL 信息）

        // 旧格式：绝对毫秒戳 → 换算剩余秒（截断；已过期为负）。
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return (token, (leading - nowMs) / 1000L);
    }
}
