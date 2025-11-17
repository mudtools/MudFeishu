
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Exceptions;
using System.Collections.Concurrent;

namespace Mud.Feishu;

internal class TenantTokenManagerWithCache : ITokenManager
{
    private class AppCredentialToken(long expire, string tenantAccessToken, string? appAccessToken)
    {
        public long Expire { get; set; } = expire;
        public string TenantAccessToken { get; set; } = tenantAccessToken;
        public string? AppAccessToken { get; set; } = appAccessToken;
    }

    private readonly FeishuOptions _options;
    private readonly IFeishuAuthenticationApi _authenticationApi;
    private readonly ILogger<TenantTokenManagerWithCache> _logger;

    // 使用并发字典处理应用令牌缓存
    private readonly ConcurrentDictionary<string, AppCredentialToken> _appTokenCache = new();

    public TenantTokenManagerWithCache(
        IFeishuAuthenticationApi authenticationApi,
        IOptions<FeishuOptions> options,
        ILogger<TenantTokenManagerWithCache> logger)
    {
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        // 尝试从缓存获取有效令牌
        if (TryGetValidTokenFromCache(out var cachedToken))
        {
            return cachedToken;
        }

        // 获取新令牌
        var result = await GetNewTokenAsync(cancellationToken);

        // 验证并缓存令牌
        ValidateTokenResult(result);
        var newToken = CreateAppCredentialToken(result);

        // 更新缓存
        UpdateTokenCache(newToken);

        return newToken.TenantAccessToken;
    }

    /// <summary>
    /// 尝试从缓存获取有效令牌
    /// </summary>
    private bool TryGetValidTokenFromCache(out string? token)
    {
        token = null;
        if (_appTokenCache.TryGetValue(_options.AppId, out var cachedToken) &&
            !IsTokenExpired(cachedToken.Expire))
        {
            token = cachedToken.TenantAccessToken;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取新的访问令牌
    /// </summary>
    private async Task<TenantAppCredentialResult> GetNewTokenAsync(CancellationToken cancellationToken)
    {
        var credentials = new AppCredentials
        {
            AppId = _options.AppId,
            AppSecret = _options.AppSecret
        };

        return _options.AppType switch
        {
            FeishuAppType.SelfTenantBuiltApp =>
                await _authenticationApi.GetAppAccessTokenAsync(credentials, cancellationToken),

            FeishuAppType.SelfBuiltApp =>
                await _authenticationApi.GetTenantAccessTokenAsync(credentials, cancellationToken),

            _ => throw new FeishuException(400, $"不支持的AppType: {_options.AppType}")
        };
    }

    /// <summary>
    /// 验证令牌结果
    /// </summary>
    private void ValidateTokenResult(TenantAppCredentialResult? result)
    {
        if (result == null)
        {
            LogAndThrowException(443, "获取飞书访问令牌失败: 返回结果为null");
        }

        if (result.Code != 0)
        {
            LogAndThrowException(result.Code, $"获取飞书访问令牌失败，错误码: {result.Code}, 消息: {result.Msg}");
        }

        if (string.IsNullOrEmpty(result.TenantAccessToken))
        {
            LogAndThrowException(443, "获取飞书访问令牌失败: TenantAccessToken为空");
        }
    }

    /// <summary>
    /// 创建应用凭证令牌
    /// </summary>
    private AppCredentialToken CreateAppCredentialToken(TenantAppCredentialResult result)
    {
        var expireTime = CalculateExpireTime(result.Expire);

        var token = new AppCredentialToken(
            expireTime,
            result.TenantAccessToken!,
            (result as AppCredentialResult)?.AppAccessToken
        );

        return token;
    }

    /// <summary>
    /// 更新令牌缓存
    /// </summary>
    private void UpdateTokenCache(AppCredentialToken newToken)
    {
        _appTokenCache.AddOrUpdate(
            _options.AppId,
            newToken,
            (key, existingToken) => IsTokenExpired(existingToken.Expire) ? newToken : existingToken
        );
    }

    /// <summary>
    /// 判断令牌是否过期
    /// </summary>
    private bool IsTokenExpired(long expireTime)
    {
        return expireTime <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 计算过期时间戳
    /// </summary>
    private static long CalculateExpireTime(long? expiresInSeconds)
    {
        if (expiresInSeconds == null || expiresInSeconds <= 0)
        {
            // 默认2小时过期
            expiresInSeconds = 7200;
        }

        // 转换为毫秒并计算实际过期时间
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (expiresInSeconds.Value * 1000);
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
    /// 清理过期令牌（可选方法）
    /// </summary>
    public void CleanExpiredTokens()
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (var cacheKey in _appTokenCache.Keys.ToArray())
        {
            if (_appTokenCache.TryGetValue(cacheKey, out var token) &&
                IsTokenExpired(token.Expire))
            {
                _appTokenCache.TryRemove(cacheKey, out _);
            }
        }
    }
}
