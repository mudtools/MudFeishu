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
/// <para>
/// 负责用户访问令牌（User Access Token）的获取、缓存和管理。
/// 用户令牌用于用户级别的权限验证，通过授权码（Code）换取用户令牌。
/// 继承 Mud.HttpUtils v2.0 的 UserTokenManagerBase，获得内置并发安全、自动清理等能力。
/// </para>
/// <para>
/// <b>持久化接线</b>：注入 <see cref="IUserTokenStore"/> 时，基类缓存改为
/// <see cref="TokenStoreBackedTokenCache{T}"/>（键映射为裸 userId → 固定 tokenType，物理键与改造前逐字节一致），
/// 读走管线 S2 读穿透、写经基类缓存写入自动写穿，因此登录 / 刷新 / 宿主存入流程无需手工落库；
/// 管理器字段持有的 store 亦经 <see cref="PurgeGateUserTokenStoreDecorator"/> 包装，
/// 使 refresh 候选读取 / D12 过期清理 / CAS 等直调路径共享清库门与读容错口径。
/// </para>
/// <para>
/// D1 契约例外（TMA-01）：<c>InvalidateUserTokenAsync</c> 不得清除 <c>IUserTokenStore</c> 中的 refresh_token。
/// 理由：用户令牌的唯一续期路径是 <c>RefreshUserTokenAsync</c> → <c>LoadRefreshCandidateAsync</c> →
/// 从 store 取 refresh_token 做 OAuth 交换；清 store 会使恢复彻底无路（表现为必然 401），
/// 而用户侧"内存+store 双清"已由 <c>RemoveTokenAsync</c>（显式登出语义）承担。
/// 实现方式：覆写为 <c>UserTokenManagerBase.InvalidateUserAccessTokenInCache</c>（上游 U-1 入口，
/// 语义 = 仅置空访问令牌字段 + 保序写回 + 写入代际作废）—— 桥接器写穿时跳过访问令牌、保留 refresh。
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
        : base(BuildUserTokenCache(userTokenStore, options, logger))
    {
        _currentUserContext = currentUserContext;
        _authenticationApi = authenticationApi ?? throw new ArgumentNullException(nameof(authenticationApi));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tokenTypeKey = $"UserAccessToken:{_options.AppKey}";
        // 直调路径（refresh 候选读取 / D12 过期清理 / TMF-05 CAS / 登出删除）与桥接器共享同一 store，
        // 经装饰器统一获得清库门（D10）与读容错口径 —— 管理器内不再各自判定门、各自 try/catch。
        _userTokenStore = userTokenStore is null
            ? null
            : new PurgeGateUserTokenStoreDecorator(userTokenStore, _options.AppKey, _logger);
    }

    protected override int UserExpireThresholdSeconds => _options.TokenRefreshThreshold;

    /// <summary>
    /// TMR-P2-11（F11）：用户标识（OpenId/userId）日志脱敏统一出口。
    /// 与中间件默认脱敏策略一致——用户标识属敏感信息，明文入日志会造成用户画像泄露面。
    /// </summary>
    /// <param name="userId">用户标识（OpenId/userId）</param>
    /// <returns>脱敏后的用户标识</returns>
    private static string Masked(string? userId) => SensitiveDataUtils.MaskSensitiveData(userId);

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
            // 写缓存即完成持久化（桥接器写穿），不再手工落库。
            UpdateUserTokenCache(res.OpenId!, tokenInfo);
        }
        else
        {
            // OAuth v2 端点不返回 OpenId，使用 access_token 直接调用用户信息 API 获取 OpenId。
            // IFeishuAuthentication.GetUserInfoAsync 接受显式 token 参数，不走令牌管理基础设施，
            // 因此不存在循环依赖问题。
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
            _logger.LogWarning("Refresh token expired for userId: {UserId}, purging store entry", Masked(userId));
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

        _logger.LogInformation("Refreshing user token for userId: {UserId}", Masked(userId));

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
                    Masked(userId), res?.Code, res?.Msg);
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
                        _logger.LogWarning(ex, "Failed to purge refresh token after non-retryable error for userId: {UserId}", Masked(userId));
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
        return tokenInfo;
    }

    /// <inheritdoc />
    /// <remarks>
    /// D1 契约例外（TMA-01）：只失效<b>访问令牌字段</b>，保留 store 中的 refresh_token ——
    /// 401 恢复链路依赖 store 的 refresh 可达（清 store 会使恢复彻底无路，表现为必然 401）。
    /// <para>
    /// 基类实现为「整条移除」（<c>UserTokenManagerBase.InvalidateUserTokenAsync</c>），经桥接器会写穿删除
    /// access+refresh 两个物理键，故<b>不得</b>委派基类实现；改走上游 TR-02 入口
    /// <c>InvalidateUserAccessTokenInCache</c>（U-1）：字段置空 + 保序写回 + 写入代际作废，
    /// 桥接器写穿时跳过访问令牌、保留 refresh 槽位；镜像未命中时植入空访问令牌墓碑，
    /// 防止读穿透把持久层中已被拒绝的访问令牌重新带回。
    /// </para>
    /// </remarks>
    public override Task InvalidateUserTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.CompletedTask;

        InvalidateUserAccessTokenInCache(userId);
        return Task.CompletedTask;
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
    /// <remarks>
    /// 镜像优先；镜像未命中时从持久层恢复（经装饰器：清库门 + 读容错）。
    /// <para>
    /// 镜像存在但访问令牌为空的<b>失效墓碑</b>（<c>InvalidateUserTokenAsync</c> 的产物）直接返回 null，
    /// 且<b>不得</b>回落持久层 —— 否则会把持久层中已被拒绝的访问令牌重新带回，使失效被静默撤销。
    /// 其余镜像条目沿用既有语义（无论是否临近过期都原样返回，有效性由调用方判定）。
    /// </para>
    /// </remarks>
    public override async Task<UserTokenInfo?> GetTokenInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            return null;

        var cachedInfo = GetUserTokenFromCache(userId);
        if (cachedInfo != null)
            return string.IsNullOrEmpty(cachedInfo.AccessToken) ? null : cachedInfo;

        if (_userTokenStore == null)
            return null;

        var restoredInfo = await TryRestoreFromUserTokenStoreAsync(userId, cancellationToken).ConfigureAwait(false);
        if (restoredInfo == null)
            return null;

        // 回填镜像（桥接下同时幂等写穿持久层，保证恢复结果与持久层口径一致）
        UpdateUserTokenCache(userId, restoredInfo);
        return restoredInfo;
    }

    /// <inheritdoc />
    public async Task StoreUserTokenAsync(string userId, UserTokenInfo tokenInfo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId) || tokenInfo == null)
            return;

        UpdateUserTokenCache(userId, tokenInfo);
    }

    /// <summary>
    /// 刷新令牌的核心实现（TokenManagerBase 要求的抽象方法）
    /// 用户令牌不支持通过此方法刷新，用户令牌使用 RefreshUserTokenAsync 方法
    /// </summary>
    protected override Task<CredentialToken> RefreshTokenCoreAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException("User tokens should be refreshed via RefreshUserTokenAsync method.");
    }

    /// <summary>
    /// 从持久层恢复用户令牌（镜像未命中路径）。
    /// </summary>
    /// <remarks>
    /// 清库门（D10）与读故障容错由 <see cref="PurgeGateUserTokenStoreDecorator"/> 在 store 读边界统一承担，
    /// 本方法不再自行判定门、不再逐点 try/catch（判定与告警口径与改造前逐行等价）。
    /// </remarks>
    private async Task<UserTokenInfo?> TryRestoreFromUserTokenStoreAsync(string userId, CancellationToken cancellationToken)
    {
        if (_userTokenStore == null)
            return null;

        try
        {
            var storedAccessToken = await _userTokenStore.GetAccessTokenAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(storedAccessToken))
                return null;

            var (accessToken, accessTokenExpireMs) = FeishuTokenBridgeCodec.DecodeToken(storedAccessToken);

            var storedRefreshToken = await _userTokenStore.GetRefreshTokenAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
            var (refreshToken, refreshTokenExpireMs) = FeishuTokenBridgeCodec.DecodeToken(storedRefreshToken);

            _logger.LogDebug("Restored user token from IUserTokenStore for userId: {UserId}", Masked(userId));

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (accessTokenExpireMs > 0 && accessTokenExpireMs <= now)
            {
                _logger.LogDebug("Restored user access token has expired for userId: {UserId}, skipping", Masked(userId));
                return null;
            }

            // TMR-P2-12（F12）：与租户路径（FeishuAppTokenManagerBase.TryRestoreFromStoreAsync，D9）同源——
            // 临近 TokenRefreshThreshold 的恢复结果直接弃用，避免"恢复命中 → 组件判临近过期 → 立即再刷新"
            // 的自循环（缓存永不命中的资源放大）。IssuedAt=0 的保守代价（TMF-06 方案 A）维持不变，
            // 本项只对齐恢复弃用阈值，不改存储格式。
            if (accessTokenExpireMs > 0)
            {
                var restoreThresholdMs = _options.TokenRefreshThreshold * 1000L;
                if ((accessTokenExpireMs - now) <= restoreThresholdMs)
                {
                    _logger.LogDebug("Restored user access token is near expiration, skipping. userId: {UserId}", Masked(userId));
                    return null;
                }
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
            _logger.LogWarning(ex, "Failed to restore user token from IUserTokenStore for userId: {UserId}", Masked(userId));
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

        // TMR2-P1-5：凭据变更清库进行中——不得用旧凭据来源的 refresh token 发起 OAuth 交换。
        // 门判定（含 fail-open）由 PurgeGateUserTokenStoreDecorator 在 store 读边界承担：
        // Pending ⇒ 读返回 null ⇒ 本方法返回 null ⇒ RefreshUserTokenAsync 返回 null（退避）、
        // CanRefreshTokenAsync 返回 false（与改造前短路行为等价）。
        try
        {
            // 先读 refresh token（D11：refresh 的可达性独立于 access）
            var storedRefreshToken = await _userTokenStore.GetRefreshTokenAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(storedRefreshToken))
                return null;

            var (refreshToken, refreshTokenExpireMs) = FeishuTokenBridgeCodec.DecodeToken(storedRefreshToken);
            if (string.IsNullOrEmpty(refreshToken))
                return null;

            // TMA2-06 / D12：校验 refresh token 自身的过期时间。
            if (refreshTokenExpireMs > 0 && refreshTokenExpireMs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                _logger.LogDebug("Refresh token expired for userId: {UserId}, not loading candidate", Masked(userId));
                return null;
            }

            // 再读 access token（可能过期或缺失，不影响 refresh 的可达性）
            var storedAccessToken = await _userTokenStore.GetAccessTokenAsync(userId, _tokenTypeKey, cancellationToken).ConfigureAwait(false);
            string? accessToken = null;
            long accessTokenExpireMs = 0;
            if (!string.IsNullOrEmpty(storedAccessToken))
            {
                var (decodedToken, decodedExpireMs) = FeishuTokenBridgeCodec.DecodeToken(storedAccessToken);
                accessToken = decodedToken;
                accessTokenExpireMs = decodedExpireMs;

                // access 过期则不返回（但不影响 refresh 的可达性）
                if (accessTokenExpireMs > 0 && accessTokenExpireMs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    accessToken = null;
                    accessTokenExpireMs = 0;
                }
            }

            _logger.LogDebug("Loaded refresh token candidate from IUserTokenStore for userId: {UserId}", Masked(userId));

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
            _logger.LogWarning(ex, "Failed to load refresh token candidate from IUserTokenStore for userId: {UserId}", Masked(userId));
            return null;
        }
    }

    /// <summary>
    /// 构建用户令牌缓存：注入 <see cref="IUserTokenStore"/> 时装配桥接器，否则返回 null
    /// 交由基类使用其默认进程内缓存（与原无参构造行为完全一致）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 本方法在基类构造链上执行，<b>不</b>承担参数校验（构造函数体统一负责，保持既有异常类型与参数名）。
    /// </para>
    /// <para>
    /// <b>键映射（BD-1）</b>：缓存键恒为裸 userId（全仓无 scopes 传入），映射为
    /// <c>(userId, "UserAccessToken:{AppKey}")</c> ⇒ 物理键
    /// <c>{KeyPrefix}:user:{userId}:UserAccessToken\:{AppKey}:access</c>，与改造前逐字节一致。
    /// <b>禁止</b>使用 <c>DefaultUserKeyMapper</c>（裸键会被映射为 <c>(userId, userId)</c>，写出全新键格式）。
    /// </para>
    /// <para>
    /// F-06（Mud.HttpUtils ≥3.0.4，B3）：用户维度的移除/登出语义完全依赖基类
    /// <c>UserTokenManagerBase.RemoveTokenAsync</c> 的<b>异步写穿</b>——桥接器
    /// <see cref="TokenStoreBackedTokenCache{UserTokenInfo}"/> 的 <c>RemoveAsync</c> 无条件透传至
    /// <see cref="PurgeGateUserTokenStoreDecorator"/>，不再依赖本地镜像条目（多实例/冷启动一致落库）。
    /// 本类因此<b>不再覆写</b> <c>RemoveTokenAsync</c>；如需恢复覆写须同步提供冷启动对照测试。
    /// </para>
    /// </remarks>
    private static ITokenCache<UserTokenInfo>? BuildUserTokenCache(
        IUserTokenStore? userTokenStore,
        IOptions<FeishuAppConfig>? options,
        ILogger logger)
    {
        if (userTokenStore is null)
            return null;

        var appKey = options?.Value?.AppKey ?? string.Empty;
        var tokenTypeKey = $"UserAccessToken:{appKey}";

        return new TokenStoreBackedTokenCache<UserTokenInfo>(
            new PurgeGateUserTokenStoreDecorator(userTokenStore, appKey, logger),
            userKeyMapper: key => (key, tokenTypeKey),
            valueAdapter: AdaptUserTokenInfo,
            valueFactory: CreateUserTokenInfo,
            logger: logger);
    }

    /// <summary>
    /// 写穿方向的值适配：<see cref="UserTokenInfo"/> → 存储三元组。
    /// </summary>
    /// <remarks>
    /// 与改造前 <c>PersistUserTokenAsync</c> 逐条对齐：访问令牌剩余 ≤ 0 时不写 access（仅写 refresh 槽位）、
    /// refresh_token 为空时不写 refresh；两者的过期戳均按 <c>{expireMs}|{token}</c> 编码
    /// （refresh 过期戳供 Redis 端推导 TTL，<see cref="TokenStoreHelper.TryDecodeExpiry"/>）。
    /// </remarks>
    private static TokenStoreValue? AdaptUserTokenInfo(UserTokenInfo? tokenInfo)
        => tokenInfo is null
            ? null
            : new TokenStoreValue(
                accessToken: FeishuTokenBridgeCodec.EncodeToken(tokenInfo.AccessToken, tokenInfo.AccessTokenExpireTime),
                refreshToken: FeishuTokenBridgeCodec.EncodeToken(tokenInfo.RefreshToken, tokenInfo.RefreshTokenExpireTime),
                expiresInSeconds: FeishuTokenBridgeCodec.RemainingSeconds(tokenInfo.AccessTokenExpireTime));

    /// <summary>
    /// 读穿透方向的值工厂：存储三元组 → <see cref="UserTokenInfo"/>。
    /// </summary>
    /// <remarks>
    /// 两个槽位均无效（旧格式 / 损坏值）时返回 null ⇒ 读穿透视为未命中。
    /// 存储格式不含用户身份，<c>UserId</c>/<c>OpenId</c> 留空由调用方（<c>LoadRefreshCandidateAsync</c> /
    /// <c>GetTokenInfoAsync</c>）以入参 userId 回填。
    /// </remarks>
    private static UserTokenInfo? CreateUserTokenInfo(TokenStoreValue value)
    {
        var (accessToken, accessTokenExpireMs) = FeishuTokenBridgeCodec.DecodeToken(value.AccessToken);
        var (refreshToken, refreshTokenExpireMs) = FeishuTokenBridgeCodec.DecodeToken(value.RefreshToken);

        if (string.IsNullOrEmpty(accessToken) && string.IsNullOrEmpty(refreshToken))
            return null;

        return new UserTokenInfo
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpireTime = accessTokenExpireMs,
            RefreshTokenExpireTime = refreshTokenExpireMs,
            // TMF-06（方案 A）：存储格式不含签发时间，IssuedAt 保持 0
            // ⇒ 管线的 TTL 感知阈值退化为配置阈值（默认配置下判定结果一致）。
            IssuedAt = 0
        };
    }
}
