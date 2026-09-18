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
public class FeishuUserTokenStore : UserTokenStoreBase, IFeishuUserTokenStorePurge
{
    private readonly IMemoryCache _cache;
    private readonly string _appKey;

    // TMF-01：跨实例共享记账（D10）。静态注册表按 KeyPrefix → userId → tokenType 隔离：
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
    /// TMA2-02 / D8：键前缀含 AppKey 维度，确保多应用场景下用户令牌互不覆盖。
    /// </summary>
    protected override string KeyPrefix => $"feishu:{_appKey}:token";

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
    /// TMA-22 修复：refresh token 存储 TTL 由硬编码 30 天改为优先使用编码值中的过期时间，
    /// 缺失时才回落 30 天。但 IMemoryCache 场景下此方法不接收过期信息，保持 30 天默认。
    /// Redis 路径在 RedisUserTokenStore 中处理。
    /// TMA2-15 / P2-5：Memory 路径保留 30 天默认。仅本进程；过期语义由消费侧校验
    /// （TMA2-06 的 RefreshTokenExpireTime 校验）。
    /// </remarks>
    public override Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        var key = BuildUserRefreshTokenKey(userId, tokenType);
        // TMA-22: 保持 30 天默认（IMemoryCache 路径无过期信息可用）。
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

    /// <inheritdoc />
    /// <remarks>
    /// TMA-16 / P2-3 修复：仅返回本进程已知类型。
    /// TMF-01：记账改为按 KeyPrefix 的跨实例共享表，语义从「本实例已知」放宽为
    /// 「本进程本前缀已知」，保证凭据变更清库（D10）的完整性。
    /// </remarks>
    public override Task<IEnumerable<string>> GetTokenTypesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (SharedUsers.TryGetValue(userId, out var tokenTypes))
            return Task.FromResult(tokenTypes.Keys.AsEnumerable());

        return Task.FromResult(Enumerable.Empty<string>());
    }

    /// <inheritdoc />
    public override Task ClearUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (SharedUsers.TryRemove(userId, out var tokenTypes))
        {
            foreach (var tokenType in tokenTypes.Keys)
            {
                _cache.Remove(BuildUserAccessTokenKey(userId, tokenType));
                _cache.Remove(BuildUserRefreshTokenKey(userId, tokenType));
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// TMF-01（D10）：清除「任意实例曾写入本 KeyPrefix」的全部用户令牌键。
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

    // TMA2-02 / D8：用户令牌键由 TokenKeyBuilder 统一产出，与 Redis 路径逐字节一致。
    /// <summary>
    /// 构建用户访问令牌在存储中的键（D8 契约：统一由 TokenKeyBuilder 产出）。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="tokenType">令牌类型</param>
    /// <returns>存储键</returns>
    protected override string BuildUserAccessTokenKey(string userId, string tokenType)
        => TokenKeyBuilder.UserAccessKey(KeyPrefix, userId, tokenType);

    /// <summary>
    /// 构建用户刷新令牌在存储中的键（D8 契约：统一由 TokenKeyBuilder 产出）。
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
        if (SharedUsers.TryGetValue(userId, out var userTokens))
            userTokens.TryRemove(tokenType, out _);
    }
}
