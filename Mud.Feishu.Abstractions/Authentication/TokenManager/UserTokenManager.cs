// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Metrics;
using System.Collections.Concurrent;

namespace Mud.Feishu.TokenManager;

/// <summary>
/// 用户令牌管理器
/// </summary>
/// <remarks>
/// 提供用户访问令牌的获取、缓存和自动刷新功能。
/// 核心特性：
/// <list type="bullet">
///   <item><description>支持完整的 OAuth 授权流程</description></item>
///   <item><description>自动令牌刷新机制</description></item>
///   <item><description>线程安全的并发控制</description></item>
///   <item><description>防止缓存击穿的 Lazy 加载</description></item>
/// </list>
/// </remarks>
internal class UserTokenManager : IFeishuUserTokenManager
{
    private readonly ICurrentUserContext? _currentUserContext;
    private readonly IFeishuAuthentication _authenticationApi;
    private readonly FeishuAppConfig _options;
    private readonly ILogger<UserTokenManager> _logger;
    private readonly IUserTokenCache _userTokenCache;
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _tokenLoadingTasks = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new();

    private const int DefaultAccessTokenExpirationSeconds = 7200;
    private const int DefaultRefreshTokenExpirationSeconds = 2592000;

    public UserTokenManager(
        IFeishuAuthentication authenticationApi,
        ICurrentUserContext? currentUserContext,
        IOptions<FeishuAppConfig> options,
        ILogger<UserTokenManager> logger,
        IUserTokenCache userTokenCache)
    {
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userTokenCache = userTokenCache ?? throw new ArgumentNullException(nameof(userTokenCache));
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUserContext == null)
        {
            throw new InvalidOperationException("CurrentUserContext is not available. Cannot get user token.");
        }

        if (string.IsNullOrEmpty(_currentUserContext.OpenId))
        {
            throw new InvalidOperationException("Current user is not authenticated. OpenId is required to get user token.");
        }

