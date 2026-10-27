// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 基于 IMemoryCache 的飞书用户令牌存储适配器
/// </summary>
/// <remarks>
/// 实现 Mud.HttpUtils v2.0 的 IUserTokenStore 接口，将用户令牌持久化到 IMemoryCache。
/// 支持按用户标识隔离令牌数据。
/// ITokenStore 的方法通过 UserTokenStoreBase 基类委托给内部 FeishuTokenStore 实现。
/// </remarks>
public class FeishuUserTokenStore : UserTokenStoreBase, IFeishuUserTokenStorePurge, IUserTokenStoreAccessTokenInvalidation
{
    private readonly IMemoryCache _cache;
    private readonly string _appKey;

    // 跨实例共享记账（凭据变更清库需要）。静态注册表按 KeyPrefix → userId → tokenType 隔离：
    // 任意实例写入均注册到共享表，ClearAllUsersAsync/ClearUserAsync 由此可删除
    // 「任意实例曾写入」的全部用户令牌键——工厂每次 Create 新实例不再导致清库 no-op。
    // 重复/过期注册只导致对不存在键的无害 no-op Remove。
    private static readonly ConcurrentDictionary<
        string,                        // KeyPrefix
        ConcurrentDictionary<string,   // userId
            ConcurrentDictionary<string, byte>>> SharedUserTypes = new();  // tokenType

    private ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> SharedUsers
        => SharedUserTypes.GetOrAdd(KeyPrefix, _ => new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>());

    /// <summary>
    /// 初始化 FeishuUserTokenStore 实例（使用默认 AppKey）
    /// </summary>
    /// <param name="innerStore">内部令牌存储实例，用于 ITokenStore 方法委托</param>
    /// <param name="cache">内存缓存实例</param>
    public FeishuUserTokenStore(FeishuTokenStore innerStore, IMemoryCache cache)
        : this(innerStore, cache, "default")
    {
    }

