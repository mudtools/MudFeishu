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
        // R-25：logger 兜底 NullLogger，防止无日志宿主时抛 ArgumentNullException
        _logger = logger ?? NullLogger<RedisTokenStore>.Instance;
        _keyPrefix = keyPrefix ?? "feishu:token";
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildAccessTokenKey(tokenType);
        var value = await GetDatabase().StringGetAsync(key, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    public async Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
    {
        // T-M2-11 / R-23：非法过期时间显式抛异常，而非静默不落库或被服务端拒绝
        if (expiresInSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(expiresInSeconds),
                "令牌过期时间必须为正数（秒），实际值: " + expiresInSeconds);

        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildAccessTokenKey(tokenType);
        await GetDatabase().StringSetAsync(key, accessToken, TimeSpan.FromSeconds(expiresInSeconds), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildRefreshTokenKey(tokenType);
        var value = await GetDatabase().StringGetAsync(key, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// TMA-22 修复：refresh token 存储 TTL 由硬编码 30 天改为优先使用编码值中的过期时间，
    /// 缺失时才回落 30 天。使 store TTL 与业务过期语义一致。
    /// </remarks>
    public async Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildRefreshTokenKey(tokenType);
        // TMA-22：尝试从编码值中解码过期时间戳。
        var ttl = TokenStoreHelper.TryDecodeExpiry(refreshToken, out var expireMs)
            ? TimeSpan.FromMilliseconds(Math.Max(0, expireMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
            : TimeSpan.FromDays(30);
        await GetDatabase().StringSetAsync(key, refreshToken, ttl, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var db = GetDatabase();
        await db.KeyDeleteAsync(new RedisKey[] { BuildAccessTokenKey(tokenType), BuildRefreshTokenKey(tokenType) }, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// T-M2-6（R-10）：Cluster 化扫描——遍历全部主节点聚合 SCAN，避免 Cluster 下漏列。
    /// </remarks>
    public async Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
    {
        // T-M3-1：pattern 改用 RedisKeyBuilder 构造前缀部分（R-01 护栏 + R-20 转义一致性）
        var pattern = RedisKeyBuilder.Combine(_keyPrefix) + ":*:access";
        var tokenTypes = new List<string>();
        // TM-04 修复：键格式为 {prefix}:{tokenType}:access，其中 tokenType 可能含 ":"（如 "tenant:cli_xxx"）。
        var prefixWithColon = RedisKeyBuilder.Combine(_keyPrefix) + ":";
        const string accessSuffix = ":access";

        // T-M2-6：遍历全部主节点
        foreach (var server in RedisStoreHelper.GetServers(_redis))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var keys = server.Keys(pattern: pattern, pageSize: 250, flags: RedisStoreHelper.ToCommandFlags(cancellationToken));

            foreach (var key in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keyStr = key.ToString();
                if (!keyStr.StartsWith(prefixWithColon, StringComparison.Ordinal) || !keyStr.EndsWith(accessSuffix, StringComparison.Ordinal))
                    continue;

                var middle = keyStr.Substring(prefixWithColon.Length, keyStr.Length - prefixWithColon.Length - accessSuffix.Length);
                if (middle.Length > 0)
                    tokenTypes.Add(middle);
            }
        }

        return tokenTypes.Distinct();
    }

    /// <inheritdoc />
    /// <remarks>
    /// T-M2-6（R-10）：Cluster 化扫描——遍历全部主节点聚合 SCAN + 删除。
    /// 此方法会删除该前缀下全部令牌键（含 <c>{prefix}:*:access</c>、<c>{prefix}:*:refresh</c>、
    /// <c>{prefix}:user:*:*</c>），即租户令牌与用户令牌一并清除。
    /// 部分节点失败时已删除的键不可回滚（声明"部分失败"语义）。
    /// </remarks>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        // T-M3-1：pattern 改用 RedisKeyBuilder 构造前缀部分（R-01 护栏）
        var pattern = RedisKeyBuilder.Combine(_keyPrefix) + ":*";
        var db = GetDatabase();

        // T-M2-6：遍历全部主节点
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

    private IDatabase GetDatabase() => _redis.GetDatabase();

    // T-M3-1：令牌键改用 RedisKeyBuilder 统一构造（R-20/R-21 转义 + 长度护栏）
    private string BuildAccessTokenKey(string tokenType) => RedisKeyBuilder.Combine(_keyPrefix, tokenType, "access");
    private string BuildRefreshTokenKey(string tokenType) => RedisKeyBuilder.Combine(_keyPrefix, tokenType, "refresh");
}
