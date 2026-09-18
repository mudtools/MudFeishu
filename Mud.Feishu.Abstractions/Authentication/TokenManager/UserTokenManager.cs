// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Exceptions;

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 用户令牌管理器
/// </summary>
/// <remarks>
/// 负责用户访问令牌（User Access Token）的获取、缓存和管理。
/// 用户令牌用于用户级别的权限验证，通过授权码（Code）换取用户令牌。
/// 继承 Mud.HttpUtils v2.0 的 UserTokenManagerBase，获得内置并发安全、自动清理等能力。
/// 令牌缓存通过基类内置的 IMemoryCache 统一管理，确保读写一致性。
/// 可选注入 IUserTokenStore 实现分布式令牌持久化（如 Redis）。
/// <para>
/// D1 契约例外（TMA-01）：<c>UserTokenManager</c> 的 <c>InvalidateUserTokenAsync</c> 不得清除
/// <c>IUserTokenStore</c> 中的 refresh_token。理由：用户令牌的唯一续期路径是
/// <c>RefreshUserTokenAsync</c> → <c>GetTokenInfoAsync</c> → 从 store 取 refresh_token 做 OAuth 交换；
/// 清 store 会使恢复彻底无路（表现为必然 401），而用户侧"内存+store 双清"已由
/// <c>RemoveTokenAsync</c>（显式登出语义）承担。
/// </para>
/// </remarks>
internal class UserTokenManager : UserTokenManagerBase, IFeishuUserTokenManager
{
    private readonly IFeishuCurrentUserContext? _currentUserContext;
    private readonly IFeishuAuthentication _authenticationApi;
    private readonly FeishuAppConfig _options;
    private readonly ILogger<UserTokenManager> _logger;
    private readonly IUserTokenStore? _userTokenStore;
    private readonly string _tokenTypeKey;

    /// <summary>
    /// 初始化 UserTokenManager 实例
    /// </summary>
    /// <param name="currentUserContext">当前用户上下文（可选）</param>
    /// <param name="authenticationApi">飞书认证API接口</param>
    /// <param name="options">飞书配置选项</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="userTokenStore">用户令牌持久化存储（可选，用于分布式部署）</param>
    public UserTokenManager(
        IFeishuCurrentUserContext? currentUserContext,
        IFeishuAuthentication authenticationApi,
        IOptions<FeishuAppConfig> options,
        ILogger<UserTokenManager> logger,
        IUserTokenStore? userTokenStore = null)
    {
        _currentUserContext = currentUserContext;
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userTokenStore = userTokenStore;
        _tokenTypeKey = $"UserAccessToken:{_options.AppKey}";
    }

    protected override int UserExpireThresholdSeconds => _options.TokenRefreshThreshold;

    // TMA-23 修复：覆写 MetricsKey 使指标维度在多应用下可区分。
    // 返回 {TypeName}:{AppKey}，属性文档明确要求"稳定且不含敏感信息"。
    protected override string MetricsKey => $"UserTokenManager:{_options.AppKey}";

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

        if (_options.EnableLogging)
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

        if (string.IsNullOrWhiteSpace(res.AccessToken))
        {
            throw new FeishuException(443, "获取 UserAccessToken 失败: AccessToken为空");
        }

        // MT-07 / TMX-22（Mud.HttpUtils 2.0.5）：IssuedAt 必须由 IdP 侧填充。
        // 组件仅在 IssuedAt > 0 时才启用「TTL 感知的过期提前量」min(配置阈值, ttl/2)；
        // 缺失（0）会退化为纯配置阈值——对短 TTL 用户令牌（ttl <= 阈值）意味着
        // expire - threshold <= now 恒成立，令牌"刚签发即被判为需刷新"，缓存永不命中，
        // 每次取令牌都触发一次 OAuth 调用。此处以本次换取时刻作为签发时间。
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tokenInfo = new UserTokenInfo
        {
            UserId = string.Empty,
            OpenId = res.OpenId,
            UnionId = res.UnionId,
            AccessToken = res.AccessToken,
            RefreshToken = res.RefreshToken,
            AccessTokenExpireTime = issuedAt + ((res.ExpiresIn > 0 ? res.ExpiresIn : 7200) * 1000L),
            RefreshTokenExpireTime = issuedAt + ((res.RefreshTokenExpiresIn > 0 ? res.RefreshTokenExpiresIn : 30 * 24 * 3600) * 1000L),
            IssuedAt = issuedAt,
            Scope = res.Scope,
            Code = res.Code,
            Msg = res.Msg
        };

