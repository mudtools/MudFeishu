// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mud.Feishu.Exceptions;
using System.Collections.Concurrent;

namespace Mud.Feishu.TokenManager;

/// <summary>
/// 带缓存的令牌管理器基类
/// </summary>
/// <remarks>
/// 提供令牌获取、缓存、自动刷新等核心功能，采用线程安全的缓存机制防止并发请求。
/// 支持令牌过期检测、自动重试、缓存清理等特性。
/// </remarks>
public abstract class TokenManagerWithCache : ITokenManager, IDisposable
{
    /// <summary>
    /// 凭证令牌数据模型
    /// </summary>
    /// <remarks>
    /// 表示从飞书API获取的访问令牌信息，包含令牌内容、过期时间和响应状态。
    /// </remarks>
    public class CredentialToken
    {
        /// <summary>
        /// 响应消息
        /// </summary>
        /// <remarks>
        /// API返回的错误消息或成功消息，null表示无消息。
        /// </remarks>
        public string? Msg { get; init; }

        /// <summary>
        /// 响应状态码
        /// </summary>
        /// <remarks>
        /// 0表示成功，非0表示错误状态码。
        /// </remarks>
        public int Code { get; init; }

        /// <summary>
        /// 令牌过期时间戳（毫秒）
        /// </summary>
        /// <remarks>
        /// Unix时间戳格式的过期时间，使用UTC时间。
        /// </remarks>
        public
#if NET7_0_OR_GREATER
        required
#endif
  long Expire
        { get; init; }

        /// <summary>
        /// 访问令牌
        /// </summary>
        /// <remarks>
        /// 用于API认证的访问令牌字符串，null表示未获取到令牌。
        /// </remarks>
        public
#if NET7_0_OR_GREATER
        required
#endif
  string? AccessToken
        { get; init; }
    }

    /// <summary>
    /// 飞书配置选项
    /// </summary>
    /// <remarks>
    /// 包含应用ID、应用密钥等飞书API访问所需的基本配置信息。
    /// </remarks>
    protected readonly FeishuOptions _options;

    /// <summary>
    /// 飞书V3认证API接口
    /// </summary>
    /// <remarks>
    /// 用于调用飞书认证相关API的服务接口。
    /// </remarks>
    protected readonly IFeishuV3AuthenticationApi _authenticationApi;

    /// <summary>
    /// 日志记录器
    /// </summary>
    /// <remarks>
    /// 用于记录令牌管理过程中的各种日志信息。
    /// </remarks>
    protected readonly ILogger<TokenManagerWithCache> _logger;

    /// <summary>
    /// 令牌刷新阈值时间
    /// </summary>
    /// <remarks>
    /// 令牌过期前提前刷新的时间间隔，默认为5分钟。
    /// </remarks>
    private readonly TimeSpan _tokenRefreshThreshold;

    /// <summary>
    /// 令牌类型
    /// </summary>
    /// <remarks>
    /// 标识当前管理器处理的令牌类型（如App Token、User Token等）。
    /// </remarks>
    private readonly TokenType _tokeType;

    // 使用 Lazy 和 AsyncLock 解决缓存击穿和竞态条件问题
    /// <summary>
    /// 令牌加载任务字典
    /// </summary>
    /// <remarks>
    /// 使用Lazy包装确保同一时刻只有一个请求在获取特定缓存键的令牌，防止缓存击穿。
    /// </remarks>
    private readonly ConcurrentDictionary<string, Lazy<Task<CredentialToken>>> _tokenLoadingTasks = new();

    /// <summary>
    /// 令牌缓存字典
    /// </summary>
    /// <remarks>
    /// 存储已获取的有效令牌，使用线程安全的ConcurrentDictionary实现。
    /// </remarks>
    private readonly ConcurrentDictionary<string, CredentialToken> _appTokenCache = new();

    /// <summary>
    /// 缓存操作锁
    /// </summary>
    /// <remarks>
    /// 用于保护缓存操作的线程安全，避免并发写入冲突。
    /// </remarks>
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    // 常量定义
    /// <summary>
    /// 默认令牌过期时间（秒）
    /// </summary>
    /// <remarks>
    /// 飞书API默认的令牌过期时间为2小时（7200秒）。
    /// </remarks>
    private const int DefaultTokenExpirationSeconds = 7200;

    /// <summary>
    /// 默认刷新阈值时间（秒）
    /// </summary>
    /// <remarks>
    /// 在令牌过期前5分钟（300秒）开始刷新，避免因网络延迟等原因导致令牌失效。
    /// </remarks>
    private const int DefaultRefreshThresholdSeconds = 300; // 提前5分钟刷新

