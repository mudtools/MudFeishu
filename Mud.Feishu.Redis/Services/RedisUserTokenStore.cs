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
public class RedisUserTokenStore : UserTokenStoreBase, IFeishuUserTokenStorePurge, IUserTokenStoreCompareExchange
{
    /// <summary>SCAN 类删除操作的批大小（R2-08）。</summary>
    private const int DeleteBatchSize = 500;

    /// <summary>
    /// U-7（上游 3.0.10 Batch 3）：用户维度 CAS 的 Lua 原子脚本。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>KEYS[1]</c> = 用户维度键；<c>ARGV[1]</c> = 期望令牌<b>明文</b>；<c>ARGV[2]</c> = 新存储值（空串 = compare-and-delete）；
    /// <c>ARGV[3]</c> = 新键 TTL（毫秒，&lt;= 0 表示不设 TTL）。
    /// </para>
    /// <para>
    /// <b>明文比较的原子化</b>：飞书管线按桥接器约定<b>直存编码值</b>（<c>{前导}|{明文令牌}</c>，
    /// v1 相对秒 / 旧绝对毫秒戳 / v2 信封三形态的前导段不同，但<b>首个 <c>|</c> 之后恒为令牌明文</b>），
    /// 故脚本按首个分隔符切出明文档与 ARGV[1] 整值比较 —— 解码不出 Lua，比较与写入在同一次
    /// 脚本执行内完成（无「读取 → C# 比较 → 写入」的竞态窗口）。
    /// </para>
    /// </remarks>
    private const string CompareExchangeScript = @"
local v = redis.call('GET', KEYS[1])
if not v then return 0 end
local i = string.find(v, '|', 1, true)
local plain = i and string.sub(v, i + 1) or ''
if plain ~= ARGV[1] then return 0 end
if ARGV[2] == '' then
  redis.call('DEL', KEYS[1])
  return 1
end
local ttl = tonumber(ARGV[3])
if ttl and ttl > 0 then
  redis.call('SET', KEYS[1], ARGV[2], 'PX', ttl)
else
  redis.call('SET', KEYS[1], ARGV[2])
end
return 1";

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
    /// TMA2-15 / P2-5 修复：过期戳已过时（expireMs &lt;= now）删除条目而非写 TTL=0（Redis TTL=0 等于永不过期）。
    /// C-2（3.0.3）：过期信息推导经 <see cref="TokenStoreHelper.TryDecodeExpiry"/> 双读——
    /// 新格式（相对秒）投影为过期时刻后换算 TTL；旧格式（绝对毫秒戳）原样判定（已过期删键行为不变）；
    /// 「无 TTL 信息」（前导数 &lt;= 0）回落 30 天（不再误判为已过期删键）。
    /// </remarks>
    public override async Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = BuildUserRefreshTokenKey(userId, tokenType);
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // TMA-22：尝试从编码值中解码过期时间戳。
        if (TokenStoreHelper.TryDecodeExpiry(refreshToken, out var expireMs))
        {
            // TMA2-15：过期戳已过时 → 删除条目，不写入（Redis TTL=0 会造成永不过期）。
            if (expireMs <= nowMs)
            {
                await _redis.GetDatabase().KeyDeleteAsync(key, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
                return;
            }

            var ttl = TimeSpan.FromMilliseconds(expireMs - nowMs);
            await _redis.GetDatabase().StringSetAsync(key, refreshToken, ttl, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            // 无编码过期戳 → 回落 30 天。
            await _redis.GetDatabase().StringSetAsync(key, refreshToken, TimeSpan.FromDays(30), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public override async Task RemoveAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _redis.GetDatabase().KeyDeleteAsync(new RedisKey[] { BuildUserAccessTokenKey(userId, tokenType), BuildUserRefreshTokenKey(userId, tokenType) }, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
    }

    // ──────────────────────────────────────────────
    //  U-7（上游 3.0.10 Batch 3）：用户维度原生 CAS —— Lua 原子实现
    // ──────────────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// 新值按 C-2 写侧形态编码（<see cref="FeishuTokenStoreCodec"/> 单写相对秒）；
    /// <paramref name="expiresInSeconds"/> &gt; 0 时同时设置键 TTL（与 <see cref="SetAccessTokenAsync"/> 同口径），
    /// ≤ 0 时编码为「无 TTL 信息」（前导 0）且不设键 TTL（契约：宁缺勿滥）。
    /// </remarks>
    public Task<bool> CompareExchangeAccessTokenAsync(
        string userId,
        string tokenType,
        string expectedToken,
        string? newToken,
        long expiresInSeconds = 0,
        CancellationToken cancellationToken = default)
    {
        var encoded = string.IsNullOrEmpty(newToken)
            ? null
            : FeishuTokenStoreCodec.Instance.Encode(newToken, expiresInSeconds > 0 ? expiresInSeconds : 0);

        return CompareExchangeAsync(
            BuildUserAccessTokenKey(userId, tokenType),
            expectedToken,
            encoded,
            expiresInSeconds > 0 ? expiresInSeconds * 1000L : 0L,
            cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// 刷新令牌无 TTL 入参：新值编码为「无 TTL 信息」（前导 0）并回落 30 天键 TTL ——
    /// 与 <see cref="SetRefreshTokenAsync"/> 的「无编码过期戳 ⇒ 回落 30 天」分支同口径。
    /// </remarks>
    public Task<bool> CompareExchangeRefreshTokenAsync(
        string userId,
        string tokenType,
        string expectedToken,
        string? newToken,
        CancellationToken cancellationToken = default)
    {
        var encoded = string.IsNullOrEmpty(newToken)
            ? null
            : FeishuTokenStoreCodec.Instance.Encode(newToken, 0);

        return CompareExchangeAsync(
            BuildUserRefreshTokenKey(userId, tokenType),
            expectedToken,
            encoded,
            encoded is null ? 0L : (long)TimeSpan.FromDays(30).TotalMilliseconds,
            cancellationToken);
    }

    /// <summary>
    /// 用户维度 CAS 共享核心：单次 Lua 脚本内完成「取存储值 → 明文比较 → 写入/删除」。
    /// </summary>
    /// <param name="key">用户维度键。</param>
    /// <param name="expectedToken">期望的令牌明文（契约要求非空）。</param>
    /// <param name="encodedNewValue">新存储值（已编码）；<c>null</c> = compare-and-delete。</param>
    /// <param name="ttlMs">新值键 TTL（毫秒）；≤ 0 = 不设 TTL。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>匹配且写入/删除成功返回 <c>true</c>；键不存在或不匹配返回 <c>false</c>。</returns>
    private async Task<bool> CompareExchangeAsync(
        string key,
        string expectedToken,
        string? encodedNewValue,
        long ttlMs,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 契约：expectedToken 不得为空（空期望值无比较意义，且会与"已过期占位"的空明文混淆）。
        if (string.IsNullOrEmpty(expectedToken))
            return false;

        var result = await _redis.GetDatabase().ScriptEvaluateAsync(
            CompareExchangeScript,
            new RedisKey[] { key },
            new RedisValue[] { expectedToken, encodedNewValue ?? string.Empty, ttlMs },
            flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);

        return (long)result == 1L;
    }

    /// <inheritdoc />
    /// <remarks>
    /// TMA2-02 / D8：键改用 TokenKeyBuilder 统一产出。
    /// </remarks>
    protected override string BuildUserAccessTokenKey(string userId, string tokenType)
        => TokenKeyBuilder.UserAccessKey(_keyPrefix, userId, tokenType);

    /// <inheritdoc />
    /// <remarks>
    /// TMA2-02 / D8：键改用 TokenKeyBuilder 统一产出。
    /// </remarks>
    protected override string BuildUserRefreshTokenKey(string userId, string tokenType)
        => TokenKeyBuilder.UserRefreshKey(_keyPrefix, userId, tokenType);

    /// <summary>
    /// TMR2-P1-2：指定用户的 SCAN pattern —— <c>TokenKeyBuilder</c> 字面量前缀 +
    /// Redis glob 字面量转义。
    /// </summary>
    /// <param name="userId">用户唯一标识符。</param>
    /// <returns>可直接用于 <c>Keys(pattern:)</c> 的 pattern。</returns>
    private string BuildUserScanPattern(string userId)
        => RedisGlobPattern.FromLiteralPrefix(TokenKeyBuilder.UserScanPatternLiteral(_keyPrefix, userId));

    /// <inheritdoc />
    /// <remarks>
    /// T-M2-6（R-10）：Cluster 化扫描——遍历全部主节点聚合 SCAN。
    /// TMA2-02 / D8：键模式与反向解析改用 TokenKeyBuilder 统一产出。
    /// </remarks>
    public override async Task<IEnumerable<string>> GetTokenTypesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var pattern = BuildUserScanPattern(userId);
        var tokenTypes = new List<string>();

        // R2-08：异步 SCAN 枚举（不再同步阻塞）
        foreach (var server in RedisStoreHelper.GetServers(_redis))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: 250, flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keyStr = key.ToString();
                if (TokenKeyBuilder.TryParseUserTokenType(keyStr, _keyPrefix, userId, out var tokenType) && tokenType != null)
                    tokenTypes.Add(tokenType);
            }
        }

        return tokenTypes.Distinct();
    }

    /// <inheritdoc />
    /// <remarks>
    /// T-M2-6（R-10）：Cluster 化扫描——遍历全部主节点聚合 SCAN + 删除。
    /// 部分节点失败时已删除的键不可回滚（声明"部分失败"语义）。
    /// TMA2-02 / D8：键模式改用 TokenKeyBuilder 统一产出。
    /// </remarks>
    public override async Task ClearUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var pattern = BuildUserScanPattern(userId);
        var db = _redis.GetDatabase();

        // R2-08：异步 SCAN + 分批删除（500/批）
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
                    await db.KeyDeleteAsync(batch.ToArray(), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await db.KeyDeleteAsync(batch.ToArray(), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// TMF-01（D10）：凭据变更清库的全用户删除能力——SCAN 全用户键模式
    /// （<see cref="TokenKeyBuilder.AllUsersScanPatternLiteral"/>，通配 userId/tokenType/access|refresh）
    /// 逐键删除，Cluster 化遍历模式与 <see cref="ClearUserAsync"/> 一致。
    /// 与并发写入之间存在固有残余窗口（SCAN 语义）。
    /// TMR2-P1-2：pattern 经 <see cref="RedisGlobPattern"/> 字面量转义（否则含 <c>:</c>/<c>\</c> 的 appKey 永不命中）。
    /// </remarks>
    public async Task ClearAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var pattern = RedisGlobPattern.FromLiteralPrefix(TokenKeyBuilder.AllUsersScanPatternLiteral(_keyPrefix));
        var db = _redis.GetDatabase();

        // R2-08：异步 SCAN + 分批删除（500/批）
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
                    await db.KeyDeleteAsync(batch.ToArray(), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await db.KeyDeleteAsync(batch.ToArray(), flags: RedisStoreHelper.ToCommandFlags(cancellationToken)).ConfigureAwait(false);
            }
        }
    }
}
