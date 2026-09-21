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
/// <para>
/// <b>TOK-1 修复</b>：原 <c>SingletonFeishuTokenStoreFactory</c> 直接忽略 <c>appKey</c>，
/// 返回共享的 <see cref="RedisTokenStore"/> 单例；而该单例的键布局为
/// <c>feishu:token:{tokenType}:access</c>、用户令牌为 <c>feishu:token:user:{userId}:{tokenType}:access</c>，
/// <b>均不含 appKey 维度</b>。多应用场景下不同应用会写入同一键空间，导致令牌互相覆盖
/// （表现为随机 401 / 令牌串号）。
/// </para>
/// <para>
/// 本工厂为每个 appKey 构造独立键前缀 <c>feishu:{appKey}:token</c> 的存储实例，
/// 与 Memory 路径（<c>FeishuTokenStore</c> / <c>FeishuUserTokenStore</c>）的键布局保持一致。
/// 所有实例共享同一个 <see cref="IConnectionMultiplexer"/>，因此不会造成连接池膨胀。
/// </para>
/// </remarks>
public class PerAppRedisTokenStoreFactory : IFeishuTokenStoreFactory
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (ITokenStore TokenStore, IUserTokenStore UserTokenStore)> _stores =
        new(StringComparer.Ordinal);

    /// <summary>
    /// 初始化 <see cref="PerAppRedisTokenStoreFactory"/> 实例。
    /// </summary>
    /// <param name="redis">Redis 连接复用器（Singleton，由全部 per-app 存储共享）。</param>
    /// <param name="loggerFactory">日志工厂，可为 null（降级为不记日志）。</param>
    public PerAppRedisTokenStoreFactory(IConnectionMultiplexer redis, ILoggerFactory? loggerFactory = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// 构建指定应用的 Redis 键前缀，与 Memory 路径对齐。
    /// TMF2-05：委派 TokenKeyBuilder.BuildKeyPrefix——
    /// 消除经 RedisKeyBuilder.Combine 转义与 Memory 裸拼接的差异（前缀逐字节一致）。
    /// </summary>
    public static string BuildKeyPrefix(string appKey) =>
        TokenKeyBuilder.BuildKeyPrefix(appKey);

    /// <inheritdoc />
    public (ITokenStore TokenStore, IUserTokenStore? UserTokenStore) Create(string appKey)
    {
        // T-M2-11：appKey 为空抛明确异常（替代 ConcurrentDictionary 的 ArgumentNullException）
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("appKey 不能为空——空 appKey 会导致令牌键空间退化为 feishu:default:token", nameof(appKey));

        var (tokenStore, userTokenStore) = _stores.GetOrAdd(appKey, key =>
        {
            var keyPrefix = BuildKeyPrefix(key);
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
