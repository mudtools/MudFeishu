// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 飞书令牌存储值编解码器 —— 上游 <see cref="ITokenStoreCodec"/> 的飞书侧实现。
/// </summary>
/// <remarks>
/// <b>薄包装</b>：双读判别由官方 <see cref="MigrationTokenStoreCodec"/> 承担，本类保留类型与 <see cref="Instance"/> 供既有消费方
/// 二进制兼容，<b>不得</b>再维护私有双读实现。<b>写入格式</b>：<c>Encode</c> 单写相对秒 <c>{expiresInSeconds}|{token}</c>，与官方
/// <see cref="DefaultTokenStoreCodec"/> 逐字节一致，<b>不得</b>新写「绝对毫秒戳」私有形态。
/// <b>互操作</b>：官方 store（如 <c>RedisTokenStore</c>）接入飞书存量数据时须把 <see cref="Instance"/> 注入其 <c>codec</c>；两种约定不可互换直插（双重编码）。
/// </remarks>
public sealed class FeishuTokenStoreCodec : ITokenStoreCodec
{
    /// <summary>
    /// 绝对毫秒戳与相对秒的量级分界：相对秒达到 10<sup>10</sup>（约 317 年）不可能；
    /// 绝对毫秒戳低于 10<sup>10</sup> 则早于 1970-04，也不可能。两侧量级互斥 ⇒ 判别稳健。
    /// </summary>
    /// <remarks>
    /// 仅作官方 <see cref="MigrationTokenStoreCodec"/> 阈值的显式来源与守卫断言锚
    /// （须恒等于 <see cref="MigrationTokenStoreCodec.DefaultLegacyAbsoluteMillisecondsThreshold"/>）。
    /// </remarks>
    public const long AbsoluteMsThreshold = 10_000_000_000L;

    private static readonly MigrationTokenStoreCodec Inner = new(AbsoluteMsThreshold);

    /// <summary>
    /// 共享实例（无状态、线程安全）。
    /// </summary>
    public static FeishuTokenStoreCodec Instance { get; } = new();

    /// <inheritdoc />
    /// <remarks>委派官方 <see cref="MigrationTokenStoreCodec"/>（其 Encode 与 <see cref="DefaultTokenStoreCodec"/> 逐字节一致，单写官方相对秒格式）。</remarks>
    public string? Encode(string? token, long expiresInSeconds)
        => Inner.Encode(token, expiresInSeconds);

    /// <inheritdoc />
    /// <remarks>双读兼容由官方 <see cref="MigrationTokenStoreCodec"/> 承担：旧「绝对毫秒戳」值 floor 投影为剩余秒，新「相对秒」值原样返回。</remarks>
    public (string? Token, long ExpiresInSeconds)? Decode(string? encoded)
        => Inner.Decode(encoded);
}
