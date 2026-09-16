// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  本项目基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 基于 Redis 的飞书用户令牌存储适配器
/// </summary>
/// <remarks>
/// 实现 Mud.HttpUtils v2.0 的 IUserTokenStore 接口，将用户令牌持久化到 Redis。
/// 支持按用户标识隔离令牌数据，适用于多实例分布式部署场景。
/// ITokenStore 的方法通过 UserTokenStoreBase 基类委托给内部 RedisTokenStore 实现。
/// </remarks>
public class RedisUserTokenStore : UserTokenStoreBase
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _keyPrefix;

    /// <summary>
    /// 初始化 RedisUserTokenStore 实例
    /// </summary>
    /// <param name="innerStore">内部令牌存储实例，用于 ITokenStore 方法委托</param>
    /// <param name="redis">Redis 连接复用器</param>
    /// <param name="keyPrefix">Redis 键前缀，默认 "feishu:token"</param>
    public RedisUserTokenStore(
        RedisTokenStore innerStore,
        IConnectionMultiplexer redis,
        string keyPrefix = "feishu:token")
        : base(innerStore)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _keyPrefix = keyPrefix ?? "feishu:token";
    }

    /// <inheritdoc />
    protected override string KeyPrefix => _keyPrefix;

    /// <inheritdoc />
    public override async Task<string?> GetAccessTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildUserAccessTokenKey(userId, tokenType);
        var value = await _redis.GetDatabase().StringGetAsync(key, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    public override async Task SetAccessTokenAsync(string userId, string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
    {
        // T-M2-11 / R-23：非法过期时间显式抛异常
        if (expiresInSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(expiresInSeconds),
                "令牌过期时间必须为正数（秒），实际值: " + expiresInSeconds);

        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildUserAccessTokenKey(userId, tokenType);
        await _redis.GetDatabase().StringSetAsync(key, accessToken, TimeSpan.FromSeconds(expiresInSeconds), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<string?> GetRefreshTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildUserRefreshTokenKey(userId, tokenType);
        var value = await _redis.GetDatabase().StringGetAsync(key, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// TMA-22 修复：refresh token 存储 TTL 由硬编码 30 天改为优先使用编码值中的过期时间，
    /// 缺失时才回落 30 天。使 store TTL 与业务过期语义一致。
    /// </remarks>
    public override async Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildUserRefreshTokenKey(userId, tokenType);
        // TMA-22：尝试从编码值中解码过期时间戳。
        var ttl = TokenStoreHelper.TryDecodeExpiry(refreshToken, out var expireMs)
            ? TimeSpan.FromMilliseconds(Math.Max(0, expireMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
            : TimeSpan.FromDays(30);
        await _redis.GetDatabase().StringSetAsync(key, refreshToken, ttl, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task RemoveAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _redis.GetDatabase().KeyDeleteAsync(new RedisKey[] { BuildUserAccessTokenKey(userId, tokenType), BuildUserRefreshTokenKey(userId, tokenType) }, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// T-M3-1：覆写基类方法，改用 RedisKeyBuilder 统一构造（R-20/R-21 转义 + 长度护栏）。
    /// </remarks>
    protected override string BuildUserAccessTokenKey(string userId, string tokenType)
        => RedisKeyBuilder.Combine(_keyPrefix, "user", userId, tokenType, "access");

    /// <inheritdoc />
    /// <remarks>
    /// T-M3-1：覆写基类方法，改用 RedisKeyBuilder 统一构造。
    /// </remarks>
    protected override string BuildUserRefreshTokenKey(string userId, string tokenType)
        => RedisKeyBuilder.Combine(_keyPrefix, "user", userId, tokenType, "refresh");

    /// <inheritdoc />
    /// <remarks>
    /// T-M2-6（R-10）：Cluster 化扫描——遍历全部主节点聚合 SCAN。
    /// </remarks>
    public override async Task<IEnumerable<string>> GetTokenTypesAsync(string userId, CancellationToken cancellationToken = default)
    {
        // T-M3-1：pattern 前缀部分改用 RedisKeyBuilder 构造（R-01 护栏 + 转义一致性）
        var pattern = RedisKeyBuilder.Combine(_keyPrefix, "user", userId) + ":*:access";
        var tokenTypes = new List<string>();
        var prefixLength = (RedisKeyBuilder.Combine(_keyPrefix, "user", userId) + ":").Length;

        foreach (var server in RedisStoreHelper.GetServers(_redis))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var keys = server.Keys(pattern: pattern, pageSize: 250, flags: RedisStoreHelper.ToCommandFlags(cancellationToken));

            foreach (var key in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keyStr = key.ToString();

                // TM-04 修复（与 RedisTokenStore.GetTokenTypesAsync 对齐）：
                const string accessSuffix = ":access";
                if (!keyStr.EndsWith(accessSuffix, StringComparison.Ordinal))
                    continue;

                var tokenType = keyStr.Substring(prefixLength, keyStr.Length - prefixLength - accessSuffix.Length);
                if (tokenType.Length > 0)
                    tokenTypes.Add(tokenType);
            }
        }

        return tokenTypes.Distinct();
    }

    /// <inheritdoc />
    /// <remarks>
    /// T-M2-6（R-10）：Cluster 化扫描——遍历全部主节点聚合 SCAN + 删除。
    /// 部分节点失败时已删除的键不可回滚（声明"部分失败"语义）。
    /// </remarks>
    public override async Task ClearUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var pattern = RedisKeyBuilder.Combine(_keyPrefix, "user", userId) + ":*";
        var db = _redis.GetDatabase();

        foreach (var server in RedisStoreHelper.GetServers(_redis))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var keys = server.Keys(pattern: pattern, pageSize: 250, flags: RedisStoreHelper.ToCommandFlags(cancellationToken));

            foreach (var key in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await db.KeyDeleteAsync(key, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
            }
        }
    }
}
