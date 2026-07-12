// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
        var key = BuildUserAccessTokenKey(userId, tokenType);
        var value = await _redis.GetDatabase().StringGetAsync(key, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    public override async Task SetAccessTokenAsync(string userId, string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
    {
        var key = BuildUserAccessTokenKey(userId, tokenType);
        await _redis.GetDatabase().StringSetAsync(key, accessToken, TimeSpan.FromSeconds(expiresInSeconds), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<string?> GetRefreshTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildUserRefreshTokenKey(userId, tokenType);
        var value = await _redis.GetDatabase().StringGetAsync(key, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    public override async Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        var key = BuildUserRefreshTokenKey(userId, tokenType);
        await _redis.GetDatabase().StringSetAsync(key, refreshToken, TimeSpan.FromDays(30), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task RemoveAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        await _redis.GetDatabase().KeyDeleteAsync(new RedisKey[] { BuildUserAccessTokenKey(userId, tokenType), BuildUserRefreshTokenKey(userId, tokenType) }, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<string>> GetTokenTypesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var pattern = $"{_keyPrefix}:user:{userId}:*:access";
        var keys = RedisStoreHelper.GetServer(_redis).Keys(pattern: pattern, pageSize: 250, flags: RedisStoreHelper.ToCommandFlags(cancellationToken));
        var tokenTypes = new List<string>();
        var prefixLength = $"{_keyPrefix}:user:{userId}:".Length;

        foreach (var key in keys)
        {
            var keyStr = key.ToString();
            var parts = keyStr.Substring(prefixLength).Split(':');
            if (parts.Length >= 2)
                tokenTypes.Add(parts[0]);
        }

        return tokenTypes.Distinct();
    }

    /// <inheritdoc />
    public override async Task ClearUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var pattern = $"{_keyPrefix}:user:{userId}:*";
        var db = _redis.GetDatabase();
        var keys = RedisStoreHelper.GetServer(_redis).Keys(pattern: pattern, pageSize: 250, flags: RedisStoreHelper.ToCommandFlags(cancellationToken));

        foreach (var key in keys)
            await db.KeyDeleteAsync(key, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }
}