        if (!string.IsNullOrWhiteSpace(res.OpenId))
        {
            tokenInfo.UserId = res.OpenId!;
            UpdateUserTokenCache(res.OpenId!, tokenInfo);
            await PersistUserTokenAsync(res.OpenId!, tokenInfo, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // OAuth v2 端点不返回 OpenId，使用 access_token 直接调用用户信息 API 获取 OpenId。
            // IFeishuAuthentication.GetUserInfoAsync 接受显式 token 参数，不走令牌管理基础设施，
            // 因此不存在循环依赖问题。
            if (_options.EnableLogging)
                _logger.LogInformation("OAuth 端点未返回 OpenId，使用 access_token 获取用户信息");

            var userInfo = await _authenticationApi.GetUserInfoAsync(
                $"Bearer {res.AccessToken}", cancellationToken).ConfigureAwait(false);

            if (userInfo?.Data == null || string.IsNullOrEmpty(userInfo.Data.OpenId))
            {
                throw new FeishuException(
                    userInfo?.Code ?? 500,
                    $"获取用户信息失败: {userInfo?.Msg ?? "返回结果为null或OpenId为空"}");
            }

            tokenInfo.UserId = userInfo.Data.OpenId!;
            tokenInfo.OpenId = userInfo.Data.OpenId;
            tokenInfo.UnionId = userInfo.Data.UnionId;
            UpdateUserTokenCache(userInfo.Data.OpenId!, tokenInfo);
            await PersistUserTokenAsync(userInfo.Data.OpenId!, tokenInfo, cancellationToken).ConfigureAwait(false);
        }

        return tokenInfo;
    }

    /// <summary>
    /// 刷新用户令牌
    /// </summary>
    /// <remarks>
    /// TMA2-01 / D11：refresh token 的可达性独立于 access token。
    /// 此前 <c>RefreshUserTokenAsync</c> 调用 <c>GetTokenInfoAsync</c>，后者在 access token 过期时返回 <c>null</c>，
    /// 导致 refresh token 不可达——access 过期后（含进程重启、多实例接管）该用户必然 401 直到重新走 OAuth 授权。
    /// 现在使用 <c>LoadRefreshCandidateAsync</c> 读取 refresh token，即使 access token 已过期或缺失。
    /// <para>
    /// TMF-05：本方法为公共 API，宿主管线外直调<b>不</b>被组件 <c>KeyedLockTable</c> 键控锁
    /// 串行化（组件仅在 <c>GetOrRefreshTokenCoreAsync</c> 管线内持锁调用刷新，属 TMX-15-4
    /// 锁非重入不变式的姊妹约束）；不可重试失败清库前经 CAS 比对防御误删并发刷新刚持久化的新令牌。
    /// </para>
    /// </remarks>
    /// <param name="userId">用户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>刷新后的用户令牌信息，刷新失败返回 null</returns>
    public override async Task<UserTokenInfo?> RefreshUserTokenAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        // TMA2-01 / D11：使用 LoadRefreshCandidateAsync 而非 GetTokenInfoAsync，
        // 允许在 access token 过期或缺失时仍能读取 refresh token。
        var candidate = await LoadRefreshCandidateAsync(userId, cancellationToken).ConfigureAwait(false);
        if (candidate == null || string.IsNullOrEmpty(candidate.RefreshToken))
            return null;