    /// <summary>
    /// 初始化TokenManagerWithCache实例
    /// </summary>
    /// <param name="authenticationApi">飞书V3认证API接口</param>
    /// <param name="options">飞书配置选项</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="tokeType">令牌类型</param>
    /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
    public TokenManagerWithCache(
        IFeishuV3AuthenticationApi authenticationApi,
        IOptions<FeishuOptions> options,
        ILogger<TokenManagerWithCache> logger,
        TokenType tokeType)
    {
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenRefreshThreshold = TimeSpan.FromSeconds(DefaultRefreshThresholdSeconds);
        _tokeType = tokeType;
    }

    /// <summary>
    /// 获取应用身份访问令牌
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格式的Bearer令牌字符串，如果获取失败则返回null</returns>
    /// <remarks>
    /// 此方法会自动处理令牌缓存和刷新逻辑，优先使用缓存中的有效令牌。
    /// 如果缓存中没有有效令牌，则会获取新令牌并更新缓存。
    /// </remarks>
    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return await GetTokenInternalAsync(cancellationToken);
    }

    /// <summary>
    /// 统一的令牌获取方法
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格式的Bearer令牌字符串，如果获取失败则返回null</returns>
    /// <remarks>
    /// 内部核心方法，实现缓存优先、懒加载、防止缓存击穿的令牌获取逻辑。
    /// 首先检查缓存中是否有有效令牌，如果没有则使用Lazy机制确保只有一个请求在获取新令牌。
    /// </remarks>
    private async Task<string?> GetTokenInternalAsync(CancellationToken cancellationToken)
    {
        var cacheKey = GenerateCacheKey();

        // 尝试从缓存获取有效令牌
        if (TryGetValidTokenFromCache(cacheKey, out var cachedToken))
        {
            _logger.LogDebug("Using cached token for {TokenType}", _tokeType);
            return FormatBearerToken(cachedToken);
        }

        try
        {
            // 使用 Lazy 防止缓存击穿，确保同一时刻只有一个请求在获取令牌
            var lazyTask = _tokenLoadingTasks.GetOrAdd(cacheKey, _ => new Lazy<Task<CredentialToken>>(
                () => AcquireTokenAsync(cancellationToken),
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
    /// 获取新令牌的核心方法（抽象方法）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>获取到的凭证令牌</returns>
    /// <remarks>
    /// 由具体实现类重写，定义不同类型令牌的获取逻辑。
    /// </remarks>
    protected abstract Task<CredentialToken> AcquireNewTokenAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取新令牌的核心方法
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>获取并验证后的凭证令牌</returns>
    /// <remarks>
    /// 包含重试机制、错误处理、缓存更新等完整逻辑的令牌获取方法。
    /// 最多重试2次，使用指数退避策略。
    /// </remarks>
    private async Task<CredentialToken> AcquireTokenAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Acquiring new token for {TokenType}", _tokeType);

        // 实现重试机制
        var retryCount = 0;
        const int maxRetries = 2;

        while (retryCount <= maxRetries)
        {
            try
            {
                var result = await AcquireNewTokenAsync(cancellationToken);

                ValidateTokenResult(result);
                var newToken = CreateAppCredentialToken(result);

                // 原子性地更新缓存
                UpdateTokenCache(newToken);

                _logger.LogInformation("Successfully acquired new token for {TokenType}, expires at {ExpireTime}",
                    _tokeType, DateTimeOffset.FromUnixTimeMilliseconds(newToken.Expire));

                return newToken;
            }
            catch (Exception ex) when (retryCount < maxRetries && !(ex is FeishuException))
            {
                retryCount++;
                _logger.LogWarning(ex, "Failed to acquire token for {TokenType}, retry {RetryCount}/{MaxRetries}",
                    _tokeType, retryCount, maxRetries);

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
            }
        }

        throw new FeishuException(500, $"Failed to acquire {_tokeType} after {maxRetries} retries");
    }

    /// <summary>
    /// 尝试从缓存获取有效令牌（考虑刷新阈值）
    /// </summary>
    /// <param name="cacheKey">缓存键</param>
    /// <param name="token">输出的令牌字符串</param>
    /// <returns>如果获取到有效令牌返回true，否则返回false</returns>
    /// <remarks>
    /// 检查缓存中是否存在指定键的令牌，并且令牌未过期或接近过期时间。
    /// 考虑了刷新阈值，提前在过期前一段时间就认为令牌无效。
    /// </remarks>
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
    /// <param name="expireTime">令牌过期时间戳（毫秒）</param>
    /// <returns>如果令牌已过期或即将过期返回true，否则返回false</returns>
    /// <remarks>
    /// 比较当前时间与令牌过期时间，考虑刷新阈值。
    /// 当距离过期时间小于刷新阈值时，认为令牌即将过期。
    /// </remarks>
    private bool IsTokenExpiredOrNearExpiry(long expireTime)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var thresholdTime = currentTime + (long)_tokenRefreshThreshold.TotalMilliseconds;
        return expireTime <= thresholdTime;
    }

    /// <summary>
    /// 生成缓存键
    /// </summary>
    /// <returns>组合应用ID和令牌类型的缓存键字符串</returns>
    /// <remarks>
    /// 使用应用ID和令牌类型组合生成唯一缓存键，确保不同应用或不同类型的令牌使用不同的缓存。
    /// </remarks>
    private string GenerateCacheKey()
    {
        return $"{_options.AppId}:{_tokeType}";
    }

    /// <summary>
    /// 格式化 Bearer Token
    /// </summary>
    /// <param name="token">原始访问令牌</param>
    /// <returns>格式化后的Bearer令牌字符串</returns>
    /// <remarks>
    /// 在令牌前添加"Bearer "前缀，符合HTTP认证标准格式。
    /// </remarks>
    private static string FormatBearerToken(string? token)
    {
        return $"Bearer {token}";
    }

    /// <summary>
    /// 验证令牌结果
    /// </summary>
    /// <param name="result">从API获取的凭证令牌结果</param>
    /// <exception cref="FeishuException">当验证失败时抛出飞书异常</exception>
    /// <remarks>
    /// 验证令牌结果的有效性，包括：
    /// 1. 结果对象不为null
    /// 2. 响应状态码为0（成功）
    /// 3. 访问令牌不为空
    /// 验证失败时记录错误日志并抛出异常。
    /// </remarks>
    private void ValidateTokenResult(CredentialToken? result)
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
    /// <param name="result">API返回的原始凭证令牌</param>
    /// <returns>包含计算过期时间的凭证令牌</returns>
    /// <remarks>
    /// 根据API返回的过期时间计算实际的过期时间戳，并创建新的凭证令牌对象。
    /// 如果API未返回过期时间，则使用默认过期时间。
    /// </remarks>
    private CredentialToken CreateAppCredentialToken(CredentialToken result)
    {
        var expireTime = CalculateExpireTime(result.Expire);

        return new CredentialToken
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
    /// <param name="newToken">新的凭证令牌</param>
    /// <remarks>
    /// 将新获取的令牌更新到缓存中，使用原子操作确保线程安全。
    /// 如果缓存中已存在相同键的令牌，则替换为新令牌。
    /// </remarks>
    private void UpdateTokenCache(CredentialToken newToken)
    {
        var cacheKey = GenerateCacheKey();
        _appTokenCache.AddOrUpdate(cacheKey, newToken, (key, existing) => newToken);
    }

    /// <summary>
    /// 计算过期时间戳
    /// </summary>
    /// <param name="expiresInSeconds">API返回的过期时间（秒），可能为null</param>
    /// <returns>Unix时间戳格式的过期时间（毫秒）</returns>
    /// <remarks>
    /// 将API返回的过期时间转换为Unix时间戳格式。
    /// 如果API未返回过期时间或返回值无效，则使用默认过期时间（2小时）。
    /// </remarks>
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
    /// <param name="code">错误码</param>
    /// <param name="message">错误消息</param>
    /// <exception cref="FeishuException">总是抛出飞书异常</exception>
    /// <remarks>
    /// 统一的错误处理方法，记录错误日志并创建飞书异常。
    /// 确保错误信息的一致性和可追踪性。
    /// </remarks>
    private void LogAndThrowException(int code, string message)
    {
        _logger.LogError("Feishu token request failed. Code: {Code}, Message: {Message}", code, message);
        throw new FeishuException(code, message);
    }

    /// <summary>
    /// 清理过期令牌
    /// </summary>
    /// <remarks>
    /// 遍历缓存中的所有令牌，移除已过期或即将过期的令牌。
    /// 建议定期调用此方法以释放内存空间。
    /// 清理完成后会记录清理的令牌数量。
    /// </remarks>
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
    /// <returns>包含总令牌数和过期令牌数的元组</returns>
    /// <remarks>
    /// 返回缓存的统计信息，用于监控和调优：
    /// - Total: 缓存中的令牌总数
    /// - Expired: 已过期或即将过期的令牌数量
    /// 可用于定期清理和性能监控。
    /// </remarks>
    public (int Total, int Expired) GetCacheStatistics()
    {
        var total = _appTokenCache.Count;
        var expired = _appTokenCache.Count(kvp => IsTokenExpiredOrNearExpiry(kvp.Value.Expire));
        return (total, expired);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <remarks>
    /// 实现IDisposable接口，释放所有托管资源。
    /// 主要是释放SemaphoreSlim对象，避免资源泄漏。
    /// </remarks>
    public void Dispose()
    {
        _cacheLock?.Dispose();
    }
}