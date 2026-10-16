// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions.Authentication;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 按应用隔离的 Redis 令牌存储工厂。
/// </summary>
/// <remarks>
/// 每个 appKey 对应独立键前缀 <c>feishu:{appKey}:token</c> 的存储实例，与 Memory 路径
/// （<c>FeishuTokenStore</c> / <c>FeishuUserTokenStore</c>）的键布局逐字节一致——键含 appKey 维度，
/// 避免多应用写入同一键空间导致令牌互相覆盖（表现为随机 401 / 令牌串号）。
/// 所有实例共享同一个 <see cref="IConnectionMultiplexer"/>，不会造成连接池膨胀。
/// </remarks>
public class PerAppRedisTokenStoreFactory : IFeishuTokenStoreFactory
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly string? _tokenKeyPrefix;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (ITokenStore TokenStore, IUserTokenStore UserTokenStore)> _stores =
        new(StringComparer.Ordinal);

    /// <summary>
    /// 初始化 <see cref="PerAppRedisTokenStoreFactory"/> 实例。
    /// </summary>
    /// <param name="redis">Redis 连接复用器（Singleton，由全部 per-app 存储共享）。</param>
    /// <param name="loggerFactory">日志工厂，可为 null（降级为不记日志）。</param>
    /// <param name="tokenKeyPrefix">
    /// 令牌键前缀的环境段，来自 <c>RedisOptions.TokenKeyPrefix</c>；
    /// <c>null</c>/空白时使用 <see cref="Mud.Feishu.Abstractions.Consts.DefaultTokenKeyPrefix"/>。
    /// </param>
    public PerAppRedisTokenStoreFactory(
        IConnectionMultiplexer redis,
        ILoggerFactory? loggerFactory = null,
        string? tokenKeyPrefix = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _loggerFactory = loggerFactory;
        _tokenKeyPrefix = tokenKeyPrefix;
    }

    /// <summary>
    /// 构建指定应用的 Redis 键前缀，与 Memory 路径 <c>feishu:{appKey}:token</c> <b>逐字节一致</b>。
    /// 委派 <c>TokenKeyBuilder.BuildKeyPrefix</c>（键前缀的唯一出口）——
    /// 消除经 <c>RedisKeyBuilder.Combine</c> 预转义与 Memory 裸拼接的差异（前缀逐字节一致）；
    /// 环境段由 <c>RedisOptions.TokenKeyPrefix</c> 注入（默认 <c>feishu</c>），空值回落默认值。
    /// </summary>
    /// <remarks>
    /// <b>此处不得预转义</b>：转义与规范化唯一归口 <c>TokenKeyBuilder</c>（由本方法委派调用）。若先用 <c>RedisKeyBuilder.Combine</c>
    /// 产出已转义前缀、再被 <c>NormalizePrefix</c> 二次转义，Memory 与 Redis 键布局将不再逐字节一致，且 SCAN pattern 与键中的双反斜杠不匹配
    /// ⇒ <c>ClearAsync</c> / <c>GetTokenTypesAsync</c> / <c>ClearAllUsersAsync</c> 永不命中（清库静默失效）。
    /// 键段超过 256 字符抛 <see cref="ArgumentException"/>，空前缀由固定 <c>feishu:</c> 前缀兜底。
    /// </remarks>
    public static string BuildKeyPrefix(string appKey) =>
        // 委派唯一出口；TokenKeyBuilder.BuildKeyPrefix 返回**未转义**前缀
        // （转义单点归口 TokenKeyBuilder.NormalizeSegment，由键构造路径 Combine 施加）。
        TokenKeyBuilder.BuildKeyPrefix(appKey);

    /// <summary>
    /// 构建指定应用在指定环境段下的 Redis 键前缀。
    /// </summary>
    /// <param name="appKey">应用唯一标识</param>
    /// <param name="tokenKeyPrefix">环境段（如 <c>dev</c>/<c>prod</c>）；<c>null</c> 时取默认 <c>feishu</c></param>
    /// <returns>如 <c>prod:cli_a:token</c></returns>
    public static string BuildKeyPrefix(string appKey, string? tokenKeyPrefix) =>
        TokenKeyBuilder.BuildKeyPrefix(appKey, tokenKeyPrefix);

    /// <inheritdoc />
    public (ITokenStore TokenStore, IUserTokenStore? UserTokenStore) Create(string appKey)
    {
        // appKey 为空抛明确异常（替代 ConcurrentDictionary 的 ArgumentNullException）
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("appKey 不能为空——空 appKey 会导致令牌键空间退化为 feishu:default:token", nameof(appKey));

        var (tokenStore, userTokenStore) = _stores.GetOrAdd(appKey, key =>
        {
            var keyPrefix = BuildKeyPrefix(key, _tokenKeyPrefix);
            var logger = _loggerFactory?.CreateLogger<RedisTokenStore>();

            var store = new RedisTokenStore(
                _redis,
                logger ?? NullLogger<RedisTokenStore>.Instance,
                keyPrefix);

            var userStore = new RedisUserTokenStore(store, _redis, keyPrefix);
            return (store, userStore);
        });

        return (tokenStore, userTokenStore);
    }
}
