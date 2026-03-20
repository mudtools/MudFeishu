// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Metrics;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.Exceptions;
using System.Collections.Concurrent;

namespace Mud.Feishu.TokenManager;

/// <summary>
/// 带缓存的令牌管理器基类
/// </summary>
/// <remarks>
/// 提供令牌获取、缓存、自动刷新等核心功能，采用线程安全的缓存机制防止并发请求。
/// 支持令牌过期检测、自动重试、缓存清理等特性。
/// 通过抽象缓存层支持多种缓存实现（内存、Redis等）。
/// </remarks>
public abstract class TokenManagerWithCache : ITokenManager, IDisposable
{
    /// <summary>
    /// 飞书配置选项
    /// </summary>
    /// <remarks>
    /// 包含应用ID、应用密钥等飞书API访问所需的基本配置信息。
    /// </remarks>
    protected readonly FeishuAppConfig _options;

    /// <summary>
    /// 飞书V3认证API接口
    /// </summary>
    /// <remarks>
    /// 用于调用飞书认证相关API的服务接口。
    /// </remarks>
    protected readonly IFeishuAuthentication _authenticationApi;

    /// <summary>
    /// 日志记录器
    /// </summary>
    /// <remarks>
    /// 用于记录令牌管理过程中的各种日志信息。
    /// </remarks>
    protected readonly ILogger<TokenManagerWithCache> _logger;

    /// <summary>
    /// 令牌缓存提供者
    /// </summary>
    /// <remarks>
    /// 抽象缓存接口，支持内存、Redis等多种实现。
    /// </remarks>
    protected readonly ITokenCache _tokenCache;

    /// <summary>
    /// 令牌类型
    /// </summary>
    /// <remarks>
    /// 标识当前管理器处理的令牌类型（如App Token、User Token等）。
    /// </remarks>
    protected readonly TokenType _tokenType;

    // 使用 Lazy 和 AsyncLock 解决缓存击穿和竞态条件问题
    /// <summary>
    /// 令牌加载任务字典
    /// </summary>
    /// <remarks>
    /// 使用Lazy包装确保同一时刻只有一个请求在获取特定缓存键的令牌，防止缓存击穿。
    /// </remarks>
    private readonly ConcurrentDictionary<string, Lazy<Task<CredentialToken>>> _tokenLoadingTasks = new();

    // 常量定义
    /// <summary>
    /// 默认令牌过期时间（秒）
    /// </summary>
    /// <remarks>
    /// 飞书API默认的令牌过期时间为2小时（7200秒）。
    /// </remarks>
    private const int DefaultTokenExpirationSeconds = 7200;

    /// <summary>
    /// 初始化TokenManagerWithCache实例
    /// </summary>
    /// <param name="authenticationApi">飞书V3认证API接口</param>
    /// <param name="options">飞书配置选项</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="tokenCache">令牌缓存提供者</param>
    /// <param name="tokenType">令牌类型</param>
    /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
    public TokenManagerWithCache(
        IFeishuAuthentication authenticationApi,
        IOptions<FeishuAppConfig> options,
        ILogger<TokenManagerWithCache> logger,
        ITokenCache tokenCache,
        TokenType tokenType)
    {
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
        _tokenType = tokenType;
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
    public virtual async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
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
    /// 缓存中存储的是原始token（不带Bearer前缀），返回时统一添加前缀。
    /// </remarks>
    private async Task<string> GetTokenInternalAsync(CancellationToken cancellationToken)
    {
        var cacheKey = GenerateCacheKey();

        // 尝试从缓存获取有效令牌（缓存中存储的是不带前缀的原始token）
        var cachedToken = await _tokenCache.GetAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedToken))
        {
            _logger.LogDebug("Using cached token for {TokenType}, AppId: {AppId}", _tokenType, _options.AppId);
            // 记录缓存命中指标
            using var _ = FeishuMetricsHelper.RecordTokenFetch(_tokenType.ToString(), fromCache: true);
            // 确保返回的token格式统一：Bearer {token}
            return FormatBearerToken(cachedToken);
        }