        // TMA2-06 / D12：校验 refresh token 自身的过期时间。
        // 过期 → 删 store 条目、返回 null（进入退避）。
        if (candidate.RefreshTokenExpireTime > 0 && candidate.RefreshTokenExpireTime <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        {
            _logger.LogWarning("Refresh token expired for userId: {UserId}, purging store entry", userId);
            if (_userTokenStore != null)
            {
                try
                {
                    await _userTokenStore.RemoveAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to purge expired refresh token for userId: {UserId}", userId);
                }
            }
            return null;
        }

        if (_options.EnableLogging)
            _logger.LogInformation("Refreshing user token for userId: {UserId}", userId);

        var credentials = new OAuthRefreshTokenRequest
        {
            GrantType = "refresh_token",
            ClientId = _options.AppId,
            ClientSecret = _options.AppSecret,
            RefreshToken = candidate.RefreshToken
        };

        var res = await _authenticationApi.GetOAuthenRefreshAccessTokenAsync(credentials, cancellationToken);

        if (res == null || res.Code != 0)
        {
            // TMA2-06 / D12：OAuth 失败按错误码分类。
            // 不可重试集合（invalid_grant / refresh token 失效 / scope 不符）→ 清 store refresh token + 返回 null（进退避）。
            if (FeishuOAuthErrorClassifier.IsUnretryable(res?.Code, res?.Msg))
            {
                _logger.LogWarning(
                    "OAuth refresh failed with non-retryable error for userId: {UserId}, code: {Code}, msg: {Msg}. Purging refresh token.",
                    userId, res?.Code, res?.Msg);
                if (_userTokenStore != null)
                {
                    try
                    {
                        var encodedRefreshToken = await _userTokenStore.GetRefreshTokenAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(encodedRefreshToken))
                        {
                            // TMF-05（CAS）：仅当 store 仍持有本次尝试所用的 refresh token 时才清除，
                            // 防止宿主直调（绕过组件 KeyedLockTable）场景下误删并发刷新刚持久化的新令牌。
                            // 比较失败时跳过清库——新令牌由持有者管理；已知保守代价：Feishu 复用旧
                            // refresh_token 且两次持久化时间戳不同时会跳过清库（活性损失，非正确性问题）。
                            var attempted = TokenStoreHelper.EncodeStoredToken(candidate.RefreshToken!, candidate.RefreshTokenExpireTime);
                            if (string.Equals(encodedRefreshToken, attempted, StringComparison.Ordinal))
                            {
                                await _userTokenStore.RemoveAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(ex, "Failed to purge refresh token after non-retryable error for userId: {UserId}", userId);
                    }
                }
                return null;
            }

            // 可重试失败 → 抛异常（保留可见性，组件会记退避/负缓存）
            throw new FeishuException(res?.Code ?? 500, $"刷新 UserAccessToken 失败: {res?.Msg ?? "返回结果为null"}");
        }

        // MT-07 / TMX-22：同 GetUserTokenWithCodeAsync——刷新得到的令牌以本次刷新时刻为签发时间，
        // 使 TTL 感知阈值（min(配置阈值, ttl/2)）真正生效。
        var refreshedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tokenInfo = new UserTokenInfo
        {
            UserId = candidate.UserId,
            OpenId = candidate.OpenId ?? userId,
            UnionId = candidate.UnionId,
            AccessToken = res.AccessToken ?? candidate.AccessToken,
            RefreshToken = res.RefreshToken ?? candidate.RefreshToken,
            AccessTokenExpireTime = refreshedAt + ((res.ExpiresIn > 0 ? res.ExpiresIn : 7200) * 1000L),
            RefreshTokenExpireTime = refreshedAt + ((res.RefreshTokenExpiresIn > 0 ? res.RefreshTokenExpiresIn : 30 * 24 * 3600) * 1000L),
            IssuedAt = refreshedAt,
            Scope = candidate.Scope,
            Code = res.Code,
            Msg = res.Msg
        };

        UpdateUserTokenCache(userId, tokenInfo);
        await PersistUserTokenAsync(userId, tokenInfo, cancellationToken).ConfigureAwait(false);
        return tokenInfo;
    }

    /// <inheritdoc />
    public override async Task<bool> RemoveTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        RemoveUserTokenFromCache(userId);

        if (_userTokenStore != null)
        {
            await _userTokenStore.RemoveAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
        }

        return true;
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
    /// <remarks>
    /// TMA2-01 / D11：在 access token 过期但 refresh token 有效时返回 true。
    /// 此前使用 <c>GetTokenInfoAsync</c>，后者在 access 过期时返回 null → <c>CanRefreshTokenAsync</c> 返回 false。
    /// </remarks>
    public override async Task<bool> CanRefreshTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return false;

        // TMA2-01：先检查缓存（可能含未过期的 refresh token）
        var cachedInfo = GetUserTokenFromCache(userId);
        if (cachedInfo != null && !string.IsNullOrEmpty(cachedInfo.RefreshToken))
        {
            // 即使 access 过期，只要 refresh 有效就返回 true
            if (cachedInfo.RefreshTokenExpireTime <= 0 || cachedInfo.RefreshTokenExpireTime > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                return true;
        }

        // TMA2-01：缓存未命中或 refresh 过期时，从 store 加载候选
        var candidate = await LoadRefreshCandidateAsync(userId, cancellationToken).ConfigureAwait(false);
        return candidate != null && !string.IsNullOrEmpty(candidate.RefreshToken);
    }

    /// <inheritdoc />
    public override async Task<UserTokenInfo?> GetTokenInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        var cachedInfo = GetUserTokenFromCache(userId);
        if (cachedInfo != null)
            return cachedInfo;

        if (_userTokenStore != null)
        {
            var restoredInfo = await TryRestoreFromUserTokenStoreAsync(userId, cancellationToken).ConfigureAwait(false);
            if (restoredInfo != null)
            {
                UpdateUserTokenCache(userId, restoredInfo);
                return restoredInfo;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task StoreUserTokenAsync(string userId, UserTokenInfo tokenInfo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId) || tokenInfo == null)
            return;

        UpdateUserTokenCache(userId, tokenInfo);
        await PersistUserTokenAsync(userId, tokenInfo, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 刷新令牌的核心实现（TokenManagerBase 要求的抽象方法）
    /// 用户令牌不支持通过此方法刷新，用户令牌使用 RefreshUserTokenAsync 方法
    /// </summary>
    protected override Task<CredentialToken> RefreshTokenCoreAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException("User tokens should be refreshed via RefreshUserTokenAsync method.");
    }

    private async Task PersistUserTokenAsync(string userId, UserTokenInfo tokenInfo, CancellationToken cancellationToken)
    {
        if (_userTokenStore == null)
            return;

        try
        {
            var remainingSeconds = (tokenInfo.AccessTokenExpireTime - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) / 1000L;
            if (remainingSeconds > 0)
            {
                var encodedAccessToken = TokenStoreHelper.EncodeStoredToken(tokenInfo.AccessToken!, tokenInfo.AccessTokenExpireTime);
                await _userTokenStore.SetAccessTokenAsync(userId, _tokenTypeKey, encodedAccessToken, remainingSeconds, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(tokenInfo.RefreshToken))
            {
                var encodedRefreshToken = TokenStoreHelper.EncodeStoredToken(tokenInfo.RefreshToken!, tokenInfo.RefreshTokenExpireTime);
                await _userTokenStore.SetRefreshTokenAsync(userId, _tokenTypeKey, encodedRefreshToken, cancellationToken).ConfigureAwait(false);
            }
        }
        // NEW-TM-01 修复：过滤 OperationCanceledException，避免取消操作被误记录为失败
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to persist user token to IUserTokenStore for userId: {UserId}", userId);
        }
    }

    private async Task<UserTokenInfo?> TryRestoreFromUserTokenStoreAsync(string userId, CancellationToken cancellationToken)
    {
        if (_userTokenStore == null)
            return null;

        try
        {
            var storedAccessToken = await _userTokenStore.GetAccessTokenAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(storedAccessToken))
                return null;

            var (accessToken, accessTokenExpireMs) = TokenStoreHelper.DecodeStoredToken(storedAccessToken!);

            var storedRefreshToken = await _userTokenStore.GetRefreshTokenAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
            var (refreshToken, refreshTokenExpireMs) = !string.IsNullOrEmpty(storedRefreshToken)
                ? TokenStoreHelper.DecodeStoredToken(storedRefreshToken!)
                : (null, 0L);

            _logger.LogDebug("Restored user token from IUserTokenStore for userId: {UserId}", userId);

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (accessTokenExpireMs > 0 && accessTokenExpireMs <= now)
            {
                _logger.LogDebug("Restored user access token has expired for userId: {UserId}, skipping", userId);
                return null;
            }

            var safeExpireSeconds = _options.TokenRefreshThreshold + 60;

            // TMF-06（方案 A）：恢复令牌的 IssuedAt 不可知——{expire}|{token} 存储格式不含签发时间，
            // UserTokenInfo.IssuedAt 保持 0 → 组件 TTL 感知阈值（min(阈值, ttl/2)）退化为纯配置阈值
            // （TMX-22 语义）。默认配置（TTL 7200s ≫ 阈值 300s）下判定结果与 TTL 感知完全一致
            // （恢复令牌必为过去签发，纯阈值判定偏保守但正确）；短 TTL（ttl ≤ TokenRefreshThreshold）
            // 部署不应依赖跨重启的 store 恢复路径（README 部署提示同步声明）。
            // 格式升级备选（方案 B，短 TTL 场景真实落地时启用）见 .docs/令牌与多应用管理-审查修复与完善方案.md §七。
            return new UserTokenInfo
            {
                UserId = userId,
                OpenId = null,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpireTime = accessTokenExpireMs > 0 ? accessTokenExpireMs : now + (safeExpireSeconds * 1000L),
                RefreshTokenExpireTime = refreshTokenExpireMs
            };
        }
        // NEW-TM-01 修复：过滤 OperationCanceledException，避免取消操作被误记录为失败
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to restore user token from IUserTokenStore for userId: {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// TMA2-01 / D11：加载 refresh token 候选——独立于 access token 的存在性与有效性。
    /// 仅服务 <see cref="RefreshUserTokenAsync"/> 与 <see cref="CanRefreshTokenAsync"/>，
    /// 允许返回 <c>AccessToken = null</c> 的候选（<c>AccessTokenExpireTime = 0</c>）。
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>含 refresh token 的候选信息，无可用 refresh token 时返回 null</returns>
    private async Task<UserTokenInfo?> LoadRefreshCandidateAsync(string userId, CancellationToken cancellationToken)
    {
        // 先检查缓存（可能含有效 refresh token）
        var cachedInfo = GetUserTokenFromCache(userId);
        if (cachedInfo != null && !string.IsNullOrEmpty(cachedInfo.RefreshToken))
            return cachedInfo;

        if (_userTokenStore == null)
            return null;

        try
        {
            // 先读 refresh token（D11：refresh 的可达性独立于 access）
            var storedRefreshToken = await _userTokenStore.GetRefreshTokenAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(storedRefreshToken))
                return null;

            var (refreshToken, refreshTokenExpireMs) = TokenStoreHelper.DecodeStoredToken(storedRefreshToken!);
            if (string.IsNullOrEmpty(refreshToken))
                return null;

            // TMA2-06 / D12：校验 refresh token 自身的过期时间。
            if (refreshTokenExpireMs > 0 && refreshTokenExpireMs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                _logger.LogDebug("Refresh token expired for userId: {UserId}, not loading candidate", userId);
                return null;
            }

            // 再读 access token（可能过期或缺失，不影响 refresh 的可达性）
            var storedAccessToken = await _userTokenStore.GetAccessTokenAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
            string? accessToken = null;
            long accessTokenExpireMs = 0;
            if (!string.IsNullOrEmpty(storedAccessToken))
            {
                var (decodedToken, decodedExpireMs) = TokenStoreHelper.DecodeStoredToken(storedAccessToken!);
                accessToken = decodedToken;
                accessTokenExpireMs = decodedExpireMs;

                // access 过期则不返回（但不影响 refresh 的可达性）
                if (accessTokenExpireMs > 0 && accessTokenExpireMs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    accessToken = null;
                    accessTokenExpireMs = 0;
                }
            }

            _logger.LogDebug("Loaded refresh token candidate from IUserTokenStore for userId: {UserId}", userId);

            // TMA2-16 / P2-6：恢复时回填 OpenId/UnionId
            // store 中不持久化 OpenId/UnionId（编码值仅含 token + 过期戳），
            // 此处以 userId 兑底填充 OpenId，避免后续调用丢失用户标识。
            return new UserTokenInfo
            {
                UserId = userId,
                OpenId = userId,
                UnionId = null,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpireTime = accessTokenExpireMs,
                RefreshTokenExpireTime = refreshTokenExpireMs
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load refresh token candidate from IUserTokenStore for userId: {UserId}", userId);
            return null;
        }
    }

}
