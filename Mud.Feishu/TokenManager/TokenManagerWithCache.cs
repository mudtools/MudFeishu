
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Exceptions;
using System.Collections.Concurrent;

namespace Mud.Feishu;

internal class TokenManagerWithCache : ITokenManager, IDisposable
{
    private class AppCredentialToken
    {
        public string? Msg { get; init; }
        public int Code { get; init; }
        public required long Expire { get; init; }
        public required string AccessToken { get; init; }
    }

    private readonly FeishuOptions _options;
    private readonly IFeishuV3AuthenticationApi _authenticationApi;
    private readonly ILogger<TokenManagerWithCache> _logger;
    private readonly TimeSpan _tokenRefreshThreshold;

    // 使用 Lazy 和 AsyncLock 解决缓存击穿和竞态条件问题
    private readonly ConcurrentDictionary<string, Lazy<Task<AppCredentialToken>>> _tokenLoadingTasks = new();
    private readonly ConcurrentDictionary<string, AppCredentialToken> _appTokenCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    // 常量定义
    private const int DefaultTokenExpirationSeconds = 7200;
    private const int DefaultRefreshThresholdSeconds = 300; // 提前5分钟刷新

    public TokenManagerWithCache(
        IFeishuV3AuthenticationApi authenticationApi,
        IOptions<FeishuOptions> options,
        ILogger<TokenManagerWithCache> logger)
    {
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenRefreshThreshold = TimeSpan.FromSeconds(DefaultRefreshThresholdSeconds);
    }