        try
        {
            // 记录缓存未命中指标
            using var _ = FeishuMetricsHelper.RecordTokenFetch(_tokenType.ToString(), fromCache: false);

            // 使用 Lazy 防止缓存击穿，确保同一时刻只有一个请求在获取令牌
            var lazyTask = _tokenLoadingTasks.GetOrAdd(cacheKey, _ => new Lazy<Task<CredentialToken>>(
                () => AcquireTokenAsync(cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));

            var token = await lazyTask.Value;
            // UpdateTokenCacheAsync 已经处理了缓存存储（移除Bearer前缀）
            // 这里需要从原始token中移除可能的前缀后格式化返回
            return FormatBearerToken(RemoveBearerPrefix(token.AccessToken));
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
    protected abstract Task<CredentialToken?> AcquireNewTokenAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取新令牌的核心方法
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>获取并验证后的凭证令牌</returns>
    /// <remarks>
    /// 包含重试机制、错误处理、缓存更新等完整逻辑的令牌获取方法。
    /// 最多重试2次，使用指数退避策略。仅对可重试异常进行重试。
    /// </remarks>
    private async Task<CredentialToken> AcquireTokenAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Acquiring new token for {TokenType}, AppId: {AppId}", _tokenType, _options.AppId);

        // 记录令牌刷新
        FeishuMetricsHelper.RecordTokenRefresh(_tokenType.ToString());

        // 实现重试机制（使用配置参数）
        var retryCount = 0;
        const int maxRetries = 2; // Token获取最多重试2次（独立于HTTP重试）

        while (true)
        {
            try
            {
                var result = await AcquireNewTokenAsync(cancellationToken);

                ValidateTokenResult(result);
                // result 已经由 ValidateTokenResult 验证非空，使用 null-forgiving operator
                var newToken = CreateAppCredentialToken(result!);

                // 原子性地更新缓存
                await UpdateTokenCacheAsync(newToken, cancellationToken);

                _logger.LogInformation("Successfully acquired new token for {TokenType}, AppId: {AppId}, expires at {ExpireTime}",
                    _tokenType, _options.AppId, DateTimeOffset.FromUnixTimeMilliseconds(newToken.Expire));

                return newToken;
            }
            catch (Exception ex)
            {
                // 判断是否为可重试异常
                if (!IsRetryableException(ex))
                {
                    // 不可重试异常，直接抛出
                    _logger.LogError(ex, "Failed to acquire {TokenType} due to non-retryable exception. Exception type: {ExceptionType}, Message: {ErrorMessage}",
                        _tokenType, ex.GetType().Name, ex.Message);
                    throw;
                }

                if (retryCount < maxRetries)
                {
                    retryCount++;
                    // 使用配置的 RetryDelayMs 进行指数退避
                    var delayMs = _options.RetryDelayMs * Math.Pow(2, retryCount - 1);
                    _logger.LogWarning(ex, "Failed to acquire token for {TokenType}, retry {RetryCount}/{MaxRetries} in {DelayMs}ms. Error: {ErrorMessage}",
                        _tokenType, retryCount, maxRetries, delayMs, ex.Message);

                    await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
                }
                else
                {
                    // 记录完整的异常信息,包括堆栈跟踪
                    _logger.LogError(ex, "Failed to acquire {TokenType} after {MaxRetries} retries. Exception type: {ExceptionType}, Message: {ErrorMessage}, StackTrace: {StackTrace}",
                        _tokenType, maxRetries, ex.GetType().Name, ex.Message, ex.StackTrace);

                    var errorMessage = $"Failed to acquire {_tokenType} after {maxRetries} retries: {ex.Message} ({ex.GetType().Name})";
                    throw new FeishuException(500, errorMessage, ex);
                }
            }
        }
    }

    /// <summary>
    /// 判断异常是否可重试
    /// </summary>
    /// <param name="ex">异常对象</param>
    /// <returns>如果异常可重试返回true，否则返回false</returns>
    /// <remarks>
    /// 可重试异常包括网络异常、超时异常等临时性错误。
    /// 不可重试异常包括认证失败、参数错误、不支持的操作等。
    /// </remarks>
    private static bool IsRetryableException(Exception ex)
    {
        // 不可重试的异常类型
        if (ex is FeishuException ||
            ex is NotSupportedException ||
            ex is NotImplementedException ||
            ex is ArgumentException ||
            ex is ArgumentNullException ||
            ex is InvalidOperationException)
        {
            return false;
        }

        // 可重试的异常类型：网络异常、超时异常等
        return ex is HttpRequestException ||
               ex is TimeoutException ||
               ex is TaskCanceledException;
    }

    /// <summary>
    /// 生成缓存键
    /// </summary>
    /// <returns>组合应用ID和令牌类型的缓存键字符串</returns>
    /// <remarks>
    /// 使用应用ID和令牌类型组合生成唯一缓存键，确保不同应用或不同类型的令牌使用不同的缓存。
    /// </remarks>
    protected string GenerateCacheKey(string? userId = null)
    {
        if (!string.IsNullOrEmpty(userId))
            return $"{_options.AppId}:{_tokenType}:{userId}";
        return $"{_options.AppId}:{_tokenType}";
    }

    /// <summary>
    /// 格式化 Bearer Token
    /// </summary>
    /// <param name="token">原始访问令牌（不带 Bearer 前缀）</param>
    /// <returns>格式化后的Bearer令牌字符串</returns>
    /// <remarks>
    /// 在令牌前添加"Bearer "前缀，符合HTTP认证标准格式。
    /// 注意：输入参数应为原始token（不带Bearer前缀），本方法会统一添加前缀。
    /// </remarks>
    protected string FormatBearerToken(string? token)
    {
        return TokenUtils.FormatBearerToken(token);
    }

    /// <summary>
    /// 移除 Bearer 前缀
    /// </summary>
    /// <param name="token">可能包含Bearer前缀的令牌</param>
    /// <returns>不包含Bearer前缀的原始令牌</returns>
    /// <remarks>
    /// 确保缓存中存储的是原始token值，不包含"Bearer "前缀。
    /// </remarks>
    protected static string? RemoveBearerPrefix(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return token;

        return token.StartsWith("Bearer ") ? token.Substring(7) : token;
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

        // result 已经由前面的 null 检查保证非空，使用断言消除编译器警告
        var validatedResult = result!;

        if (validatedResult.Code != 0)
        {
            LogAndThrowException(validatedResult.Code, $"获取飞书访问令牌失败，错误码: {validatedResult.Code}, 消息: {validatedResult.Msg}");
        }

        if (string.IsNullOrEmpty(validatedResult.AccessToken))
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
    protected CredentialToken CreateAppCredentialToken(CredentialToken result)
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
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 将新获取的令牌更新到缓存中，使用异步操作确保线程安全。
    /// 如果缓存中已存在相同键的令牌，则替换为新令牌。
    /// 注意：缓存中只存储原始token值（不带"Bearer "前缀），避免重复前缀。
    /// </remarks>
    protected virtual async Task UpdateTokenCacheAsync(CredentialToken newToken, CancellationToken cancellationToken)
    {
        var cacheKey = GenerateCacheKey();
        var expiresIn = CalculateExpirationFromTimestamp(newToken.Expire);

        // 移除可能的 Bearer 前缀，只存储原始 token
        // 使用 null-forgiving operator 消除编译器警告，因为 newToken.AccessToken 在此上下文中不会为 null
        var rawToken = RemoveBearerPrefix(newToken.AccessToken!) ?? string.Empty;
        await _tokenCache.SetAsync(cacheKey, rawToken, expiresIn, cancellationToken);
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
    /// 从时间戳计算过期时间间隔
    /// </summary>
    /// <param name="expirationTime">过期时间戳（毫秒）</param>
    /// <returns>从当前时间到过期时间的时间间隔</returns>
    /// <remarks>
    /// 根据令牌的过期时间戳计算剩余有效时间。
    /// 会考虑刷新阈值，提前认为令牌过期，以便自动刷新。
    /// </remarks>
    protected TimeSpan CalculateExpirationFromTimestamp(long expirationTime)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // 考虑刷新阈值，提前认为令牌过期
        var refreshThresholdMs = TimeSpan.FromSeconds(_options.TokenRefreshThreshold).TotalMilliseconds;
        var effectiveExpirationTime = expirationTime - (long)refreshThresholdMs;
        var remainingMs = effectiveExpirationTime - currentTime;
        return TimeSpan.FromMilliseconds(Math.Max(0, remainingMs));
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
        _logger.LogError("Feishu token request failed. TokenType: {TokenType}, AppId: {AppId}, Code: {Code}, Message: {Message}",
            _tokenType, _options.AppId, code, message);
        throw new FeishuException(code, message);
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
    public async Task<(int Total, int Expired)> GetCacheStatisticsAsync()
    {
        return await _tokenCache.GetStatisticsAsync(default);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <remarks>
    /// 实现IDisposable接口，释放所有托管资源。
    /// 如果缓存提供者实现了IDisposable，则一并释放。
    /// </remarks>
    public void Dispose()
    {
        if (_tokenCache is IDisposable disposableCache)
        {
            disposableCache.Dispose();
        }
    }
}
