// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 基于 IMemoryCache 的飞书令牌存储适配器
/// </summary>
/// <remarks>
/// 实现 Mud.HttpUtils v2.0 的 ITokenStore 接口，将令牌持久化到 IMemoryCache。
/// 适用于单实例部署场景，应用重启后令牌会丢失。
/// 对于多实例分布式部署，应使用 RedisTokenStore 替代。
/// </remarks>
public class FeishuTokenStore : ITokenStore
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _tokenTypes = new();

    /// <summary>
    /// 初始化 FeishuTokenStore 实例
    /// </summary>
    /// <param name="cache">内存缓存实例</param>
    public FeishuTokenStore(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public Task<string?> GetAccessTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildAccessTokenKey(tokenType);
        var token = _cache.Get<string>(key);
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task SetAccessTokenAsync(string tokenType, string accessToken, long expiresInSeconds, CancellationToken cancellationToken = default)
    {
        _tokenTypes.TryAdd(tokenType, 0);
        var key = BuildAccessTokenKey(tokenType);
        _cache.Set(key, accessToken, TimeSpan.FromSeconds(expiresInSeconds));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetRefreshTokenAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        var key = BuildRefreshTokenKey(tokenType);
        var token = _cache.Get<string>(key);
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task SetRefreshTokenAsync(string tokenType, string refreshToken, CancellationToken cancellationToken = default)
    {
        var key = BuildRefreshTokenKey(tokenType);
        _cache.Set(key, refreshToken, TimeSpan.FromDays(30));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string tokenType, CancellationToken cancellationToken = default)
    {
        _tokenTypes.TryRemove(tokenType, out _);
        _cache.Remove(BuildAccessTokenKey(tokenType));
        _cache.Remove(BuildRefreshTokenKey(tokenType));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetTokenTypesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tokenTypes.Keys.AsEnumerable());
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        foreach (var tokenType in _tokenTypes.Keys)
        {
            _cache.Remove(BuildAccessTokenKey(tokenType));
            _cache.Remove(BuildRefreshTokenKey(tokenType));
        }

        _tokenTypes.Clear();
        return Task.CompletedTask;
    }

    private static string BuildAccessTokenKey(string tokenType) => $"feishu:token:{tokenType}:access";
    private static string BuildRefreshTokenKey(string tokenType) => $"feishu:token:{tokenType}:refresh";
}