    public async Task<string?> GetTenantAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return await GetTokenInternalAsync(TokenType.TenantAccessToken, cancellationToken);
    }

    public async Task<string?> GetUserAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return await GetTokenInternalAsync(TokenType.UserAccessToken, cancellationToken);
    }

    /// <summary>
    /// 统一的令牌获取方法
    /// </summary>
    private async Task<string?> GetTokenInternalAsync(TokenType tokenType, CancellationToken cancellationToken)
    {
        var cacheKey = GenerateCacheKey(tokenType);

        // 尝试从缓存获取有效令牌
        if (TryGetValidTokenFromCache(cacheKey, out var cachedToken))
        {
            _logger.LogDebug("Using cached token for {TokenType}", tokenType);
            return FormatBearerToken(cachedToken);
        }

        try
        {
            // 使用 Lazy 防止缓存击穿，确保同一时刻只有一个请求在获取令牌
            var lazyTask = _tokenLoadingTasks.GetOrAdd(cacheKey, _ => new Lazy<Task<AppCredentialToken>>(
                () => AcquireTokenAsync(tokenType, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));

            var token = await lazyTask.Value;
            return FormatBearerToken(token.AccessToken);
        }
        finally
        {
            // 清理已完成的任务
            _tokenLoadingTasks.TryRemove(cacheKey, out _);
        }
    }

    /// <summary>
    /// 获取新令牌的核心方法
    /// </summary>
    private async Task<AppCredentialToken> AcquireTokenAsync(TokenType tokenType, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Acquiring new token for {TokenType}", tokenType);

        // 实现重试机制
        var retryCount = 0;
        const int maxRetries = 2;

        while (retryCount <= maxRetries)
        {
            try
            {
                var result = await (tokenType == TokenType.TenantAccessToken
                    ? GetNewTenantTokenAsync(cancellationToken)
                    : GetNewUserTokenAsync(cancellationToken));

                ValidateTokenResult(result);
                var newToken = CreateAppCredentialToken(result);

                // 原子性地更新缓存
                UpdateTokenCache(newToken, tokenType);

                _logger.LogInformation("Successfully acquired new token for {TokenType}, expires at {ExpireTime}",
                    tokenType, DateTimeOffset.FromUnixTimeMilliseconds(newToken.Expire));

                return newToken;
            }
            catch (Exception ex) when (retryCount < maxRetries && !(ex is FeishuException))
            {
                retryCount++;
                _logger.LogWarning(ex, "Failed to acquire token for {TokenType}, retry {RetryCount}/{MaxRetries}",
                    tokenType, retryCount, maxRetries);

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
            }
        }

        throw new FeishuException(500, $"Failed to acquire {tokenType} after {maxRetries} retries");
    }

    /// <summary>
    /// 尝试从缓存获取有效令牌（考虑刷新阈值）
    /// </summary>
    private bool TryGetValidTokenFromCache(string cacheKey, out string? token)
    {
        token = null;

        if (_appTokenCache.TryGetValue(cacheKey, out var cachedToken) &&
            !IsTokenExpiredOrNearExpiry(cachedToken.Expire))
        {
            token = cachedToken.AccessToken;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 判断令牌是否过期或即将过期
    /// </summary>
    private bool IsTokenExpiredOrNearExpiry(long expireTime)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var thresholdTime = currentTime + (long)_tokenRefreshThreshold.TotalMilliseconds;
        return expireTime <= thresholdTime;
    }

    /// <summary>
    /// 生成缓存键
    /// </summary>
    private string GenerateCacheKey(TokenType tokenType)
    {
        return $"{_options.AppId}:{tokenType}";
    }

    /// <summary>
    /// 格式化 Bearer Token
    /// </summary>
    private static string FormatBearerToken(string token)
    {
        return $"Bearer {token}";
    }

    /// <summary>
    /// 获取应用访问令牌
    /// </summary>
    private async Task<AppCredentialToken> GetNewTenantTokenAsync(CancellationToken cancellationToken)
    {
        var credentials = new AppCredentials
        {
            AppId = _options.AppId,
            AppSecret = _options.AppSecret
        };

        var res = await _authenticationApi.GetTenantAccessTokenAsync(credentials, cancellationToken);
        return new AppCredentialToken
        {
            AccessToken = res.TenantAccessToken ?? string.Empty,
            Expire = res.Expire,
            Code = res.Code,
            Msg = res.Msg
        };
    }

    /// <summary>
    /// 获取用户访问令牌
    /// </summary>
    private async Task<AppCredentialToken> GetNewUserTokenAsync(CancellationToken cancellationToken)
    {
        var credentials = new AppCredentials
        {
            AppId = _options.AppId,
            AppSecret = _options.AppSecret
        };

        var res = await _authenticationApi.GetAppAccessTokenAsync(credentials, cancellationToken);
        return new AppCredentialToken
        {
            AccessToken = res.AppAccessToken ?? string.Empty,
            Expire = res.Expire,
            Code = res.Code,
            Msg = res.Msg
        };
    }

    /// <summary>
    /// 验证令牌结果
    /// </summary>
    private void ValidateTokenResult(AppCredentialToken? result)
    {
        if (result == null)
        {
            LogAndThrowException(443, "获取飞书访问令牌失败: 返回结果为null");
        }

        if (result.Code != 0)
        {
            LogAndThrowException(result.Code, $"获取飞书访问令牌失败，错误码: {result.Code}, 消息: {result.Msg}");
        }

        if (string.IsNullOrEmpty(result.AccessToken))
        {
            LogAndThrowException(443, "获取飞书访问令牌失败: AccessToken为空");
        }
    }

    /// <summary>
    /// 创建应用凭证令牌
    /// </summary>
    private AppCredentialToken CreateAppCredentialToken(AppCredentialToken result)
    {
        var expireTime = CalculateExpireTime(result.Expire);

        return new AppCredentialToken
        {
            Expire = expireTime,
            AccessToken = result.AccessToken,
            Code = result.Code,
            Msg = result.Msg
        };
    }

    /// <summary>
    /// 更新令牌缓存
    /// </summary>
    private void UpdateTokenCache(AppCredentialToken newToken, TokenType tokenType)
    {
        var cacheKey = GenerateCacheKey(tokenType);
        _appTokenCache.AddOrUpdate(cacheKey, newToken, (key, existing) => newToken);
    }

    /// <summary>
    /// 计算过期时间戳
    /// </summary>
    private static long CalculateExpireTime(long? expiresInSeconds)
    {
        var actualExpiresIn = expiresInSeconds ?? DefaultTokenExpirationSeconds;

        if (actualExpiresIn <= 0)
        {
            actualExpiresIn = DefaultTokenExpirationSeconds;
        }

        // 转换为毫秒并计算实际过期时间
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (actualExpiresIn * 1000);
    }

    /// <summary>
    /// 记录日志并抛出异常
    /// </summary>
    private void LogAndThrowException(int code, string message)
    {
        _logger.LogError("Feishu token request failed. Code: {Code}, Message: {Message}", code, message);
        throw new FeishuException(code, message);
    }

    /// <summary>
    /// 清理过期令牌
    /// </summary>
    public void CleanExpiredTokens()
    {
        var expiredKeys = _appTokenCache
                        .Where(kvp => IsTokenExpiredOrNearExpiry(kvp.Value.Expire))
                        .Select(kvp => kvp.Key)
                        .ToList();

        foreach (var cacheKey in expiredKeys)
        {
            _appTokenCache.TryRemove(cacheKey, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleaned {Count} expired tokens", expiredKeys.Count);
        }
    }

    /// <summary>
    /// 获取缓存统计信息（用于监控）
    /// </summary>
    public (int Total, int Expired) GetCacheStatistics()
    {
        var total = _appTokenCache.Count;
        var expired = _appTokenCache.Count(kvp => IsTokenExpiredOrNearExpiry(kvp.Value.Expire));
        return (total, expired);
    }

    public void Dispose()
    {
        _cacheLock?.Dispose();
    }
}