    /// <summary>
    /// 初始化 FeishuUserTokenStore 实例（指定 AppKey 用于多应用隔离）
    /// </summary>
    /// <param name="innerStore">内部令牌存储实例，用于 ITokenStore 方法委托</param>
    /// <param name="cache">内存缓存实例</param>
    /// <param name="appKey">应用唯一标识，用于构建隔离的缓存键</param>
    public FeishuUserTokenStore(FeishuTokenStore innerStore, IMemoryCache cache, string appKey)
        : base(innerStore)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("AppKey 不能为空", nameof(appKey));
        _appKey = appKey;
    }

    /// <summary>
    /// 键前缀含 AppKey 维度，确保多应用场景下用户令牌互不覆盖。
    /// </summary>
    // 前缀构造委派 TokenKeyBuilder.BuildKeyPrefix，与 Redis 路径一致。
    protected override string KeyPrefix => TokenKeyBuilder.BuildKeyPrefix(_appKey);

    /// <inheritdoc />
    public override Task<string?> GetAccessTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildUserAccessTokenKey(userId, tokenType);
        var token = _cache.Get<string>(key);
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public override Task SetAccessTokenAsync(string userId, string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
    {
        TrackUserTokenType(userId, tokenType);
        var key = BuildUserAccessTokenKey(userId, tokenType);
        _cache.Set(key, accessToken, TimeSpan.FromSeconds(expiresInSeconds));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<string?> GetRefreshTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildUserRefreshTokenKey(userId, tokenType);
        var token = _cache.Get<string>(key);
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    /// <remarks>
    /// refresh token 存储 TTL 优先使用编码值中的过期时间，缺失时回落 30 天。
    /// IMemoryCache 场景下本方法不接收过期信息，保持 30 天默认（Redis 路径在 RedisUserTokenStore 中处理）。
    /// Memory 路径仅覆盖本进程；过期语义由消费侧校验（RefreshTokenExpireTime）。
    /// </remarks>
    public override Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        // refresh 写入同样必须记账。若仅写 refresh 而从未写 access
        // （如 PersistUserTokenAsync 在 access 剩余秒数 ≤ 0 时只持久化 refresh，
        // 或宿主经 StoreUserTokenAsync 传入已过期的 access），该 (userId, tokenType)
        // 不在共享记账中 → ClearAllUsersAsync 无法删除其 refresh 键（清库盲区）。
        TrackUserTokenType(userId, tokenType);
        var key = BuildUserRefreshTokenKey(userId, tokenType);
        // 保持 30 天默认（IMemoryCache 路径无过期信息可用）。
        _cache.Set(key, refreshToken, TimeSpan.FromDays(30));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task RemoveAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        UntrackUserTokenType(userId, tokenType);
        _cache.Remove(BuildUserAccessTokenKey(userId, tokenType));
        _cache.Remove(BuildUserRefreshTokenKey(userId, tokenType));
        return Task.CompletedTask;
    }

    /// <summary>
    /// 仅删除访问令牌键，保留 refresh 键与记账（refresh 仍是唯一的续期凭据）。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="tokenType">令牌类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task InvalidateAccessTokenAsync(string userId, string tokenType, CancellationToken cancellationToken = default)
    {
        _cache.Remove(BuildUserAccessTokenKey(userId, tokenType));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// 返回本用户已记账的 tokenType（跨实例共享记账）。
    /// 实例重建后旧实例写入的记账仍然可见，保证凭据变更清库的完整性。
    /// </remarks>
    public override Task<IEnumerable<string>> GetTokenTypesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (SharedUsers.TryGetValue(userId, out var tokenTypes))
            return Task.FromResult(tokenTypes.Keys.AsEnumerable());

        return Task.FromResult(Enumerable.Empty<string>());
    }

    /// <inheritdoc />
    /// <remarks>
    /// 删除该用户全部已记账令牌键，并修剪外层 (KeyPrefix → userId) 条目。
    /// <para>
    /// 外层条目在内层为空时修剪：保留空条目会使每历史用户 ~64B 空字典长期驻留。
    /// 修剪与 <see cref="TrackUserTokenType"/> 的 GetOrAdd 并发交错的残余窗口被容忍：
    /// Track 侧重建出的孤儿注册最坏导致一次对不存在键的无害 no-op Remove，
    /// 且 Memory 后端键自带 TTL（access 按 expire、refresh ≤30d），不产生无界泄漏。
    /// </para>
    /// </remarks>
    public override Task ClearUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (SharedUsers.TryGetValue(userId, out var tokenTypes))
        {
            foreach (var tokenType in tokenTypes.Keys)
            {
                _cache.Remove(BuildUserAccessTokenKey(userId, tokenType));
                _cache.Remove(BuildUserRefreshTokenKey(userId, tokenType));
            }

            // 修剪空外层条目（先清键再修剪，修剪与 Track 重建的竞态按上述容忍）。
            SharedUsers.TryRemove(userId, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// 清除「任意实例曾写入本 KeyPrefix」的全部用户令牌键。
    /// 枚举共享记账（快照语义：枚举期间新写入的 (userId, tokenType) 由残余窗口覆盖，
    /// 与 Redis SCAN 语义一致），逐键删除后仅清空表内容。
    /// </remarks>
    public Task ClearAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = SharedUsers;
        foreach (var pair in users)
        {
            var userId = pair.Key;
            var tokenTypes = pair.Value;
            foreach (var tokenType in tokenTypes.Keys)
            {
                _cache.Remove(BuildUserAccessTokenKey(userId, tokenType));
                _cache.Remove(BuildUserRefreshTokenKey(userId, tokenType));
            }
        }

        users.Clear();
        return Task.CompletedTask;
    }

    // 用户令牌键由 TokenKeyBuilder 统一产出，与 Redis 路径一致。
    /// <summary>
    /// 构建用户访问令牌在存储中的键（统一由 TokenKeyBuilder 产出）。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="tokenType">令牌类型</param>
    /// <returns>存储键</returns>
    protected override string BuildUserAccessTokenKey(string userId, string tokenType)
        => TokenKeyBuilder.UserAccessKey(KeyPrefix, userId, tokenType);

    /// <summary>
    /// 构建用户刷新令牌在存储中的键（统一由 TokenKeyBuilder 产出）。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="tokenType">令牌类型</param>
    /// <returns>存储键</returns>
    protected override string BuildUserRefreshTokenKey(string userId, string tokenType)
        => TokenKeyBuilder.UserRefreshKey(KeyPrefix, userId, tokenType);

    private void TrackUserTokenType(string userId, string tokenType)
    {
        var userTokens = SharedUsers.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        userTokens.TryAdd(tokenType, 0);
    }

    private void UntrackUserTokenType(string userId, string tokenType)
    {
        if (!SharedUsers.TryGetValue(userId, out var userTokens))
            return;

        userTokens.TryRemove(tokenType, out _);

        // 内层为空时修剪外层条目（每历史用户 ~64B 空字典驻留 → 0）。
        // 与 TrackUserTokenType 的 GetOrAdd 并发交错被容忍：Track 侧重建出的孤儿注册
        // 最坏导致一次对不存在键的无害 no-op Remove。
        if (userTokens.IsEmpty)
            SharedUsers.TryRemove(userId, out _);
    }
}
