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
/// ITokenStore 的方法通过组合 RedisTokenStore 实现，避免重复代码。
/// </remarks>
public class RedisUserTokenStore : IUserTokenStore
{
    private readonly RedisTokenStore _innerStore;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisUserTokenStore> _logger;
    private readonly string _keyPrefix;

    /// <summary>
    /// 初始化 RedisUserTokenStore 实例
    /// </summary>
    /// <param name="innerStore">内部令牌存储实例，用于 ITokenStore 方法委托</param>
    /// <param name="redis">Redis 连接复用器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="keyPrefix">Redis 键前缀，默认 "feishu:token"</param>
    public RedisUserTokenStore(
        RedisTokenStore innerStore,
        IConnectionMultiplexer redis,
        ILogger<RedisUserTokenStore> logger,
        string keyPrefix = "feishu:token")
    {
        _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyPrefix = keyPrefix ?? "feishu:token";
    }

    /// <inheritdoc />
    public Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
        => _innerStore.GetAccessTokenAsync(tokenType, cancellationToken);

    /// <inheritdoc />
    public Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
        => _innerStore.SetAccessTokenAsync(tokenType, accessToken, expiresInSeconds, cancellationToken);

    /// <inheritdoc />
    public Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
        => _innerStore.GetRefreshTokenAsync(tokenType, cancellationToken);

    /// <inheritdoc />
    public Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
        => _innerStore.SetRefreshTokenAsync(tokenType, refreshToken, cancellationToken);

    /// <inheritdoc />
    public Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
        => _innerStore.RemoveAsync(tokenType, cancellationToken);

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildUserAccessTokenKey(userId, tokenType);
        var value = await GetDatabase().StringGetAsync(key).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    public async Task SetAccessTokenAsync(string userId, string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
    {
        var key = BuildUserAccessTokenKey(userId, tokenType);
        await GetDatabase().StringSetAsync(key, accessToken, TimeSpan.FromSeconds(expiresInSeconds)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetRefreshTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildUserRefreshTokenKey(userId, tokenType);
        var value = await GetDatabase().StringGetAsync(key).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    public async Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        var key = BuildUserRefreshTokenKey(userId, tokenType);
        await GetDatabase().StringSetAsync(key, refreshToken, TimeSpan.FromDays(30)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        var db = GetDatabase();
        await db.KeyDeleteAsync(new RedisKey[] { BuildUserAccessTokenKey(userId, tokenType), BuildUserRefreshTokenKey(userId, tokenType) }).ConfigureAwait(false);
    }

    private IDatabase GetDatabase() => _redis.GetDatabase();

    private string BuildUserAccessTokenKey(string userId, string tokenType) => $"{_keyPrefix}:user:{userId}:{tokenType}:access";
    private string BuildUserRefreshTokenKey(string userId, string tokenType) => $"{_keyPrefix}:user:{userId}:{tokenType}:refresh";
}
