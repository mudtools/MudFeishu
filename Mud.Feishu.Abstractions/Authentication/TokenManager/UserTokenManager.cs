// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Exceptions;

namespace Mud.Feishu.TokenManager;

/// <summary>
/// 用户令牌管理器
/// </summary>
/// <remarks>
/// 负责用户访问令牌（User Access Token）的获取、缓存和管理。
/// 用户令牌用于用户级别的权限验证，通过授权码（Code）换取用户令牌。
/// 继承 Mud.HttpUtils v2.0 的 UserTokenManagerBase，获得内置并发安全、自动清理、IMemoryCache 缓存等能力。
/// </remarks>
internal class UserTokenManager : UserTokenManagerBase, IFeishuUserTokenManager
{
    private readonly ConcurrentDictionary<string, UserTokenInfo> _userTokenLookup = new();
    private readonly ICurrentUserContext? _currentUserContext;
    private readonly IFeishuAuthentication _authenticationApi;
    private readonly FeishuAppConfig _options;
    private readonly ILogger<UserTokenManager> _logger;

    /// <summary>
    /// 初始化 UserTokenManager 实例
    /// </summary>
    /// <param name="currentUserContext">当前用户上下文（可选）</param>
    /// <param name="authenticationApi">飞书认证API接口</param>
    /// <param name="options">飞书配置选项</param>
    /// <param name="logger">日志记录器</param>
    public UserTokenManager(
        ICurrentUserContext? currentUserContext,
        IFeishuAuthentication authenticationApi,
        IOptions<FeishuAppConfig> options,
        ILogger<UserTokenManager> logger)
    {
        _currentUserContext = currentUserContext;
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUserContext == null)
            throw new InvalidOperationException("CurrentUserContext is not available. Cannot get user token.");

        if (!_currentUserContext.IsAuthenticated)
            throw new InvalidOperationException("Current user is not authenticated. Cannot get user token.");

        return await GetOrRefreshTokenAsync(_currentUserContext.OpenId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to obtain user token.");
    }

    /// <inheritdoc />
    public override async Task<string?> GetTokenAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("OpenId cannot be null or empty.", nameof(userId));

        return await GetOrRefreshTokenAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<UserTokenInfo?> GetUserTokenWithCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Code cannot be null or empty.", nameof(code));

        if (string.IsNullOrEmpty(redirectUri))
            throw new ArgumentException("RedirectUri cannot be null or empty.", nameof(redirectUri));

        _logger.LogInformation("Exchanging code for user token");

        var credentials = new OAuthTokenRequest
        {
            GrantType = "authorization_code",
            ClientId = _options.AppId,
            ClientSecret = _options.AppSecret,
            Code = code,
            RedirectUri = redirectUri
        };

        var res = await _authenticationApi.GetOAuthenAccessTokenAsync(credentials, cancellationToken);

        if (res == null || res.Code != 0)
        {
            throw new FeishuException(res?.Code ?? 500, $"获取 UserAccessToken 失败: {res?.Msg ?? "返回结果为null"}");
        }

        if (string.IsNullOrEmpty(res.AccessToken))
        {
            throw new FeishuException(443, "获取 UserAccessToken 失败: AccessToken为空");
        }

        var tokenInfo = new UserTokenInfo
        {
            UserId = string.Empty,
            AccessToken = res.AccessToken,
            RefreshToken = res.RefreshToken,
            AccessTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ((res.ExpiresIn > 0 ? res.ExpiresIn : 7200) * 1000L),
            RefreshTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ((res.RefreshTokenExpiresIn > 0 ? res.RefreshTokenExpiresIn : 30 * 24 * 3600) * 1000L),
            Scope = res.Scope,
            Code = res.Code,
            Msg = res.Msg
        };

        return tokenInfo;
    }

    /// <inheritdoc />
    public override async Task<UserTokenInfo?> RefreshUserTokenAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        var cachedInfo = await GetTokenInfoAsync(userId, cancellationToken).ConfigureAwait(false);
        if (cachedInfo == null)
            return null;

        if (string.IsNullOrEmpty(cachedInfo.RefreshToken))
            return null;

        _logger.LogInformation("Refreshing user token for userId: {UserId}", userId);

        var credentials = new OAuthRefreshTokenRequest
        {
            GrantType = "refresh_token",
            ClientId = _options.AppId,
            ClientSecret = _options.AppSecret,
            RefreshToken = cachedInfo.RefreshToken
        };

        var res = await _authenticationApi.GetOAuthenRefreshAccessTokenAsync(credentials, cancellationToken);

        if (res == null || res.Code != 0)
        {
            _logger.LogWarning("Failed to refresh user token for userId: {UserId}, error: {Msg}", userId, res?.Msg);
            return null;
        }

        var tokenInfo = new UserTokenInfo
        {
            UserId = cachedInfo.UserId,
            OpenId = cachedInfo.OpenId,
            UnionId = cachedInfo.UnionId,
            AccessToken = res.AccessToken ?? cachedInfo.AccessToken,
            RefreshToken = res.RefreshToken ?? cachedInfo.RefreshToken,
            AccessTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ((res.ExpiresIn > 0 ? res.ExpiresIn : 7200) * 1000L),
            RefreshTokenExpireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ((res.RefreshTokenExpiresIn > 0 ? res.RefreshTokenExpiresIn : 30 * 24 * 3600) * 1000L),
            Scope = cachedInfo.Scope,
            Code = res.Code,
            Msg = res.Msg
        };

        UpdateUserTokenCache(userId, tokenInfo);
        _userTokenLookup[userId] = tokenInfo;
        return tokenInfo;
    }

    /// <inheritdoc />
    public override Task<bool> RemoveTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(false);

        RemoveUserTokenFromCache(userId);
        _userTokenLookup.TryRemove(userId, out _);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public override async Task<bool> HasValidTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        var cachedInfo = await GetTokenInfoAsync(userId, cancellationToken).ConfigureAwait(false);
        return cachedInfo != null && !string.IsNullOrEmpty(cachedInfo.AccessToken);
    }

    /// <inheritdoc />
    public override async Task<bool> CanRefreshTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        var cachedInfo = await GetTokenInfoAsync(userId, cancellationToken).ConfigureAwait(false);
        return cachedInfo != null && !string.IsNullOrEmpty(cachedInfo.RefreshToken);
    }

    /// <inheritdoc />
    public override Task<UserTokenInfo?> GetTokenInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult<UserTokenInfo?>(null);

        if (_userTokenLookup.TryGetValue(userId, out var tokenInfo))
            return Task.FromResult<UserTokenInfo?>(tokenInfo);

        return Task.FromResult<UserTokenInfo?>(null);
    }

    /// <inheritdoc />
    public Task StoreUserTokenAsync(string userId, UserTokenInfo tokenInfo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId) || tokenInfo == null)
            return Task.CompletedTask;

        _userTokenLookup[userId] = tokenInfo;
        UpdateUserTokenCache(userId, tokenInfo);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 刷新令牌的核心实现（TokenManagerBase 要求的抽象方法）
    /// 用户令牌不支持通过此方法刷新，用户令牌使用 RefreshUserTokenAsync 方法
    /// </summary>
    protected override Task<CredentialToken> RefreshTokenCoreAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException("User tokens should be refreshed via RefreshUserTokenAsync method.");
    }
}
