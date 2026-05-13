// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 基于 IMemoryCache 的飞书用户令牌存储适配器
/// </summary>
/// <remarks>
/// 实现 Mud.HttpUtils v2.0 的 IUserTokenStore 接口，将用户令牌持久化到 IMemoryCache。
/// 支持按用户标识隔离令牌数据。
/// ITokenStore 的方法通过 UserTokenStoreBase 基类委托给内部 FeishuTokenStore 实现。
/// </remarks>
public class FeishuUserTokenStore : UserTokenStoreBase
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userTokenTypes = new();

    /// <summary>
    /// 初始化 FeishuUserTokenStore 实例
    /// </summary>
    /// <param name="innerStore">内部令牌存储实例，用于 ITokenStore 方法委托</param>
    /// <param name="cache">内存缓存实例</param>
    public FeishuUserTokenStore(FeishuTokenStore innerStore, IMemoryCache cache)
        : base(innerStore)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

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
    public override Task SetRefreshTokenAsync(string userId, string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        var key = BuildUserRefreshTokenKey(userId, tokenType);
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

    public override Task<IEnumerable<string>> GetTokenTypesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_userTokenTypes.TryGetValue(userId, out var tokenTypes))
            return Task.FromResult(tokenTypes.Keys.AsEnumerable());

        return Task.FromResult(Enumerable.Empty<string>());
    }

    public override Task ClearUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_userTokenTypes.TryRemove(userId, out var tokenTypes))
        {
            foreach (var tokenType in tokenTypes.Keys)
            {
                _cache.Remove(BuildUserAccessTokenKey(userId, tokenType));
                _cache.Remove(BuildUserRefreshTokenKey(userId, tokenType));
            }
        }

        return Task.CompletedTask;
    }

    private void TrackUserTokenType(string userId, string tokenType)
    {
        var userTokens = _userTokenTypes.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        userTokens.TryAdd(tokenType, 0);
    }

    private void UntrackUserTokenType(string userId, string tokenType)
    {
        if (_userTokenTypes.TryGetValue(userId, out var userTokens))
            userTokens.TryRemove(tokenType, out _);
    }
}