        return await GetTokenAsync(_currentUserContext.UserId, cancellationToken) ?? throw new InvalidOperationException("Failed to obtain user token.");
    }

    public async Task<string?> GetTokenAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("UserId cannot be null or empty for user token operations.", nameof(userId));
        }

        var cacheKey = GenerateCacheKey(userId);

        var tokenInfo = await _userTokenCache.GetAsync(cacheKey, cancellationToken);
        if (tokenInfo != null && tokenInfo.IsAccessTokenValid(_options.TokenRefreshThreshold))
        {
            _logger.LogDebug("Using cached access token for user {UserId}", userId);
            FeishuMetricsHelper.RecordTokenFetch("UserAccessToken", fromCache: true);
            return FormatBearerToken(tokenInfo.AccessToken);
        }

        try
        {
            FeishuMetricsHelper.RecordTokenFetch("UserAccessToken", fromCache: false);

            var lazyTask = _tokenLoadingTasks.GetOrAdd(cacheKey, _ => new Lazy<Task<string?>>(
                () => GetOrRefreshTokenInternalAsync(userId!, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));

            return await lazyTask.Value;
        }
        finally
        {
            _tokenLoadingTasks.TryRemove(cacheKey, out _);
        }
    }

    public async Task<UserTokenInfo?> GetTokenInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("UserId cannot be null or empty.", nameof(userId));
        }

        var cacheKey = GenerateCacheKey(userId);
        return await _userTokenCache.GetAsync(cacheKey, cancellationToken);
    }

    public async Task<UserTokenInfo?> GetUserTokenWithCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(code))
        {
            throw new ArgumentException("Code cannot be null or empty.", nameof(code));
        }

        _logger.LogInformation("Getting user token with authorization code");

        var credentials = new OAuthTokenRequest
        {
            GrantType = "authorization_code",
            ClientId = _options.AppId,
            ClientSecret = _options.AppSecret,
            Code = code,
            RedirectUri = redirectUri
        };

        var res = await _authenticationApi.GetOAuthenAccessTokenAsync(credentials, cancellationToken);
        if (res == null)
        {
            _logger.LogWarning("Failed to get user token: API returned null");
            return null;
        }

        var token = CreateCredentialTokenFromResult(res);

        if (res.Code != 0)
        {
            _logger.LogWarning("Failed to get user token: {Code} - {Msg}", res.Code, res.Msg);
            return token;
        }

        var userInfo = await _authenticationApi.GetUserInfoAsync(token.AccessToken, cancellationToken);
        var openId = userInfo?.Data?.OpenId;
        var unionId = userInfo?.Data?.UnionId;

        if (string.IsNullOrEmpty(openId))
        {
            _logger.LogWarning("Failed to get user info from token");
            return token;
        }

        var userId = openId;
        var cacheKey = GenerateCacheKey(userId);

        var tokenInfo = UserTokenInfo.FromCredentialToken(token, userId, openId, unionId);
        await _userTokenCache.SetAsync(cacheKey, tokenInfo, cancellationToken);

        _logger.LogInformation("User token acquired for user {UserId}, access token expires at {ExpireTime}, refresh token expires at {RefreshExpireTime}",
            userId,
            DateTimeOffset.FromUnixTimeMilliseconds(token.AccessTokenExpireTime),
            token.RefreshTokenExpireTime > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(token.RefreshTokenExpireTime) : "N/A");

        return tokenInfo;
    }

    public async Task<UserTokenInfo?> RefreshUserTokenAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("UserId cannot be null or empty.", nameof(userId));
        }

        var cacheKey = GenerateCacheKey(userId);

        var refreshLock = _refreshLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            var tokenInfo = await GetTokenInfoInternalAsync(cacheKey, cancellationToken);
            if (tokenInfo == null)
            {
                _logger.LogWarning("No token info found for user {UserId}, cannot refresh", userId);
                return null;
            }

            if (string.IsNullOrEmpty(tokenInfo.RefreshToken))
            {
                _logger.LogWarning("No refresh token found for user {UserId}", userId);
                return null;
            }

            if (!tokenInfo.IsRefreshTokenValid())
            {
                _logger.LogWarning("Refresh token expired for user {UserId}", userId);
                return null;
            }

            _logger.LogInformation("Refreshing user token for user {UserId}", userId);
            FeishuMetricsHelper.RecordTokenRefresh("UserAccessToken");

            var credentials = new OAuthRefreshTokenRequest
            {
                GrantType = "refresh_token",
                ClientId = _options.AppId,
                ClientSecret = _options.AppSecret,
                RefreshToken = tokenInfo.RefreshToken
            };

            var res = await _authenticationApi.GetOAuthenRefreshAccessTokenAsync(credentials, cancellationToken);
            if (res == null)
            {
                _logger.LogWarning("Failed to refresh user token: API returned null");
                return null;
            }

            var newToken = CreateCredentialTokenFromResult(res);

            if (res.Code != 0)
            {
                _logger.LogWarning("Failed to refresh user token: {Code} - {Msg}", res.Code, res.Msg);
                return newToken;
            }

            tokenInfo.UpdateFromCredentialToken(newToken);
            await _userTokenCache.SetAsync(cacheKey, tokenInfo, cancellationToken);

            _logger.LogInformation("User token refreshed for user {UserId}, new access token expires at {ExpireTime}",
                userId, DateTimeOffset.FromUnixTimeMilliseconds(newToken.AccessTokenExpireTime));

            return tokenInfo;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    public async Task<bool> RemoveTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var cacheKey = GenerateCacheKey(userId);
        var removed = await _userTokenCache.RemoveAsync(cacheKey, cancellationToken);

        if (removed)
        {
            _logger.LogInformation("User token removed for user {UserId}", userId);
        }

        return removed;
    }

    public async Task<bool> HasValidTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var cacheKey = GenerateCacheKey(userId);
        return await _userTokenCache.ExistsAsync(cacheKey, cancellationToken);
    }

    public async Task<bool> CanRefreshTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var cacheKey = GenerateCacheKey(userId);
        var tokenInfo = await GetTokenInfoInternalAsync(cacheKey, cancellationToken);

        return tokenInfo?.IsRefreshTokenValid() ?? false;
    }

    private async Task<string?> GetOrRefreshTokenInternalAsync(string userId, CancellationToken cancellationToken)
    {
        var cacheKey = GenerateCacheKey(userId);

        var tokenInfo = await GetTokenInfoInternalAsync(cacheKey, cancellationToken);

        if (tokenInfo == null)
        {
            _logger.LogWarning("No token found for user {UserId}. Please use GetUserTokenWithCodeAsync to obtain a token.", userId);
            return null;
        }

        if (tokenInfo.IsAccessTokenValid(_options.TokenRefreshThreshold))
        {
            return FormatBearerToken(tokenInfo.AccessToken);
        }

        if (tokenInfo.IsRefreshTokenValid())
        {
            _logger.LogInformation("Access token expired, attempting to refresh for user {UserId}", userId);

            var newToken = await RefreshUserTokenAsync(userId, cancellationToken);
            if (newToken != null && !string.IsNullOrEmpty(newToken.AccessToken))
            {
                return FormatBearerToken(newToken.AccessToken);
            }

            _logger.LogWarning("Failed to refresh token for user {UserId}", userId);
            return null;
        }

        _logger.LogWarning("Both access token and refresh token expired for user {UserId}. Re-authorization required.", userId);
        return null;
    }

    private async Task<UserTokenInfo?> GetTokenInfoInternalAsync(string cacheKey, CancellationToken cancellationToken)
    {
        return await _userTokenCache.GetAsync(cacheKey, cancellationToken);
    }

    private UserTokenInfo CreateCredentialTokenFromResult(OAuthCredentialsResult res)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var accessTokenExpire = now + ((res.ExpiresIn > 0 ? res.ExpiresIn : DefaultAccessTokenExpirationSeconds) * 1000L);
        var refreshTokenExpire = now + ((res.RefreshTokenExpiresIn > 0 ? res.RefreshTokenExpiresIn : DefaultRefreshTokenExpirationSeconds) * 1000L);

        return new UserTokenInfo
        {
            AccessToken = res.AccessToken,
            AccessTokenExpireTime = accessTokenExpire,
            RefreshToken = res.RefreshToken,
            RefreshTokenExpireTime = refreshTokenExpire,
            Scope = res.Scope,
            Code = res.Code,
            Msg = res.ErrorDescription ?? (res.Code == 0 ? "Success" : "Unknown error")
        };
    }

    private string GenerateCacheKey(string? userId)
    {
        return $"{_options.AppId}:UserAccessToken:{userId}";
    }

    private static string FormatBearerToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return "Bearer ";

        return token.StartsWith("Bearer ") ? token : $"Bearer {token}";
    }
}
