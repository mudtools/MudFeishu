// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 基于 Redis 的飞书令牌存储适配器
/// </summary>
/// <remarks>
/// 实现 Mud.HttpUtils v2.0 的 ITokenStore 接口，将令牌持久化到 Redis。
/// 适用于多实例分布式部署场景，确保各实例共享令牌状态。
/// </remarks>
public class RedisTokenStore : ITokenStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisTokenStore> _logger;
    private readonly string _keyPrefix;

    /// <summary>
    /// 初始化 RedisTokenStore 实例
    /// </summary>
    /// <param name="redis">Redis 连接复用器</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="keyPrefix">Redis 键前缀，默认 "feishu:token"</param>
    public RedisTokenStore(
        IConnectionMultiplexer redis,
        ILogger<RedisTokenStore> logger,
        string keyPrefix = "feishu:token")
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyPrefix = keyPrefix ?? "feishu:token";
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildAccessTokenKey(tokenType);
        var value = await GetDatabase().StringGetAsync(key).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    public async Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
    {
        var key = BuildAccessTokenKey(tokenType);
        await GetDatabase().StringSetAsync(key, accessToken, TimeSpan.FromSeconds(expiresInSeconds)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildRefreshTokenKey(tokenType);
        var value = await GetDatabase().StringGetAsync(key).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    public async Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        var key = BuildRefreshTokenKey(tokenType);
        await GetDatabase().StringSetAsync(key, refreshToken, TimeSpan.FromDays(30)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        var db = GetDatabase();
        await db.KeyDeleteAsync(new RedisKey[] { BuildAccessTokenKey(tokenType), BuildRefreshTokenKey(tokenType) }).ConfigureAwait(false);
    }

    private IDatabase GetDatabase() => _redis.GetDatabase();

    private string BuildAccessTokenKey(string tokenType) => $"{_keyPrefix}:{tokenType}:access";
    private string BuildRefreshTokenKey(string tokenType) => $"{_keyPrefix}:{tokenType}:refresh";
}
