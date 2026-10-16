// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 飞书令牌存储值编解码器 —— 上游 <see cref="ITokenStoreCodec"/>（F-2/C-2 裁决）的飞书侧实现。
/// </summary>
/// <remarks>
/// <para>
/// <b>U-1 回收（上游 3.0.10 Batch 1）</b>：双读判别逻辑已上收官方
/// <see cref="MigrationTokenStoreCodec"/>（阈值默认 10<sup>10</sup>，与两下游实测钉死值一致）。
/// 本类自该版本起退化为<b>薄包装</b>——保留类型与 <see cref="Instance"/> 供既有消费方
/// （<see cref="TokenStoreHelper"/>、<see cref="FeishuTokenBridgeCodec"/>、互操作锚点）二进制兼容，
/// 不再维护私有双读实现；旧值投影语义（floor 取整）以官方实现为准。
/// </para>
/// <para>
/// <b>写入格式（C-2 裁决：Encode 单写相对秒）</b>：<c>{expiresInSeconds}|{token}</c>——
/// 与上游 <see cref="DefaultTokenStoreCodec"/> 逐字节一致。自 3.0.3 起不再新写旧版
/// 「绝对毫秒戳」私有形态；存量旧值经官方双读兼容，按条目自然过期收敛，无需回灌。
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
    /// <remarks>
    /// U-1 回收后仅作官方 <see cref="MigrationTokenStoreCodec"/> 阈值的显式来源与守卫断言锚
    /// （须恒等于 <see cref="MigrationTokenStoreCodec.DefaultLegacyAbsoluteMillisecondsThreshold"/>）。
    /// </remarks>
    public const long AbsoluteMsThreshold = 10_000_000_000L;

    private static readonly MigrationTokenStoreCodec Inner = new(AbsoluteMsThreshold);

    /// <summary>
    /// 共享实例（无状态、线程安全）。
    /// </summary>
    public static FeishuTokenStoreCodec Instance { get; } = new();

    /// <inheritdoc />
    /// <remarks>委派官方 <see cref="MigrationTokenStoreCodec"/>（其 Encode 与 <see cref="DefaultTokenStoreCodec"/> 逐字节一致，C-2：单写官方相对秒格式）。</remarks>
    public string? Encode(string? token, long expiresInSeconds)
        => Inner.Encode(token, expiresInSeconds);

    /// <inheritdoc />
    /// <remarks>双读兼容由官方 <see cref="MigrationTokenStoreCodec"/> 承担：旧「绝对毫秒戳」值 floor 投影为剩余秒，新「相对秒」值原样返回。</remarks>
    public (string? Token, long ExpiresInSeconds)? Decode(string? encoded)
        => Inner.Decode(encoded);
}
