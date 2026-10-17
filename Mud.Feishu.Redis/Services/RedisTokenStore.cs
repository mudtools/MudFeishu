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
public class RedisTokenStore : ITokenStore, ITokenStoreAppPurge
{
    /// <summary>SCAN 类删除操作的批大小（R2-08）。</summary>
    private const int DeleteBatchSize = 500;

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
        // logger 兜底 NullLogger，防止无日志宿主时抛 ArgumentNullException
        _logger = logger ?? NullLogger<RedisTokenStore>.Instance;
        _keyPrefix = keyPrefix ?? "feishu:token";
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // 令牌读写同样上报指标（本类型不包装异常，失败按原始异常分类）
        var startTimestamp = RedisMetricsHelper.Begin();
        try
        {
            var key = BuildAccessTokenKey(tokenType);
            var value = await GetDatabase().StringGetAsync(key, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
            Record(FeishuMetrics.RedisCommands.TokenGet, FeishuMetrics.RedisOutcomes.Success, startTimestamp);
            return value.HasValue ? value.ToString() : null;
        }
        catch (RedisException ex)
        {
            Record(FeishuMetrics.RedisCommands.TokenGet, RedisMetricsHelper.FromException(ex), startTimestamp);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
    {
        // 非法过期时间显式抛异常，而非静默不落库或被服务端拒绝
        if (expiresInSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(expiresInSeconds),
                "令牌过期时间必须为正数（秒），实际值: " + expiresInSeconds);

        cancellationToken.ThrowIfCancellationRequested();
        var startTimestamp = RedisMetricsHelper.Begin();
        try
        {
            var key = BuildAccessTokenKey(tokenType);
            await GetDatabase().StringSetAsync(key, accessToken, TimeSpan.FromSeconds(expiresInSeconds), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
            Record(FeishuMetrics.RedisCommands.TokenSet, FeishuMetrics.RedisOutcomes.Success, startTimestamp);
        }
        catch (RedisException ex)
        {
            Record(FeishuMetrics.RedisCommands.TokenSet, RedisMetricsHelper.FromException(ex), startTimestamp);
            throw;
        }
    }

    /// <summary>上报一次令牌存储操作。</summary>
    private static void Record(string command, string outcome, long startTimestamp)
        => RedisMetricsHelper.Record(command, FeishuMetrics.DedupTypes.Token, outcome, startTimestamp);

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
    /// refresh token 存储 TTL 优先取编码值中的过期时间，缺失时才回落 30 天，
    /// 使 store TTL 与业务过期语义一致。
    /// 过期戳已过时（expireMs &lt;= now）删除条目，而非写 TTL=0（Redis TTL=0 等于永不过期）。
    /// 过期信息推导经 <see cref="TokenStoreHelper.TryDecodeExpiry"/> 双读：
    /// 新格式（相对秒）投影为过期时刻后换算 TTL；旧格式（绝对毫秒戳）原样判定（已过期删键行为不变）；
    /// 「无 TTL 信息」（前导数 &lt;= 0）回落 30 天，不得误判为已过期删键。
    /// </remarks>
    public async Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildRefreshTokenKey(tokenType);
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 尝试从编码值中解码过期时间戳。
        if (TokenStoreHelper.TryDecodeExpiry(refreshToken, out var expireMs))
        {
            // 过期戳已过时 → 删除条目，不写入（Redis TTL=0 会造成永不过期）。
            if (expireMs <= nowMs)
            {
                await GetDatabase().KeyDeleteAsync(key, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
                return;
            }

            var ttl = TimeSpan.FromMilliseconds(expireMs - nowMs);
            await GetDatabase().StringSetAsync(key, refreshToken, ttl, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            // 无编码过期戳 → 回落 30 天。
            await GetDatabase().StringSetAsync(key, refreshToken, TimeSpan.FromDays(30), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        }
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
    /// Cluster 化扫描——遍历全部主节点聚合 SCAN，避免 Cluster 下漏列。
    /// 键模式统一经 TokenKeyBuilder 产出。
    /// </remarks>
    public async Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
    {
        var pattern = BuildTenantScanPattern();
        var tokenTypes = new List<string>();

        // 遍历全部主节点，改用异步 SCAN 枚举（避免同步阻塞一页 5s）
        foreach (var server in RedisStoreHelper.GetServers(_redis))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: 250, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keyStr = key.ToString();
                if (TokenKeyBuilder.TryParseTenantTokenType(keyStr, _keyPrefix, out var tokenType) && tokenType != null)
                    tokenTypes.Add(tokenType);
            }
        }

        // SCAN 规模可观测
        RedisMetricsHelper.RecordScan(FeishuMetrics.RedisCommands.TokenGetTypes, tokenTypes.Count, deleted: false);
        return tokenTypes.Distinct();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Cluster 化扫描——遍历全部主节点聚合 SCAN，避免 Cluster 下漏列。
    /// 会删除该前缀下全部令牌键（<c>{prefix}:*:access</c>、<c>{prefix}:*:refresh</c>、
    /// <c>{prefix}:user:*:*</c>），即租户与用户持久化令牌一并清除，语义与 Memory 后端
    /// （<c>FeishuTokenStore.ClearAsync</c> + <c>IFeishuUserTokenStorePurge.ClearAllUsersAsync</c>）一致。
    /// 部分节点失败时已删除的键不可回滚（声明"部分失败"语义）。
    /// </remarks>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
        => await ClearCoreAsync(cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 清库核心（<see cref="ClearAsync"/> 与 <see cref="PurgeAppAsync"/> 共用）：
    /// Cluster 化 SCAN + 分批删除，返回实际删除键数。
    /// </summary>
    private async Task<long> ClearCoreAsync(CancellationToken cancellationToken)
    {
        var pattern = BuildTenantScanPattern();
        var db = GetDatabase();
        var deletedCount = 0L;

        // 遍历全部主节点，异步 SCAN + 分批删除（500/批，减少 RTT 与"已删一半后异常"的窗口）
        foreach (var server in RedisStoreHelper.GetServers(_redis))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = new List<RedisKey>(DeleteBatchSize);
            await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: 250, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                batch.Add(key);

                if (batch.Count >= DeleteBatchSize)
                {
                    deletedCount += await db.KeyDeleteAsync(batch.ToArray(), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                deletedCount += await db.KeyDeleteAsync(batch.ToArray(), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
            }
        }

        // 删除规模可观测（"清空却删 0 个键"类缺陷一眼可辨）
        RedisMetricsHelper.RecordScan(FeishuMetrics.RedisCommands.TokenClear, deletedCount, deleted: true);
        return deletedCount;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="ITokenStoreAppPurge"/> 实现 ——
    /// 快照枚举本实例键空间（与 <see cref="ClearAsync"/> 同一 SCAN pattern，物理键含
    /// access / refresh / user 维度），<see cref="GetTokenTypesAsync"/> 的 tokenType 归一化不适用本契约
    /// （官方契约约定返回物理键）。跨节点去重防 SCAN 重复面。
    /// </remarks>
    public async Task<IReadOnlyCollection<string>> EnumerateKeysAsync(string appKey, CancellationToken cancellationToken = default)
    {
        var pattern = BuildTenantScanPattern();
        var keys = new List<string>();

        foreach (var server in RedisStoreHelper.GetServers(_redis))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: 250, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                keys.Add(key.ToString());
            }
        }

        return keys.Distinct(StringComparer.Ordinal).ToList();
    }

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="ITokenStoreAppPurge"/> 实现 ——
    /// 本实例命名空间已由 KeyPrefix 绑定单应用（per-app 工厂 <c>PerAppRedisTokenStoreFactory</c>），
    /// <paramref name="appKey"/> 参数按官方契约「实现方处置」以实例键空间为边界（与
    /// <see cref="ClearAsync"/> 语义逐位一致），不参与过滤。节点失败直接上抛（契约：不得静默半删）。
    /// </remarks>
    public async Task<int> PurgeAppAsync(string appKey, CancellationToken cancellationToken = default)
    {
        var deleted = await ClearCoreAsync(cancellationToken).ConfigureAwait(false);
        return checked((int)deleted);
    }

    private IDatabase GetDatabase() => _redis.GetDatabase();

    // 令牌键改用 TokenKeyBuilder 统一产出，与 Memory 路径逐字节一致。
    private string BuildAccessTokenKey(string tokenType) => TokenKeyBuilder.TenantAccessKey(_keyPrefix, tokenType);
    private string BuildRefreshTokenKey(string tokenType) => TokenKeyBuilder.TenantRefreshKey(_keyPrefix, tokenType);

    /// <summary>
    /// 租户 SCAN pattern —— 由 <c>TokenKeyBuilder</c> 产出字面量前缀后，
    /// 再经 Redis glob 字面量转义（<c>\</c> 与 <c>*?[]</c>），否则含转义符/通配符的 appKey 下永不命中。
    /// </summary>
    private string BuildTenantScanPattern()
        => RedisGlobPattern.FromLiteralPrefix(TokenKeyBuilder.TenantScanPatternLiteral(_keyPrefix));
}
